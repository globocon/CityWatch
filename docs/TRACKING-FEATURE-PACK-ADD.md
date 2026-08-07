# CityWatch.Tracking — Enterprise Feature Pack
## Architecture & Design Document

**Version:** 2.0 · **Date:** 7 August 2026
**Module:** `CityWatch.Tracking` — Live Patrol Tracking & Verified Proof of Patrol
**Governing constraint:** the existing CityWatch platform is production software serving 827 client sites and 1,202 guards. **Nothing stable gets rewritten.**

**Companion document:** [PATROL-TRACKING-STRATEGY-REVIEW.md](PATROL-TRACKING-STRATEGY-REVIEW.md) — the business case and competitive positioning. This document is the engineering design.

### Amendment record

| Version | Change |
|---|---|
| 1.0 | Initial feature-pack design |
| **2.0** | **Part II added:** §20 event-driven architecture (replaces the direct-integration model in §4.2), §21 testing strategy, §22 implementation backlog. §13.1 revised — Phase 0 API authentication **cannot** be a single step (§13.1.1); the deployed mobile app sends no credentials at all, so enforcing auth in one move would take every field device offline. |

> **⚠ Blocking decision — see §13.1.1.** The mobile app (v1.52.2, in production) contains **no `AuthenticationHeaderValue` anywhere** — every API call is anonymous. Adding a global `FallbackPolicy` would break NFC scanning, logbook entry and sync on every deployed device simultaneously. Phase 0 must run as a staged migration, and the pace of that migration is a business decision, not an engineering one.

---

## Contents

| § | Section |
|---|---|
| 1 | Existing architecture review |
| 2 | Reusable components |
| 3 | Proposed module architecture |
| 4 | Integration points |
| 5 | Tracking modes |
| 6 | Required mobile changes |
| 7 | Required backend changes |
| 8 | Required database additions |
| 9 | Required APIs |
| 10 | Required SignalR changes |
| 11 | UI/UX design |
| 12 | Performance analysis |
| 13 | Security review |
| 14 | AI readiness |
| 15 | Deployment strategy |
| 16 | Risks |
| 17 | Rollback strategy |
| 18 | Phase plan |
| 19 | Approval checklist |
| | **Part II — v2.0** |
| 20 | **Event-driven architecture** (supersedes §4.2) |
| 21 | **Testing strategy** |
| 22 | **Implementation backlog & milestones** |

---

## 0. Design Stance

Three principles govern every decision in this document.

**Additive, never subtractive.** The feature pack adds a project, a `DbContext`, a set of tables, a controller, a hub and a mobile service. It changes **11 lines** across the existing codebase (§4.6). Nothing existing is refactored, and no existing table gains a column.

**NFC is the spine; GPS is the connective tissue.** The brief is right, and this inverts the usual telematics design. An NFC scan already establishes officer + vehicle + checkpoint + site + customer + timestamp with physical certainty. GPS does not need to re-establish any of that — it needs to prove *what happened between two scans*. This makes the GPS record smaller, cheaper and more defensible, because every GPS run is bracketed by two facts rather than floating free.

**Disabled means absent.** With `Tracking:Enabled = false` the module registers no hosted services, opens no database connections, maps no endpoints, and adds nothing to any page. This is not a runtime `if` — it is a registration-time branch, so "off" costs zero.

---

## 1. Existing Architecture Review

All findings verified against the current `master` working tree.

### 1.1 Solution topology

Seven projects, `net7.0`, built with the .NET 10 SDK:

| Project | Role |
|---|---|
| `CityWatch.Web` | Main Razor Pages app + 15 API controllers (5 mobile-facing) |
| `CityWatch.RadioCheck` | Control room app — hosts `ControlRoomMap` |
| `CityWatch.Kpi` | Reporting / KPI generation |
| `CityWatch.Data` | Shared `CityWatchDbContext`, 210 models, provider classes |
| `CityWatch.Common` | Shared helpers + `UpdateHub` |
| `CityWatch.Data.Tests`, `CityWatch.Common.Tests` | Test projects |

Three separate web applications **share one `CityWatchDbContext`** with **214 `DbSet` properties**. Each app registers it independently (`Web/Program.cs:29`, `RadioCheck/Program.cs:27`, `Kpi/Program.cs:24`).

**Architectural consequence:** every DbSet added to `CityWatchDbContext` is paid for by all three applications at model-build time, and any change to that class is a change to the shared surface of the entire platform. **This is the single strongest argument for a separate `TrackingDbContext`** (§3.3).

### 1.2 Database change management

**There are no EF Core migrations.** Schema changes ship as **401 hand-written, numbered SQL scripts** in `DbScript/` (e.g. `308_Alter_Table_ClientSiteSmartWandTags.sql`), applied manually.

**Consequence for this design:** the feature pack must ship its own numbered, **idempotent** SQL scripts, and the rollback path is a corresponding uninstall script (§17). No `dotnet ef` command will ever be run against this database.

### 1.3 The Control Room

`CityWatch.RadioCheck/Pages/ControlRoomMap.cshtml` (671 lines) + `wwwroot/js/controlRoomMap.js` (1,354 lines):

- Leaflet 1.9.4 + `leaflet.markercluster` 1.5.3, CARTO light/dark basemaps, Australia-bounded
- **A dedicated `carLayer = L.layerGroup()` for PCAR vehicles, already separate from the clustered site layer**
- CSS marker glide already present: `.leaflet-marker-icon { transition: transform 1.8s linear; }`, `.pcar-ghost` at `.9s`
- `OnGetChangeToken` — a cheap poll endpoint returning a composite token over activity/wand/PCAR max-IDs and timestamps, so a full reload only happens on real change
- Filter model: status / site / region / updated / alert / frequency / guard text
- Diff-based marker updates that preserve operator context (no re-centre, no popup close on refresh)

**This is the highest-value existing asset in the whole design.** The car layer, the glide transition and the diff-update discipline are precisely what live tracking needs, and they already work.

### 1.4 The PCAR workflow

| Entity | Purpose | Rows |
|---|---|---|
| `PcarRoute` | Named route bound to a SmartWand allocation | — |
| `PcarRouteDetails` | Ordered stops, per-day time windows (`StartMon`/`EndMon`/`VisitMon`, plus public-holiday `Pho`) | — |
| `PcarRouteDailyVisits` | Actual visits: `GpsCoordinates`, `TimeOn`/`TimeOff`, status enum, `ParentVisitId` | **0** |
| `PcarVisitHistory` | Audit: `ServerUtcTime`, `EventDateTimeLocal`, `EventDateTimeLocalWithOffset`, timezone name/short/offset, `EventMobileUtcDateTime` | — |

Visits are generated by `AppConfigurationProvider` (~line 606) and persisted through `GuardLogDataProvider.SavePcarSaveVisitTimeAsync`. Visit acceptance/cancellation is handled in `GuardSecurityNumberController` (~line 4068).

**`PcarVisitHistory`'s time model is adopted verbatim as the house standard** for all tracking timestamps (§8.4). It already reconciles device clock, device timezone and server clock — the problem most tracking systems discover too late.

### 1.5 The NFC workflow

`ScannerController` (`api/Scanner`) is the NFC surface:

| Endpoint | Role |
|---|---|
| `GET GetScannerControlSettings` | Per-site scanner config |
| `GET GetScannerTagInfoData` | Live scan resolution |
| `POST SyncOfflineSmartWandTagHitData` | **Offline scan replay** |
| `POST CheckAndRegisterDeviceWithSmartWand` | **Device registration** |
| `POST GetSmartWandByDeviceId` | **Device → unit resolution** |
| `POST SaveNFCtagInfoData`, `GET GetClientSiteSmartWands`, … | Tag/wand administration |

The scan path runs `MobileAppDataServices.CreateSmartWandScannerHitLogRecord(...)` — which **already accepts `GPScoordinates`** — then raises a `PostActivityRequest` that writes the logbook entry.

**Critical finding:** the NFC scan already carries GPS. The Normal Patrol tracking mode (§5.1) therefore requires **no new mobile capture code at all** — only that the existing coordinate be forwarded to the tracking store.

**Pre-existing performance note:** `SyncOfflineSmartWandTagHitData` contains a `Thread.Sleep(500)` inside its per-record loop (a deliberate wait for SignalR logbook refresh). A device replaying 100 offline scans blocks a request thread for 50 seconds. **The tracking sync path must not go through this endpoint** — §6.4 keeps it separate for exactly this reason.

### 1.6 `ClientSiteSmartWand` — the unit identity that already exists

```csharp
public class ClientSiteSmartWand {
    int Id; int ClientSiteId;
    string SmartWandId; string PhoneNumber; string SIMProvider; string IMEI;
    string? DeviceType; string? DeviceId; string? DeviceName;
    int? PatrolCarId;                    // → ClientSitePatrolCar
    [NotMapped] string? PatrolCarName;
    bool IsDeleted;
}
```

**This is the single most important reuse discovery in the review.** It already binds:

> physical device (`DeviceId`, `IMEI`) → wand (`SmartWandId`) → **patrol car** (`PatrolCarId`) → site (`ClientSiteId`)

There is no need to invent a "tracked unit" entity, a device registry, or a vehicle-assignment model. **`ClientSiteSmartWand.Id` *is* the tracking unit key.** `PcarRoute.Smartwandallocation` already points at it, and `PcarRouteDailyVisits.SmartWandId` already stamps every visit with it.

### 1.7 Mobile application

`C4iSytemsMobApp` — .NET MAUI 8 (`net8.0-android;net8.0-ios;net8.0-maccatalyst`), app ID `com.C4isystem.c4isystemsmobapp`, v1.52.2, min Android SDK 21 / target 34, iOS 11+.

Existing services (`C4iSytemsMobApp/Services/`):

| Service | Relevance |
|---|---|
| `SyncService` | **Offline sync engine.** Semaphore-guarded `SyncAsync()` calling six per-type sync methods over a local EF Core `AppDbContext` |
| `SyncApiService` | HTTP transport for sync |
| `ConnectivityListener` | Network state monitoring |
| `PermissionService` | GPS + Bluetooth permissions; one-shot `Geolocation.GetLocationAsync` at `Medium` accuracy, cached to `Preferences["GpsCoordinates"]` |
| `NfcService`, `TagStatusService`, `ScannerControlServices` | NFC pipeline |
| `MessageBus` | In-app eventing |
| `GuardApiServices` | Authenticated API calls |

`SyncService.SyncAsync()` is the extension point — six calls become seven (§6.4).

**Blocking platform gaps** (unchanged from the strategy review):

| Gap | Location | Effect |
|---|---|---|
| No `ACCESS_BACKGROUND_LOCATION` | `Platforms/Android/AndroidManifest.xml` | Android 10+ stops delivering location off-foreground |
| No `FOREGROUND_SERVICE` / `FOREGROUND_SERVICE_LOCATION` | same | Cannot run a location foreground service on target SDK 34 |
| Only `LocationWhenInUse` requested | `PermissionService` | Wrong runtime grant |
| **Duplicate `UIBackgroundModes` key** | `Platforms/iOS/Info.plist:41` and `:45` | Second array overrides the first; neither declares `location`. Background location will not be delivered, **and the `audio` mode is silently dead today.** |

### 1.8 Authentication and authorisation

- Cookie authentication only (`Web/Program.cs:101`), no `FallbackPolicy`
- **0 of 15 API controllers carry `[Authorize]`** — every endpoint is anonymous
- `LoginController.GetUserLogin(string userName, string password)` — no `[HttpPost]`; **credentials in the query string**
- Both hubs mapped without `.RequireAuthorization()` (`Web/Program.cs:171–172`)
- **The entire role model is `User.IsAdmin` (a single `bool`).** There is no roles table, no claims-based permission framework.
- `CityWatch.RadioCheck` uses `AllowAnonymousToFolder("/")`; pages self-guard with `User.Identity.IsAuthenticated`
- CORS policy `AllowSpecificOrigin` uses `.AllowAnyHeader().AllowAnyMethod().AllowCredentials()` against a configured origin list

