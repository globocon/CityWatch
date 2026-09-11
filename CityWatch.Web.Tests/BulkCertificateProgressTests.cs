using CityWatch.Data.Models;
using CityWatch.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Covers the progress reporting behind the Produce button: the plan the server builds from the
    /// posted ids, the per-certificate progress a poller sees while the run is in flight, and Stop.
    ///
    /// IRPLCertificateGeneratorService is mocked throughout, so nothing is generated, uploaded to
    /// Dropbox or emailed.
    /// </summary>
    [TestClass]
    public class BulkCertificateProgressTests
    {
        private Mock<IRPLCertificateGeneratorService> _certificateService;

        private static Guard MakeGuard(int id, string name, string initial) =>
            new Guard { Id = id, Name = name, Initial = initial, IsActive = true };

        private static HrSettings MakeCourse(int id, string description) =>
            new HrSettings { Id = id, Description = description };

        private static BulkCertificateJob JobFor(IEnumerable<Guard> guards, IEnumerable<HrSettings> courses,
            int[] guardIds, int[] courseIds)
        {
            var plan = BulkCertificateRelease.BuildPlan(guards, courses, guardIds, courseIds);
            Assert.IsTrue(plan.IsValid, plan.Message);
            return new BulkCertificateJob { Pairings = plan.Pairings };
        }

        [TestInitialize]
        public void Setup() => _certificateService = new Mock<IRPLCertificateGeneratorService>();

        /* ---------------- the plan ---------------- */

        [TestMethod]
        public void Plan_PairsEveryGuardWithEveryCourse()
        {
            var plan = BulkCertificateRelease.BuildPlan(
                new[] { MakeGuard(1, "A", "A.A"), MakeGuard(2, "B", "B.B") },
                new[] { MakeCourse(10, "Level 2"), MakeCourse(11, "Level 3"), MakeCourse(12, "Level 5") },
                new[] { 1, 2 }, new[] { 10, 11, 12 });

            Assert.IsTrue(plan.IsValid);
            Assert.AreEqual(6, plan.Pairings.Count, "Two guards times three courses.");
        }

        [TestMethod]
        public void Plan_RejectsAnEmptySelection_WithTheMessageTheOperatorSees()
        {
            var guards = new[] { MakeGuard(1, "A", "A.A") };
            var courses = new[] { MakeCourse(10, "Level 2") };

            Assert.AreEqual("Please select at least one guard.",
                BulkCertificateRelease.BuildPlan(guards, courses, new int[0], new[] { 10 }).Message);
            Assert.AreEqual("Please select a course certificate.",
                BulkCertificateRelease.BuildPlan(guards, courses, new[] { 1 }, new int[0]).Message);
            Assert.AreEqual("None of the selected guards are active.",
                BulkCertificateRelease.BuildPlan(guards, courses, new[] { 999 }, new[] { 10 }).Message);
            Assert.AreEqual("The selected course certificates no longer exist.",
                BulkCertificateRelease.BuildPlan(guards, courses, new[] { 1 }, new[] { 999 }).Message);
        }

        /* ---------------- progress ---------------- */

        [TestMethod]
        public void Progress_TotalIsKnownBeforeAnyCertificateIsIssued()
        {
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A"), MakeGuard(2, "B", "B.B") },
                             new[] { MakeCourse(10, "Level 2") }, new[] { 1, 2 }, new[] { 10 });

            var progress = job.ToProgress();

            Assert.AreEqual(2, progress.Total, "The bar needs a denominator before the run starts.");
            Assert.AreEqual(0, progress.Completed);
            Assert.AreEqual(0, progress.PercentComplete);
            Assert.AreEqual("Queued", progress.Status);
            Assert.IsFalse(progress.IsTerminal);
        }

        /// <summary>
        /// The point of the whole exercise: a poller must see the count and the percentage move as
        /// each certificate finishes, not just at the end.
        /// </summary>
        [TestMethod]
        public void Progress_AdvancesAsEachCertificateIsIssued()
        {
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A"), MakeGuard(2, "B", "B.B"), MakeGuard(3, "C", "C.C"), MakeGuard(4, "D", "D.D") },
                             new[] { MakeCourse(10, "Level 2") }, new[] { 1, 2, 3, 4 }, new[] { 10 });

            var seen = new List<BulkCertificateProgress>();

            // Snapshot from inside the run, exactly where a poll would land.
            _certificateService.Setup(z => z.IssueCertificateForGuard(It.IsAny<int>(), 10))
                .Callback(() => seen.Add(job.ToProgress()));

            BulkCertificateRelease.Run(job, _certificateService.Object, NullLogger.Instance);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, seen.Select(p => p.Completed).ToArray(),
                "Each certificate must be visible to a poller as it finishes.");
            CollectionAssert.AreEqual(new[] { 0, 25, 50, 75 }, seen.Select(p => p.PercentComplete).ToArray());

            var final = job.ToProgress();
            Assert.AreEqual(100, final.PercentComplete);
            Assert.AreEqual("Completed", final.Status);
            Assert.AreEqual(4, final.Issued);
            Assert.IsTrue(final.IsTerminal);
        }

        [TestMethod]
        public void Progress_NamesTheCertificateCurrentlyBeingBuilt()
        {
            var job = JobFor(new[] { MakeGuard(4, "Bruno Timpano", "B.T") },
                             new[] { MakeCourse(13, "Thermal Camera (FLIR Ti)") }, new[] { 4 }, new[] { 13 });

            string stepDuringRun = null;
            _certificateService.Setup(z => z.IssueCertificateForGuard(4, 13))
                .Callback(() => stepDuringRun = job.ToProgress().CurrentStep);

            BulkCertificateRelease.Run(job, _certificateService.Object, NullLogger.Instance);

            Assert.AreEqual("Bruno Timpano [B.T] - Thermal Camera (FLIR Ti)", stepDuringRun);
        }

        [TestMethod]
        public void Progress_NeverReports100UntilTheJobIsTerminal()
        {
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A") }, new[] { MakeCourse(10, "Level 2") },
                             new[] { 1 }, new[] { 10 });

            int? percentOnLastCertificate = null;
            _certificateService.Setup(z => z.IssueCertificateForGuard(1, 10))
                .Callback(() => percentOnLastCertificate = job.ToProgress().PercentComplete);

            BulkCertificateRelease.Run(job, _certificateService.Object, NullLogger.Instance);

            Assert.IsTrue(percentOnLastCertificate < 100, "A bar at 100% while work is still running reads as a hang.");
            Assert.AreEqual(100, job.ToProgress().PercentComplete);
        }

        [TestMethod]
        public void Progress_CarriesFailuresWithoutStoppingTheRun()
        {
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A"), MakeGuard(2, "B", "B.B"), MakeGuard(3, "C", "C.C") },
                             new[] { MakeCourse(10, "Level 2") }, new[] { 1, 2, 3 }, new[] { 10 });

            _certificateService.Setup(z => z.IssueCertificateForGuard(2, 10))
                .Throws(new InvalidOperationException("No Training/Test Question settings configured"));

            BulkCertificateRelease.Run(job, _certificateService.Object, NullLogger.Instance);

            var progress = job.ToProgress();
            Assert.AreEqual("Completed", progress.Status);
            Assert.AreEqual(2, progress.Issued);
            Assert.AreEqual(1, progress.Failed);
            Assert.AreEqual(3, progress.Results.Count);

            var failure = progress.Results.Single(r => !r.Success);
            StringAssert.Contains(failure.Status, "No Training/Test Question settings configured");
            _certificateService.Verify(z => z.IssueCertificateForGuard(3, 10), Times.Once);
        }

        [TestMethod]
        public void Progress_EstimatesRemainingTimeOnlyOnceSomethingHasFinished()
        {
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A"), MakeGuard(2, "B", "B.B") },
                             new[] { MakeCourse(10, "Level 2") }, new[] { 1, 2 }, new[] { 10 });

            Assert.IsNull(job.ToProgress().EstimatedRemainingSeconds, "Nothing to average yet.");

            double?[] estimates = new double?[2];
            var index = 0;
            _certificateService.Setup(z => z.IssueCertificateForGuard(It.IsAny<int>(), 10))
                .Callback(() =>
                {
                    estimates[index++] = job.ToProgress().EstimatedRemainingSeconds;
                    Thread.Sleep(20);   // give the elapsed clock something to measure
                });

            BulkCertificateRelease.Run(job, _certificateService.Object, NullLogger.Instance);

            Assert.IsNull(estimates[0], "No estimate before the first certificate completes.");
            Assert.IsNotNull(estimates[1], "One completed certificate is enough to project the rest.");
            Assert.IsNull(job.ToProgress().EstimatedRemainingSeconds, "A finished job has nothing remaining.");
        }

        /* ---------------- stop ---------------- */

        [TestMethod]
        public void Stop_HaltsAfterTheCertificateInFlight_AndKeepsWhatWasIssued()
        {
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A"), MakeGuard(2, "B", "B.B"), MakeGuard(3, "C", "C.C") },
                             new[] { MakeCourse(10, "Level 2") }, new[] { 1, 2, 3 }, new[] { 10 });

            // Stop pressed while the first certificate is being built.
            _certificateService.Setup(z => z.IssueCertificateForGuard(1, 10))
                .Callback(() => job.Cancellation.Cancel());

            BulkCertificateRelease.Run(job, _certificateService.Object, NullLogger.Instance);

            var progress = job.ToProgress();
            Assert.AreEqual("Cancelled", progress.Status);
            Assert.AreEqual(1, progress.Issued, "The certificate already in flight still counts - it exists.");
            Assert.IsTrue(progress.IsTerminal);

            _certificateService.Verify(z => z.IssueCertificateForGuard(2, 10), Times.Never);
            _certificateService.Verify(z => z.IssueCertificateForGuard(3, 10), Times.Never);
        }

        /* ---------------- job store ---------------- */

        [TestMethod]
        public void JobStore_ReturnsAJobByIdAndNullForAnythingElse()
        {
            var store = new BulkCertificateJobStore();
            var job = JobFor(new[] { MakeGuard(1, "A", "A.A") }, new[] { MakeCourse(10, "Level 2") },
                             new[] { 1 }, new[] { 10 });

            store.Add(job);

            Assert.AreSame(job, store.Get(job.JobId));
            Assert.IsNull(store.Get("not-a-job"), "An expired or unknown id must not throw.");
            Assert.IsNull(store.Get(null));
        }
    }
}
