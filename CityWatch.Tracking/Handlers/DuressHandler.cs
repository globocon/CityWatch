using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Handlers
{
    /// <summary>
    /// Observes duress — never gates it (§4.5). The platform's duress path (alerts, email,
    /// SMS, the control-room banner) has already fired before this handler runs; tracking's
    /// contribution is pinning the position and, from M1.8, escalating the device to
    /// Duress Mode via the command channel.
    /// </summary>
    public sealed class DuressHandler : IDomainEventHandler<DuressActivated>
    {
        private readonly TrackingDbContext _db;
        private readonly Services.IModeCommandService _commands;
        private readonly ILogger<DuressHandler> _logger;

        public DuressHandler(TrackingDbContext db, Services.IModeCommandService commands,
            ILogger<DuressHandler> logger)
        {
            _db = db;
            _commands = commands;
            _logger = logger;
        }

        public async Task HandleAsync(DuressActivated e, CancellationToken ct)
        {
            /* Resolve the unit: the duress path knows the guard and site, not the wand.
               An active session for the guard is the association that matters. */
            var session = await _db.TrackingSessions
                .FirstOrDefaultAsync(s => s.Status == "Active" &&
                    (e.SmartWandId != null ? s.UnitId == e.SmartWandId : s.GuardId == e.GuardId), ct);

            if (session == null)
            {
                _logger.LogInformation(
                    "Duress at site {Site} (guard {Guard}) has no active tracking session; platform duress path unaffected.",
                    e.ClientSiteId, e.GuardId);
                return;
            }

            await _commands.RequestDuressAsync(session.UnitId, ct);
        }
    }
}
