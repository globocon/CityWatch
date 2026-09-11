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
    /// The service envelope defaults to Australia but must be configurable: hard-coded
    /// geography silently discards legitimate fixes — testing from another country, or any
    /// future expansion — and a silent discard is the worst kind of failure.
    /// </summary>
    [TestClass]
    public class ServiceAreaTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 14, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Session = Guid.NewGuid();
        private const int Unit = 42;

        // Sydney, and Kochi in India — well outside the Australian envelope.
        private const decimal SydLat = -33.8688m, SydLon = 151.2093m;
        private const decimal IndiaLat = 9.9312m, IndiaLon = 76.2673m;

        private TrackingDbContext _db = null!;
        private Channel<TrackPoint> _channel = null!;

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
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-1), Status = "Active"
            });
            await _db.SaveChangesAsync();
            _channel = Channel.CreateBounded<TrackPoint>(100);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private async Task<IngestResponse> IngestAsync(TrackingOptions options, decimal lat, decimal lon)
        {
            var service = new IngestService(_db, new InMemoryLiveStateStore(), _channel.Writer,
                new UnitRateLimiter(options), options, NullLogger<IngestService>.Instance,
                commands: null, utcNow: () => Now);
            return await service.IngestAsync(new PositionBatch
            {
                UnitId = Unit,
                SessionId = Session,
                DeviceUtc = Now,
                Points = { new PositionPoint { Seq = 1, Utc = Now.AddSeconds(-5), Lat = lat, Lon = lon, AccuracyM = 8 } }
            }, CancellationToken.None);
        }

        [TestMethod]
        public async Task ByDefault_AustraliaIsAccepted_AndElsewhereIsNot()
        {
            var options = new TrackingOptions();
            Assert.IsTrue(options.EnforceServiceArea, "Australia-only stays the default.");

            Assert.AreEqual(1, (await IngestAsync(options, SydLat, SydLon)).Accepted);
            Assert.AreEqual(1, (await IngestAsync(options, IndiaLat, IndiaLon)).Rejected,
                "A fix from outside the service area is a fault or a spoof by default.");
        }

        [TestMethod]
        public async Task EnforcementOff_AcceptsFixesFromAnywhere()
        {
            var options = new TrackingOptions { EnforceServiceArea = false };

            Assert.AreEqual(1, (await IngestAsync(options, IndiaLat, IndiaLon)).Accepted,
                "Testing from another country must not require a code change.");
        }

        [TestMethod]
        public async Task WidenedArea_AcceptsTheNewRegion_AndStillRejectsBeyondIt()
        {
            // An envelope spanning the Indian Ocean, covering both India and Australia.
            var options = new TrackingOptions
            {
                ServiceArea = new TrackingOptions.ServiceAreaOptions
                {
                    MinLat = -45.5m, MaxLat = 37.0m, MinLon = 68.0m, MaxLon = 156.5m
                }
            };

            Assert.AreEqual(1, (await IngestAsync(options, IndiaLat, IndiaLon)).Accepted);
            Assert.AreEqual(1, (await IngestAsync(options, SydLat, SydLon)).Accepted);
            Assert.AreEqual(1, (await IngestAsync(options, 51.5074m, -0.1278m)).Rejected,
                "London is still outside the widened envelope.");
        }

        [TestMethod]
        public async Task NonsenseCoordinates_AreAlwaysRejected_EvenWithEnforcementOff()
        {
            var options = new TrackingOptions { EnforceServiceArea = false };

            Assert.AreEqual(1, (await IngestAsync(options, 0m, 0m)).Rejected, "Null island.");
            Assert.AreEqual(1, (await IngestAsync(options, 91m, 200m)).Rejected, "Not a coordinate.");
        }
    }
}
