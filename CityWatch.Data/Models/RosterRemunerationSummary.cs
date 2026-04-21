using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class RosterRemunerationSummary
    {
        [Key]
        public int Id { get; set; }

        public DateTime WeekStartDate { get; set; }

        public int GuardId { get; set; }

        [ForeignKey("GuardId")]
        public virtual Guard Guard { get; set; }

        public bool IsPaid { get; set; }

        public string Notes { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }
    }
}
