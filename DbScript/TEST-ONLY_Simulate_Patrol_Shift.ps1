<#
  TEST-ONLY_Simulate_Patrol_Shift.ps1
  Drives a whole patrol shift against a TEST server without any NFC hardware.

  An NFC scan is just an HTTP call. This script makes the same calls the phone
  would, using tag UIDs that already exist in the database, so the real code path
  runs end to end:

      scan API -> hit log -> domain event -> tracking -> session state -> map

  It also posts GPS batches so the car actually moves between sites.

  USAGE (from the repo root):
      .\DbScript\TEST-ONLY_Simulate_Patrol_Shift.ps1 `
          -BaseUrl  "http://test.c4i-system.com" `
          -UnitId   9101 `
          -GuardId  2 `
          -FleetSiteId 625 `
          -InCarTagUid "0448CFC2ED6E81" `
          -SiteTagUid  "044B45AA655281" `
          -SiteId      <martha cove site id>

  Find real tag UIDs with:
      SELECT t.UId, t.LabelDescription, t.ClientSiteId, cs.Name
      FROM ClientSiteSmartWandTags t JOIN ClientSites cs ON cs.Id = t.ClientSiteId
      WHERE t.IsDeleted = 0 AND cs.Id IN (<fleet site>, <client site>);

  The unit must be enrolled with consent first (script 363 does all units).
#>

param(
    [Parameter(Mandatory = $true)] [string] $BaseUrl,
    [Parameter(Mandatory = $true)] [int]    $UnitId,
    [Parameter(Mandatory = $true)] [int]    $GuardId,
    [Parameter(Mandatory = $true)] [int]    $FleetSiteId,      # e.g. 625 Romeo Patrol Cars
    [Parameter(Mandatory = $true)] [string] $InCarTagUid,      # "Romeo 03 (in-car)"
    [Parameter(Mandatory = $true)] [string] $SiteTagUid,       # a checkpoint at the client site
    [Parameter(Mandatory = $true)] [int]    $SiteId,           # that client site's id
    [int]    $UserId       = 0,
    [string] $PositionName = "Mobile Patrols (Car) M1",
    [int]    $PositionId   = 11,
    [string] $Callsign     = "Romeo 03"
)

$ErrorActionPreference = 'Stop'
$api = $BaseUrl.TrimEnd('/')

function Show([string]$step, $obj) {
    Write-Host ""
    Write-Host "=== $step ===" -ForegroundColor Cyan
    if ($obj) { $obj | ConvertTo-Json -Depth 4 -Compress | Write-Host }
}

# ---------- 1. open the shift (what the login screen does) ----------
$start = Invoke-RestMethod -Method Post -Uri "$api/api/tracking/session/start" `
    -ContentType 'application/json' -Body (@{
        unitId       = $UnitId
        guardId      = $GuardId
        clientSiteId = $FleetSiteId
        isPatrolCar  = $true
        callsign     = $Callsign
        positionId   = $PositionId
        positionName = $PositionName
    } | ConvertTo-Json)
Show "1. Shift started - $PositionName ($Callsign)" $start
$session = $start.sessionId
if (-not $session) { throw "No session returned. Is the unit enrolled WITH consent, and Tracking:Enabled=true?" }

# ---------- helpers ----------
$seq = 0
function Send-Gps([double]$lat, [double]$lon, [int]$speed, [int]$heading) {
    $script:seq++
    $body = @{
        unitId         = $UnitId
        sessionId      = $session
        deviceUtc      = (Get-Date).ToUniversalTime().ToString("o")
        commandSeqSeen = 0
        points         = @(@{
            seq        = $script:seq
            utc        = (Get-Date).ToUniversalTime().ToString("o")
            lat        = $lat
            lon        = $lon
            accuracyM  = 8
            speedKph   = $speed
            headingDeg = $heading
            batteryPct = 74
            isMock     = $false
            source     = "transit"
        })
    } | ConvertTo-Json -Depth 4
    Invoke-RestMethod -Method Post -Uri "$api/api/tracking/positions" -ContentType 'application/json' -Body $body
}

