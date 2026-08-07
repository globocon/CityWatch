using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Events.Events;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Handlers;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The corrected model: the PATROL CAR is the tracked unit, identified by the Position
    /// picked at login ("Mobile Patrols (Car) M1"). NFC scans annotate where the car is —
    /// they do NOT gate GPS, which stays continuous so a missed scan cannot lose a journey.
    ///
    ///   in-car tag (officer's own fleet site)  -> departing -> Transit
    ///   site tag   (a client site)             -> arrived   -> AtSite
    /// </summary>
    [TestClass]
    public class PatrolCarStateTests
    {
        private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);
        private const int Unit = 42;
        private const int FleetSite = 625;      // "Citywatch M1 - Romeo Patrol Cars"
        private const int MarthaCove = 700;
        private const int Docklands = 701;

        private TrackingDbContext _db = null!;
        private InMemoryLiveStateStore _live = null!;
        private Channel<TrackPoint> _channel = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _db.TrackingUnitEnrolments.AddRange(
                new TrackingUnitEnrolment
                {
                    UnitId = Unit, IsEnabled = true, EnrolledUtc = Now.AddDays(-1),
                    EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1)
                },
                new TrackingUnitEnrolment   // the second car's device
                {
                    UnitId = 99, IsEnabled = true, EnrolledUtc = Now.AddDays(-1),
                    EnrolledByUserId = 1, ConsentRecordedUtc = Now.AddDays(-1)
                });
            await _db.SaveChangesAsync();
            _live = new InMemoryLiveStateStore();
            _channel = Channel.CreateBounded<TrackPoint>(200);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private SessionService Sessions()
            => new(_db, _live, NullLogger<SessionService>.Instance, segments: null, utcNow: () => Now);

        private NfcAnchorHandler Handler()
            => new(_db, _live, Sessions(), _channel.Writer, NullLogger<NfcAnchorHandler>.Instance);

        private Task<TrackingSession?> StartCarAsync(string position = "Mobile Patrols (Car) M1",
                                                     string callsign = "Romeo 03")
            => Sessions().StartAsync(Unit, 7, FleetSite, null, CancellationToken.None,
                                     isPatrolCar: true, callsign: callsign,
                                     positionId: 11, positionName: position);

        private static NfcCheckpointScanned Scan(int tagSiteId, string label, string? siteName = null,
                                                 bool offline = false, DateTime? at = null)
            => new(Unit, "04A2B1", tagSiteId, 7, null, "-33.8688,151.2093",
                   at ?? Now, 1, offline)
            {
                LoggedInClientSiteId = FleetSite,
                LabelDescription = label,
                TagSiteName = siteName
            };

        /* ---------------- identity ---------------- */

        [TestMethod]
        public async Task TheCarIsThePosition_NotTheDevice()
        {
            var session = await StartCarAsync();

            Assert.AreEqual("Mobile Patrols (Car) M1", session!.PatrolCarPositionName);
            Assert.AreEqual(11, session.PatrolCarPositionId);
            Assert.AreEqual("Romeo 03", session.Callsign);
        }

        [TestMethod]
        public async Task ReLogin_WithADifferentCar_UpdatesTheIdentity()
        {
            await StartCarAsync("Mobile Patrols (Car) M1");
            await Sessions().StartAsync(Unit, 7, FleetSite, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 05", positionId: 12,
                positionName: "Mobile Patrols (Car) M5");

            var stored = await _db.TrackingSessions.SingleAsync();
            Assert.AreEqual("Mobile Patrols (Car) M5", stored.PatrolCarPositionName);
            Assert.AreEqual("Romeo 05", stored.Callsign);
        }

        /* ---------------- tag classification ---------------- */

        [TestMethod]
        public void InCarTag_IdentifiedByLabel_OrByTheOfficersOwnFleetSite()
        {
            Assert.IsTrue(NfcAnchorHandler.IsInCarTag(Scan(FleetSite, "Romeo 03 (in-car)")),
                "Label says in-car.");
            Assert.IsTrue(NfcAnchorHandler.IsInCarTag(Scan(FleetSite, "TEMP")),
                "Tag belongs to the site the officer logged in to — their own fleet base.");
            Assert.IsFalse(NfcAnchorHandler.IsInCarTag(Scan(MarthaCove, "OC1 P01 - BQQ Area")),
                "A checkpoint at a client site is a site visit.");
            Assert.IsTrue(NfcAnchorHandler.IsInCarTag(Scan(MarthaCove, "Romeo 02 (in-car)")),
                "A mis-filed in-car tag still reads as in-car from its label.");
        }

        /* ---------------- the real shift ---------------- */

        [TestMethod]
        public async Task FullShift_TransitToSiteAndBackOut()
        {
            var session = await StartCarAsync();
            Assert.AreEqual("Transit", session!.TravelState, "A shift starts leaving the base.");

            // Arrives at Martha Cove and scans several checkpoints there.
            var handler = Handler();
            await handler.HandleAsync(Scan(MarthaCove, "OC1 P01 - BQQ Area", "Martha Cove Marina"), CancellationToken.None);
            var atSite = await _db.TrackingSessions.SingleAsync();
            Assert.AreEqual("AtSite", atSite.TravelState);
            Assert.AreEqual(MarthaCove, atSite.CurrentSiteId);
            Assert.AreEqual("Martha Cove Marina", atSite.CurrentSiteName);
            var arrivedAt = atSite.TravelStateSinceUtc;

            // More tags at the SAME site must not restart the clock.
            await handler.HandleAsync(Scan(MarthaCove, "OC1 P02 - Concrete Path", "Martha Cove Marina",
                                            at: Now.AddMinutes(4)), CancellationToken.None);
            var still = await _db.TrackingSessions.SingleAsync();
            Assert.AreEqual("AtSite", still.TravelState);
            Assert.AreEqual(arrivedAt, still.TravelStateSinceUtc,
                "Extra checkpoints confirm presence; they are not a fresh arrival.");

            // Back in the car -> departing.
            await handler.HandleAsync(Scan(FleetSite, "Romeo 03 (in-car)", at: Now.AddMinutes(12)),
                                       CancellationToken.None);
            var leaving = await _db.TrackingSessions.SingleAsync();
            Assert.AreEqual("Transit", leaving.TravelState);
            Assert.IsNull(leaving.CurrentSiteId, "Travelling means no current site.");
            Assert.AreEqual(Now.AddMinutes(12), leaving.TravelStateSinceUtc);

            // Arrives at the next site.
            await handler.HandleAsync(Scan(Docklands, "DL P01 - Gate", "Docklands", at: Now.AddMinutes(35)),
                                       CancellationToken.None);
            var next = await _db.TrackingSessions.SingleAsync();
            Assert.AreEqual("AtSite", next.TravelState);
            Assert.AreEqual(Docklands, next.CurrentSiteId);
        }

        [TestMethod]
        public async Task GpsKeepsRunning_RegardlessOfState()
        {
            // The whole point of continuous tracking: a missed in-car scan must not lose
            // the journey. Nothing here stops the sampler — state is only a label.
            await StartCarAsync();
            var handler = Handler();

            await handler.HandleAsync(Scan(MarthaCove, "OC1 P01", "Martha Cove"), CancellationToken.None);
            var atSite = await _db.TrackingSessions.SingleAsync();

            Assert.AreEqual("Active", atSite.Status,
                "Arriving at a site never ends or suspends the tracking session.");
            Assert.IsTrue(_channel.Reader.TryRead(out _),
                "The scan still writes its anchor point; GPS sampling is unaffected.");
        }

        [TestMethod]
        public async Task OfflineScanReplay_DoesNotRewriteWhereTheCarIsNow()
        {
            await StartCarAsync();
            var handler = Handler();
            await handler.HandleAsync(Scan(Docklands, "DL P01", "Docklands", at: Now), CancellationToken.None);

            // An hour-old queued scan from Martha Cove syncs late.
            await handler.HandleAsync(Scan(MarthaCove, "OC1 P01", "Martha Cove",
                                            offline: true, at: Now.AddHours(-1)), CancellationToken.None);

            var stored = await _db.TrackingSessions.SingleAsync();
            Assert.AreEqual(Docklands, stored.CurrentSiteId,
                "A replayed scan is history — it must not move the car back to a site it has left.");
        }

        [TestMethod]
        public async Task TwoCarsOfTheSameFleet_AtTheSameSite_StayDistinct()
        {
            // The exact case that broke the device-keyed model: M1 and M2 both at Martha
            // Cove, both scanning the SAME site tags.
            await StartCarAsync("Mobile Patrols (Car) M1", "Romeo 01");
            await Sessions().StartAsync(99, 8, FleetSite, null, CancellationToken.None,
                isPatrolCar: true, callsign: "Romeo 02", positionId: 12,
                positionName: "Mobile Patrols (Car) M2");

            var sessions = await _db.TrackingSessions.Where(s => s.Status == "Active").ToListAsync();
            Assert.AreEqual(2, sessions.Count);
            CollectionAssert.AreEquivalent(
                new[] { "Mobile Patrols (Car) M1", "Mobile Patrols (Car) M2" },
                sessions.Select(s => s.PatrolCarPositionName).ToArray(),
                "Cars are told apart by Position, never by the tags they scan.");
        }

        [TestMethod]
        public async Task ScanWithNoSession_IsIgnored()
        {
            await Handler().HandleAsync(Scan(MarthaCove, "OC1 P01", "Martha Cove"), CancellationToken.None);

            Assert.AreEqual(0, await _db.TrackingSessions.CountAsync());
        }
    }
}
