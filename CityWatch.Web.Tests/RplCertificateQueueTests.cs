using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Covers the daily RPL run behind /api/RPLCertificate/RPLCertificate, which a Windows scheduled
    /// task calls once a day.
    ///
    /// GenerateRPLCertificate treats TrainingCourseCertificateRPL as a work queue: rows with
    /// isDeleted = 0 and an AssessmentEndDate in the past are issued and then marked done. Marking
    /// them done used to re-derive a certificate document from the course and only fire when that
    /// document had isRPLEnabled set, so rows that did not match were re-issued - PDF, Dropbox upload,
    /// a fresh compliance record and a "New Certificate Issued" email - every single day. The two
    /// mismatch shapes below are taken from live data.
    ///
    /// ICertificateGenerator is mocked, so no PDF is built and nothing is uploaded, and the alert
    /// email list is empty so no message is addressed.
    /// </summary>
    [TestClass]
    public class RplCertificateQueueTests
    {
        private const int CourseId = 105;
        private const int GuardId = 1586;

        private Mock<IGuardDataProvider> _guardDataProvider;
        private Mock<IGuardLogDataProvider> _guardLogDataProvider;
        private Mock<IConfigDataProvider> _configDataProvider;

        private static TrainingCourseCertificateRPL Row(int id, int guardId, int certificateDocumentId) =>
            new TrainingCourseCertificateRPL
            {
                Id = id,
                GuardId = guardId,
                TrainingCourseCertificateId = certificateDocumentId,
                AssessmentStartDate = new DateTime(2025, 8, 1),
                AssessmentEndDate = new DateTime(2025, 8, 19),   // well in the past - due
                FileName = "assessment-evidence.pdf"
            };

        /// <param name="certificateDocuments">
        /// The course's certificate documents. Order matters: the old code took the first.
        /// </param>
        private RPLCertificateGeneratorService CreateService(
            List<TrainingCourseCertificateRPL> queue,
            List<TrainingCourseCertificate> certificateDocuments)
        {
            _guardDataProvider = new Mock<IGuardDataProvider>();
            _guardLogDataProvider = new Mock<IGuardLogDataProvider>();
            _configDataProvider = new Mock<IConfigDataProvider>();

            // Mirrors the provider, which filters out consumed rows.
            _guardDataProvider.Setup(z => z.GetCourseCertificateRPL())
                .Returns(() => queue.Where(x => !x.isDeleted).ToList());

            // Consuming a row writes it back with isDeleted set; reflect that in the queue so a
            // second run sees what the real provider would.
            _guardLogDataProvider.Setup(z => z.SaveTrainingCourseCertificateRPL(It.IsAny<TrainingCourseCertificateRPL>()))
                .Callback<TrainingCourseCertificateRPL>(saved =>
                {
                    var existing = queue.FirstOrDefault(x => x.Id == saved.Id);
                    if (existing != null)
                    {
                        existing.isDeleted = saved.isDeleted;
                        existing.FileName = saved.FileName;
                    }
                });

            var course = new HrSettings
            {
                Id = CourseId,
                Description = "Thermal Camera (FLIR Ti)",
                HRGroupId = 3,
                ReferenceNoNumbers = new ReferenceNoNumbers { Name = "03" },
                ReferenceNoAlphabets = new ReferenceNoAlphabets { Name = "e" }
            };

            _configDataProvider.Setup(z => z.GetHRSettings()).Returns(new List<HrSettings> { course });
            _configDataProvider.Setup(z => z.GetCourseCertificateDocuments()).Returns(certificateDocuments);
            _configDataProvider.Setup(z => z.GetCourseCertificateDocsUsingSettingsId(CourseId)).Returns(certificateDocuments);
            _configDataProvider.Setup(z => z.GetTQSettings(CourseId))
                .Returns(new List<TrainingTestQuestionSettings> { new TrainingTestQuestionSettings { Id = 1, HRSettingsId = CourseId } });
            _configDataProvider.Setup(z => z.GetTrainingCoursesWithHrSettingsId(CourseId)).Returns(new List<TrainingCourses>());

            _guardDataProvider.Setup(z => z.GetGuardDetailsUsingId(It.IsAny<int>()))
                .Returns(new List<Guard> { new Guard { Id = GuardId, Name = "A Guard", SecurityNo = "1" } });

            var certificateGenerator = new Mock<ICertificateGenerator>();
            certificateGenerator
                .Setup(z => z.GeneratePdf(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns("certificate.pdf");

            // No recipients, so the notification has nowhere to go.
            var clientDataProvider = new Mock<IClientDataProvider>();
            clientDataProvider.Setup(z => z.GetGlobalComplianceAlertEmail()).Returns(new List<GlobalComplianceAlertEmail>());

            return new RPLCertificateGeneratorService(
                _guardLogDataProvider.Object,
                _guardDataProvider.Object,
                _configDataProvider.Object,
                certificateGenerator.Object,
                Options.Create(new CityWatch.Data.Helpers.EmailOptions { FromAddress = "noreply@test|CityWatch" }),
                clientDataProvider.Object,
                NullLogger<RPLCertificateGeneratorService>.Instance);
        }

        /* ---------------- the two live mismatch shapes ---------------- */

        /// <summary>
        /// "Thermal Camera (FLIR Ti)": the course's certificate document has isRPLEnabled = 0, so the
        /// old consumption block never ran at all. 13 rows were stuck in this state.
        /// </summary>
        [TestMethod]
        public void DueRow_WhoseCertificateDocumentHasRplDisabled_IsStillConsumed()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 14) };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = false }
                });

            service.GenerateRPLCertificate();

            Assert.IsTrue(queue.Single().isDeleted,
                "The row must be marked done regardless of the document's isRPLEnabled flag.");
        }

        /// <summary>
        /// "DashCAM - Martha Cove": the rows point at one certificate document while the course
        /// resolves to another, so the old lookup by document id found nothing to mark.
        /// </summary>
        [TestMethod]
        public void DueRow_PointingAtADifferentDocumentOfTheSameCourse_IsStillConsumed()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(24, GuardId, 21) };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    // The course resolves to 22 first; the queue row points at 21.
                    new TrainingCourseCertificate { Id = 22, HRSettingsId = CourseId, isRPLEnabled = true },
                    new TrainingCourseCertificate { Id = 21, HRSettingsId = CourseId, isRPLEnabled = true }
                });

            service.GenerateRPLCertificate();

            Assert.IsTrue(queue.Single().isDeleted, "The row processed is the row that must be marked done.");
        }

        /* ---------------- the symptom ---------------- */

        /// <summary>
        /// The reported problem: the scheduler runs daily, so a row that is not consumed produces a
        /// certificate and an email every day forever.
        /// </summary>
        [TestMethod]
        public void RunningTheSchedulerTwice_DoesNotIssueTheSameCertificateTwice()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 14) };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = false }
                });

            service.GenerateRPLCertificate();   // today
            service.GenerateRPLCertificate();   // tomorrow

            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(It.IsAny<GuardComplianceAndLicense>()),
                Times.Once, "A second daily run must not add another compliance record for the same assessment.");
        }

        /// <summary>Consumption must mark only the row being processed, not every row for that guard.</summary>
        [TestMethod]
        public void ConsumingOneRow_LeavesTheGuardsOtherAssessmentsAlone()
        {
            var future = Row(99, GuardId, 14);
            future.AssessmentEndDate = DateTime.Now.Date.AddDays(30);   // not due yet

            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 14), future };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = true }
                });

            service.GenerateRPLCertificate();

            Assert.IsTrue(queue.Single(x => x.Id == 13).isDeleted, "The due row is done.");
            Assert.IsFalse(queue.Single(x => x.Id == 99).isDeleted, "An assessment that has not ended must be left alone.");
        }

        /// <summary>
        /// SaveTrainingCourseCertificateRPL overwrites every column it is given. The old consumption
        /// left FileName off the object it built, which blanked the assessment evidence file name.
        /// </summary>
        [TestMethod]
        public void ConsumingARow_KeepsItsEvidenceFileName()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 14) };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = true }
                });

            service.GenerateRPLCertificate();

            Assert.AreEqual("assessment-evidence.pdf", queue.Single().FileName);
        }

        /* ---------------- rows that must not be touched ---------------- */

        [TestMethod]
        public void RowWithNoCertificateDocument_IsSkippedAndLeftForInvestigation()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 999) };
            var service = CreateService(queue, new List<TrainingCourseCertificate>());

            service.GenerateRPLCertificate();

            Assert.IsFalse(queue.Single().isDeleted, "Nothing was issued, so nothing may be marked done.");
            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(It.IsAny<GuardComplianceAndLicense>()), Times.Never);
        }

        [TestMethod]
        public void RowWhoseCourseHasNoTestQuestionSettings_IsNotConsumed()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 14) };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = true }
                });

            // The course is unconfigured, so IssueCertificateForGuard fails fast.
            _configDataProvider.Setup(z => z.GetTQSettings(CourseId)).Returns(new List<TrainingTestQuestionSettings>());

            service.GenerateRPLCertificate();

            Assert.IsFalse(queue.Single().isDeleted, "A failed issue must stay on the queue.");
            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(It.IsAny<GuardComplianceAndLicense>()), Times.Never);
        }

        /// <summary>
        /// The Bulk Certificate Release and the admin release issue on demand, with no queue row -
        /// they must not consume one belonging to the same guard and course.
        /// </summary>
        [TestMethod]
        public void OnDemandRelease_DoesNotConsumeAnyQueueRow()
        {
            var queue = new List<TrainingCourseCertificateRPL> { Row(13, GuardId, 14) };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = true }
                });

            service.IssueCertificateForGuard(GuardId, CourseId);

            Assert.IsFalse(queue.Single().isDeleted, "The guard's pending RPL assessment is unrelated to an on-demand release.");
            _guardLogDataProvider.Verify(z => z.SaveTrainingCourseCertificateRPL(It.IsAny<TrainingCourseCertificateRPL>()), Times.Never);
            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(It.IsAny<GuardComplianceAndLicense>()), Times.Once);
        }

        /// <summary>One bad row must not stop the rest of the daily run.</summary>
        [TestMethod]
        public void OneUnissuableRow_DoesNotStopTheRest()
        {
            var queue = new List<TrainingCourseCertificateRPL>
            {
                Row(1, GuardId, 999),   // no certificate document
                Row(2, GuardId, 14),
                Row(3, GuardId, 14)
            };
            var service = CreateService(queue,
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 14, HRSettingsId = CourseId, isRPLEnabled = true }
                });

            service.GenerateRPLCertificate();

            Assert.IsFalse(queue.Single(x => x.Id == 1).isDeleted);
            Assert.IsTrue(queue.Single(x => x.Id == 2).isDeleted);
            Assert.IsTrue(queue.Single(x => x.Id == 3).isDeleted);
        }
    }
}
