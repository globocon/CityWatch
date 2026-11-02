<#
.SYNOPSIS
    Nightly: move GuardLogs and ClientSiteRadioChecksActivityStatus_History rows older than 18 months
    (and the log entries' GuardLogsDocumentImages photo records) from prod-citywatch into
    prod_citywatch_backup, deleting from prod only what was archived.

.DESCRIPTION
    Meant to run once a day from Windows Task Scheduler (see README.md). The first run moves the
    whole backlog (stopping at MaxRunMinutes and carrying on the next night); after that each night
    moves the one day that has just passed the 18-month mark.

    For history first, then GuardLogs (history rows point at log entries), it works through the
    old rows in batches of BatchSize ids. Each batch is ONE transaction across the two databases:

      1. pick the batch's rows older than the cutoff, locking them against change (UPDLOCK);
      2. copy exactly those rows into the archive (for GuardLogs, also their photo / attachment
         records, which prod would otherwise delete with them through ON DELETE CASCADE);
      3. delete them from prod - only rows whose archive copy now exists;
      4. if the numbers picked, archived and deleted are not all equal, roll the whole batch back
         and stop.

    So a row is never deleted from prod unless the same transaction put it in the archive, and a
    failure at any point leaves both databases as they were before that batch.

    Left in prod:
      - GuardLogs rows still referenced from prod (ClientSiteRadioChecksActivityStatus, or a
        history row that is not being archived);
      - rows on hold in dbo.ProdDeleteHold (put there by Restore-CityWatchArchive.ps1, so a restore
        is not undone the next night).

    Keeping prod responsive:
      - BatchSize 1000: each row also locks a key in every index, and SQL Server escalates to a
        table lock at ~5000 locks; small batches never lock the app's tables as a whole.
      - DelayMs pause between batches; LOCK_TIMEOUT 15 s and DEADLOCK_PRIORITY LOW (the job gives
        way to the app and retries); MAXDOP 1.
      - The deletes go through prod's transaction log: the job pauses while the log is over
        MaxLogPercent full and stops for the night if it stays full.
      - Stops at MaxRunMinutes; the next run carries on.

    Will not delete anything when:
      - prod_citywatch_backup has no full backup in the last RequireArchiveBackupDays days - it
        holds the only live copy of archived rows;
      - prod lacks the IX_RadioChecksActivityStatus_History_LBId index from DbScript/385 - without
        it every GuardLogs delete scans the whole history table.

    -ReportOnly: count what would be archived, change nothing.

    Both databases must be on the same SQL Server instance (the copy and delete share one
    transaction). Run 01_Create_prod_citywatch_backup.sql once first. Windows authentication unless
    -ConnectionString is given; the account needs on prod db_datareader + db_datawriter and VIEW
    DATABASE STATE, db_owner on prod_citywatch_backup, and read on msdb.dbo.backupset.

    Exit code 0 = finished or stopped at the time limit / log limit, 1 = failed.

.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File Backup-CityWatchArchive.ps1 -SqlServer "localhost"

.EXAMPLE
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File Backup-CityWatchArchive.ps1 -SqlServer "localhost" -ReportOnly
#>
[CmdletBinding()]
param(
    [string]$SqlServer = 'localhost',
    [string]$SourceDb = 'prod-citywatch',
    [string]$BackupDb = 'prod_citywatch_backup',
    [ValidateRange(1, 120)][int]$ProdKeepMonths = 18,
    [ValidateRange(100, 4000)][int]$BatchSize = 1000,
    [ValidateRange(0, 60000)][int]$DelayMs = 300,
    [ValidateRange(1, 1440)][int]$MaxRunMinutes = 240,
    [ValidateRange(10, 95)][int]$MaxLogPercent = 70,
    # Nothing is deleted unless the archive database has a full backup at most this many days old
    # (it holds the only live copy of archived rows). 0 = do not check.
    [ValidateRange(0, 365)][int]$RequireArchiveBackupDays = 7,
    [switch]$ReportOnly,
    [string]$LogDir = (Join-Path $PSScriptRoot 'logs'),
    [ValidateRange(1, 3650)][int]$KeepLogDays = 30,
    # Optional, e.g. for SQL authentication. Its Initial Catalog is ignored; BackupDb is used.
    [string]$ConnectionString
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$runId = [guid]::NewGuid()
$runStarted = Get-Date
$deadline = $runStarted.AddMinutes($MaxRunMinutes)

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

$src = Q $SourceDb

$GL = 'GuardLogs'
$HIST = 'ClientSiteRadioChecksActivityStatus_History'
$IMG = 'GuardLogsDocumentImages'

# ---------------------------------------------------------------------------------------------
# SQL helpers: one connection for the whole run, in the backup database
# ---------------------------------------------------------------------------------------------
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
if ($ConnectionString) { $builder.ConnectionString = $ConnectionString }
else { $builder['Data Source'] = $SqlServer; $builder['Integrated Security'] = $true }
$builder['Initial Catalog'] = $BackupDb
$builder['Application Name'] = 'CityWatch archive backup'
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
    Invoke-Sql "INSERT INTO dbo.ArchiveRunLog (RunId, Operation, TableName, StartedAtUtc, Status)
                OUTPUT inserted.Id VALUES (@RunId, 'Archive', @Table, SYSUTCDATETIME(), 'Running');" @{ RunId = $runId; Table = $Table } -Scalar
}

function Complete-RunLog($LogId, [string]$Status, $FromId, $ToId, [long]$Moved, [string]$Message) {
    [void](Invoke-Sql "UPDATE dbo.ArchiveRunLog SET FinishedAtUtc = SYSUTCDATETIME(), Status = @Status, FromId = @FromId, ToId = @ToId,
                           RowsCopied = @Moved, RowsPurged = @Moved, Message = @Message WHERE Id = @LogId;" @{
        LogId = $LogId; Status = $Status; FromId = $FromId; ToId = $ToId; Moved = $Moved; Message = $Message })
}

