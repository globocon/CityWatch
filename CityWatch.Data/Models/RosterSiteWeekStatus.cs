using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class RosterSiteWeekStatus
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public virtual ClientSite ClientSite { get; set; }

        public DateTime StartDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } // Live, Disputed (editable) / Invoiced, Paid, Cancelled (locked - P9 Issue 65)

        public DateTime UpdatedDate { get; set; }

        [StringLength(255)]
        public string UpdatedBy { get; set; }
    }
}
