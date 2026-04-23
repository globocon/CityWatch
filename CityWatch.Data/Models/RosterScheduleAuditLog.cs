using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class RosterScheduleAuditLog
    {
        [Key]
        public int Id { get; set; }

        public int RosterScheduleId { get; set; }

        public DateTime ActionTime { get; set; }

        public int? UserId { get; set; } // ApplicationUserId for Web actions

        public int? GuardId { get; set; } // GuardId for Mobile actions

        [StringLength(20)]
        public string ActionSource { get; set; } // e.g., "Web" or "Mobile"

        [StringLength(50)]
        public string Action { get; set; } // e.g., "Created", "Edited", "Accepted", "Declined", "Deleted"

        public string Details { get; set; } // e.g., "Guard changed from Bruno to Tim", or raw JSON

        [StringLength(50)]
        public string IPAddress { get; set; }

        public string Platform { get; set; } // e.g., "Mobile (Android)", "Web (Chrome)"

        public int? OldStatus { get; set; }

        public int? NewStatus { get; set; }

        [ForeignKey("RosterScheduleId")]
        public virtual RosterSchedule RosterSchedule { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("GuardId")]
        public virtual Guard Guard { get; set; }
    }
}
