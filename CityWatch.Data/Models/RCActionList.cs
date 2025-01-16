using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class RCActionList
    {
        [Key]
        public int Id { get; set; }
        public string SiteAlarmKeypadCode { get; set; }
        public string Action1 { get; set; }
        public string Sitephysicalkey { get; set; }
        public string Action2 { get; set; }
        public string SiteCombinationLook { get; set; }
        public string Action3 { get; set; }
        public string Action4 { get; set; }
        public string Action5 { get; set; }
        public string ControlRoomOperator { get; set; }
        public int SettingsId { get; set; }
        public string Imagepath { get; set; }
        public string DateandTimeUpdated { get; set; }
        public int ClientSiteID { get; set; }
        public bool IsRCBypass { get; set; }

        [ForeignKey("SettingsId")]
        [JsonIgnore]
        public ClientSiteKpiSetting ClientSiteKpiSetting { get; set; }
        [NotMapped]
        public string SOPFileNme { get; set; }
        [NotMapped]
        public List<string> SOPAlarmFileNme { get; set; }
    }
}
