using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Tests.UnitTests.Services
{
    /// <summary>
    /// Patrol Randomisation rules: STND only, any days, max 2 hours a day, and a random next checkpoint
    /// that never repeats the tag just struck and covers every tag before repeating one.
    /// </summary>
    [TestClass]
    public class PatrolRandomisationHelperTests
    {
        private static PatrolRandomisationHelper.DayWindow Day(DayOfWeek d, string start, string end) => new()
        {
            Day = d,
            Start = start == null ? null : TimeSpan.Parse(start),
            End = end == null ? null : TimeSpan.Parse(end)
        };

        [TestMethod]
        public void Validate_ThursdayUnderTwoHours_IsValid()
        {
            var error = PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Thursday, "02:00", "03:59") }, out var window);

            Assert.IsNull(error);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(DayOfWeek.Thursday, window[0].Day);
            Assert.AreEqual(TimeSpan.FromHours(2), window[0].Start);
        }

        [TestMethod]
        public void Validate_ExactlyTwoHours_IsValid()
        {
            Assert.IsNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Monday, "10:00", "12:00") }, out _));
        }

        [TestMethod]
        public void Validate_OverTwoHours_IsRefused()
        {
            Assert.IsNotNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Monday, "10:00", "12:01") }, out _));
        }

        [TestMethod]
        public void Validate_SeveralDays_AreAllKept()
        {
            var error = PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Sunday, "22:00", "23:59"), Day(DayOfWeek.Monday, "10:00", "11:00"), Day(DayOfWeek.Thursday, "02:00", "03:59") },
                out var windows);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Sunday }, windows.Select(w => w.Day).ToArray());
        }

        [TestMethod]
        public void Validate_OneBadDayAmongMany_IsRefused()
        {
            var error = PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Monday, "10:00", "11:00"), Day(DayOfWeek.Tuesday, "10:00", "13:00") }, out _);
            StringAssert.Contains(error, "Tuesday");
        }

        [TestMethod]
        public void Validate_SameDayTwice_IsRefused()
        {
            Assert.IsNotNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Monday, "10:00", "11:00"), Day(DayOfWeek.Monday, "12:00", "13:00") }, out _));
        }

        [TestMethod]
        public void Validate_EndBeforeStart_IsRefused()
        {
            Assert.IsNotNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Monday, "23:30", "00:30") }, out _));
        }

        [TestMethod]
        public void Validate_MissingEnd_IsRefused()
        {
            Assert.IsNotNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                new[] { Day(DayOfWeek.Monday, "10:00", null) }, out _));
        }

        [TestMethod]
        public void Validate_EnabledWithoutWindow_IsRefused()
        {
            Assert.IsNotNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, true,
                Array.Empty<PatrolRandomisationHelper.DayWindow>(), out _));
        }

        [TestMethod]
        public void Validate_DisabledWithoutWindow_IsValid()
        {
            Assert.IsNull(PatrolRandomisationHelper.Validate(PatrolTouringMode.STND, false,
                Array.Empty<PatrolRandomisationHelper.DayWindow>(), out var window));
            Assert.AreEqual(0, window.Count);
        }

        [DataTestMethod]
        [DataRow(PatrolTouringMode.PCAR)]
        [DataRow(PatrolTouringMode.INSP)]
        public void Validate_EnabledOutsideStnd_IsRefused(PatrolTouringMode mode)
        {
            Assert.IsNotNull(PatrolRandomisationHelper.Validate(mode, true,
                new[] { Day(DayOfWeek.Monday, "10:00", "11:00") }, out _));
        }

        private static ClientSitePatrolRandomisationSetting ThursdayTwoToFour(bool enabled = true) => new()
        {
            IsEnabled = enabled,
            Schedules = new List<ClientSitePatrolRandomisationSchedule>
            {
                new() { DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(2, 0, 0), EndTime = new TimeSpan(3, 59, 0) },
                new() { DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(22, 0, 0) }
            }
        };

        // 2026-11-19 is a Thursday.
        [DataTestMethod]
        [DataRow("2026-11-19 02:00", true)]
        [DataRow("2026-11-19 03:58", true)]
        [DataRow("2026-11-19 03:59", false)]
        [DataRow("2026-11-19 01:59", false)]
        [DataRow("2026-11-20 02:30", false)] // Friday
        [DataRow("2026-11-21 21:59", true)]  // Saturday window
        [DataRow("2026-11-21 02:30", false)] // Saturday, Thursday's times
        public void IsWindowActive_FollowsDayAndTimes(string siteLocal, bool expected)
        {
            Assert.AreEqual(expected, PatrolRandomisationHelper.IsWindowActive(ThursdayTwoToFour(), PatrolTouringMode.STND,
                DateTime.Parse(siteLocal)));
        }

        [TestMethod]
        public void IsWindowActive_DisabledOrNotStnd_IsFalse()
        {
            var t = DateTime.Parse("2026-11-19 02:30");
            Assert.IsFalse(PatrolRandomisationHelper.IsWindowActive(ThursdayTwoToFour(enabled: false), PatrolTouringMode.STND, t));
            Assert.IsFalse(PatrolRandomisationHelper.IsWindowActive(ThursdayTwoToFour(), PatrolTouringMode.PCAR, t));
            Assert.IsFalse(PatrolRandomisationHelper.IsWindowActive(null, PatrolTouringMode.STND, t));
        }

        private static List<ClientSiteSmartWandTags> Tags(params string[] uids) =>
            uids.Select((u, i) => new ClientSiteSmartWandTags { Id = i + 1, UId = u, LabelDescription = "Tag " + u }).ToList();

        [TestMethod]
        public void Describe_ListsDaysMondayFirst()
        {
            Assert.AreEqual("Thu 02:00-03:59, Sat 20:00-22:00", PatrolRandomisationHelper.Describe(ThursdayTwoToFour().Schedules));
        }

        [TestMethod]
        public void PickNextTag_NeverReturnsTheTagJustStruck()
        {
            var tags = Tags("A", "B");
            for (var i = 0; i < 50; i++)
                Assert.AreEqual("B", PatrolRandomisationHelper.PickNextTag(tags, "a", null).UId);
        }

        [TestMethod]
        public void PickNextTag_SkipsTagsAlreadyHandedOutThisWindow()
        {
            var tags = Tags("A", "B", "C", "D");
            var next = PatrolRandomisationHelper.PickNextTag(tags, "A", new[] { "B", "C" }, n => 0);
            Assert.AreEqual("D", next.UId);
        }

        [TestMethod]
        public void PickNextTag_AllUsed_StartsAgainButNotOnCurrentTag()
        {
            var tags = Tags("A", "B", "C");
            var picked = Enumerable.Range(0, 2)
                .Select(i => PatrolRandomisationHelper.PickNextTag(tags, "A", new[] { "A", "B", "C" }, n => i).UId)
                .ToList();
            CollectionAssert.AreEquivalent(new[] { "B", "C" }, picked);
        }

        [TestMethod]
        public void PickNextTag_WeightsPointsMissedInLast72Hours()
        {
            // Pool B, C, D (A was just struck); C is a priority point with weight 3: rolls 0 = B, 1-3 = C, 4 = D.
            var tags = Tags("A", "B", "C", "D");
            var picks = Enumerable.Range(0, 5)
                .Select(roll => PatrolRandomisationHelper.PickNextTag(tags, "A", null, total => { Assert.AreEqual(5, total); return roll; }, new[] { "C" }).UId)
                .ToArray();
            CollectionAssert.AreEqual(new[] { "B", "C", "C", "C", "D" }, picks);
        }

        [TestMethod]
        public void PickNextTag_SingleOrNoTag_ReturnsNull()
        {
            Assert.IsNull(PatrolRandomisationHelper.PickNextTag(Tags("A"), "A", null));
            Assert.IsNull(PatrolRandomisationHelper.PickNextTag(Tags(), "A", null));
        }
    }
}
