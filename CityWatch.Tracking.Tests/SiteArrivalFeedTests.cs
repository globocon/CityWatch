using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services.Geofencing;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The bell's contract: only CONFIRMED arrivals are served, labelled the way the
    /// operator talks (callsign first), newest first, windowed.
    /// </summary>
    [TestClass]
    public class SiteArrivalFeedTests
    {
        private static readonly DateTime Now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid CarSession = Guid.NewGuid();

        private TrackingDbContext _db = null!;
        private SiteArrivalFeed _feed = null!;

        [TestInitialize]
        public async Task Setup()
        {
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);

            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = CarSession, UnitId = 2000010, GuardId = 7, ClientSiteId = 625,
                StartedUtc = Now.AddHours(-6), Status = "Active", IsPatrolCar = true,
                Callsign = "Romeo 03", PatrolCarPositionName = "Mobile Patrols (Car) M1"
            });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 7, Name = "Bruno Timpano" });
            await _db.SaveChangesAsync();

            _feed = new SiteArrivalFeed(_db, new TrackingOptions(), () => Now);
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private async Task AddVisit(int id, DateTime entered, DateTime? confirmed, DateTime? exited, string site = "Martha Cove Marina")
        {
            _db.TrackingSiteVisits.Add(new TrackingSiteVisit
            {
                Id = id, UnitId = 2000010, SessionId = CarSession, SiteId = 390,
                SiteName = site, EnteredUtc = entered, ConfirmedUtc = confirmed, ExitedUtc = exited
            });
            await _db.SaveChangesAsync();
        }

        [TestMethod]
        public async Task Only_confirmed_visits_are_served()
        {
            await AddVisit(1, Now.AddMinutes(-30), confirmed: Now.AddMinutes(-28), exited: null);
            await AddVisit(2, Now.AddMinutes(-20), confirmed: null, exited: Now.AddMinutes(-19));   // drive-past
            var list = await _feed.GetRecentAsync(null, CancellationToken.None);
            Assert.AreEqual(1, list.Count, "a drive-past must never be served to the bell");
            Assert.AreEqual(1, list[0].Id);
        }

        [TestMethod]
        public async Task Labelled_with_callsign_and_guard_and_stay_length()
        {
            await AddVisit(1, Now.AddMinutes(-45), confirmed: Now.AddMinutes(-43), exited: null);
            var a = (await _feed.GetRecentAsync(null, CancellationToken.None)).Single();
            Assert.AreEqual("Romeo 03", a.Label, "callsign is what operators say on the radio");
            Assert.AreEqual("Bruno Timpano", a.GuardName);
            Assert.AreEqual("car", a.Kind);
            Assert.IsTrue(a.StillOnSite);
            Assert.AreEqual(45, a.MinutesOnSite, "an open stay is measured to now");
        }

        [TestMethod]
        public async Task Window_excludes_old_arrivals_and_newest_come_first()
        {
            await AddVisit(1, Now.AddHours(-20), confirmed: Now.AddHours(-20), exited: Now.AddHours(-19));
            await AddVisit(2, Now.AddHours(-3), confirmed: Now.AddHours(-3), exited: Now.AddHours(-2));
            await AddVisit(3, Now.AddMinutes(-10), confirmed: Now.AddMinutes(-8), exited: null);
            var list = await _feed.GetRecentAsync(null, CancellationToken.None);   // default 12 h
            Assert.AreEqual(2, list.Count, "outside the window is off the feed (still in the table)");
            Assert.AreEqual(3, list[0].Id, "newest first");
            Assert.AreEqual(2, list[1].Id);
        }

        [TestMethod]
        public async Task Ended_stays_report_their_length()
        {
            await AddVisit(1, Now.AddHours(-2), confirmed: Now.AddHours(-2), exited: Now.AddMinutes(-75));
            var a = (await _feed.GetRecentAsync(null, CancellationToken.None)).Single();
            Assert.IsFalse(a.StillOnSite);
            Assert.AreEqual(Now.AddMinutes(-75), a.ExitedUtc, "the client renders the 'left' line from this");
            Assert.AreEqual(45, a.MinutesOnSite, "a closed stay is measured to its exit");
        }

        [TestMethod]
        public async Task A_fresh_exit_keeps_an_old_visit_in_the_window()
        {
            /* Arrived 13 h ago (outside the 12 h window) but drove off 1 h ago: the "left"
               line must still reach the bell — the departure is the news. */
            await AddVisit(1, Now.AddHours(-13), confirmed: Now.AddHours(-13), exited: Now.AddHours(-1));
            var list = await _feed.GetRecentAsync(null, CancellationToken.None);
            Assert.AreEqual(1, list.Count, "a visit is in the window while EITHER event is");
        }
    }
}
