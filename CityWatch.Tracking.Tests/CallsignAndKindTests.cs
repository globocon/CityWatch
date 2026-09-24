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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The guard's own login-screen declarations ("Mobile Patrol Car" toggle + Callsign)
    /// are the authority for what a unit is and what it's called. The wand's PatrolCarId is
    /// only a fallback: the same wand can be in a car today and on foot tomorrow.
    /// </summary>
    [TestClass]
    public class CallsignAndKindTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 10, 0, 0, DateTimeKind.Utc);
        private const int Unit = 42;

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _memory = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = Unit, IsEnabled = true, EnrolledUtc = Now.AddDays(-1),
                EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1)
            });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 7, Name = "Bruno Timpano" });
            await _db.SaveChangesAsync();
            _memory = new InMemoryLiveStateStore();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private SessionService Sessions()
            => new(_db, _memory, NullLogger<SessionService>.Instance, segments: null, utcNow: () => Now);

        private async Task AddFixAsync(Guid sessionId)
        {
            _db.TrackPoints.Add(new TrackPoint
            {
                UnitId = Unit, SessionId = sessionId, Seq = 1,
                RecordedUtc = Now.AddSeconds(-10), ReceivedUtc = Now.AddSeconds(-10),
                Latitude = -33.8688m, Longitude = 151.2093m, SourceType = 2, ModeAtCapture = 2
            });
            await _db.SaveChangesAsync();
        }

        private Task<System.Collections.Generic.IReadOnlyList<LiveUnitDto>> SnapshotAsync()
            => new LiveSnapshotService(_memory, _db, () => Now).GetSnapshotAsync(CancellationToken.None);

        [TestMethod]
        public async Task PatrolCarToggleOn_RendersAsCar_WithCallsignLabel()
        {
            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 1");
            await AddFixAsync(session!.Id);

            var unit = (await SnapshotAsync()).Single();

            Assert.AreEqual("car", unit.Kind);
            Assert.AreEqual("Romeo 1", unit.Callsign, "The radio callsign is what operators see.");
            Assert.AreEqual("Bruno Timpano", unit.GuardName, "The driver is still identified.");
        }

        [TestMethod]
        public async Task PatrolCarToggleOff_RendersAsGuard_EvenWhenTheWandIsCarAllocated()
        {
            // Wand is allocated to a patrol car, but tonight the officer is on foot.
            _db.PlatformSmartWands.Add(new PlatformSmartWand { Id = Unit, ClientSiteId = 12, PatrolCarId = 3 });
            await _db.SaveChangesAsync();

            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None,
                isPatrolCar: false, callsign: null);
            await AddFixAsync(session!.Id);

            var unit = (await SnapshotAsync()).Single();

            Assert.AreEqual("guard", unit.Kind,
                "The per-shift declaration must beat the wand's static allocation.");
            Assert.IsNull(unit.Callsign);
        }

        [TestMethod]
        public async Task NoDeclaration_FallsBackToTheWandAllocation()
        {
            // Sessions opened before the declaration was captured.
            _db.PlatformSmartWands.Add(new PlatformSmartWand { Id = Unit, ClientSiteId = 12, PatrolCarId = 3 });
            await _db.SaveChangesAsync();

            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None);
            await AddFixAsync(session!.Id);

            Assert.AreEqual("car", (await SnapshotAsync()).Single().Kind);
        }

        [TestMethod]
        public async Task ReLogin_RefreshesTheDeclarations_OnTheSameSession()
        {
            var first = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None,
                isPatrolCar: false, callsign: null);

            // Officer switches the toggle on and picks a callsign, then logs in again.
            var second = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 4");

            Assert.AreEqual(first!.Id, second!.Id, "Same officer + unit keeps the session.");
            var stored = await _db.TrackingSessions.SingleAsync();
            Assert.IsTrue(stored.IsPatrolCar!.Value);
            Assert.AreEqual("Romeo 4", stored.Callsign);
        }

        [TestMethod]
        public async Task BlankCallsign_IsStoredAsNull_NotEmptyString()
        {
            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None,
                isPatrolCar: true, callsign: "   ");
            await AddFixAsync(session!.Id);

            Assert.IsNull((await SnapshotAsync()).Single().Callsign,
                "A blank pick must not render as an empty label.");
        }

        [TestMethod]
        public async Task IdleList_UsesTheDeclaredKindAndCallsign()
        {
            var session = await Sessions().StartAsync(Unit, 7, 12, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 1");
            _db.TrackPoints.AddRange(
                new TrackPoint { UnitId = Unit, SessionId = session!.Id, Seq = 1, RecordedUtc = Now.AddMinutes(-30), ReceivedUtc = Now.AddMinutes(-30), Latitude = -33.8700m, Longitude = 151.2000m, SourceType = 2, ModeAtCapture = 2 },
                new TrackPoint { UnitId = Unit, SessionId = session.Id, Seq = 2, RecordedUtc = Now.AddMinutes(-1), ReceivedUtc = Now.AddMinutes(-1), Latitude = -33.8701m, Longitude = 151.2001m, SourceType = 2, ModeAtCapture = 2 });
            await _db.SaveChangesAsync();

            var idle = await new IdleDetectionService(_db, new TrackingOptions(), () => Now)
                .GetIdleUnitsAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

            Assert.AreEqual("car", idle.Single().Kind);
            Assert.AreEqual("Romeo 1", idle.Single().Callsign);
        }
    }
}
