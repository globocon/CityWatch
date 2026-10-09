using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// "Disable PCAR" (LB > Schedule) and the "Schedule PCAR" tab, through the scheduler.
    ///
    /// No database and no network, as in PeriodicLogDumpTests: the report generators are mocked and
    /// write a one-page PDF, Dropbox is mocked, and recipients are left empty so no SMTP connection
    /// is attempted. What is asserted is which report the scheduler asks for with which PCAR filter,
    /// what it names it, and what it records.
    /// </summary>
    [TestClass]
    public class PcarScheduleTests
    {
        private const int SiteId = 390;
        private const string SiteName = "Martha Cove Marina";

        private string _webRoot;
        private string _outputDirectory;

        private Mock<IClientDataProvider> _clientDataProvider;
        private Mock<IGuardLogReportGenerator> _guardLogReportGenerator;
        private Mock<IKeyVehicleLogReportGenerator> _keyVehicleLogReportGenerator;
        private Mock<IDropboxService> _dropboxService;

        private readonly List<string> _uploadedPaths = new();
        private readonly List<ClientSitePeriodicLogUpload> _recordedUploads = new();
        private readonly List<ClientSiteLogBook> _createdLogBooks = new();
        private readonly List<(int Id, string FileName)> _markedUploaded = new();

        private static readonly DateTime Yesterday = DateTime.Now.AddDays(-1).Date;
        private static readonly DateTime WeekStart = SiteLogUploadService.GetPreviousWeek(DateTime.Now.Date).PeriodStart;

        [TestInitialize]
        public void Setup()
        {
            _webRoot = Path.Combine(Path.GetTempPath(), "cw-pcar-" + Guid.NewGuid().ToString("N"));
            _outputDirectory = Path.Combine(_webRoot, "Pdf", "Output");
            Directory.CreateDirectory(_outputDirectory);

            _uploadedPaths.Clear();
            _recordedUploads.Clear();
            _createdLogBooks.Clear();
            _markedUploaded.Clear();

            _clientDataProvider = new Mock<IClientDataProvider>();
            _guardLogReportGenerator = new Mock<IGuardLogReportGenerator>();
            _keyVehicleLogReportGenerator = new Mock<IKeyVehicleLogReportGenerator>();
            _dropboxService = new Mock<IDropboxService>();

            _clientDataProvider.Setup(z => z.GetClientSiteKpiSetting(It.IsAny<int>()))
                .Returns(new ClientSiteKpiSetting { ClientSiteId = SiteId, DropboxImagesDir = "/C4i/Martha Cove", DropboxScheduleisActive = true });
            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>());
            _clientDataProvider.Setup(z => z.SavePeriodicLogUpload(It.IsAny<ClientSitePeriodicLogUpload>()))
                .Callback<ClientSitePeriodicLogUpload>(r => _recordedUploads.Add(r));
            _clientDataProvider.Setup(z => z.GetInProgressPeriodicLogDumpJob(It.IsAny<string>()))
                .Returns((PeriodicLogDumpJob)null);
            _clientDataProvider.Setup(z => z.SavePeriodicLogDumpJob(It.IsAny<PeriodicLogDumpJob>())).Returns(1);

            // Log books created by the run come back from GetClientSiteLogBook, as the real provider does.
            _clientDataProvider.Setup(z => z.SaveClientSiteLogBook(It.IsAny<ClientSiteLogBook>()))
                .Callback<ClientSiteLogBook>(b => { b.Id = 9000 + _createdLogBooks.Count; _createdLogBooks.Add(b); })
                .Returns(() => 9000 + _createdLogBooks.Count - 1);
            _clientDataProvider.Setup(z => z.GetClientSiteLogBook(It.IsAny<int>(), It.IsAny<LogBookType>(), It.IsAny<DateTime>()))
                .Returns<int, LogBookType, DateTime>((site, type, date) =>
                    _createdLogBooks.FirstOrDefault(b => b.ClientSiteId == site && b.Type == type && b.Date == date));
            _clientDataProvider.Setup(z => z.MarkClientSiteLogBookAsUploaded(It.IsAny<int>(), It.IsAny<string>()))
                .Callback<int, string>((id, file) => _markedUploaded.Add((id, file)));

            _dropboxService.Setup(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<DropboxSettings, string, string>((_, _, dbxPath) => _uploadedPaths.Add(dbxPath))
                .ReturnsAsync(true);

            StubGenerators(pcarEntriesExist: true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_webRoot))
                    Directory.Delete(_webRoot, recursive: true);
            }
            catch
            {
            }
        }

        /* ---------------- fixtures ---------------- */

        private static ClientSite MakeSite(PatrolTouringMode mode = PatrolTouringMode.STND) => new()
        {
            Id = SiteId,
            Name = SiteName,
            IsActive = true,
            PatrolTourMode = mode,
            GuardLogEmailTo = string.Empty   // no SMTP
        };

        private static List<ClientSiteLogBook> MakeDay(ClientSite site, DateTime date) => new()
        {
            new ClientSiteLogBook { Id = 100, ClientSiteId = site.Id, ClientSite = site, Type = LogBookType.DailyGuardLog, Date = date },
            new ClientSiteLogBook { Id = 200, ClientSiteId = site.Id, ClientSite = site, Type = LogBookType.VehicleAndKeyLog, Date = date }
        };

        private static List<ClientSiteLogBook> MakeWeek(ClientSite site) =>
            Enumerable.Range(0, 7)
                .Select(offset => new ClientSiteLogBook
                {
                    Id = 100 + offset,
                    ClientSiteId = site.Id,
                    ClientSite = site,
                    Type = LogBookType.DailyGuardLog,
                    Date = WeekStart.AddDays(offset)
                })
                .ToList();

        private string WriteDayPdf(string fileName)
        {
            using (var writer = new iText.Kernel.Pdf.PdfWriter(Path.Combine(_outputDirectory, fileName)))
            using (var document = new iText.Kernel.Pdf.PdfDocument(writer))
            {
                document.AddNewPage();
            }
            return fileName;
        }

        /// <summary>
        /// Every generator call writes a PDF named after its report and filter, except a PCAR-only
        /// report on a day without patrol car entries - the real generator returns no file then.
        /// </summary>
        private void StubGenerators(bool pcarEntriesExist)
        {
            string Produce(string kind, int id, PcarEntryFilter filter)
            {
                if (filter == PcarEntryFilter.OnlyPcar && !pcarEntriesExist)
                    return string.Empty;
                var name = $"{kind}-{id}-{filter}";
                return WriteDayPdf(filter == PcarEntryFilter.OnlyPcar ? $"{name} - PCAR-Report.pdf" : $"{name}.pdf");
            }

            _guardLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()))
                .Returns<int, string>((id, _) => Produce("guard", id, PcarEntryFilter.All));
            _guardLogReportGenerator.Setup(z => z.GeneratePdfReportSmartWand(It.IsAny<int>()))
                .Returns<int>(id => Produce("wand", id, PcarEntryFilter.All));
            _guardLogReportGenerator.Setup(z => z.GeneratePdfReportFusion(It.IsAny<int>()))
                .Returns<int>(id => Produce("fusion", id, PcarEntryFilter.All));

            _guardLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PcarEntryFilter>()))
                .Returns<int, string, PcarEntryFilter>((id, _, f) => Produce("guard", id, f));
            _guardLogReportGenerator.Setup(z => z.GeneratePdfReportSmartWand(It.IsAny<int>(), It.IsAny<PcarEntryFilter>()))
                .Returns<int, PcarEntryFilter>((id, f) => Produce("wand", id, f));
            _guardLogReportGenerator.Setup(z => z.GeneratePdfReportFusion(It.IsAny<int>(), It.IsAny<PcarEntryFilter>()))
                .Returns<int, PcarEntryFilter>((id, f) => Produce("fusion", id, f));

            _keyVehicleLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<KvlStatusFilter>()))
                .Returns<int, KvlStatusFilter>((id, _) => Produce("kvl", id, PcarEntryFilter.All));
        }

        private void StubDailyLogBooks(List<ClientSiteLogBook> books) =>
            _clientDataProvider.Setup(z => z.GetClientSiteLogBooksForDailyLogBookGeneration(It.IsAny<DateTime>())).Returns(books);

        private void StubPeriodicLogBooks(List<ClientSiteLogBook> books) =>
            _clientDataProvider.Setup(z => z.GetClientSiteLogBooksForPeriodicLogBookGeneration(
                It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(books);

        private SiteLogUploadService CreateService()
        {
            var webHostEnvironment = new Mock<IWebHostEnvironment>();
            webHostEnvironment.SetupGet(z => z.WebRootPath).Returns(_webRoot);

            return new SiteLogUploadService(
                _clientDataProvider.Object,
                _guardLogReportGenerator.Object,
                _keyVehicleLogReportGenerator.Object,
                _dropboxService.Object,
                Options.Create(new CityWatch.Data.Helpers.EmailOptions { FromAddress = "noreply@test|CityWatch" }),
                webHostEnvironment.Object,
                Mock.Of<ILogger<SiteLogUploadService>>(),
                Options.Create(new CityWatch.Web.Helpers.Settings()),
                new ConfigurationBuilder().Build());
        }

        private void VerifyNoFilteredReportRequested()
        {
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PcarEntryFilter>()), Times.Never);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(It.IsAny<int>(), It.IsAny<PcarEntryFilter>()), Times.Never);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>(), It.IsAny<PcarEntryFilter>()), Times.Never);
        }

        /* ---------------- daily: Disable PCAR ---------------- */

        [TestMethod]
        public void Daily_DisablePcarOff_ReportsAreRequestedExactlyAsBefore()
        {
            var site = MakeSite();
            site.UploadGuardLog = site.UploadSWLog = site.UploadFusionLog = site.UploadKVLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(100, null), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(100), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>()), Times.Once);
            _keyVehicleLogReportGenerator.Verify(z => z.GeneratePdfReport(200, It.IsAny<KvlStatusFilter>()), Times.Once);
            VerifyNoFilteredReportRequested();
            Assert.AreEqual(4, _uploadedPaths.Count);
            Assert.IsFalse(_uploadedPaths.Any(p => p.Contains("PCAR-Report")));
        }

        [TestMethod]
        public void Daily_DisablePcarOn_LbSwFusionExcludePcar_KvUnchanged()
        {
            var site = MakeSite();
            site.UploadGuardLog = site.UploadSWLog = site.UploadFusionLog = site.UploadKVLog = true;
            site.DisablePcarDailyLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(100, null, PcarEntryFilter.ExcludePcar), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(100, PcarEntryFilter.ExcludePcar), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>(), PcarEntryFilter.ExcludePcar), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _keyVehicleLogReportGenerator.Verify(z => z.GeneratePdfReport(200, It.IsAny<KvlStatusFilter>()), Times.Once);
            Assert.AreEqual(4, _uploadedPaths.Count);
        }

        [TestMethod]
        public void Daily_DisablePcarOn_ForAPcarSite_IsIgnored()
        {
            var site = MakeSite(PatrolTouringMode.PCAR);
            site.UploadGuardLog = site.UploadSWLog = site.UploadFusionLog = true;
            site.DisablePcarDailyLog = true;   // e.g. set before the site became a PCAR site
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            VerifyNoFilteredReportRequested();
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(100, null), Times.Once);
        }

        [TestMethod]
        public void Daily_WeeklyDisablePcar_DoesNotAffectTheDailyDump()
        {
            var site = MakeSite();
            site.UploadGuardLog = true;
            site.DisablePcarWeeklyLog = site.DisablePcarMonthlyLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            VerifyNoFilteredReportRequested();
        }

        /* ---------------- daily: Schedule PCAR ---------------- */

        [TestMethod]
        public void Daily_SchedulePcar_ProducesOnlyPcarReports_WithTheirOwnNames()
        {
            var site = MakeSite();
            site.UploadPcarGuardLog = site.UploadPcarSWLog = site.UploadPcarFusionLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(100, null, PcarEntryFilter.OnlyPcar), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(100, PcarEntryFilter.OnlyPcar), Times.Once);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(100, PcarEntryFilter.OnlyPcar), Times.Once);

            // The normal dump is off for this site, so nothing else is produced.
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _keyVehicleLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<KvlStatusFilter>()), Times.Never);

            Assert.AreEqual(3, _uploadedPaths.Count);
            Assert.IsTrue(_uploadedPaths.All(p => p.EndsWith(" - PCAR-Report.pdf")), string.Join("\n", _uploadedPaths));
            // Same day folder as the normal daily dump.
            Assert.IsTrue(_uploadedPaths.All(p => p.Contains($"/{Yesterday:yyyyMMdd}/")));
        }

        [TestMethod]
        public void Daily_SchedulePcar_IsTrackedSeparatelyFromTheNormalDump()
        {
            var site = MakeSite();
            site.UploadGuardLog = true;
            site.UploadPcarGuardLog = site.UploadPcarSWLog = site.UploadPcarFusionLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            CollectionAssert.AreEquivalent(
                new[] { LogBookType.PcarGuardLog, LogBookType.PcarSmartWandLog, LogBookType.PcarFusionLog },
                _createdLogBooks.Select(b => b.Type).ToArray());

            // The normal guard log book (100) plus one PCAR tracking book per report.
            Assert.AreEqual(4, _markedUploaded.Count);
            Assert.IsTrue(_markedUploaded.Any(m => m.Id == 100 && !m.FileName.Contains("PCAR-Report")));
            Assert.AreEqual(3, _markedUploaded.Count(m => m.FileName.EndsWith(" - PCAR-Report.pdf")));
        }

        [TestMethod]
        public void Daily_SchedulePcar_NoPcarEntries_NothingSentOrRecorded()
        {
            StubGenerators(pcarEntriesExist: false);
            var site = MakeSite();
            site.UploadPcarGuardLog = site.UploadPcarSWLog = site.UploadPcarFusionLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));

            CreateService().ProcessDailyGuardLogsNew();

            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _createdLogBooks.Count);
            Assert.AreEqual(0, _markedUploaded.Count);
        }

        [TestMethod]
        public void Daily_SchedulePcar_AlreadySent_IsNotSentAgain()
        {
            var site = MakeSite();
            site.UploadPcarGuardLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday));
            _createdLogBooks.Add(new ClientSiteLogBook { Id = 555, ClientSiteId = SiteId, Type = LogBookType.PcarGuardLog, Date = Yesterday, DbxUploaded = true });

            CreateService().ProcessDailyGuardLogsNew();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<PcarEntryFilter>()), Times.Never);
            Assert.AreEqual(0, _uploadedPaths.Count);
        }

        [TestMethod]
        public void Daily_SchedulePcar_FusionFallsBackToTheKvLogBook()
        {
            var site = MakeSite();
            site.UploadPcarFusionLog = true;
            StubDailyLogBooks(MakeDay(site, Yesterday).Where(b => b.Type == LogBookType.VehicleAndKeyLog).ToList());

            CreateService().ProcessDailyGuardLogsNew();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(200, PcarEntryFilter.OnlyPcar), Times.Once);
        }

        [TestMethod]
        public void SecondRun_SchedulePcar_SendsButMarksNothing()
        {
            var site = MakeSite();
            site.UploadPcarGuardLog = site.UploadPcarSWLog = true;
            StubDailyLogBooks(MakeDay(site, DateTime.Now.Date));

            CreateService().ProcessDailyGuardLogsSecondRunNew();

            Assert.AreEqual(2, _uploadedPaths.Count);
            Assert.AreEqual(0, _markedUploaded.Count);
            Assert.AreEqual(0, _createdLogBooks.Count);
        }

        [TestMethod]
        public void SecondRun_DisablePcarOn_ExcludesPcar()
        {
            var site = MakeSite();
            site.UploadGuardLog = true;
            site.DisablePcarDailyLog = true;
            StubDailyLogBooks(MakeDay(site, DateTime.Now.Date));

            CreateService().ProcessDailyGuardLogsSecondRunNew();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(100, null, PcarEntryFilter.ExcludePcar), Times.Once);
        }

        /* ---------------- weekly / monthly ---------------- */

        [TestMethod]
        public void Weekly_DisablePcarOn_ExcludesPcarFromLbSwFusion()
        {
            var site = MakeSite();
            site.UploadGuardWeeklyLog = site.UploadSWWeeklyLog = site.UploadFusionWeeklyLog = true;
            site.DisablePcarWeeklyLog = true;
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessWeeklyGuardLogs();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), null, PcarEntryFilter.ExcludePcar), Times.Exactly(7));
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(It.IsAny<int>(), PcarEntryFilter.ExcludePcar), Times.Exactly(7));
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>(), PcarEntryFilter.ExcludePcar), Times.Exactly(7));
            Assert.AreEqual(3, _uploadedPaths.Count);
            Assert.IsFalse(_uploadedPaths.Any(p => p.Contains("PCAR-Report")));
        }

        [TestMethod]
        public void Weekly_DisablePcarOff_RequestsReportsExactlyAsBefore()
        {
            var site = MakeSite();
            site.UploadGuardWeeklyLog = site.UploadSWWeeklyLog = site.UploadFusionWeeklyLog = true;
            site.DisablePcarDailyLog = site.DisablePcarMonthlyLog = true;   // other periods only
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessWeeklyGuardLogs();

            VerifyNoFilteredReportRequested();
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), null), Times.Exactly(7));
        }

        [TestMethod]
        public void Weekly_SchedulePcar_ProducesPcarDocuments_RecordedUnderPcarTypes()
        {
            var site = MakeSite();
            site.UploadPcarGuardWeeklyLog = site.UploadPcarSWWeeklyLog = site.UploadPcarFusionWeeklyLog = true;
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessWeeklyGuardLogs();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), null, PcarEntryFilter.OnlyPcar), Times.Exactly(7));
            Assert.AreEqual(3, _uploadedPaths.Count);
            Assert.IsTrue(_uploadedPaths.All(p => p.EndsWith("(Weekly) - PCAR-Report.pdf")), string.Join("\n", _uploadedPaths));
            Assert.IsTrue(_uploadedPaths.All(p => p.Contains("/WEEKLY LOGS/")));
            Assert.IsTrue(_uploadedPaths.Any(p => p.Contains("/Daily Guard Log - Martha Cove Marina - ")));

            CollectionAssert.AreEquivalent(
                new[] { LogBookType.PcarGuardLog, LogBookType.PcarSmartWandLog, LogBookType.PcarFusionLog },
                _recordedUploads.Select(r => r.LogBookType).ToArray());
            Assert.IsTrue(_recordedUploads.All(r => r.FileName.EndsWith(" - PCAR-Report.pdf")));
        }

        [TestMethod]
        public void Weekly_NormalAndPcar_HaveDistinctFileNames()
        {
            var site = MakeSite();
            site.UploadGuardWeeklyLog = true;
            site.UploadPcarGuardWeeklyLog = true;
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessWeeklyGuardLogs();

            Assert.AreEqual(2, _uploadedPaths.Count);
            Assert.AreEqual(2, _uploadedPaths.Select(Path.GetFileName).Distinct().Count());
            Assert.AreEqual(1, _uploadedPaths.Count(p => p.EndsWith("(Weekly).pdf")));
            Assert.AreEqual(1, _uploadedPaths.Count(p => p.EndsWith("(Weekly) - PCAR-Report.pdf")));
        }

        [TestMethod]
        public void Weekly_NormalDumpAlreadyProduced_DoesNotBlockThePcarDump()
        {
            var site = MakeSite();
            site.UploadGuardWeeklyLog = true;
            site.UploadPcarGuardWeeklyLog = true;
            StubPeriodicLogBooks(MakeWeek(site));
            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>
                {
                    new() { ClientSiteId = SiteId, LogBookType = LogBookType.DailyGuardLog, PeriodType = LogDumpPeriodType.Weekly, PeriodStartDate = WeekStart }
                });

            CreateService().ProcessWeeklyGuardLogs();

            Assert.AreEqual(1, _uploadedPaths.Count);
            StringAssert.EndsWith(_uploadedPaths.Single(), "(Weekly) - PCAR-Report.pdf");
        }

        [TestMethod]
        public void Weekly_SchedulePcar_NoPcarEntriesInThePeriod_NothingSent()
        {
            StubGenerators(pcarEntriesExist: false);
            var site = MakeSite();
            site.UploadPcarGuardWeeklyLog = site.UploadPcarSWWeeklyLog = site.UploadPcarFusionWeeklyLog = true;
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessWeeklyGuardLogs();

            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _recordedUploads.Count);
        }

        [TestMethod]
        public void Monthly_SchedulePcarAndDisablePcar_UseTheMonthlyFlags()
        {
            var site = MakeSite();
            site.UploadGuardMonthlyLog = true;
            site.DisablePcarMonthlyLog = true;
            site.UploadPcarSWMonthlyLog = true;
            site.UploadPcarSWWeeklyLog = false;
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessMonthlyGuardLogs();

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), null, PcarEntryFilter.ExcludePcar), Times.Exactly(7));
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(It.IsAny<int>(), PcarEntryFilter.OnlyPcar), Times.Exactly(7));
            Assert.AreEqual(1, _uploadedPaths.Count(p => p.EndsWith("(Monthly) - PCAR-Report.pdf")));
            Assert.AreEqual(1, _uploadedPaths.Count(p => p.EndsWith("(Monthly).pdf")));
            Assert.IsTrue(_uploadedPaths.All(p => p.Contains("/MONTHLY LOGS/")));
        }

        [TestMethod]
        public void Weekly_SchedulePcarFlags_DoNotTriggerTheMonthlyRun()
        {
            var site = MakeSite();
            site.UploadPcarGuardWeeklyLog = true;
            StubPeriodicLogBooks(MakeWeek(site));

            CreateService().ProcessMonthlyGuardLogs();

            Assert.AreEqual(0, _uploadedPaths.Count);
        }
    }
}
