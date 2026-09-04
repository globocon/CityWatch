using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CityWatch.Kpi.Services.FastReport
{
    /// <summary>
    /// Per-job memoisation store shared by every caching data-provider decorator in a
    /// single report scope.
    ///
    /// Scope matters: the cache lives exactly as long as one report generation. It is
    /// never shared between jobs, so a report can never observe data captured before it
    /// started. That keeps the freshness semantics identical to the existing generator,
    /// which reads everything inside one request anyway.
    /// </summary>
    public sealed class ReportScopeCache
    {
        private readonly ConcurrentDictionary<string, object> _values = new();
        private readonly ConcurrentDictionary<string, MethodCounter> _counters = new();

        private int _hits;
        private int _misses;
        private int _passThrough;
        private long _dataAccessTicks;

        /// <summary>
        /// Optional hook so the orchestrator can surface "Loading guard compliance..."
        /// style detail while a long provider call is in flight.
        /// </summary>
        public Action<string> OnDataAccess { get; set; }

        public int Hits => Volatile.Read(ref _hits);
        public int Misses => Volatile.Read(ref _misses);
        public int PassThrough => Volatile.Read(ref _passThrough);

        /// <summary>Every intercepted provider call, cached or not. Drives intra-site progress.</summary>
        public int TotalCalls => Hits + Misses + PassThrough;

        public long DataAccessMilliseconds =>
            (long)TimeSpan.FromTicks(Interlocked.Read(ref _dataAccessTicks)).TotalMilliseconds;

        public bool TryGet(string key, out object value) => _values.TryGetValue(key, out value);

        public void Set(string key, object value) => _values[key] = value;

        /// <summary>
        /// Nesting depth of intercepted calls on the current async flow.
        ///
        /// Decorated services call each other - IViewDataService.GetKpiReportData calls
        /// IClientDataProvider, IPatrolDataReportService.GetDailyPatrolData calls
        /// IIrDataProvider - so a naive sum counts the same wall-clock once per layer and
        /// can report more data-access time than the whole report took. Only the outermost
        /// call contributes to the total; per-method figures still record their own elapsed
        /// so the breakdown stays useful.
        /// </summary>
        private static readonly AsyncLocal<int> CallDepth = new();

        /// <summary>Increments nesting depth and returns the new value (1 = outermost).</summary>
        public int EnterCall() => CallDepth.Value += 1;

        public void ExitCall() => CallDepth.Value -= 1;

        public void RecordHit(string method)
        {
            Interlocked.Increment(ref _hits);
            Counter(method).AddHit();
        }

        public void RecordMiss(string method, long elapsedTicks, bool isOutermost)
        {
            Interlocked.Increment(ref _misses);
            if (isOutermost)
                Interlocked.Add(ref _dataAccessTicks, elapsedTicks);
            Counter(method).AddCall(elapsedTicks);
        }

        public void RecordPassThrough(string method, long elapsedTicks, bool isOutermost)
        {
            Interlocked.Increment(ref _passThrough);
            if (isOutermost)
                Interlocked.Add(ref _dataAccessTicks, elapsedTicks);
            Counter(method).AddCall(elapsedTicks);
        }

        /// <summary>
        /// Signals that a provider call just happened. A non-null label also updates the
        /// user-visible step text; null means "count this call, leave the text alone" and is
        /// used for cache hits so the progress bar keeps advancing during cached work.
        /// </summary>
        public void ReportActivity(string label) => OnDataAccess?.Invoke(label);

        private MethodCounter Counter(string method) =>
            _counters.GetOrAdd(method, _ => new MethodCounter());

        /// <summary>Most expensive methods first - what to look at when tuning further.</summary>
        public List<FastReportMethodStat> TopMethods(int take = 12) =>
            _counters
                .Select(kv => new FastReportMethodStat
                {
                    Method = kv.Key,
                    Calls = kv.Value.Calls,
                    Hits = kv.Value.Hits,
                    ElapsedMilliseconds = (long)TimeSpan.FromTicks(kv.Value.Ticks).TotalMilliseconds
                })
                .OrderByDescending(z => z.ElapsedMilliseconds)
                .ThenByDescending(z => z.Calls)
                .Take(take)
                .ToList();

        private sealed class MethodCounter
        {
            private int _calls;
            private int _hits;
            private long _ticks;

            public int Calls => Volatile.Read(ref _calls);
            public int Hits => Volatile.Read(ref _hits);
            public long Ticks => Interlocked.Read(ref _ticks);

            public void AddHit()
            {
                Interlocked.Increment(ref _hits);
            }

            public void AddCall(long ticks)
            {
                Interlocked.Increment(ref _calls);
                Interlocked.Add(ref _ticks, ticks);
            }
        }
    }
}