**Design consequence:** the brief requires role-based permissions and customer isolation, but **rebuilding CityWatch's auth is explicitly out of scope.** §13.2 resolves this with a tracking-local permission table that layers on top of the existing identity rather than replacing it.

### 1.9 Real-time plumbing

```csharp
app.MapHub<UpdateHub>("/updateHub");                        // Program.cs:171
app.MapHub<MobileAppSignalRHub>("/MobileAppSignalRHub");    // Program.cs:172
```

- `MobileAppSignalRHub` — **correct pattern**: `Groups.AddToGroupAsync(Context.ConnectionId, ClientSiteId.ToString())`
- `UpdateHub` — **anti-pattern**: `Clients.All.SendAsync("ReceiveDuressAlarmAlert", …)`. Every connected client receives every duress alert.

`UpdateHub` is left untouched. `PatrolTrackingHub` copies `MobileAppSignalRHub`'s group discipline.

### 1.10 Code-health observations (context for "do not modify")

- `GuardLogDataProvider.cs` — **8,600+ lines**
- `GuardSecurityNumberController.cs` — **4,100+ lines**
- `CityWatchDbContext` — 214 DbSets across three applications

These files work and are in production. **The design deliberately touches none of them.** Their size is itself the argument.

### 1.11 Production scale

| Table | Rows |
|---|---|
| `ClientSites` | 827 |
| `Guards` | 1,202 |
| `GuardLogs` | 2,356,302 |
| `ClientSitePatrolCars` | 20 |
| `PcarRouteDailyVisits` | 0 |

---

## 2. Reusable Components

Everything below is used **as-is**. This is what makes the feature pack small.

| # | Existing asset | Reused for | Change required |
|---|---|---|---|
| 1 | **`ClientSiteSmartWand`** (device→wand→car→site) | The tracking unit identity | **None** |
| 2 | `ClientSitePatrolCar` (Model, Rego) | Vehicle display metadata | None |
| 3 | `ClientSite.Gps`, `.Name`, `.State`, `.TypeId` | Geofence centres, labels, customer scope | None |
| 4 | `PcarRoute` / `PcarRouteDetails` | Planned route → adherence scoring (Phase 3) | None |
| 5 | `PcarRouteDailyVisits` (incl. `GpsCoordinates`) | NFC-anchored fixes | None |
| 6 | **`PcarVisitHistory` time model** | Timestamp schema for all telemetry | None (copied as a pattern) |
| 7 | `ScannerController` NFC pipeline | Normal Patrol mode anchors | None (read-only tap, §4.2) |
| 8 | **`SyncService` / `SyncApiService`** | Offline queue for track points | **+1 method call** |
| 9 | `ConnectivityListener` | Network-recovery trigger | None |
| 10 | `PermissionService` | Location permission flow | Extended, not replaced |
| 11 | Mobile `AppDbContext` (device EF Core) | Local point buffer | +1 DbSet |
| 12 | `MessageBus` | Mode-change notification in-app | None |
| 13 | **`controlRoomMap.js` `carLayer`** | Live vehicle markers | Extended via a new file |
| 14 | Leaflet + markercluster + CARTO | Map rendering | None |
| 15 | `MobileAppSignalRHub` group pattern | Hub scoping template | None (copied) |
| 16 | `OnGetChangeToken` polling model | Degraded-mode fallback | None |
| 17 | Cookie auth + `User.Identity` | Operator identity | None |
| 18 | `FileDownloadAuditLogs` / `KeyVehicleLogAuditHistory` | Audit-table shape | None (pattern) |
| 19 | `DbScript/` numbered SQL convention | Schema deployment | None (followed) |
| 20 | `GuardRcClientSiteAccess`, `HrSettingsClientSites` | Site-scope precedent | None (pattern) |

**Reuse ratio: the module adds one new concept — the position stream. Everything it hangs from already exists.**

---

## 3. Proposed Module Architecture

### 3.1 New project

```
CityWatch.Tracking/                        (net7.0 class library)
├── Configuration/
│   ├── TrackingOptions.cs                 Enabled, sampling policy, retention, limits
│   └── ServiceCollectionExtensions.cs     AddCityWatchTracking() / MapCityWatchTracking()
├── Data/
│   ├── TrackingDbContext.cs               6 DbSets — SEPARATE from CityWatchDbContext
│   └── Entities/                          TrackPoint, Session, Segment, UnitState,
│                                          ModeCommand, AccessAudit
├── Contracts/
│   ├── IPositionSource.cs                 Device-agnostic ingest (phone / telematics / API)
│   ├── PositionBatch.cs / PositionPoint.cs
│   └── TrackingMode.cs                    Normal | Transit | Live | Duress
├── Services/
│   ├── IngestService.cs                   Validate → dedupe → plausibility → enqueue
│   ├── LiveStateStore.cs                  ILiveStateStore: InMemory (P1) → Redis (P2)
│   ├── ModeCommandService.cs              Live Mode arbitration + expiry
│   ├── GeofenceEvaluator.cs               Site enter / exit / dwell
│   ├── SegmentBuilder.cs                  Roll-up on session/leg close
│   └── NfcAnchorSubscriber.cs             Reads scan hits → anchor points (§4.2)
├── Hosted/
│   ├── PositionWriter.cs                  Channel<T> drain → SqlBulkCopy
│   ├── BroadcastTicker.cs                 1 Hz scoped diff frames
│   ├── SessionReaper.cs                   Close stale sessions
│   └── MaintenanceJob.cs                  Partition + retention
├── Hubs/
│   └── PatrolTrackingHub.cs               Control-room-scoped groups
└── Api/
    └── TrackingController.cs              Ingest + query + command
```

Referenced by `CityWatch.Web` (ingest + hub) and `CityWatch.RadioCheck` (map queries). **`CityWatch.Data` does not reference `CityWatch.Tracking`** — the dependency points one way only, so the existing data layer cannot develop a dependency on the module.

### 3.2 Registration — the entire integration into `Program.cs`

```csharp
// CityWatch.Web/Program.cs — 1 line added among ~40 existing AddScoped calls
builder.Services.AddCityWatchTracking(builder.Configuration);

// …after app.MapHub<MobileAppSignalRHub>(…) — 1 line
app.MapCityWatchTracking();
```

Inside `AddCityWatchTracking`:

```csharp
public static IServiceCollection AddCityWatchTracking(this IServiceCollection s, IConfiguration cfg)
{
    var opt = cfg.GetSection("Tracking").Get<TrackingOptions>() ?? new();
    s.AddSingleton(opt);
    if (!opt.Enabled) return s;          // ← registration-time branch: OFF costs nothing
    s.AddDbContext<TrackingDbContext>(…);
    s.AddSingleton<ILiveStateStore, InMemoryLiveStateStore>();
    s.AddScoped<IIngestService, IngestService>();
    s.AddHostedService<PositionWriter>();
    s.AddHostedService<BroadcastTicker>();
    …
    return s;
}
```

`MapCityWatchTracking()` returns immediately when disabled, so no endpoint and no hub route exists. **`Tracking:Enabled = false` is indistinguishable from the module not being deployed.**

### 3.3 Why a separate `TrackingDbContext`

| | Add to `CityWatchDbContext` | Separate `TrackingDbContext` |
|---|---|---|
| Blast radius | Touches a 214-DbSet class shared by 3 apps | Zero |
| Model-build cost | Paid by Kpi and RadioCheck too | Paid only where registered |
| Accidental joins | An `.Include()` can reach the point table | Structurally impossible |
| Independent deploy | No | Yes |
| Rollback | Requires editing shared code | Delete a registration line |
| Bulk-write tuning | Inherits shared context config | Tuned in isolation (`NoTracking` default) |

Both contexts point at the same physical database — no distributed transaction, no second connection string. The separation is a *code* boundary, which is exactly the boundary the brief asks for.

### 3.4 Per-customer enablement

`TrackingOptions.Enabled` is the global kill switch. Per-customer control is data, not configuration:

```
TrackingUnitEnrolment (SmartWandId PK, IsEnabled, EnrolledUtc, ConsentRecordedUtc, …)
```

A unit is tracked only when the module is enabled **and** the unit is enrolled **and** consent is recorded (§13.5). Enrolment is per `ClientSiteSmartWand`, which resolves through `ClientSiteId → ClientSite.TypeId` to the customer. **Enabling a customer is inserting rows, not a deployment.**

---

## 4. Integration Points

Exhaustive. Every place the feature pack meets existing code.

### 4.1 Summary

| # | Point | Direction | Type | Existing code changed |
|---|---|---|---|---|
| I1 | `Web/Program.cs` registration | — | Additive | **2 lines** |
| I2 | NFC scan → anchor point | Read | **Read-only tap** | **0 lines** |
| I3 | `ClientSiteSmartWand` → unit identity | Read | Query | 0 |
| I4 | `ClientSite.Gps` → geofences | Read | Query | 0 |
| I5 | `PcarRouteDetails` → planned route | Read | Query (Phase 3) | 0 |
| I6 | Mobile `SyncService` | Write | Additive | **1 line** |
| I7 | Mobile permissions/manifests | — | Additive | manifest + plist |
| I8 | `ControlRoomMap.cshtml` | Read | Additive | **~4 lines** (2 script tags, 2 markup hooks) |
| I9 | `RadioCheck/Program.cs` | — | Additive | **2 lines** |
| I10 | Duress → Duress Mode | Read | Event subscription | 0 |
| I11 | KPI reporting | Read | New views only | 0 |

**Total existing-code delta: 11 lines + 2 mobile manifest files.**

### 4.2 I2 — the NFC anchor tap (the design's keystone)

The brief requires that an NFC scan immediately publishes NFC + GPS + timestamp + accuracy. The obvious implementation — editing `CreateSmartWandScannerHitLogRecord` — is exactly what the primary rule forbids: that method sits on the critical path of a working production workflow.

**Recommended: the mobile app sends the anchor, not the server.**

When the app completes a scan it already holds every field. It writes one `TrackPoint` with `SourceType = NfcAnchor` and `AnchorTagUid` into its **existing local cache**, which the existing `SyncService` uploads to the **new** tracking endpoint. The NFC path itself is never entered.

| Approach | Existing code changed | Risk to NFC workflow |
|---|---|---|
| Modify `CreateSmartWandScannerHitLogRecord` | Core production method | **High** — a tracking bug breaks scanning |
| Server-side event/hook in the scan path | Provider + service wiring | Medium |
| **Mobile emits a parallel anchor point** | **None** | **None** |

The third option also degrades correctly: if tracking is off, unenrolled or broken, **the NFC scan is completely unaffected** — it does not know tracking exists. That property is worth more than the small duplication of the coordinate.

**Reconciliation (Phase 3):** a nightly job matches anchor points to `ClientSiteSmartWandTagsHitLog` rows on `(SmartWandId, TagUid, HitUtcDateTime ± 60s)`. A verified match is what produces the *Verified* in Verified Proof of Patrol — the two independent records agreeing is the evidence. A mismatch is a flagged exception, which is also useful.

### 4.3 I8 — Control Room integration

`ControlRoomMap.cshtml` gains **two script tags and two markup hooks**, all inside an `@if (Model.TrackingEnabled)` block:

```html
@if (Model.TrackingEnabled) {
    <script src="~/lib/leaflet-rotatedmarker/leaflet.rotatedMarker.js"></script>
    <script src="~/js/tracking/controlRoomTracking.js"></script>
}
```

`controlRoomTracking.js` is a **new file** that attaches to the existing map through a small published surface:

```javascript
window.CRM = { map, carLayer, sites, COL, selectSite };   // added to controlRoomMap.js
```

That one exported object is the only change to the 1,354-line file. Tracking reads it and never writes to internal state. **If `controlRoomTracking.js` throws, the existing map keeps working** — the tracking layer is loaded second and adds only to `carLayer`.

### 4.4 I6 — Mobile sync integration

```csharp
public async Task SyncAsync() {
    …
    await SyncSmartWandTagsHitLogCache();
    await SyncLogbookCache();
    await SyncLogbookDocumentsCache();
    await SyncPatrolCarLogsCache();
    await SyncCustomFieldLogsCache();
    await SyncIrRequestsLogsCache();
    await SyncTrackingPointsCache();      // ← the single added line
}
```

