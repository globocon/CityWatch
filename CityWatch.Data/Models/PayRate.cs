using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class PayRate
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }
        public int? PayRateGroupId { get; set; }
        public decimal SellRateToClient { get; set; }
        public decimal Comms1 { get; set; }
        public decimal Comms2 { get; set; }
        public decimal GuardPayRate { get; set; }
        public string Currency { get; set; }
        public bool IsDeleted { get; set; }

        public virtual PayRateGroup PayRateGroup { get; set; }
    }
}
