using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class SiteEventLog
    {
        [Key]
        public int Id { get; set; }
        public int GuardId { get; set; }
        public int SiteId { get; set; }
        public string GuardName { get; set; }
        public string SiteName { get; set; }
        public string ProjectName { get; set; }
        public string ActivityType { get; set; }
        public string Module { get; set; }
        public string SubModule { get; set; }
        public string GoogleMapCoordinates { get; set; }
        public string IPAddress { get; set; }
        public DateTime EventTime { get; set; }
        public string ToAddress { get; set; }
        public string ToMessage { get; set; }
        public string FromAddress { get; set; }
        public string EventServerTimeZone { get; set; }
        public int EventServerOffsetMinute { get; set; }
        public DateTime EventLocalTime { get; set; }
        public string EventLocalTimeZone { get; set; }
        public int EventLocalOffsetMinute { get; set; }
        public string EventChannel { get; set; }
        public string EventStatus { get; set; }
        public string EventErrorMsg { get; set; }
    }
}