`SyncTrackingPointsCache()` is a new method in a **partial class file** (`SyncService.Tracking.cs`), so `SyncService.cs` itself changes by one line. It wraps its whole body in try/catch: **a tracking sync failure must never abort the six existing syncs**, and it runs last so it cannot delay them.

### 4.5 I10 — Duress integration

`ClientSiteDuress` already carries `GpsCoordinates`. Duress Mode is triggered by the app locally (immediate, no round-trip) *and* by a server-side mode command when the control room raises duress. **The duress alert path itself is untouched** — tracking observes it, never gates it. If tracking is down, duress still fires.

### 4.6 The complete existing-code diff

| File | Lines | Nature |
|---|---|---|
| `CityWatch.Web/Program.cs` | 2 | Two registration calls |
| `CityWatch.RadioCheck/Program.cs` | 2 | Two registration calls |
| `CityWatch.RadioCheck/Pages/ControlRoomMap.cshtml` | ~4 | Flag-guarded script tags + panel hooks |
| `…/wwwroot/js/controlRoomMap.js` | 1 | Export `window.CRM` |
| `…/Pages/ControlRoomMap.cshtml.cs` | ~2 | `TrackingEnabled` property |
| Mobile `SyncService.cs` | 1 | One method call |
| Mobile `AppDbContext.cs` | 1 | One DbSet |
| Mobile `MauiProgram.cs` | ~2 | Service registration |
| `AndroidManifest.xml` | +4 permissions | Additive |
| `Info.plist` | fix duplicate key | **Bug fix, required regardless** |
| **Total** | **~15 lines + 2 manifests** | |

---

## 5. Tracking Modes

### 5.1 Mode definitions

| Mode | Trigger | Sample | Upload | Purpose |
|---|---|---|---|---|
| **Normal Patrol** | NFC scan completes | **event-driven only** | with next batch (≤60 s) | Anchor: NFC + GPS + timestamp + accuracy. **Zero continuous GPS.** |
| **Transit** | Geofence exit, or motion > 15 km/h for 30 s | adaptive 10–60 s (§5.2) | 60 s batch | Verify movement between sites |
| **Live** | **Operator presses "Track Vehicle Live"** | 2–5 s | 5 s batch | Smooth real-time movement, selected vehicles only |
| **Duress** | Duress raised (device or server) | 2 s | **immediate, unbatched** | Officer safety |

**Precedence: Duress > Live > Transit > Normal.** A duress event overrides an expiring Live session; Live overrides Transit; Transit falls back to Normal on geofence entry or 5 minutes stationary.

**Normal Patrol is the default and it costs almost nothing** — no background service work, no periodic GPS, one extra fix per scan the app was already taking. A customer can run tracking indefinitely in Normal-only and get NFC-anchored proof with negligible battery impact. This is the mode that makes the feature adoptable.

### 5.2 Transit Mode — recommended intervals

| Sub-state | Detection | Sample | Rationale |
|---|---|---|---|
| Stationary | < 10 m movement over 3 fixes | **60 s heartbeat** | A parked car needs one point, not thirty |
| Steady travel | > 15 km/h, heading stable ±15° | **10 s** | Straight-line travel interpolates perfectly |
| Manoeuvring | heading Δ > 25°, or speed Δ > 20 km/h | **4 s** | Corners are where fixed intervals lose the road |
| Approaching site | within 500 m of a route stop | **5 s** | Arrival time is contractual |

Plus **distance filtering** (drop any fix within 25 m of the last accepted one unless the heartbeat is due) and **corner preservation** (always keep a fix where heading changes materially — this is what makes a replayed trail follow the road instead of cutting corners).

**Recommended headline answer: ~10 seconds while driving, 60 seconds while stationary, 4 seconds while manoeuvring.** Against a fixed 2-second poll this removes roughly 60–80% of points while producing a *better* trail, because the surviving points are the ones carrying information.

**All thresholds are server-pushed policy, not compiled constants.** They will need tuning against real patrol behaviour, and an app-store release cycle must not be in that loop.

### 5.3 Live Mode — the command channel

This is the design's hardest problem. The operator presses a button; the phone must react in seconds; the phone is not holding an open socket (deliberately — §7.3).

**Recommended: authoritative command-on-response, accelerated by silent push.**

```
Operator clicks "Track Live" on PC-04
   │
   ▼
POST /api/tracking/command  { unitId, mode: Live, ttlSeconds: 900 }
   │
   ├─► TrackingModeCommand row written (server state = authority)
   │
   ├─► [fast path]  silent push (FCM data / APNS content-available)
   │                → app wakes, polls command endpoint, enters Live
   │                → typical latency 1–5 s
   │
   └─► [reliable path]  every ingest response carries the current
                        desired mode; the app reconciles on each batch
                        → worst-case latency = one batch interval (≤60 s)
```

**Why the response carries the mode:** push delivery is best-effort — a doze-mode Android or a throttled iOS device may not receive it. But the device is *already* talking to the server on its own schedule, and that conversation cannot be lost without the device also being offline (in which case no mechanism would work). Making the ingest response authoritative means **the fast path is an optimisation, never a dependency.** This is the difference between a feature that works in a demo and one that works at 3 a.m. in a basement car park.

Command semantics:
- **TTL-bounded** — Live Mode carries an expiry (default 15 min) and auto-reverts. A forgotten Live session cannot drain a battery all night.
- **Idempotent** — the command has a monotonic `CommandSeq`; the app applies only newer commands, so out-of-order delivery is safe.
- **Explicitly cancellable** — the operator can end it; the app confirms in its next batch.
- **Acknowledged** — until the app confirms, the control room shows *"Live requested…"*, not *"Live"*. **The UI never claims a state the device has not confirmed.**
- **Concurrency-capped** — `Tracking:MaxConcurrentLiveUnits` (default 10). Live Mode is a spotlight, not a floodlight; the cap protects both the server and the honesty of the feature.
- **Audited** — every Live request records operator, unit, time and duration (§13.4). Turning on close surveillance of a named officer is exactly the action that must leave a trace.

### 5.4 Mode state machine

```
                    ┌──────────────────────────────────────┐
                    │             NORMAL                   │  default; NFC anchors only
                    └───┬──────────────────────────────▲───┘
        geofence exit / │                              │ geofence entry
        motion detected │                              │ or 5 min stationary
                    ┌───▼──────────────────────────────┴───┐
                    │             TRANSIT                  │  adaptive 10–60 s
                    └───┬──────────────────────────────▲───┘
       operator command │                              │ TTL expiry or cancel
                    ┌───▼──────────────────────────────┴───┐
                    │              LIVE                    │  2–5 s, TTL-bounded
                    └───┬──────────────────────────────▲───┘
             duress on  │                              │ duress cleared
                    ┌───▼──────────────────────────────┴───┐
                    │             DURESS                   │  2 s, unbatched, no TTL
                    └──────────────────────────────────────┘
```

Duress is enterable from **any** state and exitable only by explicit cancellation. There is no timeout on duress — that is deliberate.

---

## 6. Required Mobile Changes

### 6.1 Platform prerequisites (blocking, Phase 0)

**Android** — add to `Platforms/Android/AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```
plus a `Service` with `android:foregroundServiceType="location"`. Android 11+ requires background location as a **separate second prompt** after foreground is granted, preceded by an in-app rationale — requested together, users decline.

**iOS** — merge the two `UIBackgroundModes` arrays (`Info.plist:41` and `:45`) into one containing `location`, `bluetooth-central`, `bluetooth-peripheral` and `audio`. Then `AllowsBackgroundLocationUpdates = true`, `ShowsBackgroundLocationIndicator = true` (also a privacy control — §13.5), `PausesLocationUpdatesAutomatically = false`.

**This plist fix is required regardless of the tracking decision** — the `audio` background mode is currently dead.

### 6.2 New mobile components

```
Services/Tracking/
├── TrackingService.cs            Mode state machine; the only public entry point
├── LocationSampler.cs            Platform-abstracted sampling with adaptive policy
├── TrackingPointStore.cs         Local buffer over the existing AppDbContext
├── TrackingModeClient.cs         Command poll + reconcile from ingest response
├── TrackingPolicy.cs             Server-pushed thresholds (§5.2)
└── Platforms/
    ├── Android/TrackingForegroundService.cs
    └── iOS/TrackingLocationDelegate.cs
```

`TrackingService` is registered in `MauiProgram.cs` and **starts only when a patrol session is active and the unit is enrolled**. No session ⇒ the sampler is never constructed.

### 6.3 Reused without modification

Login, authentication, patrol workflow, NFC workflow, `ConnectivityListener`, `MessageBus`, `GuardApiServices` transport, `Preferences` storage, `AppDbContext` infrastructure. `PermissionService` is **extended** with a `RequestBackgroundLocationAsync()` method; its existing methods are untouched.

### 6.4 Offline queue

One DbSet added to the existing mobile `AppDbContext`:

```csharp
public DbSet<TrackingPointCache> TrackingPointCache { get; set; }
```

Behaviour: persist to SQLite **before** attempting upload; ring buffer capped at ~10,000 points (a full 12-hour shift offline, still trivially small); upload oldest-first in bounded batches flagged `Backfilled` so the map does not animate a vehicle through an hour of history; exponential backoff with jitter; honour `Retry-After`; delete locally **only on confirmed server acknowledgement**; **never block the UI on upload**.

**Deliberately separate from `SyncOfflineSmartWandTagHitData`** — that endpoint's `Thread.Sleep(500)` per record (§1.5) makes it unsuitable for volume, and mixing the two would let a tracking backlog delay NFC replay. Different endpoint, different queue, same `SyncService` orchestration.

### 6.5 Battery

Vehicle chargers make this tractable, but it must still be measured, not asserted.

- Fused/network-assisted provider when accuracy demand is low
- Batched uploads so the cellular radio wakes once per minute — **radio wake-ups typically cost more than the GPS chip**
- `BatteryPct` in every batch; surfaced as unit health in the control room
- Auto-degrade to the stationary profile below a configured threshold, and tell the operator
- **Acceptance target: ≤ 10% per hour in Transit mode on a mid-range Android, screen off.** Measured on real devices before pilot. Any number quoted before that measurement is a guess. **Normal Patrol mode should be within noise of today's consumption** — that is the point of it.

---

## 7. Required Backend Changes

### 7.1 New — all inside `CityWatch.Tracking`

Ingest, live state, mode command, geofence, segment builder, four hosted services, hub, controller (§3.1). None of it lives in `CityWatch.Data` or `CityWatch.Web` beyond the controller's registration.

### 7.2 Changed in existing projects

**Two lines each** in `CityWatch.Web/Program.cs` and `CityWatch.RadioCheck/Program.cs`. Nothing else.

### 7.3 Ingest path

```
POST /api/tracking/positions
  → authenticate + authorise (unit enrolled? session valid? operator scope?)
  → per-unit rate limit
  → validate  (AU bounds; accuracy ≤ 100 m; timestamp sanity)
  → dedupe    ((UnitId, Seq) — idempotent, safe to retry)
  → plausibility (implied speed > 250 km/h ⇒ FLAG, never silently drop)
  → LiveStateStore.Update(unit)          ← synchronous, in-memory, ~microseconds
  → Channel<TrackPoint>.Write(points)    ← non-blocking enqueue
  → GeofenceEvaluator.Evaluate(points)   ← in-process, cheap
  → RESPOND with { accepted, desiredMode, policy, commandSeq }   ← §5.3
```

**The HTTP response never waits for a database write.** The `PositionWriter` hosted service drains the channel and writes with `SqlBulkCopy` every ~1 s or 500 points. If the database is briefly unavailable, ingest keeps accepting and the live map keeps working — the buffer absorbs it, and the channel is bounded so it fails loudly rather than exhausting memory.

**Never write track points through EF Core change tracking.** `TrackingDbContext` exists for queries and small writes; the hot path uses `SqlBulkCopy` directly.

