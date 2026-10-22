using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// The roll-up written when a session or leg closes. Every report, KPI query and export
    /// reads this table; nothing but replay and evidentiary export reads TrackPoint (§8.3).
    /// That rule is what keeps reporting fast when the point table reaches the hundreds of
    /// millions of rows.
    /// </summary>
    [Table("TrackSegment")]
    public class TrackSegment
    {
        [Key]
        public long Id { get; set; }

        public int UnitId { get; set; }

        public Guid SessionId { get; set; }

        /// <summary>Site the leg departed from; null for the first leg of a session.</summary>
        public int? FromSiteId { get; set; }

        /// <summary>Site the leg arrived at; null when the session ended mid-travel.</summary>
        public int? ToSiteId { get; set; }

        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public int DistanceM { get; set; }

        public int DurationSec { get; set; }

        public short? MaxSpeedKph { get; set; }

        public short? AvgSpeedKph { get; set; }

        public int PointCount { get; set; }

        /// <summary>NFC anchor scans inside the leg — the verification currency.</summary>
        public int AnchorScanCount { get; set; }

        /// <summary>Planned-vs-actual adherence, 0–100. Populated in Phase 3.</summary>
        public byte? AdherenceScore { get; set; }

        /// <summary>Aggregate of the leg's point flags (any mock, any implausible, …).</summary>
        public byte Flags { get; set; }
    }
}
