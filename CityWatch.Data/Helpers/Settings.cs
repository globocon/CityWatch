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

        /// <summary>
        /// Test-mode safety valve for the monthly KPI send.
        ///
        /// When this holds one or more addresses (comma separated), every recipient of the
        /// monthly KPI email - To, CC, BCC, the schedule's own recipients and the standing
        /// BCC alike - is discarded and the message goes only to the addresses listed here,
        /// with "[TEST]" prefixed to the subject.
        ///
        /// Set it to "" (or remove the key) to go back to live sending. Nothing else needs
        /// to change. It is deliberately opt-in: an absent key means normal behaviour, so
        /// production cannot start redirecting by accident.
        /// </summary>
        public string TestModeRedirectTo { get; set; }
    }

    public class GoogleMapSettings
    {
        public string ApiKey { get; set; }
        public string GpsImageZoom { get; set; }
        public string GpsImageSize { get; set; }
    }
}