### 7.4 Background jobs and multi-instance safety

| Job | Cadence | Leader-only |
|---|---|---|
| `PositionWriter` | continuous | No — per-instance channel |
| `BroadcastTicker` | 1 Hz | **Yes** |
| `SessionReaper` | 60 s | **Yes** |
| `SegmentBuilder` | on close + nightly | **Yes** |
| `MaintenanceJob` | monthly | **Yes** |

**Running `BroadcastTicker` on three instances triples every frame.** Phase 1 is single-instance and uses a configuration flag (`Tracking:IsLeaderInstance`). Phase 2 introduces a Redis-based distributed lock. This must be decided before the first multi-instance deploy, not after.

---

## 8. Required Database Additions

**No existing table is altered. No existing column changes type. No existing index is touched.**

### 8.1 New tables

| Table | Purpose | Growth |
|---|---|---|
| `TrackingUnitEnrolment` | Per-unit on/off + consent | ~fleet size |
| `TrackingSession` | Officer + unit + route + start/end | ~shifts/day |
| `TrackPoint` | **Append-only position stream** | High — partitioned |
| `TrackSegment` | Leg/trip roll-up — **all reporting reads this** | ~legs/day |
| `TrackingModeCommand` | Live/Duress command + ack audit | Low |
| `TrackingAccessAudit` | Who viewed whose location | Moderate |

### 8.2 `TrackPoint`

```sql
CREATE TABLE dbo.TrackPoint (
    Id              bigint IDENTITY(1,1) NOT NULL,
    UnitId          int            NOT NULL,   -- ClientSiteSmartWand.Id (no FK — see below)
    SessionId       uniqueidentifier NOT NULL,
    Seq             int            NOT NULL,   -- device sequence, for dedupe
    RecordedUtc     datetime2(0)   NOT NULL,   -- device clock
    ReceivedUtc     datetime2(0)   NOT NULL,   -- server clock
    Latitude        decimal(9,6)   NOT NULL,
    Longitude       decimal(9,6)   NOT NULL,
    SpeedKph        smallint       NULL,
    HeadingDeg      smallint       NULL,
    AccuracyM       smallint       NULL,
    BatteryPct      tinyint        NULL,
    SourceType      tinyint        NOT NULL,   -- 1 NfcAnchor 2 Transit 3 Live 4 Duress
    ModeAtCapture   tinyint        NOT NULL,
    Flags           tinyint        NOT NULL DEFAULT 0, -- mock|lowAcc|implausible|backfilled
    AnchorTagUid    varchar(64)    NULL,       -- set when SourceType = NfcAnchor
    CONSTRAINT PK_TrackPoint PRIMARY KEY NONCLUSTERED (Id)
);
CREATE CLUSTERED INDEX CX_TrackPoint_Unit_Time ON dbo.TrackPoint (UnitId, RecordedUtc);
CREATE UNIQUE INDEX UX_TrackPoint_Dedupe     ON dbo.TrackPoint (UnitId, SessionId, Seq);
```

**No foreign keys.** `UnitId` references `ClientSiteSmartWand.Id` logically but not physically. Three reasons: FK checks cost measurably on high-rate inserts; a deleted wand must not cascade-delete evidentiary history; and **an FK would create a hard schema dependency from the tracking module onto a stable table**, which is precisely the coupling the primary rule forbids. Referential integrity is enforced at ingest (the unit must be enrolled) and on read (a join that drops unmatched rows).

**Partitioning:** monthly on `RecordedUtc`. Retention by `ALTER TABLE … SWITCH` (instant metadata operation) rather than `DELETE` (a logged, blocking scan). Phase 1 may ship unpartitioned at 20 vehicles; **the partition function ships in Phase 1 regardless** so the switch is never a migration.

**Column-choice notes:** `decimal(9,6)` gives ~11 cm resolution — well beyond GPS accuracy, and exact, unlike `float`. `smallint` for speed/heading/accuracy is sufficient (0–32767) and halves the row. `datetime2(0)` — sub-second precision on a GPS fix is meaningless. Row ≈ 60 bytes; ~80 with index overhead.

**Deliberately not used: SQL Server `geography`.** It costs more per row, complicates `SqlBulkCopy`, and the only spatial query needed (nearest unit) runs against the in-memory live state, not the history table. Revisit only if spatial history queries become a requirement.

### 8.3 `TrackSegment` — the reporting surface

Written when a leg or session closes: `UnitId`, `SessionId`, `FromSiteId`, `ToSiteId`, `StartUtc`, `EndUtc`, `DistanceM`, `DurationSec`, `MaxSpeedKph`, `AvgSpeedKph`, `PointCount`, `AnchorScanCount`, `AdherenceScore`, `Flags`.

**Every report, KPI query, dashboard and export reads `TrackSegment`. Nothing reads `TrackPoint` except replay and evidentiary export.** Enforced structurally: `TrackPoint` is exposed only through a narrow provider interface, so an accidental `.Include()` cannot reach it.

### 8.4 Timestamp model

Adopted from `PcarVisitHistory` (§1.4): store **device UTC**, **server UTC**, and the timezone context. Display in site-local. Flag clock skew beyond a configured threshold rather than silently trusting either clock. Across 827 sites in multiple states, this is not optional.

### 8.5 Deployment scripts

Following the existing `DbScript/` convention:

```
DbScript/402_Create_Tracking_Schema.sql          -- tables, indexes, partition function
DbScript/403_Create_Tracking_Enrolment_Seed.sql  -- enrolment rows, no units enabled
DbScript/404_Rollback_Tracking_Schema.sql        -- uninstall (§17)
```

Every script is **idempotent** (`IF NOT EXISTS` guards) and **additive** — no `ALTER` against an existing table appears anywhere in the feature pack.

---

## 9. Required APIs

All under `api/tracking`. All authenticated. **No existing endpoint changes signature or behaviour.**

| Method | Route | Caller | Purpose |
|---|---|---|---|
| `POST` | `/api/tracking/positions` | Mobile | Batch ingest; **response carries desired mode + policy** |
| `POST` | `/api/tracking/session/start` | Mobile | Open a patrol session |
| `POST` | `/api/tracking/session/end` | Mobile | Close; triggers segment roll-up |
| `GET` | `/api/tracking/mode/{unitId}` | Mobile | Fast-path poll after silent push |
| `POST` | `/api/tracking/mode/ack` | Mobile | Confirm mode applied |
| `GET` | `/api/tracking/policy` | Mobile | Server-pushed sampling thresholds |
| `GET` | `/api/tracking/live` | Control room | Snapshot of units in operator scope |
| `POST` | `/api/tracking/command` | Control room | **"Track Vehicle Live"** — TTL-bounded, audited |
| `DELETE` | `/api/tracking/command/{id}` | Control room | End Live Mode |
| `GET` | `/api/tracking/history/{unitId}` | Control room | Replay points for a window — **audited** |
| `GET` | `/api/tracking/segments` | Reporting | Roll-ups for KPI/analytics |

### 9.1 Ingest contract

```jsonc
POST /api/tracking/positions
{
  "unitId": 42,                            // ClientSiteSmartWand.Id
  "sessionId": "8f3c…",
  "deviceUtc": "2026-08-07T04:12:00Z",
  "commandSeqSeen": 17,                    // last mode command the app applied
  "points": [
    { "seq": 1181, "utc": "2026-08-07T04:11:58Z",
      "lat": -33.865143, "lon": 151.209900,
      "accuracyM": 8, "speedKph": 47, "headingDeg": 118,
      "batteryPct": 63, "isMock": false,
      "source": "transit" },
    { "seq": 1182, "utc": "2026-08-07T04:12:00Z",
      "lat": -33.865900, "lon": 151.210400,
      "accuracyM": 6, "source": "nfcAnchor", "tagUid": "04A2…" }
  ]
}

200 OK
{
  "accepted": 2, "rejected": 0,
  "desiredMode": "Live",                   // ← authoritative mode delivery (§5.3)
  "commandSeq": 18,
  "commandTtlSeconds": 900,
  "policy": { "transitSteadySec": 10, "stationarySec": 60, "distanceFilterM": 25 },
  "serverUtc": "2026-08-07T04:12:01Z"      // ← clock-skew reconciliation
}
```

Idempotent on `(unitId, sessionId, seq)` — a retried batch is safe by construction, which is what makes aggressive client-side retry acceptable.

### 9.2 Device-agnostic ingest

`IPositionSource` is defined in Phase 1 and implemented only by the phone. Phase 3 adds telematics and third-party fleet-API adapters behind the same interface. **Designing the seam now costs one interface; retrofitting it later costs a rewrite of the ingest path.** A customer who already runs Geotab or Samsara then becomes an integration rather than a competitor.

---

## 10. Required SignalR Changes

### 10.1 Existing hubs — untouched

`UpdateHub` and `MobileAppSignalRHub` are not modified. `UpdateHub`'s `Clients.All` broadcast is left as-is (out of scope) but is explicitly **not** the template.

### 10.2 New hub

```csharp
[Authorize]                                    // ← unlike the existing hubs
public class PatrolTrackingHub : Hub
{
    public async Task JoinControlRoom(int controlRoomId) { … }   // scope-checked
}
```

Mapped as `app.MapHub<PatrolTrackingHub>("/trackingHub").RequireAuthorization()` inside `MapCityWatchTracking()`.

Group key = **control room scope**, following `MobileAppSignalRHub`'s `ClientSiteId` grouping pattern. Until a `ControlRoom` entity exists (Phase 2), the group key is a derived scope hash over the operator's accessible sites — so the correct behaviour ships in Phase 1 and the entity formalises it later.

### 10.3 Broadcast model

**One `IHostedService` ticking at 1 Hz**, computing changed units per scope and sending **one frame per group** — not one message per position.

```
Naive per-position:   O operators × V vehicles  messages/interval
Tick-based diff:      O messages/interval, each carrying ≤ V small deltas
```

At 10 operators × 100 vehicles: **200 msg/s naive vs 10 msg/s tick-based.** At 1,000 vehicles the naive design would produce ~16,700 msg/s.

Frame payload per changed unit: `unitId, lat, lon, heading, speed, ageFlag, mode` ≈ 40 bytes. Serialised with a `System.Text.Json` source-generated context and short property names.

**Devices do not connect to SignalR.** §7.3 explains why: battery, cellular reconnect storms, and the fact that the ingest response already provides a reliable command channel.

### 10.4 Degradation

If the hub connection drops, the control room **says so in the header** and falls back to polling the existing `OnGetChangeToken` pattern at a longer interval. **A frozen map that looks healthy is worse than a map that admits it is stale.**

---

## 11. UI/UX Design

### 11.1 Layout — additive to the existing Control Room

```
┌────────────────────────────────────────────────────────────────────────┐
│ CityWatch Control  │ Sydney Control Room ▾ │ 18 units · 3 exceptions  ⚠ │
├──────────────┬─────────────────────────────────────┬───────────────────┤
│ UNITS   [new]│                                     │ EXCEPTIONS  (3)   │
│ ▸ search     │                                     │ ⚠ PC-04 off-route │
│              │        EXISTING LIVE MAP            │ ⚠ PC-11 stale 6m  │
│ ● PC-04 Live │   (existing clusters + site markers │ ⚠ PC-07 speed 92  │
│ ● PC-07 Trans│    + NEW car layer / trails)        ├───────────────────┤
│ ● PC-11 Stale│                                     │ SELECTED UNIT     │
│ ○ PC-19 Norm │                                     │ PC-04 · ABC-123   │
│              │                                     │ J. Smith          │
│ [filters]    │                                     │ 47 km/h · NE      │
│ existing ▾   │                                     │ Fix 4 s ago       │
│ + mode    ▾  │                                     │ Last NFC: Westf.  │
│ + tracked ▾  │                                     │ Next: Chatswood   │
│              │                                     │ ETA 6 min         │
│              │                                     │ 7/12 stops · 94%  │
│              │                                     │ [◉ Track Live]    │
│              │                                     │ [▶ Replay]        │
├──────────────┴─────────────────────────────────────┴───────────────────┤
│ ◀◀ ◀ ▶ ▶▶   ●──────────────────  14:32  [1×][4×][16×]   LIVE ⟳   [new] │
└────────────────────────────────────────────────────────────────────────┘
```

