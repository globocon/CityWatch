using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Events.Events;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Handlers;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The device is never the tracked unit. A "SmartWand" record is a registered phone;
    /// what is tracked is a CAR (login Position) or a PERSON (the guard). These tests pin
    /// that a scan still reaches its session even though the scan carries a device id and
    /// the session is keyed on the car or the guard.
    /// </summary>
    [TestClass]
    public class DeviceIndependenceTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 16, 0, 0, DateTimeKind.Utc);
        private const int Guard = 4;              // Bruno Timpano
        private const int FleetSite = 625;
        private const int MarthaCove = 390;
        private static readonly int CarUnit = TrackingUnitKey.FromPosition(10);   // 2,000,010 = M1
        private static readonly int FootUnit = TrackingUnitKey.FromGuard(Guard);  // 1,000,004

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _live = null!;
        private Channel<TrackPoint> _channel = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _db.TrackingUnitEnrolments.AddRange(
                new TrackingUnitEnrolment { UnitId = CarUnit, IsEnabled = true, EnrolledUtc = Now.AddDays(-1), EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1) },
                new TrackingUnitEnrolment { UnitId = FootUnit, IsEnabled = true, EnrolledUtc = Now.AddDays(-1), EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1) });
            _db.PlatformGuards.Add(new PlatformGuard { Id = Guard, Name = "Bruno Timpano" });
            await _db.SaveChangesAsync();
            _live = new InMemoryLiveStateStore();
            _channel = Channel.CreateBounded<TrackPoint>(100);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private SessionService Sessions()
            => new(_db, _live, NullLogger<SessionService>.Instance, segments: null, utcNow: () => Now);

        private NfcAnchorHandler Handler()
            => new(_db, _live, Sessions(), _channel.Writer, NullLogger<NfcAnchorHandler>.Instance);

        /// <summary>A scan as the platform publishes it: it carries the DEVICE id (or 0 when
        /// no wand is allocated), never the tracked unit's id.</summary>
        private static NfcCheckpointScanned Scan(int deviceWandId, int tagSiteId, string label)
            => new(deviceWandId, "04A2B1", tagSiteId, Guard, null, "-33.8688,151.2093", Now, 1, false)
            {
                LoggedInClientSiteId = FleetSite,
                LabelDescription = label,
                TagSiteName = "Martha Cove Marina"
            };

        [TestMethod]
        public async Task PatrolCar_TracksWithNoWandAtAll()
        {
            // Exactly Bruno's real logins: SmartWandId NULL.
            var session = await Sessions().StartAsync(CarUnit, Guard, FleetSite, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 03", positionId: 10, positionName: "Mobile Patrols (Car) M1");

            Assert.IsNotNull(session, "A patrol car must track without any device allocation.");
            Assert.AreEqual(CarUnit, session!.UnitId);
        }

        [TestMethod]
        public async Task FootGuard_TracksWithNoWandAtAll()
        {
            var session = await Sessions().StartAsync(FootUnit, Guard, MarthaCove, null, CancellationToken.None,
                isPatrolCar: false, callsign: null);

            Assert.IsNotNull(session);
            Assert.AreEqual(FootUnit, session!.UnitId);
            Assert.AreEqual(Guard, TrackingUnitKey.ToGuardId(session.UnitId));
        }

        [TestMethod]
        public async Task ScanWithNoWandId_StillReachesTheCarsSession()
        {
            // The scan says wand 0 — the officer has no wand — yet it must still update the
            // car's session. The guard is the link between the phone and the unit.
            var session = await Sessions().StartAsync(CarUnit, Guard, FleetSite, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 03", positionId: 10, positionName: "Mobile Patrols (Car) M1");

            await Handler().HandleAsync(Scan(deviceWandId: 0, MarthaCove, "OC1 P01 - BQQ Area"), CancellationToken.None);

            var stored = await _db.TrackingSessions.SingleAsync(s => s.Id == session!.Id);
            Assert.AreEqual("AtSite", stored.TravelState);
            Assert.AreEqual(MarthaCove, stored.CurrentSiteId);
        }

        [TestMethod]
        public async Task AnchorPoint_IsStoredAgainstTheUnit_NotTheDevice()
        {
            await Sessions().StartAsync(CarUnit, Guard, FleetSite, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 03", positionId: 10, positionName: "Mobile Patrols (Car) M1");

            // Scan reports device 137 — a registered phone that is NOT the tracked unit.
            await Handler().HandleAsync(Scan(deviceWandId: 137, MarthaCove, "OC1 P01"), CancellationToken.None);

            Assert.IsTrue(_channel.Reader.TryRead(out var point));
            Assert.AreEqual(CarUnit, point!.UnitId,
                "The anchor belongs to the car, never to the phone that scanned it.");
            Assert.AreEqual(CarUnit, _live.Get(CarUnit)!.UnitId);
            Assert.IsNull(_live.Get(137), "The device must never appear as a unit on the map.");
        }

        [TestMethod]
        public async Task ScanFromAGuardWithNoSession_ChangesNothing()
        {
            await Handler().HandleAsync(Scan(0, MarthaCove, "OC1 P01"), CancellationToken.None);

            Assert.AreEqual(0, await _db.TrackingSessions.CountAsync());
            Assert.IsFalse(_channel.Reader.TryRead(out _));
        }

        [TestMethod]
        public async Task MapShowsCarAndFootGuardCorrectly_FromTheUnitKeyAlone()
        {
            // Two sessions, no IsPatrolCar declared: the key alone must classify them.
            var car = await Sessions().StartAsync(CarUnit, Guard, FleetSite, null, CancellationToken.None);
            var foot = await Sessions().StartAsync(FootUnit, 99, MarthaCove, null, CancellationToken.None);
            _db.PlatformGuards.Add(new PlatformGuard { Id = 99, Name = "Foot Officer" });
            _db.TrackPoints.AddRange(
                new TrackPoint { UnitId = CarUnit, SessionId = car!.Id, Seq = 1, RecordedUtc = Now.AddSeconds(-5), ReceivedUtc = Now.AddSeconds(-5), Latitude = -33.86m, Longitude = 151.20m, SourceType = 2, ModeAtCapture = 2 },
                new TrackPoint { UnitId = FootUnit, SessionId = foot!.Id, Seq = 1, RecordedUtc = Now.AddSeconds(-5), ReceivedUtc = Now.AddSeconds(-5), Latitude = -33.87m, Longitude = 151.21m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await new LiveSnapshotService(_live, _db, () => Now)
                .GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual("car", snapshot.Single(u => u.UnitId == CarUnit).Kind);
            Assert.AreEqual("guard", snapshot.Single(u => u.UnitId == FootUnit).Kind);
        }
    }
}
