using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public virtual ICollection<RosterGroupSite> RosterGroupSites { get; set; }
        public virtual ICollection<RosterSchedule> RosterSchedules { get; set; }
    }
}
