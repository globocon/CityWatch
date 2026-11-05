using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// One position fix. Append-only, narrow, and deliberately without foreign keys (D7):
    /// FK checks cost on high-rate inserts, a deleted wand must not cascade into evidentiary
    /// history, and an FK would couple the pack's schema to a stable table. Referential
    /// integrity is enforced at ingest (the unit must be enrolled) and on read.
    ///
    /// The hot write path does NOT go through this entity — PositionWriter uses SqlBulkCopy.
    /// This mapping exists for replay/history queries and for tests.
    /// </summary>
    [Table("TrackPoint")]
    public class TrackPoint
    {
        [Key]
        public long Id { get; set; }

        /// <summary>ClientSiteSmartWand.Id — logical reference only (D3, D7).</summary>
        public int UnitId { get; set; }

        public Guid SessionId { get; set; }

        /// <summary>Device-assigned sequence; (UnitId, SessionId, Seq) is the dedupe key.</summary>
        public int Seq { get; set; }

        /// <summary>Device clock, UTC.</summary>
        public DateTime RecordedUtc { get; set; }

        /// <summary>Server clock, UTC. Staleness and skew both derive from the pair.</summary>
        public DateTime ReceivedUtc { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal Longitude { get; set; }

        public short? SpeedKph { get; set; }

        public short? HeadingDeg { get; set; }

        public short? AccuracyM { get; set; }

        public byte? BatteryPct { get; set; }

        /// <summary>Contracts.TrackPointSource — NfcAnchor points are the trust anchors.</summary>
        public byte SourceType { get; set; }

        /// <summary>Contracts.TrackingMode at the moment of capture.</summary>
        public byte ModeAtCapture { get; set; }

        /// <summary>Contracts.TrackPointFlags — mock / low-accuracy / implausible / backfilled.</summary>
        public byte Flags { get; set; }

        /// <summary>NFC tag UID when SourceType is NfcAnchor; the reconciliation key for
        /// Verified Proof of Patrol (Phase 3).</summary>
        [MaxLength(64)]
        public string? AnchorTagUid { get; set; }
    }
}
