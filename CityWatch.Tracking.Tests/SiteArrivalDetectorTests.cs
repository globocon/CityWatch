using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
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
    /// The rule under test is the one that stops false alarms: crossing into a site's radius
    /// opens a CANDIDATE, and only dwelling there confirms it. A car driving past a site on
    /// the main road must never put an arrival in the control room's bell.
    /// </summary>
    [TestClass]
    public class SiteArrivalDetectorTests
    {
        private static readonly DateTime Now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid Session = Guid.NewGuid();
        private const int Unit = 2000010;

        /* Hyundai - Nunawading, straight from the production data shape. ~150 m at this
           latitude is ~0.00135° of latitude. */
        private const decimal SiteLat = -37.81805m;
        private const decimal SiteLon = 145.1849757m;

        private TrackingDbContext _db = null!;
        private TrackingOptions _options = null!;

        [TestInitialize]
        public void Setup()
        {
            /* NoTracking mirrors production DI (the 12 Aug lesson): a detector that forgets
               AsTracking on its mutating reads must fail here, not on the test server. */
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
            /* UseGpsDetection on: these tests exercise the GPS dwell logic itself. The
               production default is NFC-only — see Gps_detection_off_by_default below. */
            _options = new TrackingOptions
            {
                Enabled = true,
                SiteGeofence = { Enabled = true, UseGpsDetection = true, EnterRadiusM = 150, ExitRadiusM = 250, DwellSeconds = 120 }
            };
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private sealed class FixedCatalogue : ISiteGeofenceCatalogue
        {
            private readonly IReadOnlyList<GeofenceSite> _sites;
            public FixedCatalogue(params GeofenceSite[] sites) => _sites = sites;
            public Task<IReadOnlyList<GeofenceSite>> GetAsync(CancellationToken ct) => Task.FromResult(_sites);
        }

        private SiteArrivalDetector Detector(params GeofenceSite[] sites)
            => new(_db, new FixedCatalogue(sites), _options, NullLogger<SiteArrivalDetector>.Instance);

        private static GeofenceSite Hyundai => new(1, "Hyundai - Nunawading", SiteLat, SiteLon);

        /// <summary>Metres of latitude as degrees — the cheap way to place a fix at a
        /// known distance from the site.</summary>
        private static decimal MetresNorth(int m) => SiteLat + (decimal)(m / 111_000.0);

        private static GeoFix At(int metresFromSite, int secondsFromNow)
            => new(MetresNorth(metresFromSite), SiteLon, Now.AddSeconds(secondsFromNow));

        private Task Run(SiteArrivalDetector d, params GeoFix[] fixes)
            => d.EvaluateAsync(Unit, Session, isCar: true, fixes, CancellationToken.None);

        private List<TrackingSiteVisit> Visits() => _db.TrackingSiteVisits.OrderBy(v => v.Id).ToList();

        /* ---------------- arrival ---------------- */

        [TestMethod]
        public async Task Entering_and_dwelling_confirms_an_arrival()
        {
            var d = Detector(Hyundai);
            await Run(d, At(50, 0));                    // crossed in — candidate only
            Assert.IsNull(Visits().Single().ConfirmedUtc, "an arrival must not be announced on first contact");

            await Run(d, At(60, 150));                  // still inside after the dwell window
            var visit = Visits().Single();
            Assert.IsNotNull(visit.ConfirmedUtc, "dwelling inside must confirm the visit");
            Assert.AreEqual("Hyundai - Nunawading", visit.SiteName);
            Assert.AreEqual(Now, visit.EnteredUtc, "the arrival time is the crossing, not the confirmation");
            Assert.IsNull(visit.ExitedUtc);
        }

        [TestMethod]
        public async Task Drive_past_never_confirms()
        {
            var d = Detector(Hyundai);
            /* 60 km/h past the site: inside for two fixes 10 s apart, then gone. */
            await Run(d, At(100, 0), At(80, 10), At(400, 30));
            var visit = Visits().Single();
            Assert.IsNull(visit.ConfirmedUtc, "a drive-past must never reach the bell");
            Assert.IsNotNull(visit.ExitedUtc, "the pass is still recorded, closed, as evidence");
        }

        [TestMethod]
        public async Task Wobble_across_the_boundary_is_one_visit_not_three()
        {
            var d = Detector(Hyundai);
            /* GPS drift: 140 m, 200 m, 130 m. The 200 m fix is outside ENTER but inside
               EXIT — hysteresis says still there. */
            await Run(d, At(140, 0), At(200, 60), At(130, 150));
            var visits = Visits();
            Assert.AreEqual(1, visits.Count, "hysteresis must absorb boundary wobble");
            Assert.IsNotNull(visits[0].ConfirmedUtc, "150 s inside is a confirmed stay");
        }

        [TestMethod]
        public async Task Hysteresis_band_alone_never_confirms()
        {
            var d = Detector(Hyundai);
            /* Enters properly, then drifts to 200 m and sits there. Staying is allowed;
               confirming from the band is not — arrival demands the tighter radius. */
            await Run(d, At(200, 0));
            Assert.AreEqual(0, Visits().Count, "the hysteresis band must not open a candidate");
        }

        [TestMethod]
        public async Task Leaving_past_the_exit_radius_closes_the_visit()
        {
            var d = Detector(Hyundai);
            await Run(d, At(50, 0), At(50, 150));       // confirmed
            await Run(d, At(300, 400));                 // beyond ExitRadius
            var visit = Visits().Single();
            Assert.IsNotNull(visit.ConfirmedUtc);
            Assert.AreEqual(Now.AddSeconds(400), visit.ExitedUtc);
        }

        [TestMethod]
        public async Task Second_arrival_at_same_site_is_a_new_visit()
        {
            var d = Detector(Hyundai);
            await Run(d, At(50, 0), At(50, 150));       // stay 1, confirmed
            await Run(d, At(400, 300));                 // gone
            await Run(d, At(50, 600), At(50, 780));     // back again, dwells again
            var visits = Visits();
            Assert.AreEqual(2, visits.Count);
            Assert.IsTrue(visits.All(v => v.ConfirmedUtc != null));
            Assert.IsNotNull(visits[0].ExitedUtc);
            Assert.IsNull(visits[1].ExitedUtc);
        }

        [TestMethod]
        public async Task Guards_are_ignored_when_cars_only()
        {
            var d = Detector(Hyundai);
            await d.EvaluateAsync(Unit, Session, isCar: false,
                new[] { At(50, 0), At(50, 150) }, CancellationToken.None);
            Assert.AreEqual(0, Visits().Count, "a posted guard dwelling at their site is not an arrival event");
        }

        [TestMethod]
        public async Task Geofence_disabled_records_nothing()
        {
            _options.SiteGeofence.Enabled = false;
            var d = Detector(Hyundai);
            await Run(d, At(50, 0), At(50, 150));
            Assert.AreEqual(0, Visits().Count);
        }

        [TestMethod]
        public async Task Gps_detection_off_by_default_but_nfc_still_records()
        {
            /* The production stance (17 Aug): scans are the source of truth. With default
               options a car can sit inside the radius forever and GPS concludes nothing —
               but the site tag and the in-car tag still write the entered/left record. */
            _options = new TrackingOptions { Enabled = true };
            Assert.IsFalse(_options.SiteGeofence.UseGpsDetection, "GPS detection must be opt-in");

            var d = Detector(Hyundai);
            await Run(d, At(50, 0), At(50, 300));
            Assert.AreEqual(0, Visits().Count, "no GPS conclusions when detection is off");

            await d.ApplyScanAsync(Unit, Session, 1, "Hyundai - Nunawading", false, Now, CancellationToken.None);
            await d.ApplyScanAsync(Unit, Session, 625, null, isInCarTag: true, Now.AddMinutes(20), CancellationToken.None);
            var visit = Visits().Single();
            Assert.AreEqual(Now, visit.ConfirmedUtc, "site tag = entered");
            Assert.AreEqual(Now.AddMinutes(20), visit.ExitedUtc, "in-car tag = left");
        }

        [TestMethod]
        public async Task Nearest_site_wins_when_radii_overlap()
        {
            var other = new GeofenceSite(2, "Next Door", MetresNorth(120), SiteLon);
            var d = Detector(Hyundai, other);
            await Run(d, At(20, 0));                    // 20 m from Hyundai, 100 m from Next Door
            Assert.AreEqual(1, Visits().Single().SiteId, "the closer site claims the fix");
        }

        /* ---------------- NFC path ---------------- */

        [TestMethod]
        public async Task Nfc_scan_confirms_immediately_no_dwell()
        {
            var d = Detector(Hyundai);
            await d.ApplyScanAsync(Unit, Session, 390, "Martha Cove Marina", isInCarTag: false, Now, CancellationToken.None);
            var visit = Visits().Single();
            Assert.AreEqual(Now, visit.ConfirmedUtc, "a person tagging the site needs no dwell window");
            Assert.AreEqual("Nfc", visit.Source);
            Assert.AreEqual("Martha Cove Marina", visit.SiteName);
        }

        [TestMethod]
        public async Task Nfc_scan_upgrades_a_gps_candidate_instead_of_duplicating()
        {
            var d = Detector(Hyundai);
            await Run(d, At(50, 0));                    // GPS candidate at site 1, unconfirmed
            await d.ApplyScanAsync(Unit, Session, 1, "Hyundai - Nunawading", isInCarTag: false,
                Now.AddSeconds(30), CancellationToken.None);
            var visit = Visits().Single();
            Assert.IsNotNull(visit.ConfirmedUtc, "the scan is confirmation");
            Assert.AreEqual("Nfc", visit.Source);
        }

        [TestMethod]
        public async Task Repeated_tags_during_one_stay_stay_one_visit()
        {
            var d = Detector(Hyundai);
            await d.ApplyScanAsync(Unit, Session, 1, "Hyundai - Nunawading", false, Now, CancellationToken.None);
            await d.ApplyScanAsync(Unit, Session, 1, "Hyundai - Nunawading", false, Now.AddMinutes(5), CancellationToken.None);
            await d.ApplyScanAsync(Unit, Session, 1, "Hyundai - Nunawading", false, Now.AddMinutes(9), CancellationToken.None);
            Assert.AreEqual(1, Visits().Count, "several checkpoints per visit is normal, one arrival");
        }

        [TestMethod]
        public async Task In_car_tag_ends_the_stay()
        {
            var d = Detector(Hyundai);
            await d.ApplyScanAsync(Unit, Session, 1, "Hyundai - Nunawading", false, Now, CancellationToken.None);
            await d.ApplyScanAsync(Unit, Session, 625, null, isInCarTag: true, Now.AddMinutes(20), CancellationToken.None);
            var visit = Visits().Single();
            Assert.AreEqual(Now.AddMinutes(20), visit.ExitedUtc, "back in the car means the visit ended");
        }

        /* ---------------- the real scan wire ---------------- */

        [TestMethod]
        public async Task Scan_event_to_bell_record_the_whole_wire()
        {
            /* Production path: NfcCheckpointScanned → NfcAnchorHandler (session by GUARD,
               in-car decided by label/fleet-site) → SessionService → detector. Default
               options: GPS detection off, scans still write the entered/left record. */
            _options = new TrackingOptions { Enabled = true };
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 625,
                StartedUtc = Now.AddHours(-1), Status = "Active", IsPatrolCar = true, Callsign = "Romeo 03"
            });
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            var live = new InMemoryLiveStateStore();
            var channel = System.Threading.Channels.Channel.CreateBounded<TrackPoint>(100);
            var detector = Detector(Hyundai);
            var sessions = new SessionService(_db, live, NullLogger<SessionService>.Instance,
                segments: null, utcNow: () => Now, arrivals: detector);
            var handler = new Handlers.NfcAnchorHandler(_db, live, sessions, channel.Writer,
                NullLogger<Handlers.NfcAnchorHandler>.Instance);

            /* Officer tags the client site's checkpoint. */
            await handler.HandleAsync(new CityWatch.Events.Events.NfcCheckpointScanned(
                Unit, "044B45AA655281", 390, 7, null, "-37.8,145.1", Now, 1, false)
            { LoggedInClientSiteId = 625, LabelDescription = "Front gate", TagSiteName = "Martha Cove Marina" },
                CancellationToken.None);

            var visit = Visits().Single();
            Assert.AreEqual("Martha Cove Marina", visit.SiteName);
            Assert.AreEqual(Now, visit.ConfirmedUtc, "site tag = entered, instantly");

            /* Back at the car, officer tags the dashboard tag (their own fleet site). */
            await handler.HandleAsync(new CityWatch.Events.Events.NfcCheckpointScanned(
                Unit, "0448CFC2ED6E81", 625, 7, null, "-37.8,145.1", Now.AddMinutes(18), 2, false)
            { LoggedInClientSiteId = 625, LabelDescription = "Romeo 01 (in-car)" },
                CancellationToken.None);

            visit = Visits().Single();
            Assert.AreEqual(Now.AddMinutes(18), visit.ExitedUtc, "in-car tag = left");

            /* A replayed offline scan must never move the record. */
            await handler.HandleAsync(new CityWatch.Events.Events.NfcCheckpointScanned(
                Unit, "044B45AA655281", 390, 7, null, "-37.8,145.1", Now.AddMinutes(30), 3, true)
            { LoggedInClientSiteId = 625, TagSiteName = "Martha Cove Marina" },
                CancellationToken.None);
            Assert.AreEqual(1, Visits().Count, "offline replays are history, not news");
        }

        /* ---------------- GPS parsing ---------------- */

        [TestMethod]
        public void Gps_parser_accepts_the_production_format_and_rejects_junk()
        {
            Assert.IsTrue(SiteGeofenceCatalogue.TryParseGps("-37.81805,145.1849757", out var lat, out var lon));
            Assert.AreEqual(-37.81805m, lat);
            Assert.AreEqual(145.1849757m, lon);
            Assert.IsTrue(SiteGeofenceCatalogue.TryParseGps(" -37.8 , 145.2 ", out _, out _), "spaces exist in the wild");

            Assert.IsFalse(SiteGeofenceCatalogue.TryParseGps(null, out _, out _));
            Assert.IsFalse(SiteGeofenceCatalogue.TryParseGps("", out _, out _));
            Assert.IsFalse(SiteGeofenceCatalogue.TryParseGps("not a coordinate", out _, out _));
            Assert.IsFalse(SiteGeofenceCatalogue.TryParseGps("-37.8", out _, out _));
            Assert.IsFalse(SiteGeofenceCatalogue.TryParseGps("0,0", out _, out _), "null island is not a site");
            Assert.IsFalse(SiteGeofenceCatalogue.TryParseGps("91,145", out _, out _), "out of range");
        }
    }
}
