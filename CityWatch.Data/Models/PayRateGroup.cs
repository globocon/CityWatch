using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    [Table("PayRateGroup")]
    public class PayRateGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<PayRate> PayRates { get; set; }
        public virtual ICollection<PayRateGroupSite> PayRateGroupSites { get; set; }
    }
}
