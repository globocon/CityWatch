using System;

namespace CityWatch.Web.Models
{
    public class irOfflineFilesAttachmentsCache
    {        
        public Guid UniqueRecordId { get; set; } = Guid.NewGuid();
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
        public string ServerFileNameWithPath { get; set; } = string.Empty;
    }

    public class irOfflineCache
    {        
        public string IrId { get; set; }
        public IncidentRequest IncidentRequest { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsSynced { get; set; } = false;
        public Guid UniqueRecordId { get; set; } = Guid.NewGuid();
        public int guardId { get; set; }
        public int clientsiteId { get; set; }
        public int userId { get; set; }
        public string gps { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
    }

}
