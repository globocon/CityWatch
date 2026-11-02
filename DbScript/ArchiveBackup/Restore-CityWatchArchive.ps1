<#
.SYNOPSIS
    Puts GuardLogs (with their photo / attachment records) and ClientSiteRadioChecksActivityStatus_History
    rows back into prod-citywatch from prod_citywatch_backup. Run by hand.

.DESCRIPTION
    Only rows that are MISSING from prod are inserted (matched on Id, which is kept), so running it
    twice, or over a range where most rows still exist, is harmless. Nothing in prod is updated or
    deleted.

    Without -Execute it only reports what it would do: how many archive rows match, how many are
    already in prod, how many would be restored, and how many cannot be (their logbook / guard /
    site / KV / IR no longer exists in prod - the foreign keys would reject them). Run it that way
    first.

    With both tables, GuardLogs is restored first, because history rows point at log entries (LBId).
    Restoring GuardLogs also restores their GuardLogsDocumentImages rows (the photo / attachment
    records; the files themselves are never touched).

    Rows older than 18 months are deleted from prod again by the nightly Backup-CityWatchArchive.ps1.
    So restored rows are put on hold (dbo.ProdDeleteHold) for HoldDays days (default 30); the nightly
    job leaves them alone until then. Use -HoldDays 0 for no hold.

    Restored log entries do not count as guard activity: the script sets the session flag that the
    Insert_GuardLogs trigger from DbScript/385 checks, and refuses to run if that trigger is not in
    place (the old trigger would credit the newest entry in prod instead of the restored ones).

    Needs: INSERT and ALTER on the three prod tables (IDENTITY_INSERT), and on the archive read +
    INSERT on dbo.ProdDeleteHold / dbo.ArchiveRunLog.

.EXAMPLE
    # What would come back for 1-15 March 2026, site 887
    .\Restore-CityWatchArchive.ps1 -FromDate 2026-03-01 -ToDate 2026-03-16 -ClientSiteId 887

.EXAMPLE
    # Do it
    .\Restore-CityWatchArchive.ps1 -FromDate 2026-03-01 -ToDate 2026-03-16 -ClientSiteId 887 -Execute

.EXAMPLE
    # Particular log entries only
    .\Restore-CityWatchArchive.ps1 -Table GuardLogs -Id 4388966,4388967 -Execute
#>
[CmdletBinding()]
param(
    [ValidateSet('GuardLogs', 'ClientSiteRadioChecksActivityStatus_History', 'Both')]
    [string]$Table = 'Both',
    # EventDateTime range: from (inclusive) .. to (exclusive)
    [datetime]$FromDate,
    [datetime]$ToDate,
    # Lists: -Id 1,2,3 (also works as "1,2,3" when started with powershell.exe -File)
    [string[]]$ClientSiteId,
    [string[]]$Id,
    # Restore every archived row missing from prod (no date / id filter). Use with care.
    [switch]$All,
    [switch]$Execute,
    # Days the nightly job must not delete the restored rows from prod again (0 = no hold)
    [ValidateRange(0, 3650)][int]$HoldDays = 30,
    [string]$SqlServer = 'localhost',
    [string]$SourceDb = 'prod-citywatch',
    [string]$BackupDb = 'prod_citywatch_backup',
    [ValidateRange(100, 100000)][int]$BatchSize = 5000,
    [ValidateRange(0, 60000)][int]$DelayMs = 200,
    [string]$LogDir = (Join-Path $PSScriptRoot 'logs'),
    [string]$ConnectionString
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function ConvertTo-IntList([string[]]$Values, [string]$Name) {
    $list = @()
    foreach ($part in ($Values -join ',').Split(',')) {
        $p = $part.Trim()
        if ($p -eq '') { continue }
        $n = 0
        if (-not [int]::TryParse($p, [ref]$n)) { throw "-${Name}: '$p' is not a whole number" }
        $list += $n
    }
    return , $list
}
$Id = ConvertTo-IntList $Id 'Id'
$ClientSiteId = ConvertTo-IntList $ClientSiteId 'ClientSiteId'

if (($PSBoundParameters.ContainsKey('FromDate') -xor $PSBoundParameters.ContainsKey('ToDate'))) { throw 'Give both -FromDate and -ToDate' }
if ($PSBoundParameters.ContainsKey('FromDate') -and $ToDate -le $FromDate) { throw '-ToDate must be after -FromDate' }
if (-not $All -and -not $PSBoundParameters.ContainsKey('FromDate') -and -not $Id) { throw 'Give -FromDate/-ToDate or -Id (or -All to restore everything missing)' }

$runId = [guid]::NewGuid()
$runStarted = Get-Date
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }
$logFile = Join-Path $LogDir ('Restore-CityWatchArchive_{0:yyyyMMdd_HHmmss}.log' -f $runStarted)

