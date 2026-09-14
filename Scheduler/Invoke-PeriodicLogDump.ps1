<#
  Invoke-PeriodicLogDump.ps1

  Calls the weekly or monthly log dump endpoint on CityWatch.Web. Written for
  Windows Task Scheduler, which is how the daily dump (api/SiteLogNew/Upload)
  is already triggered.

  The endpoint does the work; this script only makes the call and reports
  whether it was accepted. The run's own outcome - which sites produced a
  document, what was skipped and what failed - is recorded by the application
  in three tables, and those are where to look when a dump is wrong:

      PeriodicLogDumpJobs           one row per run: start, finish, success,
                                    and produced/skipped/failed counts
      ClientSitePeriodicLogUploads  one row per document: site, period, log
                                    type, file name and Dropbox path
      SchedulerTaskErrors           one row per failure, with the site and
                                    period it happened to

  The endpoint always covers the last COMPLETED period - the previous Monday
  to Sunday, or the previous calendar month - whichever day it is actually
  run. A task that fires late still sends a whole period rather than a partial
  one, and a period already produced is skipped rather than sent twice, so
  running this more than once is safe.

  USAGE
      .\Invoke-PeriodicLogDump.ps1 -BaseUrl "https://test.c4i-system.com" -Period Weekly
      .\Invoke-PeriodicLogDump.ps1 -BaseUrl "https://test.c4i-system.com" -Period Monthly

  REGISTERING THE TASKS (run elevated, on the web server)

      $ps     = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
      $script = "C:\CityWatch\Scheduler\Invoke-PeriodicLogDump.ps1"
      $base   = "https://your-server"

      # Weekly - 2am every Monday, as the site settings screen promises
      schtasks /Create /TN "CityWatch Weekly Log Dump" /SC WEEKLY /D MON /ST 02:00 `
        /TR "`"$ps`" -NoProfile -ExecutionPolicy Bypass -File `"$script`" -BaseUrl `"$base`" -Period Weekly" `
        /RU SYSTEM /RL HIGHEST /F

      # Monthly - 6am on the 1st
      schtasks /Create /TN "CityWatch Monthly Log Dump" /SC MONTHLY /D 1 /ST 06:00 `
        /TR "`"$ps`" -NoProfile -ExecutionPolicy Bypass -File `"$script`" -BaseUrl `"$base`" -Period Monthly" `
        /RU SYSTEM /RL HIGHEST /F

  EXIT CODES - Task Scheduler shows these as "Last Run Result"
      0  the endpoint accepted and completed the run
      1  the endpoint returned an error status, or could not be reached
      2  bad arguments

  NOTES
    - No credentials. Like the daily actions on the same controller, these are
      anonymous because the caller is a scheduler rather than a signed-in user.
    - The default timeout is an hour, not PowerShell's 100 seconds. A dump
      builds a PDF per site per day and merges them, so a real run takes
      minutes and the default would abandon it midway - while the server kept
      going, leaving the task looking failed when it had not.
    - The endpoint refuses to run in a Development environment, so pointing
      this at a dev instance returns 500 by design rather than uploading.
#>

[CmdletBinding()]
param(
    # e.g. https://test.c4i-system.com - no trailing path.
    [Parameter(Mandatory = $true)]
    [string] $BaseUrl,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Weekly', 'Monthly')]
    [string] $Period,

    # A dump is minutes of work, not seconds. See NOTES above.
    [ValidateRange(60, 86400)]
    [int] $TimeoutSeconds = 3600,

    # Defaults to a Logs folder beside this script.
    [string] $LogPath,

    # Keep this many days of log files.
    [ValidateRange(1, 3650)]
    [int] $LogRetentionDays = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------- logging --

if (-not $LogPath) {
    $LogPath = Join-Path $PSScriptRoot 'Logs'
}

try {
    if (-not (Test-Path $LogPath)) {
        New-Item -ItemType Directory -Path $LogPath -Force | Out-Null
    }
}
catch {
    Write-Error "Could not create the log folder '$LogPath': $($_.Exception.Message)"
    exit 2
}

$logFile = Join-Path $LogPath ("PeriodicLogDump-{0}-{1}.log" -f $Period, (Get-Date -Format 'yyyyMM'))

function Write-Log {
    param([string] $Message, [string] $Level = 'INFO')

    $line = "{0} [{1}] {2}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Message

    Write-Output $line
    try {
        Add-Content -Path $logFile -Value $line -Encoding utf8
    }
    catch {
        # A log that cannot be written must not fail the run it is logging.
    }
}

# ------------------------------------------------------------------- call ---

$action = if ($Period -eq 'Weekly') { 'UploadWeekly' } else { 'UploadMonthly' }
$url = "{0}/api/SiteLogNew/{1}" -f $BaseUrl.TrimEnd('/'), $action

Write-Log "$Period log dump starting. GET $url (timeout ${TimeoutSeconds}s)"

# Windows PowerShell 5.1 can still default to TLS 1.0, which modern servers refuse.
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
}
catch {
    Write-Log "Could not set TLS 1.2: $($_.Exception.Message)" 'WARN'
}

$started = Get-Date
$exitCode = 0

try {
    $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing

    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    $body = if ($response.Content) { $response.Content.Trim() } else { '' }

    Write-Log "HTTP $($response.StatusCode) after ${elapsed}s. Response: $body"

    if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
        Write-Log "$Period log dump finished. See PeriodicLogDumpJobs for what the run actually did."
    }
    else {
        Write-Log "$Period log dump returned an unexpected status." 'ERROR'
        $exitCode = 1
    }
}
catch {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds

    # An HTTP error status arrives here as an exception, so dig the code out
    # when there is one - it separates "the server said no" from "no server".
    $status = ''
    try {
        if ($_.Exception.PSObject.Properties.Name -contains 'Response' -and $_.Exception.Response) {
            $status = " (HTTP $([int]$_.Exception.Response.StatusCode))"
        }
    }
    catch {
    }

    Write-Log "$Period log dump failed after ${elapsed}s$($status): $($_.Exception.Message)" 'ERROR'
    Write-Log "Check SchedulerTaskErrors and PeriodicLogDumpJobs - the run may have started before this call failed." 'ERROR'
    $exitCode = 1
}

# -------------------------------------------------------------- tidy logs ---

try {
    Get-ChildItem -Path $LogPath -Filter 'PeriodicLogDump-*.log' -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$LogRetentionDays) } |
        Remove-Item -Force -ErrorAction SilentlyContinue
}
catch {
    # Housekeeping only.
}

exit $exitCode
