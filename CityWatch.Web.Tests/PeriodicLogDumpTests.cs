using CityWatch.Common.Models;
using CityWatch.Common.Services;
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
    /// The weekly and monthly log dumps - "Enable Weekly/Monthly Log Dump" on a site's KPI settings.
    ///
    /// The settings and their ClientSites columns shipped with p2-178, but nothing read them: there
    /// was no periodic counterpart to ProcessDailyGuardLogsNew and no endpoint to call one, so sites
    /// with the boxes ticked received nothing. These cover the run that now does it, including the
    /// two things that keep it safe to schedule: a completed dump is recorded and never repeated,
    /// and every failure is recorded against the site and period it happened to.
    ///
    /// No database and no network. The report generators are mocked and write a real one-page PDF
    /// per day into a temporary web root, which is what lets the merge be checked by page count.
    /// The Dropbox service is mocked, and the recipients are left empty in most tests so no SMTP
    /// connection is attempted.
    /// </summary>
    [TestClass]
    public class PeriodicLogDumpTests
    {
        private const int SiteId = 61;
        private const string SiteName = "VISY Carrara [VR]";

        private string _webRoot;
        private string _outputDirectory;

        private Mock<IClientDataProvider> _clientDataProvider;
        private Mock<IGuardLogReportGenerator> _guardLogReportGenerator;
        private Mock<IKeyVehicleLogReportGenerator> _keyVehicleLogReportGenerator;
        private Mock<IDropboxService> _dropboxService;

        private readonly List<string> _uploadedPaths = new();
        private readonly List<ClientSitePeriodicLogUpload> _recordedUploads = new();
        private readonly List<SchedulerTaskError> _recordedErrors = new();
        private readonly List<PeriodicLogDumpJob> _savedJobs = new();

        /* The week the run will actually pick, whenever these are run.

           These were fixed dates to begin with - the week that was "previous" on the day they were
           written - and they passed until the calendar moved on, because the service takes its
           period from DateTime.Now and the assertions did not. Derived from the same rule the
           service uses, so the fixture and the run can no longer disagree. The arithmetic itself is
           pinned separately, by the tests below that pass GetPreviousWeek an explicit date. */
        private static readonly DateTime WeekStart = SiteLogUploadService.GetPreviousWeek(DateTime.Now.Date).PeriodStart;
        private static readonly DateTime WeekEnd = SiteLogUploadService.GetPreviousWeek(DateTime.Now.Date).PeriodEnd;

        [TestInitialize]
        public void Setup()
        {
            _webRoot = Path.Combine(Path.GetTempPath(), "cw-periodic-" + Guid.NewGuid().ToString("N"));
            _outputDirectory = Path.Combine(_webRoot, "Pdf", "Output");
            Directory.CreateDirectory(_outputDirectory);

            _uploadedPaths.Clear();
            _recordedUploads.Clear();
            _recordedErrors.Clear();
            _savedJobs.Clear();

            _clientDataProvider = new Mock<IClientDataProvider>();
            _guardLogReportGenerator = new Mock<IGuardLogReportGenerator>();
            _keyVehicleLogReportGenerator = new Mock<IKeyVehicleLogReportGenerator>();
            _dropboxService = new Mock<IDropboxService>();

            _clientDataProvider.Setup(z => z.GetClientSiteKpiSetting(It.IsAny<int>()))
                .Returns(new ClientSiteKpiSetting
                {
                    ClientSiteId = SiteId,
                    DropboxImagesDir = "/C4i/VISY Carrara",
                    DropboxScheduleisActive = true
                });

            // Nothing produced yet, unless a test says otherwise.
            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>());

            _clientDataProvider.Setup(z => z.SavePeriodicLogUpload(It.IsAny<ClientSitePeriodicLogUpload>()))
                .Callback<ClientSitePeriodicLogUpload>(r => _recordedUploads.Add(r));

            _clientDataProvider.Setup(z => z.SaveSchedulerTaskError(It.IsAny<SchedulerTaskError>()))
                .Callback<SchedulerTaskError>(e => _recordedErrors.Add(e));

            // Nothing in progress, unless a test says otherwise.
            _clientDataProvider.Setup(z => z.GetInProgressPeriodicLogDumpJob(It.IsAny<string>()))
                .Returns((PeriodicLogDumpJob)null);

            /* The run saves the same instance twice - once to open the job, once to close it - so
               only the first call records it; the assertions then read the final state. */
            _clientDataProvider.Setup(z => z.SavePeriodicLogDumpJob(It.IsAny<PeriodicLogDumpJob>()))
                .Callback<PeriodicLogDumpJob>(j => { if (!_savedJobs.Contains(j)) _savedJobs.Add(j); })
                .Returns(77);

            _dropboxService.Setup(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<DropboxSettings, string, string>((_, _, dbxPath) => _uploadedPaths.Add(dbxPath))
                .ReturnsAsync(true);
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

        private static ClientSite MakeSite(bool lb = false, bool kv = false, bool sw = false, bool fusion = false,
            string weeklyRecipients = "", bool monthlyLb = false, string monthlyRecipients = "")
        {
            return new ClientSite
            {
                Id = SiteId,
                Name = SiteName,
                IsActive = true,
                UploadGuardWeeklyLog = lb,
                UploadKVWeeklyLog = kv,
                UploadSWWeeklyLog = sw,
                UploadFusionWeeklyLog = fusion,
                GuardLogEmailWeeklyLogTo = weeklyRecipients,
                UploadGuardMonthlyLog = monthlyLb,
                GuardLogEmailMonthlyLogTo = monthlyRecipients
            };
        }

        private static List<ClientSiteLogBook> MakeWeek(ClientSite site, LogBookType type, int firstId)
        {
            return Enumerable.Range(0, 7)
                .Select(offset => new ClientSiteLogBook
                {
                    Id = firstId + offset,
                    ClientSiteId = site.Id,
                    ClientSite = site,
                    Type = type,
                    Date = WeekStart.AddDays(offset),
                    DbxUploaded = false
                })
                .ToList();
        }

        private string WriteDayPdf(string fileName)
        {
            var path = Path.Combine(_outputDirectory, fileName);

            using (var writer = new iText.Kernel.Pdf.PdfWriter(path))
            using (var document = new iText.Kernel.Pdf.PdfDocument(writer))
            {
                document.AddNewPage();
            }

            return fileName;
        }

        private void StubGeneratorsToProduceOnePagePerDay()
        {
            _guardLogReportGenerator
                .Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()))
                .Returns<int, string>((id, _) => WriteDayPdf($"guard-{id}.pdf"));

            _guardLogReportGenerator
                .Setup(z => z.GeneratePdfReportSmartWand(It.IsAny<int>()))
                .Returns<int>(id => WriteDayPdf($"wand-{id}.pdf"));

            _guardLogReportGenerator
                .Setup(z => z.GeneratePdfReportFusion(It.IsAny<int>()))
                .Returns<int>(id => WriteDayPdf($"fusion-{id}.pdf"));

            _keyVehicleLogReportGenerator
                .Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<KvlStatusFilter>()))
                .Returns<int, KvlStatusFilter>((id, _) => WriteDayPdf($"kvl-{id}.pdf"));
        }

        private void StubLogBooks(List<ClientSiteLogBook> books)
        {
            _clientDataProvider
                .Setup(z => z.GetClientSiteLogBooksForPeriodicLogBookGeneration(
                    It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(books);
        }

        private SiteLogUploadService CreateService()
        {
            var webHostEnvironment = new Mock<IWebHostEnvironment>();
            webHostEnvironment.SetupGet(z => z.WebRootPath).Returns(_webRoot);
            webHostEnvironment.SetupGet(z => z.EnvironmentName).Returns("Production");

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

        private static int PageCount(string path)
        {
            using var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(path));
            return document.GetNumberOfPages();
        }

        /* ---------------- which period is covered ---------------- */

        [TestMethod]
        public void OnItsMonday_TheWeekCoveredIsTheOneThatJustEnded()
        {
            // Monday 14 Sep 2026, the 2am run the settings screen promises.
            var (start, end) = SiteLogUploadService.GetPreviousWeek(new DateTime(2026, 9, 14));

            Assert.AreEqual(new DateTime(2026, 9, 7), start);    // Monday
            Assert.AreEqual(new DateTime(2026, 9, 13), end);     // Sunday
            Assert.AreEqual(DayOfWeek.Monday, start.DayOfWeek);
            Assert.AreEqual(DayOfWeek.Sunday, end.DayOfWeek);
        }

        [TestMethod]
        public void OnASunday_TheWeekCoveredIsStillTheLastCompletedOne()
        {
            /* DayOfWeek numbers Sunday as 0, so the obvious arithmetic puts Sunday in the following
               week and would dump a week that has not finished yet. */
            var (start, end) = SiteLogUploadService.GetPreviousWeek(new DateTime(2026, 9, 20)); // Sunday

            Assert.AreEqual(new DateTime(2026, 9, 7), start);
            Assert.AreEqual(new DateTime(2026, 9, 13), end);
        }

        [TestMethod]
        public void OnAnyDay_TheWeekIsAlwaysAWholeCompletedMondayToSunday()
        {
            // A scheduler that fires late, or an operator running it by hand, must not get a part week.
            foreach (var offset in Enumerable.Range(0, 28))
            {
                var today = new DateTime(2026, 9, 14).AddDays(offset);
                var (start, end) = SiteLogUploadService.GetPreviousWeek(today);

                Assert.AreEqual(DayOfWeek.Monday, start.DayOfWeek, $"start for {today:yyyy-MM-dd}");
                Assert.AreEqual(DayOfWeek.Sunday, end.DayOfWeek, $"end for {today:yyyy-MM-dd}");
                Assert.AreEqual(6, (end - start).Days);
                Assert.IsTrue(end < today, $"week end {end:yyyy-MM-dd} must be before {today:yyyy-MM-dd}");
            }
        }

        [TestMethod]
        public void TheMonthCovered_IsAlwaysTheLastCompletedCalendarMonth()
        {
            var (start, end) = SiteLogUploadService.GetPreviousMonth(new DateTime(2026, 9, 1));

            Assert.AreEqual(new DateTime(2026, 8, 1), start);
            Assert.AreEqual(new DateTime(2026, 8, 31), end);
        }

        [TestMethod]
        public void TheMonthCovered_HandlesJanuaryAndLeapFebruary()
        {
            // Year boundary.
            var (janStart, janEnd) = SiteLogUploadService.GetPreviousMonth(new DateTime(2026, 1, 1));
            Assert.AreEqual(new DateTime(2025, 12, 1), janStart);
            Assert.AreEqual(new DateTime(2025, 12, 31), janEnd);

            // 2024 is a leap year - the last day is the 29th, not a hard-coded 28.
            var (febStart, febEnd) = SiteLogUploadService.GetPreviousMonth(new DateTime(2024, 3, 15));
            Assert.AreEqual(new DateTime(2024, 2, 1), febStart);
            Assert.AreEqual(new DateTime(2024, 2, 29), febEnd);
        }

        /* ---------------- the dump itself ---------------- */

        [TestMethod]
        public void AWeekOfGuardLogs_IsMergedIntoOneDocumentAndUploaded()
        {
            var site = MakeSite(lb: true);
            var week = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100);
            StubLogBooks(week);
            StubGeneratorsToProduceOnePagePerDay();

            string mergedPath = null;
            _dropboxService.Setup(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<DropboxSettings, string, string>((_, localFile, dbxPath) =>
                {
                    _uploadedPaths.Add(dbxPath);
                    // Copied because the service deletes the merged file as soon as it is delivered.
                    mergedPath = localFile + ".kept";
                    File.Copy(localFile, mergedPath, overwrite: true);
                })
                .ReturnsAsync(true);

            CreateService().ProcessWeeklyGuardLogs();

            Assert.IsNotNull(mergedPath, "Nothing was uploaded.");
            Assert.AreEqual(7, PageCount(mergedPath), "The week's seven daily logs were not all merged.");

            foreach (var logBook in week)
                _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(logBook.Id, null), Times.Once);
        }

        [TestMethod]
        public void TheWeeklyDocument_IsFiledUnderItsOwnDropboxFolder()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            var uploaded = _uploadedPaths.Single();

            /* Its own folder, not a day folder: a periodic dump must never land on top of, or be
               mistaken for, the daily one. */
            StringAssert.Contains(uploaded, "/WEEKLY LOGS/");
            StringAssert.Contains(uploaded, $"/C4i/VISY Carrara/FLIR - Wand Recordings - IRs - Daily Logs/{WeekStart:yyyy}/");
            StringAssert.Contains(uploaded, $"{WeekStart:yyyyMMdd}-{WeekEnd:yyyyMMdd} (Weekly).pdf");
            StringAssert.Contains(uploaded, "Daily Guard Log");
        }

        [TestMethod]
        public void TheMonthlyDocument_IsFiledSeparatelyFromTheWeeklyOne()
        {
            var site = MakeSite(monthlyLb: true);
            StubLogBooks(MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessMonthlyGuardLogs();

            var uploaded = _uploadedPaths.Single();
            StringAssert.Contains(uploaded, "/MONTHLY LOGS/");
            StringAssert.Contains(uploaded, "(Monthly).pdf");
        }

        [TestMethod]
        public void TheWeeklyRun_NeverMarksALogBookAsUploaded()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            /* DbxUploaded belongs to the daily run. Setting it here would tell the daily dump that
               every day of the period had already been sent, silently turning the daily dump off
               for any site that also enables a periodic one. */
            _clientDataProvider.Verify(z => z.MarkClientSiteLogBookAsUploaded(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void OnlyTheTickedLogTypes_AreProduced()
        {
            var site = MakeSite(lb: true, kv: true);   // SW and Fusion off

            var books = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100);
            books.AddRange(MakeWeek(site, LogBookType.VehicleAndKeyLog, firstId: 200));
            StubLogBooks(books);
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            Assert.AreEqual(2, _uploadedPaths.Count, "Expected one document for LB and one for KV.");
            Assert.IsTrue(_uploadedPaths.Any(p => p.Contains("Daily Guard Log")));
            Assert.IsTrue(_uploadedPaths.Any(p => p.Contains("Key & Vehicle Log")));

            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(It.IsAny<int>()), Times.Never);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void TheWeeklyFlags_DoNotTriggerTheMonthlyRun()
        {
            // Weekly LB on, monthly off - a monthly run must produce nothing for this site.
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessMonthlyGuardLogs();

            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _recordedUploads.Count);
        }

        [TestMethod]
        public void SmartWandAndFusion_AreBuiltFromTheGuardLogBook()
        {
            StubLogBooks(MakeWeek(MakeSite(sw: true, fusion: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            /* Both reports take the daily guard log book id - smart wand entries live in the guard
               log - which is exactly how the daily run calls them. */
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportSmartWand(It.IsAny<int>()), Times.Exactly(7));
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>()), Times.Exactly(7));
            Assert.AreEqual(2, _uploadedPaths.Count);
        }

        [TestMethod]
        public void Fusion_FallsBackToTheKeyAndVehicleLogOnDaysWithNoGuardLog()
        {
            var site = MakeSite(fusion: true);

            var books = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100).Take(3).ToList();
            books.Add(new ClientSiteLogBook
            {
                Id = 250,
                ClientSiteId = site.Id,
                ClientSite = site,
                Type = LogBookType.VehicleAndKeyLog,
                Date = WeekStart.AddDays(5)
            });
            StubLogBooks(books);
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            // Four days covered, not three - the KV-only day is not dropped from the fusion week.
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(It.IsAny<int>()), Times.Exactly(4));
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReportFusion(250), Times.Once);
        }

        /* ---------------- not repeating a finished period ---------------- */

        [TestMethod]
        public void ACompletedDump_IsRecordedWithEverythingNeededToTraceIt()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            var recorded = _recordedUploads.Single();

            Assert.AreEqual(SiteId, recorded.ClientSiteId);
            Assert.AreEqual(LogDumpPeriodType.Weekly, recorded.PeriodType);
            Assert.AreEqual(LogBookType.DailyGuardLog, recorded.LogBookType);
            Assert.AreEqual(WeekStart, recorded.PeriodStartDate);
            Assert.AreEqual(WeekEnd, recorded.PeriodEndDate);
            Assert.AreEqual(7, recorded.DaysIncluded);
            Assert.IsTrue(recorded.DropboxUploaded);
            StringAssert.Contains(recorded.FileName, "(Weekly).pdf");
            StringAssert.Contains(recorded.FilePath, "/WEEKLY LOGS/");
            Assert.AreNotEqual(default, recorded.UploadedOn);
        }

        [TestMethod]
        public void APeriodAlreadyProduced_IsNotProducedAgain()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            // The scheduler already ran for this week.
            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(LogDumpPeriodType.Weekly, It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>
                {
                    new ClientSitePeriodicLogUpload
                    {
                        ClientSiteId = SiteId,
                        PeriodType = LogDumpPeriodType.Weekly,
                        LogBookType = LogBookType.DailyGuardLog,
                        PeriodStartDate = WeekStart,
                        PeriodEndDate = WeekEnd
                    }
                });

            CreateService().ProcessWeeklyGuardLogs();

            /* The whole point of the tracking table: a second run is a no-op, so a re-triggered
               task cannot send a site the same week twice. */
            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _recordedUploads.Count);
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void OnlyTheLogTypeAlreadyProduced_IsSkipped()
        {
            var site = MakeSite(lb: true, kv: true);
            var books = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100);
            books.AddRange(MakeWeek(site, LogBookType.VehicleAndKeyLog, firstId: 200));
            StubLogBooks(books);
            StubGeneratorsToProduceOnePagePerDay();

            // The guard log went out; the key and vehicle log did not.
            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(LogDumpPeriodType.Weekly, It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>
                {
                    new ClientSitePeriodicLogUpload
                    {
                        ClientSiteId = SiteId,
                        PeriodType = LogDumpPeriodType.Weekly,
                        LogBookType = LogBookType.DailyGuardLog,
                        PeriodStartDate = WeekStart
                    }
                });

            CreateService().ProcessWeeklyGuardLogs();

            // Tracking is per log type, so the half that failed last time still gets its chance.
            Assert.AreEqual(1, _uploadedPaths.Count);
            StringAssert.Contains(_uploadedPaths.Single(), "Key & Vehicle Log");
            Assert.AreEqual(LogBookType.VehicleAndKeyLog, _recordedUploads.Single().LogBookType);
        }

        [TestMethod]
        public void AWeeklyDump_DoesNotBlockTheMonthlyOneForTheSameSite()
        {
            var site = MakeSite(lb: true, monthlyLb: true);
            StubLogBooks(MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            // Weekly is done; the monthly run is a different period type and must still go.
            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(LogDumpPeriodType.Weekly, It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>
                {
                    new ClientSitePeriodicLogUpload
                    {
                        ClientSiteId = SiteId,
                        PeriodType = LogDumpPeriodType.Weekly,
                        LogBookType = LogBookType.DailyGuardLog,
                        PeriodStartDate = WeekStart
                    }
                });

            CreateService().ProcessMonthlyGuardLogs();

            Assert.AreEqual(1, _uploadedPaths.Count);
            Assert.AreEqual(LogDumpPeriodType.Monthly, _recordedUploads.Single().PeriodType);
        }

        [TestMethod]
        public void APeriodThatProducedNothing_IsNotRecordedAsDone()
        {
            var site = MakeSite(lb: true);

            // Enabled, but the week held only key and vehicle books - no guard log to dump.
            StubLogBooks(MakeWeek(site, LogBookType.VehicleAndKeyLog, firstId: 200));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            /* Nothing was produced, so there is nothing to avoid repeating - recording it would
               permanently suppress a dump that a later run could legitimately produce. */
            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _recordedUploads.Count);
        }

        /* ---------------- recording failures ---------------- */

        [TestMethod]
        public void AFailingLogType_IsRecordedAgainstItsSiteAndPeriod()
        {
            var site = MakeSite(lb: true, kv: true);
            var books = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100);
            books.AddRange(MakeWeek(site, LogBookType.VehicleAndKeyLog, firstId: 200));
            StubLogBooks(books);
            StubGeneratorsToProduceOnePagePerDay();

            _guardLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("guard log report is broken"));

            CreateService().ProcessWeeklyGuardLogs();

            var error = _recordedErrors.Single();
            Assert.AreEqual("Weekly Log Dump", error.TaskName);
            Assert.AreEqual(LogDumpPeriodType.Weekly, error.PeriodType);
            Assert.AreEqual(SiteId, error.ClientSiteId);
            Assert.AreEqual(LogBookType.DailyGuardLog, error.LogBookType);
            Assert.AreEqual(WeekStart, error.PeriodStartDate);
            Assert.AreEqual(WeekEnd, error.PeriodEndDate);
            StringAssert.Contains(error.ErrorMessage, "guard log report is broken");
            StringAssert.Contains(error.ErrorDetails, "InvalidOperationException");
        }

        [TestMethod]
        public void OneFailingLogType_DoesNotStopTheOthers()
        {
            var site = MakeSite(lb: true, kv: true);
            var books = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100);
            books.AddRange(MakeWeek(site, LogBookType.VehicleAndKeyLog, firstId: 200));
            StubLogBooks(books);
            StubGeneratorsToProduceOnePagePerDay();

            _guardLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("guard log report is broken"));

            CreateService().ProcessWeeklyGuardLogs();

            // The key and vehicle week still goes out, and is still recorded as done.
            Assert.AreEqual(1, _uploadedPaths.Count);
            StringAssert.Contains(_uploadedPaths.Single(), "Key & Vehicle Log");
            Assert.AreEqual(1, _recordedUploads.Count);
        }

        [TestMethod]
        public void AFailureToEvenStart_IsRecordedWithNoSiteAgainstIt()
        {
            _clientDataProvider
                .Setup(z => z.GetClientSiteLogBooksForPeriodicLogBookGeneration(
                    It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new TimeoutException("database unreachable"));

            CreateService().ProcessWeeklyGuardLogs();

            var error = _recordedErrors.Single();
            // No site id: this is the run failing, not one site failing, and the two must be
            // distinguishable when reading the table.
            Assert.IsNull(error.ClientSiteId);
            Assert.IsNull(error.LogBookType);
            Assert.AreEqual("Weekly Log Dump", error.TaskName);
            StringAssert.Contains(error.ErrorMessage, "database unreachable");
        }

        [TestMethod]
        public void AFailedDropboxUpload_IsRecordedButTheDumpStillCounts()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            _dropboxService.Setup(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new IOException("dropbox refused the file"));

            CreateService().ProcessWeeklyGuardLogs();

            // The document was produced, so the period is recorded - but honestly, as not uploaded.
            var recorded = _recordedUploads.Single();
            Assert.IsFalse(recorded.DropboxUploaded);
            Assert.IsNotNull(recorded.FileName);

            // And the reason is queryable rather than buried in free text.
            Assert.AreEqual(1, _recordedErrors.Count);
            StringAssert.Contains(_recordedErrors.Single().ErrorMessage, "dropbox refused the file");
        }

        /* ---------------- the run record ---------------- */

        [TestMethod]
        public void EveryRun_OpensAndClosesAJobRecord()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            var job = _savedJobs.Single();
            Assert.AreEqual("Weekly Log Dump", job.TaskName);
            Assert.AreEqual(LogDumpPeriodType.Weekly, job.PeriodType);
            Assert.AreEqual(WeekStart, job.PeriodStartDate);
            Assert.AreEqual(WeekEnd, job.PeriodEndDate);
            Assert.AreNotEqual(default, job.CreatedDate);

            // Closed: an open row blocks the task until a later run declares it stale.
            Assert.IsNotNull(job.CompletedDate);
            Assert.AreEqual(true, job.Success);

            Assert.AreEqual(1, job.DumpsProduced);
            Assert.AreEqual(0, job.DumpsSkipped);
            Assert.AreEqual(0, job.DumpsFailed);
            StringAssert.Contains(job.StatusMessage, "Produced 1, skipped 0, failed 0.");

            // Opened before the work and completed after it - two saves of the one row.
            _clientDataProvider.Verify(z => z.SavePeriodicLogDumpJob(It.IsAny<PeriodicLogDumpJob>()), Times.Exactly(2));
        }

        [TestMethod]
        public void ARunThatOverlapsAnother_DoesNotStart()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            _clientDataProvider.Setup(z => z.GetInProgressPeriodicLogDumpJob("Weekly Log Dump"))
                .Returns(new PeriodicLogDumpJob { Id = 5, TaskName = "Weekly Log Dump", CreatedDate = DateTime.Now });

            CreateService().ProcessWeeklyGuardLogs();

            /* Both runs would find the same periods unproduced - the tracking row is only written
               once a dump finishes - and would build the same documents into the same scratch file
               names. */
            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _recordedUploads.Count);
            Assert.AreEqual(0, _savedJobs.Count, "No job row should be opened for a run that was skipped.");
            _guardLogReportGenerator.Verify(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TheGuardIsPerTask_SoWeeklyDoesNotBlockMonthly()
        {
            StubLogBooks(MakeWeek(MakeSite(monthlyLb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            // A weekly run is in progress; the monthly task is unrelated and must still go.
            _clientDataProvider.Setup(z => z.GetInProgressPeriodicLogDumpJob("Weekly Log Dump"))
                .Returns(new PeriodicLogDumpJob { Id = 5, TaskName = "Weekly Log Dump", CreatedDate = DateTime.Now });

            CreateService().ProcessMonthlyGuardLogs();

            Assert.AreEqual(1, _uploadedPaths.Count);
            Assert.AreEqual("Monthly Log Dump", _savedJobs.Single().TaskName);
        }

        [TestMethod]
        public void AFailedDump_IsCountedAndMakesTheRunUnsuccessful()
        {
            var site = MakeSite(lb: true, kv: true);
            var books = MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100);
            books.AddRange(MakeWeek(site, LogBookType.VehicleAndKeyLog, firstId: 200));
            StubLogBooks(books);
            StubGeneratorsToProduceOnePagePerDay();

            _guardLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()))
                .Throws(new InvalidOperationException("guard log report is broken"));

            CreateService().ProcessWeeklyGuardLogs();

            var job = _savedJobs.Single();
            Assert.AreEqual(false, job.Success);
            Assert.AreEqual(1, job.DumpsProduced);   // the key and vehicle log still went
            Assert.AreEqual(1, job.DumpsFailed);
            StringAssert.Contains(job.StatusMessage, "guard log report is broken");
            Assert.IsNotNull(job.CompletedDate);
        }

        [TestMethod]
        public void ASkippedPeriod_IsCountedWithoutFailingTheRun()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            _clientDataProvider.Setup(z => z.GetPeriodicLogUploads(LogDumpPeriodType.Weekly, It.IsAny<DateTime>()))
                .Returns(new List<ClientSitePeriodicLogUpload>
                {
                    new ClientSitePeriodicLogUpload
                    {
                        ClientSiteId = SiteId,
                        PeriodType = LogDumpPeriodType.Weekly,
                        LogBookType = LogBookType.DailyGuardLog,
                        PeriodStartDate = WeekStart
                    }
                });

            CreateService().ProcessWeeklyGuardLogs();

            var job = _savedJobs.Single();
            Assert.AreEqual(true, job.Success, "Nothing failed - there was simply nothing left to do.");
            Assert.AreEqual(0, job.DumpsProduced);
            Assert.AreEqual(1, job.DumpsSkipped);
        }

        [TestMethod]
        public void ARunThatAbortsOutright_StillClosesItsJobRecord()
        {
            _clientDataProvider
                .Setup(z => z.GetClientSiteLogBooksForPeriodicLogBookGeneration(
                    It.IsAny<LogDumpPeriodType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new TimeoutException("database unreachable"));

            CreateService().ProcessWeeklyGuardLogs();

            /* Left open, this row would block every later run of the task until one declared it
               stale - a worse failure than the one that caused it. */
            var job = _savedJobs.Single();
            Assert.IsNotNull(job.CompletedDate);
            Assert.AreEqual(false, job.Success);
            StringAssert.Contains(job.StatusMessage, "Run aborted: database unreachable");
        }

        [TestMethod]
        public void ARunWithNoEligibleSites_IsRecordedAsASuccessfulEmptyRun()
        {
            StubLogBooks(new List<ClientSiteLogBook>());

            CreateService().ProcessWeeklyGuardLogs();

            var job = _savedJobs.Single();
            Assert.AreEqual(true, job.Success);
            Assert.AreEqual(0, job.DumpsProduced);
            Assert.IsNotNull(job.CompletedDate);
            StringAssert.Contains(job.StatusMessage, "No log books found");
        }

        /* ---------------- housekeeping ---------------- */

        [TestMethod]
        public void NothingIsSent_WhenNoDayProducedAPage()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));

            // The generator answers, but writes no file - the report could not be produced.
            _guardLogReportGenerator.Setup(z => z.GeneratePdfReport(It.IsAny<int>(), It.IsAny<string>()))
                .Returns("never-written.pdf");

            CreateService().ProcessWeeklyGuardLogs();

            Assert.AreEqual(0, _uploadedPaths.Count);
            Assert.AreEqual(0, _recordedUploads.Count);
        }

        [TestMethod]
        public void TheScratchDayFiles_AreCleanedUp()
        {
            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            /* Seven day PDFs plus a merged one per site per type, every week, would otherwise pile
               up in wwwroot indefinitely. */
            var leftOver = Directory.GetFiles(_outputDirectory, "*.pdf");
            CollectionAssert.AreEqual(Array.Empty<string>(), leftOver,
                "Left behind: " + string.Join(", ", leftOver.Select(Path.GetFileName)));
        }

        [TestMethod]
        public void ASiteWithDropboxSchedulingOff_IsNotUploadedTo()
        {
            _clientDataProvider.Setup(z => z.GetClientSiteKpiSetting(It.IsAny<int>()))
                .Returns(new ClientSiteKpiSetting
                {
                    ClientSiteId = SiteId,
                    DropboxImagesDir = "/C4i/VISY Carrara",
                    DropboxScheduleisActive = false
                });

            StubLogBooks(MakeWeek(MakeSite(lb: true), LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            // The daily upload honours this flag too; the periodic one must not bypass it.
            Assert.AreEqual(0, _uploadedPaths.Count);
            _dropboxService.Verify(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void SiteNamesWithPathCharacters_DoNotBreakTheFileName()
        {
            var site = MakeSite(lb: true);
            site.Name = @"VISY / Carrara: North\South";

            StubLogBooks(MakeWeek(site, LogBookType.DailyGuardLog, firstId: 100));
            StubGeneratorsToProduceOnePagePerDay();

            CreateService().ProcessWeeklyGuardLogs();

            // Site names are free text and end up in a path; a stray slash would write to nowhere.
            var fileName = Path.GetFileName(_uploadedPaths.Single());
            Assert.AreEqual(-1, fileName.IndexOfAny(Path.GetInvalidFileNameChars()), $"Bad file name: {fileName}");
            StringAssert.Contains(fileName, "(Weekly).pdf");
        }
    }
}
