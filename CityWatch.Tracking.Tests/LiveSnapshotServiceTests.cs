using System;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The two-process reality (§ M1.7): ingest warms the store in CityWatch.Web, while the
    /// control room asks from CityWatch.RadioCheck where the store is cold. Both must see
    /// the same fleet.
    /// </summary>
    [TestClass]
    public class LiveSnapshotServiceTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 6, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Session = Guid.NewGuid();
        private const int Unit = 42;

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _memory = null!;
        private LiveSnapshotService _service = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-2), Status = "Active"
            });
            await _db.SaveChangesAsync();

            _memory = new InMemoryLiveStateStore();
            _service = new LiveSnapshotService(_memory, _db, () => Now);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        [TestMethod]
        public async Task WarmStore_ServesFromMemory()
        {
            _memory.Update(new UnitLiveState
            {
                UnitId = Unit, SessionId = Session, Lat = -33.86m, Lon = 151.20m,
                Mode = TrackingMode.Transit, Source = TrackPointSource.Transit,
                RecordedUtc = Now.AddSeconds(-10), ReceivedUtc = Now.AddSeconds(-8)
            });

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(-33.86m, snapshot[0].Lat);
            Assert.AreEqual(8, snapshot[0].AgeSeconds);
        }

        [TestMethod]
        public async Task ColdStore_FallsBackToLatestStoredPoint()
        {
            // The RadioCheck process: nothing in memory, points in the shared database.
            _db.TrackPoints.AddRange(
                new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-10), ReceivedUtc = Now.AddMinutes(-10), Latitude = -34.0m, Longitude = 151.0m, SourceType = 2, ModeAtCapture = 2 },
                new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 2, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddMinutes(-1), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual(1, snapshot.Count);
            Assert.AreEqual(-33.9m, snapshot[0].Lat, "The most recent point wins.");
            Assert.AreEqual(60, snapshot[0].AgeSeconds);
        }

        [TestMethod]
        public async Task ColdStore_IgnoresBackfilledPoints()
        {
            _db.TrackPoints.AddRange(
                new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-3), ReceivedUtc = Now.AddMinutes(-3), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 },
                new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 2, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddSeconds(-30), Latitude = -34.5m, Longitude = 150.5m, SourceType = 2, ModeAtCapture = 2, Flags = (byte)TrackPointFlags.Backfilled });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual(-33.9m, snapshot[0].Lat,
                "A backfilled point is history, not a live position — same rule as the warm path.");
        }

        [TestMethod]
        public async Task StaleMemoryFromAPreviousSession_IsNotTrusted()
        {
            // Memory still holds yesterday's session for this unit; today's session has a point.
            _memory.Update(new UnitLiveState
            {
                UnitId = Unit, SessionId = Guid.NewGuid() /* old session */, Lat = -30m, Lon = 150m,
                RecordedUtc = Now.AddHours(-20), ReceivedUtc = Now.AddHours(-20)
            });
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-2), ReceivedUtc = Now.AddMinutes(-2), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual(-33.9m, snapshot[0].Lat,
                "Memory keyed to a closed session must not shadow the current session's data.");
        }

        [TestMethod]
        public async Task NoActiveSessions_EmptySnapshot()
        {
            var session = await _db.TrackingSessions.SingleAsync();
            session.Status = "Completed";
            await _db.SaveChangesAsync();

            _memory.Update(new UnitLiveState { UnitId = Unit, SessionId = Session, Lat = -33m, Lon = 151m, RecordedUtc = Now, ReceivedUtc = Now });

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual(0, snapshot.Count, "No active session ⇒ nothing on the map, whatever memory holds.");
        }

        [TestMethod]
        public async Task SessionWithNoFixYet_IsSimplyAbsent()
        {
            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);
            Assert.AreEqual(0, snapshot.Count, "An open session with no data is not an error and not a marker.");
        }

        [TestMethod]
        public async Task GuardIdentity_FlowsThroughTheSnapshot()
        {
            /* #153 Part 2: with a hundred Muhammads on the books, the card must say WHICH
               one — full name, licence (+ state), and contact. */
            _db.PlatformGuards.Add(new PlatformGuard
            {
                Id = 7, Name = "Muhammad Bilal", SecurityNo = "569-829-111",
                State = "VIC", Mobile = "+61 421 945 291", Email = "m.bilal@citywatchsecurity.com"
            });
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddMinutes(-1), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual("Muhammad Bilal", snapshot[0].GuardName);
            Assert.AreEqual("569-829-111", snapshot[0].GuardLicense);
            Assert.AreEqual("VIC", snapshot[0].GuardState);
            Assert.AreEqual("+61 421 945 291", snapshot[0].GuardMobile);
            Assert.AreEqual("m.bilal@citywatchsecurity.com", snapshot[0].GuardEmail);
        }

        [TestMethod]
        public async Task CarWithoutDeclaredPosition_IsNamedByItsLoginSite()
        {
            /* #153 Part 1: R1's phone declared no position at login, so its card said only
               "Patrol Car" while R5 said "Mobile Patrols (Car) M1 · Patrol Car". The login
               site is the car's identity in the platform — fall back to it. */
            var session = await _db.TrackingSessions.SingleAsync();
            session.IsPatrolCar = true;
            session.Callsign = "R1";
            _db.PlatformClientSites.Add(new PlatformClientSite { Id = 12, Name = "Mobile Patrols (Car) M1", IsActive = true });
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddMinutes(-1), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual("Mobile Patrols (Car) M1", snapshot[0].PatrolCar,
                "A car with no declared position is named by its login site, not left blank.");
        }

        [TestMethod]
        public async Task DeclaredPosition_IsNeverOverriddenByTheLoginSite()
        {
            var session = await _db.TrackingSessions.SingleAsync();
            session.IsPatrolCar = true;
            session.PatrolCarPositionName = "Mobile Patrols (Car) M2";
            _db.PlatformClientSites.Add(new PlatformClientSite { Id = 12, Name = "Head Office", IsActive = true });
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddMinutes(-1), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual("Mobile Patrols (Car) M2", snapshot[0].PatrolCar,
                "What the officer declared at login wins over any fallback.");
        }

        [TestMethod]
        public async Task GuardWithoutPosition_GetsNoCarName()
        {
            var session = await _db.TrackingSessions.SingleAsync();
            session.IsPatrolCar = false;
            _db.PlatformClientSites.Add(new PlatformClientSite { Id = 12, Name = "Mobile Patrols (Car) M1", IsActive = true });
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddMinutes(-1), Latitude = -33.9m, Longitude = 151.1m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.IsNull(snapshot[0].PatrolCar,
                "The fallback is a car-naming rule; a guard on foot has no car name.");
        }

        [TestMethod]
        public async Task DuressMarker_Persists_WhileTheAlarmIsOn()
        {
            _db.PlatformClientSiteDuress.Add(new PlatformClientSiteDuress
            {
                ClientSiteId = 12, IsEnabled = true, EnabledBy = 7   // the session's guard
            });
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-5), ReceivedUtc = Now.AddMinutes(-5), Latitude = -33.9m, Longitude = 151.1m, SourceType = 4, ModeAtCapture = 4 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual((byte)TrackingMode.Duress, snapshot[0].Mode,
                "A live alarm keeps the marker in duress, however old the last fix.");
        }

        [TestMethod]
        public async Task DuressMarker_StandsDown_WhenTheControlRoomDeactivatesTheAlarm()
        {
            /* The phone is unreachable: its last stored point still says duress, but the
               control room has deactivated the alarm (ClientSiteDuress rows deleted). The
               map must stand down NOW — from the control room an eternal stale DURESS is
               indistinguishable from a real one, which is the dangerous part. */
            _db.TrackPoints.Add(new TrackPoint { UnitId = Unit, SessionId = Session, Seq = 1, RecordedUtc = Now.AddMinutes(-30), ReceivedUtc = Now.AddMinutes(-30), Latitude = -33.9m, Longitude = 151.1m, SourceType = 4, ModeAtCapture = 4 });
            await _db.SaveChangesAsync();

            var snapshot = await _service.GetSnapshotAsync(CancellationToken.None);

            Assert.AreEqual((byte)TrackingMode.Transit, snapshot[0].Mode,
                "No ClientSiteDuress row ⇒ no DURESS accent, whatever the last point captured.");
        }
    }
}
