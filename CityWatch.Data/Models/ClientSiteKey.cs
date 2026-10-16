using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteKey
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public string KeyNo { get; set; }

        public string Description { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
        //p2-140 key photos  -start
        public string ImagePath { get; set; }
        [NotMapped]
        public string ImagePathNew { get; set; }
        //p2-140 key photos  -end
    }
}
