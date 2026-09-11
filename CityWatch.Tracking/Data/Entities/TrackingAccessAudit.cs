using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// Who looked at whose location. Every historical read and every live-tracking command
    /// writes a row (§13.4). In a workplace-surveillance dispute, proving who accessed an
    /// officer's movements matters as much as the movements themselves — this table is the
    /// answer to that question, following the FileDownloadAuditLogs precedent.
    /// </summary>
    [Table("TrackingAccessAudit")]
    public class TrackingAccessAudit
    {
        [Key]
        public long Id { get; set; }

        public int UserId { get; set; }

        /// <summary>ViewLive | ViewHistory | CommandLive | CommandCancel | Export | BreakGlass.</summary>
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Unit whose data was accessed; null for fleet-wide views.</summary>
        public int? UnitId { get; set; }

        /// <summary>Start of the window read, for history access.</summary>
        public DateTime? WindowFromUtc { get; set; }

        public DateTime? WindowToUtc { get; set; }

        public DateTime AccessedUtc { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>Required for BreakGlass, optional otherwise.</summary>
        [MaxLength(500)]
        public string? Justification { get; set; }
    }
}
