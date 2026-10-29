using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Model
{
    [TestClass]
    public class TagHistoryReportTests
    {
        private static KpiSendTagHistorySchedules Schedule(DayOfWeek runDay, string time = "07:00", DateTime? start = null, DateTime? end = null)
        {
            return new KpiSendTagHistorySchedules
            {
                RunDayOfWeek = (int)runDay,
                Time = time,
                StartDate = start ?? new DateTime(2026, 1, 1),
                EndDate = end
            };
        }

        [TestMethod]
        public void GetNextRunOn_LaterTheSameWeek_ReturnsThatDay()
        {
            // Wednesday 21 Oct 2026, 10:00 -> next Friday 07:00
            var next = KpiTagHistoryScheduleRunOnCalculator.GetNextRunOn(Schedule(DayOfWeek.Friday), new DateTime(2026, 10, 21, 10, 0, 0));

            Assert.AreEqual(new DateTime(2026, 10, 23, 7, 0, 0), next);
        }

        [TestMethod]
        public void GetNextRunOn_RunDayTodayButTimePassed_ReturnsNextWeek()
        {
            var next = KpiTagHistoryScheduleRunOnCalculator.GetNextRunOn(Schedule(DayOfWeek.Wednesday), new DateTime(2026, 10, 21, 10, 0, 0));

            Assert.AreEqual(new DateTime(2026, 10, 28, 7, 0, 0), next);
        }

        [TestMethod]
        public void GetNextRunOn_RunDayTodayTimeNotYetPassed_ReturnsToday()
        {
            var next = KpiTagHistoryScheduleRunOnCalculator.GetNextRunOn(Schedule(DayOfWeek.Wednesday, "18:30"), new DateTime(2026, 10, 21, 10, 0, 0));

            Assert.AreEqual(new DateTime(2026, 10, 21, 18, 30, 0), next);
        }

        [TestMethod]
        public void GetNextRunOn_StartDateInFuture_FirstRunOnOrAfterStart()
        {
            var schedule = Schedule(DayOfWeek.Monday, start: new DateTime(2026, 11, 10));
            var next = KpiTagHistoryScheduleRunOnCalculator.GetNextRunOn(schedule, new DateTime(2026, 10, 21, 10, 0, 0));

            Assert.AreEqual(new DateTime(2026, 11, 16, 7, 0, 0), next);
        }

        [TestMethod]
        public void GetNextRunOn_PastEndDate_ReturnsMaxValue()
        {
            var schedule = Schedule(DayOfWeek.Monday, end: new DateTime(2026, 10, 25));
            var next = KpiTagHistoryScheduleRunOnCalculator.GetNextRunOn(schedule, new DateTime(2026, 10, 21, 10, 0, 0));

            Assert.AreEqual(DateTime.MaxValue, next);
        }

        [TestMethod]
        public void GetReportPeriod_IsTheSevenDaysBeforeTheRunDate()
        {
            var (from, to) = KpiTagHistoryScheduleRunOnCalculator.GetReportPeriod(new DateTime(2026, 10, 21, 7, 0, 0));

            Assert.AreEqual(new DateTime(2026, 10, 14), from);
            Assert.AreEqual(new DateTime(2026, 10, 20), to);
        }

        private static List<TagHistoryReportViewModel> Rows()
        {
            return new List<TagHistoryReportViewModel>
            {
                new() { Id = 1, SiteName = "B Site", TagUId = "02", Action = "Changed", ChangedOn = new DateTime(2026, 10, 15, 9, 0, 0) },
                new() { Id = 2, SiteName = "A Site", TagUId = "03", Action = "Added", ChangedOn = new DateTime(2026, 10, 14, 9, 0, 0) },
                new() { Id = 3, SiteName = "A Site", TagUId = "01", Action = "Deleted", ChangedOn = new DateTime(2026, 10, 16, 9, 0, 0) },
            };
        }

        [TestMethod]
        public void SortTagHistory_NoSortSelected_DefaultsToDateTime()
        {
            var sorted = ClientSiteWandDataProvider.SortTagHistory(Rows(), null, false);

            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, sorted.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void SortTagHistory_BySite_TiesBrokenByDateTime()
        {
            var sorted = ClientSiteWandDataProvider.SortTagHistory(Rows(), TagHistorySortBy.Site, false);

            CollectionAssert.AreEqual(new[] { 2, 3, 1 }, sorted.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void SortTagHistory_ByTagUIdDescending()
        {
            var sorted = ClientSiteWandDataProvider.SortTagHistory(Rows(), TagHistorySortBy.TagUId, true);

            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, sorted.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void Display_MatchesReportFormat()
        {
            var row = new TagHistoryReportViewModel
            {
                Action = "Changed",
                Note = "label changed",
                GuardName = "Sam Smith",
                GuardInitial = "S.S",
                GuardSecurityNo = "556-998-3333",
                ChangedOn = new DateTime(2025, 8, 23, 12, 0, 0)
            };

            Assert.AreEqual("Sam Smith [S.S] - Security No: 556-998-3333", row.GuardDisplay);
            Assert.AreEqual("23 AUG 2025 @ 12:00 hrs", row.ActionDateTimeText);
            Assert.AreEqual("Changed [label changed]", row.TagAction);
        }

        [TestMethod]
        public void Request_EmptySiteList_MeansAllSites()
        {
            var request = new TagHistoryReportRequest { ClientSiteId = null };

            Assert.AreEqual(0, request.ClientSiteIds.Length);
            CollectionAssert.AreEqual(new[] { 3, 7 }, new TagHistoryReportRequest { ClientSiteId = "3, 7," }.ClientSiteIds);
        }
    }
}
