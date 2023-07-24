using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class KpiScheduleRun
    {
        [Key]
        public int Id { get; set; }

        public int ScheduleId { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? CompletedOn { get; set; }

        public bool Success { get; set; }

        public string StatusMessage { get; set; }

        [ForeignKey("ScheduleId")]
        public KpiSendSchedule SendSchedule { get; set; }
    }
}
