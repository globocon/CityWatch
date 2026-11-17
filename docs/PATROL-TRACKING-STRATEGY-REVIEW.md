# Real-Time Patrol Vehicle Tracking — Architecture & Product Strategy Review

**Status:** Review for approval. **No code to be written until the recommended architecture is approved.**
**Date:** 7 August 2026
**Author:** Review conducted across Product Owner, Enterprise Architect, Principal Engineer, Mobile Architect, GIS, Cloud, Performance, Security, UX and Fleet Consultant perspectives.
**Subject repositories:** `globocon/CityWatch` (web + API + data), `globocon/CityWatchMobile` (MAUI app v1.52.2)

---

## 0. Executive Summary

**Recommendation: BUILD — but not as described, and not as a standalone tracking product.**

Three conclusions drive everything below.

**1. You are further along than the brief assumes.** CityWatch already has a working control-room map, a patrol-car route domain model, a checkpoint-scan pipeline, an offline-sync pattern, a MAUI app that already reads GPS, and two SignalR hubs. This is not a greenfield feature. It is the completion of a feature that is roughly 40% built. That materially changes the effort estimate and the risk profile — downward.

**2. Real-time vehicle tracking on its own is not a differentiator.** It is table stakes. Samsara, Geotab, Verizon Connect and Teletrac Navman do vehicle tracking better than CityWatch ever will, with hardwired telematics, and they sell it cheaply. If the pitch is "we put a dot on a map," CityWatch loses that comparison on hardware quality and price. **The defensible position is fusion:** GPS proves the vehicle was in the area; the NFC SmartWand scan proves an officer physically touched the checkpoint; the incident report proves what they did there; the KPI engine proves it happened at the contracted frequency. No fleet-telematics vendor has the second, third and fourth. No guard-tour vendor has all four joined into one auditable record. **That fused chain of custody is the flagship feature. Tracking is a component of it.**

**3. There is a security blocker that must be closed before any location data is transmitted.** Of the 15 API controllers in `CityWatch.Web/API`, **none carry an `[Authorize]` attribute**. The mobile login endpoint accepts a password as a query-string parameter. Both SignalR hubs are mapped without authorisation, and `UpdateHub` broadcasts duress alerts to `Clients.All`. Shipping continuous officer location onto that surface would create a live, unauthenticated feed of where security officers are — a far graver exposure than anything currently at risk. **This is a Phase 0 gate, not a Phase 3 hardening task.**

**Verdict on premium positioning:** Yes, monetise it — but tier it correctly. See §12.9. Selling "live tracking" as the add-on invites a price comparison with Samsara that you lose. Selling **Verified Proof of Patrol** as the add-on has no direct comparator.

---

## 1. What Already Exists — Grounded Audit

Everything in this section was verified against the current `master` working tree, not inferred.

### 1.1 The control room map is already built

`CityWatch.RadioCheck/Pages/ControlRoomMap.cshtml` (671 lines) + `wwwroot/js/controlRoomMap.js` (1,354 lines) is a substantially complete live-operations dashboard:

- **Leaflet 1.9.4** with `leaflet.markercluster` 1.5.3, CARTO Voyager (light) and Dark Matter (night ops) basemaps, layer switcher
- Map bounds locked to Australia (`AU_BOUNDS`), min zoom 4, viscous bounds
- Guard-count marker clustering with worst-status roll-up (`alarm` > `warn` > `ok` > `off`)
- A dedicated `carLayer` for PCAR vehicles, separate from the clustered site layer
- CSS marker glide already in place: `.leaflet-marker-icon { transition: transform 1.8s linear; }` and a `.pcar-ghost` variant at `.9s`
- 30-second refresh, optimised by a cheap **change-token endpoint** (`OnGetChangeToken`) that hashes max-IDs and last-timestamps across the activity, wand-scan and PCAR-visit tables so a full reload only happens when something actually changed
- Diff-based change animation, toasts, autocomplete search, and a filter model already covering status / site / region / updated / alert / frequency / guard text

**Assessment:** this is good work and a genuine asset. The UX foundation for the flagship feature exists. It is not a prototype to be replaced; it is a base to be extended.

### 1.2 The patrol-car domain model already exists

| Entity | File | Purpose |
|---|---|---|
| `ClientSitePatrolCar` | `Models/ClientSitePatrolCar.cs` | Vehicle registry — Model, Rego, site. **20 rows in the production copy.** |
| `PcarRoute` | `Models/PcarRoute.cs` | Named route, bound to a SmartWand allocation |
| `PcarRouteDetails` | same file | Ordered stops with per-day-of-week windows (`StartMon`/`EndMon`/`VisitMon` … plus a public-holiday `Pho` set) |
| `PcarRouteDailyVisits` | same file | Actual visits, with `GpsCoordinates`, `TimeOn`/`TimeOff`, status enum, parent-visit linkage. **0 rows — the feature is built but not yet in production use.** |
| `PcarVisitHistory` | same file | Audit trail with a genuinely well-designed time model: `ServerUtcTime`, `EventDateTimeLocal`, `EventDateTimeLocalWithOffset`, timezone name + short name, UTC offset minutes, and the device's own UTC clock |

**`PcarVisitHistory`'s time model should become the house standard for all telemetry.** It already solves the hard problem — reconciling device clock, device timezone, and server clock — that every tracking system gets wrong on the first attempt.

### 1.3 The critical architectural gap

From `ControlRoomMap.cshtml.cs:120–122`, verbatim:

> *"PCAR live route data: today's wand-scan confirmed site visits per patrol guard, in chronological order… **A patrol's current location is the site of its most recent scan.**"*

That single sentence is the whole gap. Today, position is **inferred from discrete checkpoint events**. Between two scans — which on a multi-site patrol route may be 20–40 minutes apart — the vehicle's marker sits motionless on the last site it visited. The map is not lying, but it is showing a *stale, quantised, site-snapped* position.

The requested feature is the move from **event-derived position** to **continuous telemetry**. Everything else in the brief (breadcrumbs, replay, speed, heading, geofence alerts, nearest-unit dispatch) is downstream of that one change. It is a genuinely significant change — it introduces a high-frequency write path, a new storage growth curve, a battery-life constraint and a privacy regime — but it is *one* change, into a system already shaped to receive it.

### 1.4 Mobile app: reads GPS, cannot track

`C4iSytemsMobApp` is **.NET MAUI 8** (`net8.0-android;net8.0-ios;net8.0-maccatalyst`), app ID `com.C4isystem.c4isystemsmobapp`, version 1.52.2, min Android SDK 21 / target 34, iOS 11+.

Current location handling (`Services/PermissionService.cs`) is **one-shot only**:

```csharp
var location = await Geolocation.GetLocationAsync(new GeolocationRequest {
    DesiredAccuracy = GeolocationAccuracy.Medium,
    Timeout = TimeSpan.FromSeconds(10)
});
Preferences.Set("GpsCoordinates", location.Latitude + "," + location.Longitude);
```

Called from `LoginPage`, `WebIncidentReport` — i.e. a location stamp attached to an event. The last-known value is cached in `Preferences` and reused when permission is unavailable.

**Blocking platform gaps:**

| Gap | Evidence | Consequence |
|---|---|---|
| No `ACCESS_BACKGROUND_LOCATION` | `Platforms/Android/AndroidManifest.xml` declares only `ACCESS_COARSE_LOCATION` and `ACCESS_FINE_LOCATION` | Android 10+ stops delivering location the moment the app leaves the foreground |
| No `FOREGROUND_SERVICE` / `FOREGROUND_SERVICE_LOCATION` | absent from the manifest | Cannot legally run a location foreground service on Android 14 (target SDK is 34) |
| Only `LocationWhenInUse` requested | `PermissionService.CheckAndRequestPermissionsAsync()` | Even with the manifest fixed, the runtime grant is wrong |
| **Duplicate `UIBackgroundModes` key** | `Platforms/iOS/Info.plist` lines 41 and 45 | Two `UIBackgroundModes` arrays in one plist. The second (`bluetooth-central`, `bluetooth-peripheral`) overrides the first (`audio`), and **neither declares `location`.** iOS will not deliver background location. The audio background mode is also silently dead today — worth checking whether an existing feature depends on it. |

