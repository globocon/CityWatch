using System;
using System.Collections.Concurrent;
using CityWatch.Tracking.Configuration;

namespace CityWatch.Tracking.Services
{
    /// <summary>
    /// Fixed-window batch counter per unit. Deliberately simple: the limit exists to stop a
    /// runaway device or a scripted flood, not to shape traffic — a healthy device sends one
    /// batch a minute in Transit and twelve in Live, far under the default of 30.
    /// </summary>
    public sealed class UnitRateLimiter
    {
        private readonly record struct Window(long Minute, int Count);

        private readonly ConcurrentDictionary<int, Window> _windows = new();
        private readonly int _perMinute;

        public UnitRateLimiter(TrackingOptions options)
            => _perMinute = Math.Max(1, options.IngestRateLimitPerUnitPerMinute);

        public bool TryAcquire(int unitId, DateTime utcNow)
        {
            var minute = utcNow.Ticks / TimeSpan.TicksPerMinute;
            var updated = _windows.AddOrUpdate(
                unitId,
                _ => new Window(minute, 1),
                (_, w) => w.Minute == minute ? w with { Count = w.Count + 1 } : new Window(minute, 1));
            return updated.Count <= _perMinute;
        }
    }
}