# ---------------------------------------------------------------------------------------------
# Archive table columns: add any column prod has gained since, return the shared column list
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
        FROM $src.sys.columns c
        JOIN $src.sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(N'$src.dbo.$t') AND c.is_computed = 0
          AND c.name COLLATE DATABASE_DEFAULT NOT IN (SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.$t'))
        ORDER BY c.column_id;"
    foreach ($row in $missing.Rows) {
        Write-Log ("{0}: prod has a new column {1} {2}; adding it to the archive" -f $Table, $row.name, $row.TypeSql) 'WARN'
        [void](Invoke-Sql ("ALTER TABLE dbo.$t ADD {0} {1} NULL;" -f (Q $row.name), $row.TypeSql))
    }

    $cols = Invoke-Sql "
        SELECT c.name FROM $src.sys.columns c
        WHERE c.object_id = OBJECT_ID(N'$src.dbo.$t') AND c.is_computed = 0
          AND c.name COLLATE DATABASE_DEFAULT IN (SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.$t'))
        ORDER BY c.column_id;"
    $names = @($cols.Rows | ForEach-Object { Q $_.name })
    if ($names -notcontains '[Id]') { throw "$Table has no Id column" }
    $list = $names -join ', '
    $columnCache[$Table] = $list
    return $list
}

function Get-ProdLogPercent {
    [double](Invoke-Sql "EXEC $src.sys.sp_executesql N'SELECT used_log_space_in_percent FROM sys.dm_db_log_space_usage';" -Scalar)
}

# $false when prod's log stays too full: stop for tonight
function Wait-ForProdLog([string]$Table) {
    for ($i = 0; $i -lt 10; $i++) {
        $pct = Get-ProdLogPercent
        if ($pct -le $MaxLogPercent) { return $true }
        if ($i -eq 0) { Write-Log ("{0}: prod transaction log is {1:N0}% full (limit {2}%) - waiting for a log backup / checkpoint" -f $Table, $pct, $MaxLogPercent) 'WARN' }
        if ((Get-Date) -gt $deadline) { break }
        Start-Sleep -Seconds 60
    }
    Write-Log ("{0}: prod transaction log is still over {1}% - stopping for tonight" -f $Table, $MaxLogPercent) 'WARN'
    return $false
}

# ---------------------------------------------------------------------------------------------
# Move one table's old rows: archive + delete per batch, in one transaction
# ---------------------------------------------------------------------------------------------
function Move-OldRows([string]$Table, [datetime]$Cutoff) {
    $t = Q $Table
    $hold = "NOT EXISTS (SELECT 1 FROM dbo.ProdDeleteHold h WHERE h.TableName = N'$Table' AND s.Id BETWEEN h.FromId AND h.ToId AND h.HoldUntilUtc > SYSUTCDATETIME())"
    if ($Table -eq $GL) {
        $eligible = "s.EventDateTime < @Cutoff AND $hold
                  AND NOT EXISTS (SELECT 1 FROM $src.dbo.ClientSiteRadioChecksActivityStatus a WHERE a.LBId = s.Id)
                  AND NOT EXISTS (SELECT 1 FROM $src.dbo.$(Q $HIST) x WHERE x.LBId = s.Id)"
    }
    else { $eligible = "s.EventDateTime < @Cutoff AND $hold" }

    # Old rows sit at the start of the Id range (Ids follow time): from prod's first Id up to the
    # first Id of the kept period, found by binary search on the clustered key. A late-synced row
    # with an old time and a newer Id is picked up once the cutoff passes the rows around it.
    $b = (Invoke-Sql "
        DECLARE @min INT, @lo INT, @hi INT, @mid INT, @t DATETIME;
        SELECT @min = MIN(Id), @hi = MAX(Id) FROM $src.dbo.$t;
        SET @lo = @min;
        WHILE @lo < @hi
        BEGIN
            SET @mid = @lo + (@hi - @lo) / 2;
            SET @t = NULL;
            SELECT TOP (1) @t = EventDateTime FROM $src.dbo.$t WHERE Id >= @mid ORDER BY Id;
            IF @t >= @Cutoff SET @hi = @mid ELSE SET @lo = @mid + 1;
        END
        SELECT @min AS MinId, @lo AS FirstKeptId;" @{ Cutoff = $Cutoff }).Rows[0]
    if ($b.MinId -is [DBNull] -or [int]$b.MinId -ge [int]$b.FirstKeptId) {
        Write-Log ("{0}: nothing in prod older than {1:yyyy-MM-dd}" -f $Table, $Cutoff)
        return 'Succeeded'
    }
    $lo = [int]$b.MinId; $last = [int]$b.FirstKeptId - 1

    if ($ReportOnly) {
        $n = Invoke-Sql "SELECT COUNT_BIG(*) FROM $src.dbo.$t s WHERE s.Id BETWEEN @Lo AND @Hi AND $eligible;" @{ Lo = $lo; Hi = $last; Cutoff = $Cutoff } -Scalar
        Write-Log ("{0}: {1:N0} rows older than {2:yyyy-MM-dd} would be archived and deleted (Id {3} to {4})" -f $Table, $n, $Cutoff, $lo, $last)
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
                DELETE bi FROM dbo.$(Q $IMG) bi
                WHERE EXISTS (SELECT 1 FROM $src.dbo.$(Q $IMG) si JOIN #ids i ON i.Id = si.GuardLogId WHERE si.Id = bi.Id);
                INSERT INTO dbo.$(Q $IMG) ($imgCols)
                SELECT $siCols FROM $src.dbo.$(Q $IMG) si JOIN #ids i ON i.Id = si.GuardLogId;"
        }

        $batchSql = "
            SET XACT_ABORT ON; SET LOCK_TIMEOUT 15000; SET DEADLOCK_PRIORITY LOW;
            IF OBJECT_ID(N'tempdb..#ids') IS NOT NULL DROP TABLE #ids;
            CREATE TABLE #ids (Id INT PRIMARY KEY);
            BEGIN TRAN;
            -- 1. the rows to move, locked against change until commit
            INSERT INTO #ids (Id)
            SELECT s.Id FROM $src.dbo.$t s WITH (UPDLOCK, ROWLOCK)
            WHERE s.Id BETWEEN @Lo AND @Hi AND $eligible
            OPTION (MAXDOP 1);
            DECLARE @picked INT = @@ROWCOUNT, @archived INT = 0, @deleted INT = 0;
            IF @picked > 0
            BEGIN
                -- 2. archive exactly these rows (replacing any older copy from an earlier restore)
                DELETE b FROM dbo.$t b JOIN #ids i ON i.Id = b.Id;
                INSERT INTO dbo.$t ($columns)
                SELECT $sCols FROM $src.dbo.$t s JOIN #ids i ON i.Id = s.Id;
                SET @archived = @@ROWCOUNT;
                $extra
                -- 3. delete them from prod, only where the archive copy exists
                DELETE s FROM $src.dbo.$t s
                JOIN #ids i ON i.Id = s.Id
                WHERE EXISTS (SELECT 1 FROM dbo.$t b WHERE b.Id = s.Id);
                SET @deleted = @@ROWCOUNT;
                -- 4. all or nothing
                IF @archived <> @picked OR @deleted <> @picked
                    THROW 50001, 'Archived / deleted row counts do not match the rows picked - batch rolled back', 1;
            END
            COMMIT;
            DROP TABLE #ids;
            SELECT @picked;"

        Write-Log ("{0}: archiving rows older than {1:yyyy-MM-dd} (Id {2} to {3})" -f $Table, $Cutoff, $lo, $last)
        $fromId = $lo
        $status = 'Succeeded'
        $batches = 0
        while ($lo -le $last) {
            if ((Get-Date) -gt $deadline) {
                $status = 'Stopped'
                Write-Log ("{0}: reached the {1} minute limit at Id {2}; the next run continues" -f $Table, $MaxRunMinutes, $lo) 'WARN'
                break
            }
            if ($batches % 25 -eq 0 -and -not (Wait-ForProdLog $Table)) { $status = 'Stopped'; break }
            $hi = [int][math]::Min([long]$lo + $BatchSize - 1, [long]$last)
            $n = Invoke-WithRetry { Invoke-Sql $batchSql @{ Lo = $lo; Hi = $hi; Cutoff = $Cutoff } -Scalar } "$Table batch $lo-$hi"
            $moved += [long]$n
            $toId = $hi
            $batches++
            if ($batches % 50 -eq 0) { Write-Log ("{0}: up to Id {1}, {2:N0} rows archived and deleted" -f $Table, $hi, $moved) }
            $lo = $hi + 1
            if ($DelayMs -gt 0) { Start-Sleep -Milliseconds $DelayMs }
        }

        Complete-RunLog $logId $status $fromId $toId $moved ("rows older than {0:yyyy-MM-dd} archived and deleted from prod" -f $Cutoff)
        Write-Log ("{0}: {1}: {2:N0} rows archived and deleted from prod" -f $Table, $status, $moved)
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
    Write-Log ("Archive run {0}: {1} -> {2} on {3}; keep {4} months in prod; batch {5}, delay {6} ms, limit {7} min{8}" -f `
        $runId, $SourceDb, $BackupDb, $builder['Data Source'], $ProdKeepMonths, $BatchSize, $DelayMs, $MaxRunMinutes,
        $(if ($ReportOnly) { '; REPORT ONLY' } else { '' }))
    $conn.Open()

    # Only one archive / restore at a time
    $lock = Invoke-Sql "DECLARE @r INT; EXEC @r = sp_getapplock @Resource = N'CityWatchArchive', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 0; SELECT @r;" -Scalar
    if ($lock -lt 0) { throw 'Another archive or restore run is going' }

    foreach ($required in @($GL, $HIST, $IMG, 'ProdDeleteHold', 'ArchiveRunLog')) {
        if ($null -eq (Invoke-Sql "SELECT OBJECT_ID(N'dbo.$(Q $required)');" -Scalar)) {
            throw "dbo.$required does not exist in $BackupDb - run 01_Create_prod_citywatch_backup.sql"
        }
    }
    if ($null -eq (Invoke-Sql "SELECT 1 FROM $src.sys.indexes WHERE name = N'IX_RadioChecksActivityStatus_History_LBId' AND object_id = OBJECT_ID(N'$src.dbo.$(Q $HIST)');" -Scalar)) {
        throw 'Apply DbScript/385 to prod first: without its IX_RadioChecksActivityStatus_History_LBId index every GuardLogs delete scans the whole history table'
    }
    if (-not $ReportOnly -and $RequireArchiveBackupDays -gt 0 -and $null -eq (Invoke-Sql "
            SELECT TOP (1) 1 FROM msdb.dbo.backupset bs
            WHERE bs.database_name = @Db AND bs.type = 'D' AND bs.backup_finish_date >= DATEADD(DAY, -@Days, GETDATE())
              -- this database, not an earlier one with the same name (msdb keeps history by name)
              AND bs.database_guid = (SELECT database_guid FROM sys.database_recovery_status WHERE database_id = DB_ID(@Db));" @{ Db = $BackupDb; Days = $RequireArchiveBackupDays } -Scalar)) {
        throw ("{0} has no full backup in the last {1} days. It holds the only live copy of archived rows - back it up, then run again" -f $BackupDb, $RequireArchiveBackupDays)
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
    if ($conn.State -eq 'Open') { $conn.Close() }
    Write-Log ("Finished in {0:N1} minutes" -f ((Get-Date) - $runStarted).TotalMinutes)
}

if ($ok) { exit 0 } else { exit 1 }
