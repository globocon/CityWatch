using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Api;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services;
using CityWatch.Tracking.Services.Push;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CityWatch.Tracking.Tests
{
    /// <summary>
    /// Custom operator messages (§Push, the ✉ button): text to one unit's phone, or to every
    /// online unit of a kind. Same discipline as the ping suite — "Sent" means FCM accepted
    /// the message, cooldowns live on the audit trail, and the message text itself is the
    /// audit row's Justification. Message and ping cooldowns must never interfere.
    /// </summary>
    [TestClass]
    public class TrackingMessageTests
    {
        private static readonly DateTime Now = new(2026, 8, 13, 9, 0, 0, DateTimeKind.Utc);
        private const int CarUnit = 2000016;         // position-keyed patrol car
        private const int GuardUnit = 1000007;       // guard-keyed foot officer
        private const int Operator = 5;
        private const string Hello = "Return to base";

        private TrackingDbContext _db = null!;
        private TrackingOptions _options = null!;
        private FakeSender _sender = null!;
        private FakeSnapshot _snapshot = null!;
        private DateTime _clock;

        private sealed class FakeSender : ITrackingNudgeSender
        {
            public bool IsConfigured { get; set; } = true;
            public Queue<NudgeSendStatus> Results { get; } = new();
            public List<(string Token, int UnitId, string Reason, string RequestId)> Sent { get; } = new();
            public List<(string Token, int UnitId, string Title, string Body, string RequestId)> Messages { get; } = new();

            public Task<NudgeSendStatus> SendNudgeAsync(string fcmToken, int unitId, string reason,
                string requestId, CancellationToken ct)
            {
                Sent.Add((fcmToken, unitId, reason, requestId));
                return Task.FromResult(Results.Count > 0 ? Results.Dequeue() : NudgeSendStatus.Sent);
            }

            public Task<NudgeSendStatus> SendMessageAsync(string fcmToken, int unitId, string title,
                string body, string requestId, CancellationToken ct)
            {
                Messages.Add((fcmToken, unitId, title, body, requestId));
                return Task.FromResult(Results.Count > 0 ? Results.Dequeue() : NudgeSendStatus.Sent);
            }
        }

        private sealed class FakeSnapshot : ILiveSnapshotService
        {
            public List<LiveUnitDto> Units { get; } = new();

            public Task<IReadOnlyList<LiveUnitDto>> GetSnapshotAsync(CancellationToken ct)
                => Task.FromResult<IReadOnlyList<LiveUnitDto>>(Units);
        }

        [TestInitialize]
        public void Setup()
        {
            /* NoTracking mirrors production DI (the 12 Aug lesson): these tests must fail
               if a service mutates a queried entity without AsTracking. */
            _db = new TrackingDbContext(new DbContextOptionsBuilder<TrackingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);
            _options = new TrackingOptions();
            _sender = new FakeSender();
            _snapshot = new FakeSnapshot();
            _clock = Now;
        }

        [TestCleanup]
        public void Cleanup() => _db.Dispose();

        private DeviceTokenService Tokens()
            => new(_db, NullLogger<DeviceTokenService>.Instance, () => _clock);

        private TrackingMessageService Messages()
            => new(_db, Tokens(), _sender, _snapshot, _options,
                NullLogger<TrackingMessageService>.Instance, () => _clock);

        private TrackingPingService Ping()
            => new(_db, Tokens(), _sender, _options, NullLogger<TrackingPingService>.Instance, () => _clock);

        private async Task AddTokenAsync(int unitId, string token)
        {
            _db.TrackingDeviceTokens.Add(new TrackingDeviceToken
            {
                UnitId = unitId, FcmToken = token, Platform = "android",
                CreatedUtc = Now, UpdatedUtc = Now, LastSeenUtc = Now, IsActive = true
            });
            await _db.SaveChangesAsync();
        }

        private static LiveUnitDto Online(int unitId, string kind, int ageSeconds = 10)
            => new(unitId, Guid.NewGuid(), 0m, 0m, null, null, null, null, 1, 0,
                Now.AddSeconds(-ageSeconds), ageSeconds)
            { Kind = kind };

        /* ------------------------------ unit send ------------------------------ */

        [TestMethod]
        public async Task SendToUnit_SendsPayload_AndAuditsTheWords()
        {
            await AddTokenAsync(CarUnit, "tok-1");

            var result = await Messages().SendToUnitAsync(CarUnit, "  " + Hello + "  ", Operator,
                "10.0.0.1", CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.Sent, result.Status);
            Assert.AreEqual(1, result.TokensSent);
            Assert.IsNotNull(result.RequestId);

            var push = _sender.Messages.Single();
            Assert.AreEqual("tok-1", push.Token);
            Assert.AreEqual(CarUnit, push.UnitId);
            Assert.AreEqual(TrackingMessageService.PushTitle, push.Title);
            Assert.AreEqual(Hello, push.Body, "The body is the trimmed operator text.");
            Assert.AreEqual(result.RequestId, push.RequestId,
                "The correlation id travels with the message.");

            var audit = await _db.TrackingAccessAudits.SingleAsync();
            Assert.AreEqual("CommandMessage", audit.Action);
            Assert.AreEqual(Operator, audit.UserId);
            Assert.AreEqual(CarUnit, audit.UnitId);
            Assert.AreEqual(Hello, audit.Justification,
                "What was said is part of the audit, not just that something was said.");
        }

        [TestMethod]
        public async Task SendToUnit_EmptyOrWhitespace_IsRefused()
        {
            await AddTokenAsync(CarUnit, "tok-1");

            var empty = await Messages().SendToUnitAsync(CarUnit, "", Operator, null, CancellationToken.None);
            var blank = await Messages().SendToUnitAsync(CarUnit, "   ", Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.InvalidMessage, empty.Status);
            Assert.AreEqual(TrackingMessageStatus.InvalidMessage, blank.Status);
            Assert.AreEqual(0, _sender.Messages.Count);
            Assert.AreEqual(0, await _db.TrackingAccessAudits.CountAsync(),
                "A refused message leaves no audit row — nothing was commanded.");
        }

        [TestMethod]
        public async Task SendToUnit_OverlongMessage_IsRefused()
        {
            await AddTokenAsync(CarUnit, "tok-1");
            var overlong = new string('x', TrackingMessageService.MaxMessageLength + 1);

            var result = await Messages().SendToUnitAsync(CarUnit, overlong, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.InvalidMessage, result.Status);
            Assert.AreEqual(0, _sender.Messages.Count);
        }

        [TestMethod]
        public async Task SendToUnit_WithoutTokens_SaysSoInsteadOfPretending()
        {
            var result = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.NoTokens, result.Status);
            Assert.AreEqual(0, _sender.Messages.Count);
        }

        [TestMethod]
        public async Task SendToUnit_WhenPushNotConfigured_RefusesLoudly()
        {
            _sender.IsConfigured = false;
            await AddTokenAsync(CarUnit, "tok-1");

            var result = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.NotConfigured, result.Status);
            Assert.AreEqual(0, _sender.Messages.Count);
        }

        [TestMethod]
        public async Task SendToUnit_InsideCooldown_IsRefused_ThenAllowedAfter()
        {
            await AddTokenAsync(CarUnit, "tok-1");
            await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);

            var second = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);
            Assert.AreEqual(TrackingMessageStatus.Cooldown, second.Status,
                "A double-click must not double-message the phone.");

            _clock = Now.AddSeconds(_options.Fcm.MessageCooldownSeconds + 1);
            var third = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);
            Assert.AreEqual(TrackingMessageStatus.Sent, third.Status);
        }

        [TestMethod]
        public async Task PingCooldown_DoesNotBlockMessage()
        {
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(), UnitId = CarUnit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-1), Status = "Active"
            });
            await _db.SaveChangesAsync();
            await AddTokenAsync(CarUnit, "tok-1");

            var ping = await Ping().PingAsync(CarUnit, Operator, null, "ManualPing", CancellationToken.None);
            Assert.AreEqual(PingStatus.Sent, ping.Status);

            var message = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);
            Assert.AreEqual(TrackingMessageStatus.Sent, message.Status,
                "A CommandPing audit row must not put messaging on cooldown.");
        }

        [TestMethod]
        public async Task MessageCooldown_DoesNotBlockPing()
        {
            _db.TrackingSessions.Add(new TrackingSession
            {
                Id = Guid.NewGuid(), UnitId = CarUnit, GuardId = 7, ClientSiteId = 12,
                StartedUtc = Now.AddHours(-1), Status = "Active"
            });
            await _db.SaveChangesAsync();
            await AddTokenAsync(CarUnit, "tok-1");

            var message = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);
            Assert.AreEqual(TrackingMessageStatus.Sent, message.Status);

            var ping = await Ping().PingAsync(CarUnit, Operator, null, "ManualPing", CancellationToken.None);
            Assert.AreEqual(PingStatus.Sent, ping.Status,
                "A CommandMessage audit row must not put ping on cooldown.");
        }

        [TestMethod]
        public async Task SendToUnit_InvalidToken_IsRetired()
        {
            await AddTokenAsync(CarUnit, "tok-dead");
            _sender.Results.Enqueue(NudgeSendStatus.InvalidToken);

            var result = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.Sent, result.Status);
            Assert.AreEqual(0, result.TokensSent, "An invalid token is not a successful send.");
            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.IsFalse(stored.IsActive, "FCM said Unregistered: the token must be retired.");
        }

        [TestMethod]
        public async Task SendToUnit_TransientFailure_KeepsTokenActive()
        {
            await AddTokenAsync(CarUnit, "tok-1");
            _sender.Results.Enqueue(NudgeSendStatus.Failed);

            var result = await Messages().SendToUnitAsync(CarUnit, Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.Sent, result.Status);
            Assert.AreEqual(0, result.TokensSent);
            var stored = await _db.TrackingDeviceTokens.SingleAsync();
            Assert.IsTrue(stored.IsActive, "A transient failure must not burn the token.");
        }

        /* ------------------------------ broadcast ------------------------------ */

        [TestMethod]
        public async Task Broadcast_ExcludesStaleUnits()
        {
            _snapshot.Units.Add(Online(CarUnit, "car", ageSeconds: 100));
            _snapshot.Units.Add(Online(GuardUnit, "guard",
                ageSeconds: TrackingMessageService.OnlineThresholdSeconds + 1));
            await AddTokenAsync(CarUnit, "tok-car");
            await AddTokenAsync(GuardUnit, "tok-guard");

            var result = await Messages().BroadcastAsync("all", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.Sent, result.Status);
            Assert.AreEqual(1, result.UnitsTargeted, "A stale unit is offline; it is not a recipient.");
            Assert.AreEqual(CarUnit, _sender.Messages.Single().UnitId);
        }

        [TestMethod]
        public async Task Broadcast_CarKind_TargetsCarsOnly()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            _snapshot.Units.Add(Online(GuardUnit, "guard"));
            await AddTokenAsync(CarUnit, "tok-car");
            await AddTokenAsync(GuardUnit, "tok-guard");

            var result = await Messages().BroadcastAsync("car", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(1, result.UnitsTargeted);
            Assert.AreEqual(1, result.UnitsSent);
            Assert.AreEqual(CarUnit, _sender.Messages.Single().UnitId);
        }

        [TestMethod]
        public async Task Broadcast_GuardKind_TargetsGuardsOnly()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            _snapshot.Units.Add(Online(GuardUnit, "guard"));
            await AddTokenAsync(CarUnit, "tok-car");
            await AddTokenAsync(GuardUnit, "tok-guard");

            var result = await Messages().BroadcastAsync("guard", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(1, result.UnitsTargeted);
            Assert.AreEqual(1, result.UnitsSent);
            Assert.AreEqual(GuardUnit, _sender.Messages.Single().UnitId);
        }

        [TestMethod]
        public async Task Broadcast_AllKind_TargetsEveryOnlineUnit()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            _snapshot.Units.Add(Online(GuardUnit, "guard"));
            await AddTokenAsync(CarUnit, "tok-car");
            await AddTokenAsync(GuardUnit, "tok-guard");

            var result = await Messages().BroadcastAsync("all", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(2, result.UnitsTargeted);
            Assert.AreEqual(2, result.UnitsSent);
            Assert.AreEqual(2, result.TokensSent);
            CollectionAssert.AreEquivalent(new[] { CarUnit, GuardUnit },
                _sender.Messages.Select(m => m.UnitId).ToArray());
        }

        [TestMethod]
        public async Task Broadcast_CooledDownUnit_IsSkippedAndCounted()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            _snapshot.Units.Add(Online(GuardUnit, "guard"));
            await AddTokenAsync(CarUnit, "tok-car");
            await AddTokenAsync(GuardUnit, "tok-guard");
            _db.TrackingAccessAudits.Add(new TrackingAccessAudit
            {
                UserId = Operator, Action = "CommandMessage", UnitId = CarUnit, AccessedUtc = Now
            });
            await _db.SaveChangesAsync();

            var result = await Messages().BroadcastAsync("all", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.Sent, result.Status,
                "A partially-skipped broadcast is still a broadcast — never a whole-batch failure.");
            Assert.AreEqual(2, result.UnitsTargeted);
            Assert.AreEqual(1, result.UnitsSent);
            Assert.AreEqual(1, result.UnitsSkippedCooldown);
            Assert.AreEqual(GuardUnit, _sender.Messages.Single().UnitId,
                "The freshly-messaged phone must not be messaged twice.");
        }

        [TestMethod]
        public async Task Broadcast_TokenlessUnit_IsCounted()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            _snapshot.Units.Add(Online(GuardUnit, "guard"));
            await AddTokenAsync(CarUnit, "tok-car");     // the guard has no push registration

            var result = await Messages().BroadcastAsync("all", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(2, result.UnitsTargeted);
            Assert.AreEqual(1, result.UnitsSent);
            Assert.AreEqual(1, result.UnitsSkippedNoToken);
            Assert.AreEqual(CarUnit, _sender.Messages.Single().UnitId);
        }

        [TestMethod]
        public async Task Broadcast_AuditsEveryAttemptedUnit_WithSharedRequestId()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            _snapshot.Units.Add(Online(GuardUnit, "guard"));
            await AddTokenAsync(CarUnit, "tok-car");
            await AddTokenAsync(GuardUnit, "tok-guard");

            var result = await Messages().BroadcastAsync("all", Hello, Operator, null, CancellationToken.None);

            var audits = await _db.TrackingAccessAudits.ToListAsync();
            Assert.AreEqual(2, audits.Count, "One audit row per unit actually attempted.");
            Assert.IsTrue(audits.All(a => a.Action == "CommandMessage" && a.Justification == Hello
                                          && a.UserId == Operator));
            CollectionAssert.AreEquivalent(new[] { CarUnit, GuardUnit },
                audits.Select(a => a.UnitId!.Value).ToArray());
            Assert.IsTrue(_sender.Messages.All(m => m.RequestId == result.RequestId),
                "One broadcast is one correlated act — a single request id across every send.");
        }

        [TestMethod]
        public async Task Broadcast_NoOnlineMatch_SaysNoRecipients()
        {
            _snapshot.Units.Add(Online(CarUnit, "car"));
            await AddTokenAsync(CarUnit, "tok-car");

            var result = await Messages().BroadcastAsync("guard", Hello, Operator, null, CancellationToken.None);

            Assert.AreEqual(TrackingMessageStatus.NoRecipients, result.Status,
                "\"Nobody online\" is a loud refusal, not a silent 202.");
            Assert.AreEqual(0, _sender.Messages.Count);
            Assert.AreEqual(0, await _db.TrackingAccessAudits.CountAsync());
        }

        /* --------------------- controller: read-only viewer --------------------- */

        private static TrackingController ControllerWithSid(string sid)
            => new(null!, null!, null!, null!, null!, null!, null!, new TrackingOptions())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.Sid, sid) }, "test"))
                    }
                }
            };

        [TestMethod]
        public async Task Controller_ReadOnlySidZero_IsRefused_BeforeTheServiceRuns()
        {
            /* The keyed map viewer signs in with Sid "0" — authenticated enough to watch,
               never enough to speak to an officer's phone. The service argument is null!,
               so these fail loudly if the guard does not fire first. */
            var single = await ControllerWithSid("0").Message(CarUnit,
                new TrackingController.UnitMessageRequest(Hello), null!, default);
            var broadcast = await ControllerWithSid("0").BroadcastMessage(
                new TrackingController.BroadcastMessageRequest(Hello, "all"), null!, default);

            Assert.AreEqual(403, ((ObjectResult)single).StatusCode);
            Assert.AreEqual(403, ((ObjectResult)broadcast).StatusCode);
        }
    }
}
