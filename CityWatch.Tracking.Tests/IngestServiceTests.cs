using System;
using System.Collections.Generic;
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
    [TestClass]
    public class IngestServiceTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 4, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Session = Guid.NewGuid();
        private const int Unit = 42;

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _liveState = null!;
        private Channel<TrackPoint> _channel = null!;
        private IngestService _service = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = Unit,
                IsEnabled = true,
                EnrolledUtc = Now.AddDays(-30),
                EnrolledByUserId = 1,
                ConsentRecordedUtc = Now.AddDays(-30)
            });
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session,
                UnitId = Unit,
                GuardId = 7,
                ClientSiteId = 12,
                StartedUtc = Now.AddHours(-1),
                Status = "Active"
            });
            await _db.SaveChangesAsync();

            _liveState = new InMemoryLiveStateStore();
            _channel = Channel.CreateBounded<TrackPoint>(1000);
            _service = new IngestService(_db, _liveState, _channel.Writer,
                new UnitRateLimiter(new TrackingOptions()), new TrackingOptions(),
                NullLogger<IngestService>.Instance, () => Now);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private static PositionBatch Batch(params PositionPoint[] points) => new()
        {
            UnitId = Unit,
            SessionId = Session,
            DeviceUtc = Now,
            Points = points.ToList()
        };

        private static PositionPoint Point(int seq, decimal lat = -33.865143m, decimal lon = 151.209900m,
            DateTime? utc = null, string source = "transit")
            => new() { Seq = seq, Utc = utc ?? Now.AddSeconds(-5), Lat = lat, Lon = lon, AccuracyM = 8, Source = source };

        [TestMethod]
        public async Task ValidPoint_IsAccepted_Enqueued_AndDrivesLiveState()
        {
            var response = await _service.IngestAsync(Batch(Point(1)), CancellationToken.None);

            Assert.AreEqual(1, response.Accepted);
            Assert.AreEqual(0, response.Rejected);
            Assert.IsTrue(_channel.Reader.TryRead(out var written));
            Assert.AreEqual(Unit, written!.UnitId);
            Assert.AreEqual(Now, written.ReceivedUtc, "Server clock stamps ReceivedUtc.");
            Assert.IsNotNull(_liveState.Get(Unit));
        }

        [TestMethod]
        public async Task UnenrolledUnit_IsRefused_Entirely()
        {
            var batch = Batch(Point(1));
            batch.UnitId = 999;

            var response = await _service.IngestAsync(batch, CancellationToken.None);

            Assert.AreEqual(0, response.Accepted);
            Assert.AreEqual(1, response.Rejected);
            Assert.IsFalse(_channel.Reader.TryRead(out _), "Nothing may reach storage for an unenrolled unit.");
        }

        [TestMethod]
        public async Task EnabledWithoutConsent_IsRefused()
        {
            // The §13.5 structural guarantee: IsEnabled is not enough.
            var enrolment = await _db.TrackingUnitEnrolments.FirstAsync(e => e.UnitId == Unit);
            enrolment.ConsentRecordedUtc = null;
            await _db.SaveChangesAsync();

            var response = await _service.IngestAsync(Batch(Point(1)), CancellationToken.None);

            Assert.AreEqual(0, response.Accepted);
        }

        [TestMethod]
        public async Task NoActiveSession_IsRefused()
        {
            var batch = Batch(Point(1));
            batch.SessionId = Guid.NewGuid();   // unknown session

            var response = await _service.IngestAsync(batch, CancellationToken.None);

            Assert.AreEqual(0, response.Accepted, "No session, no tracking (§6.5).");
        }

        [TestMethod]
        public async Task OutOfBounds_And_NullIsland_AreRejected()
        {
            var response = await _service.IngestAsync(Batch(
                Point(1, lat: 48.8566m, lon: 2.3522m),   // Paris: outside the AU envelope
                Point(2, lat: 0m, lon: 0m),               // null island
                Point(3)),                                // valid
                CancellationToken.None);

            Assert.AreEqual(1, response.Accepted);
            Assert.AreEqual(2, response.Rejected);
        }

        [TestMethod]
        public async Task FutureTimestamp_IsRejected()
        {
            var response = await _service.IngestAsync(
                Batch(Point(1, utc: Now.AddMinutes(10))), CancellationToken.None);

            Assert.AreEqual(0, response.Accepted, "A future timestamp cannot be evidence.");
        }

        [TestMethod]
        public async Task Teleport_IsFlaggedImplausible_NotDropped()
        {
            await _service.IngestAsync(Batch(Point(1)), CancellationToken.None);   // Sydney
            _channel.Reader.TryRead(out _);

            // Perth ~3,300 km away, 10 seconds later ⇒ ~1.2M km/h
            var response = await _service.IngestAsync(
                Batch(Point(2, lat: -31.9523m, lon: 115.8613m, utc: Now.AddSeconds(5))),
                CancellationToken.None);

            Assert.AreEqual(1, response.Accepted, "Flag, never drop (§13.6).");
            Assert.IsTrue(_channel.Reader.TryRead(out var written));
            Assert.IsTrue(((TrackPointFlags)written!.Flags).HasFlag(TrackPointFlags.Implausible));
        }

        [TestMethod]
        public async Task MockProvider_And_LowAccuracy_AreFlagged()
        {
            var mock = Point(1);
            mock.IsMock = true;
            mock.AccuracyM = 250;   // above the 100 m threshold

            await _service.IngestAsync(Batch(mock), CancellationToken.None);

            Assert.IsTrue(_channel.Reader.TryRead(out var written));
            var flags = (TrackPointFlags)written!.Flags;
            Assert.IsTrue(flags.HasFlag(TrackPointFlags.MockProvider));
            Assert.IsTrue(flags.HasFlag(TrackPointFlags.LowAccuracy));
        }

        [TestMethod]
        public async Task BackfilledPoints_AreStored_ButDoNotDriveLiveState()
        {
            var live = Point(1);
            await _service.IngestAsync(Batch(live), CancellationToken.None);
            var before = _liveState.Get(Unit)!;

            var backfill = Point(2, lat: -34.5m, utc: Now.AddMinutes(-30));
            backfill.Backfilled = true;
            var response = await _service.IngestAsync(Batch(backfill), CancellationToken.None);

            Assert.AreEqual(1, response.Accepted, "Backfill is stored — it is history.");
            Assert.AreEqual(before.Lat, _liveState.Get(Unit)!.Lat,
                "…but must never move the live marker (§6.4).");
        }

        [TestMethod]
        public async Task NfcAnchor_CarriesTagUid_AndNormalMode()
        {
            var anchor = Point(1, source: "nfcAnchor");
            anchor.TagUid = "04A2B1C3";

            await _service.IngestAsync(Batch(anchor), CancellationToken.None);

            Assert.IsTrue(_channel.Reader.TryRead(out var written));
            Assert.AreEqual((byte)TrackPointSource.NfcAnchor, written!.SourceType);
            Assert.AreEqual("04A2B1C3", written.AnchorTagUid);
            Assert.AreEqual((byte)TrackingMode.Normal, written.ModeAtCapture);
        }

        [TestMethod]
        public async Task RateLimit_RefusesExcessBatches_WithRetryAfter()
        {
            var options = new TrackingOptions { IngestRateLimitPerUnitPerMinute = 2 };
            var service = new IngestService(_db, _liveState, _channel.Writer,
                new UnitRateLimiter(options), options, NullLogger<IngestService>.Instance, () => Now);

            await service.IngestAsync(Batch(Point(1)), CancellationToken.None);
            await service.IngestAsync(Batch(Point(2)), CancellationToken.None);
            var third = await service.IngestAsync(Batch(Point(3)), CancellationToken.None);

            Assert.AreEqual(0, third.Accepted);
            Assert.AreEqual(60, third.RetryAfterSeconds);
        }

        [TestMethod]
        public async Task Response_AlwaysCarriesPolicyAndServerClock()
        {
            var response = await _service.IngestAsync(Batch(Point(1)), CancellationToken.None);

            Assert.IsNotNull(response.Policy, "Thresholds are server-pushed policy (§5.2).");
            Assert.AreEqual(Now, response.ServerUtc, "The device reconciles skew from this.");
        }

        [TestMethod]
        public void ImpliedSpeed_SanityChecks()
        {
            // Sydney CBD → North Sydney ≈ 3 km; in 3 minutes ⇒ ≈ 60 km/h
            var kph = IngestService.ImpliedSpeedKph(-33.8688m, 151.2093m, -33.8404m, 151.2073m, 3.0 / 60.0);
            Assert.IsTrue(kph is > 50 and < 75, $"Expected ~60 km/h, got {kph:F1}");

            Assert.AreEqual(double.MaxValue, IngestService.ImpliedSpeedKph(0, 0, 1, 1, 0),
                "Zero elapsed time is never plausible.");
        }
    }
}
