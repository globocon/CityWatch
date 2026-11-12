using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CityWatch.Tracking.Configuration;
using CityWatch.Tracking.Hubs;
using CityWatch.Tracking.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CityWatch.Tracking.Hosted
{
    /// <summary>
    /// The 1 Hz diff broadcaster (§10.3). Wakes once a second, asks the live store what
    /// changed since the last tick, and sends ONE frame to the operators group — or nothing
    /// at all when the fleet is idle, which at patrol scale is most seconds of most nights.
    ///
    /// Leader-only (§7.4): running this on N instances multiplies every frame by N. Phase 1
    /// gates on Tracking:IsLeaderInstance; Phase 2 replaces the flag with a Redis lock (D11).
    /// </summary>
    public sealed class BroadcastTicker : BackgroundService
    {
        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

        private readonly ILiveStateStore _liveState;
        private readonly IHubContext<PatrolTrackingHub> _hub;
        private readonly TrackingOptions _options;
        private readonly ILogger<BroadcastTicker> _logger;

        public BroadcastTicker(ILiveStateStore liveState, IHubContext<PatrolTrackingHub> hub,
            TrackingOptions options, ILogger<BroadcastTicker> logger)
        {
            _liveState = liveState;
            _hub = hub;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.IsLeaderInstance)
            {
                _logger.LogInformation("BroadcastTicker idle: this instance is not the leader.");
                return;
            }

            long cursor = 0;
            using var timer = new PeriodicTimer(Tick);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    var (changed, version) = _liveState.ChangedSince(cursor);
                    cursor = version;
                    if (changed.Count == 0)
                        continue;   // idle fleet ⇒ empty frame ⇒ no message at all

                    var now = DateTime.UtcNow;
                    var frame = new
                    {
                        t = now,
                        u = changed.Select(c => new
                        {
                            id = c.UnitId,
                            la = c.Lat,
                            lo = c.Lon,
                            s = c.SpeedKph,
                            h = c.HeadingDeg,
                            m = (byte)c.Mode,
                            f = (byte)c.Flags,
                            a = (int)Math.Max(0, (now - c.ReceivedUtc).TotalSeconds)
                        }).ToArray()
                    };

                    try
                    {
                        await _hub.Clients.Group(PatrolTrackingHub.OperatorsGroup)
                            .SendAsync("Frame", frame, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        /* A failed frame is not worth crashing the pump: the next tick
                           re-diffs from the same cursor semantics (client is last-write-wins),
                           and the browser's poll fallback covers a dead hub honestly. */
                        _logger.LogWarning(ex, "Broadcast frame failed; continuing.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }
    }
}
