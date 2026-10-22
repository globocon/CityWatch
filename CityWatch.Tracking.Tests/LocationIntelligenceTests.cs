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
using CityWatch.Tracking.Services.Geocoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>Phase 2: stops, cached geocoding, honest speed fallback.</summary>
    [TestClass]
    public class LocationIntelligenceTests
    {
        private static readonly DateTime Now = new(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc);

        /* ---------------- stop detection ---------------- */

        private static StopDetector.TrailPoint P(decimal lat, decimal lon, int minute)
            => new(lat, lon, Now.AddMinutes(minute));

        [TestMethod]
        public void StopDetector_FindsAStop_AndIgnoresJitter()
        {
            /* Drive, then sit for 14 minutes wobbling within GPS noise (~20 m), then drive. */
            var points = new List<StopDetector.TrailPoint>
            {
                P(9.9000m, 76.2000m, 0),
                P(9.9100m, 76.2000m, 2),
                P(9.9200m, 76.2000m, 4),      // arrives
                P(9.92005m, 76.20005m, 6),    // jitter
                P(9.91995m, 76.19995m, 9),    // jitter
                P(9.92002m, 76.20002m, 14),
                P(9.92000m, 76.20000m, 18),   // still here — 14 min so far
                P(9.9300m, 76.2100m, 20),     // leaves
                P(9.9400m, 76.2200m, 22)
            };

            var stops = StopDetector.Detect(points, radiusM: 60, minMinutes: 4);

            Assert.AreEqual(1, stops.Count, "One meaningful stop, jitter never splits it.");
            Assert.AreEqual(14, stops[0].DurationMinutes);
            Assert.IsTrue(Math.Abs(stops[0].Lat - 9.92m) < 0.001m);
        }

        [TestMethod]
        public void StopDetector_ShortPause_IsNotAStop()
        {
            var points = new List<StopDetector.TrailPoint>
            {
                P(9.90m, 76.20m, 0),
                P(9.91m, 76.20m, 2),
                P(9.91m, 76.20m, 4),          // 2-minute pause: traffic light, not a stop
                P(9.92m, 76.20m, 6),
                P(9.93m, 76.20m, 8)
            };

            Assert.AreEqual(0, StopDetector.Detect(points, radiusM: 60, minMinutes: 4).Count);
        }

        [TestMethod]
        public void StopDetector_StopAtTrailEnd_IsReported()
        {
            var points = new List<StopDetector.TrailPoint>
            {
                P(9.90m, 76.20m, 0),
                P(9.95m, 76.20m, 2),
                P(9.95m, 76.2000m, 5),
                P(9.95001m, 76.20001m, 12)     // still parked when the window closes
            };

            var stops = StopDetector.Detect(points, radiusM: 60, minMinutes: 4);
            Assert.AreEqual(1, stops.Count);
            Assert.AreEqual(10, stops[0].DurationMinutes);
        }

        /* ---------------- geocode cache ---------------- */

        private sealed class CountingGeocoder : IReverseGeocoder
        {
            public int Calls;
            public string? Answer = "Main Road, Pala";
            public Task<string?> ResolveAsync(decimal lat, decimal lon, CancellationToken ct)
            {
                Calls++;
                return Task.FromResult(Answer);
            }
        }

        private static TrackingDbContext Db() => new(new DbContextOptionsBuilder<TrackingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [TestMethod]
        public async Task Geocode_SameCell_HitsProviderOnce()
        {
            using var db = Db();
            var provider = new CountingGeocoder();
            var svc = new GeocodeService(db, provider, new TrackingOptions(),
                NullLogger<GeocodeService>.Instance, () => Now);

            var first = await svc.GetAddressAsync(9.93125m, 76.26731m, default);
            var second = await svc.GetAddressAsync(9.93129m, 76.26739m, default);   // same ~110 m cell

            Assert.AreEqual("Main Road, Pala", first);
            Assert.AreEqual("Main Road, Pala", second);
            Assert.AreEqual(1, provider.Calls, "The cache is the rate-limit protection.");
        }

        [TestMethod]
        public async Task Geocode_FailureIsCached_NotRetriedPerRequest()
        {
            using var db = Db();
            var provider = new CountingGeocoder { Answer = null };
            var svc = new GeocodeService(db, provider, new TrackingOptions(),
                NullLogger<GeocodeService>.Instance, () => Now);

            Assert.IsNull(await svc.GetAddressAsync(9.9m, 76.2m, default));
            Assert.IsNull(await svc.GetAddressAsync(9.9m, 76.2m, default));
            Assert.AreEqual(1, provider.Calls, "A provider outage must not become a retry storm.");
        }

        [TestMethod]
        public async Task Geocode_Disabled_NeverCallsProvider()
        {
            using var db = Db();
            var provider = new CountingGeocoder();
            var options = new TrackingOptions();
            options.Geocoding.Enabled = false;
            var svc = new GeocodeService(db, provider, options,
                NullLogger<GeocodeService>.Instance, () => Now);

            Assert.IsNull(await svc.GetAddressAsync(9.9m, 76.2m, default));
            Assert.AreEqual(0, provider.Calls);
        }

        /* ---------------- live speed fallback ---------------- */

        private const int Unit = 2000010;
        private static readonly Guid Session = Guid.NewGuid();

        private static async Task<(IngestService svc, InMemoryLiveStateStore store)> IngestSetup()
        {
            var db = Db();
            db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = Unit, IsEnabled = true, EnrolledUtc = Now.AddDays(-1),
                EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1)
            });
            db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 1,
                StartedUtc = Now.AddHours(-1), Status = "Active"
            });
            await db.SaveChangesAsync();
            var store = new InMemoryLiveStateStore();
            var svc = new IngestService(db, store, Channel.CreateBounded<TrackPoint>(1000).Writer,
                new UnitRateLimiter(new TrackingOptions()),
                new TrackingOptions { EnforceServiceArea = false },
                NullLogger<IngestService>.Instance, commands: null, utcNow: () => Now);
            return (svc, store);
        }

        private static PositionBatch Batch(params PositionPoint[] points) => new()
        {
            UnitId = Unit, SessionId = Session, DeviceUtc = Now, Points = points.ToList()
        };

        [TestMethod]
        public async Task Ingest_NoDeviceSpeed_DerivesFromFixes_AndSaysSo()
        {
            var (svc, store) = await IngestSetup();

            /* Two fixes 60 s apart, ~1 km apart → ~60 km/h. */
            await svc.IngestAsync(Batch(
                new PositionPoint { Seq = 1, Utc = Now.AddSeconds(-70), Lat = 9.9000m, Lon = 76.2000m, AccuracyM = 8 }), default);
            await svc.IngestAsync(Batch(
                new PositionPoint { Seq = 2, Utc = Now.AddSeconds(-10), Lat = 9.9090m, Lon = 76.2000m, AccuracyM = 8 }), default);

            var state = store.Get(Unit)!;
            Assert.IsTrue(state.SpeedDerived, "Derived speed must be marked derived.");
            Assert.IsNotNull(state.SpeedKph);
            Assert.IsTrue(state.SpeedKph is > 40 and < 80, $"Implied ~60 km/h, got {state.SpeedKph}.");
        }

        [TestMethod]
        public async Task Ingest_DeviceSpeed_IsNeverOverridden()
        {
            var (svc, store) = await IngestSetup();

            await svc.IngestAsync(Batch(
                new PositionPoint { Seq = 1, Utc = Now.AddSeconds(-70), Lat = 9.9000m, Lon = 76.2000m, AccuracyM = 8 },
                new PositionPoint { Seq = 2, Utc = Now.AddSeconds(-10), Lat = 9.9090m, Lon = 76.2000m, AccuracyM = 8, SpeedKph = 42 }), default);

            var state = store.Get(Unit)!;
            Assert.AreEqual((short)42, state.SpeedKph);
            Assert.IsFalse(state.SpeedDerived);
        }

        [TestMethod]
        public async Task Ingest_GpsJump_ProducesNoFakeSpeed()
        {
            var (svc, store) = await IngestSetup();

            /* 60 km in 60 s = 3600 km/h — flagged Implausible; speed must stay null. */
            await svc.IngestAsync(Batch(
                new PositionPoint { Seq = 1, Utc = Now.AddSeconds(-70), Lat = 9.9000m, Lon = 76.2000m, AccuracyM = 8 }), default);
            await svc.IngestAsync(Batch(
                new PositionPoint { Seq = 2, Utc = Now.AddSeconds(-10), Lat = 10.4400m, Lon = 76.2000m, AccuracyM = 8 }), default);

            var state = store.Get(Unit)!;
            Assert.IsNull(state.SpeedKph, "A teleport must not be dressed up as a speed.");
        }
    }
}
