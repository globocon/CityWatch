using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Services
{
    /// <summary>
    /// Duplicate NFC scans in Fusion / wand strike reports: rows equal to the minute with the same
    /// displayed content are shown once; anything that differs is kept.
    /// </summary>
    [TestClass]
    public class DuplicateRecordFilterTests
    {
        private const string Point = "Patrol Cyan - Point 09n - Building \"E\" - VR NTH4 VP4 Back Wall [NFC]";

        private static ClientSiteRadioChecksActivityStatus_History Sw(int id, string time, int guardId = 1751, string notes = Point, int siteId = 427) => new()
        {
            Id = id,
            ClientSiteId = siteId,
            GuardId = guardId,
            GuardName = "[P.R (2)] Parthiv Roshan Randeri",
            ActivityType = "SW",
            Notes = notes,
            ActivityDescription = notes,
            EventDateTime = DateTime.Parse(time)
        };

        [TestMethod]
        public void Fusion_SameMinuteSameContent_KeepsLowestIdOnce()
        {
            var rows = new List<ClientSiteRadioChecksActivityStatus_History>
            {
                Sw(5101381, "2026-10-30 22:20:49.913"),
                Sw(5101379, "2026-10-30 22:20:44.957"),
                Sw(5101380, "2026-10-30 22:20:44.953"),
            };

            var result = DuplicateRecordFilter.RemoveDuplicateFusionLogs(rows);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(5101379, result[0].Id);
        }

        [TestMethod]
        public void Fusion_DifferentMinuteGuardSiteOrText_AreKept()
        {
            var rows = new List<ClientSiteRadioChecksActivityStatus_History>
            {
                Sw(1, "2026-10-30 22:20:10"),
                Sw(2, "2026-10-30 22:21:10"),                          // next minute
                Sw(3, "2026-10-30 22:20:10", guardId: 1604),           // other guard
                Sw(4, "2026-10-30 22:20:10", siteId: 55),              // other site
                Sw(5, "2026-10-30 22:20:10", notes: "Other point [NFC]") // other point
            };

            var result = DuplicateRecordFilter.RemoveDuplicateFusionLogs(rows);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void Fusion_KeepsCallerOrder()
        {
            var rows = new List<ClientSiteRadioChecksActivityStatus_History>
            {
                Sw(9, "2026-10-30 22:19:00", notes: "A"),
                Sw(3, "2026-10-30 22:20:00", notes: "B"),
                Sw(2, "2026-10-30 22:20:30", notes: "B"),
                Sw(7, "2026-10-30 22:21:00", notes: "C")
            };

            var result = DuplicateRecordFilter.RemoveDuplicateFusionLogs(rows);

            CollectionAssert.AreEqual(new[] { 9, 2, 7 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void TagHits_SameScanSentTwice_CountedOnce()
        {
            ClientSiteSmartWandTagsHitLog Hit(int id, string utc, string tag = "042B89E2711191") => new()
            {
                Id = id,
                LoggedInClientSiteId = 427,
                LoggedInGuardId = 1751,
                SmartWandId = 102,
                TagUId = tag,
                LabelDescription = "Patrol Cyan - Point 09n",
                HitUtcDateTime = DateTime.Parse(utc)
            };

            var rows = new List<ClientSiteSmartWandTagsHitLog>
            {
                Hit(1574730, "2026-10-25 11:24:17.190"),
                Hit(1574732, "2026-10-25 11:24:17.190"),
                Hit(1574734, "2026-10-25 11:24:17.213"),
                Hit(1574740, "2026-10-25 11:24:20.000", tag: "044E76E2711191")
            };

            var result = DuplicateRecordFilter.RemoveDuplicateTagHits(rows);

            CollectionAssert.AreEqual(new[] { 1574730, 1574740 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void Null_ReturnsEmpty()
        {
            Assert.AreEqual(0, DuplicateRecordFilter.RemoveDuplicateFusionLogs(null).Count);
            Assert.AreEqual(0, DuplicateRecordFilter.RemoveDuplicateTagHits(null).Count);
        }
    }
}
