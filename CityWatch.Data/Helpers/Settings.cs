using System;

namespace CityWatch.Data.Helpers
{
    public class EmailOptions
    {
        public const string Email = "Email";
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string CcAddress { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public class GoogleMapSettings
    {
        public string ApiKey { get; set; }
        public string GpsImageZoom { get; set; }
        public string GpsImageSize { get; set; }
    }
}
