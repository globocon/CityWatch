<#
.SYNOPSIS
    Nightly: move GuardLogs and ClientSiteRadioChecksActivityStatus_History rows older than 18 months
    (with the log entries' GuardLogsDocumentImages photo records) from the source database into the
    archive database, deleting from the source only what was archived.

.DESCRIPTION
    -ConnectionString connects to the SOURCE database (its Database / Initial Catalog), e.g.
        "Server=10.0.2.5;Database=prod-citywatch;User Id=...;Password=...;TrustServerCertificate=True;"
    -BackupDb is the archive database, on the same SQL Server instance (default prod_citywatch_backup;
    create it once with 01_Create_prod_citywatch_backup.sql).

    What it does:
      1. Looks for rows older than ProdKeepMonths (18) months in the source: history first, then
         GuardLogs (history rows point at log entries). If there are none, it stops.
      2. Moves them in batches of BatchSize (1000). Each batch is ONE transaction:
           - pick the batch's old rows and lock them;
           - copy them into the archive (for GuardLogs also their photo records, which the source
             would otherwise delete with them through ON DELETE CASCADE);
           - delete them from the source - only rows whose archive copy now exists;
           - if the picked / archived / deleted counts differ, roll the batch back and stop.
         So a row is only deleted from the source after it has been archived, and a failure leaves
         both databases as they were before that batch.

    Left in the source: GuardLogs rows still referenced there (ClientSiteRadioChecksActivityStatus,
    or a history row that is kept), and rows on hold after a restore (archive dbo.ProdDeleteHold).

    Light on the source: 1000-row batches (no table locks), DelayMs pause between batches, gives way
    to the app on lock waits, pauses while the source's transaction log is over MaxLogPercent full,
    stops after MaxRunMinutes (the next run carries on).

    Needs DbScript/385 applied to the source (its LBId index): without it every GuardLogs delete
    scans the whole history table, so the script stops if it is missing.

    -ReportOnly: count what would be moved, change nothing.

    The login needs on the source db_datareader, db_datawriter and VIEW DATABASE STATE, and on the
    archive db_owner (or read / write on its tables).

    Exit code 0 = finished (or stopped at the time / log limit, to carry on next run), 1 = failed.

.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File Backup-CityWatchArchive.ps1 -ConnectionString "Server=...;Database=prod-citywatch;..." -ReportOnly
#>
[CmdletBinding()]
param(
    # Connects to the source database (Database / Initial Catalog in the string)
    [string]$ConnectionString,
    [string]$BackupDb = 'prod_citywatch_backup',
    [ValidateRange(1, 120)][int]$ProdKeepMonths = 18,
    [ValidateRange(100, 4000)][int]$BatchSize = 1000,
    [ValidateRange(0, 60000)][int]$DelayMs = 300,
    [ValidateRange(1, 1440)][int]$MaxRunMinutes = 240,
    [ValidateRange(10, 95)][int]$MaxLogPercent = 70,
    [switch]$ReportOnly,
    # Default: a logs folder next to the script
    [string]$LogDir,
    [ValidateRange(1, 3650)][int]$KeepLogDays = 30
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$runId = [guid]::NewGuid()
$runStarted = Get-Date
$deadline = $runStarted.AddMinutes($MaxRunMinutes)

# Worked out here, not as the parameter default: Windows PowerShell 5.1 can leave $PSScriptRoot
# empty while parameter defaults are evaluated
if (-not $LogDir) {
    $scriptDir = $PSScriptRoot
    if (-not $scriptDir -and $MyInvocation.MyCommand.Path) { $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path }
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
    $LogDir = Join-Path $scriptDir 'logs'
}
if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }
$logFile = Join-Path $LogDir ('Backup-CityWatchArchive_{0:yyyyMMdd_HHmmss}.log' -f $runStarted)
Get-ChildItem -Path $LogDir -Filter 'Backup-CityWatchArchive_*.log' -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepLogDays) } |
    Remove-Item -Force -ErrorAction SilentlyContinue

