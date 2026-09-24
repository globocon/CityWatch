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

        [NotMapped]
        public int? PatrolCarId { get; set; }

        [NotMapped]
        public string? PatrolCarName { get; set; }        
        public string? GPScoordinates { get; set; }
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

    public class ClientSiteSmartWandTagsHitLogViewModel
    {        
        public int? Id { get; set; }
        public int LoggedInClientSiteId { get; set; }
        public int? LoggedInUserId { get; set; }
        public int? LoggedInGuardId { get; set; }
        public string? TagUId { get; set; }
        public int? TagsTypeId { get; set; }
        public string LabelDescription { get; set; }
        public int? TagLinkedClientSiteId { get; set; }
        public DateTime? HitUtcDateTime { get; set; }
        public int? SmartWandId { get; set; }
        public DateTime? HitLocalDateTime { get; set; }
        public string? SmartWandNameId { get; set; }
        public ClientSite? LoggedInClientSite { get; set; }        
        public ClientSite? LinkedClientSite { get; set; }
        public SmartWandTagsType? SmartWandTagsType { get; set; }
        public Guard? LoggedInGuard { get; set; }
        public User? LoggedInUser { get; set; }
        public Guid? UniqueRecordId { get; set; }
        public bool? IsOfflineRecord { get; set; }
        public DateTime? OfflineRecordSyncUtcDateTime { get; set; }
        public bool? IsScanFromLinkedSite { get; set; }
        public int? PatrolCarId { get; set; }
        public string? PatrolCarName { get; set; }
        public string? GPScoordinates { get; set; }


        public ClientSiteSmartWandTagsHitLogViewModel()
        {
        }

        public ClientSiteSmartWandTagsHitLogViewModel(ClientSiteSmartWandTagsHitLog z)
        {
            Id = z.Id;
            LoggedInClientSiteId = z.LoggedInClientSiteId;
            LoggedInUserId = z.LoggedInUserId;
            LoggedInGuardId = z.LoggedInGuardId;
            TagUId = z.TagUId;
            TagsTypeId = z.TagsTypeId;
            LabelDescription = z.LabelDescription;
            TagLinkedClientSiteId = z.TagLinkedClientSiteId;
            HitUtcDateTime = z.HitUtcDateTime;
            SmartWandId = z.SmartWandId;
            HitLocalDateTime = z.HitLocalDateTime;
            SmartWandNameId = z.SmartWandNameId;
            LoggedInClientSite = z.LoggedInClientSite;
            LinkedClientSite = z.LinkedClientSite;
            SmartWandTagsType = z.SmartWandTagsType;
            LoggedInGuard = z.LoggedInGuard;
            LoggedInUser = z.LoggedInUser;
            UniqueRecordId = z.UniqueRecordId;
            IsOfflineRecord = z.IsOfflineRecord;
            OfflineRecordSyncUtcDateTime = z.OfflineRecordSyncUtcDateTime;
            IsScanFromLinkedSite = z.IsScanFromLinkedSite;
            PatrolCarId = z.PatrolCarId;
            PatrolCarName = z.PatrolCarName;
            GPScoordinates = z.GPScoordinates;
        }
    }

}
