using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class ClientSiteKpiNote
    {
        [Key]
        public int Id { get; set; }

        public int SettingsId { get; set; }

        public DateTime ForMonth { get; set; }

        public string Notes { get; set; }
        public string HRRecords { get; set; }

        [ForeignKey("SettingsId")]
        [JsonIgnore]
        public ClientSiteKpiSetting ClientSiteKpiSetting { get; set; }
    }
}

