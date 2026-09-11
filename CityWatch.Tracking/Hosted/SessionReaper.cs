using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Data;
using CityWatch.Tracking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Hosted
{
    /// <summary>
    /// Expires sessions nobody ended. A unit that goes quiet mid-shift stays on the map —
    /// findable is the point — but a login nobody closed must not haunt the control room
    /// as a permanently-stationary unit the next day (the "14h 59m ago" marker).
    ///
    /// Stale = no point RECEIVED (server clock — device clocks lie) for
    /// Reaper.StaleAfterHours, and the session is older than that too. Closure goes
    /// through ISessionService.EndAsync, so an expiry is indistinguishable from a logout
    /// everywhere else: live state cleared, segments rolled up, Status=Expired /
    /// EndReason=Reaper in the evidentiary record.
    ///
    /// Leader-only, same flag-gate pattern as <see cref="BroadcastTicker"/> (D11).
    /// </summary>
    public sealed class SessionReaper : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly TrackingOptions _options;
        private readonly ILogger<SessionReaper> _logger;

        public SessionReaper(IServiceScopeFactory scopes, TrackingOptions options, ILogger<SessionReaper> logger)
        {
            _scopes = scopes;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.IsLeaderInstance || !_options.Reaper.Enabled)
                return;

            /* First sweep soon after startup — a restart must not wait a whole period to
               clear yesterday's leftovers — then settle into the configured cadence. */
            var delay = TimeSpan.FromMinutes(1);
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { break; }
                delay = TimeSpan.FromMinutes(Math.Max(1, _options.Reaper.SweepMinutes));

                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();
                    await SweepAsync(
                        db, scope.ServiceProvider.GetRequiredService<ISessionService>(),
                        DateTime.UtcNow, _options.Reaper, _logger, stoppingToken);
                    await ReconcileDuressAsync(db, _logger, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Session reaper sweep failed; retrying next period.");
                }
            }
        }

        /// <summary>One sweep. Static so tests exercise the policy without timers or a host.</summary>
        public static async Task<int> SweepAsync(TrackingDbContext db, ISessionService sessions,
            DateTime nowUtc, TrackingOptions.ReaperOptions options, ILogger logger, CancellationToken ct)
        {
            var cutoff = nowUtc.AddHours(-Math.Max(1, options.StaleAfterHours));

            /* A recent point keeps a session alive no matter how old the login is — long
               shifts are normal, abandoned logins are not. Ids only: EndAsync re-reads
               each session itself and is a no-op for anything closed in between. */
            var staleIds = await db.TrackingSessions
                .Where(s => s.Status == "Active" && s.StartedUtc < cutoff)
                .Where(s => !db.TrackPoints.Any(p => p.SessionId == s.Id && p.ReceivedUtc >= cutoff))
                .Select(s => s.Id)
                .ToListAsync(ct);

            foreach (var id in staleIds)
                await sessions.EndAsync(id, "Reaper", ct);

            if (staleIds.Count > 0)
                logger.LogInformation("Session reaper expired {Count} session(s) silent for over {Hours}h.",
                    staleIds.Count, options.StaleAfterHours);
            return staleIds.Count;
        }

        /// <summary>Stands down duress commands whose alarm the control room has deactivated
        /// (its deactivate DELETES the ClientSiteDuress rows). The device's own heartbeat does
        /// this in ResolveAsync; this sweep is for the phone that is offline or dead — its
        /// marker must not flash DURESS for a day after the alarm was cleared (18 Aug field
        /// report: a cleared duress sat Active for 24h+ because nothing ever re-resolved it).</summary>
        public static async Task<int> ReconcileDuressAsync(TrackingDbContext db, ILogger logger, CancellationToken ct)
        {
            var orphaned = await db.TrackingModeCommands
                .AsTracking()   // mutated below; the context default is NoTracking
                .Where(c => c.DesiredMode == (byte)Contracts.TrackingMode.Duress &&
                            (c.Status == "Pending" || c.Status == "Active"))
                .Where(c => !db.PlatformClientSiteDuress.Any(d => d.IsEnabled &&
                            db.TrackingSessions.Any(s =>
                                s.UnitId == c.UnitId && s.Status == "Active" && s.GuardId == d.EnabledBy)))
                .ToListAsync(ct);
            if (orphaned.Count == 0)
                return 0;

            foreach (var command in orphaned)
            {
                command.Status = "Cancelled";
                command.EndReason = "DuressCleared";
            }
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Duress reconcile stood down {Count} command(s) with no backing ClientSiteDuress row.",
                orphaned.Count);
            return orphaned.Count;
        }
    }
}
