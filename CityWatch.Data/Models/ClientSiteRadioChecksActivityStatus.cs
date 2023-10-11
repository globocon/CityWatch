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
        
        public DateTime? LastIRCreatedTime { get; set; }
        public DateTime? LastKVCreatedTime { get; set; }
        public DateTime? LastLBCreatedTime { get; set; }
        public DateTime? GuardLoginTime { get; set; }
        public DateTime? GuardLogoutTime { get; set; }
        public string IRDescription { get; set; }
        public string KVDescription { get; set; }
        public string LBDescription { get; set; }
        
        public string ActivityType { get; set; }
        [ForeignKey("GuardId")]
        public Guard Guard { get; set; }

        [ForeignKey("ClientSiteId")]
        public ClientSite ClientSite { get; set; }
    }
}
