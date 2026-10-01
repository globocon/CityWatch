using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public  class KpiSendKVClientSites
    {
        [Key]
        public int Id { get; set; }

        public int KVScheduleId { get; set; }

        public int ClientSiteId { get; set; }

        [ForeignKey("KVScheduleId")]
        [JsonIgnore]
        public KpiSendKVSchedules Schedule { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
