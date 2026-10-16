# CityWatch Control Room — Command Centre Upgrade
## Existing-System Audit & Phased Plan (Deliverables A–J)

**Date:** 11 Aug 2026 · **Branch audited:** `feature/TrackingFeaturePack_Dileep` (PR #1798)
**Scope:** CityWatch.RadioCheck control-room map + CityWatch.Tracking feature pack + CityWatch.Web ingest host
**Rule observed:** inspect first, code second. No code has been written for this plan.

---

## A. Existing Feature Audit

Verdicts: **KEEP** (works, don't rebuild) · **IMPROVE** (works but weak) · **FIX** (broken) ·
**COMPLETE** (partial) · **BUILD** (missing) · **SKIP** (unnecessary now).

| # | Feature | State | Verdict |
|---|---------|-------|---------|
| 1 | Position-based unit identity (car = PositionId+2M, guard = GuardId+1M; device is NOT the unit) | Exists, tested, field-verified | **KEEP — protected architecture** |
| 2 | GPS ingest pipeline (batch POST, enrolment+consent gates, session gate, rate limiter, validity/flag rules, channel writer) | Exists, 102 tests | **KEEP** |
| 3 | Session lifecycle (one Active per unit; same-guard re-login idempotent; different guard supersedes; logout closes; segment roll-up on close) | Exists | **KEEP** (see B2 for the silent-supersede gap) |
| 4 | NFC anchors annotate AtSite/Transit; scans never gate GPS | Exists, field-verified | **KEEP** |
| 5 | Live snapshot dual-process (memory in Web, DB fallback in RadioCheck) | Exists | **KEEP** |
| 6 | Live overlay: 5s poll, staleness buckets (fresh/soft/hollow/dead), mode chips, pan-lock release | Exists, live on test-rc | **KEEP** |
| 7 | Track Vehicle Live command (TTL, concurrency cap, audited, pending-until-ack honesty) | Exists | **KEEP** |
| 8 | Idle detection + idle panel (30s poll) | Exists | **KEEP** (layout collision → B6) |
| 9 | Duress handling (map centre + popup; legacy updateHub refresh) | Exists | **KEEP** |
| 10 | History endpoint (26h cap, 5000-point cap, stated truncation, per-view access audit) | Exists | **FIX** — ignores session boundaries (B1) |
| 11 | Segments roll-up (distance, duration, speeds, anchor count, AdherenceScore, per **session**) | Exists | **KEEP — reuse for stats (§20)** |
| 12 | Base map: site clustering, guard/site autocomplete, filters, tabs, banners, KPI header, change-token fast refresh, site tour | Exists | **KEEP** |
| 13 | PCAR wand-scan route + scan-sequence replay (base map) | Exists | **KEEP** (distinct from GPS replay) |
| 14 | Keyed read-only map link (`?key=` viewer cookie) | Exists, verified | **KEEP** |
| 15 | SignalR PatrolTrackingHub (Frame/JoinControlRoom) | Exists, dormant fast path | **KEEP, measure before relying on it** |
| 16 | GPS replay UI (8h fixed window, bare clock, tiny truncation note) | Partial | **COMPLETE → professional player (Phase 3)** |
| 17 | Unit trail on live map | Partial — client-only, 500 pts, page-lifetime | **COMPLETE → server-backed full session (Phase 3 data, Phase 1 UX)** |
| 18 | Marker visuals (emoji 🚓/👮, no rotation animation, jump between fixes) | Weak | **IMPROVE (Phase 1)** |
| 19 | Unit popup anchored on marker (blocks map — client complaint a) | Weak | **REPLACE with docked panel/bottom sheet (Phase 1)** |
| 20 | Zoom controls (Leaflet default, buried under header cards on mobile) | Weak | **IMPROVE (Phase 1)** |
| 21 | Floating-widget layout (idle panel over Refresh; replay bar over status pill) | Broken on mobile | **FIX (Phase 1)** |
| 22 | Time formatting ("In transit 301m" reads as metres; idle chip while "In transit") | Broken trust | **FIX (Phase 1)** |
| 23 | Search for tracked units (M1, callsign) | Missing (base map searches guards/sites only) | **BUILD (Phase 1)** |
| 24 | Follow mode | Missing | **BUILD (Phase 1)** |
| 25 | Reverse-geocoded street address | Missing | **BUILD (Phase 2, server-cached)** |
| 26 | Stop detection in history/replay | Missing (live idle exists; historical stops don't) | **BUILD (Phase 2, server-computed)** |
| 27 | Satellite + tactical dark map modes | Partial (dark tiles exist; panels not themed; no satellite) | **COMPLETE (Phase 2)** |
| 28 | Speed fallback when device sends none | Missing (shows "—") | **BUILD (Phase 2, server-side from fixes)** |
| 29 | Guards-at-scale layer (initials avatar, moving/stationary, site grouping, toggles) | Partial — server enrolment done (1,361 guards), mobile foot-guard flow unverified | **COMPLETE (Phase 4)** |
| 30 | PCAR↔Site↔Guard cross-navigation | Missing | **BUILD (Phase 4)** |
| 31 | Alert engine (stationary-too-long = exists as idle; offline, route-delay, arrival events = missing) | Partial | **COMPLETE (Phase 5)** |
| 32 | Planned vs actual patrol (PcarRoute/DailyVisits data exists; comparison view missing) | Partial | **COMPLETE (Phase 5)** |
| 33 | Patrol performance stats (data exists in TrackSegments) | Partial | **COMPLETE (Phase 5, reuse segments — no new model)** |
| 34 | Device health card (battery/accuracy already in payload; network/app-version not collected) | Partial | **Surface what exists (Phase 2); document mobile gaps, build nothing new** |
| 35 | Incident/SOS experience (duress exists; nearest-responder view missing) | Partial | **COMPLETE (Phase 5)** |
| 36 | AI control room | Missing | **SKIP until data is trustworthy (Phase 6)** |
| 37 | SignalR-everywhere / Redis / WebSocket rewrite | — | **SKIP — measure the 5s poll first** |

---

## B. Broken / Weak — fix before wow

### B1. 🚨 Replay stitches two patrol cars into one line (CONFIRMED ROOT CAUSE)
Observed: two PCARs logged in simultaneously (Cochin + Poonjar); replay draws a line bridging the cities.

Traced chain: login → both officers selected the **same Position** → same `UnitId` →
`SessionService.StartAsync` correctly **supersedes** the first session (one Active per unit,
`SessionService.cs:82`) → ingest Gate 2 correctly rejects the superseded phone's batches
(`IngestService.cs:82-88`) → **but** `TrackingController.History` queries
`WHERE UnitId = X AND RecordedUtc BETWEEN from AND to` (`TrackingController.cs:246`) —
**no session filter** — so the window returns session A's Cochin points followed by session B's
Poonjar points, ordered by time, and the client draws one polyline through all of them.
`TrackPoint.SessionId` already exists; it is simply never used to separate the streams.

Fix (data-first, not a JS patch):
1. History response groups points **by session** (`sessions:[{sessionId, guardId, guardName, startedUtc, endedUtc, points:[…]}]`).
2. Client draws one polyline per session and **never connects across a boundary**; replay plays one session at a time with a session picker when the window holds several.
3. `/live` payload gains `sessionId` + `sessionStartedUtc` (already on the DTO internally, just not projected) so the live trail can be fetched per-session too.
4. Regression tests: M1-only, M2-only, simultaneous sessions never mix, same site never merges, same SmartWand never merges, supersede boundary produces two trails.

### B2. Silent supersede = silent tracking loss
When officer B takes over a unit, officer A's phone keeps uploading forever with every batch
rejected — no signal to the device, the officer, or the control room. Fix: ingest response gains
`sessionSuperseded: true` (device re-prompts login / shows "another officer signed into M1");
control room raises an attention item ("M1: session taken over by J. Smith 14:32"). Prevention
option (login warns "M1 is already live with officer X — choose another car?") is a product
decision for Dileep — documented, not assumed.

### B3. Live marker teleports across the country on supersede — same fix as B1.3; the marker
must reset its trail when `sessionId` changes.

### B4. Popup-on-marker blocks the map (client complaint a) → docked panel / bottom sheet.
### B5. Zoom control buried on mobile; browser-zoom breaks divIcons (complaint c) → custom big controls.
### B6. Floating widgets collide (idle panel over Refresh; replay bar over status pill) → layout slots.
### B7. `301m` minutes read as metres; idle badge shown while "In transit" → humanise + reconcile.
### B8. Replay shows no date, no window, no direction (complaints b, d) → Phase 3 player.

---

## C. Missing — genuinely new work
Realistic rotated vehicle sprites + guard initial-avatars · smooth interpolation between fixes ·
Follow mode with resume-after-pan · unit search ("M3" → centre+follow) · docked asset card ·
server-cached reverse geocoding · historical stop detection · satellite layer + tactical dark
theming of overlay widgets · professional replay player (date picker, timeline, progressive
draw, time-gradient) · guard layer at scale with site grouping · cross-navigation · alert feed
beyond duress/idle · planned-vs-actual view · KPI strip for tracking assets.

---

## D. Architecture impact (all additive; identity rule untouched)

| Area | Change |
|------|--------|
| `GET /api/tracking/history/{unitId}` | Group by session (B1). Additive response shape; old flat `points` kept one release for compatibility, then removed. |
| `GET /api/tracking/live` | Add `sessionId`, `sessionStartedUtc`, `stateSinceUtc`. |
| Ingest response | Add `sessionSuperseded` flag (B2). |
| NEW `GET /api/tracking/address?lat&lon` | Server-side reverse geocode: `GeocodeCache` table keyed on ~100 m grid cell, provider behind an interface (Nominatim default, throttled queue, honest `null` on miss/rate-limit — UI falls back to coordinates/site name). Never called per GPS update. |
| History response | Server-computed `stops:[{lat,lon,fromUtc,toUtc,durationMin,nearSite}]` derived from the point stream (same jitter rules as IdleDetectionService — reuse `GeoMath`). |
| DB | No schema change for B1 (SessionId already on TrackPoint). New: `GeocodeCache`. New index `IX_TrackPoint_Unit_Session_RecordedUtc` if the grouped query needs it (measure). |
| Mobile (CityWatchMobile) | Nothing required for Phases 1–3. Phase 4 requires verifying the foot-guard login → session → GPS flow end-to-end on a phone. Device health beyond battery/accuracy (network, app version) is NOT collected today — documented as future mobile work, not built now. |
| Performance | Keep 5s poll. Measure payload at 12 cars + N guards before touching SignalR/Redis. Guard layer uses the existing site-cluster pattern; markers reused, not recreated (current `upsert` already does this — keep). |
| Security | All new operator endpoints `[Authorize]` + the existing access-audit pattern extends to address lookups tied to a unit. Keyed viewer link stays read-only. Phase 0 JWT debt unchanged, still pre-production gate (see citywatch-api-security-gaps). |

---

## E. UX design (one responsive page — no mobile-only fork)

**Mobile (primary):** search bar + filter chips (ALL/PCARS/GUARDS/SITES/ALERTS) pinned top;
bottom sheet with collapsed/half/full states for asset details; bottom-right zoom stack
(+ / − / fit-all / follow-target); asset drawer as a swipe-up list; FOLLOW banner with
STOP/RESUME; replay as full-width bottom player.
**Desktop:** same components docked — left asset drawer, right asset card, top KPI strip,
bottom replay bar. **Control-room wall:** tactical dark, KPI strip large, alert feed visible.
Touch targets ≥44 px, no hover-only actions, every workflow passes the 12-item 5-second test (§31).

---

## F. WOW features ranked (value × effort × risk)

| Rank | Feature | Value | Effort | Risk | Phase |
|------|---------|-------|--------|------|-------|
| 1 | Correct per-session replay (B1) — trust is the product | Critical | S | Low | 1 |
| 2 | Realistic rotating car + smooth glide | High | S | Low | 1 |
| 3 | Search → centre → FOLLOW in 2 taps | High | M | Low | 1 |
| 4 | Docked card / bottom sheet (map never blocked) | High | M | Low | 1 |
| 5 | Street address under assets (cached geocode) | High | M | Med (provider limits) | 2 |
| 6 | Professional replay player w/ date+time & direction | High | M | Low | 3 |
| 7 | Full-session trail + stops with dwell times | High | M | Low | 2–3 |
| 8 | Satellite + tactical dark | Med-High | S | Low | 2 |
| 9 | Guard layer at scale w/ initials + moving/stationary | High | L | Med (mobile verify) | 4 |
| 10 | Site grouping + PCAR↔Site↔Guard navigation | Med | M | Low | 4 |
| 11 | Alert feed (offline, stationary, arrival, takeover) | High | M | Med (rule tuning) | 5 |
| 12 | Planned vs actual + patrol performance (reuse segments/PcarRoute) | High | M | Low | 5 |

---

## G. Phase plan

**Phase 1 — Correct data + Visual command map** *(the trust + look release)*
B1 session-grouped history + `/live` session fields + B2 supersede signal + regression tests ·
realistic car sprite w/ heading rotation + interpolation · guard/site/unit **search + FOLLOW** ·
docked card / bottom sheet replaces popup · zoom stack · widget layout slots · time-format fixes.

**Phase 2 — Location intelligence**
Reverse-geocode cache + address on card/marker · historical stops w/ dwell · speed fallback ·
satellite + tactical dark (overlay themed) · device-health card from existing fields.

**Phase 3 — Professional replay**
Date/window picker · session picker when multiple · player controls (⏮ ▶ ⏸ ⏭, 1/2/4×) ·
timeline with start/end + big current date-time · progressive draw + time-gradient direction ·
stops/sites/NFC anchors on the trail · "REPLAY · M1 · Tue 11 Aug 06:00→14:00" header.

**Phase 4 — Guard intelligence**
Verify foot-guard mobile flow on a real phone first · initials avatars · moving/stationary +
duration · site grouping + expand · layer toggles · cross-navigation.

**Phase 5 (optional) — Security intelligence:** alert feed, planned-vs-actual, patrol
performance, incident/nearest-responder view.
**Phase 6 (future) — AI control room** over the by-then-trustworthy data. Not before.

Each phase: implement → test → deploy to test-rc → client feedback before the next.

---

## H. Testing plan

- **Identity/session regression (new, Phase 1):** M1 replay contains only M1; simultaneous
  sessions never mix; supersede boundary = two separate trails; same site/tag/SmartWand never
  merges units; superseded ingest returns `sessionSuperseded`.
- **GPS:** jitter-stop vs real stop; teleport point flagged Implausible not drawn as travel;
  missing speed/heading; backfill never drives live picture (existing tests keep passing).
- **Replay:** single session, multi-session window, empty window, 26h cap, truncation honesty.
- **UI manual matrix:** small/large Android, iPhone-size, tablet, desktop, wall screen — run the
  §31 twelve 5-second tests each phase; controls never overlap (B6 checklist).
- Existing 102 tracking tests must stay green. (Note: 7 CityWatch.Data.Tests failures are
  pre-existing on master — not this work's regression bar.)

## I. Deployment plan
Per phase to the test hosts (`test.c4i-system.com` Web + `test-rc.c4i-system.com` RC):
server-side VS publish → **stop app pool** → robocopy `/E /XF appsettings.json web.config` →
start pool (flag is read at startup; config edits need a recycle). Both hosts carry
`Tracking:Enabled=true`; Web is the leader instance. DbScripts stay idempotent and numbered
(368+). Production remains gated behind Phase 0 security debt — unchanged.

## J. Client demonstration plan
- **P1 demo:** search "M1" → map flies → FOLLOW → car glides realistically; open two sessions on
  one unit in test data → replay shows two clean separate trails + takeover attention item.
- **P2 demo:** click car → card reads "Main Road, Pala · 42 km/h · NE"; switch satellite/tactical;
  trail shows "⏸ 14 min — Bund Road".
- **P3 demo:** pick yesterday 06:00–14:00 → watch the route draw itself with the clock running;
  scrub the timeline; date always on screen.
- **P4 demo:** toggle GUARDS → avatars with initials at sites → tap guard → moving/stationary →
  Site 14 → see everyone there → follow the PCAR from the same card.