`NSLocationAlwaysAndWhenInUseUsageDescription` *is* already present with sensible copy, so the iOS purpose string is done.

### 1.5 Offline sync precedent already established

Four tables follow a consistent offline-queue pattern:

- `ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced`
- `PostActivityRequestLocalCacheOfflineNotSynced`
- **`PatrolCarLogRequestLocalCacheOfflineNotSynced`**
- `CustomFieldLogRequestHeadLocalCacheOfflineNotSynced`

The third is directly relevant. **Do not invent a new offline mechanism.** Extend this one.

### 1.6 Real-time plumbing exists — and is the wrong shape

Two hubs are registered in `CityWatch.Web/Program.cs:171–172`:

```csharp
app.MapHub<UpdateHub>("/updateHub");
app.MapHub<MobileAppSignalRHub>("/MobileAppSignalRHub");
```

`MobileAppSignalRHub` (crowd control) **does use groups correctly**, keyed on `ClientSiteId` — a good pattern to copy.

`UpdateHub` (`CityWatch.Common/SignalRHub/UpdateHub.cs`) is the anti-pattern:

```csharp
public async Task SendUpdateWithMessage(string message)
    => await Clients.All.SendAsync("ReceiveDuressAlarmAlert", message);
```

Every connected client receives every duress alert, regardless of which client, site or control room it belongs to. **This must not be the template for position broadcast.** At 827 sites, `Clients.All` for position updates would be both a privacy breach and a performance catastrophe.

### 1.7 Production scale (from the local `prod-citywatch` copy)

| Table | Rows |
|---|---|
| `ClientSites` | **827** |
| `Guards` | **1,202** |
| `GuardLogs` | **2,356,302** |
| `ClientSitePatrolCars` | **20** |
| `PcarRouteDailyVisits` | **0** |

Two things stand out. **827 sites and 1,202 guards** is a serious installed base — this is not a pilot system. **20 patrol cars** is the current tracked fleet, which means the brief's 500- and 1000-vehicle scenarios are aspirational by a factor of 25–50×; design for them, but do not pay for them in Phase 1.

### 1.8 Pre-existing defects surfaced during this audit

These are **not** part of the tracking feature, but three of them intersect it and must be resolved regardless.

| # | Severity | Finding |
|---|---|---|
| A1 | **Critical** | **No authentication on any API controller.** 0 of 15 files under `CityWatch.Web/API/` (including all five `MobileAppControllers`) carry `[Authorize]`. `Program.cs` configures cookie auth but sets no `FallbackPolicy`, so every API endpoint is anonymous. |
| A2 | **Critical** | **Password in query string.** `LoginController.GetUserLogin(string userName, string password)` has no `[HttpPost]` and takes credentials as URL parameters — they land in IIS logs, browser history and any proxy in between. |
| A3 | **Critical** | **Hardcoded Azure Storage account key** in `CityWatch.Data/Models/DailyPatrolData.cs:280`, committed to git. This key must be rotated and moved to configuration. (Note: this is exactly what the `gitleaks` gate on the santhomPay repo is designed to catch. CityWatch has **no CI workflow at all**.) |
| A4 | **High** | Both SignalR hubs are mapped without `.RequireAuthorization()`. |
| A5 | **Medium** | Leaflet, markercluster and CARTO tiles are all loaded from third-party CDNs (`unpkg.com`, `basemaps.cartocdn.com`). A 24/7 control room should not have an unmanaged third-party availability dependency in its critical path. CARTO's free basemap tier also needs a commercial-use licence review. |
| A6 | **Medium** | `AllowAnonymousToFolder("/")` in `CityWatch.RadioCheck` suppresses `[Authorize]`; pages self-guard with `User.Identity.IsAuthenticated`. Any new page that forgets the guard is public by default. `ControlRoomMap.cshtml.cs` does this correctly — but it is correct by discipline, not by construction. |

---

## 2. Business Analysis

### 2.1 Would this become the most attractive feature in CityWatch?

**Not on its own. Yes, as the visible surface of something bigger.**

The honest competitive read: a live map with moving vehicles is *demonstrable*. It wins the room in a sales meeting in a way a KPI report never does. That is real commercial value and should not be dismissed — but it is **demo value**, and demo value is only durable if the thing behind it is defensible.

What makes it defensible for CityWatch specifically:

- **The NFC anchor.** GPS can be spoofed, drifts in urban canyons, and proves proximity, not presence. The existing `ClientSiteSmartWandTags` NFC scan proves an officer was physically within centimetres of a fixed tag. **GPS + NFC together produce an evidentiary record neither can produce alone.** A fleet vendor cannot claim this. A guard-tour vendor without vehicle telemetry cannot claim it either.
- **The incident chain.** CityWatch already links incident reports, key/vehicle logs, logbooks and radio checks to sites and guards. Adding position turns a set of records into a *reconstructable timeline*: here is where the vehicle was, here is the checkpoint it scanned, here is the report written, here is the KPI it satisfied.
- **The KPI engine.** `MinPatrolFreq` and `DailyWandFq` already exist and already feed the map. Continuous tracking makes patrol-frequency compliance *provable* rather than *asserted*.

### 2.2 Would customers pay extra for it?

**Yes — but be precise about who "the customer" is.** There are two, and they buy different things.

| Buyer | Pays for | Why | Pricing shape |
|---|---|---|---|
| **The security firm** (your direct customer) | Operational control | Fewer disputes, faster dispatch, evidence in liability claims, ability to bid on contracts that mandate GPS | Per-vehicle / month |
| **The end client** (property owner, council, retail centre) | Proof of service | They pay for patrols they cannot see. Every security contract in the country has a trust gap. This closes it. | Per-site / month, or bundled into the firm's contract as a premium tier |

The end-client portal is the higher-margin product and the harder one to copy, because it depends on CityWatch's existing site/KPI/report model. **A security firm that can hand its client a login showing verified patrol history wins renewals.** That is the sales argument, and it is worth more than the tracking itself.

**Willingness-to-pay caution:** the security services market is price-competitive and margin-thin. Per-vehicle pricing will be benchmarked against consumer-grade GPS trackers, which are cheap. Do not price the dot. Price the proof.

### 2.3 Which industries benefit?

Ranked by fit with the existing 827-site base:

1. **Mobile patrol / alarm response security** — the core case. Multi-site routes, response-time SLAs, client disputes. Direct fit.
2. **Static guarding with a roving supervisor** — supervisor coverage verification across a site portfolio.
3. **Cash-in-transit and secure logistics** — highest willingness to pay, but the highest bar: needs duress integration, route deviation alerting, and likely hardwired telematics rather than a phone. A Phase 3+ target.
4. **Facilities management and cleaning contractors** — same proof-of-attendance problem, lower security requirements. An adjacent-market expansion, not a Phase 1 focus.
5. **Local council / ranger services** — parking, compliance, after-hours patrols. Strong fit with the existing public-sector reporting orientation.
6. **Emergency and roadside response** — nearest-unit dispatch is the whole product. Higher availability requirements than CityWatch currently meets.

### 2.4 Competitive comparison

*The market characterisations below reflect general segment patterns and should be verified against current vendor documentation before use in sales material.*

| Segment | Typical strength | Typical weakness | CityWatch's position |
|---|---|---|---|
| **Fleet telematics** (Samsara, Geotab, Verizon Connect, Teletrac Navman) | Hardwired devices, ignition state, driver behaviour, mature and cheap | Knows nothing about security work — no checkpoints, incidents, guard licensing, client sites | **Do not compete. Integrate.** Ingest their feed. |
| **Guard-tour / workforce** (TrackTik, Silvertrac, Novagems, QR-Patrol) | Checkpoints, incident reports, client portals | Vehicle tracking usually thin or bolted on; officer-centric not vehicle-centric | **Direct competitor.** Beating them on the fused record is the goal. |
| **Police / emergency CAD-AVL** | Nearest-unit dispatch, unit status, extreme reliability | Enterprise cost, long procurement, not sold to private security | **Borrow the patterns**, not the price point. |

