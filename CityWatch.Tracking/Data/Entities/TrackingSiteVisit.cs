using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// One stay at one client site. Created from GPS the moment a unit crosses into a site's
    /// radius, but only <see cref="ConfirmedUtc"/> makes it real: a car that drives past on
    /// the main road creates a row and never confirms it, so the control room is never told
    /// about an arrival that did not happen.
    /// </summary>
    /// <remarks>
    /// Mirrors DbScript/370. This is the durable half of the bell: alerts that survive a
    /// refresh, that every operator sees the same way, and that are recorded even when no
    /// browser is open.
    /// </remarks>
    [Table("TrackingSiteVisit")]
    public class TrackingSiteVisit
    {
        [Key]
        public int Id { get; set; }

        public int UnitId { get; set; }

        public Guid SessionId { get; set; }

        /// <summary>ClientSites.Id.</summary>
        public int SiteId { get; set; }

        /// <summary>
        /// Denormalised on purpose: the bell must keep showing the name the operator saw,
        /// even after the site is renamed or deactivated.
        /// </summary>
        [MaxLength(200)]
        public string SiteName { get; set; } = string.Empty;

        /// <summary>First fix inside the radius — the honest arrival time, not the time the
        /// dwell window happened to elapse.</summary>
        public DateTime EnteredUtc { get; set; }

        /// <summary>Null while this is still a candidate. Set once the unit is still inside
        /// after the dwell window (or immediately for an NFC scan). Only confirmed visits
        /// are ever shown.</summary>
        public DateTime? ConfirmedUtc { get; set; }

        /// <summary>Null means the unit is still there.</summary>
        public DateTime? ExitedUtc { get; set; }

        /// <summary>Gps | Nfc — how the arrival was established.</summary>
        [MaxLength(10)]
        public string Source { get; set; } = "Gps";

        public decimal? EnteredLat { get; set; }

        public decimal? EnteredLon { get; set; }

        /// <summary>Distance of the confirming fix from the site centre. Kept because it is
        /// the measure of how much this detection is worth.</summary>
        public int? DistanceM { get; set; }
    }
}
