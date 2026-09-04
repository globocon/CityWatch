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
using CityWatch.Tracking.Services.Geofencing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// End to end through the real ingest pipeline: a phone's position batch, nothing else,
    /// is what produces the "entered site" record — and a batch the pipeline distrusts
    /// (low accuracy, mock provider) produces nothing.
    /// </summary>
    [TestClass]
    public class IngestGeofenceIntegrationTests
    {
        private static readonly DateTime Now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        private const int Unit = 2000010;
        private static readonly Guid Session = Guid.NewGuid();

        private const decimal SiteLat = -37.81805m;
        private const decimal SiteLon = 145.1849757m;

        private TrackingDbContext _db = null!;
        private TrackingOptions _options = null!;
        private InMemoryLiveStateStore _live = null!;
        private Channel<TrackPoint> _channel = null!;
        private DateTime _clock;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
            _options = new TrackingOptions
            {
                Enabled = true,
                EnforceServiceArea = false,
                SiteGeofence = { UseGpsDetection = true, EnterRadiusM = 150, ExitRadiusM = 250, DwellSeconds = 120 }
            };
            _live = new InMemoryLiveStateStore();
            _channel = Channel.CreateBounded<TrackPoint>(1000);
            _clock = Now;

            _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = Unit, IsEnabled = true, EnrolledUtc = Now.AddDays(-1),
                EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1)
            });
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 625,
                StartedUtc = Now.AddHours(-1), Status = "Active", IsPatrolCar = true, Callsign = "Romeo 03"
            });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();   // seeded entities must not shadow the service's own reads
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private sealed class FixedCatalogue : ISiteGeofenceCatalogue
        {
            public Task<IReadOnlyList<GeofenceSite>> GetAsync(CancellationToken ct)
                => Task.FromResult<IReadOnlyList<GeofenceSite>>(
                    new[] { new GeofenceSite(1, "Hyundai - Nunawading", SiteLat, SiteLon) });
        }

        private IngestService Ingest()
        {
            /* Production gives every request a fresh scoped context; one long-lived test
               context must shed its identity map between "requests" or the second batch's
               session Update trips over the first's tracked instance. */
            _db.ChangeTracker.Clear();
            return new(_db, _live, _channel.Writer, new UnitRateLimiter(_options), _options,
                NullLogger<IngestService>.Instance, commands: null, utcNow: () => _clock,
                arrivals: new SiteArrivalDetector(_db, new FixedCatalogue(), _options,
                    NullLogger<SiteArrivalDetector>.Instance));
        }

        private static PositionBatch Batch(int seqFrom, params PositionPoint[] points)
        {
            var seq = seqFrom;
            foreach (var p in points) p.Seq = seq++;
            return new PositionBatch { UnitId = Unit, SessionId = Session, DeviceUtc = Now, Points = points.ToList() };
        }

        private static PositionPoint AtSite(int secondsFromNow, double? accuracyM = 15, bool mock = false)
            => new()
            {
                Utc = Now.AddSeconds(secondsFromNow),
                Lat = SiteLat + 0.0003m,      // ~33 m from the site centre
                Lon = SiteLon,
                AccuracyM = accuracyM,
                IsMock = mock,
                Source = "transit"
            };

        [TestMethod]
        public async Task A_position_batch_alone_produces_the_arrival()
        {
            var first = await Ingest().IngestAsync(Batch(1, AtSite(0)), CancellationToken.None);
            Assert.AreEqual(1, first.Accepted);

            _clock = Now.AddSeconds(150);
            var second = await Ingest().IngestAsync(Batch(2, AtSite(150)), CancellationToken.None);
            Assert.AreEqual(1, second.Accepted);

            var visit = _db.TrackingSiteVisits.Single();
            Assert.IsNotNull(visit.ConfirmedUtc, "dwelling at the site through ingest must confirm the arrival");
            Assert.AreEqual("Hyundai - Nunawading", visit.SiteName);
            Assert.AreEqual(Session, visit.SessionId);
        }

        [TestMethod]
        public async Task Distrusted_fixes_never_reach_the_geofence()
        {
            /* Accepted (stored as evidence, flagged) but 500 m accuracy sits "inside" half
               the suburb — the geofence must not conclude anything from it. */
            await Ingest().IngestAsync(Batch(1, AtSite(0, accuracyM: 500)), CancellationToken.None);
            _clock = Now.AddSeconds(150);
            await Ingest().IngestAsync(Batch(2, AtSite(150, accuracyM: 500)), CancellationToken.None);
            Assert.AreEqual(0, _db.TrackingSiteVisits.Count(), "low-accuracy fixes must not open visits");
        }

        [TestMethod]
        public async Task Mock_provider_fixes_never_reach_the_geofence()
        {
            await Ingest().IngestAsync(Batch(1, AtSite(0, mock: true)), CancellationToken.None);
            _clock = Now.AddSeconds(150);
            await Ingest().IngestAsync(Batch(2, AtSite(150, mock: true)), CancellationToken.None);
            Assert.AreEqual(0, _db.TrackingSiteVisits.Count(), "a spoofed location must not write history");
        }

        [TestMethod]
        public async Task Geofence_failure_never_rejects_the_positions()
        {
            /* A detector that throws must cost an alert, not evidence. */
            var svc = new IngestService(_db, _live, _channel.Writer, new UnitRateLimiter(_options),
                _options, NullLogger<IngestService>.Instance, commands: null, utcNow: () => _clock,
                arrivals: new ThrowingDetector());
            var res = await svc.IngestAsync(Batch(1, AtSite(0)), CancellationToken.None);
            Assert.AreEqual(1, res.Accepted, "positions were valid; the geofence fault is its own problem");
            Assert.AreEqual(0, res.Rejected);
        }

        private sealed class ThrowingDetector : ISiteArrivalDetector
        {
            public Task EvaluateAsync(int unitId, Guid sessionId, bool isCar, IReadOnlyList<GeoFix> fixes, CancellationToken ct)
                => throw new InvalidOperationException("catalogue on fire");
            public Task ApplyScanAsync(int unitId, Guid sessionId, int siteId, string? siteName, bool isInCarTag,
                DateTime occurredUtc, CancellationToken ct)
                => throw new InvalidOperationException("catalogue on fire");
        }
    }
}
