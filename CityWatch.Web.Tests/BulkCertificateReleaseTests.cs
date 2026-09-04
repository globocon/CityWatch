using CityWatch.Data;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Common.Services;
using CityWatch.Web.Pages.Admin;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Covers the Bulk Certificate Release handlers on /Admin/Settings.
    ///
    /// IRPLCertificateGeneratorService is mocked, which is what keeps this round of testing free of
    /// side effects: the real service generates a certificate PDF, uploads it to Dropbox and sends a
    /// course-completed email. Mocking it means none of that happens, while still proving that the
    /// bulk operation calls the single-guard issuing logic once per guard/course pairing.
    /// </summary>
    [TestClass]
    public class BulkCertificateReleaseTests
    {
        private Mock<IRPLCertificateGeneratorService> _certificateService;
        private Mock<IGuardDataProvider> _guardDataProvider;
        private Mock<IConfigDataProvider> _configDataProvider;

        private static Guard MakeGuard(int id, string name, string initial, string securityNo) =>
            new Guard { Id = id, Name = name, Initial = initial, SecurityNo = securityNo, IsActive = true };

        private static HrSettings MakeCourse(int id, string description) =>
            new HrSettings { Id = id, Description = description };

        private SettingsModel CreateModel(List<Guard> activeGuards, List<HrSettings> courses)
        {
            _certificateService = new Mock<IRPLCertificateGeneratorService>();
            _guardDataProvider = new Mock<IGuardDataProvider>();
            _configDataProvider = new Mock<IConfigDataProvider>();

            _guardDataProvider.Setup(z => z.GetActiveGuards()).Returns(activeGuards);
            _configDataProvider.Setup(z => z.GetHRSettings()).Returns(courses);

            var webHostEnvironment = new Mock<IWebHostEnvironment>();
            webHostEnvironment.SetupGet(z => z.WebRootPath).Returns(@"C:\wwwroot");

            // Never connected to - the handlers under test do not touch the context.
            var dbContext = new CityWatchDbContext(
                new DbContextOptionsBuilder<CityWatchDbContext>().UseSqlServer("Server=(local);Database=none").Options,
                null);

            return new SettingsModel(
                webHostEnvironment.Object,
                Mock.Of<IClientDataProvider>(),
                _configDataProvider.Object,
                Mock.Of<IUserDataProvider>(),
                Mock.Of<IViewDataService>(),
                Mock.Of<IGuardLogDataProvider>(),
                Mock.Of<ITimesheetReportGenerator>(),
                _guardDataProvider.Object,
                Options.Create(new CityWatch.Web.Helpers.Settings()),
                Mock.Of<IDropboxService>(),
                Mock.Of<ICertificateGenerator>(),
                Options.Create(new EmailOptions()),
                dbContext,
                _certificateService.Object,
                // The progress-tracked path is covered separately; the synchronous handler under
                // test here runs the release inline through the mock above.
                Mock.Of<IBulkCertificateReleaseService>(),
                Mock.Of<ILogger<SettingsModel>>());
        }

        private static dynamic Payload(JsonResult result) => result.Value;

        private static int PropInt(object o, string name) => (int)o.GetType().GetProperty(name).GetValue(o);
        private static bool PropBool(object o, string name) => (bool)o.GetType().GetProperty(name).GetValue(o);
        private static string PropString(object o, string name) => (string)o.GetType().GetProperty(name).GetValue(o);
        private static IEnumerable<object> PropResults(object o) =>
            ((System.Collections.IEnumerable)o.GetType().GetProperty("results").GetValue(o)).Cast<object>();

        /* ---------------- guard list ---------------- */

        [TestMethod]
        public void GuardList_ReturnsIdNameInitialAndSecurityNo()
        {
            var model = CreateModel(
                new List<Guard> { MakeGuard(1, "Nihar Guptha", "N.G", "123-xcf-396") },
                new List<HrSettings>());

            var payload = ((JsonResult)model.OnGetBulkCertReleaseGuards()).Value;
            var first = ((System.Collections.IEnumerable)payload).Cast<object>().Single();

            Assert.AreEqual("Nihar Guptha", PropString(first, "Name"));
            Assert.AreEqual("N.G", PropString(first, "Initial"));
            Assert.AreEqual("123-xcf-396", PropString(first, "SecurityNo"));
        }

        /* ---------------- validation ---------------- */

        [TestMethod]
        public void NoGuardSelected_IsRejected_AndNoCertificateIssued()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new int[0], new[] { 10 })).Value;

            Assert.IsFalse(PropBool(payload, "success"));
            Assert.AreEqual("Please select at least one guard.", PropString(payload, "message"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void NoCourseSelected_IsRejected_AndNoCertificateIssued()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, new int[0])).Value;

            Assert.IsFalse(PropBool(payload, "success"));
            Assert.AreEqual("Please select a course certificate.", PropString(payload, "message"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void NullArguments_AreRejected()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            Assert.IsFalse(PropBool(((JsonResult)model.OnPostBulkReleaseCertificates(null, new[] { 10 })).Value, "success"));
            Assert.IsFalse(PropBool(((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, null)).Value, "success"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        /* ---------------- server side id validation ---------------- */

        [TestMethod]
        public void UnknownGuardId_FromBrowser_IsIgnored()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            // 999 is not an active guard.
            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1, 999 }, new[] { 10 })).Value;

            Assert.AreEqual(1, PropInt(payload, "issued"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(1, 10), Times.Once);
            _certificateService.Verify(z => z.IssueCertificateForGuard(999, It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void UnknownCourseId_FromBrowser_IsIgnored()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, new[] { 10, 888 })).Value;

            Assert.AreEqual(1, PropInt(payload, "issued"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(1, 888), Times.Never);
        }

        [TestMethod]
        public void AllGuardsInactive_IsRejected()
        {
            var model = CreateModel(new List<Guard>(), new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, new[] { 10 })).Value;

            Assert.IsFalse(PropBool(payload, "success"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        /* ---------------- duplicate prevention ---------------- */

        [TestMethod]
        public void RepeatedGuardOrCourseId_IssuesOnlyOnce()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1, 1, 1 }, new[] { 10, 10 })).Value;

            Assert.AreEqual(1, PropInt(payload, "issued"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(1, 10), Times.Once);
        }

        /* ---------------- the bulk pairing ---------------- */

        [TestMethod]
        public void OneGuardOneCourse_IssuesOnce()
        {
            var model = CreateModel(new List<Guard> { MakeGuard(1, "A", "A.A", "1") },
                                    new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, new[] { 10 })).Value;

            Assert.IsTrue(PropBool(payload, "success"));
            Assert.AreEqual(1, PropInt(payload, "issued"));
            Assert.AreEqual(0, PropInt(payload, "failed"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(1, 10), Times.Once);
        }

        [TestMethod]
        public void TwoGuardsOneCourse_IssuesTwice()
        {
            var model = CreateModel(
                new List<Guard> { MakeGuard(1, "A", "A.A", "1"), MakeGuard(2, "B", "B.B", "2") },
                new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1, 2 }, new[] { 10 })).Value;

            Assert.AreEqual(2, PropInt(payload, "issued"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(1, 10), Times.Once);
            _certificateService.Verify(z => z.IssueCertificateForGuard(2, 10), Times.Once);
        }

        [TestMethod]
        public void ManyGuardsMultipleCourses_IssuesEveryPairing()
        {
            var guards = Enumerable.Range(1, 12).Select(i => MakeGuard(i, "Guard " + i, "G" + i, "S" + i)).ToList();
            var courses = new List<HrSettings> { MakeCourse(10, "Level 2"), MakeCourse(11, "Level 4"), MakeCourse(12, "Level 5") };
            var model = CreateModel(guards, courses);

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(
                guards.Select(g => g.Id).ToArray(), new[] { 10, 11, 12 })).Value;

            Assert.AreEqual(36, PropInt(payload, "issued"), "12 guards x 3 courses");
            Assert.AreEqual(0, PropInt(payload, "failed"));
            _certificateService.Verify(z => z.IssueCertificateForGuard(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(36));
        }

        /* ---------------- failure isolation ---------------- */

        [TestMethod]
        public void OneGuardFailing_DoesNotStopTheOthers()
        {
            var model = CreateModel(
                new List<Guard> { MakeGuard(1, "A", "A.A", "1"), MakeGuard(2, "B", "B.B", "2"), MakeGuard(3, "C", "C.C", "3") },
                new List<HrSettings> { MakeCourse(10, "Level 2") });

            _certificateService.Setup(z => z.IssueCertificateForGuard(2, 10))
                .Throws(new InvalidOperationException("already certified"));

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1, 2, 3 }, new[] { 10 })).Value;

            Assert.IsTrue(PropBool(payload, "success"));
            Assert.AreEqual(2, PropInt(payload, "issued"));
            Assert.AreEqual(1, PropInt(payload, "failed"));

            // Guard 3 was still attempted after guard 2 threw.
            _certificateService.Verify(z => z.IssueCertificateForGuard(3, 10), Times.Once);

            var failure = PropResults(payload).Single(r => !PropBool(r, "success"));
            StringAssert.Contains(PropString(failure, "status"), "already certified");
        }

        /* ---------------- reporting ---------------- */

        [TestMethod]
        public void ResultRows_CarryGuardLabelWithInitialAndCourseName()
        {
            var model = CreateModel(
                new List<Guard> { MakeGuard(1, "Nihar Guptha", "N.G", "123-xcf-396") },
                new List<HrSettings> { MakeCourse(10, "C4i System Training - Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, new[] { 10 })).Value;
            var row = PropResults(payload).Single();

            Assert.AreEqual("Nihar Guptha [N.G]", PropString(row, "guard"));
            Assert.AreEqual("C4i System Training - Level 2", PropString(row, "course"));
            Assert.IsTrue(PropBool(row, "success"));
        }

        /// <summary>
        /// A course with no TrainingTestQuestionSettings row (e.g. "Martha Cove - Alarm Faults") used to
        /// throw NullReferenceException out of RPLCertificateGeneratorService. The bulk run must report
        /// it against that guard/course and keep processing the rest.
        /// </summary>
        [TestMethod]
        public void CourseWithoutTestQuestionSettings_IsReportedAndDoesNotStopTheRun()
        {
            var model = CreateModel(
                new List<Guard> { MakeGuard(1, "Aaryan Bajaj", "A.B", "Z85-202-10S"), MakeGuard(2, "Abbas Syed", "A.S", "410378680") },
                new List<HrSettings> { MakeCourse(94, "Martha Cove - Alarm Faults"), MakeCourse(73, "Martha Cove - CAMS Operations") });

            // Course 94 has no settings row - the service now throws a descriptive error for it.
            _certificateService.Setup(z => z.IssueCertificateForGuard(It.IsAny<int>(), 94))
                .Throws(new InvalidOperationException(
                    "No Training/Test Question settings configured for course 'Martha Cove - Alarm Faults', so a certificate cannot be issued."));

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1, 2 }, new[] { 94, 73 })).Value;

            Assert.IsTrue(PropBool(payload, "success"), "The run itself must complete, not blow up.");
            Assert.AreEqual(2, PropInt(payload, "issued"), "Both guards still get the configured course.");
            Assert.AreEqual(2, PropInt(payload, "failed"), "Both guards fail on the unconfigured course.");

            // The configured course was still attempted for both guards after the failures.
            _certificateService.Verify(z => z.IssueCertificateForGuard(1, 73), Times.Once);
            _certificateService.Verify(z => z.IssueCertificateForGuard(2, 73), Times.Once);

            var failures = PropResults(payload).Where(r => !PropBool(r, "success")).ToList();
            Assert.AreEqual(2, failures.Count);
            foreach (var failure in failures)
            {
                Assert.AreEqual("Martha Cove - Alarm Faults", PropString(failure, "course"));
                StringAssert.Contains(PropString(failure, "status"), "No Training/Test Question settings configured");
            }
        }

        [TestMethod]
        public void GuardWithoutInitial_FallsBackToNameOnly()
        {
            var model = CreateModel(
                new List<Guard> { MakeGuard(1, "No Initial", null, "1") },
                new List<HrSettings> { MakeCourse(10, "Level 2") });

            var payload = ((JsonResult)model.OnPostBulkReleaseCertificates(new[] { 1 }, new[] { 10 })).Value;
            var row = PropResults(payload).Single();

            Assert.AreEqual("No Initial", PropString(row, "guard"));
        }
    }
}
