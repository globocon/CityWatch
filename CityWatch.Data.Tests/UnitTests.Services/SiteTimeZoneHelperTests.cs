using CityWatch.Data.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CityWatch.Data.Tests.UnitTests.Services
{
    /// <summary>
    /// Wand Strikes report times: UTC hit times must convert to site time with daylight saving,
    /// not the fixed "+10:00" of ClientSiteKpiSettings.UTC.
    /// </summary>
    [TestClass]
    public class SiteTimeZoneHelperTests
    {
        private static DateTime D(string s) => DateTime.Parse(s);

        [TestMethod]
        public void Melbourne_DuringDaylightSaving_IsUtcPlus11()
        {
            // VISY-Coolaroo [VR] (FSO), hit-log 1663463: Fusion showed 14-Nov 00:51, the old
            // Wand Strikes export 13-Nov 23:51.
            var zone = SiteTimeZoneHelper.Resolve("AUS Eastern Standard Time");

            var local = SiteTimeZoneHelper.UtcToSiteLocal(D("2026-11-13 13:51:42"), zone);

            Assert.AreEqual(D("2026-11-14 00:51:42"), local);
            Assert.AreEqual("+11:00", SiteTimeZoneHelper.FormatOffset(D("2026-11-13 13:51:42"), zone));
        }

        [TestMethod]
        public void Melbourne_OutsideDaylightSaving_IsUtcPlus10()
        {
            var zone = SiteTimeZoneHelper.Resolve("AUS Eastern Standard Time");

            Assert.AreEqual(D("2026-07-14 00:51:42"), SiteTimeZoneHelper.UtcToSiteLocal(D("2026-07-13 14:51:42"), zone));
            Assert.AreEqual("+10:00", SiteTimeZoneHelper.FormatOffset(D("2026-07-13 14:51:42"), zone));
        }

        [TestMethod]
        public void Brisbane_HasNoDaylightSaving()
        {
            var zone = SiteTimeZoneHelper.Resolve("E. Australia Standard Time");

            Assert.AreEqual(D("2026-11-13 23:51:42"), SiteTimeZoneHelper.UtcToSiteLocal(D("2026-11-13 13:51:42"), zone));
        }

        [TestMethod]
        public void Perth_IsUtcPlus8()
        {
            var zone = SiteTimeZoneHelper.Resolve("W. Australia Standard Time");

            Assert.AreEqual(D("2026-11-13 21:51:42"), SiteTimeZoneHelper.UtcToSiteLocal(D("2026-11-13 13:51:42"), zone));
        }

        [TestMethod]
        public void BlankOrUnknownZone_FallsBackToAusEastern()
        {
            foreach (var tz in new[] { null, "", "  ", "Not A Zone" })
            {
                var zone = SiteTimeZoneHelper.Resolve(tz);
                Assert.AreEqual(D("2026-11-14 00:51:42"), SiteTimeZoneHelper.UtcToSiteLocal(D("2026-11-13 13:51:42"), zone), tz ?? "null");
            }
        }

        [TestMethod]
        public void LocalMidnightToUtc_UsesOffsetOfThatDate()
        {
            var zone = SiteTimeZoneHelper.Resolve("AUS Eastern Standard Time");

            // Report range bounds: 14-Nov local midnight is 13-Nov 13:00 UTC in daylight saving,
            // 14-Jul local midnight is 13-Jul 14:00 UTC outside it.
            Assert.AreEqual(D("2026-11-13 13:00"), SiteTimeZoneHelper.SiteLocalToUtc(D("2026-11-14"), zone));
            Assert.AreEqual(D("2026-07-13 14:00"), SiteTimeZoneHelper.SiteLocalToUtc(D("2026-07-14"), zone));
        }

        [TestMethod]
        public void TimeSkippedByDstChange_DoesNotThrow()
        {
            var zone = SiteTimeZoneHelper.Resolve("AUS Eastern Standard Time");

            // 4-Oct-2026 02:30 does not exist in Melbourne (clocks jump 02:00 -> 03:00).
            var utc = SiteTimeZoneHelper.SiteLocalToUtc(D("2026-10-04 02:30"), zone);

            Assert.AreEqual(D("2026-10-03 16:30"), utc);
        }
    }
}
