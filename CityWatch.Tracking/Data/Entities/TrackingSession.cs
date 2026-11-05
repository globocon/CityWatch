using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// One officer + one unit + one shift. The privacy boundary: no open session, no tracking,
    /// enforced at ingest (§6.5, §13.5). Closing a session is an audited event and a hard stop.
    /// </summary>
    [Table("TrackingSession")]
    public class TrackingSession
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>ClientSiteSmartWand.Id.</summary>
        public int UnitId { get; set; }

        public int GuardId { get; set; }

        /// <summary>Site the session was opened against.</summary>
        public int ClientSiteId { get; set; }

        /// <summary>PcarRoute.Id when the patrol runs a planned route; null for ad-hoc.</summary>
        public int? PcarRouteId { get; set; }

        public DateTime StartedUtc { get; set; }

        public DateTime? EndedUtc { get; set; }

        /// <summary>Active | Completed | Cancelled | Expired — Expired means the reaper closed
        /// it after the configured no-fix window, which is itself worth knowing.</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>How the session ended: OfficerLogout, PatrolEnded, OperatorClosed, Reaper.</summary>
        [MaxLength(30)]
        public string? EndReason { get; set; }

        /// <summary>Rolling last-fix marker maintained by ingest; what the reaper reads.</summary>
        public DateTime? LastFixUtc { get; set; }

        /// <summary>
        /// The guard's own "Mobile Patrol Car" toggle from the login screen. This is the
        /// authority for the map symbol: the same wand can be in a car today and on foot
        /// tomorrow, so a per-shift declaration beats the wand's static PatrolCarId.
        /// Null for sessions opened before this was captured — the wand is then the fallback.
        /// </summary>
        public bool? IsPatrolCar { get; set; }

        /// <summary>
        /// Callsign chosen at login ("Romeo 1"). What operators actually say on the radio,
        /// so it is what the marker is labelled with when present.
        /// </summary>
        [MaxLength(50)]
        public string? Callsign { get; set; }

        /// <summary>
        /// THE CAR. Position chosen at login — "Mobile Patrols (Car) M1", "M2", "M3"…
        /// This is the tracked unit's real identity: several cars from the same fleet roam
        /// the same sites at once and are told apart by Position, never by the tags they
        /// scan (they all scan the same site tags).
        /// </summary>
        public int? PatrolCarPositionId { get; set; }

        [MaxLength(120)]
        public string? PatrolCarPositionName { get; set; }

        /// <summary>
        /// Site the car is currently at, set by the most recent site-tag scan and cleared
        /// when the in-car tag is scanned. Null means travelling.
        /// </summary>
        public int? CurrentSiteId { get; set; }

        [MaxLength(200)]
        public string? CurrentSiteName { get; set; }

        /// <summary>
        /// AtSite | Transit. Derived from scans and used for display and leg boundaries —
        /// it does NOT gate GPS. Tracking stays continuous so a missed scan can never
        /// lose a journey; the scans annotate the trail rather than switching it on and off.
        /// </summary>
        [MaxLength(20)]
        public string TravelState { get; set; } = "Transit";

        /// <summary>When the current TravelState began — drives "at site 12 min" on the map.</summary>
        public DateTime? TravelStateSinceUtc { get; set; }
    }
}
