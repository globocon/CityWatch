using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Events;
using CityWatch.Events.Events;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Contracts;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Services
{
    public sealed record ModeResolution(TrackingMode DesiredMode, int CommandSeq, int? TtlSecondsRemaining);

    public interface IModeCommandService
    {
        /// <summary>Operator requests Live Mode. Enforces the concurrency cap and the TTL,
        /// supersedes any earlier command for the unit, audits the act (§5.3).</summary>
        Task<(bool Ok, string? Error, TrackingModeCommand? Command)> RequestLiveAsync(
            int unitId, int operatorUserId, string? ipAddress, CancellationToken ct);

        /// <summary>Ends Live Mode for a unit. Ending twice is a no-op.</summary>
        Task CancelAsync(int unitId, int? operatorUserId, string reason, string? ipAddress, CancellationToken ct);

        /// <summary>The device's view: expires lapsed commands, applies the device's ack,
        /// and returns the authoritative desired mode (§5.3, D5). Called on every ingest.</summary>
        Task<ModeResolution> ResolveAsync(int unitId, int commandSeqSeen, CancellationToken ct);

        /// <summary>Escalates a unit to Duress Mode: no cap, no TTL, supersedes everything.
        /// System-issued (duress is raised by the platform, not by an operator here).</summary>
        Task RequestDuressAsync(int unitId, CancellationToken ct);
    }

    /// <summary>
    /// The authority for §5.4's mode state machine, server side. The row is the truth; push
    /// is an accelerator; the ingest response is the guaranteed delivery path.
    /// </summary>
    public sealed class ModeCommandService : IModeCommandService
    {
        private readonly TrackingDbContext _db;
        private readonly TrackingOptions _options;
        private readonly IDomainEventPublisher _events;
        private readonly ILogger<ModeCommandService> _logger;
        private readonly Func<DateTime> _utcNow;

        public ModeCommandService(TrackingDbContext db, TrackingOptions options,
            IDomainEventPublisher events, ILogger<ModeCommandService> logger, Func<DateTime>? utcNow = null)
        {
            _db = db;
            _options = options;
            _events = events;
            _logger = logger;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<(bool Ok, string? Error, TrackingModeCommand? Command)> RequestLiveAsync(
            int unitId, int operatorUserId, string? ipAddress, CancellationToken ct)
        {
            var now = _utcNow();

            var hasSession = await _db.TrackingSessions
                .AnyAsync(s => s.UnitId == unitId && s.Status == "Active", ct);
            if (!hasSession)
                return (false, "Unit has no active patrol session.", null);

            /* Duress owns the unit: an operator cannot downgrade it to Live (§5.1 precedence). */
            var duressActive = await _db.TrackingModeCommands.AnyAsync(c =>
                c.UnitId == unitId && c.DesiredMode == (byte)TrackingMode.Duress &&
                (c.Status == "Pending" || c.Status == "Active"), ct);
            if (duressActive)
                return (false, "Unit is in Duress Mode; Live Mode cannot override it.", null);

            /* Live Mode is a spotlight, not a floodlight (§5.3). */
            var liveCount = await _db.TrackingModeCommands.CountAsync(c =>
                c.DesiredMode == (byte)TrackingMode.Live &&
                (c.Status == "Pending" || c.Status == "Active") &&
                c.ExpiresUtc > now && c.UnitId != unitId, ct);
            if (liveCount >= _options.MaxConcurrentLiveUnits)
                return (false, $"Live tracking limit reached ({_options.MaxConcurrentLiveUnits} units).", null);

            await SupersedeOpenCommandsAsync(unitId, "Superseded", ct);

            var command = new TrackingModeCommand
            {
                UnitId = unitId,
                CommandSeq = await NextSeqAsync(unitId, ct),
                DesiredMode = (byte)TrackingMode.Live,
                IssuedByUserId = operatorUserId,
                IssuedUtc = now,
                ExpiresUtc = now.AddSeconds(_options.LiveModeTtlSeconds),
                Status = "Pending"
            };
            _db.TrackingModeCommands.Add(command);

            /* Turning close surveillance on a named officer is exactly the act that must
               leave a trace (§13.4). Same transaction as the command itself. */
            _db.TrackingAccessAudits.Add(new TrackingAccessAudit
            {
                UserId = operatorUserId,
                Action = "CommandLive",
                UnitId = unitId,
                AccessedUtc = now,
                IpAddress = ipAddress
            });
            await _db.SaveChangesAsync(ct);

            _events.Publish(new LiveTrackingRequested(unitId, operatorUserId, _options.LiveModeTtlSeconds, now));
            _logger.LogInformation("Live Mode requested for unit {Unit} by user {User} (seq {Seq}, ttl {Ttl}s).",
                unitId, operatorUserId, command.CommandSeq, _options.LiveModeTtlSeconds);

            return (true, null, command);
        }

        public async Task CancelAsync(int unitId, int? operatorUserId, string reason, string? ipAddress, CancellationToken ct)
        {
            var now = _utcNow();
            /* AsTracking: the context defaults to NoTracking (DI, §3.3), under which these
               mutations silently persist NOTHING — field-verified 12 Aug: every command row
               sat "Pending" forever, so Stop Live never actually cancelled server-side. */
            var open = await _db.TrackingModeCommands
                .AsTracking()
                .Where(c => c.UnitId == unitId && (c.Status == "Pending" || c.Status == "Active"))
                .ToListAsync(ct);
            if (open.Count == 0)
                return;

            foreach (var command in open)
            {
                command.Status = "Cancelled";
                command.EndReason = reason;
            }

            if (operatorUserId is { } userId)
            {
                _db.TrackingAccessAudits.Add(new TrackingAccessAudit
                {
                    UserId = userId,
                    Action = "CommandCancel",
                    UnitId = unitId,
                    AccessedUtc = now,
                    IpAddress = ipAddress
                });
            }
            await _db.SaveChangesAsync(ct);

            _events.Publish(new LiveTrackingEnded(unitId, operatorUserId, reason, now));
        }

        public async Task<ModeResolution> ResolveAsync(int unitId, int commandSeqSeen, CancellationToken ct)
        {
            var now = _utcNow();

            /* AsTracking: TTL expiry and the device ack below mutate these rows; without it
               the NoTracking default made both silent no-ops (commands never left Pending). */
            var open = await _db.TrackingModeCommands
                .AsTracking()
                .Where(c => c.UnitId == unitId && (c.Status == "Pending" || c.Status == "Active"))
                .OrderByDescending(c => c.CommandSeq)
                .ToListAsync(ct);

            var dirty = false;
            TrackingModeCommand? current = null;

            foreach (var command in open)
            {
                /* TTL enforcement happens here, on the device's own heartbeat: a forgotten
                   Live session reverts even if no operator ever clicks stop (§5.3). */
                if (command.ExpiresUtc is { } expiry && expiry <= now)
                {
                    command.Status = "Expired";
                    command.EndReason = "TtlExpired";
                    dirty = true;
                    _events.Publish(new LiveTrackingEnded(command.UnitId, null, "Expired", now));
                    continue;
                }

                /* Duress has no TTL, but it is not eternal either: it lives exactly as long
                   as the platform's ClientSiteDuress rows say the alarm is on. The control
                   room deactivating duress DELETES those rows without telling this pack —
                   so the device's own heartbeat is where a cleared alarm stands down. */
                if (command.DesiredMode == (byte)TrackingMode.Duress &&
                    !await DuressStillOnAsync(unitId, ct))
                {
                    command.Status = "Cancelled";
                    command.EndReason = "DuressCleared";
                    dirty = true;
                    _events.Publish(new LiveTrackingEnded(command.UnitId, null, "DuressCleared", now));
                    continue;
                }

                current ??= command;   // newest non-expired wins
            }

            if (current != null && commandSeqSeen >= current.CommandSeq && current.AcknowledgedUtc == null)
            {
                /* The UI stops saying "Live requested…" only now (§11.3 rule 5). */
                current.AcknowledgedUtc = now;
                current.Status = "Active";
                dirty = true;
            }

            if (dirty)
                await _db.SaveChangesAsync(ct);

            if (current == null)
                return new ModeResolution(TrackingMode.Normal, commandSeqSeen, null);

            return new ModeResolution(
                (TrackingMode)current.DesiredMode,
                current.CommandSeq,
                current.ExpiresUtc is { } exp ? (int?)Math.Max(0, (int)(exp - now).TotalSeconds) : null);
        }

        public async Task RequestDuressAsync(int unitId, CancellationToken ct)
        {
            var now = _utcNow();

            /* Idempotent: duress raised twice keeps the one open command. */
            var alreadyInDuress = await _db.TrackingModeCommands.AnyAsync(c =>
                c.UnitId == unitId && c.DesiredMode == (byte)TrackingMode.Duress &&
                (c.Status == "Pending" || c.Status == "Active"), ct);
            if (alreadyInDuress)
                return;

            await SupersedeOpenCommandsAsync(unitId, "DuressOverride", ct);

            _db.TrackingModeCommands.Add(new TrackingModeCommand
            {
                UnitId = unitId,
                CommandSeq = await NextSeqAsync(unitId, ct),
                DesiredMode = (byte)TrackingMode.Duress,
                IssuedByUserId = null,      // system-issued
                IssuedUtc = now,
                ExpiresUtc = null,          // duress never times out (§5.4)
                Status = "Pending"
            });
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("Unit {Unit} commanded to Duress Mode.", unitId);
        }

        /* Backed = an enabled ClientSiteDuress row raised by the guard of this unit's active
           session — the same guard-keyed association DuressHandler used to escalate. A unit
           with no active session has nobody the alarm could be for; that counts as cleared. */
        private async Task<bool> DuressStillOnAsync(int unitId, CancellationToken ct)
            => await _db.PlatformClientSiteDuress.AnyAsync(d => d.IsEnabled &&
                   _db.TrackingSessions.Any(s =>
                       s.UnitId == unitId && s.Status == "Active" && s.GuardId == d.EnabledBy), ct);

        private async Task SupersedeOpenCommandsAsync(int unitId, string reason, CancellationToken ct)
        {
            var open = await _db.TrackingModeCommands
                .AsTracking()                     // mutated below; see CancelAsync
                .Where(c => c.UnitId == unitId && (c.Status == "Pending" || c.Status == "Active"))
                .ToListAsync(ct);
            foreach (var command in open)
            {
                command.Status = "Superseded";
                command.EndReason = reason;
            }
        }

        private async Task<int> NextSeqAsync(int unitId, CancellationToken ct)
            => (await _db.TrackingModeCommands.Where(c => c.UnitId == unitId)
                   .MaxAsync(c => (int?)c.CommandSeq, ct) ?? 0) + 1;
    }
}
