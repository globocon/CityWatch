using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Hosted;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The reaper closes ABANDONED sessions (no point received for StaleAfterHours) and
    /// nothing else. The line it must never cross: a unit that is merely quiet mid-shift —
    /// or on a long shift with a live phone — stays on the map, because "findable when
    /// dark" is a field-test mandate, not an accident.
    /// </summary>
    [TestClass]
    public class SessionReaperTests
    {
        private static readonly DateTime Now = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TrackingOptions.ReaperOptions Options = new();   // 12h default

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _live = null!;

        [TestInitialize]
        public void Setup()
        {
            /* NoTracking mirrors production (the 12 Aug lesson): the sweep must fail here
               if the close path ever loses its mutation-safe pattern. */
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
            _live = new InMemoryLiveStateStore();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private SessionService Sessions()
            => new(_db, _live, NullLogger<SessionService>.Instance, segments: null, utcNow: () => Now);

        private Task<int> SweepAsync()
            => SessionReaper.SweepAsync(_db, Sessions(), Now, Options,
                NullLogger.Instance, CancellationToken.None);

        private async Task<TrackingSession> SeedSessionAsync(int unitId, DateTime startedUtc,
            DateTime? lastPointUtc = null)
        {
            var session = new TrackingSession
            {
                Id = Guid.NewGuid(), UnitId = unitId, GuardId = unitId, ClientSiteId = 1,
                StartedUtc = startedUtc, Status = "Active",
                TravelState = "Transit", TravelStateSinceUtc = startedUtc
            };
            _db.TrackingSessions.Add(session);
            if (lastPointUtc.HasValue)
                _db.TrackPoints.Add(new TrackPoint
                {
                    UnitId = unitId, SessionId = session.Id, Seq = 1,
                    RecordedUtc = lastPointUtc.Value, ReceivedUtc = lastPointUtc.Value,
                    Latitude = -37.8m, Longitude = 144.9m
                });
            await _db.SaveChangesAsync();
            /* Production's reaper scope starts with an empty change tracker; seeding must
               not leave tracked instances behind to collide with the service's Update(). */
            _db.ChangeTracker.Clear();
            return session;
        }

        [TestMethod]
        public async Task AbandonedSession_NoPointsAndOld_IsExpired()
        {
            var stale = await SeedSessionAsync(1000002, Now.AddHours(-15));

            var swept = await SweepAsync();

            Assert.AreEqual(1, swept);
            var row = await _db.TrackingSessions.SingleAsync(s => s.Id == stale.Id);
            Assert.AreEqual("Expired", row.Status);
            Assert.AreEqual("Reaper", row.EndReason);
            Assert.AreEqual(Now, row.EndedUtc);
        }

        [TestMethod]
        public async Task AbandonedSession_OnlyStalePoints_IsExpired()
        {
            await SeedSessionAsync(1000002, Now.AddHours(-20), lastPointUtc: Now.AddHours(-15));

            Assert.AreEqual(1, await SweepAsync());
        }

        [TestMethod]
        public async Task LongShift_WithRecentPoint_Survives()
        {
            /* A 20-hour login with a phone that reported 5 minutes ago is a long shift,
               not an abandoned session. */
            var session = await SeedSessionAsync(2000010, Now.AddHours(-20), lastPointUtc: Now.AddMinutes(-5));

            Assert.AreEqual(0, await SweepAsync());
            Assert.AreEqual("Active", (await _db.TrackingSessions.SingleAsync(s => s.Id == session.Id)).Status);
        }

        [TestMethod]
        public async Task FreshSession_NoPointsYet_Survives()
        {
            /* Just logged in, GPS still warming up: nothing to reap. */
            await SeedSessionAsync(2000010, Now.AddMinutes(-30));

            Assert.AreEqual(0, await SweepAsync());
        }

        [TestMethod]
        public async Task QuietMidShiftUnit_InsideTheWindow_Survives()
        {
            /* Dark for 3 hours (dead battery, no signal) — inside the 12h window this unit
               MUST stay: the map keeps it findable, the reaper keeps its hands off. */
            await SeedSessionAsync(2000010, Now.AddHours(-6), lastPointUtc: Now.AddHours(-3));

            Assert.AreEqual(0, await SweepAsync());
        }

        [TestMethod]
        public async Task ExpiredUnit_LeavesTheLiveMap()
        {
            var stale = await SeedSessionAsync(1000002, Now.AddHours(-15));
            _live.Update(new UnitLiveState
            {
                UnitId = stale.UnitId, SessionId = stale.Id,
                Lat = -37.8m, Lon = 144.9m, ReceivedUtc = Now.AddHours(-15)
            });
            Assert.IsNotNull(_live.Get(stale.UnitId));

            await SweepAsync();

            Assert.IsNull(_live.Get(stale.UnitId));   // §13.5: off the map immediately
        }

        [TestMethod]
        public async Task ClosedSessions_AreNeverTouched()
        {
            var done = await SeedSessionAsync(1000002, Now.AddHours(-30));
            var row = await _db.TrackingSessions.AsTracking().SingleAsync(s => s.Id == done.Id);
            row.Status = "Completed";
            row.EndReason = "OfficerLoggedOut";
            await _db.SaveChangesAsync();

            Assert.AreEqual(0, await SweepAsync());
            var after = await _db.TrackingSessions.SingleAsync(s => s.Id == done.Id);
            Assert.AreEqual("OfficerLoggedOut", after.EndReason);   // untouched, not re-reaped
        }

        /* ---- duress reconcile: the sweep for the phone that cannot heartbeat. ---- */

        private async Task<TrackingModeCommand> SeedDuressCommandAsync(int unitId)
        {
            var command = new TrackingModeCommand
            {
                UnitId = unitId, CommandSeq = 1, DesiredMode = 4 /* Duress */,
                IssuedUtc = Now.AddHours(-26), Status = "Active"
            };
            _db.TrackingModeCommands.Add(command);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
            return command;
        }

        private Task<int> ReconcileAsync()
            => SessionReaper.ReconcileDuressAsync(_db, NullLogger.Instance, CancellationToken.None);

        [TestMethod]
        public async Task DeactivatedAlarm_OfflinePhone_CommandIsStoodDown()
        {
            /* The 18 Aug live incident: duress cleared in the control room yesterday
               (ClientSiteDuress rows deleted), phone never heartbeated again, command
               sat Active for 24h+ and the map flashed a dead alarm all day. */
            await SeedSessionAsync(2000010, Now.AddHours(-20), lastPointUtc: Now.AddMinutes(-5));
            var command = await SeedDuressCommandAsync(2000010);

            Assert.AreEqual(1, await ReconcileAsync());
            var row = await _db.TrackingModeCommands.SingleAsync(c => c.Id == command.Id);
            Assert.AreEqual("Cancelled", row.Status);
            Assert.AreEqual("DuressCleared", row.EndReason);
        }

        [TestMethod]
        public async Task LiveAlarm_IsNeverTouchedByTheReconcile()
        {
            var session = await SeedSessionAsync(2000010, Now.AddHours(-2), lastPointUtc: Now.AddMinutes(-5));
            _db.PlatformClientSiteDuress.Add(new PlatformClientSiteDuress
            {
                ClientSiteId = 1, IsEnabled = true, EnabledBy = session.GuardId
            });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
            var command = await SeedDuressCommandAsync(2000010);

            Assert.AreEqual(0, await ReconcileAsync());
            var row = await _db.TrackingModeCommands.SingleAsync(c => c.Id == command.Id);
            Assert.AreEqual("Active", row.Status, "A backed alarm is an emergency, not a leak.");
        }
    }
}
