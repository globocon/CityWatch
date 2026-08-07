using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly TrackingOptions _options;

        public TrackingController(IIngestService ingest, ISessionService sessions,
            ILiveSnapshotService snapshot, TrackingOptions options)
        {
            _ingest = ingest;
            _sessions = sessions;
            _snapshot = snapshot;
            _options = options;
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

        /* ------------------------------ operator ------------------------------ */

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
