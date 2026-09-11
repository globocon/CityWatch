using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    [TestClass]
    public class ModeCommandServiceTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 7, 0, 0, DateTimeKind.Utc);
        private const int Unit = 42;
        private const int Operator = 5;

        private TrackingDbContext _db = null!;
        private TrackingOptions _options = null!;
        private DateTime _clock;

        [TestInitialize]
        public async Task Setup()
        {
            /* NoTracking mirrors the production DI registration (ServiceCollectionExtensions).
               The 12 Aug field bug — commands stuck Pending forever because ResolveAsync/
               CancelAsync mutated untracked entities — was invisible to tests that ran with
               the tracking default. This fixture must fail if a service forgets AsTracking. */
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(), UnitId = Unit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-1), Status = "Active"
            });
            await _db.SaveChangesAsync();

            _options = new TrackingOptions { LiveModeTtlSeconds = 900, MaxConcurrentLiveUnits = 2 };
            _clock = Now;
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private ModeCommandService Service()
            => new(_db, _options, NullDomainEventPublisher.Instance,
                   NullLogger<ModeCommandService>.Instance, () => _clock);

        [TestMethod]
        public async Task RequestLive_IssuesTtlBoundedPendingCommand_AndAudits()
        {
            var (ok, _, command) = await Service().RequestLiveAsync(Unit, Operator, "10.0.0.1", CancellationToken.None);

            Assert.IsTrue(ok);
            Assert.AreEqual("Pending", command!.Status, "Pending until the device acks (§11.3 rule 5).");
            Assert.AreEqual(Now.AddSeconds(900), command.ExpiresUtc, "Live is always TTL-bounded.");
            var audit = await _db.TrackingAccessAudits.SingleAsync();
            Assert.AreEqual("CommandLive", audit.Action);
            Assert.AreEqual(Operator, audit.UserId);
            Assert.AreEqual("10.0.0.1", audit.IpAddress);
        }

        [TestMethod]
        public async Task RequestLive_WithoutActiveSession_IsRefused()
        {
            var session = await _db.TrackingSessions.AsTracking().SingleAsync();
            session.Status = "Completed";
            await _db.SaveChangesAsync();

            var (ok, error, _) = await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            Assert.IsFalse(ok);
            StringAssert.Contains(error, "no active patrol session");
        }

        [TestMethod]
        public async Task ConcurrencyCap_RefusesTheNthPlusOneUnit()
        {
            for (var unit = 100; unit < 102; unit++)   // fill the cap of 2 with other units
            {
                _db.TrackingSessions.Add(new TrackingSession { Id = Guid.NewGuid(), UnitId = unit, GuardId = unit, ClientSiteId = 1, StartedUtc = Now, Status = "Active" });
                await _db.SaveChangesAsync();
                var (ok, _, _) = await Service().RequestLiveAsync(unit, Operator, null, CancellationToken.None);
                Assert.IsTrue(ok);
            }

            var (refused, error, _) = await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            Assert.IsFalse(refused);
            StringAssert.Contains(error, "limit");
        }

        [TestMethod]
        public async Task Resolve_DeliversLive_ThenAckActivatesIt()
        {
            var (_, _, command) = await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            // Device polls with an old ack: sees the command, not yet acknowledged.
            var first = await Service().ResolveAsync(Unit, commandSeqSeen: 0, CancellationToken.None);
            Assert.AreEqual(TrackingMode.Live, first.DesiredMode);
            Assert.AreEqual(command!.CommandSeq, first.CommandSeq);
            Assert.IsNotNull(first.TtlSecondsRemaining);

            // Device reports it applied the command: status flips to Active, ack recorded.
            await Service().ResolveAsync(Unit, commandSeqSeen: command.CommandSeq, CancellationToken.None);
            var stored = await _db.TrackingModeCommands.SingleAsync(c => c.Id == command.Id);
            Assert.AreEqual("Active", stored.Status);
            Assert.IsNotNull(stored.AcknowledgedUtc);
        }

        [TestMethod]
        public async Task Resolve_ExpiresLapsedLive_AndRevertsToNormal()
        {
            await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            _clock = Now.AddSeconds(901);   // past the TTL
            var resolution = await Service().ResolveAsync(Unit, 0, CancellationToken.None);

            Assert.AreEqual(TrackingMode.Normal, resolution.DesiredMode,
                "A forgotten Live session reverts on its own (§5.3).");
            var stored = await _db.TrackingModeCommands.SingleAsync();
            Assert.AreEqual("Expired", stored.Status);
        }

        /* The truth table: raising duress inserts ClientSiteDuress rows; the control room
           deactivating the alarm deletes them. Guard 7 matches the session seeded in Setup. */
        private async Task AlarmOnAsync()
        {
            _db.PlatformClientSiteDuress.Add(new PlatformClientSiteDuress
            {
                ClientSiteId = 12, IsEnabled = true, EnabledBy = 7
            });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();   // seeding must not shadow the service's own reads
        }

        private async Task AlarmClearedAsync()
        {
            _db.PlatformClientSiteDuress.RemoveRange(await _db.PlatformClientSiteDuress.ToListAsync());
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
        }

        [TestMethod]
        public async Task Duress_OverridesLive_AndNeverExpires_WhileTheAlarmIsOn()
        {
            await AlarmOnAsync();
            await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            await Service().RequestDuressAsync(Unit, CancellationToken.None);

            var resolution = await Service().ResolveAsync(Unit, 0, CancellationToken.None);
            Assert.AreEqual(TrackingMode.Duress, resolution.DesiredMode);
            Assert.IsNull(resolution.TtlSecondsRemaining, "Duress has no TTL (§5.4).");
            var superseded = await _db.TrackingModeCommands.SingleAsync(c => c.DesiredMode == (byte)TrackingMode.Live);
            Assert.AreEqual("Superseded", superseded.Status);

            _clock = Now.AddHours(12);   // duress persists across a whole shift if uncancelled
            var later = await Service().ResolveAsync(Unit, 0, CancellationToken.None);
            Assert.AreEqual(TrackingMode.Duress, later.DesiredMode);
        }

        [TestMethod]
        public async Task Duress_StandsDown_WhenTheControlRoomDeactivatesTheAlarm()
        {
            await AlarmOnAsync();
            await Service().RequestDuressAsync(Unit, CancellationToken.None);
            var during = await Service().ResolveAsync(Unit, 0, CancellationToken.None);
            Assert.AreEqual(TrackingMode.Duress, during.DesiredMode);

            await AlarmClearedAsync();   // what the control room's deactivate actually does

            var after = await Service().ResolveAsync(Unit, 0, CancellationToken.None);
            Assert.AreEqual(TrackingMode.Normal, after.DesiredMode,
                "A cleared alarm must stand the device down on its next heartbeat.");
            var stored = await _db.TrackingModeCommands.SingleAsync();
            Assert.AreEqual("Cancelled", stored.Status);
            Assert.AreEqual("DuressCleared", stored.EndReason);
        }

        [TestMethod]
        public async Task Duress_StandsDown_WhenTheAlarmBelongsToAnotherGuard()
        {
            _db.PlatformClientSiteDuress.Add(new PlatformClientSiteDuress
            {
                ClientSiteId = 12, IsEnabled = true, EnabledBy = 999   // someone else's alarm
            });
            await _db.SaveChangesAsync();
            await Service().RequestDuressAsync(Unit, CancellationToken.None);

            var resolution = await Service().ResolveAsync(Unit, 0, CancellationToken.None);

            Assert.AreEqual(TrackingMode.Normal, resolution.DesiredMode,
                "Another guard's open alarm must not keep this unit in duress.");
        }

        [TestMethod]
        public async Task Live_CannotOverrideDuress()
        {
            await Service().RequestDuressAsync(Unit, CancellationToken.None);

            var (ok, error, _) = await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            Assert.IsFalse(ok, "Precedence: Duress > Live (§5.1).");
            StringAssert.Contains(error, "Duress");
        }

        [TestMethod]
        public async Task Duress_IsIdempotent()
        {
            await Service().RequestDuressAsync(Unit, CancellationToken.None);
            await Service().RequestDuressAsync(Unit, CancellationToken.None);

            Assert.AreEqual(1, await _db.TrackingModeCommands.CountAsync(),
                "Duress raised twice keeps one open command.");
        }

        [TestMethod]
        public async Task Cancel_EndsLive_AndAudits()
        {
            await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            await Service().CancelAsync(Unit, Operator, "Cancelled", "10.0.0.2", CancellationToken.None);

            var command = await _db.TrackingModeCommands.SingleAsync();
            Assert.AreEqual("Cancelled", command.Status);
            var resolution = await Service().ResolveAsync(Unit, 0, CancellationToken.None);
            Assert.AreEqual(TrackingMode.Normal, resolution.DesiredMode);
            Assert.AreEqual(2, await _db.TrackingAccessAudits.CountAsync(), "CommandLive + CommandCancel.");
        }

        [TestMethod]
        public async Task NewLiveRequest_SupersedesTheOldOne_WithAHigherSeq()
        {
            var (_, _, first) = await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);
            var (_, _, second) = await Service().RequestLiveAsync(Unit, Operator, null, CancellationToken.None);

            Assert.IsTrue(second!.CommandSeq > first!.CommandSeq, "Seq is monotonic per unit.");
            var old = await _db.TrackingModeCommands.SingleAsync(c => c.Id == first.Id);
            Assert.AreEqual("Superseded", old.Status);
        }
    }
}
