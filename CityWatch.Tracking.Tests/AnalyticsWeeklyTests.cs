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
    /// The A4 weekly patrol-frequency grid — the client-evidence table. The rules that
    /// must never drift: rounds are the BEST guard's count, never a sum that invents a
    /// patrol nobody made (the board's own conservative rule); a day with duty but not
    /// enough rounds is MISSED, declared; a day with nobody rostered is no-duty, not a
    /// failure; and the worst row leads, because that is where the Monday call starts.
    /// </summary>
    [TestClass]
    public class AnalyticsWeeklyTests
    {
        private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Midnight = Now.Date;
        private static readonly DateTime WeekFrom = Midnight.AddDays(-6);

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.PlatformClientSites.AddRange(
                new PlatformClientSite { Id = 12, Name = "Impact Malvern", IsActive = true },
                new PlatformClientSite { Id = 40, Name = "Oxford St", IsActive = true });
            _db.PlatformSiteKpis.Add(new PlatformSiteKpi { Id = 1, ClientSiteId = 12, MinPatrolFreq = 6 });

            /* Site 12, day 0: six traditional-wand rounds — target met. */
            _db.PlatformDailyWandFqs.Add(new PlatformDailyWandFq
            { Id = 1, ClientSiteId = 12, Fq = 6, FqDate = WeekFrom });

            /* Site 12, day 1: guard 7 completed 3 smart rounds, guard 9 completed 2.
               The site got THREE rounds (the best guard's), not five — missed. */
            for (var i = 0; i < 3; i++)
                _db.PlatformWandRounds.Add(new PlatformWandRound
                { ClientSiteId = 12, GuardId = 7, InspectionStartDatetimeLocal = WeekFrom.AddDays(1).AddHours(2 + i) });
            for (var i = 0; i < 2; i++)
                _db.PlatformWandRounds.Add(new PlatformWandRound
                { ClientSiteId = 12, GuardId = 9, InspectionStartDatetimeLocal = WeekFrom.AddDays(1).AddHours(3 + i) });

            /* Site 12, today: a guard is on and scanning, but zero completed rounds. */
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(), UnitId = TrackingUnitKey.FromGuard(7), GuardId = 7,
                ClientSiteId = 12, StartedUtc = Now.AddHours(-2), Status = "Active", IsPatrolCar = false
            });
            _db.PlatformWandScans.AddRange(
                new PlatformWandScan { SmartWandId = 1, LoggedInGuardId = 7, LoggedInClientSiteId = 12, TagLinkedClientSiteId = 12, HitUtcDateTime = Now.AddHours(-1) },
                new PlatformWandScan { SmartWandId = 1, LoggedInGuardId = 7, LoggedInClientSiteId = 12, TagLinkedClientSiteId = 12, HitUtcDateTime = Now.AddMinutes(-30) },
                /* Site 40 (no target), day 1: one scan — activity, not compliance. */
                new PlatformWandScan { SmartWandId = 1, LoggedInGuardId = 9, LoggedInClientSiteId = 40, TagLinkedClientSiteId = 40, HitUtcDateTime = WeekFrom.AddDays(1).AddHours(5) });

            /* The PREVIOUS week: one met day at site 12 — the delta's baseline. */
            _db.PlatformDailyWandFqs.Add(new PlatformDailyWandFq
            { Id = 2, ClientSiteId = 12, Fq = 7, FqDate = WeekFrom.AddDays(-4) });

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

        private async Task<JsonElement> WeeklyAsync()
        {
            var result = await Controller().Weekly(WeekFrom, Now, 0, default);
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "Weekly should return 200.");
            return JsonSerializer.SerializeToElement(ok!.Value);
        }

        [TestMethod]
        public async Task Weekly_MetMissed_AndTheMaxNotSumRoundsRule()
        {
            var body = await WeeklyAsync();
            var site = body.GetProperty("sites").EnumerateArray()
                .Single(s => s.GetProperty("SiteId").GetInt32() == 12);
            var cells = site.GetProperty("Cells").EnumerateArray().ToList();

            Assert.AreEqual(7, cells.Count);
            Assert.AreEqual("met", cells[0].GetProperty("State").GetString(),
                "Six traditional rounds against a target of six.");
            Assert.AreEqual("missed", cells[1].GetProperty("State").GetString(),
                "3 + 2 rounds by two guards is THREE rounds, not five — the board's rule.");
            Assert.AreEqual(3, cells[1].GetProperty("Done").GetInt32());
            Assert.AreEqual("noduty", cells[2].GetProperty("State").GetString(),
                "Nobody rostered is not a failure.");
            Assert.AreEqual("missed", cells[6].GetProperty("State").GetString(),
                "On duty and scanning today, but zero completed rounds — declared.");
            Assert.AreEqual(2, cells[6].GetProperty("Scans").GetInt32());
            Assert.AreEqual(1, site.GetProperty("Met").GetInt32());
            Assert.AreEqual(2, site.GetProperty("Missed").GetInt32());
        }

        [TestMethod]
        public async Task Weekly_WorstFirst_AndNoTargetMeansActivityNotCompliance()
        {
            var body = await WeeklyAsync();
            var sites = body.GetProperty("sites").EnumerateArray().ToList();

            Assert.AreEqual(12, sites[0].GetProperty("SiteId").GetInt32(),
                "The row with the most missed days leads — that is where the Monday call starts.");
            var oxford = sites.Single(s => s.GetProperty("SiteId").GetInt32() == 40);
            Assert.AreEqual(0, oxford.GetProperty("Target").GetInt32());
            Assert.AreEqual("active", oxford.GetProperty("Cells")[1].GetProperty("State").GetString(),
                "No agreed frequency ⇒ activity is reported, never judged.");
            Assert.AreEqual(0, oxford.GetProperty("Missed").GetInt32());
        }

        [TestMethod]
        public async Task Weekly_CarriesTotals_AndThePreviousWeekBaseline()
        {
            var body = await WeeklyAsync();

            Assert.AreEqual(1, body.GetProperty("totals").GetProperty("met").GetInt32());
            Assert.AreEqual(2, body.GetProperty("totals").GetProperty("missed").GetInt32());
            Assert.AreEqual(1, body.GetProperty("prevTotals").GetProperty("met").GetInt32(),
                "Last week's met day is the delta's baseline.");
        }

        [TestMethod]
        public async Task Weekly_RejectsAnImpossibleTimezone()
        {
            Assert.IsInstanceOfType(await Controller().Weekly(WeekFrom, Now, 2000, default),
                typeof(BadRequestObjectResult));
        }
    }
}
