using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using CityWatch.Tracking.Api;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// The Cochin↔Poonjar regression suite. A unit id is a CAR, and a car changes hands:
    /// two officers' sessions on one unit must never render as one journey — not in history,
    /// not in replay, and the superseded phone must be told it is no longer being tracked.
    /// </summary>
    [TestClass]
    public class SessionIsolationTests
    {
        private static readonly DateTime Now = new(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc);
        private const int Unit = 2000010;           // position-keyed patrol car
        private const int OtherUnit = 2000014;
        private static readonly Guid SessionA = Guid.NewGuid();   // officer 1, Cochin, superseded
        private static readonly Guid SessionB = Guid.NewGuid();   // officer 2, Poonjar, active

        /* Two cities ~60 km apart: any polyline containing both is the defect. */
        private const decimal CochinLat = 9.9312m, CochinLon = 76.2673m;
        private const decimal PoonjarLat = 9.6710m, PoonjarLon = 76.8110m;

        private TrackingDbContext _db = null!;

        [TestInitialize]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new TrackingDbContext(options);

            _db.TrackingUnitEnrolments.Add(new TrackingUnitEnrolment
            {
                UnitId = Unit,
                IsEnabled = true,
                EnrolledUtc = Now.AddDays(-30),
                EnrolledByUserId = 1,
                ConsentRecordedUtc = Now.AddDays(-30)
            });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 7, Name = "J. Smith" });
            _db.PlatformGuards.Add(new PlatformGuard { Id = 9, Name = "A. Thomas" });

            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = SessionA,
                UnitId = Unit,
                GuardId = 7,
                ClientSiteId = 12,
                StartedUtc = Now.AddHours(-4),
                EndedUtc = Now.AddHours(-2),
                Status = "Completed",
                EndReason = "SupersededByNewSession"
            });
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = SessionB,
                UnitId = Unit,
                GuardId = 9,
                ClientSiteId = 12,
                StartedUtc = Now.AddHours(-2),
                Status = "Active"
            });

            /* Session A drove around Cochin; session B around Poonjar. */
            for (var i = 0; i < 5; i++)
            {
                _db.TrackPoints.Add(Point(SessionA, i, CochinLat + i * 0.001m, CochinLon, Now.AddHours(-4).AddMinutes(i * 10)));
                _db.TrackPoints.Add(Point(SessionB, i, PoonjarLat + i * 0.001m, PoonjarLon, Now.AddHours(-2).AddMinutes(i * 10)));
            }
            await _db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private static TrackPoint Point(Guid session, int seq, decimal lat, decimal lon, DateTime utc) => new()
        {
            UnitId = Unit,
            SessionId = session,
            Seq = seq,
            RecordedUtc = utc,
            ReceivedUtc = utc,
            Latitude = lat,
            Longitude = lon,
            SourceType = 0,
            ModeAtCapture = 1,
            Flags = 0
        };

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

        private async Task<JsonElement> HistoryAsync(DateTime fromUtc, DateTime toUtc)
        {
            var result = await Controller().History(Unit, fromUtc, toUtc, default);
            var ok = result as OkObjectResult;
            Assert.IsNotNull(ok, "History should return 200.");
            return JsonSerializer.SerializeToElement(ok!.Value);
        }

        /* ---------------- history: the session boundary is the truth boundary ---------------- */

        [TestMethod]
        public async Task History_TwoSessionsInWindow_AreSeparateGroups_NeverOneStream()
        {
            var body = await HistoryAsync(Now.AddHours(-5), Now);

            var sessions = body.GetProperty("sessions");
            Assert.AreEqual(2, sessions.GetArrayLength(),
                "Two sessions in the window must come back as two groups.");

            /* Ordered by start: officer 1 (Cochin) first, officer 2 (Poonjar) second. */
            var first = sessions[0];
            var second = sessions[1];
            Assert.AreEqual(SessionA.ToString(), first.GetProperty("sessionId").GetString());
            Assert.AreEqual("J. Smith", first.GetProperty("guardName").GetString());
            Assert.AreEqual(SessionB.ToString(), second.GetProperty("sessionId").GetString());
            Assert.AreEqual("A. Thomas", second.GetProperty("guardName").GetString());

            /* No group may contain both cities — that line is the invented journey. */
            foreach (var session in sessions.EnumerateArray())
            {
                var lats = session.GetProperty("points").EnumerateArray()
                    .Select(p => p.GetProperty("lat").GetDecimal()).ToList();
                Assert.IsTrue(lats.Count > 0, "Every returned session carries its points.");
                var nearCochin = lats.Count(l => Math.Abs(l - CochinLat) < 0.1m);
                Assert.IsTrue(nearCochin == 0 || nearCochin == lats.Count,
                    "A single session's trail must never span both officers' journeys.");
            }
        }

        [TestMethod]
        public async Task History_HasNoFlatPointsProperty_ClientsCannotDrawOneLine()
        {
            var body = await HistoryAsync(Now.AddHours(-5), Now);
            Assert.IsFalse(body.TryGetProperty("points", out _),
                "The flat point stream is the defect's API shape; it must be gone.");
        }

        [TestMethod]
        public async Task History_WindowCoveringOneSession_ReturnsOnlyThatSession()
        {
            var body = await HistoryAsync(Now.AddHours(-2), Now);

            var sessions = body.GetProperty("sessions");
            Assert.AreEqual(1, sessions.GetArrayLength());
            Assert.AreEqual(SessionB.ToString(), sessions[0].GetProperty("sessionId").GetString());
            Assert.AreEqual(5, sessions[0].GetProperty("points").GetArrayLength());
        }

        [TestMethod]
        public async Task History_AnotherUnitsPoints_NeverAppear()
        {
            _db.TrackPoints.Add(new TrackPoint
            {
                UnitId = OtherUnit,
                SessionId = Guid.NewGuid(),
                Seq = 1,
                RecordedUtc = Now.AddHours(-1),
                ReceivedUtc = Now.AddHours(-1),
                Latitude = CochinLat,
                Longitude = CochinLon,
                SourceType = 0,
                ModeAtCapture = 1,
                Flags = 0
            });
            await _db.SaveChangesAsync();

            var body = await HistoryAsync(Now.AddHours(-5), Now);
            var total = body.GetProperty("sessions").EnumerateArray()
                .Sum(s => s.GetProperty("points").GetArrayLength());
            Assert.AreEqual(10, total, "History for one unit must contain that unit's points only.");
        }

        /* ---------------- ingest: the superseded phone must be told ---------------- */

        private IngestService Ingest() => new(_db, new InMemoryLiveStateStore(),
            Channel.CreateBounded<TrackPoint>(1000).Writer,
            new UnitRateLimiter(new TrackingOptions()),
            /* Kerala test coordinates: the service-area envelope is off, as on the test hosts. */
            new TrackingOptions { EnforceServiceArea = false },
            NullLogger<IngestService>.Instance, commands: null, utcNow: () => Now);

        private static PositionBatch Batch(Guid session) => new()
        {
            UnitId = Unit,
            SessionId = session,
            DeviceUtc = Now,
            Points = { new PositionPoint { Seq = 100, Utc = Now.AddSeconds(-5), Lat = CochinLat, Lon = CochinLon, AccuracyM = 8 } }
        };

        [TestMethod]
        public async Task Ingest_SupersededSession_RejectsAndSaysSo()
        {
            var response = await Ingest().IngestAsync(Batch(SessionA), default);

            Assert.AreEqual(0, response.Accepted);
            Assert.AreEqual(1, response.Rejected);
            Assert.IsTrue(response.SessionSuperseded,
                "The superseded device must learn its officer is no longer being tracked.");
        }

        [TestMethod]
        public async Task Ingest_ActiveSession_NoSupersededFlag()
        {
            var response = await Ingest().IngestAsync(Batch(SessionB), default);

            Assert.AreEqual(1, response.Accepted);
            Assert.IsFalse(response.SessionSuperseded);
        }

        [TestMethod]
        public async Task Ingest_UnknownSession_RejectsWithoutSupersededFlag()
        {
            var response = await Ingest().IngestAsync(Batch(Guid.NewGuid()), default);

            Assert.AreEqual(1, response.Rejected);
            Assert.IsFalse(response.SessionSuperseded,
                "An unknown session is not a takeover; the device just re-authenticates.");
        }

        [TestMethod]
        public async Task Ingest_NormallyEndedSession_RejectsWithoutSupersededFlag()
        {
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(),
                UnitId = Unit,
                GuardId = 7,
                ClientSiteId = 12,
                StartedUtc = Now.AddHours(-8),
                EndedUtc = Now.AddHours(-6),
                Status = "Completed",
                EndReason = "DeviceRequested"
            });
            await _db.SaveChangesAsync();
            var ended = _db.TrackingSessions.Local.First(s => s.EndReason == "DeviceRequested");

            var response = await Ingest().IngestAsync(Batch(ended.Id), default);

            Assert.AreEqual(1, response.Rejected);
            Assert.IsFalse(response.SessionSuperseded);
        }
    }
}
