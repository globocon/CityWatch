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
    public class HrSettingsClientSites
    {
        [Key]
        public int Id { get; set; } 

        public int HrSettingsId { get; set; }
        
        public int ClientSiteId { get; set; }

        [ForeignKey("HrSettingsId")]
        [JsonIgnore]
        public HrSettings Schedule { get; set; }

        [ForeignKey("ClientSiteId")]        
        public ClientSite ClientSite { get; set; }
    }
}