**The strategically important line in that table is the first one.** A large security firm may already have Geotab or Samsara in its vehicles. If CityWatch's only tracking input is the officer's phone, it is *competing* with hardware the customer already paid for. If the ingest pipeline is device-agnostic — phone, or telematics API, or both — CityWatch becomes the layer that *makes their existing fleet data mean something in a security context*. That is a much better position, and it costs one abstraction to reach.

### 2.5 What would make it market-leading

Beyond the brief's list, in descending order of competitive value:

1. **Verified Proof of Patrol** — a per-shift, per-site, tamper-evident record fusing GPS trail + NFC scan + timestamp + officer identity, exportable as a client-facing PDF. This is the product.
2. **Telematics-agnostic ingest** — accept position from the MAUI app, a hardwired unit, or a third-party fleet API. §2.4.
3. **Route adherence scoring** — the planned route already exists in `PcarRouteDetails` with per-day time windows. Comparing intended vs actual is nearly free once telemetry lands, and no guard-tour competitor has the planned-route data model to do it.
4. **Duress pre-emption** — `ClientSiteDuress` already carries `GpsCoordinates`. A duress event should slam the map to the officer's live position and pin it. Officer safety is the emotional sell that closes deals.
5. **Client-facing live view, time-boxed** — the end client sees the patrol arrive, for the duration of the visit only. Borrowed directly from ride-share.
6. **Exception-based operations** — at 827 sites an operator cannot watch everything. Surface only deviations: late, off-route, stationary too long, speeding, missed checkpoint.

---

## 3. Comparable Products — Practices to Adopt

| Source | Practice | Why it matters here |
|---|---|---|
| **Uber driver tracking** | Client-side interpolation between sparse updates; the marker animates smoothly at 60fps from a 4–5 second server tick | Decouples perceived smoothness from update frequency. **This is the single biggest battery and bandwidth saving available.** The CSS transition already in `ControlRoomMap.cshtml:380` is the seed of this. |
| **Uber** | Batched, compressed uploads with exponential backoff; the device never holds a socket open | Argues for HTTPS batch POST over device-side SignalR. See §5.3. |
| **Google Maps live location** | Explicit consent, visible "you are sharing" indicator, hard time limit, one-tap stop | The privacy model. Adopt wholesale for the officer-facing side (§9.7). |
| **Google Maps** | Snap-to-road before display | Raw GPS in a city looks drunk. Road-snapping makes a trail look professional. Defer to Phase 3 — it costs API calls. |
| **Fleet management** | Trip segmentation on ignition on/off; harsh-braking and speeding as *events*, not raw samples | Store trips, not just points. Reporting queries hit a trip table, never the raw stream. |
| **Fleet management** | Exception reporting by default | An operator watching 100 dots sees nothing. An operator seeing 3 exceptions acts. |
| **Police CAD-AVL** | Explicit unit status (Available / En route / On scene / Unavailable) set by the officer, not inferred | Inferring status from movement is unreliable and operators do not trust it. Make it explicit and make it one tap. |
| **Police CAD-AVL** | Nearest-unit by *travel time*, not straight-line distance | Straight-line "nearest" is wrong across a river or a rail line and erodes operator trust fast. Phase 2 can ship haversine; Phase 3 should use a routing engine. |
| **Emergency response** | Last known position must survive network loss and be *labelled as stale*, with age | A map that shows a 20-minute-old position as current is worse than no map. Every marker carries an age and degrades visually. |
| **Emergency response** | Officer safety functions never share a failure domain with reporting | Duress must not be behind the same queue as position telemetry. |

---

## 4. Functional Requirements

### 4.1 Assessment of the requested scope

| # | Requirement | Status | Notes |
|---|---|---|---|
| 1 | Live GPS tracking of patrol vehicles | **New** | The core change. §5. |
| 2 | Driver/officer login | **Exists** | `GuardLogin`, `GuardLoginDetail`, `LoginController`. Needs auth hardening (A1/A2). |
| 3 | Vehicle assignment | **Partial** | `ClientSitePatrolCar` exists but binds a car to a *site*, not to a *shift and officer*. Needs a `PatrolUnitAssignment` concept. |
| 4 | Live map updates | **Partial** | Map exists; 30s change-token polling. Needs push. |
| 5 | Moving vehicle markers | **Partial** | `carLayer` + CSS glide exist; positions are scan-derived. |
| 6 | Breadcrumb travel path | **New** | Trivial once telemetry lands. |
| 7 | Route history | **New** | Storage design in §5.6. |
| 8 | Patrol replay | **New** | Highest-value single feature after live view — it settles disputes. |
| 9 | Speed | **New** | Take from the GPS fix, not derived from position deltas — derived speed is noisy. |
| 10 | Direction of travel | **New** | Take GPS heading. Needs `leaflet-rotatedmarker` or a CSS rotation on the icon. |
| 11 | Last update time | **Partial** | The map has an `updated` filter. Needs per-unit staleness with visual degradation. |
| 12 | Online / offline status | **Partial** | Status roll-up exists for sites; needs a per-unit connectivity state distinct from on/off duty. |
| 13 | Patrol start/end | **Partial** | `PcarVisitHistory.Action` already models Accepted/Cancelled/Started/Completed/Pushed. Extend, don't replace. |
| 14 | Checkpoint visits | **Exists** | `PcarRouteDailyVisits` + SmartWand NFC. **The strongest existing asset.** |
| 15 | Geofence alerts | **New** | `ClientSite.Gps` gives centres; needs a radius (or polygon) and a dwell model. |
| 16 | Nearest patrol identification | **New** | Haversine in Phase 2; travel-time in Phase 3. |
| 17 | Incident dispatch support | **Partial** | `IncidentReport` has `JobNumber`, `JobTime`, `CallSign`, `ResponseTime`. Dispatch *to a unit* is new. |
| 18 | Search and filtering | **Exists** | Already good in `controlRoomMap.js`. Extend the filter model. |
| 19 | Multi-site support | **Exists** | 827 sites, region filter present. |
| 20 | Multiple control rooms | **Gap** | No control-room entity exists. **This is the scoping primitive that fixes `Clients.All`** — see §5.5. Do this early; retrofitting scope is expensive. |

### 4.2 Recommended additions

| Addition | Rationale | Phase |
|---|---|---|
| **Unit status (explicit)** | Available / En route / On scene / Break / Unavailable, officer-set. The CAD pattern. Without it, dispatch is guesswork. | 2 |
| **Stale-position degradation** | Every marker shows fix age; visual decay at 2 / 5 / 15 minutes. Prevents the map from lying. | 1 |
| **Verified Proof of Patrol export** | The commercial product. §2.5. | 3 |
| **Route adherence score** | Planned (`PcarRouteDetails`) vs actual. Nearly free. | 3 |
| **Exception feed** | Late / off-route / stationary / speeding / missed checkpoint. Makes 100 units watchable. | 2 |
| **Telematics-agnostic ingest** | §2.4. Costs one interface if designed in Phase 1; costs a rewrite if not. | 1 (design), 3 (implement) |
| **Duress-driven map takeover** | `ClientSiteDuress.GpsCoordinates` already exists. | 2 |
| **Officer-visible tracking indicator** | Legal and IR requirement, not a nicety. §9.7. | 1 |
| **Time-boxed client live view** | Differentiator, and the end-client's reason to pay. | 4 |
| **Two-way "acknowledge" on dispatch** | An unacknowledged dispatch is not a dispatch. | 2 |

**One item to explicitly reject:** *dashcam / video integration*. It will be asked for. It multiplies bandwidth, storage, privacy exposure and support burden by an order of magnitude, and it is a hardware business. Partner, do not build.

