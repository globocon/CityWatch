using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class KpiSendCustomWandClientSites
    {
        [Key]
        public int Id { get; set; }

        public int CustomWandScheduleId { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("CustomWandScheduleId")]
        [JsonIgnore]
        public KpiSendCustomWandSchedules Schedule { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
