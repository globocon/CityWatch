using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public enum LastActivitySource
    {
        DailyGuardLog,

        KeyVehicleLog,

        RadioCheck
    }

    public class ClientSiteActivityStatus
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public int? GuardId { get; set; }

        public bool? Status { get; set; }

        public LastActivitySource? LastActiveSrcId { get; set; }

        public DateTime? LastActiveAt { get; set; }

        public string LastActiveDescription { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }
    }
}
