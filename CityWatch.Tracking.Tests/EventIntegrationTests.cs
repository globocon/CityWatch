using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Handlers;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    [TestClass]
    public class EventIntegrationTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 5, 0, 0, DateTimeKind.Utc);
        private const int Unit = 42;

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _liveState = null!;
        private Channel<TrackPoint> _channel = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = Unit,
                IsEnabled = true,
                EnrolledUtc = Now.AddDays(-10),
                EnrolledByUserId = 1,
                ConsentRecordedUtc = Now.AddDays(-10)
            });
            await _db.SaveChangesAsync();

            _liveState = new InMemoryLiveStateStore();
            _channel = Channel.CreateBounded<TrackPoint>(100);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private SessionService Sessions()
            => new(_db, _liveState, NullLogger<SessionService>.Instance, segments: null, utcNow: () => Now);

        private NfcAnchorHandler AnchorHandler()
            => new(_db, _liveState, Sessions(), _channel.Writer, NullLogger<NfcAnchorHandler>.Instance);

        private SessionLifecycleHandler Lifecycle()
            => new(Sessions(), NullLogger<SessionLifecycleHandler>.Instance);

        /* ---------------- registration (flag on/off) ---------------- */

        [TestMethod]
        public void FlagOn_RegistersAllSixEventSubscriptions_AndTheRealPublisher()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddCityWatchTracking(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Tracking:Enabled"] = "true",
                    ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;"
                }).Build());
            using var provider = services.BuildServiceProvider();

            Assert.IsInstanceOfType(provider.GetRequiredService<IDomainEventPublisher>(),
                typeof(ChannelDomainEventPublisher), "Subscribing must activate the real bus.");
            using var scope = provider.CreateScope();
            Assert.IsNotNull(scope.ServiceProvider.GetService<IDomainEventHandler<NfcCheckpointScanned>>());
            Assert.IsNotNull(scope.ServiceProvider.GetService<IDomainEventHandler<OfficerLoggedOut>>());
            Assert.IsNotNull(scope.ServiceProvider.GetService<IDomainEventHandler<DuressActivated>>());
        }

        [TestMethod]
        public void FlagOff_LeavesTheNullPublisher_SoPublishSitesAreNoOps()
        {
            // RT3/RT4: this is what makes the ~20 lines added to production files inert.
            var services = new ServiceCollection();
            services.AddDomainEvents();   // what a host gets when tracking never subscribes
            services.AddCityWatchTracking(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Tracking:Enabled"] = "false" }).Build());
            using var provider = services.BuildServiceProvider();

            Assert.IsInstanceOfType(provider.GetRequiredService<IDomainEventPublisher>(),
                typeof(NullDomainEventPublisher));
        }

        /* ---------------- NFC anchor (§20.3) ---------------- */

        [TestMethod]
        public async Task Scan_WithActiveSession_WritesNegativeSeqAnchor_AndDrivesLiveState()
        {
            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None);
            Assert.IsNotNull(session);

            await AnchorHandler().HandleAsync(new NfcCheckpointScanned(
                Unit, "04A2B1", 12, 7, null, "-33.865143,151.209900", Now, 1, isOfflineRecord: false),
                CancellationToken.None);

            Assert.IsTrue(_channel.Reader.TryRead(out var point));
            Assert.AreEqual((byte)TrackPointSource.NfcAnchor, point!.SourceType);
            Assert.AreEqual("04A2B1", point.AnchorTagUid);
            Assert.IsTrue(point.Seq < 0, "Server anchors use negative Seq so they never collide with device sequences.");
            Assert.AreEqual(-33.865143m, point.Latitude);
            Assert.IsNotNull(_liveState.Get(Unit), "A fresh anchor is the unit's live position in Normal mode.");
        }

        [TestMethod]
        public async Task Scan_WithoutSession_ProducesNothing()
        {
            await AnchorHandler().HandleAsync(new NfcCheckpointScanned(
                Unit, "04A2B1", 12, 7, null, "-33.86,151.20", Now, 1, false), CancellationToken.None);

            Assert.IsFalse(_channel.Reader.TryRead(out _), "No session, no tracking (§6.5) applies to anchors too.");
        }

        [TestMethod]
        public async Task OfflineScan_IsBackfilled_AndDoesNotMoveTheLiveMarker()
        {
            await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None);

            await AnchorHandler().HandleAsync(new NfcCheckpointScanned(
                Unit, "04A2B1", 12, 7, null, "-34.5,150.0", Now.AddHours(-2), 1, isOfflineRecord: true),
                CancellationToken.None);

            Assert.IsTrue(_channel.Reader.TryRead(out var point), "Backfilled anchors are still history.");
            Assert.IsTrue(((TrackPointFlags)point!.Flags).HasFlag(TrackPointFlags.Backfilled));
            Assert.IsNull(_liveState.Get(Unit), "…but must not paint the live map.");
        }

        [TestMethod]
        public void TryParseGps_HandlesTheStringsThePlatformActuallyProduces()
        {
            Assert.IsTrue(NfcAnchorHandler.TryParseGps("-33.865143,151.209900", out var lat, out var lon));
            Assert.AreEqual(-33.865143m, lat);
            Assert.AreEqual(151.209900m, lon);

            Assert.IsTrue(NfcAnchorHandler.TryParseGps(" -33.86 , 151.20 ", out _, out _), "Whitespace happens.");
            Assert.IsFalse(NfcAnchorHandler.TryParseGps(null, out _, out _));
            Assert.IsFalse(NfcAnchorHandler.TryParseGps("", out _, out _));
            Assert.IsFalse(NfcAnchorHandler.TryParseGps("no fix", out _, out _));
            Assert.IsFalse(NfcAnchorHandler.TryParseGps("0,0", out _, out _), "Null island is not a position.");
            Assert.IsFalse(NfcAnchorHandler.TryParseGps("-33.86", out _, out _), "A single value is not a pair.");
        }

        /* ---------------- session lifecycle (§20.3) ---------------- */

        [TestMethod]
        public async Task LoginThenLogout_OpensThenHardStopsTheSession()
        {
            await Lifecycle().HandleAsync(new OfficerLoggedIn(7, Unit, 12, "dev-1", Now), CancellationToken.None);
            var open = await _db.TrackingSessions.SingleAsync(s => s.UnitId == Unit && s.Status == "Active");
            Assert.AreEqual(7, open.GuardId);

            _liveState.Update(new UnitLiveState { UnitId = Unit, SessionId = open.Id, Lat = -33.86m, Lon = 151.2m, RecordedUtc = Now, ReceivedUtc = Now });

            await Lifecycle().HandleAsync(new OfficerLoggedOut(7, Unit, Now.AddHours(8)), CancellationToken.None);

            var closed = await _db.TrackingSessions.SingleAsync(s => s.Id == open.Id);
            Assert.AreEqual("Completed", closed.Status);
            Assert.AreEqual("OfficerLogout", closed.EndReason);
            Assert.IsNull(_liveState.Get(Unit), "Logout removes the unit from the map immediately (§13.5).");
        }

        [TestMethod]
        public async Task Login_OnUnenrolledUnit_OpensNoSession()
        {
            await Lifecycle().HandleAsync(new OfficerLoggedIn(7, 999, 12, null, Now), CancellationToken.None);

            Assert.AreEqual(0, await _db.TrackingSessions.CountAsync(),
                "Enrolment + consent gate applies no matter how the session is initiated.");
        }

        [TestMethod]
        public async Task PatrolEnded_DoesNotCloseTheSession()
        {
            await Lifecycle().HandleAsync(new OfficerLoggedIn(7, Unit, 12, null, Now), CancellationToken.None);

            await Lifecycle().HandleAsync(new PatrolEnded(Unit, 7, "Completed", Now.AddHours(1)), CancellationToken.None);

            Assert.AreEqual(1, await _db.TrackingSessions.CountAsync(s => s.Status == "Active"),
                "A finished visit is a leg boundary, not the end of the shift.");
        }

        /* ---------------- duress (§20.3, §4.5) ---------------- */

        private DuressHandler Duress()
            => new(_db, new ModeCommandService(_db, new TrackingOptions(),
                       NullDomainEventPublisher.Instance, NullLogger<ModeCommandService>.Instance, () => Now),
                   NullLogger<DuressHandler>.Instance);

        [TestMethod]
        public async Task Duress_WithActiveSession_IssuesANeverExpiringCommand()
        {
            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None);
            var handler = Duress();

            await handler.HandleAsync(new DuressActivated(7, null, 12, "-33.86,151.20", Now), CancellationToken.None);

            var command = await _db.TrackingModeCommands.SingleAsync();
            Assert.AreEqual(Unit, command.UnitId, "Resolved via the guard's active session.");
            Assert.AreEqual((byte)TrackingMode.Duress, command.DesiredMode);
            Assert.IsNull(command.ExpiresUtc, "Duress never times out.");
            Assert.IsNull(command.IssuedByUserId, "System-issued, not operator-issued.");
        }

        [TestMethod]
        public async Task Duress_WithoutSession_IsObservedButChangesNothing()
        {
            var handler = Duress();

            await handler.HandleAsync(new DuressActivated(7, null, 12, null, Now), CancellationToken.None);

            Assert.AreEqual(0, await _db.TrackingModeCommands.CountAsync(),
                "Tracking observes duress; the platform's duress path is the mechanism (§4.5).");
        }
    }
}
