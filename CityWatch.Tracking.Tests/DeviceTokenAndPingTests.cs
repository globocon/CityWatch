using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services;
using CityWatch.Tracking.Services.Push;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// FCM push nudge (§Push): token registration under the ingest trust model, and the
    /// ping flow — FCM is the accelerator, the ingest response is the guarantee, so nothing
    /// here ever equates "sent" with "position received".
    /// </summary>
    [TestClass]
    public class DeviceTokenAndPingTests
    {
        private static readonly DateTime Now = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
        private const int Unit = 2000016;
        private const int Operator = 5;
        private static readonly Guid Session = Guid.NewGuid();

        private TrackingDbContext _db = null!;
        private TrackingOptions _options = null!;
        private FakeSender _sender = null!;
        private DateTime _clock;

        private sealed class FakeSender : ITrackingNudgeSender
        {
            public bool IsConfigured { get; set; } = true;
            public Queue<NudgeSendStatus> Results { get; } = new();
            public List<(string Token, int UnitId, string Reason, string RequestId)> Sent { get; } = new();

            public Task<NudgeSendStatus> SendNudgeAsync(string fcmToken, int unitId, string reason,
                string requestId, CancellationToken ct)
            {
                Sent.Add((fcmToken, unitId, reason, requestId));
                return Task.FromResult(Results.Count > 0 ? Results.Dequeue() : NudgeSendStatus.Sent);
            }
        }

        [TestInitialize]
        public async Task Setup()
        {
            /* NoTracking mirrors production DI (see ModeCommandServiceTests): these tests
               must fail if a service mutates a queried entity without AsTracking. */
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Session, UnitId = Unit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-1), Status = "Active"
            });
            await _db.SaveChangesAsync();

            _options = new TrackingOptions();
            _sender = new FakeSender();
            _clock = Now;
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private DeviceTokenService Tokens()
            => new(_db, NullLogger<DeviceTokenService>.Instance, () => _clock);

        private TrackingPingService Ping()
            => new(_db, Tokens(), _sender, _options, NullLogger<TrackingPingService>.Instance, () => _clock);

        /* ------------------------- token registration ------------------------- */

        [TestMethod]
        public async Task Register_WithActiveSession_StoresToken()
        {
            var ok = await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);

            Assert.IsTrue(ok);
            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.AreEqual(Unit, stored.UnitId);
            Assert.AreEqual("tok-1", stored.FcmToken);
            Assert.IsTrue(stored.IsActive);
        }

        [TestMethod]
        public async Task Register_WithoutActiveSession_IsRefused()
        {
            var ok = await Tokens().RegisterAsync(Unit, Guid.NewGuid(), "tok-1", "android", CancellationToken.None);

            Assert.IsFalse(ok, "A device may only register a token for the unit it is signed into.");
            Assert.AreEqual(0, await _db.TrackingDeviceTokens.CountAsync());
        }

        [TestMethod]
        public async Task Register_ForSomeoneElsesSession_IsRefused()
        {
            /* The session id exists but belongs to ANOTHER unit: the (unit, session) pair
               is the credential, exactly like ingest Gate 2. */
            var other = Guid.NewGuid();
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = other, UnitId = 999, GuardId = 8, ClientSiteId = 12,
                StartedUtc = Now, Status = "Active"
            });
            await _db.SaveChangesAsync();

            var ok = await Tokens().RegisterAsync(Unit, other, "tok-1", "android", CancellationToken.None);

            Assert.IsFalse(ok);
        }

        [TestMethod]
        public async Task Register_SameTokenNewUnit_RehomesInsteadOfDuplicating()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);

            var session2 = Guid.NewGuid();
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = session2, UnitId = 2000010, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now, Status = "Active"
            });
            await _db.SaveChangesAsync();
            var ok = await Tokens().RegisterAsync(2000010, session2, "tok-1", "android", CancellationToken.None);

            Assert.IsTrue(ok);
            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.AreEqual(2000010, stored.UnitId, "The phone changed cars; its token must follow.");
            Assert.IsTrue(stored.IsActive);
        }

        [TestMethod]
        public async Task Release_DeactivatesToken_AndIsIdempotent()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);

            await Tokens().ReleaseAsync("tok-1", CancellationToken.None);
            await Tokens().ReleaseAsync("tok-1", CancellationToken.None);   // second call: no-op

            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.IsFalse(stored.IsActive);
            Assert.IsNotNull(stored.InvalidatedUtc);
        }

        [TestMethod]
        public async Task Register_AfterRelease_Reactivates()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);
            await Tokens().ReleaseAsync("tok-1", CancellationToken.None);

            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);

            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.IsTrue(stored.IsActive, "Login after logout re-arms the same token.");
            Assert.IsNull(stored.InvalidatedUtc);
        }

        /* ------------------------------- ping ------------------------------- */

        [TestMethod]
        public async Task Ping_SendsToEveryActiveToken_AndAudits()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);

            var result = await Ping().PingAsync(Unit, Operator, "10.0.0.1", "ManualPing", CancellationToken.None);

            Assert.AreEqual(PingStatus.Sent, result.Status);
            Assert.AreEqual(1, result.TokensPinged);
            Assert.IsNotNull(result.RequestId);
            Assert.AreEqual(result.RequestId, _sender.Sent.Single().RequestId,
                "The correlation id travels with the message.");
            var audit = await _db.TrackingAccessAudits.SingleAsync();
            Assert.AreEqual("CommandPing", audit.Action);
            Assert.AreEqual(Operator, audit.UserId);
            Assert.AreEqual(Unit, audit.UnitId);
        }

        [TestMethod]
        public async Task Ping_WithoutTokens_SaysSoInsteadOfPretending()
        {
            var result = await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);

            Assert.AreEqual(PingStatus.NoTokens, result.Status);
            Assert.AreEqual(0, _sender.Sent.Count);
        }

        [TestMethod]
        public async Task Ping_WhenPushNotConfigured_RefusesLoudly()
        {
            _sender.IsConfigured = false;
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);

            var result = await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);

            Assert.AreEqual(PingStatus.NotConfigured, result.Status);
            Assert.AreEqual(0, _sender.Sent.Count);
        }

        [TestMethod]
        public async Task Ping_InsideCooldown_IsRefused_ThenAllowedAfter()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);
            await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);

            var second = await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);
            Assert.AreEqual(PingStatus.Cooldown, second.Status,
                "Repeated pings are refused — Android's high-priority quota is finite.");

            _clock = Now.AddSeconds(_options.Fcm.PingCooldownSeconds + 1);
            var third = await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);
            Assert.AreEqual(PingStatus.Sent, third.Status);
        }

        [TestMethod]
        public async Task Ping_InvalidToken_IsDeactivated_NotRetriedForever()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-dead", "android", CancellationToken.None);
            _sender.Results.Enqueue(NudgeSendStatus.InvalidToken);

            var result = await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);

            Assert.AreEqual(PingStatus.Sent, result.Status);
            Assert.AreEqual(0, result.TokensPinged, "An invalid token is not a successful send.");
            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.IsFalse(stored.IsActive, "FCM said Unregistered: the token must be retired.");
        }

        [TestMethod]
        public async Task Ping_TransientFailure_KeepsTokenActive()
        {
            await Tokens().RegisterAsync(Unit, Session, "tok-1", "android", CancellationToken.None);
            _sender.Results.Enqueue(NudgeSendStatus.Failed);

            var result = await Ping().PingAsync(Unit, Operator, null, "ManualPing", CancellationToken.None);

            Assert.AreEqual(PingStatus.Sent, result.Status);
            Assert.AreEqual(0, result.TokensPinged);
            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.IsTrue(stored.IsActive, "A transient failure must not burn the token.");
        }
    }
}
