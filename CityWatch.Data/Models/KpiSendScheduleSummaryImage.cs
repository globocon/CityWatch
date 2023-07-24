using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class KpiSendScheduleSummaryImage
    {
        [Key]
        public int Id { get; set; }

        public int ScheduleId { get; set; }       

        public string FileName { get; set; }

        public DateTime LastUpdated { get; set; }

        [ForeignKey("ScheduleId")]
        [JsonIgnore]
        public KpiSendSchedule KpiSendSchedule { get; set; }
    }
}
