using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Services
{
    /// <summary>
    /// The PCAR rules of the site log dump scheduler: what "Disable PCAR" leaves out, what a
    /// "Schedule PCAR" dump keeps, and when "Disable PCAR" applies at all.
    /// </summary>
    [TestClass]
    public class PcarEntryFilterHelperTests
    {
        private static List<GuardLog> GuardLogs() => new()
        {
            new GuardLog { Id = 1, Notes = "Patrolling OC2", IsEntryByPCAR = false },
            new GuardLog { Id = 2, Notes = "[Martha Cove Marina] point Z [PCAR NFC]", IsEntryByPCAR = true },
            new GuardLog { Id = 3, Notes = "Logbook Logged In", IsEntryByPCAR = false },
            new GuardLog { Id = 4, Notes = "PCAR visit", IsEntryByPCAR = true }
        };

        private static List<ClientSiteRadioChecksActivityStatus_History> HistoryRows() => new()
        {
            new ClientSiteRadioChecksActivityStatus_History { Id = 10, ActivityType = "LB", LBId = 1 },
            new ClientSiteRadioChecksActivityStatus_History { Id = 11, ActivityType = "SW", LBId = 2 },  // PCAR
            new ClientSiteRadioChecksActivityStatus_History { Id = 12, ActivityType = "LB", LBId = 4 },  // PCAR
            new ClientSiteRadioChecksActivityStatus_History { Id = 13, ActivityType = "KV", KVId = 99 },
            new ClientSiteRadioChecksActivityStatus_History { Id = 14, ActivityType = "IR", IRId = 7 }
        };

        private static readonly HashSet<int> PcarIds = new() { 2, 4 };

        /* ---------------- guard log report ---------------- */

        [TestMethod]
        public void GuardLogs_All_IsUnchanged()
        {
            var result = PcarEntryFilterHelper.Apply(GuardLogs(), PcarEntryFilter.All);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void GuardLogs_ExcludePcar_LeavesOutEveryPcarEntry()
        {
            var result = PcarEntryFilterHelper.Apply(GuardLogs(), PcarEntryFilter.ExcludePcar);
            CollectionAssert.AreEqual(new[] { 1, 3 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void GuardLogs_OnlyPcar_KeepsOnlyPcarEntries()
        {
            var result = PcarEntryFilterHelper.Apply(GuardLogs(), PcarEntryFilter.OnlyPcar);
            CollectionAssert.AreEqual(new[] { 2, 4 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void GuardLogs_ExcludeAndOnly_PartitionTheReport()
        {
            var excluded = PcarEntryFilterHelper.Apply(GuardLogs(), PcarEntryFilter.ExcludePcar);
            var only = PcarEntryFilterHelper.Apply(GuardLogs(), PcarEntryFilter.OnlyPcar);

            Assert.AreEqual(GuardLogs().Count, excluded.Count + only.Count);
            Assert.IsFalse(excluded.Select(x => x.Id).Intersect(only.Select(x => x.Id)).Any());
        }

        /* ---------------- smart wand / fusion rows ---------------- */

        [TestMethod]
        public void History_All_IsUnchanged()
        {
            var result = PcarEntryFilterHelper.Apply(HistoryRows(), PcarIds, PcarEntryFilter.All);
            Assert.AreEqual(5, result.Count);
        }

        [TestMethod]
        public void History_ExcludePcar_DropsPcarRows_KeepsKvAndIr()
        {
            var result = PcarEntryFilterHelper.Apply(HistoryRows(), PcarIds, PcarEntryFilter.ExcludePcar);
            CollectionAssert.AreEqual(new[] { 10, 13, 14 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void History_OnlyPcar_KeepsPcarRows_DropsKvAndIr()
        {
            // KV and IR rows have no PCAR marker, so they are never PCAR entries.
            var result = PcarEntryFilterHelper.Apply(HistoryRows(), PcarIds, PcarEntryFilter.OnlyPcar);
            CollectionAssert.AreEqual(new[] { 11, 12 }, result.Select(x => x.Id).ToArray());
        }

        [TestMethod]
        public void History_NoPcarEntries_OnlyPcarIsEmpty()
        {
            var result = PcarEntryFilterHelper.Apply(HistoryRows(), new HashSet<int>(), PcarEntryFilter.OnlyPcar);
            Assert.AreEqual(0, result.Count);
        }

        /* ---------------- when "Disable PCAR" applies ---------------- */

        private static ClientSite Site(PatrolTouringMode mode, bool daily, bool weekly, bool monthly) => new()
        {
            Id = 390,
            PatrolTourMode = mode,
            DisablePcarDailyLog = daily,
            DisablePcarWeeklyLog = weekly,
            DisablePcarMonthlyLog = monthly
        };

        [TestMethod]
        public void ScheduleFilter_Off_ByDefault()
        {
            var site = new ClientSite { Id = 390 };
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, null));
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, LogDumpPeriodType.Weekly));
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, LogDumpPeriodType.Monthly));
        }

        [TestMethod]
        public void ScheduleFilter_IsPerPeriod()
        {
            var site = Site(PatrolTouringMode.STND, daily: true, weekly: false, monthly: true);

            Assert.AreEqual(PcarEntryFilter.ExcludePcar, PcarEntryFilterHelper.GetScheduleFilter(site, null));
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, LogDumpPeriodType.Weekly));
            Assert.AreEqual(PcarEntryFilter.ExcludePcar, PcarEntryFilterHelper.GetScheduleFilter(site, LogDumpPeriodType.Monthly));
        }

        [TestMethod]
        public void ScheduleFilter_IgnoredForAPcarSite()
        {
            // Every entry of a PCAR site is a PCAR entry - excluding them would empty its reports.
            var site = Site(PatrolTouringMode.PCAR, daily: true, weekly: true, monthly: true);

            Assert.IsFalse(site.IsDisablePcarApplicable);
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, null));
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, LogDumpPeriodType.Weekly));
            Assert.AreEqual(PcarEntryFilter.All, PcarEntryFilterHelper.GetScheduleFilter(site, LogDumpPeriodType.Monthly));
        }

        [TestMethod]
        public void ScheduleFilter_AppliesToInspectorSites()
        {
            var site = Site(PatrolTouringMode.INSP, daily: true, weekly: true, monthly: true);
            Assert.IsTrue(site.IsDisablePcarApplicable);
            Assert.AreEqual(PcarEntryFilter.ExcludePcar, PcarEntryFilterHelper.GetScheduleFilter(site, null));
        }

        /* ---------------- names and types ---------------- */

        [TestMethod]
        public void PcarReportSuffix_GoesBeforeTheExtension()
        {
            Assert.AreEqual("20261005 - Daily Guard Log - Martha Cove Marina - v1.0.0.0 - PCAR-Report.pdf",
                PcarEntryFilterHelper.AppendPcarReportSuffix("20261005 - Daily Guard Log - Martha Cove Marina - v1.0.0.0.pdf"));

            Assert.AreEqual(@"C:\out\a.b - PCAR-Report.pdf",
                PcarEntryFilterHelper.AppendPcarReportSuffix(@"C:\out\a.b.pdf"));
        }

        [TestMethod]
        public void PcarReportTypes_MapToTheirBaseReport()
        {
            Assert.AreEqual(LogBookType.DailyGuardLog, PcarEntryFilterHelper.GetBaseReportType(LogBookType.PcarGuardLog));
            Assert.AreEqual(LogBookType.SmartWandLog, PcarEntryFilterHelper.GetBaseReportType(LogBookType.PcarSmartWandLog));
            Assert.AreEqual(LogBookType.FusionLog, PcarEntryFilterHelper.GetBaseReportType(LogBookType.PcarFusionLog));
            Assert.AreEqual(LogBookType.VehicleAndKeyLog, PcarEntryFilterHelper.GetBaseReportType(LogBookType.VehicleAndKeyLog));

            Assert.IsTrue(PcarEntryFilterHelper.IsPcarReportType(LogBookType.PcarGuardLog));
            Assert.IsFalse(PcarEntryFilterHelper.IsPcarReportType(LogBookType.DailyGuardLog));
        }

        [TestMethod]
        public void ExistingLogBookTypeValues_AreUnchanged()
        {
            // Stored as ints in ClientSiteLogBooks.Type and ClientSitePeriodicLogUploads.LogBookType.
            Assert.AreEqual(1, (int)LogBookType.DailyGuardLog);
            Assert.AreEqual(2, (int)LogBookType.VehicleAndKeyLog);
            Assert.AreEqual(3, (int)LogBookType.FusionLog);
            Assert.AreEqual(4, (int)LogBookType.SmartWandLog);
            Assert.AreEqual(5, (int)LogBookType.PcarGuardLog);
            Assert.AreEqual(6, (int)LogBookType.PcarSmartWandLog);
            Assert.AreEqual(7, (int)LogBookType.PcarFusionLog);
        }
    }
}
