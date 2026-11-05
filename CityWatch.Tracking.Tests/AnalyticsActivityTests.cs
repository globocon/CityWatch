using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using CityWatch.Tracking.Api;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The A2 activity cards: guards / sites / patrol cars / smart wands, plus the pulse.
    /// The rules under test are the ones a control room would notice if broken: a guard
    /// who only scanned still worked; a site someone signed in to but produced no
    /// evidence at is QUIET, not absent; a car's legs belong to the window the roll-up
    /// closed in; and a wand that has gone silent leads the wand list, never hides.
    /// </summary>
    [TestClass]
    public class AnalyticsActivityTests
    {
        private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Midnight = Now.Date;

        private static readonly int FootUnit7 = TrackingUnitKey.FromGuard(7);
        private static readonly int FootUnit11 = TrackingUnitKey.FromGuard(11);
        private static readonly int CarUnit = TrackingUnitKey.FromPosition(10);

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.PlatformGuards.AddRange(
                new PlatformGuard { Id = 7, Name = "J. Smith" },
                new PlatformGuard { Id = 9, Name = "P. Kumar" },
                new PlatformGuard { Id = 11, Name = "A. Thomas" },
                new PlatformGuard { Id = 13, Name = "M. Silva" });
            _db.PlatformClientSites.AddRange(
                new PlatformClientSite { Id = 12, Name = "Impact Malvern", IsActive = true },
                new PlatformClientSite { Id = 30, Name = "Citywatch M1 - Romeo Patrol Cars", IsActive = true });
            _db.PlatformSmartWands.AddRange(
                new PlatformSmartWand { Id = 1, ClientSiteId = 12, WandName = "Dell 5430" },
                new PlatformSmartWand { Id = 2, ClientSiteId = 12, WandName = "W2" });

            /* Guard 7 on foot since 05:00; car R3 02:00–08:00; guard 11 signed in to
               site 60 (which never produces evidence — the quiet site). */
            _db.TrackingSessions.AddRange(
                Session(FootUnit7, 7, 12, Midnight.AddHours(5), null, false),
                Session(CarUnit, 9, 30, Midnight.AddHours(2), Midnight.AddHours(8), true, "R3"),
                Session(FootUnit11, 11, 60, Midnight.AddHours(1), Midnight.AddHours(2), false));

            /* Visits: guard 7 twice and the car once, all at site 12. */
            _db.TrackingSiteVisits.AddRange(
                Visit(FootUnit7, 12, Midnight.AddHours(6)),
                Visit(CarUnit, 12, Midnight.AddHours(6.5)),
                Visit(FootUnit7, 12, Midnight.AddHours(8)));

            /* Scans in the window: wand 1 twice by guard 7 (tag site 12), and one by the
               scan-only guard 13 against site 40's tag. Wand 2 is silent today but
               scanned 14 times across the 7 baseline days; wand 1's baseline is 7. */
            _db.PlatformWandScans.AddRange(
                Scan(1, 7, 12, Midnight.AddHours(6.17)),
                Scan(1, 7, 12, Midnight.AddHours(7.17)),
                Scan(1, 13, 40, Midnight.AddHours(9)));
            for (var day = 1; day <= 7; day++)
            {
                _db.PlatformWandScans.Add(Scan(1, 7, 12, Midnight.AddDays(-day).AddHours(6)));
                _db.PlatformWandScans.Add(Scan(2, 7, 12, Midnight.AddDays(-day).AddHours(7)));
                _db.PlatformWandScans.Add(Scan(2, 7, 12, Midnight.AddDays(-day).AddHours(19)));
            }

            /* Car legs: two closed inside the window, one closed yesterday (excluded). */
            _db.TrackSegments.AddRange(
                Segment(CarUnit, Midnight.AddHours(3), Midnight.AddHours(4), 5000),
                Segment(CarUnit, Midnight.AddHours(5), Midnight.AddHours(6), 7000),
                Segment(CarUnit, Midnight.AddDays(-1).AddHours(3), Midnight.AddDays(-1).AddHours(4), 9000));

            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private static TrackingSession Session(int unitId, int guardId, int siteId,
            DateTime startedUtc, DateTime? endedUtc, bool? isPatrolCar, string? callsign = null) => new()
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            GuardId = guardId,
            ClientSiteId = siteId,
            StartedUtc = startedUtc,
            EndedUtc = endedUtc,
            Status = endedUtc == null ? "Active" : "Completed",
            IsPatrolCar = isPatrolCar,
            Callsign = callsign,
            LastFixUtc = endedUtc ?? Now
        };

        private static TrackingSiteVisit Visit(int unitId, int siteId, DateTime confirmedUtc) => new()
        {
            UnitId = unitId,
            SessionId = Guid.NewGuid(),
            SiteId = siteId,
            SiteName = "Site " + siteId,
            EnteredUtc = confirmedUtc.AddMinutes(-2),
            ConfirmedUtc = confirmedUtc
        };

        private static PlatformWandScan Scan(int wandId, int guardId, int tagSiteId, DateTime hitUtc) => new()
        {
            SmartWandId = wandId,
            LoggedInGuardId = guardId,
            LoggedInClientSiteId = 12,
            TagLinkedClientSiteId = tagSiteId,
            HitUtcDateTime = hitUtc
        };

        private static TrackSegment Segment(int unitId, DateTime startUtc, DateTime endUtc, int distanceM) => new()
        {
            UnitId = unitId,
            SessionId = Guid.NewGuid(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            DistanceM = distanceM,
            DurationSec = (int)(endUtc - startUtc).TotalSeconds,
            PointCount = 10
        };

        private AnalyticsController Controller(bool enabled = true) =>
            new(_db, new TrackingOptions
            {
                Analytics = new TrackingOptions.AnalyticsOptions { Enabled = enabled }
            })
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.Sid, "77") }, "test"))
                    }
                }
            };

        private static JsonElement Body(IActionResult result)
        {
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "Endpoint should return 200.");
            return JsonSerializer.SerializeToElement(ok!.Value);
        }

        [TestMethod]
        public async Task Guards_ScanOnlyGuardCounts_AndRankingIsEventsFirst()
        {
            var body = Body(await Controller().Guards(Midnight, Now, default));
            var rows = body.GetProperty("guards").EnumerateArray().ToList();

            Assert.AreEqual(3, rows.Count, "Session guards + the scan-only guard.");
            Assert.AreEqual("J. Smith", rows[0].GetProperty("name").GetString(),
                "Guard 7 leads: 2 check-ins + 2 visits.");
            Assert.AreEqual(2, rows[0].GetProperty("visits").GetInt32());
            Assert.AreEqual(2, rows[0].GetProperty("checkIns").GetInt32());
            Assert.AreEqual(300, rows[0].GetProperty("activeMinutes").GetInt32(), "05:00 → 10:00.");
            Assert.AreEqual("M. Silva", rows[1].GetProperty("name").GetString(),
                "One scan and no session still ranks above a session with zero events.");
            Assert.AreEqual(0, rows[1].GetProperty("sessions").GetInt32());
            Assert.AreEqual("A. Thomas", rows[2].GetProperty("name").GetString());
        }

        [TestMethod]
        public async Task Sites_BusiestFirst_AndSignedInSilenceIsQuiet()
        {
            var body = Body(await Controller().Sites(Midnight, Now, default));
            var sites = body.GetProperty("sites").EnumerateArray().ToList();

            Assert.AreEqual("Impact Malvern", sites[0].GetProperty("name").GetString());
            Assert.AreEqual(3, sites[0].GetProperty("visits").GetInt32());
            Assert.AreEqual(2, sites[0].GetProperty("checkIns").GetInt32());
            Assert.AreEqual(2, sites[0].GetProperty("units").GetInt32(), "Guard 7 and the car.");
            Assert.AreEqual("Site 40", sites[1].GetProperty("name").GetString(),
                "A scan against an unnamed site still surfaces it.");

            var quiet = body.GetProperty("quiet").EnumerateArray()
                .Select(q => q.GetProperty("siteId").GetInt32()).ToList();
            CollectionAssert.Contains(quiet, 60, "Signed in, no visit, no scan — the finding.");
            CollectionAssert.Contains(quiet, 30, "The car base had sessions but no evidence rows.");
            CollectionAssert.DoesNotContain(quiet, 12);
        }

        [TestMethod]
        public async Task Pcars_LegsBelongToTheWindowTheyClosedIn()
        {
            var body = Body(await Controller().Pcars(Midnight, Now, default));
            var cars = body.GetProperty("cars").EnumerateArray().ToList();

            Assert.AreEqual(1, cars.Count);
            var car = cars[0];
            Assert.AreEqual("R3", car.GetProperty("label").GetString());
            Assert.AreEqual("P. Kumar", car.GetProperty("guardName").GetString());
            Assert.AreEqual(2, car.GetProperty("legs").GetInt32(), "Yesterday's leg is excluded.");
            Assert.AreEqual(12.0, car.GetProperty("distanceKm").GetDouble());
            Assert.AreEqual(1, car.GetProperty("visits").GetInt32());
            Assert.AreEqual(360, car.GetProperty("activeMinutes").GetInt32(), "02:00 → 08:00.");
        }

        [TestMethod]
        public async Task Wands_SilentWandLeads_WithItsBaselineAndLastScan()
        {
            var body = Body(await Controller().Wands(Midnight, Now, default));
            var wands = body.GetProperty("wands").EnumerateArray().ToList();

            Assert.AreEqual(2, wands.Count);
            var silent = wands[0];
            Assert.AreEqual("W2", silent.GetProperty("name").GetString(),
                "Zero scans against a 2/day baseline is the worst ratio — it leads.");
            Assert.AreEqual(0, silent.GetProperty("scans").GetInt32());
            Assert.AreEqual(2.0, silent.GetProperty("prevDailyAvg").GetDouble());
            Assert.AreEqual("Impact Malvern", silent.GetProperty("siteName").GetString());
            Assert.IsTrue(silent.GetProperty("lastScanUtc").GetDateTime() < Midnight,
                "A silent wand still shows WHEN it last scanned.");

            var busy = wands[1];
            Assert.AreEqual("Dell 5430", busy.GetProperty("name").GetString());
            Assert.AreEqual(3, busy.GetProperty("scans").GetInt32());
            Assert.AreEqual(1.0, busy.GetProperty("prevDailyAvg").GetDouble());
        }

        [TestMethod]
        public async Task Summary_CarriesTheHourlyPulse()
        {
            var body = Body(await Controller().Summary(Midnight, Now, default));
            var pulse = body.GetProperty("pulse");

            Assert.AreEqual(1, pulse.GetProperty("bucketHours").GetInt32());
            var buckets = pulse.GetProperty("buckets").EnumerateArray().ToList();
            Assert.AreEqual(10, buckets.Count, "Ten whole hours since midnight.");
            /* Hour 06: one scan (06:10) + two confirmed visits (06:00, 06:30). */
            Assert.AreEqual(3, buckets[6].GetProperty("current").GetInt32());
            /* Hour 05: guard 7's sign-in. */
            Assert.AreEqual(1, buckets[5].GetProperty("current").GetInt32());
            /* Yesterday same hours had baseline scans at 06:00 and 07:00 + a car leg?
               No sessions started, no visits — hour 06 previous = the 06:00 scan. */
            Assert.AreEqual(1, buckets[6].GetProperty("previous").GetInt32());
        }

        [TestMethod]
        public async Task Summary_SevenDayWindow_BucketsByDay()
        {
            var body = Body(await Controller().Summary(Now.AddDays(-7), Now, default));
            var pulse = body.GetProperty("pulse");

            Assert.AreEqual(24, pulse.GetProperty("bucketHours").GetInt32());
            Assert.AreEqual(7, pulse.GetProperty("buckets").GetArrayLength());
        }

        [TestMethod]
        public async Task ActivityEndpoints_DisabledModule_Answers404()
        {
            Assert.IsInstanceOfType(await Controller(enabled: false).Guards(Midnight, Now, default), typeof(NotFoundResult));
            Assert.IsInstanceOfType(await Controller(enabled: false).Wands(Midnight, Now, default), typeof(NotFoundResult));
        }
    }
}