function Send-Scan([string]$tagUid, [int]$loginSiteId, [double]$lat, [double]$lon) {
    # Exactly what the phone calls when a tag is tapped. TagsTypeId 1 = NFC.
    $gps = [uri]::EscapeDataString("$lat,$lon")
    Invoke-RestMethod -Method Get -Uri ("$api/api/Scanner/GetScannerTagInfoData" +
        "?siteId=$loginSiteId&TagUid=$tagUid&GuardId=$GuardId&UserId=$UserId" +
        "&TagsTypeId=1&SmartWandId=$UnitId&gpsCoordinates=$gps")
}

function Show-State([string]$when) {
    $live = Invoke-RestMethod -Uri "$api/api/tracking/live" -UseDefaultCredentials
    $me = $live.units | Where-Object { $_.unitId -eq $UnitId }
    if ($me) {
        $site = if ($me.currentSite) { $me.currentSite } else { '-' }
        Write-Host ("  {0,-22} car={1}  state={2}  site={3}  {4}m" -f `
            $when, $me.patrolCar, $me.travelState, $site, $me.stateMinutes) -ForegroundColor Yellow
    } else {
        Write-Host "  $when : unit not on the live feed (log in to the control room to read /live)" -ForegroundColor DarkGray
    }
}

# ---------- 2. drive toward the site ----------
Write-Host ""
Write-Host "2. Driving to the site (GPS every 3s)..." -ForegroundColor Cyan
$lat = -33.9000; $lon = 151.2200
for ($i = 0; $i -lt 6; $i++) {
    $lat += 0.0035; $lon -= 0.0020
    Send-Gps $lat $lon 48 330 | Out-Null
    Write-Host ("   fix {0}  {1:N4},{2:N4}" -f ($i + 1), $lat, $lon)
    Start-Sleep -Seconds 3
}
Show-State "after driving"

# ---------- 3. arrive: scan a SITE tag ----------
$r = Send-Scan $SiteTagUid $FleetSiteId $lat $lon
Show "3. Scanned SITE tag $SiteTagUid  ->  expect AtSite" $r
Start-Sleep -Seconds 3
Show-State "after site scan"

# ---------- 4. a second tag at the same site (must NOT restart the clock) ----------
Start-Sleep -Seconds 5
$r = Send-Scan $SiteTagUid $FleetSiteId $lat $lon
Show "4. Same site again - still AtSite, arrival time unchanged" $r
Show-State "after 2nd site scan"

# ---------- 5. back in the car: scan the IN-CAR tag ----------
$r = Send-Scan $InCarTagUid $FleetSiteId $lat $lon
Show "5. Scanned IN-CAR tag $InCarTagUid  ->  expect Transit" $r
Start-Sleep -Seconds 3
Show-State "after in-car scan"

# ---------- 6. drive on ----------
Write-Host ""
Write-Host "6. Driving to the next site..." -ForegroundColor Cyan
for ($i = 0; $i -lt 6; $i++) {
    $lat += 0.0030; $lon += 0.0025
    Send-Gps $lat $lon 52 45 | Out-Null
    Start-Sleep -Seconds 3
}
Show-State "in transit"

Write-Host ""
Write-Host "Done. Watch the Control Room map - the car should have moved," -ForegroundColor Green
Write-Host "shown 'At <site>' after the site scan, then 'In transit' after the in-car scan." -ForegroundColor Green
Write-Host ""
Write-Host "To clear this test unit afterwards:" -ForegroundColor DarkGray
Write-Host "  DELETE FROM TrackPoint WHERE UnitId = $UnitId;" -ForegroundColor DarkGray
Write-Host "  DELETE FROM TrackingSession WHERE UnitId = $UnitId;" -ForegroundColor DarkGray
