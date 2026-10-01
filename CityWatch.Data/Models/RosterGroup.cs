using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace CityWatch.Data.Models
{
    public class RosterGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public bool IsDeleted { get; set; }
        
        [StringLength(255)]
        public string CoverFileName { get; set; }
        public DateTime? CoverFileDate { get; set; }

        public string AlertEmailRecipients { get; set; }
        public bool AlertOnRejectedShift { get; set; }
        public bool AlertOnReliefGuard { get; set; }

        public virtual ICollection<RosterGroupSite> RosterGroupSites { get; set; }
        public virtual ICollection<RosterSchedule> RosterSchedules { get; set; }
    }
}
