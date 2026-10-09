using System;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using CityWatch.Tracking.Services.Push;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services
{
    public enum PingStatus
    {
        Sent,
        NoTokens,
        NotConfigured,
        Cooldown
    }

    public sealed record PingResult(PingStatus Status, string? RequestId, int TokensPinged);

    public interface ITrackingPingService
    {
        /// <summary>Nudges every active device of a unit for a fresh position. "Sent" means
        /// FCM ACCEPTED the message — success of the nudge is only ever observed as a fresh
        /// position arriving on the ingest path. Audited like every operator command.</summary>
        Task<PingResult> PingAsync(int unitId, int operatorUserId, string? ipAddress, string reason,
            CancellationToken ct);
    }

    public sealed class TrackingPingService : ITrackingPingService
    {
        private const string AuditAction = "CommandPing";

        private readonly TrackingDbContext _db;
        private readonly IDeviceTokenService _tokens;
        private readonly ITrackingNudgeSender _sender;
        private readonly TrackingOptions _options;
        private readonly ILogger<TrackingPingService> _logger;
        private readonly Func<DateTime> _utcNow;

        public TrackingPingService(TrackingDbContext db, IDeviceTokenService tokens,
            ITrackingNudgeSender sender, TrackingOptions options, ILogger<TrackingPingService> logger,
            Func<DateTime>? utcNow = null)
        {
            _db = db;
            _tokens = tokens;
            _sender = sender;
            _options = options;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<PingResult> PingAsync(int unitId, int operatorUserId, string? ipAddress,
            string reason, CancellationToken ct)
        {
            if (!_sender.IsConfigured)
                return new PingResult(PingStatus.NotConfigured, null, 0);

            /* Cooldown reads the audit trail itself — the record of pings IS the audit row,
               so there is no second table to drift out of sync. The cooldown protects
               Android's high-priority delivery quota as much as the operator's patience. */
            var now = _utcNow();
            var cooldownFloor = now.AddSeconds(-Math.Max(1, _options.Fcm.PingCooldownSeconds));
            var recentlyPinged = await _db.TrackingAccessAudits.AnyAsync(a =>
                a.UnitId == unitId && a.Action == AuditAction && a.AccessedUtc >= cooldownFloor, ct);
            if (recentlyPinged)
                return new PingResult(PingStatus.Cooldown, null, 0);

            var tokens = await _tokens.GetActiveAsync(unitId, ct);
            if (tokens.Count == 0)
                return new PingResult(PingStatus.NoTokens, null, 0);

            /* Nudging a named officer's phone is an act worth a trace, exactly like
               CommandLive (§13.4). Recorded before the sends: the intent is the act. */
            var requestId = Guid.NewGuid().ToString("N");
            _db.TrackingAccessAudits.Add(new TrackingAccessAudit
            {
                UserId = operatorUserId,
                Action = AuditAction,
                UnitId = unitId,
                AccessedUtc = now,
                IpAddress = ipAddress
            });
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("TrackingNudgeRequested unit {Unit} by user {User} request {RequestId} reason {Reason}.",
                unitId, operatorUserId, requestId, reason);

            var sent = 0;
            foreach (var token in tokens)
            {
                var status = await _sender.SendNudgeAsync(token.FcmToken, unitId, reason, requestId, ct);
                if (status == NudgeSendStatus.Sent)
                    sent++;
                else if (status == NudgeSendStatus.InvalidToken)
                    await _tokens.MarkInvalidAsync(token.Id, ct);
                /* Failed: transient — the token stays active for the next attempt. */
            }

            return new PingResult(PingStatus.Sent, requestId, sent);
        }
    }
}
