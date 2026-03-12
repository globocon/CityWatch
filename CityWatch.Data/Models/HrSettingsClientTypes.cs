using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class HrSettingsClientTypes
    {
        [Key]
        public int Id { get; set; } 

        public int HrSettingsId { get; set; }
        
        public int ClientTypeId { get; set; }

        [ForeignKey("HrSettingsId")]
        [JsonIgnore]
        public HrSettings Schedule { get; set; }

        [ForeignKey("ClientTypeId")]
        public ClientType ClientType { get; set; }
    }
}
