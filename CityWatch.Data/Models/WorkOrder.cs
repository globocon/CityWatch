using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace CityWatch.Data.Models
{

    public class WorkOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string WorkOrderNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string EngLine { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal EngStart { get; set; }

        [Required]
        [MaxLength(50)]
        public string EngLeg { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal EngEnd { get; set; }

        [Required]
        [MaxLength(255)]
        public string EngDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
