using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CityWatch.Data.Models
{
    public class irOfflineCacheNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }
        public string IrId { get; set; }
        public string IncidentRequest { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; }
        public int guardId { get; set; }
        public int clientsiteId { get; set; }
        public int userId { get; set; }
        public string gps { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }

        public DateTime SyncTime { get; set; } = DateTime.Now;
        public string NotSyncError { get; set; }

    }

    public class irOfflineFilesAttachmentsCacheNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }
        public Guid UniqueRecordId { get; set; }
        public string IrId { get; set; }
        public string FileNameActual { get; set; }
        public string FileNameCache { get; set; }
        public string FileNameWithPathCache { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public int guardId { get; set; }
        public int clientsiteId { get; set; }
        public int userId { get; set; }
        public string gps { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string ServerFileNameWithPath { get; set; }
        public DateTime SyncTime { get; set; } = DateTime.Now;
        public string NotSyncError { get; set; }

    }

}
