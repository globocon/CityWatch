using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    [Table("Allowances")]
    public class Allowance
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Description { get; set; }
        
        [StringLength(50)]
        public string FQ { get; set; } // Per hr, Per shift, Per day, Per week, Per Km
        
        public decimal SellRateToClient { get; set; }
        public decimal Comms1 { get; set; }
        public decimal Comms2 { get; set; }
        public decimal GuardPayRate { get; set; }
        
        [StringLength(10)]
        public string Currency { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedDate { get; set; }
    }
}
