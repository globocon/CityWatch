using System;
using System.Collections.Generic;
using CityWatch.Tracking.Contracts;

namespace CityWatch.Tracking.Services
{
    /// <summary>
    /// Last-known state per unit. Immutable snapshot; each update replaces the whole entry.
    /// </summary>
    public sealed record UnitLiveState
    {
        public int UnitId { get; init; }
        public Guid SessionId { get; init; }
        public decimal Lat { get; init; }
        public decimal Lon { get; init; }
        public short? SpeedKph { get; init; }

        /// <summary>True when SpeedKph was computed from consecutive fixes because the
        /// device sent none. Shown as approximate ("~42 km/h") — derived is never dressed
        /// up as measured (§Phase 2.3).</summary>
        public bool SpeedDerived { get; init; }

        public short? HeadingDeg { get; init; }
        public short? AccuracyM { get; init; }
        public byte? BatteryPct { get; init; }
        public TrackingMode Mode { get; init; }
        public TrackPointSource Source { get; init; }
        public TrackPointFlags Flags { get; init; }

        /// <summary>Device clock of the fix.</summary>
        public DateTime RecordedUtc { get; init; }

        /// <summary>Server clock when the fix arrived. Staleness is computed from this,
        /// never from the device clock (§11.3 rule 2).</summary>
        public DateTime ReceivedUtc { get; init; }

        /// <summary>Monotonic store version at the time of this update; the diff cursor.</summary>
        public long Version { get; init; }
    }

    /// <summary>
    /// The live picture the control room reads and the broadcast ticker diffs against.
    /// In-memory in Phase 1; the same interface moves to Redis in Phase 2 (D11) — callers
    /// never learn which they are talking to.
    /// </summary>
    public interface ILiveStateStore
    {
        /// <summary>Record a newer fix for a unit. Out-of-order fixes are ignored:
        /// a backfilled point must never overwrite a fresher live one.</summary>
        void Update(UnitLiveState state);

        UnitLiveState? Get(int unitId);

        /// <summary>Everything currently known. Snapshot semantics — safe to enumerate.</summary>
        IReadOnlyList<UnitLiveState> Snapshot();

        /// <summary>
        /// Units whose state changed after <paramref name="sinceVersion"/>, and the store's
        /// current version to use as the next cursor. This is what makes the 1 Hz broadcast a
        /// diff (O(changed)) rather than a full frame (O(all)) — the §10.3 scaling decision.
        /// </summary>
        (IReadOnlyList<UnitLiveState> Changed, long Version) ChangedSince(long sinceVersion);

        /// <summary>Forget a unit (session ended). The map should not show off-shift vehicles.</summary>
        void Remove(int unitId);
    }
}
