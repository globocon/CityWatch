using Org.BouncyCastle.Asn1.Cms;
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
        [Required]
        public int SettingsId { get; set; }
        [Required]
        public int  PositionId { get; set; }
        [ForeignKey("PositionId")]
        [JsonIgnore]
        public DayOfWeek WeekDay { get; set; }

        [ForeignKey("SettingsId")]
        [JsonIgnore]
       
        public ClientSiteKpiSetting ClientSiteKpiSetting { get; set; }      

        public int? NoOfPatrols { get; set; }

        public string EmpHoursStart { get; set; }

        public string EmpHoursEnd { get; set; }

        public string Type { get; set; }
        [NotMapped]
        public bool DefaultValue { get; set; }
        
        public int? OrderId { get; set; }
    }
}
