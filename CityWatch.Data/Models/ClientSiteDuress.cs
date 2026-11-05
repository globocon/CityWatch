using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteDuress
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public bool IsEnabled { get; set; }

        public int EnabledBy { get; set; }

        public DateTime EnabledDate { get; set; }
        public string GpsCoordinates { get; set; }
        
        public string EnabledAddress { get; set; }

        public bool? PlayDuressAlarm { get; set; }

        public DateTime? EnabledDateTimeLocal { get; set; }
        public DateTimeOffset? EnabledDateTimeLocalWithOffset { get; set; }
        public string EnabledDateTimeZone { get; set; }
        public string EnabledDateTimeZoneShort { get; set; }
        public int? EnabledDateTimeUtcOffsetMinute { get; set; }
        public int LinkedDuressParentSiteId { get; set; }
        public int IsLinkedDuressParentSite { get; set; }

    }
}
