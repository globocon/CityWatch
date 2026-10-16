using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Web.Models
{
    public class CustomFieldLogRequestHeadLocalCacheOffline
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public List<CustomFieldLogRequestDetailCache> Details { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }

    }

    public class CustomFieldLogRequestDetailCache
    {        
        public int Id { get; set; }
        public int HeadId { get; set; }
        public string DictKey { get; set; }
        public string DictValue { get; set; }
        // Navigation back-reference
        //public CustomFieldLogRequestHeadLocalCacheOffline Head { get; set; }
    }

}
