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
    }
}
