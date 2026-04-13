using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class RosterBinder
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

        public virtual ICollection<RosterBinderProject> RosterBinderProjects { get; set; }
    }
}
