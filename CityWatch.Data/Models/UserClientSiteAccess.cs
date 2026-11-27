using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class UserClientSiteAccess
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ClientSiteId { get; set; }
        public int? ThirdPartyID { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
