using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using CityWatch.Tracking.Api;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The FQ TARGET scan summary (#153): for one site and one window, which required
    /// checkpoint tags were scanned. The rules that matter to a control-room manager and
    /// would be wrong if broken: the denominator is the required round (bypassed and deleted
    /// tags are out); a tag counts as scanned no matter WHICH guard's wand touched it
    /// (guard-independent); the latest hit sets the time and the wand credited; a hit with no
    /// wand still counts; and a required tag no one scanned reads clearly as Not Scanned.
    /// </summary>
    [TestClass]
    public class AnalyticsFqTagsTests
    {
        private const int Site = 12;
        private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Midnight = Now.Date;

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.PlatformSmartWands.AddRange(
                new PlatformSmartWand { Id = 1, ClientSiteId = Site, WandName = "Dell 5430" },
                new PlatformSmartWand { Id = 2, ClientSiteId = Site, WandName = "W2" });

            /* Three required tags (A1–A3); A4 is bypassed and A5 deleted — both out of the
               required round. A tag at another site must never leak into this site's summary. */
            _db.PlatformWandTags.AddRange(
                Tag(101, "A1", "Point 1"),
                Tag(102, "A2", "Point 2"),
                Tag(103, "A3", "Point 3"),
                Tag(104, "A4", "Point 4 (spare)", bypass: true),
                Tag(105, "A5", "Point 5 (removed)", deleted: true),
                new PlatformWandTag { Id = 106, ClientSiteId = 99, UId = "Z9", LabelDescription = "Other site" });

            /* A1 scanned by TWO different guards — counts once, latest (07:10, wand 2) wins.
               A2 scanned with no wand selected (SmartWandId 0) — still scanned, no wand name.
               A3 never scanned. A bypassed-tag hit and a yesterday hit must both be ignored. */
            _db.PlatformWandScans.AddRange(
                Scan("A1", guardId: 7, wandId: 1, Midnight.AddHours(6).AddMinutes(10)),
                Scan("A1", guardId: 99, wandId: 2, Midnight.AddHours(7).AddMinutes(10)),
                Scan("A2", guardId: 13, wandId: 0, Midnight.AddHours(6).AddMinutes(30)),
                Scan("A4", guardId: 7, wandId: 1, Midnight.AddHours(6)),
                Scan("A1", guardId: 7, wandId: 1, Midnight.AddDays(-1).AddHours(6)));

            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private static PlatformWandTag Tag(int id, string uid, string label,
            bool bypass = false, bool deleted = false) => new()
        {
            Id = id,
            ClientSiteId = Site,
            UId = uid,
            LabelDescription = label,
            FqBypass = bypass,
            IsDeleted = deleted
        };

        private static PlatformWandScan Scan(string tagUid, int guardId, int wandId, DateTime hitUtc) => new()
        {
            TagUId = tagUid,
            SmartWandId = wandId,
            LoggedInGuardId = guardId,
            LoggedInClientSiteId = Site,
            HitUtcDateTime = hitUtc
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

        private static JsonElement TagNamed(JsonElement body, string name) =>
            body.GetProperty("tags").EnumerateArray()
                .Single(t => t.GetProperty("tagName").GetString() == name);

        [TestMethod]
        public async Task Denominator_IsTheRequiredRound_BypassAndDeletedExcluded()
        {
            var body = Body(await Controller().FqTags(Site, Midnight, Now, default));

            Assert.AreEqual(3, body.GetProperty("required").GetInt32(),
                "Only A1–A3: the bypassed and deleted tags are not part of the round.");
            Assert.AreEqual(2, body.GetProperty("scanned").GetInt32());
            Assert.AreEqual(67, body.GetProperty("completePct").GetInt32(), "2 of 3 = 67%.");

            var names = body.GetProperty("tags").EnumerateArray()
                .Select(t => t.GetProperty("tagName").GetString()).ToList();
            CollectionAssert.AreEquivalent(new[] { "Point 1", "Point 2", "Point 3" }, names);
        }

        [TestMethod]
        public async Task ScannedTag_IsGuardIndependent_AndTakesTheLatestHitsWand()
        {
            var body = Body(await Controller().FqTags(Site, Midnight, Now, default));
            var a1 = TagNamed(body, "Point 1");

            Assert.IsTrue(a1.GetProperty("scanned").GetBoolean(),
                "A1 was scanned — by two different guards; it counts regardless of who.");
            Assert.AreEqual("W2", a1.GetProperty("wandName").GetString(),
                "The latest hit (07:10, wand 2) sets the credited wand, not the earlier one.");
            Assert.AreEqual(Midnight.AddHours(7).AddMinutes(10),
                a1.GetProperty("lastScanUtc").GetDateTime());
        }

        [TestMethod]
        public async Task ScanWithNoWand_StillCounts_ButNamesNoWand()
        {
            var body = Body(await Controller().FqTags(Site, Midnight, Now, default));
            var a2 = TagNamed(body, "Point 2");

            Assert.IsTrue(a2.GetProperty("scanned").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, a2.GetProperty("wandName").ValueKind,
                "SmartWandId 0 is 'no wand selected' — scanned, but nothing to credit.");
        }

        [TestMethod]
        public async Task UnscannedTag_ReadsAsNotScanned_AndLeadsTheList()
        {
            var body = Body(await Controller().FqTags(Site, Midnight, Now, default));

            var first = body.GetProperty("tags").EnumerateArray().First();
            Assert.AreEqual("Point 3", first.GetProperty("tagName").GetString(),
                "The gap comes first so the manager sees it.");
            Assert.IsFalse(first.GetProperty("scanned").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, first.GetProperty("lastScanUtc").ValueKind);
        }

        [TestMethod]
        public async Task Disabled_ReturnsNotFound()
        {
            var result = await Controller(enabled: false).FqTags(Site, Midnight, Now, default);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