function Write-Log([string]$Message, [string]$Level = 'INFO') {
    $line = '{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}' -f (Get-Date), $Level, $Message
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

function Q([string]$Name) { '[' + $Name.Replace(']', ']]') + ']' }

$src = Q $SourceDb

$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
if ($ConnectionString) { $builder.ConnectionString = $ConnectionString }
else { $builder['Data Source'] = $SqlServer; $builder['Integrated Security'] = $true }
$builder['Initial Catalog'] = $BackupDb
$builder['Application Name'] = 'CityWatch archive restore'
$conn = New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString

function Invoke-Sql([string]$Sql, [hashtable]$Params = @{}, [int]$TimeoutSec = 600, [switch]$Scalar) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = $TimeoutSec
    foreach ($k in $Params.Keys) {
        $v = $Params[$k]
        if ($null -eq $v) { $v = [DBNull]::Value }
        [void]$cmd.Parameters.AddWithValue('@' + $k, $v)
    }
    try {
        if ($Scalar) {
            $r = $cmd.ExecuteScalar()
            if ($r -is [DBNull]) { return $null }
            return $r
        }
        $result = New-Object System.Data.DataTable
        $reader = $cmd.ExecuteReader()
        try { $result.Load($reader) } finally { $reader.Close() }
        return , $result
    }
    finally { $cmd.Dispose() }
}

