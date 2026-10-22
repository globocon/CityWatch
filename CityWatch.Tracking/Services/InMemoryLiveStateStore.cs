using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CityWatch.Tracking.Services
{
    /// <summary>
    /// Phase 1 live state store. A concurrent dictionary and a monotonic version counter —
    /// deliberately boring. At 1,000 units this is ~200 KB; the interesting property is the
    /// version cursor, which the broadcast ticker uses to send only what changed.
    /// </summary>
    public sealed class InMemoryLiveStateStore : ILiveStateStore
    {
        private readonly ConcurrentDictionary<int, UnitLiveState> _units = new();
        private long _version;

        public void Update(UnitLiveState state)
        {
            if (state == null)
                return;

            var stamped = state with { Version = Interlocked.Increment(ref _version) };

            _units.AddOrUpdate(
                state.UnitId,
                stamped,
                (_, existing) =>
                {
                    /* Reject regressions: an offline backfill replaying an hour of history must
                       not drag the live marker backwards. Newer RecordedUtc wins; on a tie the
                       later arrival wins (it carries the newer server view). */
                    if (stamped.RecordedUtc < existing.RecordedUtc)
                        return existing;
                    return stamped;
                });
        }

        public UnitLiveState? Get(int unitId)
            => _units.TryGetValue(unitId, out var state) ? state : null;

        public IReadOnlyList<UnitLiveState> Snapshot()
            => _units.Values.ToList();

        public (IReadOnlyList<UnitLiveState> Changed, long Version) ChangedSince(long sinceVersion)
        {
            /* Read the version first: anything that updates between these two lines appears in
               this diff AND has Version <= the returned cursor, so the next diff repeats it
               rather than missing it. Duplicates are harmless (last-write-wins on the client);
               gaps are not. */
            var current = Interlocked.Read(ref _version);
            var changed = _units.Values.Where(u => u.Version > sinceVersion).ToList();
            return (changed, current);
        }

        public void Remove(int unitId)
        {
            _units.TryRemove(unitId, out _);
            /* Bump the version so pollers holding a cursor learn something changed; the removal
               itself is communicated by the unit's absence from the next snapshot. */
            Interlocked.Increment(ref _version);
        }
    }
}
