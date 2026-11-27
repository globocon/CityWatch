using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace CityWatch.Data.Models
{
    public class SmsChannel
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int CompanyId { get; set; } = 1;
        public string SmsProvider { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string SmsSender { get; set; } = string.Empty;
    }

    public class SmsChannelEventLog
    {
        public int? GuardId { get; set; }
        public int? SiteId { get; set; }
        public string? GuardName { get; set; }
        public string? SiteName { get; set; }
        public string GuardNumber { get; set; }
    }
}