---

## 5. Technical Architecture

### 5.1 Principles

1. **Additive and modular.** A new `CityWatch.Tracking` project. Existing tables are not altered. If tracking is disabled, CityWatch behaves exactly as it does today.
2. **Telemetry never touches the OLTP hot path.** The position stream is append-only, physically separate, and never joined to `GuardLogs` (2.36M rows) in a live query.
3. **Feature-flagged.** `Tracking:Enabled`, default `false`. The map falls back to today's scan-derived behaviour when off.
4. **Device-agnostic ingest.** One internal contract; multiple sources.
5. **UTC on the wire, site-local in the UI.** Reuse the `PcarVisitHistory` time model.
6. **Typed coordinates.** The existing `string GpsCoordinates` = `"lat,lon"` pattern (in `GuardLog`, `ClientSiteDuress`, `PcarRouteDailyVisits`, `ClientSite.Gps`) is fine for a single stamped event. **It must not be used for telemetry.** Use `decimal(9,6)` columns.

### 5.2 Component view

```
┌──────────────────────┐   ┌──────────────────────┐   ┌─────────────────────┐
│  MAUI app            │   │  Telematics unit     │   │  3rd-party fleet    │
│  (foreground svc)    │   │  (future)            │   │  API (future)       │
└──────────┬───────────┘   └──────────┬───────────┘   └──────────┬──────────┘
           │  batched HTTPS POST (30–60 s, 6–20 points)          │
           └────────────────────┬──────────────────┬─────────────┘
                                ▼                  ▼
                  ┌──────────────────────────────────────────┐
                  │  TrackingIngestController  (authenticated)│
                  │  validate → dedupe → plausibility → queue │
                  └─────────────┬─────────────────┬──────────┘
                                │                 │
                   ┌────────────▼──────┐   ┌──────▼──────────────────┐
                   │ Live State Store  │   │ Channel<T> → writer     │
                   │ last known / unit │   │ batched SqlBulkCopy     │
                   │ (memory → Redis)  │   └──────┬──────────────────┘
                   └────────┬──────────┘          ▼
                            │            ┌──────────────────────────┐
                            │            │ PatrolTrackPoint (part.) │
                            │            │ PatrolTrackSegment       │
                            │            └──────────────────────────┘
                            ▼
              ┌──────────────────────────────┐
              │ BroadcastHostedService (1 Hz)│  diff per control-room scope
              │  → PatrolTrackingHub groups  │
              └──────────────┬───────────────┘
                             ▼
                  ┌────────────────────────┐
                  │ ControlRoomMap (Leaflet)│  interpolates between frames
                  └────────────────────────┘
              ┌────────────────────────────────────┐
              │ GeofenceEvaluator (on ingest)      │ → exceptions, alerts
              └────────────────────────────────────┘
```

### 5.3 Mobile → server: HTTPS batch, not SignalR

**Recommendation: batched HTTPS POST from the device. SignalR only server→browser.**

| | Device-side SignalR | Batched HTTPS POST |
|---|---|---|
| Cellular reliability | Reconnect storms in patchy coverage | Stateless; retry is trivial |
| Battery | Persistent socket + keepalives | Radio wakes once per batch, then sleeps |
| Offline | Buffer + replay must be hand-built | Fits the existing `*LocalCacheOfflineNotSynced` pattern exactly |
| Backpressure | Hard | Server returns 429/503; client backs off |
| Ordering | Manual | Batch carries sequence numbers |

Ride-share apps upload position over HTTP batches for exactly these reasons. **Server→browser is the opposite case** and SignalR is right there — the browser is on stable wifi, needs sub-second updates, and one socket serves many units.

For server→device (dispatch), use push notifications with an in-app poll fallback. Do not hold a device socket open just to deliver an occasional dispatch.

### 5.4 Ingest contract

```
POST /api/tracking/positions      (authenticated; per-unit rate limited)

{
  "unitId": 42,
  "sessionId": "guid",              // patrol session; ties points to a shift
  "deviceUtc": "2026-08-07T04:12:00Z",
  "points": [
    { "seq": 1181, "utc": "...", "lat": -33.865143, "lon": 151.209900,
      "accuracyM": 8.0, "speedKph": 47.2, "headingDeg": 118.0,
      "altitudeM": 24.0, "isMock": false, "batteryPct": 63,
      "source": "phone" }
  ]
}
```

