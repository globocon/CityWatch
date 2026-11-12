using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    [Table("PayRateGroupSite")]
    public class PayRateGroupSite
    {
        [Key]
        public int Id { get; set; }

        public int PayRateGroupId { get; set; }
        public int ClientSiteId { get; set; }

        [ForeignKey("PayRateGroupId")]
        public virtual PayRateGroup PayRateGroup { get; set; }

        [ForeignKey("ClientSiteId")]
        public virtual ClientSite ClientSite { get; set; }
    }
}
