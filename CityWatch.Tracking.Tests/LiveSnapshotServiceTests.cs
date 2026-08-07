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
    }
}
