using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class PostActivityRequestLocalCacheOfflineNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }
        public int Id { get; set; }
        public int guardId { get; set; }
        public int clientsiteId { get; set; }
        public int userId { get; set; }
        public string activityString { get; set; }
        public string gps { get; set; }
        public bool systemEntry { get; set; } = true;
        public int scanningType { get; set; } = 0;
        public string tagUID { get; set; } = "NA";
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsNewGuard { get; set; } = false;
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public DateTime SyncTime { get; set; } = DateTime.Now;
        public string NotSyncError { get; set; }
        public int? LogbookclientsiteId { get; set; }
        public bool IsEntryByPCAR { get; set; } = false;
        public int? CallSignId { get; set; }
        public int? PositionId { get; set; }

    }
}
