using System;
using System.Collections.Generic;

namespace CityWatch.Tracking.Contracts
{
    /// <summary>
    /// One position fix as it travels from any source to the ingest pipeline.
    /// This is the wire shape from §9.1 — changes here are contract changes.
    /// </summary>
    public sealed class PositionPoint
    {
        /// <summary>Device-assigned monotonic sequence; the dedupe key with unit + session.</summary>
        public int Seq { get; set; }

        /// <summary>Device clock, UTC.</summary>
        public DateTime Utc { get; set; }

        public decimal Lat { get; set; }

        public decimal Lon { get; set; }

        public double? AccuracyM { get; set; }

        public double? SpeedKph { get; set; }

        public double? HeadingDeg { get; set; }

        public byte? BatteryPct { get; set; }

        public bool IsMock { get; set; }

        /// <summary>nfcAnchor | transit | live | duress — parsed to <see cref="TrackPointSource"/>.</summary>
        public string Source { get; set; } = "transit";

        /// <summary>Set when Source is nfcAnchor.</summary>
        public string? TagUid { get; set; }

        /// <summary>True when replayed from the device's offline cache.</summary>
        public bool Backfilled { get; set; }
    }

    /// <summary>A batch of points from one unit's session.</summary>
    public sealed class PositionBatch
    {
        /// <summary>ClientSiteSmartWand.Id — the tracking unit key (D3).</summary>
        public int UnitId { get; set; }

        public Guid SessionId { get; set; }

        /// <summary>Device clock at send time; with the server clock this measures skew.</summary>
        public DateTime DeviceUtc { get; set; }

        /// <summary>Last mode command sequence the device has applied (§5.3).</summary>
        public int CommandSeqSeen { get; set; }

        public List<PositionPoint> Points { get; set; } = new();
    }

    /// <summary>
    /// The ingest response. Carries the authoritative desired mode: push notifications only
    /// accelerate delivery, this is what guarantees it (§5.3, D5).
    /// </summary>
    public sealed class IngestResponse
    {
        public int Accepted { get; set; }

        public int Rejected { get; set; }

        public TrackingMode DesiredMode { get; set; } = TrackingMode.Normal;

        public int CommandSeq { get; set; }

        public int? CommandTtlSeconds { get; set; }

        /// <summary>Current sampling policy, so threshold tuning never needs an app release.</summary>
        public Configuration.TrackingOptions.SamplingPolicyOptions? Policy { get; set; }

        /// <summary>Server clock for skew reconciliation on the device.</summary>
        public DateTime ServerUtc { get; set; }
    }

    /// <summary>
    /// Device-agnostic ingest seam (D16 in v1 numbering; §9.2). Phase 1 has one implementation
    /// (the phone, via the ingest controller). Phase 3 adds telematics and third-party fleet
    /// adapters behind the same contract, which is what turns a customer's existing Geotab or
    /// Samsara fleet from a competitor into a data source.
    /// </summary>
    public interface IPositionSource
    {
        /// <summary>Stable identifier stored with each point's provenance ("phone", "geotab", …).</summary>
        string SourceName { get; }
    }
}
