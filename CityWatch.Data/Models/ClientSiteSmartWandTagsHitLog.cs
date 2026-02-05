using Microsoft.VisualBasic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class ClientSiteSmartWandTagsHitLog
    {
        [Key]
        public int Id { get; set; }
        public int LoggedInClientSiteId { get; set; }
        public int LoggedInUserId { get; set; }
        public int LoggedInGuardId { get; set; }
        public string TagUId { get; set; }
        public int? TagsTypeId { get; set; }
        public string LabelDescription { get; set; }
        public int? TagLinkedClientSiteId { get; set; }
        public DateTime HitUtcDateTime { get; set; } =  DateTime.UtcNow;
        public int? SmartWandId { get; set; }

        [NotMapped]
        public DateTime HitLocalDateTime { get; set; }

        [NotMapped]
        public string? SmartWandNameId { get; set; }

        [ForeignKey("LoggedInClientSiteId")]
        public ClientSite LoggedInClientSite { get; set; }
        [ForeignKey("TagLinkedClientSiteId")]
        public ClientSite LinkedClientSite { get; set; }
        [ForeignKey("TagsTypeId")]
        public SmartWandTagsType SmartWandTagsType { get; set; }     
        [ForeignKey("LoggedInGuardId")]
        public Guard LoggedInGuard { get; set; }
        [ForeignKey("LoggedInUserId")]
        public User LoggedInUser { get; set; }
        public Guid? UniqueRecordId { get; set; }
        public bool IsOfflineRecord { get; set; } = false;
        public DateTime? OfflineRecordSyncUtcDateTime { get; set; }
        public bool IsScanFromLinkedSite { get; set; } = false;
    }

    public class ClientSiteSmartWandTagsHitLogCacheOffline
    {        
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
        public bool IsScanFromLinkedSite { get; set; }
    }

}
