using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CityWatch.Data.Models
{
    public class CustomFieldLogRequestHeadLocalCacheOfflineNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }
        public int Id { get; set; }
        public int SiteId { get; set; }
        public List<CustomFieldLogRequestDetailCacheOfflineNotSynced> Details { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public DateTime SyncTime { get; set; } = DateTime.Now;
        public string NotSyncError { get; set; }

    }

    public class CustomFieldLogRequestDetailCacheOfflineNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }        
        public int SyncIdHeadRef { get; set; }
        public int Id { get; set; }
        public int HeadId { get; set; }
        public string DictKey { get; set; }
        public string DictValue { get; set; }
        // Navigation property
        [ForeignKey(nameof(SyncIdHeadRef))]
        public CustomFieldLogRequestHeadLocalCacheOfflineNotSynced Head { get; set; }
    }

}
