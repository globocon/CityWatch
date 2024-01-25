using System;

namespace CityWatch.Web.Helpers
{
    public static class GuardLogBookHelper
    {
        public static DateTime GetLogOffDateTime()
        {
            var logOffDate = DateTime.Today.AddDays(-1);
            return new DateTime(logOffDate.Year, logOffDate.Month, logOffDate.Day, 23, 59, 00);
        }

        // Task p6#73_TimeZone issue -- added by Binoy - Start
        public static DateTime GetLocalLogOffDateTime(DateTime ldtm)
        {
            var logOffDate = ldtm.AddDays(-1);
            return new DateTime(logOffDate.Year, logOffDate.Month, logOffDate.Day, 23, 59, 00);
        }

        public static DateTimeOffset GetLocalLogOffDateTimeWithOffset(DateTimeOffset ldtm)
        {
            var logOffDate = ldtm.AddDays(-1);
            return new DateTimeOffset(logOffDate.Year, logOffDate.Month, logOffDate.Day, 23, 59, 00,000, ldtm.Offset);
        }
        // Task p6#73_TimeZone issue -- added by Binoy - End
    }
}
