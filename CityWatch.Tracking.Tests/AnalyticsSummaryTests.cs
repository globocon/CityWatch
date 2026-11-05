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
    /// The Insights drawer's KPI endpoint (analytics A1). The isolation contract lives in
    /// the module's structure, but the numbers live here: distinct counting (two shifts by
    /// one guard is one guard), window clipping (an overnight shift contributes only the
    /// hours inside the window), the visits∪scans definition of an "active site", and the
    /// whole-days compare shift that keeps 09:00 comparing with 09:00.
    /// </summary>
    [TestClass]
    public class AnalyticsSummaryTests
    {
        /* "Today" 10:00 — so the today-window is a partial day, the case that makes the
           same-hours compare rule matter. */
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

            /* Guard 7 works twice today: an overnight shift that ran into the window
               (23:00 → 01:00, clips to 60 min) and a live one (05:00 → now, 300 min).
               One guard, two sessions — must count once. */
            _db.TrackingSessions.Add(Session(TrackingUnitKey.FromGuard(7), 7,
                Midnight.AddHours(-1), Midnight.AddHours(1), isPatrolCar: false));
            _db.TrackingSessions.Add(Session(TrackingUnitKey.FromGuard(7), 7,
                Midnight.AddHours(5), null, isPatrolCar: false));

            /* A car with the legacy null flag: position-keyed unit id decides. 120 min. */
            _db.TrackingSessions.Add(Session(TrackingUnitKey.FromPosition(10), 9,
                Midnight.AddHours(2), Midnight.AddHours(4), isPatrolCar: null));

            /* Yesterday same hours: one foot-guard shift, 60 min. */
            _db.TrackingSessions.Add(Session(TrackingUnitKey.FromGuard(9), 9,
                Midnight.AddDays(-1).AddHours(8), Midnight.AddDays(-1).AddHours(9), isPatrolCar: false));

            /* Visits today: two confirmed, one candidate that never confirmed (not real). */
            _db.TrackingSiteVisits.Add(Visit(12, Midnight.AddHours(1)));
            _db.TrackingSiteVisits.Add(Visit(30, Midnight.AddHours(3)));
            _db.TrackingSiteVisits.Add(new TrackingSiteVisit
            {
                UnitId = TrackingUnitKey.FromGuard(7),
                SessionId = Guid.NewGuid(),
                SiteId = 99,
                SiteName = "Drive-past",
                EnteredUtc = Midnight.AddHours(2),
                ConfirmedUtc = null
            });

            /* Scans today: tag sites 12 and 40, plus an unlinked tag that falls back to the
               login site 50. Yesterday: one scan at site 12. */
            _db.PlatformWandScans.Add(Scan(12, 50, Midnight.AddHours(1)));
            _db.PlatformWandScans.Add(Scan(40, 50, Midnight.AddHours(6)));
            _db.PlatformWandScans.Add(Scan(null, 50, Midnight.AddHours(7)));
            _db.PlatformWandScans.Add(Scan(12, 50, Midnight.AddDays(-1).AddHours(2)));

            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private static TrackingSession Session(int unitId, int guardId,
            DateTime startedUtc, DateTime? endedUtc, bool? isPatrolCar) => new()
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            GuardId = guardId,
            ClientSiteId = 50,
            StartedUtc = startedUtc,
            EndedUtc = endedUtc,
            Status = endedUtc == null ? "Active" : "Completed",
            IsPatrolCar = isPatrolCar,
            LastFixUtc = endedUtc ?? Now
        };

        private static TrackingSiteVisit Visit(int siteId, DateTime confirmedUtc) => new()
        {
            UnitId = TrackingUnitKey.FromGuard(7),
            SessionId = Guid.NewGuid(),
            SiteId = siteId,
            SiteName = "Site " + siteId,
            EnteredUtc = confirmedUtc.AddMinutes(-2),
            ConfirmedUtc = confirmedUtc
        };

        private static PlatformWandScan Scan(int? tagSiteId, int loginSiteId, DateTime hitUtc) => new()
        {
            SmartWandId = 1,
            LoggedInGuardId = 7,
            LoggedInClientSiteId = loginSiteId,
            TagLinkedClientSiteId = tagSiteId,
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

        private async Task<JsonElement> SummaryAsync(DateTime fromUtc, DateTime toUtc)
        {
            var result = await Controller().Summary(fromUtc, toUtc, default);
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "Summary should return 200.");
            return JsonSerializer.SerializeToElement(ok!.Value);
        }

        [TestMethod]
        public async Task Summary_CountsToday_DistinctAndClippedToTheWindow()
        {
            var body = await SummaryAsync(Midnight, Now);
            var c = body.GetProperty("current");

            Assert.AreEqual(1, c.GetProperty("GuardsActive").GetInt32(),
                "Two sessions by one guard are one guard.");
            Assert.AreEqual(1, c.GetProperty("PcarsActive").GetInt32(),
                "The null-flag car counts as a car via its position-keyed unit id.");
            Assert.AreEqual(2, c.GetProperty("SiteVisits").GetInt32(),
                "Only confirmed visits are real (the drive-past never confirmed).");
            Assert.AreEqual(3, c.GetProperty("CheckIns").GetInt32());
            Assert.AreEqual(4, c.GetProperty("SitesActive").GetInt32(),
                "Visited {12,30} ∪ scanned {12,40,50} = 4 distinct sites.");
            Assert.AreEqual(60 + 300 + 120, c.GetProperty("ActiveMinutes").GetInt32(),
                "The overnight shift contributes only its 60 in-window minutes.");
        }

        [TestMethod]
        public async Task Summary_ComparesWithTheSameHoursThePreviousDay()
        {
            var body = await SummaryAsync(Midnight, Now);

            Assert.AreEqual(1, body.GetProperty("compareShiftDays").GetInt32(),
                "A partial day shifts back exactly one day — 09:00 compares with 09:00.");
            var p = body.GetProperty("previous");
            Assert.AreEqual(1, p.GetProperty("GuardsActive").GetInt32());
            Assert.AreEqual(0, p.GetProperty("PcarsActive").GetInt32());
            Assert.AreEqual(1, p.GetProperty("CheckIns").GetInt32());
            Assert.AreEqual(1, p.GetProperty("SitesActive").GetInt32());
            Assert.AreEqual(0, p.GetProperty("SiteVisits").GetInt32());
            Assert.AreEqual(60, p.GetProperty("ActiveMinutes").GetInt32());
        }

        [TestMethod]
        public async Task Summary_SevenDayWindow_ShiftsSevenDays()
        {
            var body = await SummaryAsync(Now.AddDays(-7), Now);
            Assert.AreEqual(7, body.GetProperty("compareShiftDays").GetInt32());
        }

        [TestMethod]
        public async Task Summary_RejectsInvertedAndOversizedWindows()
        {
            Assert.IsInstanceOfType(await Controller().Summary(Now, Now.AddHours(-1), default),
                typeof(BadRequestResult));
            Assert.IsInstanceOfType(await Controller().Summary(Now.AddDays(-16), Now, default),
                typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Summary_Disabled_IsIndistinguishableFromAbsent()
        {
            Assert.IsInstanceOfType(await Controller(enabled: false).Summary(Midnight, Now, default),
                typeof(NotFoundResult));
            Assert.AreEqual(0, _db.TrackingAccessAudits.Count(),
                "A disabled module must not even leave audit traces.");
        }

        [TestMethod]
        public async Task Summary_WritesOneFleetWideAuditRow_DedupedAcrossTheRefreshLoop()
        {
            await SummaryAsync(Midnight, Now);
            /* The drawer refreshes every minute; the audit trail records the operator's
               look, not every tick — repeat reads inside ten minutes add no row. */
            await SummaryAsync(Midnight, Now);

            var audit = _db.TrackingAccessAudits.Single();
            Assert.AreEqual("ViewAnalytics", audit.Action);
            Assert.IsNull(audit.UnitId);
            Assert.AreEqual(77, audit.UserId);
        }
    }
}