Everything marked `[new]` is rendered by `controlRoomTracking.js` into containers added under the flag. **The existing map, clustering, filters and site markers are untouched.**

### 11.2 The timeline scrubber

**Live and replay are the same view at different times, not two screens.** Dragging left enters replay; the LIVE button returns. Operators learn one interface, and the transition between "what is happening" and "what happened" costs no context switch — which matters when reconstructing an incident under pressure.

### 11.3 Design rules

1. **Colour means urgency, and nothing else.** Extend the existing `COL` palette (`ok`/`warn`/`alarm`/`off`/`accent`). Mode is conveyed by icon and label, never by a second colour scale — an operator at 3 a.m. cannot decode two.
2. **Never render a stale position as current.** < 30 s solid · 30 s–2 min soft pulse · 2–5 min hollow with age badge · > 5 min greyed and promoted to the exception list.
3. **Interpolate client-side.** Animate between 1 Hz frames using the CSS transition already at `ControlRoomMap.cshtml:380`. Smoothness is a rendering concern, not a sampling concern — this is what lets Transit mode sample at 10 s and still look alive.
4. **"Track Live" is a two-state control** — `[◉ Track Live]` → `[◉ Live 14:59 ⏹]` with a visible countdown. The operator always knows it is on and when it ends.
5. **Never claim an unconfirmed state.** Between command and acknowledgement the UI reads *"Live requested…"* (§5.3).
6. **Exceptions are the primary surface above ~50 units.** Nobody watches 100 dots; they watch the exception list and use the map to investigate.
7. **Preserve operator context absolutely.** No re-centre, no re-zoom, no popup close on refresh. The existing code respects this — hold the line.
8. **Duress pre-empts everything.** Full-width banner, audible alert, one click to centre and lock.
9. **Everything within 2 clicks of the map.**
10. **Degrade honestly** (§10.4).

### 11.4 Administration

One new page, `/Admin/TrackingEnrolment`, in `CityWatch.Web`: enrol units, record consent, view enrolment state per customer. **No existing admin page is modified.**

---

## 12. Performance Analysis

### 12.1 Assumptions

Adaptive sampling (§5.2) averaging ~1 point / 12 s while in Transit; 12-hour shifts; **Normal Patrol contributes only per-scan anchors**; 60 s batching (5 s in Live); ~80 bytes/row with index overhead; 1 Hz broadcast; 5–15 concurrent operators.

### 12.2 Projections

| Vehicles | Ingest req/s | Points/day | Rows/yr | Storage/yr | Broadcast msg/s | Assessment |
|---|---|---|---|---|---|---|
| **50** | 0.8 | 180 K | 66 M | ~5 GB | 3 | Single instance. Comfortable. |
| **100** | 1.7 | 360 K | 131 M | ~10 GB | 5 | Single instance + Redis live state. |
| **500** | 8.3 | 1.8 M | 657 M | ~53 GB | 10 | **Partitioning mandatory.** Multi-instance + Redis backplane + leader election. |
| **1,000** | 16.7 | 3.6 M | 1.31 B | ~105 GB | 15 | Dedicated worker instance, aggressive roll-up, archive tier. Feasible as a deliberate programme. |

**Current fleet: 20 vehicles.** The 100 column is the realistic two-year target. The 1,000 column shapes the **schema** (partitioning, roll-ups, no FKs) so it never becomes a rewrite — it should not shape Phase 1 **spend**.

**Normal-Patrol-only deployment:** a customer running no Transit tracking generates roughly *one point per NFC scan*. At 20 vehicles × 40 scans/shift that is 800 points/day — **0.02% of the Transit-mode volume.** This is a genuinely free tier, and it is the honest answer to "will this slow the system down": in Normal mode, no.

### 12.3 Impact on the existing platform

| Dimension | Impact | Why |
|---|---|---|
| Existing queries | **None** | Separate tables, no FKs, no shared indexes |
| `CityWatchDbContext` model build | **None** | Separate `TrackingDbContext` |
| Existing API latency | **None** | Separate controller; no shared middleware added |
| NFC scan path | **None** | Read-only tap (§4.2); scan code never entered |
| Control Room load time | **+~15 KB JS** when enabled, **0** when disabled | Flag-guarded script tags |
| Database size | +5–10 GB/yr at 100 vehicles | Separate partitioned tables |
| Database CPU | Bulk inserts on a separate table | Never joined to `GuardLogs` (2.36 M rows) |
| Web server memory | ~200 bytes/unit live state ⇒ 1,000 units ≈ 200 KB | Negligible |
| Mobile battery (Normal) | Within noise | One extra fix per scan |
| Mobile battery (Transit) | Target ≤ 10%/hr | Adaptive sampling; measured before pilot |
| Mobile data | 0.5–1 MB/vehicle/shift | Batched and compressed |

### 12.4 Optimisation priorities

1. **Normal Patrol as default** — the largest saving is not sampling less, it is not sampling at all
2. Adaptive sampling in Transit (§5.2) — largest device-side win
3. Tick-based diff broadcast (§10.3) — largest server-side win
4. Client-side interpolation — decouples smoothness from frequency
5. Batched `SqlBulkCopy` — turns a write problem into a non-problem
6. `TrackSegment` roll-up — keeps reporting permanently fast
7. Partitioning + retention — keeps the hot table small forever
8. Viewport culling — deferred until measured

### 12.5 Retention

Raw points: 90 days hot, 12 months compressed archive partition, then purge. Segments: 7 years (small, and the evidentiary record). **Confirm the raw-point figure against contractual and insurance requirements before committing** — it is a legal parameter, not a technical one.

---

## 13. Security Review

### 13.1 Phase 0 prerequisites (blocking)

Location data cannot be added to the current API surface. Before Phase 1:

| ID | Fix |
|---|---|
| A1 | `FallbackPolicy` requiring authentication; `[AllowAnonymous]` applied deliberately where needed |
| A2 | `LoginController` → `[HttpPost]`, credentials in body |
| A3 | Rotate the Azure Storage key hardcoded at `CityWatch.Data/Models/DailyPatrolData.cs:280`; move to Key Vault |
| A4 | `.RequireAuthorization()` on both existing hub mappings |
| A5 | Self-host Leaflet and markercluster (currently `unpkg.com`) |
| — | Token-based auth (JWT + refresh) for mobile — cookies are the wrong primitive for a native client |
| — | Add a CI workflow: build, test, secret scan. **CityWatch has none.** The santhomPay `ci.yml` gitleaks job would have caught A3. |

**An unauthenticated *ingest* endpoint is worse than an unauthenticated read endpoint:** anyone could write false positions into the evidentiary record, which destroys the product's entire value proposition. Verified Proof of Patrol that anyone can forge is not a product.

### 13.1.1 Phase 0 cannot be a single step — staged auth migration *(v2.0)*

**Verified fact:** `C4iSytemsMobApp` contains **no `AuthenticationHeaderValue` anywhere in the codebase.** `GuardApiServices` and `SyncApiService` issue plain `GetAsync`/`PostAsJsonAsync` calls with no `Authorization` header, no bearer token, no API key. The deployed app authenticates **only** by calling `LoginController.GetUserLogin`, which issues a *cookie* the native `HttpClient` calls do not carry.

**Consequence:** applying a global `FallbackPolicy` — the textbook fix for A1 — would return 401 to every API call from every deployed device the moment it shipped. Guards across 827 sites would lose NFC scanning, logbook entry, IR submission and offline sync **simultaneously and without warning**. This is a platform-wide outage, not a security improvement.

**The security defect is real and must be fixed. It cannot be fixed in one deployment.** Required sequence:

| Stage | Action | Breaks field devices? |
|---|---|---|
| **0a** | Add JWT issuance (`POST /api/auth/token`) + refresh. Add `[Authorize]` **only** to *new* endpoints. Existing endpoints unchanged. | No — purely additive |
| **0b** | Mobile release: attach `Authorization: Bearer` to every call via a `DelegatingHandler`. Server **accepts but does not require** it. | No — server still permits anonymous |
| **0c** | Measure adoption. `AppUpdateService` already reports `AppInfo.Current.VersionString` to `AppUpgrade/GetLatestAppVersion` — extend that to log version per device so adoption is a number, not a guess. | No |
| **0d** | Convert the existing in-app upgrade prompt to a **hard gate** below the minimum version, forcing the tail. | Yes, by design — with a clear message |
| **0e** | Enforce per controller, least-used first, monitoring 401 rates between each. | No, if 0c/0d done |
| **0f** | Apply the global `FallbackPolicy`. **Only now is A1 closed.** | No — nothing anonymous remains |

**A2 (password in query string) is independent and can be fixed immediately** — add `[HttpPost]` accepting a body **alongside** the existing signature, migrate the app in the 0b release, then remove the query-string overload at 0e. Same additive-then-subtract discipline.

**A3 (committed Azure Storage key) has no such constraint — rotate it now.** It is a live exposure and rotating it breaks nothing that is not already broken.

**A5, CI, and the mobile manifest/plist fixes are all independent and immediate.**

**Business decision required (§19, D15):** how aggressively to force the mobile upgrade at stage 0d. A hard gate closes the hole faster but strands any guard who cannot update mid-shift. A soft prompt is safer operationally and leaves the API anonymous for longer. **This is a risk-appetite call for the CTO, not an engineering default.**

**Effect on the roadmap:** Phase 0 becomes ~4–6 weeks (not 2–3), and stages 0a–0c can run **in parallel** with Phase 1 module construction, because the tracking module's own endpoints are new and can require authentication from their first line. **Tracking is never gated on the legacy endpoints being secured — only on tracking's own endpoints being secured, which they are by construction.**

### 13.2 Authorisation without rebuilding auth

The platform's entire role model is `User.IsAdmin` (§1.8). Rebuilding auth is out of scope. **The module therefore ships its own permission table**, keyed to the existing user identity:

```
TrackingPermission (UserId, ScopeType, ScopeId, PermissionLevel)
   ScopeType:       Global | ClientType (customer) | ClientSite
   PermissionLevel: None | ViewLive | ViewHistory | CommandLive | Admin
```

This adds a capability layer **on top of** the existing identity without touching `User`, `UserAuthenticationService` or the cookie flow. `IsAdmin` maps to `Global/Admin` by default, so existing administrators work on day one and nobody else gains access implicitly.

**Default deny.** A user with no `TrackingPermission` row sees no tracking UI and receives 403 from every tracking endpoint.

### 13.3 Customer isolation

Scope resolves `SmartWandId → ClientSiteSmartWand.ClientSiteId → ClientSite.TypeId` (the customer). Every read endpoint filters by the caller's permitted scope **in the query**, never in the view. Every SignalR frame is scoped by group membership. **A client-portal user (Phase 3) is scoped to their own sites only, and to an active visit window if time-boxed viewing is enabled.**

### 13.4 Audit

`TrackingAccessAudit` records every historical-location read and every Live command: who, which unit, which window, when, from where, why. Follows the existing `FileDownloadAuditLogs` / `KeyVehicleLogAuditHistory` pattern.

**In a workplace-surveillance context, proving *who looked at an officer's movements* is as important as the data itself** — it will be the first question in any dispute. A `Track Live` command is a deliberate act of close surveillance of a named person and is audited as such.

### 13.5 Privacy by design

- **No active patrol session ⇒ no tracking, at all.** Enforced server-side (ingest rejects), not just in the app.
- **Consent recorded before enrolment.** `TrackingUnitEnrolment.ConsentRecordedUtc` is `NOT NULL` before a unit can be enabled — the guarantee is structural, not procedural.
- **Always-visible indicator** — iOS `ShowsBackgroundLocationIndicator` + Android foreground-service notification + in-app banner.
- **Officers see their own history**, in the same detail the control room does.
- **Hard stop at session end.** No off-shift tracking under any configuration. Make it impossible, not merely disabled.
- **Retention enforced technically** (§12.5), not by policy.
- **Documented break-glass path** for out-of-scope access, requiring justification and raising an alert.

