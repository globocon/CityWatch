using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardRcClientSiteAccess
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        public int ClientSiteId { get; set; }
        

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
