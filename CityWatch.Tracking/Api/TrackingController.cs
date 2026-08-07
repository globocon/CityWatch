using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CityWatch.Tracking.Api
{
    /// <summary>
    /// The feature pack's HTTP surface (§9). Mapped only when Tracking:Enabled — with the flag
    /// off this controller's assembly is never added as an application part, so every route
    /// here is a 404 (RT2), which the mobile app treats as a normal offline condition.
    ///
    /// AUTH NOTE (accepted technical debt, §13.1.1): device endpoints are gated by
    /// enrolment + consent + a server-issued random session id rather than a bearer token,
    /// because the deployed mobile app sends no Authorization header at all. When the Phase 0
    /// JWT migration lands, [Authorize] goes on the device endpoints too and the session id
    /// reverts to being just an identifier. Operator endpoints use the existing cookie
    /// principal today.
    /// </summary>
    [ApiController]
    [Route("api/tracking")]
    public class TrackingController : ControllerBase
    {
        private readonly IIngestService _ingest;
        private readonly ISessionService _sessions;
        private readonly ILiveSnapshotService _snapshot;
        private readonly IModeCommandService _commands;
        private readonly Data.TrackingDbContext _db;
        private readonly TrackingOptions _options;

        public TrackingController(IIngestService ingest, ISessionService sessions,
            ILiveSnapshotService snapshot, IModeCommandService commands,
            Data.TrackingDbContext db, TrackingOptions options)
        {
            _ingest = ingest;
            _sessions = sessions;
            _snapshot = snapshot;
            _commands = commands;
            _db = db;
            _options = options;
        }

        /// <summary>Existing cookie principal → User.Id (ClaimTypes.Sid, set by both hosts'
        /// sign-in paths). The permission table refines this in Phase 2.</summary>
        private int? OperatorUserId()
        {
            var sid = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            return int.TryParse(sid, out var id) ? id : null;
        }

        /* ------------------------------- device ------------------------------- */

        /// <summary>Batch position ingest (§9.1). Idempotent: retried batches dedupe on
        /// (unit, session, seq) at the index level.</summary>
        [HttpPost("positions")]
        public async Task<ActionResult<IngestResponse>> Positions([FromBody] PositionBatch batch, CancellationToken ct)
        {
            if (batch == null || batch.UnitId <= 0 || batch.SessionId == Guid.Empty)
                return BadRequest();
            if (batch.Points.Count > 500)
                return BadRequest("Batch too large; split at 500 points.");

            var response = await _ingest.IngestAsync(batch, ct);

            if (response.RetryAfterSeconds is { } retry)
                Response.Headers.RetryAfter = retry.ToString();

            return Ok(response);
        }

        public sealed record StartSessionRequest(int UnitId, int GuardId, int ClientSiteId, int? PcarRouteId);

        /// <summary>Opens a patrol session. Returns 403 when the unit is not enrolled with
        /// consent — the device shows nothing and simply does not track (§13.5).</summary>
        [HttpPost("session/start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken ct)
        {
            if (request == null || request.UnitId <= 0 || request.GuardId <= 0)
                return BadRequest();

            var session = await _sessions.StartAsync(request.UnitId, request.GuardId,
                request.ClientSiteId, request.PcarRouteId, ct);
            if (session == null)
                return Forbid();

            return Ok(new { sessionId = session.Id, startedUtc = session.StartedUtc, policy = _options.Policy });
        }

        public sealed record EndSessionRequest(Guid SessionId);

        [HttpPost("session/end")]
        public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request, CancellationToken ct)
        {
            if (request == null || request.SessionId == Guid.Empty)
                return BadRequest();

            await _sessions.EndAsync(request.SessionId, "DeviceRequested", ct);
            return Ok();
        }

        /// <summary>Current sampling thresholds — server-pushed policy, no app release (§5.2).</summary>
        [HttpGet("policy")]
        public ActionResult<TrackingOptions.SamplingPolicyOptions> Policy() => Ok(_options.Policy);

        /// <summary>Device fast-path poll after a silent push (§5.3): "what mode should I be
        /// in right now?" The ingest response delivers the same answer on every batch.</summary>
        [HttpGet("mode/{unitId:int}")]
        public async Task<IActionResult> Mode(int unitId, [FromQuery] int seqSeen, CancellationToken ct)
        {
            if (unitId <= 0)
                return BadRequest();

            var resolution = await _commands.ResolveAsync(unitId, seqSeen, ct);
            return Ok(new
            {
                desiredMode = (byte)resolution.DesiredMode,
                commandSeq = resolution.CommandSeq,
                ttlSeconds = resolution.TtlSecondsRemaining,
                serverUtc = DateTime.UtcNow
            });
        }

        /* ------------------------------ operator ------------------------------ */

        public sealed record LiveCommandRequest(int UnitId);

        /// <summary>"Track Vehicle Live" (§5.3). TTL-bounded, concurrency-capped, audited.
        /// The UI shows "Live requested…" until the device acknowledges via its next batch.</summary>
        [Authorize]
        [HttpPost("command")]
        public async Task<IActionResult> RequestLive([FromBody] LiveCommandRequest request, CancellationToken ct)
        {
            if (request == null || request.UnitId <= 0)
                return BadRequest();
            if (OperatorUserId() is not { } userId)
                return Forbid();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (ok, error, command) = await _commands.RequestLiveAsync(request.UnitId, userId, ip, ct);
            if (!ok)
                return Conflict(new { error });

            return Ok(new
            {
                commandSeq = command!.CommandSeq,
                ttlSeconds = _options.LiveModeTtlSeconds,
                status = command.Status   // "Pending" until the device acks
            });
        }

        /// <summary>Ends Live Mode for a unit. Idempotent.</summary>
        [Authorize]
        [HttpDelete("command/{unitId:int}")]
        public async Task<IActionResult> CancelLive(int unitId, CancellationToken ct)
        {
            if (unitId <= 0)
                return BadRequest();
            if (OperatorUserId() is not { } userId)
                return Forbid();

            await _commands.CancelAsync(unitId, userId, "Cancelled",
                HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok();
        }

        /// <summary>Replay/history: a unit's trail for a bounded window. Every call is
        /// audited (§13.4) — who looked at whose movements is the first question in any
        /// workplace-surveillance dispute. Reads the point stream; this endpoint and
        /// evidentiary export are the ONLY readers of TrackPoint (§8.3).</summary>
        [Authorize]
        [HttpGet("history/{unitId:int}")]
        public async Task<IActionResult> History(int unitId, [FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (unitId <= 0 || toUtc <= fromUtc)
                return BadRequest();
            if ((toUtc - fromUtc) > TimeSpan.FromHours(26))
                return BadRequest("Window too large; request at most 26 hours (one shift with margin).");
            if (OperatorUserId() is not { } userId)
                return Forbid();

            _db.TrackingAccessAudits.Add(new Data.Entities.TrackingAccessAudit
            {
                UserId = userId,
                Action = "ViewHistory",
                UnitId = unitId,
                WindowFromUtc = fromUtc,
                WindowToUtc = toUtc,
                AccessedUtc = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _db.SaveChangesAsync(ct);

            const int cap = 5000;
            var points = await _db.TrackPoints
                .Where(p => p.UnitId == unitId && p.RecordedUtc >= fromUtc && p.RecordedUtc < toUtc)
                .OrderBy(p => p.RecordedUtc).ThenBy(p => p.Seq)
                .Take(cap + 1)
                .Select(p => new
                {
                    utc = p.RecordedUtc,
                    lat = p.Latitude,
                    lon = p.Longitude,
                    speedKph = p.SpeedKph,
                    headingDeg = p.HeadingDeg,
                    source = p.SourceType,
                    flags = p.Flags,
                    tag = p.AnchorTagUid
                })
                .ToListAsync(ct);

            var truncated = points.Count > cap;
            if (truncated)
                points.RemoveAt(points.Count - 1);

            /* Truncation is stated, never silent: a replay that quietly stops early would
               read as "the patrol stopped here". */
            return Ok(new { unitId, fromUtc, toUtc, truncated, points });
        }

        /// <summary>Segment roll-ups for reporting — the table every analytical consumer
        /// reads instead of the point stream (§8.3).</summary>
        [Authorize]
        [HttpGet("segments")]
        public async Task<IActionResult> Segments([FromQuery] int? unitId, [FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc, CancellationToken ct)
        {
            if (toUtc <= fromUtc)
                return BadRequest();

            var query = _db.TrackSegments.Where(s => s.StartUtc >= fromUtc && s.StartUtc < toUtc);
            if (unitId is { } unit)
                query = query.Where(s => s.UnitId == unit);

            var segments = await query
                .OrderBy(s => s.UnitId).ThenBy(s => s.StartUtc)
                .Take(2000)
                .Select(s => new
                {
                    s.UnitId,
                    s.SessionId,
                    s.StartUtc,
                    s.EndUtc,
                    s.DistanceM,
                    s.DurationSec,
                    s.MaxSpeedKph,
                    s.AvgSpeedKph,
                    s.PointCount,
                    s.AnchorScanCount,
                    s.AdherenceScore,
                    s.Flags
                })
                .ToListAsync(ct);

            return Ok(segments);
        }

        /// <summary>Live snapshot for the control room. Same-origin cookie-authenticated on
        /// both host apps; memory-fast in the ingest process, DB-backed in the control-room
        /// process (see LiveSnapshotService). Scope filtering arrives with the permission
        /// table (Phase 2).</summary>
        [Authorize]
        [HttpGet("live")]
        public async Task<IActionResult> Live(CancellationToken ct)
        {
            var units = await _snapshot.GetSnapshotAsync(ct);
            return Ok(new
            {
                serverUtc = DateTime.UtcNow,
                units = units.Select(u => new
                {
                    unitId = u.UnitId,
                    lat = u.Lat,
                    lon = u.Lon,
                    speedKph = u.SpeedKph,
                    headingDeg = u.HeadingDeg,
                    accuracyM = u.AccuracyM,
                    batteryPct = u.BatteryPct,
                    mode = u.Mode,
                    flags = u.Flags,
                    ageSeconds = u.AgeSeconds
                })
            });
        }
    }
}
