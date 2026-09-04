using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using Path = System.IO.Path;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Reproduces the "Object reference not set to an instance of an object." reported by the Bulk
    /// Certificate Release for Bruno Timpano and John Remington on "Thermal Camera (FLIR Ti)".
    ///
    /// Neither guard has a GuardTrainingAndAssessmentScore row for that course (they never sat the
    /// online test - the certificate is being released by an admin) and neither is on the course's
    /// RPL list. CertificateGenerator.AttachScoreCard dereferenced GetGuardScores(...).FirstOrDefault()
    /// unguarded, so the certificate blew up there; RPLCertificateGeneratorService.GuardCertificate
    /// then had the same problem with its TrainingCourseCertificateRPL lookup.
    ///
    /// Everything is mocked, so no email is sent, nothing is written to the database, and the Dropbox
    /// upload goes to a mock - the only real side effect is a PDF written under a temp folder that the
    /// test deletes afterwards.
    /// </summary>
    [TestClass]
    public class CertificateGenerationNullDataTests
    {
        // The real ids and data shape, taken from the dev database for the reported failure.
        private const int ThermalCameraHrSettingsId = 13;
        private const string ThermalCameraDescription = "Thermal Camera (FLIR Ti)";
        private const string CertificateFileName = "Thermal Camera (FLIR Ti)_Certificate.pdf";
        private const int CertificateDocumentId = 3;
        private const int BrunoTimpanoGuardId = 4;
        private const int JohnRemingtonGuardId = 401;

        private string _webRoot;
        private Mock<IConfigDataProvider> _configDataProvider;
        private Mock<IGuardDataProvider> _guardDataProvider;
        private Mock<IDropboxService> _dropboxService;

        [TestInitialize]
        public void Setup()
        {
            _webRoot = Path.Combine(Path.GetTempPath(), "CityWatch.Web.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_webRoot);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_webRoot))
                    Directory.Delete(_webRoot, true);
            }
            catch (IOException)
            {
                // A leftover temp folder is not worth failing a test over.
            }
        }

        [DataTestMethod]
        [DataRow(BrunoTimpanoGuardId, "Bruno Timpano", "569-829-XXX")]
        [DataRow(JohnRemingtonGuardId, "John Remington", "675-553-XXX")]
        public void GeneratePdf_GuardWithNoTestScores_DoesNotThrowNullReference(int guardId, string guardName, string securityNo)
        {
            var generator = CreateGenerator(guardId, guardName, securityNo, guardHasScores: false);

            var fileName = generator.GeneratePdf(guardId, ThermalCameraHrSettingsId, "TEST-HASH",
                isCertificateHold: false, isCertificatewithQADump: false, isCertificateExpiry: false);

            Assert.IsFalse(string.IsNullOrEmpty(fileName), "Certificate generation returned no file name.");

            var written = Path.Combine(_webRoot, "Uploads", "Guards", "License", securityNo, fileName);
            Assert.IsTrue(File.Exists(written), $"Certificate was not written to {written}.");
            Assert.IsTrue(new FileInfo(written).Length > 0, "Certificate is empty.");

            // The score card page is still attached, just without a score against the course.
            Assert.IsTrue(PageCount(written) > 1, "Score card page was not attached to the certificate.");
        }

        /// <summary>A guard who did sit the test must still get the score printed.</summary>
        [TestMethod]
        public void GeneratePdf_GuardWithTestScores_StillWritesTheScoreCard()
        {
            var generator = CreateGenerator(BrunoTimpanoGuardId, "Bruno Timpano", "569-829-XXX", guardHasScores: true);

            var fileName = generator.GeneratePdf(BrunoTimpanoGuardId, ThermalCameraHrSettingsId, "TEST-HASH",
                isCertificateHold: false, isCertificatewithQADump: false, isCertificateExpiry: false);

            var written = Path.Combine(_webRoot, "Uploads", "Guards", "License", "569-829-XXX", fileName);
            Assert.IsTrue(File.Exists(written), $"Certificate was not written to {written}.");
            Assert.IsTrue(PageCount(written) > 1, "Score card page was not attached to the certificate.");
        }

        /// <summary>
        /// The other half of the reported failure: the course's certificate document has isRPLEnabled
        /// set, but a guard released in bulk has no TrainingCourseCertificateRPL row to mark consumed.
        /// </summary>
        [TestMethod]
        public void IssueCertificateForGuard_GuardNotOnRplList_DoesNotThrowNullReference()
        {
            var service = CreateRplService(BrunoTimpanoGuardId, guardHasRplRow: false);

            service.IssueCertificateForGuard(BrunoTimpanoGuardId, ThermalCameraHrSettingsId);

            // The compliance record is what the release exists to produce.
            _guardDataProvider.Verify(z => z.SaveGuardComplianceandlicanse(
                It.Is<GuardComplianceAndLicense>(c => c.GuardId == BrunoTimpanoGuardId
                                                      && c.Description == ThermalCameraDescription)), Times.Once);

            // Nothing to mark consumed, so the RPL row must not be touched.
            _guardLogDataProvider.Verify(z => z.SaveTrainingCourseCertificateRPL(
                It.IsAny<TrainingCourseCertificateRPL>()), Times.Never);
        }

        /// <summary>A guard who is on the RPL list still has that assessment marked as consumed.</summary>
        [TestMethod]
        public void IssueCertificateForGuard_GuardOnRplList_MarksTheRplAssessmentConsumed()
        {
            var service = CreateRplService(BrunoTimpanoGuardId, guardHasRplRow: true);

            service.IssueCertificateForGuard(BrunoTimpanoGuardId, ThermalCameraHrSettingsId);

            _guardLogDataProvider.Verify(z => z.SaveTrainingCourseCertificateRPL(
                It.Is<TrainingCourseCertificateRPL>(r => r.GuardId == BrunoTimpanoGuardId && r.isDeleted)), Times.Once);
        }

        /// <summary>
        /// A course with no certificate document must say so rather than throw a bare
        /// NullReferenceException - the Bulk Certificate Release shows this text to the operator.
        /// </summary>
        [TestMethod]
        public void GeneratePdf_CourseWithNoCertificateDocument_ReportsWhatIsMissing()
        {
            var generator = CreateGenerator(BrunoTimpanoGuardId, "Bruno Timpano", "569-829-XXX",
                guardHasScores: false, courseHasCertificateDocument: false);

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                generator.GeneratePdf(BrunoTimpanoGuardId, ThermalCameraHrSettingsId, "TEST-HASH", false, false, false));

            StringAssert.Contains(ex.Message, ThermalCameraDescription);
            StringAssert.Contains(ex.Message, "certificate document");
        }

        /// <summary>Same for a course that has no training course rows at all.</summary>
        [TestMethod]
        public void GeneratePdf_CourseWithNoTrainingCourse_ReportsWhatIsMissing()
        {
            var generator = CreateGenerator(BrunoTimpanoGuardId, "Bruno Timpano", "569-829-XXX",
                guardHasScores: false, courseHasTrainingCourse: false);

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                generator.GeneratePdf(BrunoTimpanoGuardId, ThermalCameraHrSettingsId, "TEST-HASH", false, false, false));

            StringAssert.Contains(ex.Message, ThermalCameraDescription);
            StringAssert.Contains(ex.Message, "training course");
        }

        /* ---------------- fixtures ---------------- */

        private Mock<IGuardLogDataProvider> _guardLogDataProvider;

        private static int PageCount(string path)
        {
            using var pdf = new PdfDocument(new PdfReader(path));
            return pdf.GetNumberOfPages();
        }

        private static HrSettings ThermalCameraCourse() => new HrSettings
        {
            Id = ThermalCameraHrSettingsId,
            Description = ThermalCameraDescription,
            HRGroupId = 3,
            ReferenceNoNumberId = 3,
            ReferenceNoNumbers = new ReferenceNoNumbers { Id = 3, Name = "03" },
            ReferenceNoAlphabetId = 5,
            ReferenceNoAlphabets = new ReferenceNoAlphabets { Id = 5, Name = "e" }
        };

        /// <summary>
        /// The certificate template the generator fills in. Only the "Student" field is read
        /// unconditionally, so that is all the template needs.
        /// </summary>
        private void WriteCertificateTemplate()
        {
            var dir = Path.Combine(_webRoot, "TA", "HR03e", "Certificate");
            Directory.CreateDirectory(dir);

            using var pdf = new PdfDocument(new PdfWriter(Path.Combine(dir, CertificateFileName)));
            pdf.AddNewPage(PageSize.A4);
            var form = PdfAcroForm.GetAcroForm(pdf, true);
            form.AddField(PdfFormField.CreateText(pdf, new Rectangle(50, 700, 300, 20), "Student", string.Empty));
        }

        private CertificateGenerator CreateGenerator(int guardId, string guardName, string securityNo,
            bool guardHasScores, bool courseHasCertificateDocument = true, bool courseHasTrainingCourse = true)
        {
            WriteCertificateTemplate();
            BuildProviderMocks(guardId, guardName, securityNo, guardHasScores, courseHasCertificateDocument,
                courseHasTrainingCourse, guardHasRplRow: false);

            return new CertificateGenerator(
                new TestWebHostEnvironment(_webRoot),
                _configDataProvider.Object,
                new Mock<IClientDataProvider>().Object,
                Options.Create(new Settings()),
                new ConfigurationBuilder().Build(),
                NullLogger<IncidentReportGenerator>.Instance,
                new Mock<IPatrolDataReportService>().Object,
                null,                                   // CityWatchDbContext - not touched on this path
                _guardDataProvider.Object,
                _dropboxService.Object);
        }

        private RPLCertificateGeneratorService CreateRplService(int guardId, bool guardHasRplRow)
        {
            BuildProviderMocks(guardId, "Bruno Timpano", "569-829-XXX", guardHasScores: false,
                courseHasCertificateDocument: true, courseHasTrainingCourse: true, guardHasRplRow: guardHasRplRow);

            // The PDF itself is covered by the GeneratePdf tests above; here it only has to return a name.
            var certificateGenerator = new Mock<ICertificateGenerator>();
            certificateGenerator
                .Setup(z => z.GeneratePdf(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns("03e_Thermal Camera (FLIR Ti)-doi 04 SEP 26.pdf");

            // No email: GetGlobalComplianceAlertEmail returns nothing, so SendEmailNew has no recipient.
            var clientDataProvider = new Mock<IClientDataProvider>();
            clientDataProvider.Setup(z => z.GetGlobalComplianceAlertEmail())
                .Returns(new List<GlobalComplianceAlertEmail>());

            return new RPLCertificateGeneratorService(
                _guardLogDataProvider.Object,
                _guardDataProvider.Object,
                _configDataProvider.Object,
                certificateGenerator.Object,
                Options.Create(new CityWatch.Data.Helpers.EmailOptions { FromAddress = "noreply@test|CityWatch" }),
                clientDataProvider.Object,
                NullLogger<RPLCertificateGeneratorService>.Instance);
        }

        private void BuildProviderMocks(int guardId, string guardName, string securityNo, bool guardHasScores,
            bool courseHasCertificateDocument, bool courseHasTrainingCourse, bool guardHasRplRow)
        {
            var guard = new Guard { Id = guardId, Name = guardName, SecurityNo = securityNo, State = "VIC", IsActive = true };
            var course = ThermalCameraCourse();

            // Course 13 has two training courses in the dev database, TQ numbers 1 and 2.
            var trainingCourses = courseHasTrainingCourse
                ? new List<TrainingCourses>
                {
                    new TrainingCourses { Id = 6, HRSettingsId = ThermalCameraHrSettingsId, TQNumberId = 1, FileName = "Thermal Camera TQ1" },
                    new TrainingCourses { Id = 7, HRSettingsId = ThermalCameraHrSettingsId, TQNumberId = 2, FileName = "Thermal Camera TQ2" }
                }
                : new List<TrainingCourses>();

            var certificateDocuments = courseHasCertificateDocument
                ? new List<TrainingCourseCertificate>
                {
                    // isRPLEnabled is on for this course in the dev database - that is what made the
                    // missing RPL row a NullReferenceException rather than a no-op.
                    new TrainingCourseCertificate { Id = CertificateDocumentId, HRSettingsId = ThermalCameraHrSettingsId, FileName = CertificateFileName, isRPLEnabled = true }
                }
                : new List<TrainingCourseCertificate>();

            _configDataProvider = new Mock<IConfigDataProvider>();
            _configDataProvider.Setup(z => z.GetHRSettings()).Returns(new List<HrSettings> { course });
            _configDataProvider.Setup(z => z.GetCourseCertificateDocsUsingSettingsId(ThermalCameraHrSettingsId)).Returns(certificateDocuments);
            _configDataProvider.Setup(z => z.GetTrainingCoursesWithHrSettingsId(ThermalCameraHrSettingsId)).Returns(trainingCourses);
            _configDataProvider.Setup(z => z.GetTrainingCourses(ThermalCameraHrSettingsId, It.IsAny<int>()))
                .Returns((int _, int tq) => trainingCourses.Where(c => c.TQNumberId == tq).ToList());
            _configDataProvider.Setup(z => z.GetQuestionCount(It.IsAny<int>(), It.IsAny<int>())).Returns(10);
            _configDataProvider.Setup(z => z.GetTQSettings(ThermalCameraHrSettingsId))
                .Returns(new List<TrainingTestQuestionSettings>
                {
                    new TrainingTestQuestionSettings { Id = 3, HRSettingsId = ThermalCameraHrSettingsId }
                });

            // Neither guard has sat the test, been assessed practically, or been given an RPL record.
            _configDataProvider.Setup(z => z.GetGuardTrainingStartTest(guardId, It.IsAny<int>()))
                .Returns(new List<GuardTrainingStartTest>());
            _configDataProvider.Setup(z => z.GetGuardTrainingPracticalDetails(guardId, ThermalCameraHrSettingsId))
                .Returns(new List<GuardTrainingAndAssessmentPractical>());
            _configDataProvider.Setup(z => z.GetCourseCertificateRPLUsingId(It.IsAny<int>()))
                .Returns(new List<TrainingCourseCertificateRPL>());
            _configDataProvider.Setup(z => z.GetGuardScores(guardId, It.IsAny<int>()))
                .Returns(guardHasScores
                    ? new List<GuardTrainingAndAssessmentScore>
                    {
                        new GuardTrainingAndAssessmentScore { Id = 1, GuardId = guardId, TrainingCourseId = 6, guardScore = "9/10", guardCorrectQuestionsCount = 9, IsPass = true }
                    }
                    : new List<GuardTrainingAndAssessmentScore>());
            _configDataProvider.Setup(z => z.GetTrainingCoursesWithOnlyHrSettingsId(It.IsAny<int>())).Returns(trainingCourses);

            _guardDataProvider = new Mock<IGuardDataProvider>();
            _guardDataProvider.Setup(z => z.GetGuards()).Returns(new List<Guard> { guard });
            _guardDataProvider.Setup(z => z.GetGuardDetailsUsingId(guardId)).Returns(new List<Guard> { guard });
            _guardDataProvider.Setup(z => z.GetDrobox()).Returns(new DropboxDirectory { Id = 1, DropboxDir = "/CWS-HR" });
            _guardDataProvider.Setup(z => z.GetCourseCertificateRPL())
                .Returns(guardHasRplRow
                    ? new List<TrainingCourseCertificateRPL>
                    {
                        new TrainingCourseCertificateRPL
                        {
                            Id = 500, GuardId = guardId, TrainingCourseCertificateId = CertificateDocumentId,
                            AssessmentStartDate = new DateTime(2026, 1, 1), AssessmentEndDate = new DateTime(2026, 2, 1)
                        }
                    }
                    : new List<TrainingCourseCertificateRPL>());

            _guardLogDataProvider = new Mock<IGuardLogDataProvider>();

            _dropboxService = new Mock<IDropboxService>();
            _dropboxService.Setup(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(false));
        }

        /// <summary>Minimal IWebHostEnvironment - the generator only reads WebRootPath.</summary>
        private sealed class TestWebHostEnvironment : IWebHostEnvironment
        {
            public TestWebHostEnvironment(string webRootPath)
            {
                WebRootPath = webRootPath;
                ContentRootPath = webRootPath;
                WebRootFileProvider = new PhysicalFileProvider(webRootPath);
                ContentRootFileProvider = WebRootFileProvider;
            }

            public string WebRootPath { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string ApplicationName { get; set; } = "CityWatch.Web.Tests";
            public IFileProvider ContentRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public string EnvironmentName { get; set; } = "Development";
        }
    }
}
