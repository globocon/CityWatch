using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class RosterBinderProject
    {
        [Key]
        public int Id { get; set; }

        public int RosterBinderId { get; set; }
        public int RosterGroupId { get; set; }
        public int SortOrder { get; set; }

        public virtual RosterBinder RosterBinder { get; set; }
        public virtual RosterGroup RosterGroup { get; set; }
    }
}
