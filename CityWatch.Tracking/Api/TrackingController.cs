using System;
using System.Collections.Generic;
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
        private readonly IDeviceTokenService _deviceTokens;
        private readonly ITrackingPingService _ping;
        private readonly Data.TrackingDbContext _db;
        private readonly TrackingOptions _options;

        public TrackingController(IIngestService ingest, ISessionService sessions,
            ILiveSnapshotService snapshot, IModeCommandService commands,
            IDeviceTokenService deviceTokens, ITrackingPingService ping,
            Data.TrackingDbContext db, TrackingOptions options)
        {
            _ingest = ingest;
            _sessions = sessions;
            _snapshot = snapshot;
            _commands = commands;
            _deviceTokens = deviceTokens;
            _ping = ping;
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

        /// <summary>All the login-screen declarations. PositionId/PositionName identify the
        /// CAR ("Mobile Patrols (Car) M1") — the tracked unit's real identity.</summary>
        public sealed record StartSessionRequest(int UnitId, int GuardId, int ClientSiteId, int? PcarRouteId,
            bool? IsPatrolCar = null, string? Callsign = null,
            int? PositionId = null, string? PositionName = null);

        /// <summary>Opens a patrol session. Returns 403 when the unit is not enrolled with
        /// consent — the device shows nothing and simply does not track (§13.5).</summary>
        [HttpPost("session/start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken ct)
        {
            if (request == null || request.UnitId <= 0 || request.GuardId <= 0)
                return BadRequest();

            var session = await _sessions.StartAsync(request.UnitId, request.GuardId,
                request.ClientSiteId, request.PcarRouteId, ct, request.IsPatrolCar, request.Callsign,
                request.PositionId, request.PositionName);
            if (session == null)
            {
                /* Explicit 403, never Forbid(): under cookie auth Forbid() redirects to
                   /Account/AccessDenied, which does not exist — the caller gets a 404 and
                   an HTML page instead of a machine-readable refusal. Device endpoints must
                   answer devices, not browsers. */
                return StatusCode(403, new
                {
                    error = "Unit is not enrolled for tracking, or consent has not been recorded.",
                    unitId = request.UnitId
                });
            }

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
                return StatusCode(403, new { error = "Operator identity not found on the session." });

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
                return StatusCode(403, new { error = "Operator identity not found on the session." });

            await _commands.CancelAsync(unitId, userId, "Cancelled",
                HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok();
        }

        /// <summary>Units sitting in one place too long (§ idle detection). Operator-attention
        /// surface: "who has been at the same spot for 20 minutes?" Duress units excluded.</summary>
        [Authorize]
        [HttpGet("idle")]
        public async Task<IActionResult> Idle([FromQuery] int? minutes,
            [FromServices] IIdleDetectionService idleDetection, CancellationToken ct)
        {
            var threshold = TimeSpan.FromMinutes(
                minutes is > 0 and <= 24 * 60 ? minutes.Value : _options.IdleThresholdMinutes);

            var idle = await idleDetection.GetIdleUnitsAsync(threshold, ct);
            return Ok(new
            {
                thresholdMinutes = (int)threshold.TotalMinutes,
                units = idle.Select(u => new
                {
                    unitId = u.UnitId,
                    kind = u.Kind,
                    callsign = u.Callsign,
                    guardId = u.GuardId,
                    guardName = u.GuardName,
                    lat = u.Lat,
                    lon = u.Lon,
                    idleSinceUtc = u.IdleSinceUtc,
                    idleMinutes = u.IdleMinutes
                })
            });
        }

        /* ---- FCM device tokens + push nudge. FCM is the accelerator; the ingest response
           is the guarantee: nothing below ever claims a position arrived. ---- */

        public sealed record RegisterDeviceTokenRequest(int UnitId, Guid SessionId, string Token, string? Platform);

        /// <summary>Device endpoint: registers the phone's FCM token under the same trust
        /// model as ingest — the (unit, session) pair must name the ACTIVE session, so a
        /// device can only register a token for the unit it is signed into (§13.1.1).</summary>
        [HttpPost("device-token")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request,
            CancellationToken ct)
        {
            if (request == null || request.UnitId <= 0 || request.SessionId == Guid.Empty
                || string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 512)
                return BadRequest();

            var registered = await _deviceTokens.RegisterAsync(request.UnitId, request.SessionId,
                request.Token.Trim(), request.Platform, ct);
            if (!registered)
                return StatusCode(403, new { error = "No active session for this unit; token not registered." });
            return Ok();
        }

        public sealed record ReleaseDeviceTokenRequest(string Token);

        /// <summary>Logout path. Holding the token IS the credential — only the device that
        /// owns it can release it. Idempotent: releasing twice is a no-op, always 200.</summary>
        [HttpPost("device-token/release")]
        public async Task<IActionResult> ReleaseDeviceToken([FromBody] ReleaseDeviceTokenRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token))
                return BadRequest();

            await _deviceTokens.ReleaseAsync(request.Token.Trim(), ct);
            return Ok();
        }

        /// <summary>Operator nudge (the 📳 button): asks a unit's phone for a fresh position
        /// via FCM. 202 means the nudge was SENT — never that a position arrived; the fresh
        /// fix shows up through the normal ingest path or not at all. Refusals are explicit
        /// and machine-readable: a button must never silently do nothing.</summary>
        [Authorize]
        [HttpPost("ping/{unitId:int}")]
        public async Task<IActionResult> Ping(int unitId, CancellationToken ct)
        {
            if (unitId <= 0)
                return BadRequest();
            if (OperatorUserId() is not { } userId)
                return StatusCode(403, new { error = "Operator identity not found on the session." });

            var result = await _ping.PingAsync(unitId, userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(), "ManualPing", ct);
            return result.Status switch
            {
                PingStatus.NotConfigured => StatusCode(409, new
                { error = "Push is not configured on this server (Tracking:Fcm:ServiceAccountJsonPath)." }),
                PingStatus.NoTokens => StatusCode(409, new
                { error = "This unit's phone has not registered for push — it may be running an older app build." }),
                PingStatus.Cooldown => StatusCode(429, new
                { error = $"Pinged less than {_options.Fcm.PingCooldownSeconds}s ago — still waiting on the device." }),
                _ => StatusCode(202, new { requestId = result.RequestId, tokensPinged = result.TokensPinged })
            };
        }

        /* ---- Custom messages (the ✉ button): operator text to a unit's phone, or to every
           online unit of a kind. Same discipline as ping: 202 means FCM ACCEPTED the message,
           never that anyone read it, and refusals are explicit and machine-readable. ---- */

        public sealed record UnitMessageRequest(string Message);

        public sealed record BroadcastMessageRequest(string Message, string? Kind);

        private static readonly string[] BroadcastKinds = { "all", "car", "guard" };

        /// <summary>Read-only map viewers (?key=) carry Sid "0" — authenticated enough to
        /// watch, never enough to speak to an officer's phone. Applied to the message
        /// endpoints only; ping's existing behavior is unchanged.</summary>
        private IActionResult? RefuseUnlessMessagingOperator(out int userId)
        {
            if (OperatorUserId() is not { } id || id <= 0)
            {
                userId = 0;
                return StatusCode(403, new
                { error = "Messaging needs an operator sign-in (read-only map view cannot send)." });
            }
            userId = id;
            return null;
        }

        [Authorize]
        [HttpPost("message/{unitId:int}")]
        public async Task<IActionResult> Message(int unitId, [FromBody] UnitMessageRequest request,
            [FromServices] ITrackingMessageService messages, CancellationToken ct)
        {
            if (unitId <= 0 || request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest();
            if (request.Message.Trim().Length > TrackingMessageService.MaxMessageLength)
                return BadRequest(new
                { error = $"Message too long ({TrackingMessageService.MaxMessageLength} characters max)." });
            if (RefuseUnlessMessagingOperator(out var userId) is { } refusal)
                return refusal;

            var result = await messages.SendToUnitAsync(unitId, request.Message, userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return result.Status switch
            {
                TrackingMessageStatus.InvalidMessage => BadRequest(new
                { error = $"Message must be 1–{TrackingMessageService.MaxMessageLength} characters." }),
                TrackingMessageStatus.NotConfigured => StatusCode(409, new
                { error = "Push is not configured on this server (Tracking:Fcm:ServiceAccountJsonPath)." }),
                TrackingMessageStatus.NoTokens => StatusCode(409, new
                { error = "This unit's phone has not registered for push — it may be running an older app build." }),
                TrackingMessageStatus.Cooldown => StatusCode(429, new
                { error = $"Messaged less than {_options.Fcm.MessageCooldownSeconds}s ago — give the phone a moment." }),
                _ => StatusCode(202, new { requestId = result.RequestId, tokensSent = result.TokensSent })
            };
        }

        [Authorize]
        [HttpPost("message/broadcast")]
        public async Task<IActionResult> BroadcastMessage([FromBody] BroadcastMessageRequest request,
            [FromServices] ITrackingMessageService messages, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest();
            if (request.Message.Trim().Length > TrackingMessageService.MaxMessageLength)
                return BadRequest(new
                { error = $"Message too long ({TrackingMessageService.MaxMessageLength} characters max)." });
            var kind = string.IsNullOrWhiteSpace(request.Kind) ? "all" : request.Kind.Trim().ToLowerInvariant();
            if (!BroadcastKinds.Contains(kind))
                return BadRequest(new { error = "Kind must be one of: all, car, guard." });
            if (RefuseUnlessMessagingOperator(out var userId) is { } refusal)
                return refusal;

            var result = await messages.BroadcastAsync(kind, request.Message, userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return result.Status switch
            {
                TrackingMessageStatus.InvalidMessage => BadRequest(new
                { error = $"Message must be 1–{TrackingMessageService.MaxMessageLength} characters." }),
                TrackingMessageStatus.NotConfigured => StatusCode(409, new
                { error = "Push is not configured on this server (Tracking:Fcm:ServiceAccountJsonPath)." }),
                TrackingMessageStatus.NoRecipients => StatusCode(409, new
                { error = "No online units match that filter right now." }),
                _ => StatusCode(202, new
                {
                    requestId = result.RequestId,
                    unitsTargeted = result.UnitsTargeted,
                    unitsSent = result.UnitsSent,
                    tokensSent = result.TokensSent,
                    unitsSkippedNoToken = result.UnitsSkippedNoToken,
                    unitsSkippedCooldown = result.UnitsSkippedCooldown
                })
            };
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
                return StatusCode(403, new { error = "Operator identity not found on the session." });

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
                    sessionId = p.SessionId,
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

            /* Grouped BY SESSION, never returned as one flat stream. A unit id is a car, and
               a car changes hands: consecutive sessions in one window are different officers'
               movements, and a client drawing one line through both invents a journey nobody
               made (the Cochin↔Poonjar defect). The session boundary is the truth boundary. */
            var sessionIds = points.Select(p => p.sessionId).Distinct().ToList();
            var sessions = await _db.TrackingSessions
                .Where(s => sessionIds.Contains(s.Id))
                .Select(s => new
                {
                    s.Id, s.GuardId, s.StartedUtc, s.EndedUtc,
                    s.Callsign, s.PatrolCarPositionName
                })
                .ToListAsync(ct);
            var guardIds = sessions.Select(s => s.GuardId).Distinct().ToList();
            var guardNames = await _db.PlatformGuards
                .Where(g => guardIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name, ct);
            var sessionById = sessions.ToDictionary(s => s.Id);

            var grouped = points
                .GroupBy(p => p.sessionId)
                .Select(g =>
                {
                    sessionById.TryGetValue(g.Key, out var s);
                    var raw = g.ToList();

                    /* Speed fallback (§2.3): where the device sent no speed, derive it from
                       consecutive fixes — sane interval, plausible result, marked derived. */
                    var outPoints = new List<object>(raw.Count);
                    for (var i = 0; i < raw.Count; i++)
                    {
                        var speed = raw[i].speedKph;
                        var derived = false;
                        if (speed == null && i > 0)
                        {
                            var dtSec = (raw[i].utc - raw[i - 1].utc).TotalSeconds;
                            if (dtSec is >= 3 and <= 180)
                            {
                                var implied = Services.GeoMath.ImpliedSpeedKph(
                                    raw[i - 1].lat, raw[i - 1].lon, raw[i].lat, raw[i].lon, dtSec / 3600.0);
                                if (implied <= _options.PlausibilityMaxSpeedKph)
                                {
                                    speed = (short)Math.Round(implied);
                                    derived = true;
                                }
                            }
                        }
                        outPoints.Add(new
                        {
                            raw[i].utc, raw[i].lat, raw[i].lon,
                            speedKph = speed, speedDerived = derived,
                            raw[i].headingDeg, raw[i].source, raw[i].flags, raw[i].tag
                        });
                    }

                    /* Historical stops (§2.2): where the journey paused, jitter ignored. */
                    var stops = Services.StopDetector.Detect(
                            raw.Select(p => new Services.StopDetector.TrailPoint(p.lat, p.lon, p.utc)).ToList())
                        .Select(st => new
                        {
                            lat = st.Lat, lon = st.Lon,
                            fromUtc = st.FromUtc, toUtc = st.ToUtc,
                            durationMinutes = st.DurationMinutes
                        })
                        .ToList();

                    return new
                    {
                        sessionId = g.Key,
                        guardId = s?.GuardId ?? 0,
                        guardName = s != null && guardNames.TryGetValue(s.GuardId, out var name) ? name : null,
                        callsign = s?.Callsign,
                        patrolCar = s?.PatrolCarPositionName,
                        startedUtc = s?.StartedUtc,
                        endedUtc = s?.EndedUtc,
                        firstUtc = raw[0].utc,
                        points = outPoints,
                        stops
                    };
                })
                .OrderBy(s => s.startedUtc ?? s.firstUtc)
                .ToList();

            /* Truncation is stated, never silent: a replay that quietly stops early would
               read as "the patrol stopped here". */
            return Ok(new { unitId, fromUtc, toUtc, truncated, sessions = grouped });
        }

        /// <summary>Short street address for coordinates ("Main Road, Pala"), cache-first
        /// (§Phase 2.1). Null is a normal answer — the UI falls back to site/coordinates,
        /// and the map never depends on the geocoder being up.</summary>
        [Authorize]
        [HttpGet("address")]
        public async Task<IActionResult> Address([FromQuery] decimal lat, [FromQuery] decimal lon,
            [FromServices] Services.Geocoding.IGeocodeService geocode, CancellationToken ct)
        {
            if (lat is < -90 or > 90 || lon is < -180 or > 180)
                return BadRequest();

            var address = await geocode.GetAddressAsync(lat, lon, ct);
            return Ok(new { address });
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
                    /* Session identity travels with the unit so the client can reset a trail
                       the moment a unit changes hands (a trail that survives a takeover would
                       stitch two officers' movements into one line — the replay bug, live). */
                    sessionId = u.SessionId,
                    sessionStartedUtc = u.SessionStartedUtc,
                    kind = u.Kind,
                    callsign = u.Callsign,
                    guardId = u.GuardId,
                    guardName = u.GuardName,
                    patrolCar = u.PatrolCar,
                    travelState = u.TravelState,
                    currentSite = u.CurrentSiteName,
                    stateMinutes = u.StateMinutes,
                    lat = u.Lat,
                    lon = u.Lon,
                    speedKph = u.SpeedKph,
                    speedDerived = u.SpeedDerived,
                    headingDeg = u.HeadingDeg,
                    accuracyM = u.AccuracyM,
                    batteryPct = u.BatteryPct,
                    mode = u.Mode,
                    flags = u.Flags,
                    ageSeconds = u.AgeSeconds
                })
            });
        }

        /// <summary>Confirmed site arrivals for the bell (§5.1). Server-recorded, so the feed
        /// survives a page refresh, is identical on every operator's screen, and captures
        /// arrivals that happened while no browser was open. Candidates that never dwelled
        /// (drive-pasts) are stored but never served.</summary>
        [Authorize]
        [HttpGet("arrivals")]
        public async Task<IActionResult> Arrivals([FromQuery] int? hours,
            [FromServices] Services.Geofencing.ISiteArrivalFeed feed, CancellationToken ct)
        {
            var arrivals = await feed.GetRecentAsync(hours, ct);
            return Ok(new
            {
                serverUtc = DateTime.UtcNow,
                arrivals = arrivals.Select(a => new
                {
                    id = a.Id,
                    unitId = a.UnitId,
                    kind = a.Kind,
                    label = a.Label,
                    guardName = a.GuardName,
                    siteId = a.SiteId,
                    siteName = a.SiteName,
                    enteredUtc = a.EnteredUtc,
                    confirmedUtc = a.ConfirmedUtc,
                    exitedUtc = a.ExitedUtc,
                    stillOnSite = a.StillOnSite,
                    minutesOnSite = a.MinutesOnSite,
                    source = a.Source
                })
            });
        }
    }
}
