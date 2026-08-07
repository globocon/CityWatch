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
        private readonly ILogger<DuressHandler> _logger;
        private readonly Func<DateTime> _utcNow;

        public DuressHandler(TrackingDbContext db, ILogger<DuressHandler> logger, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
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

            var now = _utcNow();

            /* M1.8 replaces this direct insert with ModeCommandService (concurrency caps,
               ack tracking). The row shape is already final: Duress has no expiry, ever. */
            var lastSeq = await _db.TrackingModeCommands
                .Where(c => c.UnitId == session.UnitId)
                .MaxAsync(c => (int?)c.CommandSeq, ct) ?? 0;

            _db.TrackingModeCommands.Add(new TrackingModeCommand
            {
                UnitId = session.UnitId,
                CommandSeq = lastSeq + 1,
                DesiredMode = (byte)TrackingMode.Duress,
                IssuedByUserId = null,          // system-issued
                IssuedUtc = now,
                ExpiresUtc = null,              // duress never times out
                Status = "Pending"
            });
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("Duress: unit {Unit} commanded to Duress Mode (session {Session}).",
                session.UnitId, session.Id);
        }
    }
}