function Write-Log([string]$Message, [string]$Level = 'INFO') {
    $line = '{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}' -f (Get-Date), $Level, $Message
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

function Q([string]$Name) { '[' + $Name.Replace(']', ']]') + ']' }

$GL = 'GuardLogs'
$HIST = 'ClientSiteRadioChecksActivityStatus_History'
$IMG = 'GuardLogsDocumentImages'

# ---------------------------------------------------------------------------------------------
# Connection: the connection string, to the source database
# ---------------------------------------------------------------------------------------------
if (-not $ConnectionString) {
    Write-Log 'FAILED - give -ConnectionString (it connects to the source database)' 'ERROR'
    exit 1
}
# Passed to the constructor: the builder is a dictionary, so in PowerShell
# '$builder.ConnectionString = ...' would add a key called ConnectionString instead
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder -ArgumentList $ConnectionString
$SourceDb = $builder['Initial Catalog']
if (-not $SourceDb) {
    Write-Log 'FAILED - the connection string has no Database / Initial Catalog (the source database)' 'ERROR'
    exit 1
}
$builder['Application Name'] = 'CityWatch archive'
# No pooling: closing must end the SQL session, which releases the run lock (sp_getapplock)
$builder['Pooling'] = $false
$conn = New-Object System.Data.SqlClient.SqlConnection $builder.ToString()

$src = Q $SourceDb
$arc = Q $BackupDb

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
        $table = New-Object System.Data.DataTable
        $reader = $cmd.ExecuteReader()
        try { $table.Load($reader) } finally { $reader.Close() }
        return , $table
    }
    finally { $cmd.Dispose() }
}

# Lock timeout (1222), deadlock victim (1205) or command timeout (-2): wait and try again
function Invoke-WithRetry([scriptblock]$Action, [string]$What) {
    $waits = @(5, 15, 30, 60)
    for ($attempt = 0; ; $attempt++) {
        try { return & $Action }
        catch {
            $sqlEx = $_.Exception
            while ($sqlEx -and -not ($sqlEx -is [System.Data.SqlClient.SqlException])) { $sqlEx = $sqlEx.InnerException }
            $transient = $sqlEx -and (@(1222, 1205, -2) -contains $sqlEx.Number)
            if (-not $transient -or $attempt -ge $waits.Count) { throw }
            Write-Log ("{0}: {1} - retrying in {2}s" -f $What, $sqlEx.Message, $waits[$attempt]) 'WARN'
            Start-Sleep -Seconds $waits[$attempt]
        }
    }
}

function Start-RunLog([string]$Table) {
    Invoke-Sql "INSERT INTO $arc.dbo.ArchiveRunLog (RunId, Operation, TableName, StartedAtUtc, Status)
                OUTPUT inserted.Id VALUES (@RunId, 'Archive', @Table, SYSUTCDATETIME(), 'Running');" @{ RunId = $runId; Table = $Table } -Scalar
}

function Complete-RunLog($LogId, [string]$Status, $FromId, $ToId, [long]$Moved, [string]$Message) {
    [void](Invoke-Sql "UPDATE $arc.dbo.ArchiveRunLog SET FinishedAtUtc = SYSUTCDATETIME(), Status = @Status, FromId = @FromId, ToId = @ToId,
                           RowsCopied = @Moved, RowsPurged = @Moved, Message = @Message WHERE Id = @LogId;" @{
        LogId = $LogId; Status = $Status; FromId = $FromId; ToId = $ToId; Moved = $Moved; Message = $Message })
}

