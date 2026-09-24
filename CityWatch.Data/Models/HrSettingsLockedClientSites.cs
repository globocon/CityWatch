using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class HrSettingsLockedClientSites
    {
        [Key]
        public int Id { get; set; }

        public int HrSettingsId { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("HrSettingsId")]
        public HrSettings HrSettings { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
