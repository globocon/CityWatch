using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
    /// <summary>
    /// The callsign names the car (P4 #153, 25 Aug 2026). Phones auto-restore a stale
    /// saved Position, so six Romeo crews all logged in keyed to the one old shared
    /// position (2000010) and superseded each other off the map — twelve logins, one
    /// visible car. These tests pin the server-side cure: session/start re-keys a
    /// patrol-car login to the position its callsign names, and ingest trusts the
    /// SESSION's unit, so the phone's stale unit stamp keeps working unchanged.
    /// </summary>
    [TestClass]
    public class CallsignReKeyTests
    {
        private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);
        private const int SharedM1Unit = 2000010;      // the stale position every phone remembers
        private const int R4Unit = 2000041;
        private const int R5Unit = 2000042;

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _liveState = null!;
        private SessionService _sessions = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.PlatformPositions.AddRange(
                new PlatformPosition { Id = 10, Name = "Mobile Patrols (Car) M1", IsPatrolCar = true },
                new PlatformPosition { Id = 41, Name = "Mobile Patrols (Car) R4", IsPatrolCar = true },
                new PlatformPosition { Id = 42, Name = "Mobile Patrols (Car) R5", IsPatrolCar = true },
                new PlatformPosition { Id = 43, Name = "Mobile Patrols (Car) R6", IsPatrolCar = true },
                /* not a car: must never catch a callsign */
                new PlatformPosition { Id = 90, Name = "Concierge R4", IsPatrolCar = false });
            foreach (var unit in new[] { SharedM1Unit, R4Unit, R5Unit })   // R6 deliberately unenrolled
                _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
                {
                    UnitId = unit,
                    IsEnabled = true,
                    EnrolledUtc = Now.AddDays(-8),
                    EnrolledByUserId = 1,
                    ConsentRecordedUtc = Now.AddDays(-8)
                });
            await _db.SaveChangesAsync();

            _liveState = new InMemoryLiveStateStore();
            _sessions = new SessionService(_db, _liveState, NullLogger<SessionService>.Instance,
                segments: null, utcNow: () => Now);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        [TestMethod]
        public async Task CarLogin_CallsignReKeysTheStaleSharedPosition()
        {
            var session = await _sessions.StartAsync(SharedM1Unit, guardId: 694, clientSiteId: 625,
                pcarRouteId: null, CancellationToken.None, isPatrolCar: true, callsign: "R4",
                positionId: 10, positionName: "Mobile Patrols (Car) M1");

            Assert.IsNotNull(session);
            Assert.AreEqual(R4Unit, session!.UnitId, "The callsign names the car, not the stale preference.");
            Assert.AreEqual(41, session.PatrolCarPositionId);
            Assert.AreEqual("Mobile Patrols (Car) R4", session.PatrolCarPositionName);
        }

        [TestMethod]
        public async Task TwoCrewsOnTheStaleSharedPosition_BothStayOnTheMap()
        {
            var r4 = await _sessions.StartAsync(SharedM1Unit, 694, 625, null,
                CancellationToken.None, true, "R4", 10, "Mobile Patrols (Car) M1");
            var r5 = await _sessions.StartAsync(SharedM1Unit, 1622, 625, null,
                CancellationToken.None, true, "R5", 10, "Mobile Patrols (Car) M1");

            Assert.AreEqual(R4Unit, r4!.UnitId);
            Assert.AreEqual(R5Unit, r5!.UnitId);
            var active = _db.TrackingSessions.Where(s => s.Status == "Active").ToList();
            Assert.AreEqual(2, active.Count, "No supersession: two callsigns are two cars.");
        }

        [TestMethod]
        public async Task CallsignNamingNoCar_KeepsThePhonesUnit()
        {
            var session = await _sessions.StartAsync(SharedM1Unit, 694, 625, null,
                CancellationToken.None, true, "FOX9", 10, "Mobile Patrols (Car) M1");

            Assert.IsNotNull(session);
            Assert.AreEqual(SharedM1Unit, session!.UnitId);
            Assert.AreEqual(10, session.PatrolCarPositionId, "Nothing to re-key to — the choice stands.");
        }

        [TestMethod]
        public async Task FootGuardCalledR4_IsNeverReKeyed()
        {
            var footUnit = TrackingUnitKey.FromGuard(806);
            _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = footUnit, IsEnabled = true, EnrolledUtc = Now.AddDays(-8),
                EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-8)
            });
            await _db.SaveChangesAsync();

            var session = await _sessions.StartAsync(footUnit, 806, 12, null,
                CancellationToken.None, isPatrolCar: false, callsign: "R4");

            Assert.AreEqual(footUnit, session!.UnitId, "Only a PATROL CAR login re-keys.");
        }

        [TestMethod]
        public async Task ReKeyedUnit_MustItselfBeEnrolled()
        {
            var session = await _sessions.StartAsync(SharedM1Unit, 694, 625, null,
                CancellationToken.None, true, "R6", 10, "Mobile Patrols (Car) M1");

            Assert.IsNull(session, "R6's unit is not enrolled — consent gates the REAL unit, not the claimed one.");
        }

        [TestMethod]
        public async Task OfficerMovingCars_ClosesTheirStaleSession()
        {
            var first = await _sessions.StartAsync(SharedM1Unit, 694, 625, null,
                CancellationToken.None, true, "R4", 10, "Mobile Patrols (Car) M1");
            var second = await _sessions.StartAsync(SharedM1Unit, 694, 625, null,
                CancellationToken.None, true, "R5", 10, "Mobile Patrols (Car) M1");

            var old = await _db.TrackingSessions.FirstAsync(s => s.Id == first!.Id);
            Assert.AreEqual("Completed", old.Status,
                "The phone can't close a session it never knew the key of — the server does.");
            Assert.AreEqual("OfficerChangedCar", old.EndReason);
            Assert.AreEqual(R5Unit, second!.UnitId);
        }

        [TestMethod]
        public async Task Ingest_TrustsTheSessionsUnit_NotThePhonesStaleStamp()
        {
            var session = await _sessions.StartAsync(SharedM1Unit, 694, 625, null,
                CancellationToken.None, true, "R4", 10, "Mobile Patrols (Car) M1");
            var channel = Channel.CreateBounded<TrackPoint>(100);
            var ingest = new IngestService(_db, _liveState, channel.Writer,
                new UnitRateLimiter(new TrackingOptions()), new TrackingOptions(),
                NullLogger<IngestService>.Instance, commands: null, utcNow: () => Now);

            /* The phone still stamps the unit it chose at login: the stale shared M1. */
            var response = await ingest.IngestAsync(new PositionBatch
            {
                UnitId = SharedM1Unit,
                SessionId = session!.Id,
                DeviceUtc = Now,
                Points = { new PositionPoint { Seq = 1, Utc = Now.AddSeconds(-5), Lat = -37.8m, Lon = 145.0m, AccuracyM = 8, Source = "transit" } }
            }, CancellationToken.None);

            Assert.AreEqual(1, response.Accepted, "The session id is the anchor; the stale stamp still works.");
            Assert.IsTrue(channel.Reader.TryRead(out var written));
            Assert.AreEqual(R4Unit, written!.UnitId, "Evidence is filed under the CAR the session names.");
            Assert.IsNotNull(_liveState.Get(R4Unit), "The live map draws R4, not the stale shared unit.");
            Assert.IsNull(_liveState.Get(SharedM1Unit));
        }
    }
}
