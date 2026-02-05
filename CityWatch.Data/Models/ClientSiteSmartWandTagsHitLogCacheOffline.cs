using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }
        public int Id { get; set; }
        public int LoggedInClientSiteId { get; set; }
        public int LoggedInUserId { get; set; }
        public int LoggedInGuardId { get; set; }
        public string TagUId { get; set; }
        public int TagsTypeId { get; set; }
        public DateTime HitUtcDateTime { get; set; }
        public DateTime HitLocalDateTime { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public int? SmartWandId { get; set; }
        public string GPScoordinates { get; set; }
        public bool IsSynced { get; set; }
        public Guid UniqueRecordId { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }        
        public DateTime SyncTime { get; set; } = DateTime.Now;
        public string NotSyncError { get; set; }
        public bool IsScanFromLinkedSite { get; set; } = false;
    }
}
