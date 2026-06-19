using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CityWatch.Data.Models
{
    public class OfflineFilesRecordsNotSynced
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SyncId { get; set; }
        public int Id { get; set; }
        public string RecordLabel { get; set; }
        public string FileNameActual { get; set; }
        public string FileNameCache { get; set; }
        public string FileNameWithPathCache { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; }
        public string FileType { get; set; }  // rear / twentyfive / etc
        public bool IsNew { get; set; }   // true → newly added via picker
        public int? LogBookId { get; set; }  // null for new files, set for existing files from DB        
        public int guardId { get; set; }
        public int clientsiteId { get; set; }
        public int userId { get; set; }
        public string gps { get; set; }
        public Guid FileGroupId { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public DateTime SyncTime { get; set; } = DateTime.Now;
        public string NotSyncError { get; set; }
        public int? LogbookclientsiteId { get; set; }
        public bool? IsEntryByPCAR { get; set; } = false;
        public int? CallSignId { get; set; }
        public int? PositionId { get; set; }

    }
}
