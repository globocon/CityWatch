using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static Dropbox.Api.Sharing.RequestedLinkAccessLevel;

namespace CityWatch.Data.Models
{
    public class ClientSiteRadioChecksActivityStatus_History
    {
        [Key]
        public int Id { get; set; }
        public int? ClientSiteId { get; set; }


        public int? GuardId { get; set; }


        public DateTime? LastIRCreatedTime { get; set; }


        public DateTime? LastKVCreatedTime { get; set; }


        public DateTime? LastLBCreatedTime { get; set; }

        public DateTime? LastSWCreatedTime { get; set; }



        public DateTime? GuardLoginTime { get; set; }


        public DateTime? GuardLogoutTime { get; set; }
        public DateTime? NotificationCreatedTime { get; set; }
        public DateTime? OnDuty { get; set; }
        public DateTime? OffDuty { get; set; }
        public int? NotificationType { get; set; }
        public int? IRId { get; set; }
        public int? KVId { get; set; }
        public int? LBId { get; set; }
        public int? SWId { get; set; }

        public string ActivityType { get; set; }
        

       

        
        
        public string ActivityDescription { get; set; }
        public  DateTime? GuardLoginTimeLocal { get; set; }
        public DateTimeOffset? GuardLoginTimeLocalWithOffset { get; set; }
        public string GuardLoginTimeZone { get; set; }
        public string GuardLoginTimeZoneShort { get; set; }
        public int? GuardLoginTimeUtcOffsetMinute { get; set; }
        public string CRMSupplier { get; set; }

        public string LogBookNotes { get; set; }

        public string KVNotes { get; set; }

        public string IRNotes { get; set; }

        public string SwNotes { get; set; }

        public string SiteName { get; set; }

        public string GuardName { get; set; }

        public DateTime EventDateTime { get; set; }
        public int? EventDateTimeServerOffsetMinute { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public string Notes { get; set; }

        [JsonPropertyName("Date")]
        public string Date
        {
            get
            {

                return EventDateTime.ToString("dd MMM yyyy");

            }
        }

    }
}
