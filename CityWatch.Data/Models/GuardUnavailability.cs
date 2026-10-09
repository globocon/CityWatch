using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class GuardUnavailability
    {
        [Key]
        public int Id { get; set; }

        public int GuardId { get; set; }

        public string Reason { get; set; }

        public string ReasonOther { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        [ForeignKey("GuardId")]
        public virtual Guard Guard { get; set; }
    }
}
