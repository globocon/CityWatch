using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Patrol Randomisation strike flow and its scan log (ClientSitePatrolRandomisationLogs):
    /// a strike inside a window closes the guard's pending instruction (Completed / Skipped) and
    /// hands out a different tag; a closed window marks it Missed; offline strikes are logged but
    /// get no next checkpoint.
    /// </summary>
    [TestClass]
    public class PatrolRandomisationServiceTests
    {
        private const int SiteId = 7;
        private const int GuardId = 2;
        private const int UserId = 15;

        // Brisbane: no daylight saving, so UTC+10 all year.
        // Thursday 2026-11-19 02:00-03:59 local = Wednesday 2026-11-18 16:00-17:59 UTC.
        // Saturday 2026-11-21 20:00-22:00 local = Saturday 2026-11-21 10:00-12:00 UTC.
        private static readonly DateTime InWindowUtc = new(2026, 11, 18, 16, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime WindowEndUtc = new(2026, 11, 18, 17, 59, 0, DateTimeKind.Utc);
        private static readonly DateTime OutOfWindowUtc = new(2026, 11, 18, 18, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime SaturdayWindowUtc = new(2026, 11, 21, 11, 0, 0, DateTimeKind.Utc);

        private List<ClientSitePatrolRandomisationLog> _added;
        private List<ClientSitePatrolRandomisationLog> _resolved;
        private List<ClientSitePatrolRandomisationLog> _guardLogs;

        private PatrolRandomisationService Create(ClientSitePatrolRandomisationLog pending = null,
            PatrolTouringMode mode = PatrolTouringMode.STND)
        {
            _added = new();
            _resolved = new();
            _guardLogs ??= new();
            var wand = new Mock<IClientSiteWandDataProvider>();
            wand.Setup(x => x.GetPatrolRandomisationSetting(SiteId)).Returns(new ClientSitePatrolRandomisationSetting
            {
                ClientSiteId = SiteId,
                IsEnabled = true,
                Schedules = new List<ClientSitePatrolRandomisationSchedule>
                {
                    new() { DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(2, 0, 0), EndTime = new TimeSpan(3, 59, 0) },
                    new() { DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeSpan(20, 0, 0), EndTime = new TimeSpan(22, 0, 0) }
                }
            });
            wand.Setup(x => x.GetClientSiteTimezoneString(SiteId)).Returns("E. Australia Standard Time");
            wand.Setup(x => x.GetActiveSmartWandTagsForSite(SiteId)).Returns(new List<ClientSiteSmartWandTags>
            {
                new() { Id = 1, UId = "A", LabelDescription = "Gate" },
                new() { Id = 2, UId = "B", LabelDescription = "Dock" },
                new() { Id = 3, UId = "C", LabelDescription = "Yard" },
            });
            wand.Setup(x => x.GetPatrolRandomisationAssignedTagUIds(SiteId, GuardId, It.IsAny<DateTime>())).Returns(new List<string>());
            wand.Setup(x => x.GetPendingPatrolRandomisationLog(SiteId, GuardId)).Returns(pending);
            wand.Setup(x => x.AddPatrolRandomisationLog(It.IsAny<ClientSitePatrolRandomisationLog>()))
                .Callback<ClientSitePatrolRandomisationLog>(a => { a.Id = 100 + _added.Count; _added.Add(a); });
            wand.Setup(x => x.ResolvePatrolRandomisationLog(It.IsAny<ClientSitePatrolRandomisationLog>(), It.IsAny<bool>()))
                .Returns(true)
                .Callback<ClientSitePatrolRandomisationLog, bool>((r, _) => _resolved.Add(r));
            wand.Setup(x => x.GetPatrolRandomisationPriorityTagUIds(SiteId, It.IsAny<DateTime>())).Returns(new List<string> { "C" });
            wand.Setup(x => x.GetPatrolRandomisationLogsForGuard(SiteId, GuardId, It.IsAny<DateTime>())).Returns(() => _guardLogs);

            var client = new Mock<IClientDataProvider>();
            client.Setup(x => x.GetClientSiteDetailsWithId(SiteId)).Returns(new List<ClientSite> { new() { Id = SiteId, PatrolTourMode = mode } });

            return new PatrolRandomisationService(wand.Object, client.Object);
        }

        private static ClientSitePatrolRandomisationLog Pending(string tagUId) => new()
        {
            Id = 50,
            ClientSiteId = SiteId,
            GuardId = GuardId,
            AssignedTagId = 2,
            AssignedTagUId = tagUId,
            AssignedUtc = InWindowUtc.AddMinutes(-10),
            WindowEndUtc = WindowEndUtc,
            Status = PatrolRandomisationLogStatus.Pending
        };

        private static PatrolRandomisationStrike Strike(string uid, DateTime utc, bool offline = false) => new()
        {
            ClientSiteId = SiteId,
            GuardId = GuardId,
            UserId = UserId,
            SmartWandId = 4,
            TagUId = uid,
            HitLogId = 9,
            HitUtc = utc,
            IsOffline = offline
        };

        [TestMethod]
        public void StrikeInWindow_LogsInstructionWithSiteLocalTimes()
        {
            var result = Create().OnWandStrike(Strike("A", InWindowUtc));

            Assert.IsTrue(result.IsActive);
            Assert.AreNotEqual("A", result.NextCheckpoint.TagUId);
            var row = _added.Single();
            Assert.AreEqual(PatrolRandomisationLogStatus.Pending, row.Status);
            Assert.AreEqual(new DateTime(2026, 11, 19), row.BusinessDate);
            Assert.AreEqual(DayOfWeek.Thursday, row.DayOfWeek);
            Assert.AreEqual(new DateTime(2026, 11, 19, 2, 30, 0), row.FromScanLocal);
            Assert.AreEqual("Gate", row.FromTagLabel);
            Assert.AreEqual(false, row.FromIsOffline);
            Assert.AreEqual(UserId, row.UserId);
            Assert.AreEqual(4, row.SmartWandId);
            Assert.AreEqual("+10:00", row.SiteUtcOffset);
            Assert.AreEqual(WindowEndUtc, row.WindowEndUtc);
        }

        [TestMethod]
        public void StrikeInSecondDaysWindow_IsRandomised()
        {
            var result = Create().OnWandStrike(Strike("A", SaturdayWindowUtc));

            Assert.IsTrue(result.IsActive);
            Assert.AreEqual(DayOfWeek.Saturday, _added.Single().DayOfWeek);
        }

        [TestMethod]
        public void StrikeOfAssignedTag_CompletesPending()
        {
            var result = Create(Pending("B")).OnWandStrike(Strike("b", InWindowUtc));

            Assert.AreEqual("Completed", result.PreviousCheckpointResult);
            var r = _resolved.Single();
            Assert.AreEqual(PatrolRandomisationLogStatus.Completed, r.Status);
            Assert.AreEqual("b", r.ScannedTagUId);
            Assert.AreEqual(false, r.ScannedIsOffline);
            Assert.AreEqual(UserId, r.ScannedUserId);
            Assert.AreNotEqual("B", result.NextCheckpoint.TagUId);
        }

        [TestMethod]
        public void StrikeOfOtherTag_MarksPendingSkipped()
        {
            var result = Create(Pending("B")).OnWandStrike(Strike("C", InWindowUtc));

            Assert.AreEqual("Skipped", result.PreviousCheckpointResult);
            Assert.AreEqual(PatrolRandomisationLogStatus.Skipped, _resolved.Single().Status);
        }

        [TestMethod]
        public void StrikeAfterWindow_MarksPendingMissedAndAssignsNothing()
        {
            var result = Create(Pending("B")).OnWandStrike(Strike("C", OutOfWindowUtc));

            Assert.IsFalse(result.IsActive);
            Assert.IsNull(result.NextCheckpoint);
            Assert.AreEqual(0, _added.Count);
            Assert.AreEqual(PatrolRandomisationLogStatus.Missed, _resolved.Single().Status);
        }

        [TestMethod]
        public void OfflineStrike_AnswersPendingButGetsNoNextCheckpoint()
        {
            var result = Create(Pending("B")).OnWandStrike(Strike("B", InWindowUtc, offline: true));

            Assert.IsNull(result);
            var r = _resolved.Single();
            Assert.AreEqual(PatrolRandomisationLogStatus.Completed, r.Status);
            Assert.AreEqual(true, r.ScannedIsOffline);
            Assert.AreEqual(0, _added.Count);
        }

        [TestMethod]
        public void OfflineStrikeWithoutInstruction_IsLoggedUnguided()
        {
            Create().OnWandStrike(Strike("C", InWindowUtc, offline: true));

            var row = _added.Single();
            Assert.AreEqual(PatrolRandomisationLogStatus.Unguided, row.Status);
            Assert.AreEqual("Yard", row.ScannedTagLabel);
            Assert.AreEqual(true, row.ScannedIsOffline);
            Assert.IsNull(row.AssignedTagUId);
        }

        [TestMethod]
        public void OfflineStrikeOutsideWindow_IsNotLogged()
        {
            Create().OnWandStrike(Strike("C", OutOfWindowUtc, offline: true));
            Assert.AreEqual(0, _added.Count);
        }

        [TestMethod]
        public void NonStndSite_IsNotRandomised()
        {
            var result = Create(mode: PatrolTouringMode.PCAR).OnWandStrike(Strike("A", InWindowUtc));

            Assert.IsNull(result);
            Assert.AreEqual(0, _added.Count);
        }
    

        [TestMethod]
        public void LiveBypass_RecordsBypassedAndSendsAnotherPoint()
        {
            var result = Create(Pending("B")).Bypass(new PatrolRandomisationBypassRequest
            {
                ClientSiteId = SiteId, GuardId = GuardId, UserId = UserId, TagUId = "B", BypassUtc = InWindowUtc
            });

            Assert.IsFalse(result.AlreadyRecorded);
            Assert.AreEqual("Dock", result.BypassedTagLabel);
            Assert.AreEqual(PatrolRandomisationLogStatus.Bypassed, _resolved.Single().Status);
            Assert.AreEqual("Bypassed", result.Instruction.PreviousCheckpointResult);
            Assert.IsNotNull(result.Instruction.NextCheckpoint);
            Assert.AreNotEqual("B", result.Instruction.NextCheckpoint.TagUId);
        }

        [TestMethod]
        public void BypassResend_IsRecognisedAndChangesNothing()
        {
            _guardLogs = new List<ClientSitePatrolRandomisationLog>
            {
                new() { Id = 7, AssignedTagUId = "B", Status = PatrolRandomisationLogStatus.Bypassed, ScannedUtc = InWindowUtc }
            };
            var result = Create().Bypass(new PatrolRandomisationBypassRequest
            {
                ClientSiteId = SiteId, GuardId = GuardId, TagUId = "B", BypassUtc = InWindowUtc, IsOffline = true
            });
            _guardLogs = null;

            Assert.IsTrue(result.AlreadyRecorded);
            Assert.AreEqual(0, _added.Count);
            Assert.AreEqual(0, _resolved.Count);
        }

        [TestMethod]
        public void OfflineStrike_WhenHandsetShowedOtherPoint_InfersBypassAndJudgesAgainstIt()
        {
            var strike = Strike("C", InWindowUtc, offline: true);
            strike.ExpectedTagUId = "C";
            strike.ExpectedAssignedUtc = InWindowUtc.AddMinutes(-3);
            Create(Pending("B")).OnWandStrike(strike);

            Assert.AreEqual(PatrolRandomisationLogStatus.Bypassed, _resolved.Single().Status);
            var row = _added.Single();
            Assert.AreEqual("C", row.AssignedTagUId);
            Assert.AreEqual(PatrolRandomisationLogStatus.Completed, row.Status);
            Assert.AreEqual(true, row.ScannedIsOffline);
        }

        [TestMethod]
        public void LiveStrike_WhenHandsetMissedAnInstruction_CancelsIt()
        {
            var strike = Strike("C", InWindowUtc);
            strike.ExpectedTagUId = "C";
            Create(Pending("B")).OnWandStrike(strike);

            Assert.AreEqual(PatrolRandomisationLogStatus.Cancelled, _resolved.Single().Status);
        }

        [TestMethod]
        public void OfflineStrike_WithHandsetPickedNext_LeavesItPending()
        {
            var strike = Strike("B", InWindowUtc, offline: true);
            strike.NextTagUId = "A";
            strike.NextAssignedUtc = InWindowUtc;
            Create(Pending("B")).OnWandStrike(strike);

            Assert.AreEqual(PatrolRandomisationLogStatus.Completed, _resolved.Single().Status);
            var next = _added.Single();
            Assert.AreEqual(PatrolRandomisationLogStatus.Pending, next.Status);
            Assert.AreEqual("A", next.AssignedTagUId);
            Assert.AreEqual("B", next.FromTagUId);
            Assert.AreEqual(true, next.FromIsOffline);
        }

        [TestMethod]
        public void Instruction_CarriesWindowsAndPriorityForOfflineUse()
        {
            var result = Create().GetStatus(SiteId, GuardId, InWindowUtc);

            CollectionAssert.AreEqual(new[] { "C" }, result.PriorityTagUIds);
            Assert.IsTrue(result.UpcomingWindowsUtc.Count >= 2);
            Assert.IsTrue(result.UpcomingWindowsUtc.All(w => w.EndUtc > w.StartUtc));
        }
    }
}
