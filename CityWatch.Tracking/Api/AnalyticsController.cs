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
    /// The Insights drawer's read layer (analytics plan, phases A1–A2). A SEPARATE
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

        /// <summary>The gate every analytics read passes: flag, window sanity, operator
        /// identity, and one audit row (§13.4). Returns the refusal, or null to proceed.</summary>
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
            return null;
        }

        private static bool IsCar(bool? isPatrolCar, int unitId)
            => isPatrolCar ?? TrackingUnitKey.IsPosition(unitId);

        private static TimeSpan CompareShift(DateTime fromUtc, DateTime toUtc)
            => TimeSpan.FromDays(Math.Max(1, (int)Math.Ceiling((toUtc - fromUtc).TotalDays)));

        /* ==================== A1: summary + A2: the activity pulse ==================== */

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
            var current = await KpisAsync(fromUtc, toUtc, ct);
            var previous = await KpisAsync(fromUtc - shift, toUtc - shift, ct);

            var bucketHours = (toUtc - fromUtc) <= TimeSpan.FromHours(48) ? 1 : 24;
            var curPulse = await PulseAsync(fromUtc, toUtc, bucketHours, ct);
            var prevPulse = await PulseAsync(fromUtc - shift, toUtc - shift, bucketHours, ct);
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

        private async Task<Kpis> KpisAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
        {
            var sessions = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new { s.UnitId, s.GuardId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar })
                .ToListAsync(ct);

            var guardsActive = sessions
                .Where(s => !IsCar(s.IsPatrolCar, s.UnitId))
                .Select(s => s.GuardId).Distinct().Count();
            var pcarsActive = sessions
                .Where(s => IsCar(s.IsPatrolCar, s.UnitId))
                .Select(s => s.UnitId).Distinct().Count();

            /* Time actually signed in, clipped to the window — an overnight shift only
               contributes the hours that fall inside it. */
            var activeMinutes = (int)sessions.Sum(s =>
            {
                var start = s.StartedUtc > fromUtc ? s.StartedUtc : fromUtc;
                var end = (s.EndedUtc ?? toUtc) < toUtc ? (s.EndedUtc ?? toUtc) : toUtc;
                return end > start ? (end - start).TotalMinutes : 0;
            });

            var visitSites = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => v.SiteId)
                .ToListAsync(ct);

            var scans = _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc);
            var checkIns = await scans.CountAsync(ct);
            /* A scan is evidence for the tag's own site; a tag not linked to one still
               counts as activity at the site the officer was logged in to. */
            var scanSites = await scans
                .Select(h => h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId)
                .Distinct()
                .ToListAsync(ct);

            var sitesActive = visitSites.Distinct().Union(scanSites).Count();

            return new Kpis(guardsActive, pcarsActive, sitesActive, visitSites.Count, checkIns, activeMinutes);
        }

        /// <summary>Events per bucket: NFC scans + confirmed arrivals + session starts.
        /// The aggregate that says WHEN to look, while the cards say WHERE.</summary>
        private async Task<int[]> PulseAsync(DateTime fromUtc, DateTime toUtc, int bucketHours, CancellationToken ct)
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

            Fill(await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc)
                .Select(h => h.HitUtcDateTime).ToListAsync(ct));
            Fill((await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => v.ConfirmedUtc).ToListAsync(ct)).Select(v => v!.Value));
            Fill(await _db.TrackingSessions
                .Where(s => s.StartedUtc >= fromUtc && s.StartedUtc < toUtc)
                .Select(s => s.StartedUtc).ToListAsync(ct));

            return buckets;
        }

        /// <summary>The drawer's six headline numbers. A record so both windows serialize
        /// with identical shape and the client can diff them field by field.</summary>
        public sealed record Kpis(int GuardsActive, int PcarsActive, int SitesActive,
            int SiteVisits, int CheckIns, int ActiveMinutes);

        /* ==================== A2: the activity cards ==================== */

        /// <summary>
        /// Guard activity, ranked. A guard appears if they held a tracking session OR made
        /// an NFC scan in the window — the hit log records scans even when no session was
        /// running, and a guard who only scanned still worked.
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
                .Select(s => new { s.UnitId, s.GuardId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar })
                .ToListAsync(ct);
            var guardSessions = sessions.Where(s => !IsCar(s.IsPatrolCar, s.UnitId)).ToList();
            var unitToGuard = guardSessions
                .GroupBy(s => s.UnitId)
                .ToDictionary(g => g.Key, g => g.First().GuardId);

            var scanByGuard = await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc && h.LoggedInGuardId > 0)
                .GroupBy(h => h.LoggedInGuardId)
                .Select(g => new { GuardId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.GuardId, g => g.Count, ct);

            var visits = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => v.UnitId)
                .ToListAsync(ct);
            var visitByGuard = visits
                .Select(u => TrackingUnitKey.ToGuardId(u) ?? (unitToGuard.TryGetValue(u, out var g) ? g : 0))
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
                var minutes = (int)mine.Sum(s =>
                {
                    var start = s.StartedUtc > fromUtc ? s.StartedUtc : fromUtc;
                    var end = (s.EndedUtc ?? toUtc) < toUtc ? (s.EndedUtc ?? toUtc) : toUtc;
                    return end > start ? (end - start).TotalMinutes : 0;
                });
                return new
                {
                    guardId = id,
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

            var visitAgg = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .GroupBy(v => new { v.SiteId, v.SiteName })
                .Select(g => new { g.Key.SiteId, g.Key.SiteName, Visits = g.Count(), Units = g.Select(v => v.UnitId).Distinct().Count() })
                .ToListAsync(ct);

            var scanAgg = (await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc)
                .Select(h => h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId)
                .ToListAsync(ct))
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var sessionSites = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .GroupBy(s => s.ClientSiteId)
                .Select(g => new { SiteId = g.Key, Sessions = g.Count() })
                .ToListAsync(ct);

            var siteIds = visitAgg.Select(v => v.SiteId)
                .Union(scanAgg.Keys)
                .Union(sessionSites.Select(s => s.SiteId))
                .Distinct().ToList();
            var names = await _db.PlatformClientSites
                .Where(cs => siteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);
            string NameOf(int id) => names.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n)
                ? n!
                : visitAgg.FirstOrDefault(v => v.SiteId == id)?.SiteName ?? ("Site " + id);

            var active = visitAgg.Select(v => v.SiteId).Union(scanAgg.Keys).Distinct()
                .Select(id => new
                {
                    siteId = id,
                    name = NameOf(id),
                    visits = visitAgg.Where(v => v.SiteId == id).Sum(v => v.Visits),
                    checkIns = scanAgg.TryGetValue(id, out var c) ? c : 0,
                    units = visitAgg.Where(v => v.SiteId == id).Sum(v => v.Units)
                })
                .OrderByDescending(r => r.visits + r.checkIns)
                .ToList();

            /* Sessions opened against a site, zero evidence produced there. */
            var quiet = sessionSites
                .Where(s => !active.Any(a => a.siteId == s.SiteId))
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
                    var minutes = (int)g.Sum(s =>
                    {
                        var start = s.StartedUtc > fromUtc ? s.StartedUtc : fromUtc;
                        var end = (s.EndedUtc ?? toUtc) < toUtc ? (s.EndedUtc ?? toUtc) : toUtc;
                        return end > start ? (end - start).TotalMinutes : 0;
                    });
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
        /// the biggest drop against its own 7-day average leads the list.
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
                .Where(h => h.SmartWandId != null && h.HitUtcDateTime >= baselineFrom && h.HitUtcDateTime < toUtc)
                .Select(h => new { WandId = h.SmartWandId!.Value, h.HitUtcDateTime })
                .ToListAsync(ct);

            var byWand = scans.GroupBy(s => s.WandId).ToList();
            var wandIds = byWand.Select(g => g.Key).ToList();
            var wands = await _db.PlatformSmartWands
                .Where(w => wandIds.Contains(w.Id))
                .Select(w => new { w.Id, w.WandName, w.ClientSiteId })
                .ToListAsync(ct);
            var homeSiteIds = wands.Select(w => w.ClientSiteId).Distinct().ToList();
            var siteNames = await _db.PlatformClientSites
                .Where(cs => homeSiteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);

            var rows = byWand.Select(g =>
            {
                var inWindow = g.Count(s => s.HitUtcDateTime >= fromUtc);
                var baseline = g.Count(s => s.HitUtcDateTime < fromUtc);
                var w = wands.FirstOrDefault(x => x.Id == g.Key);
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
        /// This is where "unusual bar on a chart" turns into "08:02 arrived, 08:14
        /// scanned, 09:00 left" — and from there, one click into Replay.
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
            var events = new List<TimelineEvent>();

            /* Sessions relevant to the entity — they carry sign-ins/outs, the unit→guard
               mapping, and the labels everything else is named with. */
            var sessionQuery = _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc));
            if (guardId is { } gId) sessionQuery = sessionQuery.Where(s => s.GuardId == gId);
            else if (unitId is { } uId) sessionQuery = sessionQuery.Where(s => s.UnitId == uId);
            else if (siteId is { } stId) sessionQuery = sessionQuery.Where(s => s.ClientSiteId == stId);
            else sessionQuery = sessionQuery.Where(s => false);       // wand: scans only

            var sessions = await sessionQuery
                .Select(s => new { s.UnitId, s.GuardId, s.ClientSiteId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar, s.Callsign })
                .ToListAsync(ct);

            /* Which units' movement belongs to this entity. */
            var units = guardId is { } g2
                ? sessions.Select(s => s.UnitId).Append(TrackingUnitKey.FromGuard(g2)).Distinct().ToList()
                : unitId is { } u2 ? new List<int> { u2 }
                : new List<int>();

            /* Visits: the entity's own units, or everyone's stays at the one site. */
            var visitQuery = _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc);
            visitQuery = siteId is { } st3 ? visitQuery.Where(v => v.SiteId == st3)
                : units.Count > 0 ? visitQuery.Where(v => units.Contains(v.UnitId))
                : visitQuery.Where(v => false);
            var visits = await visitQuery
                .Select(v => new { v.UnitId, v.SiteId, v.SiteName, v.EnteredUtc, v.ConfirmedUtc, v.ExitedUtc })
                .ToListAsync(ct);

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
                    .Select(s => new { s.UnitId, s.GuardId, s.ClientSiteId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar, s.Callsign })
                    .ToListAsync(ct);
                labelSessions = sessions.Concat(stray).ToList();
            }

            /* Scans: by the guard, by the site's tags, by the one wand — or, for a car,
               by whoever was signed in to it. */
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
                .Select(h => new { h.LoggedInGuardId, h.SmartWandId, h.HitUtcDateTime, SiteId = h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId })
                .ToListAsync(ct);

            /* Legs (TrackSegments): only for entities that own units — km on the timeline. */
            var legs = units.Count == 0
                ? new List<TrackSegmentRow>()
                : await _db.TrackSegments
                    .Where(t => units.Contains(t.UnitId) && t.EndUtc >= fromUtc && t.EndUtc < toUtc)
                    .Select(t => new TrackSegmentRow(t.UnitId, t.StartUtc, t.EndUtc, t.DistanceM, t.ToSiteId))
                    .ToListAsync(ct);

            /* ---- names, once, in batches ---- */
            var guardIds = labelSessions.Select(s => s.GuardId)
                .Concat(scans.Select(s => s.LoggedInGuardId))
                .Concat(visits.Select(v => TrackingUnitKey.ToGuardId(v.UnitId) ?? 0))
                .Where(id => id > 0).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(p => guardIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);
            var siteIds = sessions.Select(s => s.ClientSiteId)
                .Concat(scans.Select(s => s.SiteId))
                .Concat(legs.Select(l => l.ToSiteId ?? 0))
                .Where(id => id > 0).Distinct().ToList();
            var siteNames = await _db.PlatformClientSites
                .Where(cs => siteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);
            var wandIds = scans.Where(s => s.SmartWandId != null).Select(s => s.SmartWandId!.Value).Distinct().ToList();
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
                events.Add(new TimelineEvent(v.ConfirmedUtc!.Value, "arrived", WhoOf(v.UnitId),
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
            var truncated = ordered.Count > cap;
            if (truncated)
                ordered = ordered.Skip(ordered.Count - cap).ToList();   // keep the most recent

            return Ok(new { fromUtc, toUtc, events = ordered, truncated });
        }

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

            var current = await WeekAsync(fromUtc, toUtc, tzOffsetMinutes, ct);
            var shift = CompareShift(fromUtc, toUtc);
            var previous = await WeekAsync(fromUtc - shift, toUtc - shift, tzOffsetMinutes, ct);

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
            int tzOffsetMinutes, CancellationToken ct)
        {
            var dayCount = Math.Max(1, (int)Math.Ceiling((toUtc - fromUtc).TotalDays));
            var fromLocalDate = fromUtc.AddMinutes(tzOffsetMinutes).Date;
            var toLocalDate = fromLocalDate.AddDays(dayCount);
            int UtcDay(DateTime utc) => (int)((utc - fromUtc).TotalHours / 24);
            int LocalDay(DateTime local) => (int)(local.Date - fromLocalDate).TotalDays;

            /* The agreed targets — a site with a target is on the grid even if it was
               silent all week; that silence is exactly the finding. */
            var targets = await _db.PlatformSiteKpis
                .Where(k => k.MinPatrolFreq != null && k.MinPatrolFreq > 0)
                .GroupBy(k => k.ClientSiteId)
                .Select(g => new { SiteId = g.Key, Target = g.Max(k => k.MinPatrolFreq!.Value) })
                .ToDictionaryAsync(g => g.SiteId, g => g.Target, ct);

            /* Rounds done, both wand generations, per LOCAL day. */
            var wandFq = await _db.PlatformDailyWandFqs
                .Where(f => f.FqDate >= fromLocalDate && f.FqDate < toLocalDate)
                .Select(f => new { f.ClientSiteId, f.FqDate, f.Fq })
                .ToListAsync(ct);
            var rounds = await _db.PlatformWandRounds
                .Where(r => r.InspectionStartDatetimeLocal >= fromLocalDate
                    && r.InspectionStartDatetimeLocal < toLocalDate)
                .Select(r => new { r.ClientSiteId, r.GuardId, r.InspectionStartDatetimeLocal })
                .ToListAsync(ct);

            /* Presence signals, per UTC day offset from the local-midnight instant. */
            var scans = await _db.PlatformWandScans
                .Where(h => h.HitUtcDateTime >= fromUtc && h.HitUtcDateTime < toUtc)
                .Select(h => new { SiteId = h.TagLinkedClientSiteId ?? h.LoggedInClientSiteId, h.HitUtcDateTime })
                .ToListAsync(ct);
            var visits = await _db.TrackingSiteVisits
                .Where(v => v.ConfirmedUtc != null && v.ConfirmedUtc >= fromUtc && v.ConfirmedUtc < toUtc)
                .Select(v => new { v.SiteId, Utc = v.ConfirmedUtc!.Value })
                .ToListAsync(ct);
            var sessions = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new { s.ClientSiteId, s.StartedUtc, s.EndedUtc })
                .ToListAsync(ct);

            var siteIds = targets.Keys
                .Union(wandFq.Select(f => f.ClientSiteId))
                .Union(rounds.Select(r => r.ClientSiteId))
                .Union(scans.Select(s => s.SiteId))
                .Union(visits.Select(v => v.SiteId))
                .Union(sessions.Select(s => s.ClientSiteId))
                .Where(id => id > 0).Distinct().ToList();
            var names = await _db.PlatformClientSites
                .Where(cs => siteIds.Contains(cs.Id))
                .ToDictionaryAsync(cs => cs.Id, cs => cs.Name, ct);

            var siteRows = new List<WeekSiteRow>();
            foreach (var siteId in siteIds)
            {
                var target = targets.TryGetValue(siteId, out var t) ? t : 0;
                var cells = new WeeklyCell[dayCount];
                int met = 0, missed = 0;
                for (var d = 0; d < dayCount; d++)
                {
                    var wf = wandFq.Where(f => f.ClientSiteId == siteId && LocalDay(f.FqDate) == d).Sum(f => f.Fq);
                    /* The board's rule: rounds are what SOMEONE completed — the best
                       guard's count, never a sum that invents a patrol nobody made. */
                    var sw = rounds.Where(r => r.ClientSiteId == siteId && LocalDay(r.InspectionStartDatetimeLocal) == d)
                        .GroupBy(r => r.GuardId)
                        .Select(g => g.Count())
                        .DefaultIfEmpty(0).Max();
                    var done = Math.Max(wf, sw);
                    var dayScans = scans.Count(s => s.SiteId == siteId && UtcDay(s.HitUtcDateTime) == d);
                    var dayVisits = visits.Count(v => v.SiteId == siteId && UtcDay(v.Utc) == d);
                    var dayStart = fromUtc.AddDays(d);
                    var dayEnd = fromUtc.AddDays(d + 1);
                    var duty = sessions.Any(s => s.ClientSiteId == siteId
                        && s.StartedUtc < dayEnd && (s.EndedUtc == null || s.EndedUtc > dayStart));

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