Server-side on every batch: reject points outside AU bounds; reject accuracy worse than a configured threshold (~100 m); dedupe on `(unitId, seq)`; **plausibility-check against the previous accepted point** (implied speed > ~250 km/h ⇒ flag, don't silently drop — a flagged teleport is evidence); record both device UTC and server UTC and the delta.

### 5.5 Broadcast: scoped groups and a server-side tick

**Two decisions that determine whether this scales.**

**Decision 1 — never `Clients.All`.** Introduce a `ControlRoom` entity (req. #20) with a defined site scope. SignalR group key = control room ID. `MobileAppSignalRHub`'s `Groups.AddToGroupAsync(ConnectionId, ClientSiteId)` is the pattern; copy it, don't copy `UpdateHub`.

**Decision 2 — broadcast on a server tick, not per ingest.** A single `IHostedService` wakes at a fixed 1 Hz, computes *changed units per control-room scope* since the last tick, and sends **one frame per group**. Not one message per position.

This is the difference between linear and quadratic. Naive per-position broadcast to *O* operators watching *V* vehicles is *O × V* messages per interval. Tick-based diff is *O* messages per interval, each carrying up to *V* small deltas. At 10 operators × 100 vehicles: **200 msg/s naive vs 10 msg/s tick-based.**

Frame payload stays small — unit id, lat, lon, heading, speed, age flag. ~40 bytes per changed unit.

### 5.6 Storage

**`PatrolTrackPoint`** — append-only, narrow, no FKs:

| Column | Type |
|---|---|
| `Id` | `bigint identity` |
| `UnitId` | `int` |
| `SessionId` | `uniqueidentifier` |
| `RecordedUtc` | `datetime2(0)` |
| `ReceivedUtc` | `datetime2(0)` |
| `Latitude` / `Longitude` | `decimal(9,6)` |
| `SpeedKph` / `HeadingDeg` / `AccuracyM` | `smallint` / `smallint` / `smallint` |
| `Flags` | `tinyint` (mock, low-accuracy, implausible, backfilled) |

Clustered index on `(UnitId, RecordedUtc)` — every read is "this unit, this window." Monthly partitioning on `RecordedUtc`; retention by partition switch, which is instant, rather than `DELETE`, which is not.

**`PatrolTrackSegment`** — the trip/leg roll-up written when a session or leg closes: start/end time, start/end position, distance, duration, max/avg speed, point count, checkpoint scans within the leg, adherence score. **All reporting and analytics query this table, never the point table.** This is what keeps the KPI engine fast as the point table grows into the hundreds of millions.

**Retention (recommend):** raw points 90 days hot, then 12 months in a compressed/archive partition, then purge. Segments retained 7 years — they are the evidentiary record and they are small. Confirm the raw-point figure against contractual and insurance requirements before committing.

### 5.7 Map technology

**Recommend: stay on Leaflet 1.9.4 for Phases 1–2.** It is already in use, already styled, already integrated with clustering, and the team knows it. Add `leaflet-rotatedmarker` (heading) and `leaflet.polylineDecorator` (direction arrows on trails).

**Re-evaluate at Phase 3** if concurrently visible moving markers exceed ~300, at which point Leaflet's DOM-based markers start to cost frames. **MapLibre GL JS** (WebGL, vector tiles, BSD-licensed, no vendor lock) is the migration target if that happens — but do not pre-emptively migrate. Marker count is an empirical trigger, not a prediction.

**Tiles are a commercial decision, not a technical one.** CARTO basemaps are currently loaded from `basemaps.cartocdn.com` and Leaflet itself from `unpkg.com`. Before this becomes a paid feature: confirm CARTO's commercial terms, and **self-host the Leaflet library** (A5). A 24/7 control room that goes dark because unpkg has an outage is an unacceptable design.

### 5.8 Cloud and hosting

Existing footprint includes Azure Blob Storage (`c4istorage1`). Recommended additions, in order of necessity:

| Service | Need | Phase |
|---|---|---|
| **Redis** (Azure Cache) | Live-state store + SignalR backplane. **Mandatory the moment there is more than one web instance** — in-memory live state silently diverges across instances otherwise. | 2 |
| **Azure SignalR Service** | Only if browser connections exceed what the app instances comfortably hold. At 10–50 operators, self-hosted is fine. | 3+ |
| **Notification Hubs / FCM+APNS** | Dispatch push | 2 |
| **Key Vault** | Direct consequence of A3 | 0 |

**Do not adopt an event-streaming platform (Event Hubs / Kafka) for Phase 1.** At 20 vehicles it is unjustified complexity. A bounded `Channel<T>` feeding a batched writer handles well over 1,000 points/second on one instance. Revisit at Phase 4 if multi-region or replay-from-log becomes a requirement.

### 5.9 Background processing

| Job | Trigger | Purpose |
|---|---|---|
| Position writer | Continuous, drains `Channel<T>` | Batched `SqlBulkCopy`, ~1s or 500 points |
| Broadcast tick | 1 Hz | §5.5 |
| Geofence evaluator | On ingest, in-process | Enter/exit/dwell against site geofences |
| Session reaper | 60 s | Close sessions with no fix for *N* minutes; mark units offline |
| Segment roll-up | On session close + nightly sweep | Populate `PatrolTrackSegment` |
| Partition maintenance | Monthly | Create next partition, switch out expired |

**Critical constraint:** these must **not** run on every web instance. Either designate a leader (a distributed lock in Redis) or move them to a dedicated worker. Running the broadcast tick on three instances triples every frame.

---

## 6. Mobile Application

### 6.1 Platform prerequisites (blocking)

**Android** — add to `Platforms/Android/AndroidManifest.xml`:
```
ACCESS_BACKGROUND_LOCATION
FOREGROUND_SERVICE
FOREGROUND_SERVICE_LOCATION      (required, target SDK 34)
POST_NOTIFICATIONS               (the foreground-service notification)
```
plus a foreground `Service` with `android:foregroundServiceType="location"`. Update `PermissionService` to request `LocationAlways`, and handle Android 11+'s rule that background location **cannot** be requested in the same prompt as foreground — it is a separate, second request, and it must be justified in-app first or users will decline it.

**iOS** — fix the **duplicate `UIBackgroundModes` key** (`Info.plist:41` and `:45`); merge into one array containing `location`, `bluetooth-central`, `bluetooth-peripheral`, and `audio` if audio is still needed. Then set `CLLocationManager.AllowsBackgroundLocationUpdates = true` and `ShowsBackgroundLocationIndicator = true` (the blue bar — this is also a privacy feature, see §9.7). Use `PausesLocationUpdatesAutomatically = false` for patrol work.

**Store review:** both stores scrutinise background location. Prepare a written justification and a demo account. Budget for at least one rejection round — this is normal, not a failure.

### 6.2 GPS update strategy — the central engineering question

**Do not poll every 2 seconds.** Fixed high-frequency polling is the single worst decision available: it drains battery, floods the network, produces mostly duplicate points, and is what makes officers disable the app.

**Recommend an adaptive strategy driven by motion state:**

| State | Detection | Sample | Upload | Rationale |
|---|---|---|---|---|
| **Stationary** | < 10 m movement for 3 consecutive fixes | 60 s heartbeat | piggyback | A parked car needs one point, not 30/minute |
| **On site** | inside a site geofence | 30 s | 120 s batch | Officer is on foot; precision matters less than the fact of presence |
| **Driving (steady)** | speed > 15 km/h, heading stable ±15° | 10 s | 60 s batch | Straight-line travel interpolates perfectly |
| **Driving (manoeuvring)** | heading change > 25°, or speed change > 20 km/h | 4 s | 60 s batch | Corners are where a fixed interval loses the road |
| **Dispatched / en route** | server-set flag | 4 s | **15 s** | Response times are contractual — accept the cost |
| **Duress** | duress activated | 2 s | **immediate, unbatched** | Officer safety overrides every other consideration |

Plus **distance filtering** (suppress any point within 25 m of the last accepted point unless the heartbeat is due) and **corner preservation** (always keep a point where heading changes materially — this is what makes a replayed trail follow the road instead of cutting corners).

Combined, this typically removes 60–80% of the points a fixed 2-second poll would generate, while producing a *better* trail, because the points that survive are the ones that carry information. **Treat these thresholds as configurable server-pushed policy, not compiled constants** — they will need tuning against real patrol behaviour, and you do not want an app-store release cycle in that loop.

### 6.3 Battery

Patrol vehicles have chargers, which makes this far more tractable than it is for a foot patrol. Nonetheless:

- Use the **fused/network-assisted provider**, not raw GPS, when accuracy demand is low
- Batch uploads so the cellular radio wakes once per minute rather than continuously — **radio wake-ups typically cost more than the GPS chip itself**
- Report `batteryPct` in the payload; surface low battery on the control-room map as a unit-health warning
- Auto-degrade to the stationary profile below a configured battery threshold, and tell the operator
- **Measure, don't guess.** Set an explicit acceptance target (suggest: **≤ 10% per hour** on a mid-range Android with the screen off) and test on real devices before pilot. Any planning number quoted before that measurement is a guess.

### 6.4 Offline and network recovery

Extend `PatrolCarLogRequestLocalCacheOfflineNotSynced`; do not invent a parallel mechanism.

- Persist every point to local SQLite **before** attempting upload
- Ring buffer sized for a full shift offline (12 h × worst-case rate ≈ 5,000 points — trivially small)
- On reconnect, upload oldest-first in bounded batches, flagged `backfilled` so the map does not animate a vehicle through an hour of history
- Exponential backoff with jitter; honour server `Retry-After`
- **Never block the UI on upload.** An officer must be able to work with no signal.
- Delete local points only on confirmed server acknowledgement

### 6.5 Session lifecycle and unit assignment

Model a **patrol session** explicitly: officer + vehicle + route + control room + start/end. This is the missing link identified at req. #3. `PcarVisitHistory.Action` already models Started/Completed/Cancelled/Pushed — extend that vocabulary rather than introducing a second one.

Rules: no session ⇒ **no tracking, at all** (this is the privacy guarantee, and it must be enforced server-side, not just in the app); one active session per officer; a session auto-closes after a configurable no-fix period, and closing it is an audited event. On logout, tracking stops immediately and visibly.

---

## 7. Control Room Experience

Designed for a professional operator on a 12-hour night shift, watching a wall-mounted screen, who must not be made to hunt.

### 7.1 Layout

```
┌────────────────────────────────────────────────────────────────────────┐
│ CityWatch Control  │ Sydney Control Room ▾ │ 18 units · 3 exceptions  ⚠ │
├──────────────┬─────────────────────────────────────┬───────────────────┤
│ UNITS        │                                     │ EXCEPTIONS  (3)   │
│ ▸ search     │                                     │ ⚠ PC-04 off-route │
│              │                                     │ ⚠ PC-11 stale 6m  │
│ ● PC-04  On  │            LIVE MAP                 │ ⚠ PC-07 speed 92  │
│ ● PC-07  Enr │      (units · trails · geofences)   ├───────────────────┤
│ ● PC-11 Stal │                                     │ SELECTED UNIT     │
│ ○ PC-19 Off  │                                     │ PC-04 · Rego …    │
│              │                                     │ J. Smith          │
│ [filters]    │                                     │ 47 km/h · NE      │
│ status ▾     │                                     │ Updated 4 s ago   │
│ site   ▾     │                                     │ Next: Westfield   │
│ region ▾     │                                     │ 7/12 stops · 94%  │
│              │                                     │ [Dispatch][Replay]│
├──────────────┴─────────────────────────────────────┴───────────────────┤
│ ◀◀  ◀  ▶  ▶▶   ●───────────────────  14:32  [1×] [4×] [16×]  LIVE ⟳    │
└────────────────────────────────────────────────────────────────────────┘
```

The timeline scrubber along the bottom is the key UX decision: **live and replay are the same view at different times**, not two screens. Dragging left enters replay; the LIVE button returns. Operators learn one interface.

### 7.2 Design rules

1. **Colour carries one meaning: urgency.** Extend the existing `COL` palette in `controlRoomMap.js` (`ok`/`warn`/`alarm`/`off`/`accent`). Never use colour for a second dimension — an operator at 3 a.m. cannot decode two colour scales.
2. **Never render a stale position as current.** Every marker carries fix age: < 30 s solid; 30 s–2 min soft pulse; 2–5 min hollow with an age badge; > 5 min greyed and moved to the exception list. **A map that quietly lies is worse than no map.**
3. **Interpolate client-side.** Animate between server frames (the CSS transition at `ControlRoomMap.cshtml:380` is already doing this for scan positions). Motion should look continuous at a 1 Hz frame rate.
4. **Exceptions are the primary surface at scale.** Above ~50 units nobody watches the map — they watch the exception list and use the map to investigate. Build the list as a first-class panel, not a toast.
5. **Preserve operator context absolutely.** Never re-centre, re-zoom or close a popup on a data refresh. The current code already respects this with its diff-based updates; hold that line.
6. **Duress pre-empts everything.** Full-width banner, audible alert, one click to centre and lock on the unit. Requires no filter change and no hunting.
7. **Dark theme is the default for night ops.** The dark basemap already exists; make it schedule-aware.
8. **Everything reachable in ≤ 2 clicks** from the map. An operator handling an incident does not navigate menus.
9. **Degrade honestly.** If the SignalR connection drops, say so in the header and fall back to polling — do not let the map appear frozen-but-fine.

---

## 8. Performance & Scalability

### 8.1 Assumptions

12-hour shifts; adaptive sampling per §6.2 averaging ~1 point per 12 seconds while active (≈ 3,600 points/vehicle/shift); 60-second upload batching; ~80 bytes per stored row including index overhead; one broadcast frame per second per control room.

### 8.2 Projections

| Vehicles | Ingest req/s | Points/day | New rows/yr | Raw storage/yr | Broadcast msg/s* | Verdict |
|---|---|---|---|---|---|---|
| **10** | 0.17 | 36 K | 13 M | ~1 GB | 2 | Trivial. Single instance. |
| **50** | 0.83 | 180 K | 66 M | ~5 GB | 3 | Comfortable. Single instance. |
| **100** | 1.7 | 360 K | 131 M | ~10 GB | 5 | Comfortable. Add Redis for the live store. |
| **500** | 8.3 | 1.8 M | 657 M | ~53 GB | 10 | **Partitioning mandatory.** Multi-instance + Redis backplane. |
| **1,000** | 16.7 | 3.6 M | 1.31 B | ~105 GB | 15 | Dedicated worker, aggressive roll-up, archive tier. Feasible but a deliberate programme. |

\* assuming 5–15 concurrent operators with tick-based diff broadcast. Under naive per-position broadcast the 1,000-vehicle case would be **~16,700 msg/s** — a 1,000× difference produced entirely by the §5.5 design decision.

**For context: the current fleet is 20 vehicles.** The 100-vehicle column is the realistic 2-year planning target. The 1,000-vehicle column should shape the *schema* (partitioning, roll-up tables, no FKs on the point table) so it is never a rewrite — but should not shape the Phase 1 *infrastructure* spend.

### 8.3 Other dimensions

**Network (device):** ~40 bytes/point compressed + HTTP overhead ⇒ roughly **0.5–1 MB per vehicle per 12-hour shift**. Negligible on any plan. Batching is what makes this true; per-point posting would be 10–20× worse from headers alone.

**Database writes:** the concern is not volume, it is *pattern*. 3.6 M individual `INSERT`s/day at 1,000 vehicles would be painful; the same volume via batched `SqlBulkCopy` every second is routine. **Never write points through EF Core change tracking.**

**Read pattern:** the danger is a report scanning the point table. Every analytical query must hit `PatrolTrackSegment`. Enforce this by convention *and* by keeping the point table out of the main `DbContext` — expose it through a dedicated narrow provider so an accidental `.Include()` is impossible.

**CPU/memory (server):** live state is ~200 bytes/unit ⇒ 1,000 units ≈ 200 KB. Irrelevant. CPU is dominated by the broadcast diff, which is O(changed units) per tick — also small. **The real server cost is serialisation**; use `System.Text.Json` with a source-generated context and short property names on the hot frame.

**Browser:** the practical ceiling is DOM markers. Leaflet handles a few hundred moving markers acceptably; beyond that, frames drop. Mitigations, in order: cluster off-screen units (already implemented), viewport culling, then WebGL rendering (§5.7).

### 8.4 Optimisation priorities

1. Adaptive sampling (§6.2) — the largest single win, and it is on the device
2. Tick-based diff broadcast (§5.5) — the largest server-side win
3. Client-side interpolation — decouples smoothness from frequency
4. Batched bulk writes — turns a write problem into a non-problem
5. Segment roll-up — keeps reporting fast permanently
6. Partitioning + retention — keeps the hot table small forever
7. Viewport culling — deferred until measured

---

## 9. Security & Privacy

### 9.1 Authentication (blocking — A1, A2)

**No location feature may ship onto the current API surface.** Required before Phase 1:

- A `FallbackPolicy` requiring authentication, with `[AllowAnonymous]` applied deliberately where genuinely needed
- Token-based auth for mobile (JWT with refresh), replacing the cookie flow — cookies are the wrong primitive for a native client
- `LoginController` converted to `[HttpPost]` with credentials in the body (A2)
- `.RequireAuthorization()` on both hub mappings (A4)
- Per-unit rate limiting on the ingest endpoint

An unauthenticated ingest endpoint is worse than an unauthenticated read endpoint: anyone could *write* false positions into the evidentiary record, which destroys the product's entire value proposition.

### 9.2 Authorisation

Location is the most sensitive data CityWatch will hold. Scope every read:

- An operator sees only units within their control room's scope
- A client user sees only their own sites, and only during an active visit if time-boxed viewing is enabled
- A supervisor sees their team
- An officer sees their own history, always — this is both fair and, in some jurisdictions, required

The existing `GuardRcClientSiteAccess` / `HrSettingsClientSites` patterns give a starting point.

### 9.3 GPS spoofing

Layered — no single control is sufficient:

1. **Device signal:** Android exposes `Location.IsFromMockProvider` / `IsMock`. Capture it, store it in `Flags`, never silently discard the point. iOS offers no equivalent, so do not rely on this alone.
2. **Server plausibility:** implied speed between consecutive points, altitude discontinuity, accuracy that is suspiciously perfect. Flag; do not drop. **A flagged anomaly is evidence; a dropped point is a gap you cannot explain later.**
3. **NFC corroboration — the strong control.** A SmartWand scan at a fixed physical tag is very hard to fake remotely. **Where GPS and NFC disagree, NFC wins and the discrepancy is itself an alert.** This is the differentiator from §2.1, doing double duty as a security control.
4. **Device attestation** (Play Integrity / DeviceCheck) — Phase 3+, when the fleet justifies it.

### 9.4 Transport and storage

TLS 1.2+ enforced (note `android:usesCleartextTraffic="true"` in the current manifest — remove it, or scope it narrowly, before shipping location). TDE at rest. Coordinates need not be column-encrypted — it would break every spatial query for little gain given TDE plus §9.2 scoping — but the **assignment** of officer to unit to session must be access-controlled, because that is what turns coordinates into personal information.

### 9.5 Audit

Every *read* of historical location is audited: who, which unit, which window, when, why. `FileDownloadAuditLogs` and `KeyVehicleLogAuditHistory` establish the precedent. In a workplace-surveillance context, being able to show *who looked at an officer's movements* is as important as the data itself — and it will be the first question asked in any dispute.

### 9.6 Compliance (Australia)

**This requires legal review before pilot. The following flags the issues; it is not legal advice.**

- **Privacy Act 1988 (Cth) and the APPs** — location tied to an identified officer is personal information. APP 1 (open policy), APP 3 (collection must be reasonably necessary), APP 5 (notification), APP 6 (use limitation), APP 11 (security) all engage.
- **State workplace-surveillance law is the sharper constraint.** NSW's *Workplace Surveillance Act 2005* requires prior written notice before tracking surveillance of employees — commonly understood as **14 days** — and covert tracking generally requires a court order. Victoria, WA, SA and the NT have their own Surveillance Devices Acts with materially different tests. **Confirm the current requirements for every state CityWatch operates in.** With 827 sites this is almost certainly multi-jurisdictional.
- **Practical consequence:** the product must ship with a **notice-and-consent workflow** — recorded acknowledgement per officer, per employer, retained and auditable. Treat this as a Phase 1 feature, not paperwork. A customer cannot lawfully switch tracking on without it, so it is on the critical path to first revenue.
- **Industrial relations.** Beyond the law, tracking is an employee-relations matter. Enterprise agreements may impose consultation obligations, and the security workforce is unionised in parts of the market. **The most likely cause of failure for this feature is not technical — it is officer resistance.** Design for consent and transparency, not for covert monitoring, and the objection largely dissolves.

### 9.7 Privacy by design

- **Tracking only within an active patrol session** (§6.5), enforced server-side
- **Always-visible indicator** in the app — iOS's `ShowsBackgroundLocationIndicator` plus an in-app banner
- **Officers see their own data**, in the same detail the control room does
- **Hard stop at session end.** No off-shift tracking, ever, under any configuration. Make it impossible, not merely disabled.
- **Retention limits enforced technically** (§5.6), not by policy alone
- **A documented break-glass path** for accessing a specific officer's history outside normal scope, requiring justification and generating an alert

---

## 10. Risks and Mitigations

| # | Risk | Sev | Mitigation |
|---|---|---|---|
| R1 | **Unauthenticated API (A1/A2/A4)** — location added to an open surface | **Critical** | Phase 0 gate. Feature does not start until closed. §9.1 |
| R2 | **Officer/union resistance** — the most likely cause of outright failure | **High** | Consent workflow, visible indicator, officer self-access, session-bounded tracking, session-end hard stop. Position it as safety (duress, nearest-unit) not surveillance. §9.7 |
| R3 | **Battery drain** → officers disable the app → the feature quietly dies | **High** | Adaptive sampling (§6.2); server-tunable policy; measured ≤10%/hr acceptance target; vehicle chargers; battery telemetry surfaced to the control room |
| R4 | **Mobile OS restrictions** — background location silently stops | **High** | Foreground service with persistent notification; manifest/plist fixes (§6.1); server-side detection of a unit that has gone quiet while nominally on shift; treat as an exception, not a silent gap |
| R5 | **Store rejection** for background location | Medium | Written justification, demo account, submit early, budget one rejection round |
| R6 | **GPS inaccuracy** — urban canyons, underground car parks, multipath | Medium | Accuracy threshold + `Flags`; never present a raw point as authoritative; **NFC scans as ground truth**; road-snapping at Phase 3; show accuracy radius rather than a false-precision dot |
| R7 | **Poor coverage** — patrols work rural and after hours | Medium | Offline-first store-and-forward (§6.4); backfill flagged so replay is honest; "last seen" with age, never a stale dot presented as live |
| R8 | **Storage growth** | Medium | Partitioning, roll-up, retention (§5.6). At 100 vehicles this is ~10 GB/yr — a planning item, not a threat. |
| R9 | **Broadcast doesn't scale** | Medium | Tick-based diff + scoped groups (§5.5). The mitigation is a design decision made now, costing nothing; retrofitting it later costs a rewrite. |
| R10 | **Regression in existing CityWatch** | Medium | Separate project, separate tables, feature flag defaulting off, no changes to existing schema. **Precondition: CityWatch has no CI at all** — a build/test gate should exist before a real-time subsystem is added. |
| R11 | **Third-party CDN / tile dependency** (A5) | Medium | Self-host Leaflet; resolve CARTO commercial terms before monetising; budget a tile licence |
| R12 | **Competing with the customer's existing telematics** | Medium | Device-agnostic ingest designed in Phase 1 (§2.4). Turns a competitor into a data source. |
| R13 | **Operator overload** at 100+ units | Medium | Exception-first UX (§7.2). |
| R14 | **Clock skew / timezone errors** across 827 sites | Medium | Store device UTC, server UTC and the delta; reuse the `PcarVisitHistory` time model; flag skew beyond a threshold |
| R15 | **Legal exposure** from non-compliant deployment | **High** | Legal review before pilot; consent workflow as a shipped feature; per-state configuration. §9.6 |
| R16 | **Scope creep into dashcam/video** | Medium | Explicitly out of scope (§4.2). Partner, don't build. |

---

## 11. Product Roadmap

Estimates assume **one full-time backend engineer, one mobile engineer, and shared front-end/QA**, and *exclude* the Phase 0 security work. They are planning ranges, not commitments — validate against team capacity before scheduling.

### Phase 0 — Security & Foundation Gate *(mandatory prerequisite)*
**~2–3 weeks · Low complexity, high urgency**

- Close A1, A2, A3, A4 (API auth, POST login, rotate the leaked storage key, hub authorisation)
- Add a CI workflow to CityWatch — build, test, secret scan. **The santhomPay `ci.yml` is a working template that already includes a `gitleaks` job which would have caught A3.**
- Self-host Leaflet (A5)
- Fix the Android manifest and the duplicate iOS `UIBackgroundModes` key (§6.1)

**Dependencies:** none. **Gate:** nothing in Phase 1 begins until this lands.
**Note:** every item here has independent value even if tracking is never built.

---

### Phase 1 — MVP: See the Vehicles
**~6–8 weeks · Medium complexity**

- `CityWatch.Tracking` project; `PatrolTrackPoint` + `PatrolTrackSegment` schema with partitioning
- Authenticated batch ingest endpoint with validation, dedupe and plausibility checks
- MAUI foreground location service with adaptive sampling and offline queue (extending `PatrolCarLogRequestLocalCacheOfflineNotSynced`)
- Patrol session lifecycle + officer/vehicle assignment
- **Consent & notice workflow** (§9.6 — on the critical path, not optional)
- In-memory live-state store
- `PatrolTrackingHub` with **control-room-scoped groups** and 1 Hz tick broadcast
- Control room: live markers with heading, speed, fix age and honest staleness degradation; client-side interpolation
- Feature flag `Tracking:Enabled`, default off
- Device-agnostic ingest *interface* defined (implementation deferred)

**Dependencies:** Phase 0. **Risk:** mobile background reliability across the Android OEM landscape — budget real-device testing, not emulators.
**Exit criteria:** 20-vehicle pilot; measured battery ≤ 10%/hr; no regression in existing functionality with the flag off.

---

### Phase 2 — Live Operations
**~6–8 weeks · Medium-high complexity**

- Breadcrumb trails and route history
- **Patrol replay** with the unified live/replay timeline scrubber (§7.1)
- Geofences (site radius, then polygons) with enter/exit/dwell events
- Exception engine and exception-first UI panel
- Explicit unit status (Available / En route / On scene / Break / Unavailable)
- Incident dispatch to a unit, with required acknowledgement
- Nearest-unit by haversine
- Duress integration: map takeover + 2 s unbatched sampling
- Push notifications for dispatch
- **Redis** for live state and SignalR backplane; leader election for background jobs

**Dependencies:** Phase 1. **Risk:** operator workflow — validate the exception model with real control-room staff before building it out.

---

### Phase 3 — Smart Patrol Intelligence *(the commercial phase)*
**~8–10 weeks · Medium-high complexity**

- **Route adherence scoring** — planned (`PcarRouteDetails`) vs actual
- **Verified Proof of Patrol** — GPS + NFC + timestamp + officer, exportable per site per period
- **Client-facing portal view** — the revenue feature
- Patrol coverage heatmaps
- Telematics-agnostic ingest **implemented** (third-party fleet API adapters)
- Road-snapping for presentable trails
- Nearest-unit by travel time
- Device attestation
- KPI engine integration: patrol frequency compliance from actual telemetry
- Archive tier and retention automation

**Dependencies:** Phase 2 + a real data corpus. **This is where the feature starts paying for itself.**

---

### Phase 4 — AI & Predictive
**~10–12 weeks · High complexity, high uncertainty**

- Route optimisation against travel time, contracted windows and risk
- Anomaly detection on patrol behaviour (unusual dwell, unusual route, unusual hours)
- Predictive ETA for dispatch
- Risk-weighted patrol frequency recommendations from historical incident density
- Automated shift narrative generation
- Time-boxed live client view

**Dependencies:** Phases 1–3 plus **at least 6–12 months of accumulated telemetry**. **Do not commit to dates on this phase.** Its value is entirely contingent on data volume and quality that does not exist yet. Treat as a research track with a revenue-bearing option, not a delivery commitment.

---

## 12. Final Recommendation

### 12.1 Overall feasibility
**High.** The domain model, control-room map, mobile app, offline pattern and real-time infrastructure all exist. This is completion, not construction. The genuine risks are organisational (officer acceptance, legal compliance) and platform-level (mobile background location), not architectural.

### 12.2 Business value
**High, with a correction to the framing.** The value is not the live map — it is the **verifiable service record**. Security firms lose contracts because they cannot prove work they actually did. CityWatch can close that gap using assets it already owns. Two revenue lines: per-vehicle operational tracking for the firm, per-site proof-of-service for the end client. The second is the higher-margin, harder-to-copy product.

### 12.3 Technical feasibility
**High, subject to two gates.** (1) API authentication must be fixed first — this is non-negotiable. (2) Mobile background location must be proven on real Android OEM devices before committing to a pilot date; that is where these projects usually slip.

### 12.4 Expected customer impact
- **Security firms:** fewer disputes, faster dispatch, defensible liability position, ability to bid on GPS-mandated contracts
- **End clients:** visible proof of a service they currently take on trust — a genuine change in the relationship
- **Officers:** meaningful safety improvement via duress-with-position and nearest-unit response — **provided it is positioned as safety rather than surveillance.** If it is positioned wrongly, this becomes the feature's biggest liability.

### 12.5 Competitive advantage
**Not from tracking. From fusion.** GPS + NFC checkpoint proof + incident report + KPI compliance, in one auditable chain, is a position neither fleet-telematics vendors nor guard-tour vendors currently occupy. It is defensible because it depends on four subsystems CityWatch already runs — which is precisely what a competitor cannot assemble quickly.

### 12.6 Recommended stack

| Layer | Choice | Rationale |
|---|---|---|
| Mobile | **.NET MAUI 8** + platform foreground services | Already built; do not rewrite |
| Ingest | **ASP.NET Core, batched HTTPS POST** | Robust on cellular; matches the offline pattern |
| Buffering | **`System.Threading.Channels`** | Sufficient to well past 1,000 vehicles; avoids premature Kafka/Event Hubs |
| Persistence | **SQL Server**, partitioned append-only + segment roll-up | Already the platform; keeps operational surface unchanged |
| Live state | **In-memory → Redis at Phase 2** | Mandatory before multi-instance |
| Real-time | **SignalR, scoped groups, 1 Hz tick** | Already in the stack; the fix is architectural, not technological |
| Map | **Leaflet 1.9.4** (self-hosted), MapLibre GL reviewed at Phase 3 | Continuity now; a measured trigger for change later |
| Tiles | **Commercial licence required** | Resolve before monetising |
| Push | **FCM + APNS** | Standard |
| Secrets | **Azure Key Vault** | Direct consequence of A3 |

### 12.7 Estimated implementation effort

| Phase | Duration | Complexity |
|---|---|---|
| 0 — Security gate | 2–3 weeks | Low, urgent |
| 1 — MVP | 6–8 weeks | Medium |
| 2 — Live ops | 6–8 weeks | Medium-high |
| 3 — Intelligence | 8–10 weeks | Medium-high |
| **0→3 total** | **~22–29 weeks** | ~5–7 months to the revenue-bearing feature |
| 4 — AI | 10–12 weeks | High, deferred, data-dependent |

Assumes one backend + one mobile engineer with shared front-end/QA. **Phase 0→1 (~8–11 weeks) delivers a demonstrable, pilotable live map** — that is the milestone worth planning the commercial conversation around.

### 12.8 Risks and mitigation
The full register is §10. The four that decide the outcome:

1. **R1 — unauthenticated API.** Blocking. Fix first.
2. **R2 — officer resistance.** The most likely cause of failure, and it is a positioning and consent-design problem, not a technical one.
3. **R15 — workplace-surveillance compliance.** Legal review before pilot; consent workflow as a Phase 1 deliverable.
4. **R3/R4 — battery and mobile OS restrictions.** Adaptive sampling and a foreground service; verify on real devices before committing to dates.

### 12.9 Should this be a premium feature?

**Yes — with the pricing attached to the right object.**

| Tier | Contents | Rationale |
|---|---|---|
| **Included** in the base platform | Live map with current scan-derived positions (i.e. today's behaviour, improved) | Keeps CityWatch competitive without discounting the new work |
| **Live Patrol Tracking** — per vehicle / month | Continuous GPS, trails, replay, geofences, dispatch, exceptions | The operational product for the security firm |
| **Verified Proof of Patrol** — per site / month | Client portal, verified GPS+NFC service records, compliance exports, time-boxed live view | **The high-margin product.** No direct comparator, and the end client is the one who pays. |

**Do not price the tracking against fleet-telematics vendors — that comparison is lost before it starts.** Price the *proof*, which they cannot sell.

### 12.10 Final recommendation — Lead Enterprise Architect & Product Strategist

**Proceed. With three conditions, in this order.**

**First, fix the front door.** No location feature may be added to an API surface where none of 15 controllers requires authentication and the login endpoint accepts a password in the URL. Phase 0 is two to three weeks, every item has standalone value, and it should be approved and started independently of any decision about tracking. If tracking is deferred indefinitely, do Phase 0 anyway.

**Second, change the pitch before writing the code.** The brief asks whether real-time vehicle tracking can be the flagship feature. Presented as tracking, the answer is no — it is a commodity, and better-resourced vendors sell it more cheaply with better hardware. Presented as **verified proof of patrol**, of which live tracking is the visible surface, the answer is yes, and the moat is the four subsystems CityWatch already runs. This distinction should be settled at the executive level before engineering begins, because it changes what gets built in Phase 3 and what gets sold at every stage.

**Third, design for consent from the first line of code.** The most probable failure mode is not a scaling wall or a battery problem. It is officers declining the permission, disabling the service, or a union raising a dispute — after the money is spent. Session-bounded tracking, a visible indicator, officer self-access and a hard stop at shift end are not compliance overhead; they are what makes the feature survive contact with the workforce. They also happen to be what the law appears to require. Build them in Phase 1.

Do those three things and this is a well-founded, differentiated, defensible product built substantially on assets that already exist. Skip any of them and it becomes an expensive map.

**Recommended next step:** approve Phase 0 immediately as standalone work, and schedule a decision session on the §12.9 commercial framing before Phase 1 design begins.

---

*Prepared for review. No implementation to commence prior to approval of the recommended architecture.*
