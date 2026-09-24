using System;using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CityWatch.Data.Models
{
    public class SiteEventLog
    {
        [Key]
        public int Id { get; set; }
        public int? GuardId { get; set; }
        public int? SiteId { get; set; }
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
        public int? EventServerOffsetMinute { get; set; }
        public DateTime EventLocalTime { get; set; }
        public string EventLocalTimeZone { get; set; }
        public int? EventLocalOffsetMinute { get; set; }
        public string EventChannel { get; set; }
        public string EventStatus { get; set; }
        public string EventErrorMsg { get; set; }
    }
}
