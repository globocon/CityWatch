using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class RosterGroupSite
    {
        [Key]
        public int Id { get; set; }

        public int RosterGroupId { get; set; }

        [ForeignKey("RosterGroupId")]
        public virtual RosterGroup RosterGroup { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("ClientSiteId")]
        public virtual ClientSite ClientSite { get; set; }
    }
}
