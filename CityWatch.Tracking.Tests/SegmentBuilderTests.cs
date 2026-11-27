using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class SegmentBuilderTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 8, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Session = Guid.NewGuid();
        private const int Unit = 42;

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public void Setup()
            => _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private static TrackPoint Point(int seq, int minute, decimal lat, decimal lon,
            TrackPointSource source = TrackPointSource.Transit, short? speed = null, byte flags = 0)
            => new()
            {
                UnitId = Unit, SessionId = Session, Seq = seq,
                RecordedUtc = Now.AddMinutes(minute), ReceivedUtc = Now.AddMinutes(minute),
                Latitude = lat, Longitude = lon, SpeedKph = speed,
                SourceType = (byte)source, ModeAtCapture = 2, Flags = flags
            };

        private SegmentBuilder Builder() => new(_db, NullLogger<SegmentBuilder>.Instance);

        [TestMethod]
        public async Task Session_SplitsIntoLegs_AtNfcAnchors()
        {
            // depart → anchor at site A (min 10) → travel → anchor at site B (min 30) → tail
            _db.TrackPoints.AddRange(
                Point(1, 0, -33.90m, 151.10m, speed: 40),
                Point(2, 5, -33.88m, 151.12m, speed: 50),
                Point(3, 10, -33.86m, 151.14m, TrackPointSource.NfcAnchor),
                Point(4, 20, -33.84m, 151.16m, speed: 60),
                Point(5, 30, -33.82m, 151.18m, TrackPointSource.NfcAnchor),
                Point(6, 35, -33.81m, 151.19m, speed: 30));
            await _db.SaveChangesAsync();

            var count = await Builder().BuildForSessionAsync(Session, CancellationToken.None);

            Assert.AreEqual(3, count, "head→A, A→B, B→tail");
            var segments = await _db.TrackSegments.OrderBy(s => s.StartUtc).ToListAsync();
            Assert.AreEqual(1, segments[0].AnchorScanCount, "Leg 1 ends on its anchor.");
            Assert.AreEqual(2, segments[1].AnchorScanCount, "Leg 2 is bounded by two verified touches.");
            Assert.IsTrue(segments[1].DistanceM > 3000, "A→B is a few km of real distance.");
            Assert.AreEqual((short)60, segments[1].MaxSpeedKph);
        }

        [TestMethod]
        public async Task ImplausiblePoints_DoNotInflateCreditedDistance()
        {
            _db.TrackPoints.AddRange(
                Point(1, 0, -33.90m, 151.10m),
                Point(2, 1, -31.95m, 115.86m, flags: (byte)TrackPointFlags.Implausible),   // Perth teleport
                Point(3, 2, -33.89m, 151.11m));
            await _db.SaveChangesAsync();

            await Builder().BuildForSessionAsync(Session, CancellationToken.None);

            var segment = await _db.TrackSegments.SingleAsync();
            Assert.IsTrue(segment.DistanceM < 5000,
                $"Teleport must not credit ~6,600 km of patrol distance (got {segment.DistanceM} m).");
            Assert.IsTrue(((TrackPointFlags)segment.Flags).HasFlag(TrackPointFlags.Implausible),
                "…but the leg is flagged so the anomaly remains visible.");
        }

        [TestMethod]
        public async Task Rebuild_IsIdempotent()
        {
            _db.TrackPoints.AddRange(
                Point(1, 0, -33.90m, 151.10m),
                Point(2, 10, -33.88m, 151.12m));
            await _db.SaveChangesAsync();

            await Builder().BuildForSessionAsync(Session, CancellationToken.None);
            await Builder().BuildForSessionAsync(Session, CancellationToken.None);

            Assert.AreEqual(1, await _db.TrackSegments.CountAsync(), "Rebuilding replaces, never duplicates.");
        }

        [TestMethod]
        public async Task FewerThanTwoPoints_ProducesNoSegments()
        {
            _db.TrackPoints.Add(Point(1, 0, -33.90m, 151.10m));
            await _db.SaveChangesAsync();

            var count = await Builder().BuildForSessionAsync(Session, CancellationToken.None);

            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public void SplitAtAnchors_AnchorIsBothLegEndAndLegStart()
        {
            var points = new System.Collections.Generic.List<TrackPoint>
            {
                Point(1, 0, -33.90m, 151.10m),
                Point(2, 10, -33.86m, 151.14m, TrackPointSource.NfcAnchor),
                Point(3, 20, -33.82m, 151.18m)
            };

            var legs = SegmentBuilder.SplitAtAnchors(points);

            Assert.AreEqual(2, legs.Count);
            Assert.AreSame(legs[0][^1], legs[1][0], "The anchor is shared: end of one leg, start of the next.");
        }
    }
}