# Columns both copies have (the archive's own ArchivedAtUtc is left out)
function Get-Columns([string]$TableName) {
    $t = Q $TableName
    $cols = Invoke-Sql "
        SELECT c.name FROM $src.sys.columns c
        WHERE c.object_id = OBJECT_ID(N'$src.dbo.$t') AND c.is_computed = 0
          AND c.name COLLATE DATABASE_DEFAULT IN (SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.$t'))
        ORDER BY c.column_id;"
    return (@($cols.Rows | ForEach-Object { Q $_.name }) -join ', ')
}

# "Parent still exists" for every enabled single-column foreign key on the prod table, read from
# prod itself so it matches prod's schema
function Get-ParentChecks([string]$TableName) {
    $t = Q $TableName
    $fks = Invoke-Sql "
        SELECT pc.name AS ColumnName,
               OBJECT_SCHEMA_NAME(fk.referenced_object_id, DB_ID(@Db)) AS RefSchema,
               OBJECT_NAME(fk.referenced_object_id, DB_ID(@Db)) AS RefTable,
               rc.name AS RefColumn
        FROM $src.sys.foreign_keys fk
        JOIN $src.sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN $src.sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
        JOIN $src.sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
        WHERE fk.parent_object_id = OBJECT_ID(N'$src.dbo.$t') AND fk.is_disabled = 0
          AND (SELECT COUNT(*) FROM $src.sys.foreign_key_columns x WHERE x.constraint_object_id = fk.object_id) = 1;" @{ Db = $SourceDb }
    $checks = @()
    foreach ($fk in $fks.Rows) {
        $c = Q $fk.ColumnName
        $checks += "(b.$c IS NULL OR EXISTS (SELECT 1 FROM $src.$(Q $fk.RefSchema).$(Q $fk.RefTable) r WHERE r.$(Q $fk.RefColumn) = b.$c))"
    }
    if ($checks.Count -eq 0) { return '1 = 1' }
    return ($checks -join "`n              AND ")
}

# Which archive rows the user asked for ($A = the table alias in the query)
function Get-Filter([string]$TableName, [string]$A = 'b') {
    # Photo records: those of the archived log entries the filter picks (-Id means log entry ids)
    if ($TableName -eq 'GuardLogsDocumentImages') {
        return "$A.GuardLogId IN (SELECT g.Id FROM dbo.GuardLogs g WHERE $(Get-Filter 'GuardLogs' 'g'))"
    }
    $parts = @()
    if ($script:FromGiven) { $parts += "$A.EventDateTime >= @FromDate AND $A.EventDateTime < @ToDate" }
    if ($Id) { $parts += ("$A.Id IN ({0})" -f (($Id | ForEach-Object { [int]$_ }) -join ',')) }
    if ($ClientSiteId) {
        $sites = ($ClientSiteId | ForEach-Object { [int]$_ }) -join ','
        if ($TableName -eq 'GuardLogs') {
            $parts += "$A.ClientSiteLogBookId IN (SELECT lb.Id FROM $src.dbo.ClientSiteLogBooks lb WHERE lb.ClientSiteId IN ($sites))"
        }
        else { $parts += "$A.ClientSiteId IN ($sites)" }
    }
    if ($parts.Count -eq 0) { return '1 = 1' }
    return ($parts -join ' AND ')
}
$script:FromGiven = $PSBoundParameters.ContainsKey('FromDate')

function Restore-Table([string]$TableName) {
    $t = Q $TableName
    $columns = Get-Columns $TableName
    $filter = Get-Filter $TableName
    $parents = Get-ParentChecks $TableName
    $dateParams = @{ FromDate = $(if ($script:FromGiven) { $FromDate } else { $null }); ToDate = $(if ($script:FromGiven) { $ToDate } else { $null }) }

    # Flags per row first: SQL Server cannot SUM an expression containing a subquery
    $counts = Invoke-Sql "
        SELECT COUNT_BIG(*) AS Matching, SUM(x.InProd) AS InProd, MIN(x.Id) AS MinId, MAX(x.Id) AS MaxId
        FROM (SELECT b.Id, CASE WHEN EXISTS (SELECT 1 FROM $src.dbo.$t p WHERE p.Id = b.Id) THEN 1 ELSE 0 END AS InProd
              FROM dbo.$t b
              WHERE $filter) x;" $dateParams
    $missing = Invoke-Sql "
        SELECT COUNT_BIG(*) AS Missing, SUM(x.Restorable) AS Restorable
        FROM (SELECT CASE WHEN $parents THEN 1 ELSE 0 END AS Restorable
              FROM dbo.$t b
              WHERE $filter AND NOT EXISTS (SELECT 1 FROM $src.dbo.$t p WHERE p.Id = b.Id)) x;" $dateParams

    $c = $counts.Rows[0]; $m = $missing.Rows[0]
    $matching = [long]$c.Matching
    $inProd = if ($c.InProd -is [DBNull]) { 0 } else { [long]$c.InProd }
    $missingCount = [long]$m.Missing
    $restorable = if ($m.Restorable -is [DBNull]) { 0 } else { [long]$m.Restorable }
    $skipped = $missingCount - $restorable
    Write-Log ("{0}: {1:N0} archived rows match; {2:N0} already in prod; {3:N0} to restore; {4:N0} cannot be restored (parent row gone from prod)" -f `
        $TableName, $matching, $inProd, $restorable, $skipped)

    if (-not $Execute -or $restorable -eq 0) { return }

    $logId = Invoke-Sql "INSERT INTO dbo.ArchiveRunLog (RunId, Operation, TableName, StartedAtUtc, FromId, ToId, Status, Message)
                         OUTPUT inserted.Id VALUES (@RunId, 'Restore', @Table, SYSUTCDATETIME(), @MinId, @MaxId, 'Running', @Filter);" @{
        RunId = $runId; Table = $TableName; MinId = $c.MinId; MaxId = $c.MaxId; Filter = $filter } -Scalar

    $restored = [long]0
    try {
        # Trigger skip flag and IDENTITY_INSERT apply to this session only
        [void](Invoke-Sql "EXEC sp_set_session_context N'CityWatch.SkipActivityTriggers', 1;")
        [void](Invoke-Sql "SET IDENTITY_INSERT $src.dbo.$t ON;")
        try {
            $batchSql = "
                SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000;
                INSERT INTO $src.dbo.$t ($columns)
                SELECT $columns FROM dbo.$t b
                WHERE b.Id BETWEEN @Lo AND @Hi AND $filter
                  AND NOT EXISTS (SELECT 1 FROM $src.dbo.$t p WHERE p.Id = b.Id)
                  AND $parents
                ORDER BY b.Id
                OPTION (MAXDOP 1);
                SELECT @@ROWCOUNT;"
            $lo = [int]$c.MinId
            $maxId = [int]$c.MaxId
            while ($lo -le $maxId) {
                $hi = [int][math]::Min([long]$lo + $BatchSize - 1, [long]$maxId)
                $p = $dateParams.Clone(); $p.Lo = $lo; $p.Hi = $hi
                $restored += [long](Invoke-Sql $batchSql $p -Scalar)
                $lo = $hi + 1
                if ($DelayMs -gt 0) { Start-Sleep -Milliseconds $DelayMs }
            }
        }
        finally {
            [void](Invoke-Sql "SET IDENTITY_INSERT $src.dbo.$t OFF;")
        }
        [void](Invoke-Sql "UPDATE dbo.ArchiveRunLog SET FinishedAtUtc = SYSUTCDATETIME(), Status = 'Succeeded', RowsCopied = @N, RowsSkipped = @Skipped WHERE Id = @LogId;" @{
            N = $restored; Skipped = $skipped; LogId = $logId })
        Write-Log ("{0}: restored {1:N0} rows" -f $TableName, $restored)

        # Keep the nightly job from deleting them again (photo records go with their log entries)
        if ($HoldDays -gt 0 -and $restored -gt 0 -and $TableName -ne 'GuardLogsDocumentImages') {
            [void](Invoke-Sql "INSERT INTO dbo.ProdDeleteHold (TableName, FromId, ToId, HoldUntilUtc, RestoreRunId)
                               VALUES (@Table, @MinId, @MaxId, DATEADD(DAY, @Days, SYSUTCDATETIME()), @RunId);" @{
                Table = $TableName; MinId = $c.MinId; MaxId = $c.MaxId; Days = $HoldDays; RunId = $runId })
            Write-Log ("{0}: Id {1} to {2} held from the nightly prod delete for {3} days" -f $TableName, $c.MinId, $c.MaxId, $HoldDays)
        }
    }
    catch {
        $msg = $_.Exception.Message
        try {
            [void](Invoke-Sql "UPDATE dbo.ArchiveRunLog SET FinishedAtUtc = SYSUTCDATETIME(), Status = 'Failed', RowsCopied = @N, Message = @Msg WHERE Id = @LogId;" @{
                N = $restored; Msg = $msg; LogId = $logId })
        } catch { }
        throw ("{0}: failed after restoring {1:N0} rows - {2}" -f $TableName, $restored, $msg)
    }
}

$ok = $true
try {
    $mode = if ($Execute) { 'RESTORE' } else { 'report only (add -Execute to restore)' }
    Write-Log ("Archive restore {0}: {1} -> {2} on {3}; {4}" -f $runId, $BackupDb, $SourceDb, $builder['Data Source'], $mode)
    $conn.Open()

    $lock = Invoke-Sql "DECLARE @r INT; EXEC @r = sp_getapplock @Resource = N'CityWatchArchive', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @r;" -Scalar
    if ($lock -lt 0) { throw 'An archive backup or restore is running; try again when it has finished' }

    $tablesToDo = switch ($Table) {
        'Both' { @('GuardLogs', 'GuardLogsDocumentImages', 'ClientSiteRadioChecksActivityStatus_History') }
        'GuardLogs' { @('GuardLogs', 'GuardLogsDocumentImages') }
        default { @($Table) }
    }

    if ($Execute -and $tablesToDo -contains 'GuardLogs') {
        $trigger = Invoke-Sql "SELECT m.definition FROM $src.sys.sql_modules m WHERE m.object_id = OBJECT_ID(N'$src.dbo.Insert_GuardLogs');" -Scalar
        if ($trigger -and $trigger -notlike '*CityWatch.SkipActivityTriggers*') {
            throw 'Insert_GuardLogs in prod is the old version - apply DbScript/385 first, or restored entries would be credited to the newest log entry as guard activity'
        }
    }

    foreach ($tableName in $tablesToDo) { Restore-Table $tableName }
}
catch {
    $ok = $false
    Write-Log ("FAILED - {0}" -f $_.Exception.Message) 'ERROR'
}
finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
    Write-Log ("Finished in {0:N1} minutes" -f ((Get-Date) - $runStarted).TotalMinutes)
}

if ($ok) { exit 0 } else { exit 1 }