# ---------------------------------------------------------------------------------------------
# Archive table columns: add any column the source has gained since, return the shared column list
# ---------------------------------------------------------------------------------------------
$columnCache = @{}
function Get-Columns([string]$Table) {
    if ($columnCache.ContainsKey($Table)) { return $columnCache[$Table] }
    $t = Q $Table
    $missing = Invoke-Sql "
        SELECT c.name,
               CASE WHEN ty.name IN ('varchar', 'char', 'varbinary', 'binary')
                        THEN ty.name + '(' + IIF(c.max_length = -1, 'MAX', CAST(c.max_length AS VARCHAR(10))) + ')'
                    WHEN ty.name IN ('nvarchar', 'nchar')
                        THEN ty.name + '(' + IIF(c.max_length = -1, 'MAX', CAST(c.max_length / 2 AS VARCHAR(10))) + ')'
                    WHEN ty.name IN ('decimal', 'numeric')
                        THEN ty.name + '(' + CAST(c.precision AS VARCHAR(3)) + ',' + CAST(c.scale AS VARCHAR(3)) + ')'
                    WHEN ty.name IN ('datetime2', 'datetimeoffset', 'time')
                        THEN ty.name + '(' + CAST(c.scale AS VARCHAR(3)) + ')'
                    ELSE ty.name END AS TypeSql
        FROM sys.columns c
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.$t') AND c.is_computed = 0
          AND c.name COLLATE DATABASE_DEFAULT NOT IN
              (SELECT a.name COLLATE DATABASE_DEFAULT FROM $arc.sys.columns a WHERE a.object_id = OBJECT_ID(N'$arc.dbo.$t'))
        ORDER BY c.column_id;"
    foreach ($row in $missing.Rows) {
        Write-Log ("{0}: the source has a new column {1} {2}; adding it to the archive" -f $Table, $row.name, $row.TypeSql) 'WARN'
        [void](Invoke-Sql ("ALTER TABLE $arc.dbo.$t ADD {0} {1} NULL;" -f (Q $row.name), $row.TypeSql))
    }

    $cols = Invoke-Sql "
        SELECT c.name FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.$t') AND c.is_computed = 0
          AND c.name COLLATE DATABASE_DEFAULT IN
              (SELECT a.name COLLATE DATABASE_DEFAULT FROM $arc.sys.columns a WHERE a.object_id = OBJECT_ID(N'$arc.dbo.$t'))
        ORDER BY c.column_id;"
    $names = @($cols.Rows | ForEach-Object { Q $_.name })
    if ($names -notcontains '[Id]') { throw "$Table has no Id column" }
    $list = $names -join ', '
    $columnCache[$Table] = $list
    return $list
}

# $false when the source's log stays too full: stop for tonight
function Wait-ForLogSpace([string]$Table) {
    for ($i = 0; $i -lt 10; $i++) {
        $pct = [double](Invoke-Sql "SELECT used_log_space_in_percent FROM sys.dm_db_log_space_usage;" -Scalar)
        if ($pct -le $MaxLogPercent) { return $true }
        if ($i -eq 0) { Write-Log ("{0}: source transaction log is {1:N0}% full (limit {2}%) - waiting for a log backup / checkpoint" -f $Table, $pct, $MaxLogPercent) 'WARN' }
        if ((Get-Date) -gt $deadline) { break }
        Start-Sleep -Seconds 60
    }
    Write-Log ("{0}: source transaction log is still over {1}% - stopping for tonight" -f $Table, $MaxLogPercent) 'WARN'
    return $false
}

