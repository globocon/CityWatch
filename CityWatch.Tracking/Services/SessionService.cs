using System;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services
{
    public interface ISessionService
    {
        /// <summary>Opens a session if the unit is enrolled with consent; returns null otherwise.
        /// Idempotent per unit: an existing active session for the same guard is returned,
        /// a stale one for another guard is closed first (one active session per unit).</summary>
        Task<TrackingSession?> StartAsync(int unitId, int guardId, int clientSiteId, int? pcarRouteId,
            CancellationToken ct, bool? isPatrolCar = null, string? callsign = null);

        /// <summary>Closes the session and removes the unit from the live map. The hard stop.</summary>
        Task EndAsync(Guid sessionId, string endReason, CancellationToken ct);

        /// <summary>Closes whatever is active for a unit — the OfficerLoggedOut path, where the
        /// caller knows the unit but not the session.</summary>
        Task EndActiveForUnitAsync(int unitId, string endReason, CancellationToken ct);
    }

    public sealed class SessionService : ISessionService
    {
        private readonly TrackingDbContext _db;
        private readonly ILiveStateStore _liveState;
        private readonly ISegmentBuilder? _segments;
        private readonly ILogger<SessionService> _logger;
        private readonly Func<DateTime> _utcNow;

        public SessionService(TrackingDbContext db, ILiveStateStore liveState,
            ILogger<SessionService> logger, ISegmentBuilder? segments = null, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _liveState = liveState;
            _segments = segments;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<TrackingSession?> StartAsync(int unitId, int guardId, int clientSiteId, int? pcarRouteId,
            CancellationToken ct, bool? isPatrolCar = null, string? callsign = null)
        {
            var enrolment = await _db.TrackingUnitEnrolments.FirstOrDefaultAsync(e => e.UnitId == unitId, ct);
            if (enrolment is not { IsEnabled: true } || enrolment.ConsentRecordedUtc == null)
                return null;   // not enrolled / no consent — tracking simply does not start (§13.5)

            var now = _utcNow();
            var active = await _db.TrackingSessions
                .FirstOrDefaultAsync(s => s.UnitId == unitId && s.Status == "Active", ct);

            if (active != null)
            {
                if (active.GuardId == guardId)
                {
                    /* Same officer re-opening (app restart, or re-login after switching the
                       patrol-car toggle): keep the session but refresh the declarations. */
                    if (isPatrolCar.HasValue) active.IsPatrolCar = isPatrolCar;
                    if (!string.IsNullOrWhiteSpace(callsign)) active.Callsign = callsign;
                    await _db.SaveChangesAsync(ct);
                    return active;
                }

                /* Different officer on the same unit: the previous session ends now. Two open
                   sessions on one unit would make the evidentiary record ambiguous. */
                await CloseAsync(active, "SupersededByNewSession", now, ct);
            }

            var session = new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = unitId,
                GuardId = guardId,
                ClientSiteId = clientSiteId,
                PcarRouteId = pcarRouteId,
                StartedUtc = now,
                Status = "Active",
                IsPatrolCar = isPatrolCar,
                Callsign = string.IsNullOrWhiteSpace(callsign) ? null : callsign.Trim()
            };
            _db.TrackingSessions.Add(session);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Tracking session {SessionId} started: unit {UnitId}, guard {GuardId}.",
                session.Id, unitId, guardId);
            return session;
        }

        public async Task EndAsync(Guid sessionId, string endReason, CancellationToken ct)
        {
            var session = await _db.TrackingSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.Status == "Active", ct);
            if (session == null)
                return;   // already closed — ending twice is a no-op, not an error

            await CloseAsync(session, endReason, _utcNow(), ct);
        }

        public async Task EndActiveForUnitAsync(int unitId, string endReason, CancellationToken ct)
        {
            var session = await _db.TrackingSessions
                .FirstOrDefaultAsync(s => s.UnitId == unitId && s.Status == "Active", ct);
            if (session == null)
                return;

            await CloseAsync(session, endReason, _utcNow(), ct);
        }

        private async Task CloseAsync(TrackingSession session, string endReason, DateTime now, CancellationToken ct)
        {
            session.Status = endReason == "Reaper" ? "Expired" : "Completed";
            session.EndedUtc = now;
            session.EndReason = endReason;
            _db.TrackingSessions.Update(session);
            await _db.SaveChangesAsync(ct);

            /* Off the map immediately: the control room must not show an off-shift vehicle,
               and the officer's tracking visibly stops with the session (§13.5). */
            _liveState.Remove(session.UnitId);

            _logger.LogInformation("Tracking session {SessionId} closed ({Reason}): unit {UnitId}.",
                session.Id, endReason, session.UnitId);

            /* Roll-up is derived data: a failure here must never fail the close, and a missed
               build is recoverable (the nightly sweep re-runs it — Phase 2 hardening). */
            if (_segments != null)
            {
                try
                {
                    await _segments.BuildForSessionAsync(session.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Segment roll-up failed for session {SessionId}; points remain intact.", session.Id);
                }
            }
        }
    }
}
