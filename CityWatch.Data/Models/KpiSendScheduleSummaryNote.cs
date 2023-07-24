using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class KpiSendScheduleSummaryNote
    {
        [Key]
        public int Id { get; set; }

        public int ScheduleId { get; set; }

        public DateTime ForMonth { get; set; }

        public string Notes { get; set; }

        [ForeignKey("ScheduleId")]
        [JsonIgnore]
        public KpiSendSchedule kpiSendSchedule { get; set; }
    }
}
