using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    [TestClass]
    public class IdleDetectionServiceTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 9, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Session = Guid.NewGuid();
        private const int Unit = 42;

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-3), Status = "Active"
            });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 7, Name = "J. Smith" });
            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private IdleDetectionService Service()
            => new(_db, new TrackingOptions(), () => Now);

        private void AddPoint(int seq, int minAgo, decimal lat, decimal lon,
            TrackingMode mode = TrackingMode.Transit, byte flags = 0)
            => _db.TrackPoints.Add(new TrackPoint
            {
                UnitId = Unit, SessionId = Session, Seq = seq,
                RecordedUtc = Now.AddMinutes(-minAgo), ReceivedUtc = Now.AddMinutes(-minAgo),
                Latitude = lat, Longitude = lon,
                SourceType = 2, ModeAtCapture = (byte)mode, Flags = flags
            });

        [TestMethod]
        public async Task UnitParkedForHalfAnHour_IsIdle_WithCorrectDuration()
        {
            // Drove until 30 min ago, then heartbeats from one spot.
            AddPoint(1, 40, -33.90m, 151.10m);          // >75 m away — the drive
            AddPoint(2, 30, -33.8700m, 151.2000m);      // arrived
            AddPoint(3, 20, -33.8701m, 151.2001m);      // ~14 m drift
            AddPoint(4, 10, -33.8700m, 151.2002m);
            AddPoint(5, 1, -33.8701m, 151.2000m);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual(1, idle.Count);
            Assert.AreEqual(Unit, idle[0].UnitId);
            Assert.IsTrue(idle[0].IdleMinutes is >= 29 and <= 31, $"Expected ~30, got {idle[0].IdleMinutes}");
            Assert.AreEqual("J. Smith", idle[0].GuardName);
            Assert.AreEqual("guard", idle[0].Kind, "No wand row / no PatrolCarId ⇒ guard on foot.");
        }

        [TestMethod]
        public async Task MovingUnit_IsNotIdle()
        {
            AddPoint(1, 20, -33.90m, 151.10m);
            AddPoint(2, 10, -33.88m, 151.12m);
            AddPoint(3, 1, -33.86m, 151.14m);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual(0, idle.Count);
        }

        [TestMethod]
        public async Task DriveOffAndReturn_CountsOnlyTheReturnStay()
        {
            AddPoint(1, 60, -33.8700m, 151.2000m);      // same spot, an hour ago
            AddPoint(2, 40, -33.90m, 151.10m);          // drove away
            AddPoint(3, 12, -33.8700m, 151.2000m);      // came back
            AddPoint(4, 1, -33.8701m, 151.2001m);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(10), CancellationToken.None);

            Assert.AreEqual(1, idle.Count);
            Assert.IsTrue(idle[0].IdleMinutes is >= 11 and <= 13,
                $"Idle since the RETURN (12 min ago), not the first visit; got {idle[0].IdleMinutes}");
        }

        [TestMethod]
        public async Task DuressUnit_IsNeverOnTheIdleList()
        {
            AddPoint(1, 30, -33.8700m, 151.2000m, TrackingMode.Duress);
            AddPoint(2, 1, -33.8700m, 151.2000m, TrackingMode.Duress);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual(0, idle.Count, "An officer in duress is an emergency, not a loiter.");
        }

        [TestMethod]
        public async Task BackfilledPoints_DoNotBreakTheIdleSpell()
        {
            AddPoint(1, 30, -33.8700m, 151.2000m);
            AddPoint(2, 15, -34.5m, 150.5m, flags: (byte)TrackPointFlags.Backfilled);   // stale replay far away
            AddPoint(3, 1, -33.8701m, 151.2001m);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual(1, idle.Count, "A replayed offline point is history, not presence.");
        }

        [TestMethod]
        public async Task PatrolCarWand_ReportsKindCar()
        {
            _db.PlatformSmartWands.Add(new PlatformSmartWand { Id = Unit, ClientSiteId = 12, PatrolCarId = 3 });
            AddPoint(1, 30, -33.8700m, 151.2000m);
            AddPoint(2, 1, -33.8701m, 151.2001m);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual("car", idle.Single().Kind);
        }

        [TestMethod]
        public async Task ClosedSession_IsInvisibleToIdleDetection()
        {
            var session = await _db.TrackingSessions.SingleAsync();
            session.Status = "Completed";
            AddPoint(1, 30, -33.8700m, 151.2000m);
            AddPoint(2, 1, -33.8701m, 151.2001m);
            await _db.SaveChangesAsync();

            var idle = await Service().GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual(0, idle.Count, "Off shift is off the list — same rule as the map (§13.5).");
        }
    }
}
