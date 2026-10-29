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
    /// The replay directory (#153 Part 11): replay must be anchorable to a SITE, a guard,
    /// a car, or everything — "Peter maybe was not on yesterday" must never mean an empty
    /// replay when someone else worked the site. The directory lists every session that
    /// can actually answer a window, with the same identity labels the live map uses.
    /// </summary>
    [TestClass]
    public class ReplayDirectoryTests
    {
        private static readonly DateTime Now = new(2026, 8, 25, 10, 0, 0, DateTimeKind.Utc);

        private const int GuardSiteId = 12;
        private const int FleetSiteId = 30;
        private static readonly int FootUnit = TrackingUnitKey.FromGuard(7);
        private static readonly int LegacyCarUnit = TrackingUnitKey.FromPosition(10);
        private static readonly int NamedCarUnit = TrackingUnitKey.FromPosition(11);

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.PlatformGuards.Add(new PlatformGuard { Id = 7, Name = "J. Smith" });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 9, Name = "A. Thomas" });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 11, Name = "P. Kumar" });
            _db.PlatformClientSites.Add(new PlatformClientSite { Id = GuardSiteId, Name = "Impact Malvern", IsActive = true });
            _db.PlatformClientSites.Add(new PlatformClientSite { Id = FleetSiteId, Name = "Citywatch M1 - Romeo Patrol Cars", IsActive = true });

            /* A foot guard who worked the client site inside the window. */
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = FootUnit,
                GuardId = 7,
                ClientSiteId = GuardSiteId,
                StartedUtc = Now.AddHours(-6),
                EndedUtc = Now.AddHours(-2),
                Status = "Completed",
                LastFixUtc = Now.AddHours(-2)
            });
            /* A car that declared no position at login: position-keyed unit, flag never set. */
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = LegacyCarUnit,
                GuardId = 9,
                ClientSiteId = FleetSiteId,
                StartedUtc = Now.AddHours(-3),
                Status = "Active",
                LastFixUtc = Now.AddMinutes(-5)
            });
            /* A fully-declared car: callsign and position both captured at login. */
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = NamedCarUnit,
                GuardId = 11,
                ClientSiteId = FleetSiteId,
                StartedUtc = Now.AddHours(-4),
                Status = "Active",
                IsPatrolCar = true,
                Callsign = "R3",
                PatrolCarPositionName = "Mobile Patrols (Car) M1",
                LastFixUtc = Now.AddMinutes(-1)
            });
            /* Still open, but its last fix predates the window: it has nothing to replay. */
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = FootUnit,
                GuardId = 7,
                ClientSiteId = GuardSiteId,
                StartedUtc = Now.AddHours(-40),
                Status = "Active",
                LastFixUtc = Now.AddHours(-30)
            });
            /* Ended before the window opened: not part of this day at all. */
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = FootUnit,
                GuardId = 7,
                ClientSiteId = GuardSiteId,
                StartedUtc = Now.AddHours(-40),
                EndedUtc = Now.AddHours(-30),
                Status = "Completed",
                LastFixUtc = Now.AddHours(-30)
            });
            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private TrackingController Controller() => new(null!, null!, null!, null!, null!, null!, _db, new TrackingOptions())
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

        private async Task<JsonElement> DirectoryAsync(DateTime fromUtc, DateTime toUtc)
        {
            var result = await Controller().ReplaySessions(fromUtc, toUtc, default);
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "The replay directory should return 200.");
            return JsonSerializer.SerializeToElement(ok!.Value);
        }

        [TestMethod]
        public async Task Directory_ListsSessionsOverlappingTheWindow_WithIdentityLabels()
        {
            var body = await DirectoryAsync(Now.AddHours(-24), Now);

            var rows = body.GetProperty("sessions").EnumerateArray().ToList();
            Assert.AreEqual(3, rows.Count, "Exactly the sessions that can answer this window.");

            var foot = rows.Single(r => r.GetProperty("unitId").GetInt32() == FootUnit);
            Assert.AreEqual("J. Smith", foot.GetProperty("guardName").GetString());
            Assert.IsFalse(foot.GetProperty("isPatrolCar").GetBoolean());
            Assert.AreEqual(GuardSiteId, foot.GetProperty("clientSiteId").GetInt32());
            Assert.AreEqual("Impact Malvern", foot.GetProperty("siteName").GetString());

            var named = rows.Single(r => r.GetProperty("unitId").GetInt32() == NamedCarUnit);
            Assert.AreEqual("R3", named.GetProperty("callsign").GetString());
            Assert.AreEqual("Mobile Patrols (Car) M1", named.GetProperty("patrolCar").GetString());
            Assert.IsTrue(named.GetProperty("isPatrolCar").GetBoolean());
        }

        [TestMethod]
        public async Task Directory_CarWithoutDeclaredPosition_AnswersToItsLoginSiteName()
        {
            var body = await DirectoryAsync(Now.AddHours(-24), Now);

            var legacy = body.GetProperty("sessions").EnumerateArray()
                .Single(r => r.GetProperty("unitId").GetInt32() == LegacyCarUnit);
            Assert.IsTrue(legacy.GetProperty("isPatrolCar").GetBoolean(),
                "A position-keyed unit is a car even when the login flag was never captured.");
            Assert.AreEqual("Citywatch M1 - Romeo Patrol Cars", legacy.GetProperty("patrolCar").GetString(),
                "Same naming rule as History: no declared position ⇒ the login site names the car.");
        }

        [TestMethod]
        public async Task Directory_SessionWhoseLastFixPredatesTheWindow_IsNotOffered()
        {
            var body = await DirectoryAsync(Now.AddHours(-24), Now);

            var staleCount = body.GetProperty("sessions").EnumerateArray()
                .Count(r => r.GetProperty("unitId").GetInt32() == FootUnit);
            Assert.AreEqual(1, staleCount,
                "An open session with no fix inside the window can only answer 'no trail' — never offered.");
        }

        [TestMethod]
        public async Task Directory_RejectsInvertedAndOversizedWindows()
        {
            Assert.IsInstanceOfType(await Controller().ReplaySessions(Now, Now.AddHours(-1), default),
                typeof(BadRequestResult));
            Assert.IsInstanceOfType(await Controller().ReplaySessions(Now.AddHours(-27), Now, default),
                typeof(BadRequestObjectResult), "The 26 h shift-with-margin cap applies to the directory too.");
        }

        [TestMethod]
        public async Task Directory_WritesOneFleetWideAuditRow()
        {
            await DirectoryAsync(Now.AddHours(-24), Now);

            var audit = _db.TrackingAccessAudits.Single();
            Assert.AreEqual("ViewHistoryIndex", audit.Action);
            Assert.IsNull(audit.UnitId, "A fleet-wide view audits with no unit, like Live does.");
            Assert.AreEqual(77, audit.UserId);
        }
    }
}
