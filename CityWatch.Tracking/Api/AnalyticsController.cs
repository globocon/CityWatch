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
            if ((toUtc - fromUtc) > TimeSpan.FromDays(8))
                return BadRequest("Window too large; request at most 8 days (the 7-day view with margin).");
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
    }
}
