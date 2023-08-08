using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class ClientSiteManningKpiSetting
    {
        [Key]
        public int Id { get; set; }

        public int SettingsId { get; set; }
        
        public int  PositionId { get; set; }
        [ForeignKey("PositionId")]
        [JsonIgnore]
        public DayOfWeek WeekDay { get; set; }

        [ForeignKey("SettingsId")]
        [JsonIgnore]
       
        public ClientSiteKpiSetting ClientSiteKpiSetting { get; set; }      

        public int? NoOfPatrols { get; set; }

        public decimal? EmpHoursStart { get; set; }

        public decimal? EmpHoursEnd { get; set; }

        public string Type { get; set; }
        [NotMapped]
        public bool DefaultValue { get; set; }
    }
}
