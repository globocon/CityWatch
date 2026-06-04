using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ShiftCancellationEmailQueue
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }
        
        [ForeignKey("GuardId")]
        public virtual Guard Guard { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public virtual ClientSite ClientSite { get; set; }

        public DateTime ShiftStart { get; set; }

        public DateTime ShiftEnd { get; set; }

        public string Reason { get; set; }

        [StringLength(100)]
        public string CancelledBy { get; set; } // e.g. "Guard" or admin name

        [StringLength(50)]
        public string Source { get; set; } // "Web" or "Mobile"

        public DateTime CreatedAt { get; set; }

        public bool IsProcessed { get; set; }
    }
}