# ---------------------------------------------------------------------------------------------
# Move one table's old rows: archive + delete per batch, in one transaction
# ---------------------------------------------------------------------------------------------
function Move-OldRows([string]$Table, [datetime]$Cutoff) {
    $t = Q $Table
    $hold = "NOT EXISTS (SELECT 1 FROM $arc.dbo.ProdDeleteHold h WHERE h.TableName = N'$Table' AND s.Id BETWEEN h.FromId AND h.ToId AND h.HoldUntilUtc > SYSUTCDATETIME())"
    if ($Table -eq $GL) {
        $eligible = "s.EventDateTime < @Cutoff AND $hold
                  AND NOT EXISTS (SELECT 1 FROM dbo.ClientSiteRadioChecksActivityStatus a WHERE a.LBId = s.Id)
                  AND NOT EXISTS (SELECT 1 FROM dbo.$(Q $HIST) x WHERE x.LBId = s.Id)"
    }
    else { $eligible = "s.EventDateTime < @Cutoff AND $hold" }

    # Old rows sit at the start of the Id range (Ids follow time): from the first Id up to the first
    # Id of the kept period, found by binary search on the clustered key. A late-synced row with an
    # old time and a newer Id is picked up once the cutoff passes the rows around it.
    $b = (Invoke-Sql "
        DECLARE @min INT, @lo INT, @hi INT, @mid INT, @t DATETIME;
        SELECT @min = MIN(Id), @hi = MAX(Id) FROM dbo.$t;
        SET @lo = @min;
        WHILE @lo < @hi
        BEGIN
            SET @mid = @lo + (@hi - @lo) / 2;
            SET @t = NULL;
            SELECT TOP (1) @t = EventDateTime FROM dbo.$t WHERE Id >= @mid ORDER BY Id;
            IF @t >= @Cutoff SET @hi = @mid ELSE SET @lo = @mid + 1;
        END
        SELECT @min AS MinId, @lo AS FirstKeptId;" @{ Cutoff = $Cutoff }).Rows[0]
    if ($b.MinId -is [DBNull] -or [int]$b.MinId -ge [int]$b.FirstKeptId) {
        Write-Log ("{0}: no data older than {1:yyyy-MM-dd} - nothing to move" -f $Table, $Cutoff)
        return 'Succeeded'
    }
    $lo = [int]$b.MinId; $last = [int]$b.FirstKeptId - 1

    if ($ReportOnly) {
        $n = Invoke-Sql "SELECT COUNT_BIG(*) FROM dbo.$t s WHERE s.Id BETWEEN @Lo AND @Hi AND $eligible;" @{ Lo = $lo; Hi = $last; Cutoff = $Cutoff } -Scalar
        Write-Log ("{0}: {1:N0} rows older than {2:yyyy-MM-dd} would be moved to the archive (Id {3} to {4})" -f $Table, $n, $Cutoff, $lo, $last)
        return 'Succeeded'
    }

    $logId = Start-RunLog $Table
    $moved = [long]0; $toId = $null
    try {
        $columns = Get-Columns $Table
        $sCols = ($columns -split ', ' | ForEach-Object { 's.' + $_ }) -join ', '
        $extra = ''
        if ($Table -eq $GL) {
            # Photo / attachment records go with the log entry (ON DELETE CASCADE): archive them first
            $imgCols = Get-Columns $IMG
            $siCols = ($imgCols -split ', ' | ForEach-Object { 'si.' + $_ }) -join ', '
            $extra = "
                DELETE bi FROM $arc.dbo.$(Q $IMG) bi
                WHERE EXISTS (SELECT 1 FROM dbo.$(Q $IMG) si JOIN #ids i ON i.Id = si.GuardLogId WHERE si.Id = bi.Id);
                INSERT INTO $arc.dbo.$(Q $IMG) ($imgCols)
                SELECT $siCols FROM dbo.$(Q $IMG) si JOIN #ids i ON i.Id = si.GuardLogId;"
        }

        $batchSql = "
            SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000; SET DEADLOCK_PRIORITY LOW;
            IF OBJECT_ID(N'tempdb..#ids') IS NOT NULL DROP TABLE #ids;
            CREATE TABLE #ids (Id INT PRIMARY KEY);
            BEGIN TRAN;
            -- 1. the rows to move, locked against change until commit
            INSERT INTO #ids (Id)
            SELECT s.Id FROM dbo.$t s WITH (UPDLOCK, ROWLOCK)
            WHERE s.Id BETWEEN @Lo AND @Hi AND $eligible
            OPTION (MAXDOP 1);
            DECLARE @picked INT = @@ROWCOUNT, @archived INT = 0, @deleted INT = 0;
            IF @picked > 0
            BEGIN
                -- 2. archive exactly these rows (replacing any older copy from an earlier restore)
                DELETE b FROM $arc.dbo.$t b JOIN #ids i ON i.Id = b.Id;
                INSERT INTO $arc.dbo.$t ($columns)
                SELECT $sCols FROM dbo.$t s JOIN #ids i ON i.Id = s.Id;
                SET @archived = @@ROWCOUNT;
                $extra
                -- 3. delete them from the source, only where the archive copy exists
                DELETE s FROM dbo.$t s
                JOIN #ids i ON i.Id = s.Id
                WHERE EXISTS (SELECT 1 FROM $arc.dbo.$t b WHERE b.Id = s.Id);
                SET @deleted = @@ROWCOUNT;
                -- 4. all or nothing
                IF @archived <> @picked OR @deleted <> @picked
                    THROW 50001, 'Archived / deleted row counts do not match the rows picked - batch rolled back', 1;
            END
            COMMIT;
            DROP TABLE #ids;
            SELECT @picked;"

        Write-Log ("{0}: moving rows older than {1:yyyy-MM-dd} to the archive (Id {2} to {3})" -f $Table, $Cutoff, $lo, $last)
        $fromId = $lo
        $status = 'Succeeded'
        $batches = 0
        while ($lo -le $last) {
            if ((Get-Date) -gt $deadline) {
                $status = 'Stopped'
                Write-Log ("{0}: reached the {1} minute limit at Id {2}; the next run continues" -f $Table, $MaxRunMinutes, $lo) 'WARN'
                break
            }
            if ($batches % 25 -eq 0 -and -not (Wait-ForLogSpace $Table)) { $status = 'Stopped'; break }
            $hi = [int][math]::Min([long]$lo + $BatchSize - 1, [long]$last)
            $n = Invoke-WithRetry { Invoke-Sql $batchSql @{ Lo = $lo; Hi = $hi; Cutoff = $Cutoff } -Scalar } "$Table batch $lo-$hi"
            $moved += [long]$n
            $toId = $hi
            $batches++
            if ($batches % 50 -eq 0) { Write-Log ("{0}: up to Id {1}, {2:N0} rows moved" -f $Table, $hi, $moved) }
            $lo = $hi + 1
            if ($DelayMs -gt 0) { Start-Sleep -Milliseconds $DelayMs }
        }

        Complete-RunLog $logId $status $fromId $toId $moved ("rows older than {0:yyyy-MM-dd} moved to the archive" -f $Cutoff)
        Write-Log ("{0}: {1}: {2:N0} rows archived and deleted from the source" -f $Table, $status, $moved)
        return $status
    }
    catch {
        $msg = $_.Exception.Message
        Write-Log ("{0}: FAILED - {1} (earlier batches are archived and deleted; the failed batch was rolled back)" -f $Table, $msg) 'ERROR'
        try { Complete-RunLog $logId 'Failed' $lo $toId $moved $msg } catch { }
        return 'Failed'
    }
}

# ---------------------------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------------------------
$ok = $true
try {
    Write-Log ("Archive run {0}: {1} -> {2} on {3}; keep {4} months; batch {5}, delay {6} ms, limit {7} min{8}" -f `
        $runId, $SourceDb, $BackupDb, $builder['Data Source'], $ProdKeepMonths, $BatchSize, $DelayMs, $MaxRunMinutes,
        $(if ($ReportOnly) { '; REPORT ONLY' } else { '' }))
    $conn.Open()

    # Only one archive / restore at a time
    $lock = Invoke-Sql "DECLARE @r INT; EXEC @r = sp_getapplock @Resource = N'CityWatchArchive', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @r;" -Scalar
    if ($lock -lt 0) { throw 'Another archive or restore run is going' }

    if ($null -eq (Invoke-Sql "SELECT DB_ID(@Db);" @{ Db = $BackupDb } -Scalar)) {
        throw "Archive database $BackupDb not found on this instance - run 01_Create_prod_citywatch_backup.sql"
    }
    foreach ($required in @($GL, $HIST, $IMG, 'ProdDeleteHold', 'ArchiveRunLog')) {
        if ($null -eq (Invoke-Sql "SELECT OBJECT_ID(N'$arc.dbo.$(Q $required)');" -Scalar)) {
            throw "$BackupDb.dbo.$required does not exist - run 01_Create_prod_citywatch_backup.sql"
        }
    }
    if ($null -eq (Invoke-Sql "SELECT 1 FROM sys.indexes WHERE name = N'IX_RadioChecksActivityStatus_History_LBId' AND object_id = OBJECT_ID(N'dbo.$(Q $HIST)');" -Scalar)) {
        throw 'Apply DbScript/385 to the source first: without its IX_RadioChecksActivityStatus_History_LBId index every GuardLogs delete scans the whole history table'
    }

    $cutoff = Invoke-Sql "SELECT DATEADD(MONTH, -@Months, CAST(CAST(GETDATE() AS DATE) AS DATETIME));" @{ Months = $ProdKeepMonths } -Scalar

    # History first: its rows reference log entries
    $h = Move-OldRows $HIST $cutoff
    if ($h -eq 'Failed') { $ok = $false }
    if ($h -eq 'Succeeded') {
        if ((Move-OldRows $GL $cutoff) -eq 'Failed') { $ok = $false }
    }
}
catch {
    $ok = $false
    Write-Log ("FAILED - {0}" -f $_.Exception.Message) 'ERROR'
}
finally {
    # Whatever state it is in (Open, Broken, never opened): Dispose closes it and ends the session
    if ($conn) { $conn.Dispose() }
    Write-Log ("Finished in {0:N1} minutes" -f ((Get-Date) - $runStarted).TotalMinutes)
}

if ($ok) { exit 0 } else { exit 1 }
