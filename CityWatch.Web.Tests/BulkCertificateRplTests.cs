using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// RPL in the Bulk Certificate Release.
    ///
    /// Two separate things are proved here, because they failed separately in production:
    ///
    ///  - the release records an RPL assessment for a course that is set up for RPL, using the one
    ///    set of details captured by rplDetailsModal for every guard in the run, and leaves non-RPL
    ///    courses exactly as they were;
    ///  - the course is recorded against the guard under its full name ("03e Thermal Camera
    ///    (FLIR Ti)"), not the bare description the RPL path used to store.
    ///
    /// The real generator is exercised for the naming and assessment tests with every provider
    /// mocked, so no PDF is built, nothing reaches Dropbox and no email is sent.
    /// </summary>
    [TestClass]
    public class BulkCertificateRplTests
    {
        private const int ThermalCameraCourseId = 105;      // 03e Thermal Camera (FLIR Ti)
        private const int ThermalCameraCertificateId = 14;
        private const int NonRplCourseId = 77;
        private const int BrunoGuardId = 1;
        private const int JohnGuardId = 2;

        private Mock<IGuardLogDataProvider> _guardLogDataProvider;
        private Mock<IGuardDataProvider> _guardDataProvider;
        private Mock<IConfigDataProvider> _configDataProvider;
        private Mock<ICertificateGenerator> _certificateGenerator;

        private static HrSettings MakeCourse(int id, string referenceNumber, string referenceLetter, string description) =>
            new HrSettings
            {
                Id = id,
                Description = description,
                HRGroupId = 1,
                ReferenceNoNumbers = referenceNumber == null ? null : new ReferenceNoNumbers { Name = referenceNumber },
                ReferenceNoAlphabets = referenceLetter == null ? null : new ReferenceNoAlphabets { Name = referenceLetter }
            };

        private static Guard MakeGuard(int id, string name, string initial) =>
            new Guard { Id = id, Name = name, Initial = initial, SecurityNo = "L" + id, IsActive = true };

        private static RplAssessmentDetails MakeDetails() => new RplAssessmentDetails
        {
            TrainingTheoryLocationId = 3,
            TrainingPracticalLocationId = 4,
            TrainingInstructorId = 5,
            AssessmentStartDate = new DateTime(2026, 9, 1),
            AssessmentEndDate = new DateTime(2026, 9, 4),
            FileName = "rpl-evidence.pdf"
        };

        /// <summary>
        /// A generator whose every dependency is mocked. Enough is stubbed for a release to run to
        /// completion: the course, its certificate document, its TQ settings and the guard.
        /// </summary>
        private RPLCertificateGeneratorService CreateGenerator(List<HrSettings> courses,
            List<TrainingCourseCertificate> certificateDocuments,
            List<TrainingCourseCertificateRPL> existingAssessments = null)
        {
            _guardLogDataProvider = new Mock<IGuardLogDataProvider>();
            _guardDataProvider = new Mock<IGuardDataProvider>();
            _configDataProvider = new Mock<IConfigDataProvider>();
            _certificateGenerator = new Mock<ICertificateGenerator>();

            _configDataProvider.Setup(z => z.GetHRSettings()).Returns(courses);

            foreach (var course in courses)
            {
                var id = course.Id;
                _configDataProvider.Setup(z => z.GetCourseCertificateDocsUsingSettingsId(id))
                    .Returns(certificateDocuments.Where(d => d.HRSettingsId == id).ToList());

                // Configured course: no practical hold, no Q&A dump, no expiry.
                _configDataProvider.Setup(z => z.GetTQSettings(id))
                    .Returns(new List<TrainingTestQuestionSettings> { new TrainingTestQuestionSettings { HRSettingsId = id } });

                _configDataProvider.Setup(z => z.GetTrainingCoursesWithHrSettingsId(id)).Returns(new List<TrainingCourses>());
            }

            foreach (var document in certificateDocuments)
            {
                var documentId = document.Id;
                _configDataProvider.Setup(z => z.GetCourseCertificateRPLUsingId(documentId))
                    .Returns((existingAssessments ?? new List<TrainingCourseCertificateRPL>())
                        .Where(a => a.TrainingCourseCertificateId == documentId).ToList());
            }

            _guardDataProvider.Setup(z => z.GetGuardDetailsUsingId(It.IsAny<int>()))
                .Returns(new List<Guard> { MakeGuard(BrunoGuardId, "Bruno Timpano", "B.T") });

            _certificateGenerator.Setup(z => z.GeneratePdf(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns("certificate.pdf");

            return new RPLCertificateGeneratorService(
                _guardLogDataProvider.Object,
                _guardDataProvider.Object,
                _configDataProvider.Object,
                _certificateGenerator.Object,
                Microsoft.Extensions.Options.Options.Create(new CityWatch.Data.Helpers.EmailOptions { FromAddress = "a@b.c|C4i" }),
                Mock.Of<IClientDataProvider>(),
                Mock.Of<ILogger<RPLCertificateGeneratorService>>());
        }

        private static List<TrainingCourseCertificate> ThermalCameraDocuments(bool rplEnabled) =>
            new List<TrainingCourseCertificate>
            {
                new TrainingCourseCertificate
                {
                    Id = ThermalCameraCertificateId,
                    HRSettingsId = ThermalCameraCourseId,
                    FileName = "Thermal.pdf",
                    isRPLEnabled = rplEnabled
                }
            };

        /* ---------------- certificate/record naming ---------------- */

        [TestMethod]
        public void CourseName_IsTheReferenceNumberLetterAndDescription()
        {
            var course = MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)");

            // The exact string the 122 existing compliance records for this course carry.
            Assert.AreEqual("03e Thermal Camera (FLIR Ti)", course.CertificateRecordName);
        }

        [TestMethod]
        public void CourseName_FallsBackToTheDescription_WhenNoReferenceIsConfigured()
        {
            // Would have thrown on HrSettings.ReferenceNo; a certificate must still be issuable.
            Assert.AreEqual("Thermal Camera (FLIR Ti)",
                MakeCourse(ThermalCameraCourseId, null, null, "Thermal Camera (FLIR Ti)").CertificateRecordName);
        }

        [TestMethod]
        public void IssuedCertificate_IsRecordedUnderTheFullCourseName()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                ThermalCameraDocuments(rplEnabled: false));

            service.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId);

            /* The defect: this stored "Thermal Camera (FLIR Ti)" while every single-guard flow
               stored "03e Thermal Camera (FLIR Ti)" for the same course. */
            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(
                It.Is<GuardComplianceAndLicense>(c => c.Description == "03e Thermal Camera (FLIR Ti)")), Times.Once);

            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(
                It.Is<GuardComplianceAndLicense>(c => c.Description == "Thermal Camera (FLIR Ti)")), Times.Never);
        }

        /* ---------------- recording the assessment ---------------- */

        [TestMethod]
        public void RplRelease_RecordsTheAssessmentAgainstTheGuard_BeforeTheCertificateIsBuilt()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                ThermalCameraDocuments(rplEnabled: true));

            var order = new List<string>();
            _guardLogDataProvider.Setup(z => z.SaveTrainingCourseCertificateRPL(It.IsAny<TrainingCourseCertificateRPL>()))
                .Callback<TrainingCourseCertificateRPL>(r => order.Add(r.isDeleted ? "consume" : "save"));
            _certificateGenerator.Setup(z => z.GeneratePdf(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Callback(() => order.Add("pdf")).Returns("certificate.pdf");

            service.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId, MakeDetails());

            /* Order is the whole point: GeneratePdf reads the assessment to decide between a score
               card and the RPL compliance document, and to date the file from the assessment end
               date. Recorded after the build, the certificate would be a score-card one dated
               today for a guard who never sat the test. */
            CollectionAssert.AreEqual(new[] { "save", "pdf", "consume" }, order);
        }

        [TestMethod]
        public void RplRelease_StoresEveryFieldFromTheModal()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                ThermalCameraDocuments(rplEnabled: true));

            TrainingCourseCertificateRPL saved = null;
            _guardLogDataProvider.Setup(z => z.SaveTrainingCourseCertificateRPL(It.IsAny<TrainingCourseCertificateRPL>()))
                .Callback<TrainingCourseCertificateRPL>(r => { if (!r.isDeleted) saved = r; });

            service.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId, MakeDetails());

            Assert.IsNotNull(saved);
            Assert.AreEqual(BrunoGuardId, saved.GuardId);
            Assert.AreEqual(ThermalCameraCertificateId, saved.TrainingCourseCertificateId);
            Assert.AreEqual(3, saved.TrainingTheoryLocationId);
            Assert.AreEqual(4, saved.TrainingPracticalLocationId);
            Assert.AreEqual(5, saved.TrainingInstructorId);
            Assert.AreEqual(new DateTime(2026, 9, 1), saved.AssessmentStartDate);
            Assert.AreEqual(new DateTime(2026, 9, 4), saved.AssessmentEndDate);
            Assert.AreEqual("rpl-evidence.pdf", saved.FileName);

            // -1, not 0, is the provider's insert sentinel; 0 silently updates nothing.
            Assert.AreEqual(-1, saved.Id);
        }

        [TestMethod]
        public void RplRelease_UpdatesTheGuardsExistingAssessment_RatherThanAddingASecond()
        {
            var existing = new TrainingCourseCertificateRPL
            {
                Id = 812,
                GuardId = BrunoGuardId,
                TrainingCourseCertificateId = ThermalCameraCertificateId
            };

            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                ThermalCameraDocuments(rplEnabled: true),
                new List<TrainingCourseCertificateRPL> { existing });

            TrainingCourseCertificateRPL saved = null;
            _guardLogDataProvider.Setup(z => z.SaveTrainingCourseCertificateRPL(It.IsAny<TrainingCourseCertificateRPL>()))
                .Callback<TrainingCourseCertificateRPL>(r => { if (!r.isDeleted) saved = r; });

            service.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId, MakeDetails());

            /* GeneratePdf takes the LAST assessment row it finds for the guard, so a second row
               would decide the certificate while the first one lingered. */
            Assert.AreEqual(812, saved.Id);
        }

        [TestMethod]
        public void RplRelease_MarksTheAssessmentConsumed_SoTheNightlyRunDoesNotReissueIt()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                ThermalCameraDocuments(rplEnabled: true));

            service.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId, MakeDetails());

            /* The release is the issuing event. A row left pending is picked up by
               api/RPLCertificate the next night, which regenerates the certificate, inserts a
               second compliance record and sends "New Certificate Issued" again - for every guard
               in the release. */
            _guardLogDataProvider.Verify(z => z.SaveTrainingCourseCertificateRPL(
                It.Is<TrainingCourseCertificateRPL>(r => r.isDeleted)), Times.Once);
        }

        [TestMethod]
        public void RplRelease_WithNoDetails_IssuesExactlyAsBefore()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(NonRplCourseId, "01", "g", "Level 5") },
                new List<TrainingCourseCertificate>
                {
                    new TrainingCourseCertificate { Id = 90, HRSettingsId = NonRplCourseId, isRPLEnabled = false }
                });

            service.IssueCertificateForGuard(BrunoGuardId, NonRplCourseId, null);

            // No assessment written at all - a non-RPL release must not start creating RPL rows.
            _guardLogDataProvider.Verify(z => z.SaveTrainingCourseCertificateRPL(
                It.IsAny<TrainingCourseCertificateRPL>()), Times.Never);
            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(
                It.Is<GuardComplianceAndLicense>(c => c.Description == "01g Level 5")), Times.Once);
        }

        [TestMethod]
        public void RplRelease_FailsWithANamedMessage_WhenTheCourseHasNoCertificateDocument()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                new List<TrainingCourseCertificate>());

            var ex = Assert.ThrowsException<InvalidOperationException>(
                () => service.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId, MakeDetails()));

            // The message is shown against the guard in the release's result list.
            StringAssert.Contains(ex.Message, "Thermal Camera (FLIR Ti)");
            Assert.IsFalse(ex.Message.Contains("Object reference"));
        }

        [TestMethod]
        public void CertificateDocumentForCourse_IsTheSameRowTheGuardGridAndTheGeneratorRead()
        {
            var service = CreateGenerator(
                new List<HrSettings> { MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)") },
                ThermalCameraDocuments(rplEnabled: true));

            var document = service.GetCertificateDocumentForCourse(ThermalCameraCourseId);

            /* A different row here would record the assessment against a certificate the generated
               PDF never looks at - exactly the mismatch that left DashCAM assessments stuck. */
            Assert.AreEqual(ThermalCameraCertificateId, document.Id);
            Assert.IsTrue(document.isRPLEnabled);
        }

        /* ---------------- planning a release ---------------- */

        private static List<Guard> TwoGuards() =>
            new List<Guard> { MakeGuard(BrunoGuardId, "Bruno Timpano", "B.T"), MakeGuard(JohnGuardId, "John Remington", "J.R") };

        private static List<HrSettings> TwoCourses() => new List<HrSettings>
        {
            MakeCourse(ThermalCameraCourseId, "03", "e", "Thermal Camera (FLIR Ti)"),
            MakeCourse(NonRplCourseId, "01", "g", "Level 5")
        };

        [TestMethod]
        public void Plan_FlagsOnlyTheRplCourses()
        {
            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId, JohnGuardId }, new[] { ThermalCameraCourseId, NonRplCourseId },
                new HashSet<int> { ThermalCameraCourseId }, MakeDetails());

            Assert.IsTrue(plan.IsValid);
            Assert.AreEqual(4, plan.Pairings.Count);
            Assert.AreEqual(2, plan.Pairings.Count(p => p.RequiresRpl));
            Assert.IsTrue(plan.Pairings.Where(p => p.RequiresRpl).All(p => p.HrSettingsId == ThermalCameraCourseId));
        }

        [TestMethod]
        public void Plan_AppliesTheOneAssessmentToEveryGuard()
        {
            var details = MakeDetails();

            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId, JohnGuardId }, new[] { ThermalCameraCourseId },
                new HashSet<int> { ThermalCameraCourseId }, details);

            // One set of details entered once, carried by the plan for the whole run.
            Assert.AreSame(details, plan.RplDetails);
            Assert.AreEqual(2, plan.Pairings.Count);
        }

        [TestMethod]
        public void Plan_IsRejected_WhenAnRplCourseIsSelectedWithNoAssessment()
        {
            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId }, new[] { ThermalCameraCourseId },
                new HashSet<int> { ThermalCameraCourseId }, null);

            Assert.IsFalse(plan.IsValid);
            StringAssert.Contains(plan.Message, "Thermal Camera (FLIR Ti)");
            Assert.AreEqual(0, plan.Pairings.Count);
        }

        [TestMethod]
        public void Plan_IsUnchanged_WhenNothingSelectedIsAnRplCourse()
        {
            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId, JohnGuardId }, new[] { NonRplCourseId });

            Assert.IsTrue(plan.IsValid);
            Assert.AreEqual(2, plan.Pairings.Count);
            Assert.IsFalse(plan.Pairings.Any(p => p.RequiresRpl));
            Assert.IsNull(plan.RplDetails);
        }

        [TestMethod]
        public void Plan_RejectsAnAssessmentThatEndsBeforeItStarts()
        {
            var details = MakeDetails();
            details.AssessmentEndDate = details.AssessmentStartDate.AddDays(-1);

            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId }, new[] { ThermalCameraCourseId },
                new HashSet<int> { ThermalCameraCourseId }, details);

            Assert.IsFalse(plan.IsValid);
            StringAssert.Contains(plan.Message, "earlier than");
        }

        [TestMethod]
        public void Plan_RejectsAnAssessmentWithNoInstructorOrLocations()
        {
            var courses = TwoCourses();
            var rplOnly = new HashSet<int> { ThermalCameraCourseId };

            var noInstructor = MakeDetails(); noInstructor.TrainingInstructorId = 0;
            var noTheory = MakeDetails(); noTheory.TrainingTheoryLocationId = 0;
            var noPractical = MakeDetails(); noPractical.TrainingPracticalLocationId = 0;

            foreach (var details in new[] { noInstructor, noTheory, noPractical })
            {
                var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), courses,
                    new[] { BrunoGuardId }, new[] { ThermalCameraCourseId }, rplOnly, details);

                Assert.IsFalse(plan.IsValid);
                Assert.AreEqual(0, plan.Pairings.Count);
            }
        }

        [TestMethod]
        public void Plan_AcceptsAnAssessmentWithNoComplianceDocument()
        {
            // Optional in the single-guard modal, so optional here too.
            var details = MakeDetails();
            details.FileName = null;

            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId }, new[] { ThermalCameraCourseId },
                new HashSet<int> { ThermalCameraCourseId }, details);

            Assert.IsTrue(plan.IsValid);
        }

        /* ---------------- what the browser posts ---------------- */

        [TestMethod]
        public void PostedRplFields_BecomeAnAssessment()
        {
            // The exact payload the modal sends: rpl.* form keys bound as BulkRplDetailsRequest.
            var request = new CityWatch.Web.Pages.Admin.SettingsModel.BulkRplDetailsRequest
            {
                TrainingTheoryLocationId = 3,
                TrainingPracticalLocationId = 4,
                TrainingInstructorId = 5,
                AssessmentStartDate = new DateTime(2026, 9, 1),
                AssessmentEndDate = new DateTime(2026, 9, 4),
                FileName = "rpl-evidence.pdf"
            };

            var details = request.ToAssessmentDetails();

            Assert.IsNotNull(details);
            Assert.AreEqual(3, details.TrainingTheoryLocationId);
            Assert.AreEqual(4, details.TrainingPracticalLocationId);
            Assert.AreEqual(5, details.TrainingInstructorId);
            Assert.AreEqual(new DateTime(2026, 9, 1), details.AssessmentStartDate);
            Assert.AreEqual(new DateTime(2026, 9, 4), details.AssessmentEndDate);
            Assert.AreEqual("rpl-evidence.pdf", details.FileName);
        }

        [TestMethod]
        public void APostWithNoRplFields_IsNoAssessmentAtAll()
        {
            /* A non-RPL release sends no rpl.* keys, but complex-type binding still hands the
               handler an instance. Read as an empty assessment rather than as a zero-valued one, or
               a release with no RPL course in it would start carrying an assessment. */
            var request = new CityWatch.Web.Pages.Admin.SettingsModel.BulkRplDetailsRequest();

            Assert.IsTrue(request.IsEmpty);
            Assert.IsNull(request.ToAssessmentDetails());
        }

        [TestMethod]
        public void APartlyFilledPost_IsNotTreatedAsEmpty()
        {
            // Half-filled is a validation failure with a message, not a silent "no RPL details".
            var request = new CityWatch.Web.Pages.Admin.SettingsModel.BulkRplDetailsRequest
            {
                TrainingTheoryLocationId = 3
            };

            Assert.IsFalse(request.IsEmpty);
            Assert.IsNotNull(request.ToAssessmentDetails());

            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId }, new[] { ThermalCameraCourseId },
                new HashSet<int> { ThermalCameraCourseId }, request.ToAssessmentDetails());

            Assert.IsFalse(plan.IsValid);
            StringAssert.Contains(plan.Message, "date");
        }

        /* ---------------- running a release ---------------- */

        [TestMethod]
        public void Run_PassesTheSameAssessmentToEveryRplPairing_AndNoneToTheOthers()
        {
            var certificateService = new Mock<IRPLCertificateGeneratorService>();
            var details = MakeDetails();

            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId, JohnGuardId }, new[] { ThermalCameraCourseId, NonRplCourseId },
                new HashSet<int> { ThermalCameraCourseId }, details);

            var job = new BulkCertificateJob { Pairings = plan.Pairings, RplDetails = plan.RplDetails };
            BulkCertificateRelease.Run(job, certificateService.Object, Mock.Of<ILogger>());

            // Both guards, the one assessment, the RPL course only.
            certificateService.Verify(z => z.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId, details), Times.Once);
            certificateService.Verify(z => z.IssueCertificateForGuard(JohnGuardId, ThermalCameraCourseId, details), Times.Once);

            // The non-RPL course takes the original call, unchanged.
            certificateService.Verify(z => z.IssueCertificateForGuard(BrunoGuardId, NonRplCourseId), Times.Once);
            certificateService.Verify(z => z.IssueCertificateForGuard(JohnGuardId, NonRplCourseId), Times.Once);
            certificateService.Verify(z => z.IssueCertificateForGuard(It.IsAny<int>(), NonRplCourseId,
                It.IsAny<RplAssessmentDetails>()), Times.Never);

            Assert.AreEqual(4, job.Issued);
            Assert.AreEqual(0, job.Failed);
        }

        [TestMethod]
        public void Run_LabelsRplResultsSoTheOperatorCanSeeWhichWereRpl()
        {
            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId }, new[] { ThermalCameraCourseId, NonRplCourseId },
                new HashSet<int> { ThermalCameraCourseId }, MakeDetails());

            var job = new BulkCertificateJob { Pairings = plan.Pairings, RplDetails = plan.RplDetails };
            BulkCertificateRelease.Run(job, Mock.Of<IRPLCertificateGeneratorService>(), Mock.Of<ILogger>());

            Assert.AreEqual("RPL certificate issued successfully",
                job.Results.Single(r => r.CourseId == ThermalCameraCourseId).Status);
            Assert.AreEqual("Certificate issued successfully",
                job.Results.Single(r => r.CourseId == NonRplCourseId).Status);
        }

        [TestMethod]
        public void Run_IsolatesAFailedRplGuard_AndCarriesOn()
        {
            var certificateService = new Mock<IRPLCertificateGeneratorService>();
            certificateService.Setup(z => z.IssueCertificateForGuard(BrunoGuardId, ThermalCameraCourseId,
                    It.IsAny<RplAssessmentDetails>()))
                .Throws(new InvalidOperationException("No certificate document is uploaded for 'Thermal Camera (FLIR Ti)'."));

            var plan = BulkCertificateRelease.BuildPlan(TwoGuards(), TwoCourses(),
                new[] { BrunoGuardId, JohnGuardId }, new[] { ThermalCameraCourseId },
                new HashSet<int> { ThermalCameraCourseId }, MakeDetails());

            var job = new BulkCertificateJob { Pairings = plan.Pairings, RplDetails = plan.RplDetails };
            BulkCertificateRelease.Run(job, certificateService.Object, Mock.Of<ILogger>());

            Assert.AreEqual(1, job.Issued);
            Assert.AreEqual(1, job.Failed);
            certificateService.Verify(z => z.IssueCertificateForGuard(JohnGuardId, ThermalCameraCourseId,
                It.IsAny<RplAssessmentDetails>()), Times.Once);
            StringAssert.Contains(job.Results.Single(r => r.GuardId == BrunoGuardId).Status, "Thermal Camera (FLIR Ti)");
        }
    }
}