### 13.6 GPS spoofing

Layered — no single control suffices:

1. **Device signal** — Android `IsFromMockProvider`; captured in `Flags`, never silently dropped. iOS has no equivalent, so this cannot stand alone.
2. **Server plausibility** — implied speed, altitude discontinuity, suspiciously perfect accuracy. **Flag, never drop: a flagged anomaly is evidence; a dropped point is a gap you cannot explain later.**
3. **NFC corroboration — the strong control.** A scan at a fixed physical tag is very hard to fake remotely. **Where GPS and NFC disagree, NFC wins and the discrepancy is itself an alert.** The NFC-anchored architecture (§4.2) makes this structural rather than an add-on.
4. **Device attestation** (Play Integrity / DeviceCheck) — Phase 3+.

### 13.7 Transport

TLS 1.2+ enforced. **Remove or narrowly scope `android:usesCleartextTraffic="true"` in the current manifest before shipping location.** TDE at rest. Coordinates are not column-encrypted — it would break every query for little gain given TDE plus §13.3 scoping — but the officer→unit→session **assignment** is access-controlled, because that is what turns coordinates into personal information.

### 13.8 Compliance (Australia) — legal review required

*Flags the issues; not legal advice.*

- **Privacy Act 1988 (Cth) / APPs** — location tied to an identified officer is personal information. APP 1, 3, 5, 6 and 11 all engage.
- **State workplace-surveillance law is the sharper constraint.** NSW's *Workplace Surveillance Act 2005* requires prior written notice of tracking surveillance — commonly understood as **14 days** — with covert tracking generally requiring a court order. Victoria, WA, SA and the NT have separate Surveillance Devices Acts with materially different tests. `ClientSite.State` exists, so per-state configuration is feasible. **Confirm current requirements for every state of operation.**
- **Consequence:** the notice-and-consent workflow is a **Phase 1 deliverable on the critical path to first revenue**, because a customer cannot lawfully enable tracking without it.
- **Industrial relations** — enterprise agreements may impose consultation obligations, and parts of the security workforce are unionised. **The most likely cause of failure is officer resistance, not technology.** Design for consent and transparency and the objection largely dissolves.

---

## 14. AI Readiness

No AI is implemented. The architecture makes it possible later **without a data migration** — which is the whole requirement.

| Future capability | What makes it feasible | Present from |
|---|---|---|
| Smart Dispatch | Live state store already holds position + mode + status per unit, queryable in O(units) | Phase 1 |
| Route Optimisation | `TrackSegment` gives actual travel times per site pair; `PcarRouteDetails` gives the plan | Phase 2 |
| Patrol Prediction | Complete `TrackSegment` history keyed by unit, site, time-of-day, day-of-week | Phase 2 |
| Incident Prediction | `TrackPoint` joins to `IncidentReport` on site + time — both already keyed to `ClientSiteId` | Phase 2 |
| Patrol Heat Maps | Raw points are geospatial and retained; aggregation is a query | Phase 1 |
| SLA Analysis | `TrackSegment.AdherenceScore` + `PcarRouteDetails` time windows | Phase 3 |
| AI Reports | Segments + anchors + incidents form a coherent narrative timeline | Phase 3 |

**Four architectural decisions that make this true, all free if taken now:**

1. **Append-only, never mutated.** History is reproducible; a model trained on last month's data can be re-derived exactly. Mutable history makes ML results unexplainable.
2. **`SourceType` and `Flags` on every point.** A future model can exclude backfilled, low-accuracy or flagged points. **Data quality that is not recorded at write time cannot be recovered later.**
3. **Both clocks stored.** Skew is measurable rather than baked in as unexplained noise.
4. **Segments are computed, not hand-entered.** Features are consistent across the entire corpus.

**Explicitly deferred:** no ML libraries, no feature store, no inference endpoints, no schema concessions to a hypothetical model. The commitment is *"this data will be usable"*, not *"this system is an AI platform."*

---

## 15. Deployment Strategy

### 15.1 Independent deployability

Each phase deploys alone and leaves the system fully working.

| Artefact | Deploy independently? | Notes |
|---|---|---|
| `CityWatch.Tracking.dll` | Yes | New assembly; no existing assembly changes behaviour |
| SQL scripts | Yes | Additive only; safe to run ahead of code |
| `CityWatch.Web` | Yes | 2-line change; harmless with the flag off |
| `CityWatch.RadioCheck` | Yes | Map degrades to today's behaviour with the flag off |
| Mobile app | Yes | **Must tolerate a server without tracking endpoints** — 404 is a normal, silent condition |

**Ordering rule:** SQL → backend → control room → mobile. Each step is safe to stop at. **The mobile release ships last and must work against both a tracking-enabled and a tracking-disabled server**, because app-store rollout is gradual and not under our control.

### 15.2 Sequence

```
1. DB scripts (402, 403) on production        — additive; zero impact; no downtime
2. Deploy CityWatch.Web  with Tracking:Enabled = false
3. Deploy CityWatch.RadioCheck with flag false
   → verify: full regression of existing functionality, flag OFF
4. Flip Tracking:Enabled = true in a staging/pilot environment
5. Enrol 2–3 pilot units; verify ingest, live map, replay
6. Mobile release to internal test track
7. Pilot: 20 vehicles, 2 weeks — battery, coverage, operator workflow
8. Progressive enrolment by customer
```

**Step 3 is the important one.** The code ships to production *disabled* and is regression-tested in place before anything is turned on. Deployment risk and feature risk are separated — if step 3 shows a regression, nothing has been enabled and the cause is unambiguous.

### 15.3 Environment configuration

```jsonc
"Tracking": {
  "Enabled": false,
  "IsLeaderInstance": true,
  "MaxConcurrentLiveUnits": 10,
  "LiveModeTtlSeconds": 900,
  "IngestRateLimitPerUnitPerMinute": 30,
  "RetentionDays": { "Points": 90, "Archive": 365, "Segments": 2555 },
  "Policy": { "transitSteadySec": 10, "stationarySec": 60, "distanceFilterM": 25 }
}
```

### 15.4 Verification gates

| Gate | Criterion |
|---|---|
| G1 | Full regression suite passes with `Enabled = false` — **no behavioural delta** |
| G2 | Existing Control Room renders identically with the flag off (visual diff) |
| G3 | NFC scan workflow unchanged, tracking on **and** off |
| G4 | Ingest sustains 3× projected peak without database write-queue growth |
| G5 | Battery ≤ 10%/hr in Transit on real mid-range Android, screen off |
| G6 | Live Mode command→ack ≤ 5 s on the push path, ≤ 60 s on the response path |
| G7 | Rollback rehearsed end-to-end in staging (§17) |
| G8 | Every tracking endpoint returns 403 without a `TrackingPermission` row |

**No phase is complete until its gates pass on evidence, not assertion.**

---

## 16. Risks

| # | Risk | Sev | Mitigation |
|---|---|---|---|
| R1 | **Unauthenticated API surface** (A1/A2/A4) | **Critical** | Phase 0 gate. No tracking work starts until closed. §13.1 |
| R2 | **Officer/union resistance** — most likely cause of outright failure | **High** | Consent workflow, visible indicator, officer self-access, session-bounded tracking, hard stop at shift end. Position as safety (duress, nearest-unit), not surveillance. Normal Patrol mode makes a low-intrusion starting posture possible. |
| R3 | Battery drain → app disabled → feature dies quietly | **High** | Normal Patrol default; adaptive Transit sampling; TTL-bounded Live; server-tunable policy; measured ≤10%/hr gate (G5) |
| R4 | Mobile OS restrictions kill background location silently | **High** | Foreground service; manifest/plist fixes; **server-side detection of a unit gone quiet while nominally on shift — treated as an exception, not a silent gap** |
| R5 | **Regression in existing CityWatch** | **High** | Separate project, separate `DbContext`, separate tables, no FKs, flag off by default, 11-line existing-code delta, G1–G3 gates, rehearsed rollback |
| R6 | Live Mode command not delivered | Medium | Response-carried mode is authoritative; push is an optimisation (§5.3); UI never claims unconfirmed state |
| R7 | Store rejection for background location | Medium | Written justification, demo account, submit early, budget one rejection round |
| R8 | GPS inaccuracy — urban canyons, underground car parks | Medium | Accuracy threshold + `Flags`; **NFC anchors as ground truth**; show accuracy radius, not a false-precision dot |
| R9 | Poor coverage — rural and after-hours patrols | Medium | Offline-first queue; backfill flagged so replay is honest; "last seen" always carries age |
| R10 | Storage growth | Medium | Partitioning, roll-up, retention. ~10 GB/yr at 100 vehicles is a planning item, not a threat. |
| R11 | Broadcast doesn't scale | Medium | Tick-based diff + scoped groups (§10.3). Costs nothing now; a rewrite later. |
| R12 | Multi-instance job duplication | Medium | Leader flag in Phase 1, Redis lock in Phase 2. **Decide before the first multi-instance deploy.** |
| R13 | Clock skew across 827 sites | Medium | Both clocks stored; `serverUtc` in every response; skew flagged |
| R14 | **No CI on CityWatch** | Medium | Phase 0 adds build/test/secret-scan. Adding a real-time subsystem to a repo with no automated gate is the compounding risk here. |
| R15 | Legal exposure from non-compliant deployment | **High** | Legal review before pilot; consent workflow as a Phase 1 feature; per-state configuration via `ClientSite.State` |
| R16 | Competing with the customer's existing telematics | Medium | `IPositionSource` designed Phase 1, implemented Phase 3 — turns a competitor into a data source |
| R17 | Scope creep into dashcam/video | Medium | **Explicitly out of scope.** Multiplies bandwidth, storage, privacy exposure and support by an order of magnitude, and it is a hardware business. Partner, do not build. |
| R18 | Tracking backlog delays NFC offline replay | Low | Separate endpoint and separate queue (§6.4); tracking sync runs last and is exception-isolated |

---

## 17. Rollback Strategy

Four levels, each faster than the one below it. **Level 1 is available at all times and takes seconds.**

### Level 1 — Disable the feature (seconds, no deployment)

```jsonc
"Tracking": { "Enabled": false }
```
Recycle the app pool. Hosted services stop, endpoints unmap, the hub route disappears, the Control Room renders exactly as it does today. **Mobile apps receive 404 on ingest, treat it as a normal offline condition, and buffer locally** — no crash, no error dialog, no user-visible change. Data is retained; re-enabling resumes.

**This is the primary rollback and it requires no build, no deploy and no DBA.**

### Level 2 — Disable per customer or unit (seconds, no restart)

```sql
UPDATE dbo.TrackingUnitEnrolment SET IsEnabled = 0 WHERE SmartWandId IN (…);
```
Takes effect on the affected units' next batch. Everyone else is unaffected. **This is the right response to a customer-specific problem** — a single complaint never requires a platform-wide action.

### Level 3 — Remove the code (one deployment)

Revert the 11 existing lines and redeploy `CityWatch.Web` and `CityWatch.RadioCheck` without the `CityWatch.Tracking` reference. **Tables remain and are simply unused** — no data loss, no schema change, and re-installation is a redeploy.

### Level 4 — Remove the schema (deliberate, last resort)

`DbScript/404_Rollback_Tracking_Schema.sql` drops the tracking tables. Clean and complete **because nothing else references them** — no FKs point in or out (§8.2), no existing table gained a column, no existing index changed. **This is only safe because of the no-FK, no-alter discipline**, which is why that discipline is not negotiable.

### 17.1 Mobile rollback

The mobile app is the slowest component to roll back (store review). Therefore:

- **The app must function correctly against a server with tracking fully disabled** — this is a hard requirement (G-mobile), verified before submission
- Tracking is **server-gated**: the app tracks only when the server confirms the unit is enrolled. Disabling server-side stops tracking on already-installed apps immediately.
- **There is no scenario requiring an emergency mobile release to stop tracking.** That property is designed in, and it is the single most important rollback characteristic of the whole feature.

