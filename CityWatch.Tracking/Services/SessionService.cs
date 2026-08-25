using System;
using System.Linq;
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
            CancellationToken ct, bool? isPatrolCar = null, string? callsign = null,
            int? positionId = null, string? positionName = null);

        /// <summary>
        /// An NFC scan landed. Updates where the car is and whether it is at a site or
        /// travelling. Does NOT start or stop GPS — sampling is continuous by design so a
        /// missed scan can never lose a journey.
        /// </summary>
        Task ApplyScanAsync(int unitId, int tagSiteId, string? tagSiteName, bool isInCarTag,
            DateTime occurredUtc, CancellationToken ct);

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
        private readonly Geofencing.ISiteArrivalDetector? _arrivals;
        private readonly ILogger<SessionService> _logger;
        private readonly Func<DateTime> _utcNow;

        public SessionService(TrackingDbContext db, ILiveStateStore liveState,
            ILogger<SessionService> logger, ISegmentBuilder? segments = null, Func<DateTime>? utcNow = null,
            Geofencing.ISiteArrivalDetector? arrivals = null)
        {
            _db = db;
            _liveState = liveState;
            _segments = segments;
            _arrivals = arrivals;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<TrackingSession?> StartAsync(int unitId, int guardId, int clientSiteId, int? pcarRouteId,
            CancellationToken ct, bool? isPatrolCar = null, string? callsign = null,
            int? positionId = null, string? positionName = null)
        {
            /* THE CALLSIGN NAMES THE CAR (P4 #153, 25 Aug 2026). Phones auto-restore a
               stale saved Position, so all six Romeo crews arrived keyed to the one old
               shared position and superseded each other off the map — twelve logins, one
               visible car. When a patrol-car login's callsign names a real car position
               ("R4" → "Mobile Patrols (Car) R4"), the SERVER re-keys the session to that
               car, whatever the phone sent. Ingest resolves by session id, so the phone's
               stale unit stamp keeps working; a callsign that names no car changes nothing. */
            if (isPatrolCar == true && !string.IsNullOrWhiteSpace(callsign))
            {
                var cs = callsign.Trim();
                var car = (await _db.PlatformPositions
                        .Where(p => p.IsPatrolCar && p.Name != null)
                        .Select(p => new { p.Id, p.Name })
                        .ToListAsync(ct))
                    .FirstOrDefault(p => p.Name!.TrimEnd()
                        .EndsWith(") " + cs, StringComparison.OrdinalIgnoreCase));
                if (car != null && Contracts.TrackingUnitKey.FromPosition(car.Id) != unitId)
                {
                    _logger.LogInformation(
                        "Session start: callsign {Callsign} re-keys unit {From} → {To} ({Car}).",
                        cs, unitId, Contracts.TrackingUnitKey.FromPosition(car.Id), car.Name);
                    unitId = Contracts.TrackingUnitKey.FromPosition(car.Id);
                    positionId = car.Id;
                    positionName = car.Name;
                }
            }

            var enrolment = await _db.TrackingUnitEnrolments.FirstOrDefaultAsync(e => e.UnitId == unitId, ct);
            if (enrolment is not { IsEnabled: true } || enrolment.ConsentRecordedUtc == null)
                return null;   // not enrolled / no consent — tracking simply does not start (§13.5)

            var now = _utcNow();

            /* One officer, one unit. The phone cannot close a session it never knew the
               key of once the server re-keys — any OTHER active session this guard holds
               is stale the moment they sign in again (crew moved cars, or an old
               guard-keyed fallback session waiting for the reaper). */
            var elsewhere = await _db.TrackingSessions
                .AsTracking()
                .Where(s => s.GuardId == guardId && s.Status == "Active" && s.UnitId != unitId)
                .ToListAsync(ct);
            foreach (var stale in elsewhere)
                await CloseAsync(stale, "OfficerChangedCar", now, ct);
            /* AsTracking: the same-officer branch below mutates this row, and the context
               defaults to NoTracking — without it the callsign/position refresh silently
               never persisted (the takeover branch was safe: CloseAsync calls Update). */
            var active = await _db.TrackingSessions
                .AsTracking()
                .FirstOrDefaultAsync(s => s.UnitId == unitId && s.Status == "Active", ct);

            if (active != null)
            {
                if (active.GuardId == guardId)
                {
                    /* Same officer re-opening (app restart, or re-login after changing the
                       car/callsign): keep the session but refresh the declarations. */
                    if (isPatrolCar.HasValue) active.IsPatrolCar = isPatrolCar;
                    if (!string.IsNullOrWhiteSpace(callsign)) active.Callsign = callsign;
                    if (positionId.HasValue) active.PatrolCarPositionId = positionId;
                    if (!string.IsNullOrWhiteSpace(positionName)) active.PatrolCarPositionName = positionName.Trim();
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
                Callsign = string.IsNullOrWhiteSpace(callsign) ? null : callsign.Trim(),
                PatrolCarPositionId = positionId,
                PatrolCarPositionName = string.IsNullOrWhiteSpace(positionName) ? null : positionName.Trim(),
                /* A shift starts with the car at its fleet base and about to leave. */
                TravelState = "Transit",
                TravelStateSinceUtc = now
            };
            _db.TrackingSessions.Add(session);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Tracking session {SessionId} started: unit {UnitId}, guard {GuardId}.",
                session.Id, unitId, guardId);
            return session;
        }

        public async Task ApplyScanAsync(int unitId, int tagSiteId, string? tagSiteName, bool isInCarTag,
            DateTime occurredUtc, CancellationToken ct)
        {
            /* AsTracking: NoTracking default made every arrival/departure below a silent
               no-op — TravelState never left "Transit" in the database, so the control room
               showed cars "in transit" while they stood scanned-in at a site (12 Aug). */
            var session = await _db.TrackingSessions
                .AsTracking()
                .FirstOrDefaultAsync(s => s.UnitId == unitId && s.Status == "Active", ct);
            if (session == null)
                return;

            if (isInCarTag)
            {
                /* Back in the vehicle: the car is leaving. Clear the current site and start
                   the transit leg. GPS is already running — this only labels the trail. */
                if (session.TravelState != "Transit")
                {
                    session.TravelState = "Transit";
                    session.TravelStateSinceUtc = occurredUtc;
                }
                session.CurrentSiteId = null;
                session.CurrentSiteName = null;
                _logger.LogInformation("Unit {Unit}: in-car scan — departing, now in transit.", unitId);
            }
            else
            {
                /* A site checkpoint: the car has arrived. Several tags get scanned per visit,
                   so only the FIRST one of a new site changes the state — later tags at the
                   same site just confirm presence. */
                var arrivedSomewhereNew = session.CurrentSiteId != tagSiteId;
                session.CurrentSiteId = tagSiteId;
                session.CurrentSiteName = tagSiteName;
                if (arrivedSomewhereNew || session.TravelState != "AtSite")
                {
                    session.TravelState = "AtSite";
                    session.TravelStateSinceUtc = occurredUtc;
                    _logger.LogInformation("Unit {Unit}: arrived at site {Site}.", unitId, tagSiteId);
                }
            }

            await _db.SaveChangesAsync(ct);

            /* The scan is also the strongest possible arrival evidence — a person put a phone
               against a tag on the site. It confirms the visit with no dwell window, and it is
               the only way a site with no coordinate on file can raise an arrival at all.
               Never allowed to fail the scan itself: a scan that reports "Tag Found" to the
               officer must not depend on the control room's notification feed. */
            if (_arrivals != null)
            {
                try
                {
                    await _arrivals.ApplyScanAsync(unitId, session.Id, tagSiteId, tagSiteName, isInCarTag, occurredUtc, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Site visit record failed for unit {Unit} scan at site {Site}.", unitId, tagSiteId);
                }
            }
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

            /* A stay cannot outlive the shift it happened in. Left open, the unit reads as
               "still at Martha Cove" tomorrow morning — the same class of stale-state noise
               the session reaper exists to prevent. */
            var openVisits = await _db.TrackingSiteVisits
                .AsTracking()
                .Where(v => v.SessionId == session.Id && v.ExitedUtc == null)
                .ToListAsync(ct);
            if (openVisits.Count > 0)
            {
                foreach (var visit in openVisits)
                    visit.ExitedUtc = now;
                await _db.SaveChangesAsync(ct);
            }

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
