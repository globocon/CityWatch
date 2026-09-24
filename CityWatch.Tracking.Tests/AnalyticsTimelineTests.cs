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
    /// The A3 drill-down timeline: one entity's merged day — sign-ins, arrivals with the
    /// stay, scans, legs — in order, named the way the map names things (an officer by
    /// name, a car by callsign). One entity per call, always; and a car's timeline
    /// carries the scans its driver made, because the car doesn't scan — the officer does.
    /// </summary>
    [TestClass]
    public class AnalyticsTimelineTests
    {
        private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Midnight = Now.Date;

        private static readonly int FootUnit = TrackingUnitKey.FromGuard(7);
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
                new PlatformGuard { Id = 9, Name = "P. Kumar" });
            _db.PlatformClientSites.AddRange(
                new PlatformClientSite { Id = 12, Name = "Impact Malvern", IsActive = true },
                new PlatformClientSite { Id = 30, Name = "Citywatch M1 - Romeo Patrol Cars", IsActive = true },
                new PlatformClientSite { Id = 40, Name = "Oxford St", IsActive = true });
            _db.PlatformSmartWands.Add(new PlatformSmartWand { Id = 1, ClientSiteId = 12, WandName = "Dell 5430" });

            _db.TrackingSessions.AddRange(
                new TrackingSession
                {
                    Id = Guid.NewGuid(), UnitId = FootUnit, GuardId = 7, ClientSiteId = 12,
                    StartedUtc = Midnight.AddHours(5), Status = "Active", IsPatrolCar = false,
                    LastFixUtc = Now
                },
                new TrackingSession
                {
                    Id = Guid.NewGuid(), UnitId = CarUnit, GuardId = 9, ClientSiteId = 30,
                    StartedUtc = Midnight.AddHours(2), EndedUtc = Midnight.AddHours(8),
                    Status = "Completed", IsPatrolCar = true, Callsign = "R3",
                    LastFixUtc = Midnight.AddHours(8)
                });

            _db.TrackingSiteVisits.AddRange(
                new TrackingSiteVisit
                {
                    UnitId = FootUnit, SessionId = Guid.NewGuid(), SiteId = 12, SiteName = "Impact Malvern",
                    EnteredUtc = Midnight.AddMinutes(5 * 60 + 58), ConfirmedUtc = Midnight.AddHours(6),
                    ExitedUtc = Midnight.AddMinutes(6 * 60 + 45)
                },
                new TrackingSiteVisit
                {
                    UnitId = CarUnit, SessionId = Guid.NewGuid(), SiteId = 12, SiteName = "Impact Malvern",
                    EnteredUtc = Midnight.AddHours(6.4), ConfirmedUtc = Midnight.AddHours(6.5)
                });

            _db.PlatformWandScans.AddRange(
                new PlatformWandScan
                {
                    SmartWandId = 1, LoggedInGuardId = 9, LoggedInClientSiteId = 30,
                    TagLinkedClientSiteId = 12, HitUtcDateTime = Midnight.AddHours(3)
                },
                new PlatformWandScan
                {
                    SmartWandId = 1, LoggedInGuardId = 7, LoggedInClientSiteId = 12,
                    TagLinkedClientSiteId = 12, HitUtcDateTime = Midnight.AddHours(6.17)
                },
                new PlatformWandScan
                {
                    SmartWandId = null, LoggedInGuardId = 7, LoggedInClientSiteId = 12,
                    TagLinkedClientSiteId = 40, HitUtcDateTime = Midnight.AddHours(7)
                });

            _db.TrackSegments.AddRange(
                new TrackSegment
                {
                    UnitId = CarUnit, SessionId = Guid.NewGuid(), StartUtc = Midnight.AddHours(3),
                    EndUtc = Midnight.AddHours(3.5), DistanceM = 5000, DurationSec = 1800,
                    PointCount = 10, ToSiteId = 12
                },
                new TrackSegment
                {
                    UnitId = FootUnit, SessionId = Guid.NewGuid(), StartUtc = Midnight.AddMinutes(6 * 60 + 50),
                    EndUtc = Midnight.AddMinutes(7 * 60 + 20), DistanceM = 1200, DurationSec = 1800,
                    PointCount = 10
                });

            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private AnalyticsController Controller() =>
            new(_db, new TrackingOptions
            {
                Analytics = new TrackingOptions.AnalyticsOptions { Enabled = true }
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

        private async Task<JsonElement> TimelineAsync(int? guardId = null, int? unitId = null,
            int? siteId = null, int? wandId = null)
        {
            var result = await Controller().Timeline(Midnight, Now, guardId, unitId, siteId, wandId, default);
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "Timeline should return 200.");
            return JsonSerializer.SerializeToElement(ok!.Value);
        }

        private static string[] Types(JsonElement body) =>
            body.GetProperty("events").EnumerateArray()
                .Select(e => e.GetProperty("Type").GetString()!).ToArray();

        [TestMethod]
        public async Task Guard_MergesHisWholeDay_InOrder()
        {
            var body = await TimelineAsync(guardId: 7);

            CollectionAssert.AreEqual(new[] { "signin", "arrived", "scan", "scan", "leg" }, Types(body),
                "05:00 sign-in, 06:00 arrival, 06:10 and 07:00 scans, 07:20 leg — chronological.");
            var events = body.GetProperty("events").EnumerateArray().ToList();
            Assert.IsTrue(events.All(e => e.GetProperty("Who").GetString() == "J. Smith"));
            var arrived = events[1];
            Assert.AreEqual(47, arrived.GetProperty("Minutes").GetInt32(),
                "The stay is entered→exited, the honest duration.");
            Assert.AreEqual("Oxford St", events[3].GetProperty("SiteName").GetString(),
                "A scan is evidence for the TAG's site.");
            Assert.AreEqual(1.2, events[4].GetProperty("Km").GetDouble());
        }

        [TestMethod]
        public async Task Site_NamesEveryVisitor_GuardByName_CarByCallsign()
        {
            var body = await TimelineAsync(siteId: 12);
            var events = body.GetProperty("events").EnumerateArray().ToList();

            CollectionAssert.AreEqual(new[] { "scan", "signin", "arrived", "scan", "arrived" }, Types(body));
            Assert.AreEqual("P. Kumar", events[0].GetProperty("Who").GetString(),
                "The 03:00 tag scan carries the officer who made it.");
            Assert.AreEqual("J. Smith", events[2].GetProperty("Who").GetString());
            var carArrival = events[4];
            Assert.AreEqual("R3", carArrival.GetProperty("Who").GetString(),
                "A car answers to its callsign, exactly like the map.");
            Assert.IsTrue(carArrival.GetProperty("Minutes").ValueKind == JsonValueKind.Null,
                "No exit yet — no invented duration.");
        }

        [TestMethod]
        public async Task Car_CarriesItsDriversScans_AndItsLegs()
        {
            var body = await TimelineAsync(unitId: CarUnit);

            CollectionAssert.AreEqual(new[] { "signin", "scan", "leg", "arrived", "signout" }, Types(body));
            var events = body.GetProperty("events").EnumerateArray().ToList();
            Assert.AreEqual("R3", events[0].GetProperty("Who").GetString());
            Assert.AreEqual("P. Kumar", events[1].GetProperty("Who").GetString(),
                "The car does not scan — its officer does, and the timeline says so.");
            Assert.AreEqual(5.0, events[2].GetProperty("Km").GetDouble());
            Assert.AreEqual("Impact Malvern", events[2].GetProperty("SiteName").GetString(),
                "A leg names where it arrived.");
        }

        [TestMethod]
        public async Task Wand_ScansOnly_WithTheCarrierNamed()
        {
            var body = await TimelineAsync(wandId: 1);
            var events = body.GetProperty("events").EnumerateArray().ToList();

            Assert.AreEqual(2, events.Count, "Only wand 1's scans; the wandless 07:00 scan is not its history.");
            Assert.IsTrue(events.All(e => e.GetProperty("Type").GetString() == "scan"));
            Assert.AreEqual("P. Kumar", events[0].GetProperty("Who").GetString());
            Assert.AreEqual("Dell 5430", events[0].GetProperty("WandName").GetString());
        }

        [TestMethod]
        public async Task Timeline_NamesExactlyOneEntity()
        {
            Assert.IsInstanceOfType(
                await Controller().Timeline(Midnight, Now, null, null, null, null, default),
                typeof(BadRequestObjectResult), "No entity is the pulse's job, not a timeline.");
            Assert.IsInstanceOfType(
                await Controller().Timeline(Midnight, Now, 7, null, 12, null, default),
                typeof(BadRequestObjectResult), "Two entities is two timelines.");
        }
    }
}
