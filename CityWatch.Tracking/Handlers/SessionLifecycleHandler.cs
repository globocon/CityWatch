using System.Threading;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using CityWatch.Tracking.Services;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Handlers
{
    /// <summary>
    /// Binds tracking sessions to the platform's own lifecycle events (§20.3).
    ///
    /// The session spans the SHIFT, not the visit: it opens on officer login (or the first
    /// patrol start, whichever the pack observes first) and closes ONLY on officer logout.
    /// A completed visit is a leg boundary for the segment builder (M1.9), never a reason
    /// to stop tracking — the vehicle is still on the road between visits, which is exactly
    /// the part GPS exists to prove.
    /// </summary>
    public sealed class SessionLifecycleHandler :
        IDomainEventHandler<OfficerLoggedIn>,
        IDomainEventHandler<OfficerLoggedOut>,
        IDomainEventHandler<PatrolStarted>,
        IDomainEventHandler<PatrolEnded>
    {
        private readonly ISessionService _sessions;
        private readonly ILogger<SessionLifecycleHandler> _logger;

        public SessionLifecycleHandler(ISessionService sessions, ILogger<SessionLifecycleHandler> logger)
        {
            _sessions = sessions;
            _logger = logger;
        }

        public async Task HandleAsync(OfficerLoggedIn e, CancellationToken ct)
        {
            if (e.SmartWandId is not { } unitId)
                return;   // no wand allocation, no tracking unit

            /* StartAsync gates on enrolment + consent and is idempotent per unit+guard,
               so an unenrolled unit or an app restart both land safely. */
            await _sessions.StartAsync(unitId, e.GuardId, e.ClientSiteId, null, ct);
        }

        public async Task HandleAsync(OfficerLoggedOut e, CancellationToken ct)
        {
            if (e.SmartWandId is not { } unitId)
                return;

            /* The hard stop (§13.5). Whatever else is happening — Live Mode, an open leg —
               tracking ceases with the shift. */
            await _sessions.EndActiveForUnitAsync(unitId, "OfficerLogout", ct);
        }

        public async Task HandleAsync(PatrolStarted e, CancellationToken ct)
        {
            if (e.GuardId is not { } guardId)
            {
                _logger.LogDebug("PatrolStarted for unit {Unit} without a guard id; session unchanged.", e.SmartWandId);
                return;
            }

            await _sessions.StartAsync(e.SmartWandId, guardId, e.ClientSiteId, e.PcarRouteId, ct);
        }

        public Task HandleAsync(PatrolEnded e, CancellationToken ct)
        {
            /* Deliberately does not close the session — see class remarks. The segment
               builder (M1.9) subscribes to this same event for leg roll-up. */
            _logger.LogDebug("PatrolEnded ({Reason}) for unit {Unit}; session remains open until logout.",
                e.Reason, e.SmartWandId);
            return Task.CompletedTask;
        }
    }
}
