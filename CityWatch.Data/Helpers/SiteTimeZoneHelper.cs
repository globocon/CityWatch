using System;

namespace CityWatch.Data.Helpers
{
    /// <summary>
    /// UTC to site-local conversion that follows daylight saving.
    ///
    /// ClientSiteKpiSettings.UTC is a fixed offset ("+10:00") with no DST rule, so converting with it
    /// puts every time an hour early from October to April for VIC/NSW/ACT/TAS/SA sites, and scans
    /// between 00:00 and 00:59 local land on the previous day (VISY-Coolaroo [VR] (FSO), Nov 2026:
    /// a 00:51 AEDT scan showed in the Wand Strikes export as 23:51 the day before).
    /// ClientSiteKpiSettings.TimezoneString holds the Windows zone id ("AUS Eastern Standard Time",
    /// "E. Australia Standard Time", "W. Australia Standard Time"), which carries the DST rules.
    /// </summary>
    public static class SiteTimeZoneHelper
    {
        public const string DefaultTimeZoneId = "AUS Eastern Standard Time";

        /// <summary>
        /// The site's zone from ClientSiteKpiSettings.TimezoneString; AUS Eastern (Melbourne/Sydney,
        /// with DST) when it is blank or unknown - the same default the rest of the app uses.
        /// </summary>
        public static TimeZoneInfo Resolve(string timezoneString)
        {
            if (!string.IsNullOrWhiteSpace(timezoneString))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(timezoneString.Trim()); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            try { return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId); }
            catch (Exception)
            {
                // Zone data unavailable on this host: a fixed +10:00 is the old behaviour.
                return TimeZoneInfo.CreateCustomTimeZone("AEST-fixed", TimeSpan.FromHours(10), "AEST", "AEST");
            }
        }

        /// <summary>Site wall-clock time for a UTC time (DateTime.Kind is ignored; the value is UTC).</summary>
        public static DateTime UtcToSiteLocal(DateTime utc, TimeZoneInfo zone) =>
            DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone),
                DateTimeKind.Unspecified);

        /// <summary>
        /// UTC instant of a site wall-clock time. Never throws: a time skipped by a DST change
        /// (not midnight in any AU zone) is taken with the zone's standard offset.
        /// </summary>
        public static DateTime SiteLocalToUtc(DateTime siteLocal, TimeZoneInfo zone)
        {
            var local = DateTime.SpecifyKind(siteLocal, DateTimeKind.Unspecified);
            return local - zone.GetUtcOffset(local);
        }

        /// <summary>The offset in force at that UTC instant, formatted "+11:00".</summary>
        public static string FormatOffset(DateTime utc, TimeZoneInfo zone)
        {
            var offset = zone.GetUtcOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
            return (offset < TimeSpan.Zero ? "-" : "+") + offset.Duration().ToString(@"hh\:mm");
        }
    }
}
