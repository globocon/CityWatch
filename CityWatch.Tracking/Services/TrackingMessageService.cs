using System;
using System.Collections.Generic;
using System.Linq;
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
    public enum TrackingMessageStatus
    {
        Sent,
        NoTokens,
        NotConfigured,
        Cooldown,
        InvalidMessage,
        NoRecipients
    }

    public sealed record UnitMessageResult(TrackingMessageStatus Status, string? RequestId, int TokensSent);

    public sealed record BroadcastMessageResult(TrackingMessageStatus Status, string? RequestId,
        int UnitsTargeted, int UnitsSent, int TokensSent, int UnitsSkippedNoToken, int UnitsSkippedCooldown);

    public interface ITrackingMessageService
    {
        /// <summary>Operator's custom text to one unit's phone. "Sent" means FCM ACCEPTED
        /// the message — never that the officer saw it. Audited like every operator
        /// command, with the text itself in the audit row's Justification.</summary>
        Task<UnitMessageResult> SendToUnitAsync(int unitId, string message, int operatorUserId,
            string? ipAddress, CancellationToken ct);

        /// <summary>kind: "all" | "car" | "guard". Recipients are resolved SERVER-side from
        /// the live snapshot (AgeSeconds ≤ OnlineThresholdSeconds) — the client's counts are
        /// advisory only. Cooled-down and tokenless units are SKIPPED and counted, never a
        /// whole-batch failure.</summary>
        Task<BroadcastMessageResult> BroadcastAsync(string kind, string message, int operatorUserId,
            string? ipAddress, CancellationToken ct);
    }

    public sealed class TrackingMessageService : ITrackingMessageService
    {
        public const string AuditAction = "CommandMessage";   // 14 chars — fits Action nvarchar(20)

        /// <summary>Justification is nvarchar(500); 240 keeps the whole text auditable with margin.</summary>
        public const int MaxMessageLength = 240;

        /// <summary>Matches the client's HOLLOW_S=300 offline line (controlRoomTracking.js).</summary>
        public const int OnlineThresholdSeconds = 300;

        public const string PushTitle = "CityWatch Control Room";

        private readonly TrackingDbContext _db;
        private readonly IDeviceTokenService _tokens;
        private readonly ITrackingNudgeSender _sender;
        private readonly ILiveSnapshotService _snapshot;
        private readonly TrackingOptions _options;
        private readonly ILogger<TrackingMessageService> _logger;
        private readonly Func<DateTime> _utcNow;

        public TrackingMessageService(TrackingDbContext db, IDeviceTokenService tokens,
            ITrackingNudgeSender sender, ILiveSnapshotService snapshot, TrackingOptions options,
            ILogger<TrackingMessageService> logger, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _tokens = tokens;
            _sender = sender;
            _snapshot = snapshot;
            _options = options;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<UnitMessageResult> SendToUnitAsync(int unitId, string message, int operatorUserId,
            string? ipAddress, CancellationToken ct)
        {
            var text = (message ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > MaxMessageLength)
                return new UnitMessageResult(TrackingMessageStatus.InvalidMessage, null, 0);

            if (!_sender.IsConfigured)
                return new UnitMessageResult(TrackingMessageStatus.NotConfigured, null, 0);

            /* Cooldown reads the audit trail itself, exactly like ping — but filtered on
               THIS action, so message and ping cooldowns never interfere with each other. */
            var now = _utcNow();
            var cooldownFloor = now.AddSeconds(-Math.Max(1, _options.Fcm.MessageCooldownSeconds));
            var recentlyMessaged = await _db.TrackingAccessAudits.AnyAsync(a =>
                a.UnitId == unitId && a.Action == AuditAction && a.AccessedUtc >= cooldownFloor, ct);
            if (recentlyMessaged)
                return new UnitMessageResult(TrackingMessageStatus.Cooldown, null, 0);

            var tokens = await _tokens.GetActiveAsync(unitId, ct);
            if (tokens.Count == 0)
                return new UnitMessageResult(TrackingMessageStatus.NoTokens, null, 0);

            /* Audited BEFORE the sends — the intent is the act (the CommandPing precedent).
               Justification carries the words: in a dispute, what was said to whom matters
               as much as who was looked at. */
            var requestId = Guid.NewGuid().ToString("N");
            _db.TrackingAccessAudits.Add(new TrackingAccessAudit
            {
                UserId = operatorUserId,
                Action = AuditAction,
                UnitId = unitId,
                AccessedUtc = now,
                IpAddress = ipAddress,
                Justification = text
            });
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("TrackingMessageRequested unit {Unit} by user {User} request {RequestId}.",
                unitId, operatorUserId, requestId);

            var sent = await SendToTokensAsync(tokens, unitId, text, requestId, ct);
            return new UnitMessageResult(TrackingMessageStatus.Sent, requestId, sent);
        }

        public async Task<BroadcastMessageResult> BroadcastAsync(string kind, string message,
            int operatorUserId, string? ipAddress, CancellationToken ct)
        {
            var text = (message ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > MaxMessageLength)
                return new BroadcastMessageResult(TrackingMessageStatus.InvalidMessage, null, 0, 0, 0, 0, 0);

            if (!_sender.IsConfigured)
                return new BroadcastMessageResult(TrackingMessageStatus.NotConfigured, null, 0, 0, 0, 0, 0);

            /* Recipients come from the server's own live picture, not from whatever the
               operator's browser happened to be showing: online = a fix newer than the
               same threshold the map calls "offline". */
            var normalizedKind = (kind ?? "all").Trim().ToLowerInvariant();
            var online = (await _snapshot.GetSnapshotAsync(ct))
                .Where(u => u.AgeSeconds <= OnlineThresholdSeconds)
                .Where(u => normalizedKind == "all" || u.Kind == normalizedKind)
                .ToList();
            if (online.Count == 0)
                return new BroadcastMessageResult(TrackingMessageStatus.NoRecipients, null, 0, 0, 0, 0, 0);

            var targetIds = online.Select(u => u.UnitId).ToList();

            /* One query per concern, never per unit: cooled-down units are skipped (an
               operator repeating a broadcast must not double-message the same phones),
               tokenless units are counted honestly. */
            var now = _utcNow();
            var cooldownFloor = now.AddSeconds(-Math.Max(1, _options.Fcm.MessageCooldownSeconds));
            var cooledDownIds = await _db.TrackingAccessAudits
                .Where(a => a.Action == AuditAction && a.AccessedUtc >= cooldownFloor
                            && a.UnitId != null && targetIds.Contains(a.UnitId.Value))
                .Select(a => a.UnitId!.Value)
                .Distinct()
                .ToListAsync(ct);
            var unitsSkippedCooldown = cooledDownIds.Count;

            var remainingIds = targetIds.Except(cooledDownIds).ToList();
            var tokensByUnit = (await _tokens.GetActiveForUnitsAsync(remainingIds, ct))
                .GroupBy(t => t.UnitId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var unitsSkippedNoToken = remainingIds.Count(id => !tokensByUnit.ContainsKey(id));

            var attemptedIds = remainingIds.Where(id => tokensByUnit.ContainsKey(id)).ToList();
            var requestId = Guid.NewGuid().ToString("N");

            /* One audit row per unit actually attempted, one SaveChanges for the batch. */
            foreach (var unitId in attemptedIds)
            {
                _db.TrackingAccessAudits.Add(new TrackingAccessAudit
                {
                    UserId = operatorUserId,
                    Action = AuditAction,
                    UnitId = unitId,
                    AccessedUtc = now,
                    IpAddress = ipAddress,
                    Justification = text
                });
            }
            if (attemptedIds.Count > 0)
                await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "TrackingBroadcastRequested kind {Kind} by user {User} request {RequestId}: {Targeted} online, {Attempted} attempted.",
                normalizedKind, operatorUserId, requestId, targetIds.Count, attemptedIds.Count);

            var tokensSent = 0;
            var unitsSent = 0;
            foreach (var unitId in attemptedIds)
            {
                var sent = await SendToTokensAsync(tokensByUnit[unitId], unitId, text, requestId, ct);
                tokensSent += sent;
                if (sent > 0)
                    unitsSent++;
            }

            /* "Nobody online" refuses above; "online but unreachable" answers with honest
               zeros — a 202 with counters is not silence. */
            return new BroadcastMessageResult(TrackingMessageStatus.Sent, requestId,
                targetIds.Count, unitsSent, tokensSent, unitsSkippedNoToken, unitsSkippedCooldown);
        }

        private async Task<int> SendToTokensAsync(List<TrackingDeviceToken> tokens, int unitId,
            string text, string requestId, CancellationToken ct)
        {
            var sent = 0;
            foreach (var token in tokens)
            {
                var status = await _sender.SendMessageAsync(token.FcmToken, unitId, PushTitle, text,
                    requestId, ct);
                if (status == NudgeSendStatus.Sent)
                    sent++;
                else if (status == NudgeSendStatus.InvalidToken)
                    await _tokens.MarkInvalidAsync(token.Id, ct);
                /* Failed: transient — the token stays active for the next attempt. */
            }
            return sent;
        }
    }
}