### 17.2 Rehearsal

Gate G7: rollback is rehearsed end-to-end in staging **before** production enablement — Level 1 and Level 2 with live traffic, Levels 3 and 4 as a scripted exercise. A rollback plan that has never been executed is a hypothesis.

---

## 18. Phase Plan

Estimates assume one backend engineer, one mobile engineer, shared front-end/QA. Planning ranges, not commitments.

### Phase 0 — Security & Foundation · ~2–3 weeks · blocking
A1–A5, JWT for mobile, CI workflow, Android manifest + iOS plist fixes.
**Independently deployable. Every item has standalone value even if tracking never ships.**

### Phase 1 — Tracking MVP · ~6–8 weeks
`CityWatch.Tracking` project + `TrackingDbContext`; SQL scripts 402–404; ingest API; **Normal Patrol + Transit modes**; mobile `TrackingService` + foreground service + offline queue; session lifecycle; **consent & enrolment workflow**; in-memory live state; `PatrolTrackingHub` with scoped groups + 1 Hz ticker; car layer with heading, speed, fix-age degradation; `IPositionSource` interface defined; flag off by default.
**Exit:** G1–G5, G8. 20-vehicle pilot.

### Phase 2 — Live Control Room · ~6–8 weeks
**Live Mode** (command channel, TTL, ack, concurrency cap); **Duress Mode**; breadcrumb trails; **patrol replay + unified timeline**; geofences with enter/exit/dwell; exception engine and panel; unit status; dispatch with acknowledgement; nearest-unit (haversine); Redis live state + backplane + leader election; push notifications.
**Exit:** G6, plus operator workflow validated with real control-room staff.

### Phase 3 — Verified Proof of Patrol · ~8–10 weeks *(the commercial phase)*
**NFC↔GPS reconciliation and verification**; route adherence scoring; **Proof of Patrol export (per site, per period)**; client-facing portal view; coverage heat maps; telematics adapters implemented; road-snapping; travel-time nearest-unit; device attestation; KPI integration; archive tier.
**This is where the feature starts paying for itself.**

### Phase 4 — AI Patrol Intelligence · ~10–12 weeks · deferred
Smart dispatch, route optimisation, patrol/incident prediction, SLA analysis, AI reports.
**Requires 6–12 months of accumulated telemetry. Do not commit to dates.** Research track with a revenue-bearing option, not a delivery commitment.

**Phase 0→3: ~22–29 weeks.** Phase 0→1 (~8–11 weeks) delivers a demonstrable, pilotable system.

---

## 19. Approval Checklist

Decisions requiring sign-off before implementation begins:

| # | Decision | Recommendation |
|---|---|---|
| D1 | Separate `TrackingDbContext` rather than extending `CityWatchDbContext` | **Approve** — §3.3 |
| D2 | NFC anchors emitted by the mobile app, not by a server-side hook in the scan path | **Approve** — §4.2; zero risk to the NFC workflow |
| D3 | `ClientSiteSmartWand.Id` as the tracking unit key (no new unit entity) | **Approve** — §1.6 |
| D4 | Batched HTTPS ingest; devices do not hold SignalR connections | **Approve** — §7.3 |
| D5 | Live Mode delivered authoritatively on the ingest response, accelerated by silent push | **Approve** — §5.3 |
| D6 | Normal Patrol as the default mode (no continuous GPS unless in Transit/Live/Duress) | **Approve** — §5.1; makes the feature adoptable |
| D7 | No foreign keys on `TrackPoint` | **Approve** — §8.2; required for clean Level-4 rollback |
| D8 | Module-local `TrackingPermission` table rather than rebuilding platform auth | **Approve** — §13.2 |
| D9 | Phase 0 as a blocking gate | **Approve** — §13.1 |
| D10 | Consent workflow as a Phase 1 deliverable, not documentation | **Approve** — §13.8 |
| D11 | Leader-election approach: config flag (P1) → Redis lock (P2) | **Approve** — §7.4 |
| D12 | Retention: points 90d / archive 12m / segments 7y | **Confirm with legal & insurance** — §12.5 |
| D13 | Dashcam/video explicitly out of scope | **Approve** — R17 |
| D14 | Leaflet retained; MapLibre reviewed at Phase 3 on a measured marker-count trigger | **Approve** |
| **D15** | **Mobile upgrade enforcement at Phase 0d — hard gate vs soft prompt** | **⚠ BUSINESS DECISION REQUIRED** — §13.1.1. Determines how long the API stays anonymous. |
| **D16** | In-process event bus with a no-op default publisher; 6 publish sites in existing code | **Approve** — §20.4. Raises the existing-code delta from 11 to ~17 lines and is what makes every future module free. |
| **D17** | Events published *after* the host workflow commits, never inside its transaction | **Approve** — §20.5. Non-negotiable: a subscriber must never be able to fail a patrol workflow. |

**Open items requiring input beyond engineering:**

- **Legal review** of state workplace-surveillance obligations (§13.8) — needed before pilot, not before design
- **CARTO basemap commercial terms** — needed before the feature is monetised
- **Retention periods** (D12) — a contractual and insurance parameter
- **Pilot customer selection** — ideally a customer with an existing GPS mandate in their contracts

---

---
---

# Part II — v2.0

---

## 20. Event-Driven Architecture

*Supersedes the direct-integration model described in §4.2. That model remains valid as a fallback if D16 is rejected.*

### 20.1 The tension this resolves — stated plainly

The directive asks for two things that pull against each other:

> *"Tracking should subscribe to these events rather than directly modifying existing workflows."*
> *"Never rewrite stable production code."*

**Something has to publish the events.** `NfcCheckpointScanned` cannot appear from nowhere — it must be raised where the NFC scan is handled, which is stable production code. There is no architecture in which a module learns about an event that nothing emits.

So the honest question is not *whether* to touch existing code, but **how small, how safe, and how many times.** Three options were considered:

| Option | Existing code touched | Future modules cost | Verdict |
|---|---|---|---|
| **A — Mobile-emitted anchors** (§4.2 v1.0) | 0 lines server-side | Each new module needs its own mobile change | Safest, but doesn't scale to Analytics/AI/Portal |
| **B — Direct calls into Tracking** from the NFC path | ~6 lines, coupled to Tracking | Each new module = another edit to the same production files | **Rejected** — this is the coupling the directive forbids |
| **C — Event bus with a no-op default** | **~6 lines, coupled to nothing** | **Zero** | **Recommended** |

**Option C is the only one where the *second* subscriber is free.** That is the entire point of the requirement: Analytics, AI, Notifications, Reporting and the Client Portal must each cost zero changes to `ScannerController`, `GuardLogDataProvider` or `MobileAppDataServices`. Option C pays a one-time cost of six lines to buy that permanently.

### 20.2 Module diagram (updated)

```
┌──────────────────────── EXISTING PLATFORM (unchanged behaviour) ────────────────────────┐
│                                                                                          │
│  CityWatch.Web            CityWatch.RadioCheck          CityWatch.Kpi                     │
│  ├─ ScannerController     ├─ ControlRoomMap             └─ reporting                      │
│  ├─ LoginController       └─ controlRoomMap.js                                            │
│  └─ GuardSecurityNumber…                                                                  │
│                    │                                                                      │
│  CityWatch.Data ───┴─ CityWatchDbContext (214 DbSets) · GuardLogDataProvider · …          │
│                                                                                           │
└───────────────────────────────────┬───────────────────────────────────────────────────────┘
                                    │ publishes (fire-and-forget, 6 sites, 1 line each)
                                    ▼
                    ┌───────────────────────────────────────────┐
                    │  CityWatch.Events   (new, ~150 lines)     │
                    │  ─────────────────────────────────────    │
                    │  IDomainEventPublisher                    │
                    │  ├─ NullDomainEventPublisher   ← DEFAULT  │  no subscribers ⇒ no-op
                    │  └─ ChannelDomainEventPublisher           │  bounded, async, isolated
                    │  IDomainEventHandler<TEvent>              │
                    │  Events: OfficerLoggedIn/Out,             │
                    │          PatrolStarted/Ended,             │
                    │          NfcCheckpointScanned,            │
                    │          PatrolVehicleExited,             │
                    │          DuressActivated,                 │
                    │          LiveTrackingRequested/Ended      │
                    └──────────────┬────────────────────────────┘
                                   │ subscribes
        ┌──────────────────────────┼──────────────────────────┬─────────────────┐
        ▼                          ▼                          ▼                 ▼
┌────────────────┐   ┌──────────────────────┐   ┌────────────────┐   ┌────────────────┐
│ CityWatch.     │   │  Analytics  (future) │   │ Notifications  │   │ Client Portal  │
│ Tracking       │   │                      │   │   (future)     │   │   (future)     │
│ ─────────────  │   └──────────────────────┘   └────────────────┘   └────────────────┘
│ TrackingDbCtx  │        ↑ each costs ZERO changes to existing code ↑
│ IngestService  │
│ LiveStateStore │
│ ModeCommandSvc │
│ GeofenceEval   │
│ SegmentBuilder │
│ 4 HostedSvcs   │
│ PatrolTracking │
│   Hub          │
│ TrackingCtrl   │
└────────────────┘
```

**`CityWatch.Events` depends on nothing.** No EF Core, no ASP.NET, no `CityWatch.Data`. It is contracts plus a channel. That is what lets `CityWatch.Data` reference it without acquiring a dependency on anything else — and why the dependency graph stays acyclic.

### 20.3 Event catalogue

| Event | Published from | Payload | Tracking's reaction |
|---|---|---|---|
| `OfficerLoggedIn` | Guard login path | GuardId, SmartWandId, ClientSiteId, UtcAt, DeviceId | Prepare session context |
| `OfficerLoggedOut` | Guard logout path | GuardId, SmartWandId, UtcAt | **Hard stop — close session, cease tracking** |
| `PatrolStarted` | PCAR visit `Started` | SessionId, UnitId, RouteId, UtcAt | Open `TrackingSession`; enter Normal |
| `PatrolEnded` | PCAR visit `Completed`/`Cancelled` | SessionId, UtcAt, Reason | Close session; trigger `SegmentBuilder` |
| `NfcCheckpointScanned` | NFC scan commit | SmartWandId, TagUid, ClientSiteId, GuardId, Gps, HitUtc, timezone block | Write `TrackPoint` with `SourceType=NfcAnchor` |
| `PatrolVehicleExited` | Geofence evaluator *(internal)* | UnitId, SiteId, UtcAt | Normal → Transit |
| `DuressActivated` | Duress raise path | GuardId, SmartWandId, ClientSiteId, Gps, UtcAt | **Duress Mode; unbatched 2 s** |
| `LiveTrackingRequested` | Tracking API *(internal)* | UnitId, OperatorUserId, TtlSeconds | Issue mode command; audit |
| `LiveTrackingEnded` | Tracking API / TTL *(internal)* | UnitId, Reason | Revert to Transit/Normal |

**Five are published from existing code** (`OfficerLoggedIn/Out`, `PatrolStarted/Ended`, `NfcCheckpointScanned`) plus `DuressActivated` — **six publish sites.** The remaining three originate inside Tracking itself and cost nothing.

Every event carries `EventId` (idempotency), `OccurredUtc` (device where applicable) and `PublishedUtc` (server) — reusing the `PcarVisitHistory` dual-clock discipline (§8.4).

### 20.4 The publish site — what a touched file actually looks like

```csharp
// existing production method — unchanged except for the final line
public async Task<(bool, bool, string, string, int, int)> CreateSmartWandScannerHitLogRecord(…)
{
    …                                    // ← entirely unchanged
    await _context.SaveChangesAsync();   // ← the existing commit

    _events.Publish(new NfcCheckpointScanned(…));   // ← THE ONE ADDED LINE
    return (…);
}
```

Constructor gains `IDomainEventPublisher _events`. **When no subscriber is registered, `_events` is `NullDomainEventPublisher` and `Publish` is an empty method body** — the JIT elides it. Tracking not installed, or `Tracking:Enabled = false`, means this line costs nothing measurable.

**Revised existing-code delta:**

