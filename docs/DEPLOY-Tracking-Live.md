# Deploying the Tracking Feature Pack to LIVE (manual runbook)

Written 17 Aug 2026, for the state at commit `10ce8b9f` (PR #1798).
Test hosts already run this; live has **no tracking schema yet**.

## 1. Live database — run the DbScripts (SSMS on the server)

In order, from `DbScript\`. All idempotent; re-running prints "nothing to do".

| Script | What it does |
|---|---|
| `360_Create_Tracking_Schema.sql` | Core tables (TrackPoint, TrackingSession, …) |
| `361_Seed_Tracking_Enrolment.sql` | Disabled enrolment rows (harmless) |
| `364_..._PatrolCar_Callsign.sql` | Callsign columns |
| `365_..._Position_And_State.sql` | Position + AtSite/Transit columns |
| `366_Enrol_PatrolCar_Positions_For_Tracking.sql` | **Required** — cars can't pass the consent gate without it |
| `367_Enrol_Guards_For_Tracking.sql` | **Required** — same for guards |
| `368_Create_Geocode_Cache.sql` | Reverse-geocode cache |
| `369_Create_Table_TrackingDeviceToken.sql` | FCM push tokens |
| `370_Create_Table_TrackingSiteVisit.sql` | Site entered/left records (the bell) |

**Skip** `362` (rollback) and `363` (superseded wand-era enrolment).

Verify:

```sql
SELECT (SELECT COUNT(*) FROM sys.tables WHERE name IN
  ('TrackPoint','TrackSegment','TrackingSession','TrackingUnitEnrolment',
   'TrackingModeCommand','TrackingAccessAudit','GeocodeCache',
   'TrackingDeviceToken','TrackingSiteVisit')) AS TablesOf9,          -- expect 9
  (SELECT COUNT(*) FROM TrackingUnitEnrolment
     WHERE UnitId > 2000000 AND IsEnabled = 1) AS CarsEnrolled,       -- expect ~12
  (SELECT COUNT(*) FROM TrackingUnitEnrolment
     WHERE UnitId BETWEEN 1000000 AND 1999999 AND IsEnabled = 1) AS GuardsEnrolled; -- expect 1200+
```

## 2. Code onto the server

1. Merge PR #1798 to master.
2. On the server checkout `C:\c4isystem\source\CityWatch24072025`: `git pull`, confirm `git log -1`.
3. Delete `bin` + `obj` (VS recycles stale publish output), then publish from VS:
   - Web → `C:\c4isystem\Publish\citywatch_webPublish`
   - RC  → `C:\c4isystem\Publish\Citwatch_RcPublish`

## 3. Live SITE configs (before copying; robocopy preserves them)

- Web site `appsettings.json`: `"Tracking": { "Enabled": true, "IsLeaderInstance": true }`
- RC site `appsettings.json`:  `"Tracking": { "Enabled": true, "IsLeaderInstance": false }`
- Do **NOT** set `EnforceServiceArea` — absent = Australia-only envelope.
- Optional push (📳/✉ buttons): add under Tracking:
  `"Fcm": { "ServiceAccountJsonPath": "C:\\c4isystem\\keys\\citywatch-tracking-firebase-adminsdk-fbsvc-b780031587.json" }`
- Optional keyed map link on RC: `"ControlRoomMap": { "AccessKey": "<long random>" }`

## 4. Copy files — STOP BOTH APP POOLS FIRST

```
robocopy C:\c4isystem\Publish\citywatch_webPublish C:\c4isystem\Websites\ir\prod-citywatch ^
  /MIR /XF appsettings.json web.config ^
  /XD wwwroot\Pdf wwwroot\GpsImage wwwroot\jsJotform wwwroot\StaffDocs

robocopy C:\c4isystem\Publish\Citwatch_RcPublish C:\c4isystem\Websites\rc\prod-citywatch ^
  /MIR /XF appsettings.json web.config ^
  /XD wwwroot\Pdf wwwroot\GpsImage wwwroot\jsJotform wwwroot\StaffDocs
```

`/MIR` is mandatory (plain `/E` left stale DLLs → 500s, 12 Aug). The `/XD` list
protects live client report archives. Start the pools.

## 5. Post-deploy checks

1. `https://cws-ir.com/api/tracking/live` → 302 to login (not 404 = flag off; not 500 = bad copy). Same on RC host.
2. RC Control Room Map: 🔔 bell present, no F12 console errors.
3. One guard login from the mobile app (guards the IR-template regression `/MIR` once caused).
4. Full flow: car login → on map ≤ 2 min → site tag scan → 🔔 "📍 entered" →
   refresh page, alert persists → in-car tag scan → "🚗 left".

## Gotchas

- **Phones**: mobile branch has TEST urls committed in `AppConfig.cs` — live phones
  need an APK built with prod URLs. Until then the live map is simply empty.
- **Test hosts** (while on the server): delete `"EnforceServiceArea": false` from both
  test site configs + recycle — Australia-only applies there too now.

## Rollback

Set `"Tracking": { "Enabled": false }` in the site configs + recycle. Feature
vanishes entirely (routes 404, zero overhead). DB tables are inert and can stay.
