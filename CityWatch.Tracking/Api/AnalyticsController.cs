using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Tracking.Api
{
    /// <summary>
    /// The Insights drawer's read layer (analytics plan, phases A1–A4). A SEPARATE
    /// controller on purpose: analytics is a discovery layer that must be able to fail,
    /// be disabled (Tracking:Analytics:Enabled), or be deleted without the live map,
    /// replay, or ingest noticing. Strictly read-only over the roll-ups the pack already
    /// maintains — sessions, segments, site visits, and the platform's NFC hit log. It
    /// never touches TrackPoint, so the drawer stays cheap on the busiest day.
    /// </summary>
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly TrackingDbContext _db;
        private readonly TrackingOptions _options;

        public AnalyticsController(TrackingDbContext db, TrackingOptions options)
        {
            _db = db;
            _options = options;
        }

        private int? OperatorUserId()
        {
            var sid = User.FindFirstValue(ClaimTypes.Sid);
            return int.TryParse(sid, out var id) ? id : null;
        }

        /// <summary>
        /// The gate every analytics read passes: flag, window sanity, operator identity,
        /// and the audit trail (§13.4). The drawer auto-refreshes every minute, so a row
        /// per request would bury the trail under half a million identical lines a year
        /// per screen — one row per operator per ten minutes still answers the question
        /// the table exists for: who was looking at historical movement data, and when.
        /// </summary>
        private async Task<IActionResult?> GateAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
        {
            if (!_options.Analytics.Enabled)
                return NotFound();
            if (toUtc <= fromUtc)
                return BadRequest();
            if ((toUtc - fromUtc) > TimeSpan.FromDays(15))
                return BadRequest("Window too large; request at most 15 days (the 14-day trend with margin).");
            if (OperatorUserId() is not { } userId)
                return StatusCode(403, new { error = "Operator identity not found on the session." });

            var since = DateTime.UtcNow.AddMinutes(-10);
            var recentlyAudited = await _db.TrackingAccessAudits
                .AnyAsync(a => a.UserId == userId && a.Action == "ViewAnalytics" && a.AccessedUtc >= since, ct);
            if (!recentlyAudited)
            {
                _db.TrackingAccessAudits.Add(new Data.Entities.TrackingAccessAudit
                {
                    UserId = userId,
                    Action = "ViewAnalytics",
                    UnitId = null,
                    WindowFromUtc = fromUtc,
                    WindowToUtc = toUtc,
                    AccessedUtc = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
                await _db.SaveChangesAsync(ct);
            }
            return null;
        }

        private static bool IsCar(bool? isPatrolCar, int unitId)
            => isPatrolCar ?? TrackingUnitKey.IsPosition(unitId);

        private static TimeSpan CompareShift(DateTime fromUtc, DateTime toUtc)
            => TimeSpan.FromDays(Math.Max(1, (int)Math.Ceiling((toUtc - fromUtc).TotalDays)));

        /// <summary>Signed-in time inside the window — an overnight shift only contributes
        /// the minutes that fall inside it. The ONE clipping rule for every endpoint.</summary>
        private static double ClippedMinutes(DateTime startedUtc, DateTime? endedUtc,
            DateTime fromUtc, DateTime toUtc)
        {
            var start = startedUtc > fromUtc ? startedUtc : fromUtc;
            var end = (endedUtc ?? toUtc) < toUtc ? (endedUtc ?? toUtc) : toUtc;
            return end > start ? (end - start).TotalMinutes : 0;
        }

        /* ==================== A1: summary + A2: the activity pulse ==================== */

        private sealed record SessionRow(Guid Id, int UnitId, int GuardId, int ClientSiteId,
            DateTime StartedUtc, DateTime? EndedUtc, bool? IsPatrolCar);
        private sealed record SiteTime(int SiteId, DateTime Utc);

        /// <summary>One window's raw facts, fetched once and shared by the KPIs and the
        /// pulse — three queries instead of the seven this used to take.</summary>
        private sealed record WindowFacts(List<SessionRow> Sessions, List<SiteTime> Visits, List<SiteTime> Scans);

        private async Task<WindowFacts> FactsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
        {
            var sessions = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new SessionRow(s.Id, s.UnitId, s.GuardId, s.ClientSiteId,
                    s.StartedUtc, s.EndedUtc, s.IsPatrolCar))
                .ToListAsync(ct);
            var visits = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => new SiteTime(v.SiteId, v.ConfirmedUtc!.Value))
                .ToListAsync(ct);
            /* A scan is evidence for the tag's own site; a tag not linked to one still
               counts as activity at the site the officer was logged in to. */
            var scans = await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc)
                .Select(h => new SiteTime(h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId, h.HitUtcDateTime))
                .ToListAsync(ct);
            return new WindowFacts(sessions, visits, scans);
        }

        private static Kpis KpisOf(WindowFacts f, DateTime fromUtc, DateTime toUtc)
        {
            var guardsActive = f.Sessions
                .Where(s => !IsCar(s.IsPatrolCar, s.UnitId))
                .Select(s => s.GuardId).Distinct().Count();
            var pcarsActive = f.Sessions
                .Where(s => IsCar(s.IsPatrolCar, s.UnitId))
                .Select(s => s.UnitId).Distinct().Count();
            var activeMinutes = (int)f.Sessions.Sum(s => ClippedMinutes(s.StartedUtc, s.EndedUtc, fromUtc, toUtc));
            /* Site 0 is "no site on record", not a site — the write path stores 0 when a
               scan carries no linkable site, and a phantom row must never inflate a KPI. */
            var sitesActive = f.Visits.Select(v => v.SiteId)
                .Union(f.Scans.Select(s => s.SiteId).Where(id => id > 0))
                .Distinct().Count();
            return new Kpis(guardsActive, pcarsActive, sitesActive, f.Visits.Count, f.Scans.Count, activeMinutes);
        }

        /// <summary>Events per bucket: NFC scans + confirmed arrivals + session starts.
        /// The aggregate that says WHEN to look, while the cards say WHERE.</summary>
        private static int[] PulseOf(WindowFacts f, DateTime fromUtc, DateTime toUtc, int bucketHours)
        {
            var count = (int)Math.Ceiling((toUtc - fromUtc).TotalHours / bucketHours);
            var buckets = new int[Math.Max(1, count)];
            void Fill(IEnumerable<DateTime> times)
            {
                foreach (var t in times)
                {
                    var i = (int)((t - fromUtc).TotalHours / bucketHours);
                    if (i >= 0 && i < buckets.Length) buckets[i]++;
                }
            }
            Fill(f.Scans.Select(s => s.Utc));
            Fill(f.Visits.Select(v => v.Utc));
            Fill(f.Sessions.Where(s => s.StartedUtc >= fromUtc && s.StartedUtc < toUtc).Select(s => s.StartedUtc));
            return buckets;
        }

        /// <summary>
        /// KPI counters for a window, beside the same counters for the previous equivalent
        /// window — the supervisor's real question is rarely "how many?", it is "more or
        /// less than normal?". The previous window is the current one shifted back a whole
        /// number of days, so 09:00 today compares with 09:00 yesterday, never with
        /// yesterday's finished day. Also carries the activity pulse (A2): events per
        /// bucket for both windows, hourly up to 48 h, daily beyond.
        /// </summary>
        [Authorize]
        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;

            var shift = CompareShift(fromUtc, toUtc);
            var curFacts = await FactsAsync(fromUtc, toUtc, ct);
            var prevFacts = await FactsAsync(fromUtc - shift, toUtc - shift, ct);
            var current = KpisOf(curFacts, fromUtc, toUtc);
            var previous = KpisOf(prevFacts, fromUtc - shift, toUtc - shift);

            var bucketHours = (toUtc - fromUtc) <= TimeSpan.FromHours(48) ? 1 : 24;
            var curPulse = PulseOf(curFacts, fromUtc, toUtc, bucketHours);
            var prevPulse = PulseOf(prevFacts, fromUtc - shift, toUtc - shift, bucketHours);
            var buckets = curPulse.Select((n, i) => new
            {
                utc = fromUtc.AddHours((double)i * bucketHours),
                current = n,
                previous = i < prevPulse.Length ? prevPulse[i] : 0
            }).ToList();

            return Ok(new
            {
                fromUtc, toUtc,
                compareShiftDays = (int)shift.TotalDays,
                current, previous,
                pulse = new { bucketHours, buckets }
            });
        }

        /// <summary>The drawer's six headline numbers. A record so both windows serialize
        /// with identical shape and the client can diff them field by field.</summary>
        public sealed record Kpis(int GuardsActive, int PcarsActive, int SitesActive,
            int SiteVisits, int CheckIns, int ActiveMinutes);

        /* ==================== A2: the activity cards ==================== */

        /// <summary>
        /// Guard activity, ranked. A guard appears if they held a tracking session OR made
        /// an NFC scan in the window — the hit log records scans even when no session was
        /// running, and a guard who only scanned still worked. Visits are attributed to
        /// the guard whose SESSION recorded them — a shared car's visits belong to the
        /// officer driving at the time, never to whoever else touched the car that day.
        /// </summary>
        [Authorize]
        [HttpGet("guards")]
        public async Task<IActionResult> Guards([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;

            var sessions = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new { s.Id, s.UnitId, s.GuardId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar })
                .ToListAsync(ct);
            var guardSessions = sessions.Where(s => !IsCar(s.IsPatrolCar, s.UnitId)).ToList();
            /* ALL sessions, cars included: a visit recorded under a car session belongs
               to the officer driving at that moment — the session's own GuardId — never
               to whoever else touched the car that day. */
            var sessionToGuard = sessions.ToDictionary(s => s.Id, s => s.GuardId);

            var scanByGuard = await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc && h.LoggedInGuardId > 0)
                .GroupBy(h => h.LoggedInGuardId)
                .Select(g => new { GuardId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.GuardId, g => g.Count, ct);

            var visits = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => new { v.UnitId, v.SessionId })
                .ToListAsync(ct);
            var visitByGuard = visits
                .Select(v => TrackingUnitKey.ToGuardId(v.UnitId)
                    ?? (sessionToGuard.TryGetValue(v.SessionId, out var g) ? g : 0))
                .Where(g => g > 0)
                .GroupBy(g => g)
                .ToDictionary(g => g.Key, g => g.Count());

            var guardIds = guardSessions.Select(s => s.GuardId)
                .Union(scanByGuard.Keys)
                .Distinct()
                .ToList();
            var names = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);

            var rows = guardIds.Select(id =>
            {
                var mine = guardSessions.Where(s => s.GuardId == id).ToList();
                var minutes = (int)mine.Sum(s => ClippedMinutes(s.StartedUtc, s.EndedUtc, fromUtc, toUtc));
                return new
                {
                    guardId = id,
                    /* The unit key travels with the row so the client never needs to know
                       the guard-offset scheme (it must match the mobile app). */
                    unitId = TrackingUnitKey.FromGuard(id),
                    name = names.TryGetValue(id, out var n) ? n : ("Guard " + id),
                    sessions = mine.Count,
                    activeMinutes = minutes,
                    visits = visitByGuard.TryGetValue(id, out var v) ? v : 0,
                    checkIns = scanByGuard.TryGetValue(id, out var c) ? c : 0
                };
            })
            .OrderByDescending(r => r.checkIns + r.visits)
            .ThenByDescending(r => r.activeMinutes)
            .ToList();

            var truncated = rows.Count > 100;
            return Ok(new { guards = rows.Take(100), truncated });
        }

        /// <summary>
        /// Site activity: the busiest sites, and — because silence is the finding — the
        /// quiet ones: sites someone was signed in to that produced no visit and no scan.
        /// </summary>
        [Authorize]
        [HttpGet("sites")]
        public async Task<IActionResult> Sites([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;

            /* Grouped by SiteId ALONE: the denormalised SiteName snapshot can change
               mid-window, and a rename must not split one site into two rows or count
               the same unit twice. */
            var visitAgg = (await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .GroupBy(v => v.SiteId)
                .Select(g => new
                {
                    SiteId = g.Key,
                    SiteName = g.Max(v => v.SiteName),
                    Visits = g.Count(),
                    Units = g.Select(v => v.UnitId).Distinct().Count()
                })
                .ToListAsync(ct))
                .ToDictionary(v => v.SiteId);

            var scanAgg = (await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc)
                .Select(h => h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId)
                .ToListAsync(ct))
                .Where(id => id > 0)          // 0 = "no site on record", not a site
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var sessionSites = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .GroupBy(s => s.ClientSiteId)
                .Select(g => new { SiteId = g.Key, Sessions = g.Count() })
                .ToListAsync(ct);

            var siteIds = visitAgg.Keys
                .Union(scanAgg.Keys)
                .Union(sessionSites.Select(s => s.SiteId))
                .Where(id => id > 0).Distinct().ToList();
            var names = await _db.PlatformClientSites
                .Where(cs => siteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);
            string NameOf(int id) => names.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n!
                : visitAgg.TryGetValue(id, out var va) && !string.IsNullOrWhiteSpace(va.SiteName)
                    ? va.SiteName
                    : ("Site " + id);

            var active = visitAgg.Keys.Union(scanAgg.Keys).Where(id => id > 0).Distinct()
                .Select(id => new
                {
                    siteId = id,
                    name = NameOf(id),
                    visits = visitAgg.TryGetValue(id, out var va) ? va.Visits : 0,
                    checkIns = scanAgg.TryGetValue(id, out var c) ? c : 0,
                    units = visitAgg.TryGetValue(id, out var vu) ? vu.Units : 0
                })
                .OrderByDescending(r => r.visits + r.checkIns)
                .ToList();
            var activeIds = active.Select(a => a.siteId).ToHashSet();

            /* Sessions opened against a site, zero evidence produced there. */
            var quiet = sessionSites
                .Where(s => s.SiteId > 0 && !activeIds.Contains(s.SiteId))
                .Select(s => new { siteId = s.SiteId, name = NameOf(s.SiteId), sessions = s.Sessions })
                .OrderByDescending(s => s.sessions)
                .Take(20)
                .ToList();

            var truncated = active.Count > 100;
            return Ok(new { sites = active.Take(100), quiet, truncated });
        }

        /// <summary>
        /// Patrol car activity: per car — closed legs, distance, confirmed site visits and
        /// signed-in hours. Distance and legs come from the TrackSegments roll-up (§8.3);
        /// a leg belongs to the window its EndUtc falls in, because that is when the
        /// roll-up was written.
        /// </summary>
        [Authorize]
        [HttpGet("pcars")]
        public async Task<IActionResult> Pcars([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;

            var sessions = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new
                {
                    s.UnitId, s.GuardId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar,
                    s.Callsign, s.PatrolCarPositionName, s.ClientSiteId
                })
                .ToListAsync(ct);
            var carSessions = sessions.Where(s => IsCar(s.IsPatrolCar, s.UnitId)).ToList();
            var carUnits = carSessions.Select(s => s.UnitId).Distinct().ToList();

            var segments = await _db.TrackSegments
                .Where(t => carUnits.Contains(t.UnitId) && t.EndUtc >= fromUtc && t.EndUtc < toUtc)
                .GroupBy(t => t.UnitId)
                .Select(g => new { UnitId = g.Key, Legs = g.Count(), DistanceM = g.Sum(t => t.DistanceM) })
                .ToDictionaryAsync(g => g.UnitId, ct);

            var visitByUnit = (await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc
                    && carUnits.Contains(v.UnitId))
                .Select(v => v.UnitId)
                .ToListAsync(ct))
                .GroupBy(u => u)
                .ToDictionary(g => g.Key, g => g.Count());

            var guardIds = carSessions.Select(s => s.GuardId).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);
            var loginSiteIds = carSessions.Select(s => s.ClientSiteId).Distinct().ToList();
            var siteNames = await _db.PlatformClientSites
                .Where(cs => loginSiteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);

            var rows = carSessions
                .GroupBy(s => s.UnitId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(s => s.StartedUtc).First();
                    /* Same naming rule as the live map and replay: callsign → declared
                       position → the login site names the car. */
                    var label = !string.IsNullOrWhiteSpace(latest.Callsign) ? latest.Callsign
                        : !string.IsNullOrWhiteSpace(latest.PatrolCarPositionName) ? latest.PatrolCarPositionName
                        : siteNames.TryGetValue(latest.ClientSiteId, out var sn) && !string.IsNullOrWhiteSpace(sn) ? sn
                        : ("PC-" + g.Key);
                    var minutes = (int)g.Sum(s => ClippedMinutes(s.StartedUtc, s.EndedUtc, fromUtc, toUtc));
                    segments.TryGetValue(g.Key, out var seg);
                    return new
                    {
                        unitId = g.Key,
                        label,
                        guardName = guardNames.TryGetValue(latest.GuardId, out var gn) ? gn : null,
                        legs = seg?.Legs ?? 0,
                        distanceKm = Math.Round((seg?.DistanceM ?? 0) / 1000.0, 1),
                        visits = visitByUnit.TryGetValue(g.Key, out var v) ? v : 0,
                        activeMinutes = minutes
                    };
                })
                .OrderByDescending(r => r.distanceKm)
                .ThenByDescending(r => r.visits)
                .ToList();

            return Ok(new { cars = rows });
        }

        /// <summary>
        /// Smart wand scan activity — the "this site has 3 wands, are they all alive?"
        /// answer. A wand appears if it scanned in the window OR in the 7 days before it,
        /// so a wand that has gone quiet is shown, not hidden. Ordered worst-first:
        /// the biggest drop against its own 7-day average leads the list. Wand id 0 is
        /// "no wand selected" (the write path stores 0, not NULL) — not a device.
        /// </summary>
        [Authorize]
        [HttpGet("wands")]
        public async Task<IActionResult> Wands([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;

            var baselineFrom = fromUtc.AddDays(-7);
            var scans = await _db.PlatformWandScans
                .Where(h => h.SmartWandId != null && h.SmartWandId > 0
                    && h.HitUtcDateTime >= baselineFrom && h.HitUtcDateTime < toUtc)
                .Select(h => new { WandId = h.SmartWandId!.Value, h.HitUtcDateTime })
                .ToListAsync(ct);

            var byWand = scans.GroupBy(s => s.WandId).ToList();
            var wandIds = byWand.Select(g => g.Key).ToList();
            var wandById = (await _db.PlatformSmartWands
                .Where(w => wandIds.Contains(w.Id))
                .Select(w => new { w.Id, w.WandName, w.ClientSiteId })
                .ToListAsync(ct))
                .ToDictionary(w => w.Id);
            var homeSiteIds = wandById.Values.Select(w => w.ClientSiteId).Distinct().ToList();
            var siteNames = await _db.PlatformClientSites
                .Where(cs => homeSiteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);

            var rows = byWand.Select(g =>
            {
                var inWindow = g.Count(s => s.HitUtcDateTime >= fromUtc);
                var baseline = g.Count(s => s.HitUtcDateTime < fromUtc);
                wandById.TryGetValue(g.Key, out var w);
                return new
                {
                    wandId = g.Key,
                    name = w?.WandName is { Length: > 0 } n ? n : ("Wand " + g.Key),
                    siteId = w?.ClientSiteId,
                    siteName = w != null && siteNames.TryGetValue(w.ClientSiteId, out var sn) ? sn : null,
                    scans = inWindow,
                    prevDailyAvg = Math.Round(baseline / 7.0, 1),
                    lastScanUtc = g.Max(s => s.HitUtcDateTime)
                };
            })
            /* Worst first: quietest against its own baseline leads. */
            .OrderBy(r => r.prevDailyAvg > 0 ? r.scans / r.prevDailyAvg : double.MaxValue)
            .ThenByDescending(r => r.prevDailyAvg)
            .ToList();

            var truncated = rows.Count > 100;
            return Ok(new { wands = rows.Take(100), truncated });
        }

        /* ==================== A3: the entity timeline ==================== */

        /// <summary>One event on somebody's day. A uniform shape so every drill-down
        /// renders the same list: sign-ins, arrivals (with the stay), scans, and legs.</summary>
        public sealed record TimelineEvent(DateTime Utc, string Type, string Who,
            int? SiteId, string? SiteName, int? UnitId, int? GuardId, string? WandName,
            DateTime? ExitedUtc, int? Minutes, double? Km);

        /// <summary>
        /// The merged event stream behind every drill-down (plan A3): what one guard, one
        /// patrol car, one site, or one smart wand actually did in the window, in order.
        /// Exactly one entity per call — a timeline of everything is the pulse's job.
        /// Attribution is session-anchored: a guard's timeline carries only the movement
        /// their OWN sessions recorded (a shared car's other drivers stay off it), and a
        /// car's timeline carries only the scans made while its session was open.
        /// </summary>
        [Authorize]
        [HttpGet("timeline")]
        public async Task<IActionResult> Timeline([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, [FromQuery] int? guardId, [FromQuery] int? unitId,
            [FromQuery] int? siteId, [FromQuery] int? wandId, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;
            var filters = new[] { guardId.HasValue, unitId.HasValue, siteId.HasValue, wandId.HasValue }
                .Count(f => f);
            if (filters != 1)
                return BadRequest("Name exactly one entity: guardId, unitId, siteId or wandId.");

            const int cap = 400;

            /* Sessions relevant to the entity — they carry sign-ins/outs, the attribution
               anchor, and the labels everything else is named with. A wand has none. */
            var sessions = wandId != null
                ? new List<TimelineSessionRow>()
                : await (guardId is { } gId
                        ? _db.TrackingSessions.Where(s => s.GuardId == gId)
                        : unitId is { } uId
                            ? _db.TrackingSessions.Where(s => s.UnitId == uId)
                            : _db.TrackingSessions.Where(s => s.ClientSiteId == siteId!.Value))
                    .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                    .Select(s => new TimelineSessionRow(s.Id, s.UnitId, s.GuardId, s.ClientSiteId,
                        s.StartedUtc, s.EndedUtc, s.IsPatrolCar, s.Callsign))
                    .ToListAsync(ct);
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var footUnit = guardId is { } g2 ? TrackingUnitKey.FromGuard(g2) : 0;

            /* Visits: the guard's own sessions (plus their guard-keyed unit), the one
               car, or everyone's stays at the one site. Newest first, capped — the cap
               must bound the work, not just the response. */
            var visits = wandId != null
                ? new List<VisitRow>()
                : await (siteId is { } st3
                        ? _db.TrackingSiteVisits.Where(v => v.SiteId == st3)
                        : guardId != null
                            ? _db.TrackingSiteVisits.Where(v => v.UnitId == footUnit || sessionIds.Contains(v.SessionId))
                            : _db.TrackingSiteVisits.Where(v => v.UnitId == unitId!.Value))
                    .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                    .OrderByDescending(v => v.ConfirmedUtc)
                    .Take(cap)
                    .Select(v => new VisitRow(v.UnitId, v.SiteId, v.SiteName, v.EnteredUtc, v.ConfirmedUtc!.Value, v.ExitedUtc))
                    .ToListAsync(ct);

            /* Scans: by the guard, by the site's tags, by the one wand — or, for a car,
               by its drivers WHILE their car session was open (a driver's foot-patrol
               scans from earlier in the day are not the car's history). */
            var scanQuery = _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc);
            if (guardId is { } g3) scanQuery = scanQuery.Where(h => h.LoggedInGuardId == g3);
            else if (wandId is { } w3) scanQuery = scanQuery.Where(h => h.SmartWandId == w3);
            else if (siteId is { } st4)
                scanQuery = scanQuery.Where(h => (h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId) == st4);
            else
            {
                var carGuards = sessions.Select(s => s.GuardId).Distinct().ToList();
                scanQuery = scanQuery.Where(h => carGuards.Contains(h.LoggedInGuardId));
            }
            var scans = await scanQuery
                .OrderByDescending(h => h.HitUtcDateTime)
                .Take(cap)
                .Select(h => new { h.LoggedInGuardId, h.SmartWandId, h.HitUtcDateTime, SiteId = h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId })
                .ToListAsync(ct);
            if (unitId != null)
                scans = scans.Where(sc => sessions.Any(s => s.GuardId == sc.LoggedInGuardId
                    && sc.HitUtcDateTime >= s.StartedUtc && sc.HitUtcDateTime < (s.EndedUtc ?? toUtc))).ToList();

            /* Legs (TrackSegments): session-anchored for a guard, unit-anchored for a car. */
            var legs = guardId != null
                ? await _db.TrackSegments
                    .Where(t => (t.UnitId == footUnit || sessionIds.Contains(t.SessionId))
                        && t.EndUtc >= fromUtc && t.EndUtc < toUtc)
                    .OrderByDescending(t => t.EndUtc).Take(cap)
                    .Select(t => new TrackSegmentRow(t.UnitId, t.StartUtc, t.EndUtc, t.DistanceM, t.ToSiteId))
                    .ToListAsync(ct)
                : unitId is { } u3
                    ? await _db.TrackSegments
                        .Where(t => t.UnitId == u3 && t.EndUtc >= fromUtc && t.EndUtc < toUtc)
                        .OrderByDescending(t => t.EndUtc).Take(cap)
                        .Select(t => new TrackSegmentRow(t.UnitId, t.StartUtc, t.EndUtc, t.DistanceM, t.ToSiteId))
                        .ToListAsync(ct)
                    : new List<TrackSegmentRow>();

            /* A site is visited by units whose sessions were opened elsewhere — the Romeo
               car signed in at its base, calling at a client site. Fetch those sessions
               too, for the LABEL only: their sign-ins belong to their own timeline. */
            var labelSessions = sessions;
            var strayUnits = visits.Select(v => v.UnitId)
                .Where(u => sessions.All(s => s.UnitId != u))
                .Distinct().ToList();
            if (strayUnits.Count > 0)
            {
                var stray = await _db.TrackingSessions
                    .Where(s => strayUnits.Contains(s.UnitId)
                        && s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                    .Select(s => new TimelineSessionRow(s.Id, s.UnitId, s.GuardId, s.ClientSiteId,
                        s.StartedUtc, s.EndedUtc, s.IsPatrolCar, s.Callsign))
                    .ToListAsync(ct);
                labelSessions = sessions.Concat(stray).ToList();
            }

            /* ---- names, once, in batches ---- */
            var guardIds = labelSessions.Select(s => s.GuardId)
                .Concat(scans.Select(s => s.LoggedInGuardId))
                .Concat(visits.Select(v => TrackingUnitKey.ToGuardId(v.UnitId) ?? 0))
                .Where(id => id > 0).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(p => guardIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);
            var siteIds = labelSessions.Select(s => s.ClientSiteId)
                .Concat(scans.Select(s => s.SiteId))
                .Concat(legs.Select(l => l.ToSiteId ?? 0))
                .Where(id => id > 0).Distinct().ToList();
            var siteNames = await _db.PlatformClientSites
                .Where(cs => siteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);
            var wandIds = scans.Where(s => s.SmartWandId is > 0).Select(s => s.SmartWandId!.Value).Distinct().ToList();
            var wandNames = await _db.PlatformSmartWands
                .Where(w => wandIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WandName, ct);

            string GuardName(int id) => guardNames.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n! : ("Guard " + id);
            string SiteName(int id) => siteNames.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n! : ("Site " + id);
            /* A unit answers to its officer's name on foot and its callsign in a car —
               the same identity the map and replay use. */
            var unitWho = labelSessions.GroupBy(s => s.UnitId).ToDictionary(gr => gr.Key, gr =>
            {
                var latest = gr.OrderByDescending(s => s.StartedUtc).First();
                return IsCar(latest.IsPatrolCar, gr.Key)
                    ? (!string.IsNullOrWhiteSpace(latest.Callsign) ? latest.Callsign! : "PC-" + gr.Key)
                    : GuardName(latest.GuardId);
            });
            string WhoOf(int unit) => unitWho.TryGetValue(unit, out var w) ? w
                : TrackingUnitKey.ToGuardId(unit) is { } vg ? GuardName(vg)
                : ("Unit " + unit);

            /* ---- merge ---- */
            var events = new List<TimelineEvent>();
            foreach (var s in sessions)
            {
                var who = WhoOf(s.UnitId);
                if (s.StartedUtc >= fromUtc)
                    events.Add(new TimelineEvent(s.StartedUtc, "signin", who,
                        s.ClientSiteId, SiteName(s.ClientSiteId), s.UnitId, s.GuardId, null, null, null, null));
                if (s.EndedUtc is { } ended && ended < toUtc)
                    events.Add(new TimelineEvent(ended, "signout", who,
                        s.ClientSiteId, SiteName(s.ClientSiteId), s.UnitId, s.GuardId, null, null, null, null));
            }
            foreach (var v in visits)
            {
                var mins = v.ExitedUtc is { } ex ? (int?)Math.Max(1, (int)(ex - v.EnteredUtc).TotalMinutes) : null;
                events.Add(new TimelineEvent(v.ConfirmedUtc, "arrived", WhoOf(v.UnitId),
                    v.SiteId, SiteName(v.SiteId), v.UnitId, TrackingUnitKey.ToGuardId(v.UnitId), null,
                    v.ExitedUtc, mins, null));
            }
            foreach (var sc in scans)
            {
                events.Add(new TimelineEvent(sc.HitUtcDateTime, "scan", GuardName(sc.LoggedInGuardId),
                    sc.SiteId, SiteName(sc.SiteId), null, sc.LoggedInGuardId,
                    sc.SmartWandId is { } wid && wandNames.TryGetValue(wid, out var wn) ? wn : null,
                    null, null, null));
            }
            foreach (var l in legs)
            {
                events.Add(new TimelineEvent(l.EndUtc, "leg", WhoOf(l.UnitId),
                    l.ToSiteId, l.ToSiteId is { } to ? SiteName(to) : null, l.UnitId, null, null,
                    null, Math.Max(1, (int)(l.EndUtc - l.StartUtc).TotalMinutes),
                    Math.Round(l.DistanceM / 1000.0, 1)));
            }

            var ordered = events.OrderBy(e => e.Utc).ToList();
            var truncated = ordered.Count > cap
                || visits.Count == cap || scans.Count == cap || legs.Count == cap;
            if (ordered.Count > cap)
                ordered = ordered.Skip(ordered.Count - cap).ToList();   // keep the most recent

            return Ok(new { fromUtc, toUtc, events = ordered, truncated });
        }

        private sealed record TimelineSessionRow(Guid Id, int UnitId, int GuardId, int ClientSiteId,
            DateTime StartedUtc, DateTime? EndedUtc, bool? IsPatrolCar, string? Callsign);

        private sealed record VisitRow(int UnitId, int SiteId, string SiteName,
            DateTime EnteredUtc, DateTime ConfirmedUtc, DateTime? ExitedUtc);

        private sealed record TrackSegmentRow(int UnitId, DateTime StartUtc, DateTime EndUtc,
            int DistanceM, int? ToSiteId);

        /* ==================== A4: the weekly patrol-frequency grid ==================== */

        /// <summary>One site's week. Cells are day states; met/missed are the row's tally.</summary>
        public sealed record WeeklyCell(string State, int Done, int Scans);

        /// <summary>
        /// The site × day patrol-frequency grid (plan A4) — the Monday answer to "where
        /// are we under-delivering?", and the table the client report prints. Per site
        /// per LOCAL day: rounds done = max(traditional DailyWandFq, best guard's
        /// smart-wand rounds) — the same conservative rule the RC board's FQ badge uses —
        /// held against the agreed MinPatrolFreq. A day with duty but no rounds is
        /// MISSED, declared, never hidden; worst rows sort first.
        /// </summary>
        [Authorize]
        [HttpGet("weekly")]
        public async Task<IActionResult> Weekly([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, [FromQuery] int tzOffsetMinutes, CancellationToken ct)
        {
            if (await GateAsync(fromUtc, toUtc, ct) is { } refused)
                return refused;
            if (tzOffsetMinutes is < -840 or > 840)
                return BadRequest("tzOffsetMinutes out of range.");

            var targets = await _db.PlatformSiteKpis
                .Where(k => k.MinPatrolFreq != null && k.MinPatrolFreq > 0)
                .GroupBy(k => k.ClientSiteId)
                .Select(g => new { SiteId = g.Key, Target = g.Max(k => k.MinPatrolFreq!.Value) })
                .ToDictionaryAsync(g => g.SiteId, g => g.Target, ct);

            var current = await WeekAsync(fromUtc, toUtc, tzOffsetMinutes, targets, detail: true, ct);
            var shift = CompareShift(fromUtc, toUtc);
            /* The previous week exists only for two totals — the cheap pass skips names. */
            var previous = await WeekAsync(fromUtc - shift, toUtc - shift, tzOffsetMinutes, targets, detail: false, ct);

            var rows = current.Sites
                .OrderByDescending(s => s.Missed)
                .ThenBy(s => s.Met)
                .ThenBy(s => s.Name)
                .ToList();
            var truncated = rows.Count > 200;

            return Ok(new
            {
                fromUtc, toUtc,
                days = current.Days,
                sites = rows.Take(200),
                totals = new { met = current.Sites.Sum(s => s.Met), missed = current.Sites.Sum(s => s.Missed) },
                prevTotals = new { met = previous.Sites.Sum(s => s.Met), missed = previous.Sites.Sum(s => s.Missed) },
                truncated
            });
        }

        private sealed record WeekSiteRow(int SiteId, string Name, int Target,
            WeeklyCell[] Cells, int Met, int Missed);
        private sealed record WeekResult(string[] Days, List<WeekSiteRow> Sites);

        private async Task<WeekResult> WeekAsync(DateTime fromUtc, DateTime toUtc,
            int tzOffsetMinutes, Dictionary<int, int> targets, bool detail, CancellationToken ct)
        {
            var dayCount = Math.Max(1, (int)Math.Ceiling((toUtc - fromUtc).TotalDays));
            /* Day buckets follow the requesting browser's clock at the WINDOW START (the
               client measures the offset there, so a DST change inside the week shifts
               at most the transition day by an hour). FqDate and inspection times are
               site-local dates; control room and sites share a timezone in this
               deployment — a cross-timezone control room is a known approximation. */
            var fromLocalDate = fromUtc.AddMinutes(tzOffsetMinutes).Date;
            var toLocalDate = fromLocalDate.AddDays(dayCount);
            int UtcDay(DateTime utc) => (int)((utc - fromUtc).TotalHours / 24);
            int LocalDay(DateTime local) => (int)(local.Date - fromLocalDate).TotalDays;

            /* Rounds done, both wand generations, pre-bucketed per (site, local day) —
               the cell loop below must be dictionary lookups, not list rescans. */
            var wandFqByCell = (await _db.PlatformDailyWandFqs
                .Where(f => f.FqDate >= fromLocalDate && f.FqDate < toLocalDate)
                .Select(f => new { f.ClientSiteId, f.FqDate, f.Fq })
                .ToListAsync(ct))
                .GroupBy(f => (f.ClientSiteId, Day: LocalDay(f.FqDate)))
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Fq));
            /* The board's rule: rounds are what SOMEONE completed — the best guard's
               count, never a sum that invents a patrol nobody made. */
            var roundsByCell = (await _db.PlatformWandRounds
                .Where(r => r.InspectionStartDatetimeLocal >= fromLocalDate
                    && r.InspectionStartDatetimeLocal < toLocalDate)
                .Select(r => new { r.ClientSiteId, r.GuardId, r.InspectionStartDatetimeLocal })
                .ToListAsync(ct))
                .GroupBy(r => (r.ClientSiteId, Day: LocalDay(r.InspectionStartDatetimeLocal)))
                .ToDictionary(g => g.Key, g => g.GroupBy(r => r.GuardId).Max(x => x.Count()));

            /* Presence signals, per UTC day offset from the local-midnight instant. */
            var scansByCell = (await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc)
                .Select(h => new { SiteId = h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId, h.HitUtcDateTime })
                .ToListAsync(ct))
                .Where(s => s.SiteId > 0)
                .GroupBy(s => (s.SiteId, Day: UtcDay(s.HitUtcDateTime)))
                .ToDictionary(g => g.Key, g => g.Count());
            var visitsByCell = (await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => new { v.SiteId, Utc = v.ConfirmedUtc!.Value })
                .ToListAsync(ct))
                .GroupBy(v => (v.SiteId, Day: UtcDay(v.Utc)))
                .ToDictionary(g => g.Key, g => g.Count());
            var sessionsBySite = (await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new { s.ClientSiteId, s.StartedUtc, s.EndedUtc })
                .ToListAsync(ct))
                .GroupBy(s => s.ClientSiteId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var siteIds = targets.Keys
                .Union(wandFqByCell.Keys.Select(k => k.ClientSiteId))
                .Union(roundsByCell.Keys.Select(k => k.ClientSiteId))
                .Union(scansByCell.Keys.Select(k => k.SiteId))
                .Union(visitsByCell.Keys.Select(k => k.SiteId))
                .Union(sessionsBySite.Keys)
                .Where(id => id > 0).Distinct().ToList();
            var names = detail
                ? await _db.PlatformClientSites
                    .Where(cs => siteIds.Contains(cs.Id))
                    .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct)
                : new Dictionary<int, string?>();

            var siteRows = new List<WeekSiteRow>();
            foreach (var siteId in siteIds)
            {
                var target = targets.TryGetValue(siteId, out var t) ? t : 0;
                var cells = new WeeklyCell[dayCount];
                int met = 0, missed = 0;
                sessionsBySite.TryGetValue(siteId, out var siteSessions);
                for (var d = 0; d < dayCount; d++)
                {
                    var done = Math.Max(
                        wandFqByCell.TryGetValue((siteId, d), out var wf) ? wf : 0,
                        roundsByCell.TryGetValue((siteId, d), out var sw) ? sw : 0);
                    var dayScans = scansByCell.TryGetValue((siteId, d), out var sc) ? sc : 0;
                    var dayVisits = visitsByCell.TryGetValue((siteId, d), out var dv) ? dv : 0;
                    var dayStart = fromUtc.AddDays(d);
                    var dayEnd = fromUtc.AddDays(d + 1);
                    var duty = siteSessions != null && siteSessions.Any(s =>
                        s.StartedUtc < dayEnd && (s.EndedUtc == null || s.EndedUtc > dayStart));

                    string state;
                    if (target > 0)
                    {
                        if (done >= target) { state = "met"; met++; }
                        else if (duty || done > 0 || dayScans > 0 || dayVisits > 0) { state = "missed"; missed++; }
                        else { state = "noduty"; }
                    }
                    else
                    {
                        state = (done > 0 || dayScans > 0 || dayVisits > 0) ? "active" : "noduty";
                    }
                    cells[d] = new WeeklyCell(state, done, dayScans);
                }
                siteRows.Add(new WeekSiteRow(siteId,
                    names.TryGetValue(siteId, out var n) && !string.IsNullOrWhiteSpace(n) ? n! : ("Site " + siteId),
                    target, cells, met, missed));
            }

            var days = Enumerable.Range(0, dayCount)
                .Select(d => fromLocalDate.AddDays(d).ToString("yyyy-MM-dd"))
                .ToArray();
            return new WeekResult(days, siteRows);
        }
    }
}
