using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dropbox.Api.Sharing.RequestedLinkAccessLevel;

namespace CityWatch.Data.Models
{
    public class RCActionListMessagesGuardLogs
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Event Date and Time is required")]
        public DateTime EventDateTime { get; set; }
        public int GuardId { get; set; }
        public int RCActionListMessagesId { get; set; }
        public DateTime? EventDateTimeLocal { get; set; }

        public DateTimeOffset? EventDateTimeLocalWithOffset { get; set; }

        public string EventDateTimeZone { get; set; }
        public string RemoteIPAddress { get; set; }

        public string EventDateTimeZoneShort { get; set; }

        public int? EventDateTimeUtcOffsetMinute { get; set; }
        public bool IsDeleted { get; set; }
    }
}
