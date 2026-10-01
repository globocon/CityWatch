using CityWatch.Data.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWatch.Data.Models
{
    public class FileDownloadAuditLogs
    {
        [Key]
        public int Id { get; set; }
        public int UserID { get; set; }
        public int GuardID { get; set; }
        public string IPAddress { get; set; }
        public string DwnlCatagory { get; set; }
        public string DwnlFileName { get; set; }
        public DateTime EventDateTime { get; set; } = TimeZoneHelper.GetCurrentTimeZoneCurrentTime();
        public int? EventDateTimeServerOffsetMinute { get; set; } = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute();
        public DateTime? EventDateTimeLocal { get; set; }
        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }
        public string EventDateTimeZone { get; set; }
        public string EventDateTimeZoneShort { get; set; }
        public int? EventDateTimeUtcOffsetMinute { get; set; }
            
        [ForeignKey("UserID")]
        public User User { get; set; }
       
        [ForeignKey("GuardID")]
        public Guard Guard { get; set; }
        
    }
}
