using System;
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
    /// The Insights drawer's read layer (analytics plan, phase A1). A SEPARATE controller
    /// on purpose: analytics is a discovery layer that must be able to fail, be disabled
    /// (Tracking:Analytics:Enabled), or be deleted without the live map, replay, or ingest
    /// noticing. Strictly read-only over the roll-ups the pack already maintains —
    /// sessions, site visits, and the platform's NFC hit log. It never touches TrackPoint,
    /// so the drawer stays cheap on the day the fleet is busiest.
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
        /// KPI counters for a window, beside the same counters for the previous equivalent
        /// window — the supervisor's real question is rarely "how many?", it is "more or
        /// less than normal?". The previous window is the current one shifted back a whole
        /// number of days, so 09:00 today compares with 09:00 yesterday, never with
        /// yesterday's finished day (a morning would otherwise always read as failure).
        /// </summary>
        [Authorize]
        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (!_options.Analytics.Enabled)
                return NotFound();
            if (toUtc <= fromUtc)
                return BadRequest();
            if ((toUtc - fromUtc) > TimeSpan.FromDays(8))
                return BadRequest("Window too large; request at most 8 days (the 7-day view with margin).");
            if (OperatorUserId() is not { } userId)
                return StatusCode(403, new { error = "Operator identity not found on the session." });

            /* Fleet-wide historical read: audited like every other one (§13.4). */
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

            var shiftDays = Math.Max(1, (int)Math.Ceiling((toUtc - fromUtc).TotalDays));
            var shift = TimeSpan.FromDays(shiftDays);
            var current = await KpisAsync(fromUtc, toUtc, ct);
            var previous = await KpisAsync(fromUtc - shift, toUtc - shift, ct);

            return Ok(new { fromUtc, toUtc, compareShiftDays = shiftDays, current, previous });
        }

        private async Task<Kpis> KpisAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
        {
            var sessions = await _db.TrackingSessions
                .Where(s => s.StartedUtc < toUtc && (s.EndedUtc == null || s.EndedUtc > fromUtc))
                .Select(s => new { s.UnitId, s.GuardId, s.StartedUtc, s.EndedUtc, s.IsPatrolCar })
                .ToListAsync(ct);

            /* Same kind rule as the live map and the replay directory: the login flag wins,
               the position-keyed unit id is the fallback for sessions that predate it. */
            var guardsActive = sessions
                .Where(s => !(s.IsPatrolCar ?? TrackingUnitKey.IsPosition(s.UnitId)))
                .Select(s => s.GuardId).Distinct().Count();
            var pcarsActive = sessions
                .Where(s => s.IsPatrolCar ?? TrackingUnitKey.IsPosition(s.UnitId))
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

        /// <summary>The drawer's six headline numbers. A record so both windows serialize
        /// with identical shape and the client can diff them field by field.</summary>
        public sealed record Kpis(int GuardsActive, int PcarsActive, int SitesActive,
            int SiteVisits, int CheckIns, int ActiveMinutes);
    }
}
