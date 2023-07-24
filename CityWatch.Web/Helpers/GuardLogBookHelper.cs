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
    }
}