| Category | v1.0 | v2.0 |
|---|---|---|
| Registration (`Program.cs` × 2) | 4 | 4 |
| Control Room | 7 | 7 |
| Mobile | 4 | 4 |
| **Event publish sites (6 × 1 line + ctor injection)** | — | **~6 + 6** |
| **Total** | **~15** | **~23 lines across 6 additional files** |

Twelve extra lines, none of which contain logic, all of which are the last statement in an already-committed method. In exchange, every future module integrates without touching production code again.

### 20.5 Safety contract — five properties, all mandatory

1. **Publish after commit, never inside a transaction.** The host workflow is durable before any subscriber runs. A subscriber cannot roll back a patrol scan. *(D17 — non-negotiable.)*
2. **Fire-and-forget onto a bounded channel.** `Publish` enqueues and returns; it never awaits a handler. Publisher latency is a channel write.
3. **Subscriber exceptions are swallowed and logged, never propagated.** A crash in Tracking is invisible to the NFC workflow. This is enforced in the dispatcher, not left to handler authors.
4. **Bounded with a drop policy.** If the channel fills, oldest events are dropped and a counter increments. **Back-pressure must never reach the publisher** — an unbounded queue turns a slow subscriber into an out-of-memory crash of the whole application.
5. **No-op by default.** `NullDomainEventPublisher` is registered in `CityWatch.Data`'s own defaults. If the events package is present but nothing subscribes, behaviour is bit-identical to today.

**Property 3 is the one that makes this acceptable in production.** Without it, an event bus is a way to let a new module take down a working one.

### 20.6 Delivery semantics — stated honestly

This is an **in-process, at-most-once, non-durable** bus. Events are lost on process restart, and by design nothing critical depends on them.

**Why not an outbox or a broker?** Because durable delivery implies writing to the database inside the host transaction — which violates property 1 and puts the tracking module back on the critical path of the NFC workflow. The trade is deliberate:

- **What can be lost:** an anchor point, a session boundary, a mode transition — all reconstructible from the NFC records that *are* durable.
- **What can never be lost:** anything a customer paid for. Duress alerting keeps its existing direct path (§4.5); the event is an *additional* observer, never the mechanism.

**A durable outbox is a Phase 4 upgrade** if Analytics needs guaranteed delivery. The `IDomainEventPublisher` interface does not change when that happens — only the implementation does. **That is the actual benefit of the abstraction: the semantics can be strengthened later without touching a single publish site.**

### 20.7 Why not MediatR / MassTransit

- **MediatR** is request/response with synchronous in-process dispatch by default — it would run handlers inside the caller's stack, breaking properties 1–3 unless carefully wrapped. Wrapping it is more code than the ~150 lines this needs.
- **MassTransit / NServiceBus** bring a broker, durability and operational surface that 20 patrol vehicles do not justify, and add a runtime dependency to a `net7.0` platform with no CI.

**Recommendation: hand-rolled, ~150 lines, zero third-party dependencies.** Revisit at Phase 4 when durability is an actual requirement rather than an anticipated one.

---

## 21. Testing Strategy

### 21.1 The constraint that shapes everything

`CityWatch.Data.Tests` and `CityWatch.Common.Tests` exist, but **there is no CI**, and `dotnet build` fails on a pre-existing static-web-asset conflict (`chart.min.js` duplicated between `CityWatch.Web` and `CityWatch.RadioCheck` wwwroot) unless run with `-p:StaticWebAssetsEnabled=false`.

**Therefore Phase 0 adds CI before Phase 1 adds a real-time subsystem.** Shipping background services, a hub and a high-rate write path into a repository with no automated gate is the compounding risk (R14).

### 21.2 Test pyramid

| Level | Scope | Count (est.) | Runs |
|---|---|---|---|
| **Unit** | Sampling policy, mode state machine, plausibility checks, geofence maths, segment roll-up, event dispatcher | ~120 | Every build |
| **Contract** | Ingest DTO round-trip, idempotency on `(unit, session, seq)`, response mode delivery | ~25 | Every build |
| **Integration** | `TrackingDbContext` against LocalDB; bulk-write path; partition switch | ~30 | Every build |
| **Regression (existing platform)** | **§21.4 — the most important tier** | ~40 | Every build |
| **Load** | Ingest at 3× projected peak; broadcast fan-out | ~6 scenarios | Per milestone |
| **Device** | Battery, background survival, offline replay, permission flows | manual matrix | Per phase |

### 21.3 New project

```
CityWatch.Tracking.Tests/     xUnit, matching the existing test projects' conventions
```

**Deterministic time.** Every service takes an `ITimeProvider`; no `DateTime.Now` in testable code. The existing codebase uses `DateTime.Now` widely — the new module does not, and that boundary is a review gate, not a preference.

### 21.4 Regression tests — proving the platform is unchanged

This tier exists to make "we didn't break anything" a **test result rather than a claim.**

| # | Assertion |
|---|---|
| RT1 | With `Tracking:Enabled = false`, no tracking service is resolvable from the container |
| RT2 | With the flag off, `/api/tracking/*` returns 404 and `/trackingHub` does not exist |
| RT3 | `NullDomainEventPublisher` is the registered default when no subscriber exists |
| RT4 | `CreateSmartWandScannerHitLogRecord` returns identical results with the publisher present and absent |
| RT5 | A subscriber that **throws** does not affect the NFC scan's return value or committed rows |
| RT6 | A subscriber that **hangs** does not increase NFC scan latency (property 2) |
| RT7 | `SyncService.SyncAsync()` completes all six original syncs when tracking sync throws |
| RT8 | Mobile app functions fully against a server returning 404 for all tracking endpoints |
| RT9 | `ControlRoomMap` renders identically with the flag off (DOM snapshot) |
| RT10 | `controlRoomTracking.js` failing to load leaves the existing map fully functional |
| RT11 | `CityWatchDbContext` model builds to the same shape before and after the pack |
| RT12 | Rollback script 404 leaves zero tracking objects and zero broken references |

**RT5, RT6 and RT7 are the ones that matter.** They test the failure modes that would otherwise be discovered in production at 3 a.m.

### 21.5 Load testing

| Scenario | Target | Pass |
|---|---|---|
| L1 Ingest sustained | 50 req/s (3× the 1,000-vehicle projection) | p99 < 200 ms, write queue stable |
| L2 Ingest burst | 500 devices reconnecting after an outage | No dropped batches, no unbounded queue |
| L3 Broadcast fan-out | 15 operators × 1,000 units | ≤ 20 msg/s, frame < 50 KB |
| L4 Replay query | 12-hour window, one unit | < 2 s |
| L5 Segment query | 30 days, 100 units | < 3 s |
| L6 **Existing platform under tracking load** | KPI report generation while ingest runs at L1 | **No measurable regression** |

**L6 is the acceptance test for the whole "won't affect performance" claim.** Everything else measures the new module; L6 measures the promise.

### 21.6 Device matrix

Minimum: two Android OEMs known for aggressive battery management (Samsung, Xiaomi) plus one stock Android, and two iOS versions. **Emulators do not test background location.** Per device: 12-hour battery in Transit, background survival across doze/app-switch/reboot, offline replay of 5,000 points, both permission-grant paths, and Live Mode command latency.

### 21.7 Definition of done, per milestone

1. Builds with `-p:StaticWebAssetsEnabled=false`
2. All tests pass, including the RT tier
3. No new compiler warnings
4. Flag-off behaviour verified
5. ADR written for any decision not already in this document
6. Commit is small, focused, and independently revertible

---

## 22. Implementation Backlog & Milestones

Each milestone builds, tests, and is independently revertible. **No milestone leaves the system in a state that requires the next one to be safe.**

### Phase 0 — Security Hardening *(≈4–6 weeks, revised per §13.1.1)*

| M | Deliverable | Touches existing? | Risk |
|---|---|---|---|
| **0.1** | **Rotate the committed Azure Storage key**; move to configuration/Key Vault | 1 file | Low — do first |
| **0.2** | CI workflow: build, test, gitleaks, vulnerable-package scan | None | None |
| **0.3** | Self-host Leaflet + markercluster | 1 file | Low |
| **0.4** | Android manifest permissions + **iOS duplicate `UIBackgroundModes` fix** | 2 files | Low — required regardless |
| **0.5** | JWT issuance + refresh; `[Authorize]` on new endpoints only | Additive | Low |
| **0.6** | `[HttpPost]` login overload **alongside** the existing signature | Additive | Low |
| **0.7** | Mobile `DelegatingHandler` attaching bearer tokens; server accepts-not-requires | Mobile | Low |
| **0.8** | Version telemetry per device; adoption dashboard | Additive | Low |
| **0.9** | **⚠ D15 gate** — upgrade enforcement, then per-controller `[Authorize]`, then `FallbackPolicy` | **Yes** | **High — business decision** |

**0.1–0.8 can start immediately. 0.9 is blocked on D15.**

### Phase 1 — Tracking Foundation *(≈6–8 weeks; 1.1–1.4 parallel with 0.5–0.8)*

| M | Deliverable | Touches existing? |
|---|---|---|
| **1.1** | `CityWatch.Events` — contracts, `NullDomainEventPublisher`, channel dispatcher, tests | **None** |
| **1.2** | `CityWatch.Tracking` skeleton — options, DI extensions, flag-off registration | **None** |
| **1.3** | `TrackingDbContext` + entities + SQL scripts 402–404 | **None** |
| **1.4** | Ingest service — validate, dedupe, plausibility, channel, `PositionWriter` | **None** |
| **1.5** | `IPositionSource` contract; phone implementation | **None** |
| **1.6** | Wire `IDomainEventPublisher` into DI; **6 publish sites** | **~12 lines** |
| **1.7** | Tracking subscribes to NFC/patrol/login events → anchor points, sessions | None |
| **1.8** | Enrolment + **consent workflow**; `/Admin/TrackingEnrolment` | New page |
| **1.9** | Mobile: `TrackingService`, sampler, foreground service, local buffer | Mobile +1 line |
| **1.10** | `LiveStateStore` (in-memory) + `PatrolTrackingHub` + `BroadcastTicker` | 2 lines |
| **1.11** | `controlRoomTracking.js` — car layer, heading, staleness, interpolation | ~7 lines |
| **1.12** | **RT1–RT12 regression suite** | None |
| **1.13** | Load L1/L2/L6; device matrix; battery gate G5 | None |

### Phase 2 — Live Operations *(≈6–8 weeks)*
Mode command channel + silent push + ack + TTL + concurrency cap · Duress Mode · breadcrumbs · replay + unified timeline · geofences · exception engine · unit status · dispatch with acknowledgement · nearest-unit (haversine) · Redis live state + backplane + leader election.

### Phase 3 — Verified Proof of Patrol *(≈8–10 weeks)*
NFC↔GPS reconciliation · adherence scoring · Proof of Patrol export · client portal view · telematics adapters · road-snapping · travel-time nearest-unit · device attestation · KPI integration · archive tier.

### Phase 4 — Analytics *(≈6–8 weeks)*
Subscribes to the same events. **Zero changes to Tracking or the existing platform** — this is the milestone that proves §20 was worth it. Heat maps, compliance analytics, SLA reporting. Durable outbox upgrade if guaranteed delivery is required.

### Phase 5 — AI Patrol Intelligence *(deferred, ≈10–12 weeks)*
Requires 6–12 months of telemetry. **No dates committed.**

### 22.1 Commit discipline

One milestone per branch, PR to `master`, verified on `test.c4i-system.com`. Commit messages state the milestone and whether existing code was touched. **Any commit touching a file outside `CityWatch.Tracking` / `CityWatch.Events` / `CityWatch.Tracking.Tests` must say why in the message** — that constraint is what keeps the delta honest over months of work.

### 22.2 Architecture decision records

`docs/adr/NNNN-title.md`, following this document's numbering. D1–D17 are seeded from §19; new decisions get an ADR at the milestone that makes them.

---

*Prepared for architectural review. §13.1.1 (D15) requires a business decision before Phase 0d. All other milestones are approved to proceed on the architecture described here.*
