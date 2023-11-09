using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CityWatch.Data.Models
{
    public class ClientSiteRadioChecksActivityStatus
    {
        [Key]
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
       

        public int GuardId { get; set; }

        
        public Nullable <DateTime> LastIRCreatedTime { get; set; }

        
        public Nullable <DateTime> LastKVCreatedTime { get; set; }

        
        public Nullable <DateTime> LastLBCreatedTime { get; set; }

        
        public Nullable <DateTime> GuardLoginTime { get; set; }

        
        public Nullable <DateTime> GuardLogoutTime { get; set; }
        public Nullable<DateTime> NotificationCreatedTime { get; set; }
        public Nullable<DateTime> OnDuty { get; set; }
        public Nullable<DateTime> OffDuty { get; set; }
        public int? NotificationType { get; set; }
        public int? IRId { get; set; }
        public int? KVId { get; set; }
        public int? LBId { get; set; }
        
        public string ActivityType { get; set; }
        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }

        [ForeignKey("IRId")]
        public IncidentReport IncidentReport { get; set; }
        [ForeignKey("LBId")]
        public GuardLog GuardLog { get; set; }
        [ForeignKey("KVId")]
        public KeyVehicleLog KeyVehicleLog { get; set; }
    }
}
