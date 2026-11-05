using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// A server-side mode command (Live/Duress escalation or cancellation). The row is the
    /// authority: the device learns the desired mode from every ingest response, with silent
    /// push only as an accelerator (§5.3, D5). Doubles as the audit trail of who put which
    /// unit under close surveillance, for how long.
    /// </summary>
    [Table("TrackingModeCommand")]
    public class TrackingModeCommand
    {
        [Key]
        public int Id { get; set; }

        public int UnitId { get; set; }

        /// <summary>Monotonic per unit. The device applies only newer commands, making
        /// out-of-order delivery safe.</summary>
        public int CommandSeq { get; set; }

        /// <summary>Contracts.TrackingMode being requested.</summary>
        public byte DesiredMode { get; set; }

        /// <summary>User.Id of the operator; null for system-issued commands (TTL expiry,
        /// duress escalation).</summary>
        public int? IssuedByUserId { get; set; }

        public DateTime IssuedUtc { get; set; }

        /// <summary>Hard expiry for Live Mode. Null only for Duress, which never times out.</summary>
        public DateTime? ExpiresUtc { get; set; }

        /// <summary>Set when the device confirms it applied the command; until then the UI
        /// shows "requested", never the target state (§11.3 rule 5).</summary>
        public DateTime? AcknowledgedUtc { get; set; }

        /// <summary>Pending | Active | Expired | Cancelled | Superseded.</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(30)]
        public string? EndReason { get; set; }
    }
}
