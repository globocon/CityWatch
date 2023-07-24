using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteSmartWand
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public string SmartWandId { get; set; }

        public string PhoneNumber { get; set; }

        [NotMapped]
        public bool IsInUse { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
