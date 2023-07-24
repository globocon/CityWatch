using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class ClientSiteDayKpiSetting
    {
        [Key]
        public int Id { get; set; }

        public int SettingsId { get; set; }

        public DayOfWeek WeekDay { get; set; }

        [ForeignKey("SettingsId")]
        [JsonIgnore]
        public ClientSiteKpiSetting ClientSiteKpiSetting { get; set; }

        public int PatrolFrequency { get; set; }

        public int? NoOfPatrols { get; set; }

        public decimal? EmpHours { get; set; }

        public decimal? ImagesTarget { get; set; }

        public decimal? WandScansTarget { get; set; }
    }
}
