# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build, test, run

```bash
dotnet build CityWatch.sln            # ~3 min. 700+ warnings are normal; judge by error count only
dotnet build CityWatch.Web/CityWatch.Web.csproj   # faster, builds Data/Common transitively
dotnet test CityWatch.Data.Tests      # MSTest + Moq (same for Common/Events/Tracking .Tests)
dotnet test --filter "FullyQualifiedName~TestName"   # single test
dotnet run --project CityWatch.Kpi    # Kpi app on https://localhost:5001
```

- Build fails with MSB3027 (file locked) if an app instance is still running — kill the `CityWatch.Kpi`/`CityWatch.Web` process first. Transient static-web-asset fingerprint errors also occur; retry once.
- All projects target net7.0. Web runs under IIS Express at https://localhost:44356 — Kpi's `Settings.IrApiUrl` points at it.
- Branches: `feature/P7_<task>` (project/task numbering), PRs to `master`.

## Solution layout

| Project | Role |
|---|---|
| CityWatch.Web | Main portal: guard logbook, Key Vehicle Log, incident reports, patrol reports, mobile-app API controllers |
| CityWatch.Kpi | KPI portal + scheduled PDF report generation/emailing (`SendScheduleService`, `ReportGenerator`) |
| CityWatch.RadioCheck | Control-room radio check app |
| CityWatch.Data | EF Core `CityWatchDbContext`, entity models, and all data providers |
| CityWatch.Common | Cross-cutting services (Dropbox, helpers) |
| CityWatch.Events / CityWatch.Tracking | Secondary apps, same patterns |

## Database — the most important constraints

- **DB-first, no EF migrations.** Schema changes are numbered SQL scripts in `DbScript/` (400+, e.g. `353_...sql`); add a new script with the next number, never a migration. `CityWatchDbContext` maps the existing SQL Server DB (`prod-citywatch` name is also used for the local dev copy; connection string in each app's `appsettings.json`).
- Entity names ≠ table names in places: entity `KeyVehicleLog` → table `VehicleKeyLogs`; `GuardLog` → `GuardLogs` (logbook entries, has an `Insert_GuardLogs` trigger feeding `ClientSiteRadioChecksActivityStatus`).
- **Business day = `ClientSiteLogBook.Date`**. Logbooks exist per site + per day + per `LogBookType` (DailyGuardLog, VehicleAndKeyLog, ...). Never compare logbook IDs to decide "same site" — compare `ClientSiteId`. `UQ_Site_Type_Date` unique-indexes `(ClientSiteId, Type, Date)`.
- **Timezones**: sites span AU timezones; `DateTime` fields like `EntryTime`/`EventDateTime` hold site-local wall-clock time, with companion columns (`*Local`, `*LocalWithOffset`, `*TimeZone`) stored per record. Server `DateTime.Today` is NOT the site's today — derive site-local dates from the stored offsets.

## Data-access architecture

- Providers in `CityWatch.Data/Providers` (`IGuardLogDataProvider` etc., interface + impl in one file) are the repository layer. Each app wraps them in its own `ViewDataService` — `CityWatch.Web/Services/ViewDataService.cs`, `CityWatch.Kpi/Services/ViewDataService.cs`, and `CityWatch.Data/Services/ViewDataService.cs` are three different classes.
- Logic is frequently copy-pasted between apps (e.g. `LEDStatusForLoginUser` exists in 6+ classes; docket/report table builders are duplicated across `ReportGenerator`, `MonthlySummaryReportGenerator`, `WeeklySummaryReportGenerator`). When fixing one copy, search for the siblings.
- Perf traps that have actually bitten here: per-row queries in report loops (N+1), multi-collection `Include` cartesian explosions (use `AsSplitQuery`), and report queries loading full entities when a grouped projection suffices. Reports run for month-wide ranges — always aggregate in SQL.

## Web app patterns

- Razor Pages with named handlers: `/Area/Page?handler=X` → `OnGetX`/`OnPostX`. POSTs from JS need the `RequestVerificationToken` header (see `reports.js` for the pattern). Kpi's Settings page allows anonymous access via `?guardId=` fallback.
- Frontend is jQuery + DataTables in `wwwroot/js` (`keyvehiclelog.js`, `reports.js`); no SPA framework.
- PDF generation uses iText7. Charts are rendered by Node (`Jering.Javascript.NodeJS` → `Scripts/ir-chart.js`: d3 + jsdom + convert-svg-to-png headless Chromium). `drawPieChart` in Kpi's `ir-chart.js` is intentionally a copy of the one in Web's `report.js` — keep them in step. Chart rendering is the slow part of report generation.
- Dropbox uploads (dockets, KPI reports) go through `CityWatch.Common.Services.DropboxService`; per-site base path comes from `ClientSiteKpiSettings.DropboxImagesDir`, day folder from the generation date (`yyyyMMdd` prefix of the file name).

## Key domain flows

- **Key Vehicle Log (KVL)**: truck/visitor entries per site logbook; `ExitTime == null` = still onsite; `HasLoadVariation` closes an entry. Manual dockets are generated PDFs recorded in `KeyVehicleLogDocketHistory` (one row per KVL, upserted — regeneration overwrites) and uploaded to Dropbox only by the single-docket flow.
- **KPI schedules** (`KpiSendSchedules`): per-schedule site lists; `SendScheduleService.ProcessSchedule` imports data, generates per-site PDFs via `ReportGenerator.GeneratePdfReport`, combines, emails. Manual run: Kpi Admin/Settings → schedule popup → Run Now (`OnPostRunSchedule`).
- **Incident reports** link to KVLs indirectly through `IncidentReportsPlatesLoaded` (PlateId/TruckNo matching, no FK).
