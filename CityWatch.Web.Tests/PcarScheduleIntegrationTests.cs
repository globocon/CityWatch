using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Integration tests for "Disable PCAR" and "Schedule PCAR", against the real database and the
    /// real report generators, for Martha Cove Marina (390), Liuzzi - Hive Shopping [High Risk] (33)
    /// and Liuzzi - Milleara Mall (34).
    ///
    /// The scheduler itself (SiteLogUploadService) runs unmodified. What is kept out:
    ///  - Dropbox: a fake that copies every document it is handed into PcarSampleReports/ instead
    ///    of uploading it - those copies are the sample reports.
    ///  - Email: the site's recipients are blanked in memory, so the scheduler never opens SMTP.
    ///  - Database writes by the scheduler (upload history, DbxUploaded, tracking rows): intercepted
    ///    by ScheduleClientDataProvider below and only counted. Every read goes to the database.
    ///
    /// Run them with: dotnet test CityWatch.Web.Tests --filter "TestCategory=PcarIntegration"
    /// Override the database with CITYWATCH_TEST_CONNECTION.
    ///
    /// Data used:
    ///  - Martha Cove Marina: one PCAR entry ever, a smart wand scan on 05-Oct-2026 by
    ///    "Citywatch M1 - Romeo Patrol Cars" (625). Samples cover that day, week and month.
    ///  - Milleara Mall: PCAR wand scans and PCAR image uploads every day. Samples cover 30-Sep-2026,
    ///    the last completed week (28-Sep to 04-Oct) and September 2026 - what the scheduler sends.
    ///  - Mercy - VIC - Aged Care (Abbotsford) (563): every guard log entry is a PCAR entry. Daily
    ///    30-Sep-2026 and the week 28-Sep to 04-Oct.
    ///  - Hive Shopping: no PCAR entries at all - same week and month, daily on 29-Sep-2026 (the site
    ///    has no log book on the 30th); its PCAR dumps must not be produced
    ///    and "Disable PCAR" must change nothing.
    ///
    /// Samples land in PcarSampleReports/&lt;site&gt;/&lt;period&gt;/&lt;scenario&gt;/ with a _run-log.txt
    /// of what the scheduler did.
    /// </summary>
    [TestClass]
    [TestCategory("PcarIntegration")]
    public class PcarScheduleIntegrationTests
    {
        private const int MarthaCoveSiteId = 390;
        private const int HiveShoppingSiteId = 33;
        private const int MillearaMallSiteId = 34;
        private const int MercyAbbotsfordSiteId = 563;
        private const int RomeoPcarSiteId = 625;
        private static readonly DateTime PcarDay = new(2026, 10, 5);

        private sealed record SiteScenario(int SiteId, string Folder, DateTime Day, (DateTime, DateTime) Week, (DateTime, DateTime) Month);

        private static readonly SiteScenario MarthaCove = new(MarthaCoveSiteId, "Martha Cove Marina", PcarDay,
            (new DateTime(2026, 10, 5), new DateTime(2026, 10, 11)), (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)));
        private static readonly SiteScenario HiveShopping = new(HiveShoppingSiteId, "Liuzzi - Hive Shopping [High Risk]", new DateTime(2026, 9, 29), // no log book on 30-Sep
            (new DateTime(2026, 9, 28), new DateTime(2026, 10, 4)), (new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)));
        private static readonly SiteScenario MillearaMall = new(MillearaMallSiteId, "Liuzzi - Milleara Mall", new DateTime(2026, 9, 30),
            (new DateTime(2026, 9, 28), new DateTime(2026, 10, 4)), (new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)));

        // Every entry of this site's guard log is made by a patrol car.
        private static readonly SiteScenario MercyAbbotsford = new(MercyAbbotsfordSiteId, "Mercy - VIC - Aged Care (Abbotsford)", new DateTime(2026, 9, 30),
            (new DateTime(2026, 9, 28), new DateTime(2026, 10, 4)), (new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)));

        // Wand scans are tagged "[PCAR NFC]" when a patrol car made them and "[NFC]"/"[BLE]" otherwise.
        private const string PcarNote = "[PCAR NFC]";
        private const string NonPcarWandNote = "Developer Test Point 1";
        private static readonly string[] NonPcarWandTags = { " [NFC]", " [BLE]" };

        private const string DefaultConnection =
            "Server=.\\SQLSERVER2025;Database=prod-citywatch;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        private static ServiceProvider _services;
        private static string _webRoot;
        private static string _samplesRoot;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            var connectionString = Environment.GetEnvironmentVariable("CITYWATCH_TEST_CONNECTION") ?? DefaultConnection;
            var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var webProjectDir = Path.Combine(solutionDir, "CityWatch.Web");
            _webRoot = Path.Combine(webProjectDir, "wwwroot");
            _samplesRoot = Path.Combine(solutionDir, "PcarSampleReports");

            // The guard log report loads wwwroot/images/... by relative path, as it does when the web
            // app runs from its project folder.
            Environment.CurrentDirectory = webProjectDir;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(webProjectDir)
                .AddJsonFile("appsettings.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string> { ["ConnectionStrings:DefaultConnection"] = connectionString })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddSignalR();
            services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(_webRoot));
            services.Configure<Settings>(configuration.GetSection(Settings.Name));
            services.AddDbContext<CityWatchDbContext>(o => o.UseSqlServer(connectionString));
            /* The report generators look a log book up with GetClientSiteLogBooks().SingleOrDefault(Id),
               which loads every active log book (117k) per report. Production behaviour, but a month of
               reports then takes hours on a dev machine. The generators here get that list for the
               sites under test only - the log book they look up is always among them, so the reports
               are unchanged. Everything else goes to the real provider. */
            services.AddScoped<ClientDataProvider>();
            services.AddScoped<IClientDataProvider>(sp => SiteScopedClientDataProvider.Create(
                sp.GetRequiredService<ClientDataProvider>(), sp.GetRequiredService<CityWatchDbContext>(),
                new[] { MarthaCoveSiteId, HiveShoppingSiteId, MillearaMallSiteId, MercyAbbotsfordSiteId }));
            services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
            services.AddScoped<IClientSiteWandDataProvider, ClientSiteWandDataProvider>();
            services.AddScoped<IGuardLogDataProvider, GuardLogDataProvider>();
            services.AddScoped<IGuardDataProvider, GuardDataProvider>();
            services.AddScoped<IGuardLoginDetailService, GuardLoginDetailService>();
            services.AddScoped<ILogbookDataService, LogbookDataService>();
            services.AddScoped<IGuardLogReportGenerator, GuardLogReportGenerator>();
            // The KV report generator's dependencies. ViewDataService also takes a Dropbox service:
            // a strict mock, so any call to it fails the test instead of reaching Dropbox.
            services.AddScoped<IUserDataProvider, UserDataProvider>();
            services.AddScoped<IGuardSettingsDataProvider, GuardSettingsDataProvider>();
            services.AddScoped<IAppConfigurationProvider, AppConfigurationProvider>();
            services.AddScoped<IIrDataProvider, IrDataProvider>();
            services.AddSingleton(new Mock<IDropboxService>(MockBehavior.Strict).Object);
            services.AddScoped<IViewDataService, ViewDataService>();
            services.AddScoped<IAuditLogViewDataService, AuditLogViewDataService>();
            services.AddScoped<IKeyVehicleLogReportGenerator, KeyVehicleLogReportGenerator>();
            _services = services.BuildServiceProvider();
        }

        [ClassCleanup]
        public static void Cleanup() => _services?.Dispose();

        /* ======================= the scenarios (also the sample reports) ======================= */

        private enum Mode { DisablePcarOff, DisablePcarOn, SchedulePcar }

        private static void Configure(ClientSite site, Mode mode)
        {
            // Every switch off first, then the scenario's own.
            foreach (var p in typeof(ClientSite).GetProperties().Where(p => p.PropertyType == typeof(bool) && p.CanWrite &&
                         (p.Name.StartsWith("Upload") || p.Name.StartsWith("DisablePcar"))))
                p.SetValue(site, false);

            site.GuardLogEmailTo = site.GuardLogEmailWeeklyLogTo = site.GuardLogEmailMonthlyLogTo = string.Empty; // never mail

            switch (mode)
            {
                case Mode.DisablePcarOff:
                case Mode.DisablePcarOn:
                    site.UploadGuardLog = site.UploadKVLog = site.UploadSWLog = site.UploadFusionLog = true;
                    site.UploadGuardWeeklyLog = site.UploadKVWeeklyLog = site.UploadSWWeeklyLog = site.UploadFusionWeeklyLog = true;
                    site.UploadGuardMonthlyLog = site.UploadKVMonthlyLog = site.UploadSWMonthlyLog = site.UploadFusionMonthlyLog = true;
                    site.DisablePcarDailyLog = site.DisablePcarWeeklyLog = site.DisablePcarMonthlyLog = mode == Mode.DisablePcarOn;
                    break;
                case Mode.SchedulePcar:
                    site.UploadPcarGuardLog = site.UploadPcarSWLog = site.UploadPcarFusionLog = true;
                    site.UploadPcarGuardWeeklyLog = site.UploadPcarSWWeeklyLog = site.UploadPcarFusionWeeklyLog = true;
                    site.UploadPcarGuardMonthlyLog = site.UploadPcarSWMonthlyLog = site.UploadPcarFusionMonthlyLog = true;
                    break;
            }
        }

        private static string Folder(SiteScenario site, string period, Mode mode) => Path.Combine(site.Folder, period, mode switch
        {
            Mode.DisablePcarOff => "1 - Schedule - Disable PCAR OFF",
            Mode.DisablePcarOn => "2 - Schedule - Disable PCAR ON",
            _ => "3 - Schedule PCAR"
        });

        private static RunResult RunDaily(Mode mode) => RunDaily(MarthaCove, mode);

        private static RunResult RunDaily(SiteScenario site, Mode mode) =>
            Run(site.SiteId, Folder(site, "Daily", mode), mode, site.Day, site.Day, (svc, _) => svc.ProcessDailyGuardLogsNew());

        private static RunResult RunPeriodic(LogDumpPeriodType periodType, Mode mode) => RunPeriodic(MarthaCove, periodType, mode);

        private static RunResult RunPeriodic(SiteScenario site, LogDumpPeriodType periodType, Mode mode)
        {
            var (start, end) = periodType == LogDumpPeriodType.Weekly ? site.Week : site.Month;
            return Run(site.SiteId, Folder(site, periodType.ToString(), mode), mode, start, end, (svc, _) =>
            {
                // The run covers the period it is given; the public entry points always pass the last
                // completed one, which for this site holds no PCAR entry. Same method, chosen period.
                var method = typeof(SiteLogUploadService).GetMethod("ProcessPeriodicGuardLogs", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Invoke(svc, new object[] { periodType, (start, end) });
            });
        }

        private sealed class RunResult
        {
            public List<string> Files { get; } = new();
            public ScheduleClientDataProvider Provider { get; set; }
            public string Folder { get; set; }
            public string Find(string contains) => Files.SingleOrDefault(f => Path.GetFileName(f).Contains(contains));
        }

        private static RunResult Run(int siteId, string folder, Mode mode, DateTime from, DateTime to,
            Action<SiteLogUploadService, ScheduleClientDataProvider> run)
        {
            var result = new RunResult { Folder = Path.Combine(_samplesRoot, folder) };
            if (Directory.Exists(result.Folder))
                Directory.Delete(result.Folder, recursive: true);
            Directory.CreateDirectory(result.Folder);

            using var scope = _services.CreateScope();
            var sp = scope.ServiceProvider;
            var context = sp.GetRequiredService<CityWatchDbContext>();

            // Untracked, so the scenario's settings never reach the database.
            var logBooks = context.ClientSiteLogBooks.AsNoTracking()
                .Include(x => x.ClientSite).ThenInclude(x => x.ClientType)
                .Where(x => x.ClientSiteId == siteId && x.Date >= from && x.Date <= to &&
                            (x.Type == LogBookType.DailyGuardLog || x.Type == LogBookType.VehicleAndKeyLog))
                .ToList();
            Assert.IsTrue(logBooks.Any(), $"No log books for site {siteId} in the period.");

            var site = logBooks.First().ClientSite;
            foreach (var book in logBooks)
                book.ClientSite = site;
            Configure(site, mode);

            var provider = ScheduleClientDataProvider.Create(sp.GetRequiredService<IClientDataProvider>(), logBooks);
            result.Provider = (ScheduleClientDataProvider)(object)provider;

            var dropbox = new Mock<IDropboxService>();
            dropbox.Setup(z => z.Upload(It.IsAny<DropboxSettings>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<DropboxSettings, string, string>((_, localFile, _) =>
                {
                    var target = Path.Combine(result.Folder, Path.GetFileName(localFile));
                    File.Copy(localFile, target, overwrite: true);
                    result.Files.Add(target);
                })
                .ReturnsAsync(true);

            var service = new SiteLogUploadService(
                provider,
                sp.GetRequiredService<IGuardLogReportGenerator>(),
                sp.GetRequiredService<IKeyVehicleLogReportGenerator>(),
                dropbox.Object,
                Options.Create(new CityWatch.Data.Helpers.EmailOptions { FromAddress = "noreply@test|CityWatch" }),
                sp.GetRequiredService<IWebHostEnvironment>(),
                Mock.Of<ILogger<SiteLogUploadService>>(),
                Options.Create(new Settings()),
                new ConfigurationBuilder().Build());

            run(service, result.Provider);

            File.WriteAllText(Path.Combine(result.Folder, "_run-log.txt"),
                string.Join(Environment.NewLine, result.Provider.History));
            return result;
        }

        private static string Text(string pdf)
        {
            using var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(pdf));
            var sb = new StringBuilder();
            for (var i = 1; i <= document.GetNumberOfPages(); i++)
                sb.AppendLine(iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(document.GetPage(i)));
            return sb.ToString();
        }

        /* ======================= daily ======================= */

        [TestMethod]
        public void Daily_DisablePcarOff_ReportsStillContainThePcarEntry()
        {
            var run = RunDaily(Mode.DisablePcarOff);

            // LB, SW, Fusion. Martha Cove has no key and vehicle log book in October, so no KV report.
            Assert.AreEqual(3, run.Files.Count, string.Join("\n", run.Files));
            StringAssert.Contains(Text(run.Find("Smart Wand Log")), PcarNote);
            StringAssert.Contains(Text(run.Find("Fusion Log")), PcarNote);
            Assert.IsFalse(run.Files.Any(f => f.Contains("PCAR-Report")));
            Assert.AreEqual(0, run.Provider.EmailRecipientsSeen, "No recipient may be seen by the scheduler.");
        }

        [TestMethod]
        public void Daily_DisablePcarOn_PcarEntryIsLeftOut_EverythingElseKept()
        {
            var off = RunDaily(Mode.DisablePcarOff);
            var on = RunDaily(Mode.DisablePcarOn);

            Assert.AreEqual(3, on.Files.Count, string.Join("\n", on.Files));
            var swOn = Text(on.Find("Smart Wand Log"));
            var fusionOn = Text(on.Find("Fusion Log"));

            Assert.IsFalse(swOn.Contains(PcarNote), "Smart wand report still lists the PCAR scan.");
            Assert.IsFalse(fusionOn.Contains(PcarNote), "Fusion report still lists the PCAR scan.");
            StringAssert.Contains(swOn, NonPcarWandNote);
            StringAssert.Contains(fusionOn, NonPcarWandNote);

            // The guard log report holds no wand scans, so this day's PCAR scan never was in it.
            Assert.AreEqual(Text(off.Find("Daily Guard Log")), Text(on.Find("Daily Guard Log")));

            // Same file names as the normal dump - "Disable PCAR" changes the content, not the report.
            CollectionAssert.AreEquivalent(off.Files.Select(Path.GetFileName).ToArray(), on.Files.Select(Path.GetFileName).ToArray());
        }

        [TestMethod]
        public void Daily_SchedulePcar_OnlyThePcarEntry_InPcarReportFiles()
        {
            var run = RunDaily(Mode.SchedulePcar);

            // SW and Fusion: the PCAR scan. LB: no PCAR entry that day, so no file at all.
            Assert.AreEqual(2, run.Files.Count, string.Join("\n", run.Files));
            Assert.IsTrue(run.Files.All(f => f.EndsWith(" - PCAR-Report.pdf")));
            Assert.IsNull(run.Find("Daily Guard Log -"));

            foreach (var file in run.Files)
            {
                var text = Text(file);
                StringAssert.Contains(text, PcarNote, file);
                Assert.IsFalse(text.Contains(NonPcarWandNote), $"{file} contains a non-PCAR entry.");
            }

            // Tracked under the PCAR types - intercepted, not written.
            CollectionAssert.AreEquivalent(new[] { LogBookType.PcarSmartWandLog, LogBookType.PcarFusionLog },
                run.Provider.LogBooksCreated.Select(x => x.Type).ToArray());
        }

        /* ======================= weekly / monthly ======================= */

        [TestMethod]
        public void Weekly_AllCombinations()
        {
            var off = RunPeriodic(LogDumpPeriodType.Weekly, Mode.DisablePcarOff);
            var on = RunPeriodic(LogDumpPeriodType.Weekly, Mode.DisablePcarOn);
            var pcar = RunPeriodic(LogDumpPeriodType.Weekly, Mode.SchedulePcar);

            AssertPeriodic(off, on, pcar, "(Weekly)");
        }

        [TestMethod]
        public void Monthly_AllCombinations()
        {
            var off = RunPeriodic(LogDumpPeriodType.Monthly, Mode.DisablePcarOff);
            var on = RunPeriodic(LogDumpPeriodType.Monthly, Mode.DisablePcarOn);
            var pcar = RunPeriodic(LogDumpPeriodType.Monthly, Mode.SchedulePcar);

            AssertPeriodic(off, on, pcar, "(Monthly)");
        }

        private static void AssertPeriodic(RunResult off, RunResult on, RunResult pcar, string periodTag)
        {
            Assert.AreEqual(3, off.Files.Count, string.Join("\n", off.Files));
            Assert.AreEqual(3, on.Files.Count, string.Join("\n", on.Files));

            StringAssert.Contains(Text(off.Find("Smart Wand Log")), PcarNote);
            StringAssert.Contains(Text(off.Find("Fusion Log")), PcarNote);
            Assert.IsFalse(Text(on.Find("Smart Wand Log")).Contains(PcarNote));
            Assert.IsFalse(Text(on.Find("Fusion Log")).Contains(PcarNote));
            Assert.IsTrue(off.Files.All(f => f.EndsWith($"{periodTag}.pdf")));

            Assert.AreEqual(2, pcar.Files.Count, string.Join("\n", pcar.Files));
            foreach (var file in pcar.Files)
            {
                StringAssert.EndsWith(file, $"{periodTag} - PCAR-Report.pdf");
                var text = Text(file);
                StringAssert.Contains(text, PcarNote);
                Assert.IsFalse(text.Contains(NonPcarWandNote));
            }

            Assert.IsTrue(pcar.Provider.PeriodicUploadsRecorded.All(r => PcarEntryFilterHelperIsPcar(r.LogBookType)));
        }

        /* ---------- every combination for Hive Shopping and Milleara Mall ---------- */

        [TestMethod] public void HiveShopping_Daily_AllCombinations() => AssertSite(HiveShopping, null, hasPcar: false);
        [TestMethod] public void HiveShopping_Weekly_AllCombinations() => AssertSite(HiveShopping, LogDumpPeriodType.Weekly, hasPcar: false);
        [TestMethod] public void HiveShopping_Monthly_AllCombinations() => AssertSite(HiveShopping, LogDumpPeriodType.Monthly, hasPcar: false);
        [TestMethod] public void MillearaMall_Daily_AllCombinations() => AssertSite(MillearaMall, null, hasPcar: true);
        [TestMethod] public void MillearaMall_Weekly_AllCombinations() => AssertSite(MillearaMall, LogDumpPeriodType.Weekly, hasPcar: true);
        [TestMethod] public void MillearaMall_Monthly_AllCombinations() => AssertSite(MillearaMall, LogDumpPeriodType.Monthly, hasPcar: true);

        private static void AssertSite(SiteScenario site, LogDumpPeriodType? periodType, bool hasPcar)
        {
            RunResult RunMode(Mode mode) => periodType == null ? RunDaily(site, mode) : RunPeriodic(site, periodType.Value, mode);

            var off = RunMode(Mode.DisablePcarOff);
            var on = RunMode(Mode.DisablePcarOn);
            var pcar = RunMode(Mode.SchedulePcar);

            // Disable PCAR changes what is in the reports, never which reports are sent or their names.
            Assert.IsTrue(off.Files.Count > 0, "The normal dump produced nothing.");
            CollectionAssert.AreEquivalent(off.Files.Select(Path.GetFileName).ToArray(), on.Files.Select(Path.GetFileName).ToArray());
            Assert.IsFalse(off.Files.Concat(on.Files).Any(f => f.Contains("PCAR-Report")));

            foreach (var file in on.Files)
                Assert.IsFalse(Text(file).Contains(PcarNote), $"{file} still lists a PCAR scan.");

            // The GPS column of the smart wand report is filled (it was blank for every row before).
            Assert.IsTrue(GoogleMapsLinkCount(off.Find("Smart Wand Log")) > 0, "Smart wand GPS column is blank.");
            Assert.IsTrue(GoogleMapsLinkCount(on.Find("Smart Wand Log")) > 0, "Smart wand GPS column is blank (Disable PCAR on).");

            var offWand = Text(off.Find("Smart Wand Log"));
            Assert.AreEqual(hasPcar, offWand.Contains(PcarNote), "PCAR scans in the normal smart wand report.");

            if (!hasPcar)
            {
                // Nothing to exclude: identical reports; nothing to send in a PCAR dump.
                foreach (var file in off.Files)
                    Assert.AreEqual(new FileInfo(file).Length > 0, true);
                Assert.AreEqual(0, pcar.Files.Count, string.Join("\n", pcar.Files));
                Assert.AreEqual(0, pcar.Provider.LogBooksCreated.Count + pcar.Provider.PeriodicUploadsRecorded.Count);
                return;
            }

            Assert.IsTrue(pcar.Files.Count >= 2, string.Join("\n", pcar.Files));
            foreach (var file in pcar.Files)
            {
                StringAssert.EndsWith(file, " - PCAR-Report.pdf");
                var text = Text(file);
                foreach (var tag in NonPcarWandTags)
                    Assert.IsFalse(text.Contains(tag), $"{file} contains a non-PCAR wand scan ({tag.Trim()}).");
            }
            StringAssert.Contains(Text(pcar.Find("Smart Wand Log")), PcarNote);
            StringAssert.Contains(Text(pcar.Find("Fusion Log")), PcarNote);
        }

        /* ---------- a site whose every entry is a PCAR entry ---------- */

        [TestMethod] public void MercyAbbotsford_Daily_AllCombinations() => AssertAllPcarSite(MercyAbbotsford, null);
        [TestMethod] public void MercyAbbotsford_Weekly_AllCombinations() => AssertAllPcarSite(MercyAbbotsford, LogDumpPeriodType.Weekly);

        private static void AssertAllPcarSite(SiteScenario site, LogDumpPeriodType? periodType)
        {
            RunResult RunMode(Mode mode) => periodType == null ? RunDaily(site, mode) : RunPeriodic(site, periodType.Value, mode);

            var off = RunMode(Mode.DisablePcarOff);
            var on = RunMode(Mode.DisablePcarOn);
            var pcar = RunMode(Mode.SchedulePcar);

            // Normal dump: everything, PCAR entries included, GPS filled.
            var offGuard = off.Find("Daily Guard Log -");
            var offWand = off.Find("Smart Wand Log");
            Assert.IsNotNull(offGuard, string.Join("\n", off.Files));
            Assert.IsNotNull(offWand, string.Join("\n", off.Files));
            Assert.IsNotNull(off.Find("Fusion Log"), string.Join("\n", off.Files));
            StringAssert.Contains(Text(offWand), PcarNote);
            var offGpsLinks = GoogleMapsLinkCount(offWand);
            Assert.IsTrue(offGpsLinks > 0, "Smart wand GPS column is blank.");

            // Disable PCAR on: nothing is left of the guard log, so that report is not sent; the
            // smart wand and fusion reports go out without a single PCAR scan.
            Assert.IsNull(on.Find("Daily Guard Log -"), "Guard log report sent although every entry is a PCAR entry.");
            foreach (var file in on.Files)
                Assert.IsFalse(Text(file).Contains(PcarNote), $"{file} still lists a PCAR scan.");
            Assert.AreEqual(0, GoogleMapsLinkCount(on.Find("Smart Wand Log")), "Scans left in the smart wand report.");

            // Schedule PCAR: the same entries as the normal dump, in "- PCAR-Report" files.
            Assert.AreEqual(3, pcar.Files.Count, string.Join("\n", pcar.Files));
            Assert.IsTrue(pcar.Files.All(f => f.EndsWith(" - PCAR-Report.pdf")));
            var pcarWand = pcar.Find("Smart Wand Log");
            StringAssert.Contains(Text(pcarWand), PcarNote);
            Assert.AreEqual(offGpsLinks, GoogleMapsLinkCount(pcarWand), "Every scan is a PCAR scan - same rows, same GPS.");
            foreach (var file in pcar.Files)
                foreach (var tag in NonPcarWandTags)
                    Assert.IsFalse(Text(file).Contains(tag), $"{file} contains a non-PCAR wand scan ({tag.Trim()}).");
        }

        private static bool PcarEntryFilterHelperIsPcar(LogBookType t) =>
            CityWatch.Data.Helpers.PcarEntryFilterHelper.IsPcarReportType(t);

        /* ======================= generator, row level ======================= */

        /// <summary>
        /// The Smart Wand report's GPS column: one Google Maps link per scan with GPS on its guard log.
        /// It was blank for every row before - the single-site query it uses never filled the field.
        /// </summary>
        [TestMethod]
        public void HiveShopping_SmartWandReport_GpsColumnIsFilled()
        {
            using var scope = _services.CreateScope();
            var sp = scope.ServiceProvider;
            var generator = sp.GetRequiredService<IGuardLogReportGenerator>();
            var guardLogs = sp.GetRequiredService<IGuardLogDataProvider>();
            var webRoot = sp.GetRequiredService<IWebHostEnvironment>().WebRootPath;

            var day = HiveShopping.Day;
            var book = sp.GetRequiredService<CityWatchDbContext>().ClientSiteLogBooks.AsNoTracking()
                .Single(x => x.ClientSiteId == HiveShoppingSiteId && x.Date == day && x.Type == LogBookType.DailyGuardLog);

            var scansWithGps = guardLogs.GetGuardFusionLogs(HiveShoppingSiteId, day, day, false)
                .Where(x => x.ActivityType.Trim().ToUpper() == "SW" && x.LBId.HasValue)
                .Select(x => x.LBId.Value).ToList();
            var expected = scansWithGps.Count(id => guardLogs.GetGuardLogGpsCoordinates(new[] { id }).ContainsKey(id));
            Assert.IsTrue(expected > 0, "Fixture: expected wand scans with GPS on the day.");

            // Hive Shopping has no PCAR entries, so its PCAR-only report is not produced at all.
            Assert.AreEqual(string.Empty, generator.GeneratePdfReportSmartWand(book.Id, PcarEntryFilter.OnlyPcar));

            foreach (var filter in new[] { PcarEntryFilter.All, PcarEntryFilter.ExcludePcar })
            {
                var file = Path.Combine(webRoot, "Pdf", "Output", generator.GeneratePdfReportSmartWand(book.Id, filter));
                var links = GoogleMapsLinkCount(file);

                // Nothing to exclude at this site: both reports have one GPS link per scan.
                Assert.AreEqual(expected, links, $"{filter}: one GPS link per scan with GPS.");

                File.Copy(file, Path.Combine(Directory.CreateDirectory(Path.Combine(_samplesRoot, "_GPS check")).FullName, Path.GetFileName(file)), overwrite: true);
                File.Delete(file);
            }
        }

        private static int GoogleMapsLinkCount(string pdf)
        {
            using var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(pdf));
            var count = 0;
            for (var i = 1; i <= document.GetNumberOfPages(); i++)
            {
                foreach (var annotation in document.GetPage(i).GetAnnotations())
                {
                    var action = annotation.GetPdfObject().GetAsDictionary(iText.Kernel.Pdf.PdfName.A);
                    var uri = action?.GetAsString(iText.Kernel.Pdf.PdfName.URI)?.ToUnicodeString();
                    if (uri != null && uri.Contains("google.com/maps"))
                        count++;
                }
            }
            return count;
        }

        [TestMethod]
        public void Generator_GuardLogReport_AllFilters()
        {
            using var scope = _services.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<IGuardLogReportGenerator>();
            var books = scope.ServiceProvider.GetRequiredService<CityWatchDbContext>().ClientSiteLogBooks.AsNoTracking()
                .Where(x => x.ClientSiteId == MarthaCoveSiteId && x.Date == PcarDay && x.Type == LogBookType.DailyGuardLog).ToList();

            foreach (var filter in new[] { PcarEntryFilter.All, PcarEntryFilter.ExcludePcar })
            {
                try
                {
                    var file = generator.GeneratePdfReport(books.Single().Id, null, filter);
                    Console.WriteLine($"{filter}: {file}");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"{filter}: {ex}");
                }
            }
        }

        [TestMethod]
        public void Generator_FilteredRowCounts_PartitionTheSmartWandAndFusionLogs()
        {
            using var scope = _services.CreateScope();
            var guardLogs = scope.ServiceProvider.GetRequiredService<IGuardLogDataProvider>();

            var rows = guardLogs.GetGuardFusionLogs(new[] { MarthaCoveSiteId }, PcarDay, PcarDay, false);
            var pcarIds = guardLogs.GetPcarGuardLogIds(rows.Where(x => x.LBId.HasValue).Select(x => x.LBId.Value));

            var excluded = CityWatch.Data.Helpers.PcarEntryFilterHelper.Apply(rows, pcarIds, PcarEntryFilter.ExcludePcar);
            var only = CityWatch.Data.Helpers.PcarEntryFilterHelper.Apply(rows, pcarIds, PcarEntryFilter.OnlyPcar);

            Assert.AreEqual(1, pcarIds.Count, "Expected the single PCAR scan of 05-Oct-2026.");
            Assert.AreEqual(1, only.Count);
            Assert.AreEqual(rows.Count, excluded.Count + only.Count);
            StringAssert.Contains(only.Single().Notes ?? only.Single().SwNotes ?? "", "PCAR");
        }

        /* ======================= settings, saved to the database ======================= */

        [TestMethod]
        public void Settings_AreSavedAndReadBack_AndDisablePcarStaysOffForAPcarSite()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CityWatchDbContext>();
            var provider = scope.ServiceProvider.GetRequiredService<IClientDataProvider>();

            // Rolled back at the end: the dev database is left as it was.
            using var transaction = context.Database.BeginTransaction();

            provider.SaveClientSiteDisablePcarSettings(MarthaCoveSiteId, true, false, true);
            provider.SaveClientSiteDisablePcarSettings(RomeoPcarSiteId, true, true, true);
            provider.SaveClientSitePcarScheduleSettings(MarthaCoveSiteId, true, false, true, false, true, false, true, true, false);

            context.ChangeTracker.Clear();
            var marthaCove = provider.GetNewClientSites(MarthaCoveSiteId).Single();
            var romeo = context.ClientSites.AsNoTracking().Single(x => x.Id == RomeoPcarSiteId);

            Assert.IsTrue(marthaCove.DisablePcarDailyLog);
            Assert.IsFalse(marthaCove.DisablePcarWeeklyLog);
            Assert.IsTrue(marthaCove.DisablePcarMonthlyLog);
            Assert.IsTrue(marthaCove.IsDisablePcarApplicable);

            Assert.IsTrue(marthaCove.UploadPcarGuardLog);
            Assert.IsFalse(marthaCove.UploadPcarSWLog);
            Assert.IsTrue(marthaCove.UploadPcarFusionLog);
            Assert.IsFalse(marthaCove.UploadPcarGuardWeeklyLog);
            Assert.IsTrue(marthaCove.UploadPcarSWWeeklyLog);
            Assert.IsFalse(marthaCove.UploadPcarFusionWeeklyLog);
            Assert.IsTrue(marthaCove.UploadPcarGuardMonthlyLog);
            Assert.IsTrue(marthaCove.UploadPcarSWMonthlyLog);
            Assert.IsFalse(marthaCove.UploadPcarFusionMonthlyLog);

            Assert.AreEqual(PatrolTouringMode.PCAR, romeo.PatrolTourMode);
            Assert.IsFalse(romeo.IsDisablePcarApplicable);
            Assert.IsFalse(romeo.DisablePcarDailyLog || romeo.DisablePcarWeeklyLog || romeo.DisablePcarMonthlyLog,
                "Disable PCAR must never be saved on for a PCAR site.");

            transaction.Rollback();
        }

        [TestMethod]
        public void Settings_SavingTheSiteFromAnotherScreen_DoesNotResetThePcarSettings()
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CityWatchDbContext>();
            var provider = scope.ServiceProvider.GetRequiredService<IClientDataProvider>();
            using var transaction = context.Database.BeginTransaction();

            provider.SaveClientSiteDisablePcarSettings(MarthaCoveSiteId, true, true, true);
            provider.SaveClientSitePcarScheduleSettings(MarthaCoveSiteId, true, true, true, true, true, true, true, true, true);
            context.ChangeTracker.Clear();

            // A site posted from another form carries none of the PCAR fields.
            var posted = context.ClientSites.AsNoTracking().Single(x => x.Id == MarthaCoveSiteId);
            posted.DisablePcarDailyLog = false;
            posted.UploadPcarGuardLog = false;
            provider.SaveClientSite(posted);
            context.ChangeTracker.Clear();

            var saved = context.ClientSites.AsNoTracking().Single(x => x.Id == MarthaCoveSiteId);
            Assert.IsTrue(saved.DisablePcarDailyLog);
            Assert.IsTrue(saved.UploadPcarGuardLog);

            transaction.Rollback();
        }

        /* ======================= plumbing ======================= */

        /// <summary>
        /// The real IClientDataProvider for every read, except the log book selection (pinned to
        /// Martha Cove and the scenario's period, with the scenario's settings) and the Dropbox
        /// settings (forced active, so the run reaches the fake Dropbox). Every write the scheduler
        /// makes is intercepted and kept in memory.
        /// </summary>
        public class ScheduleClientDataProvider : DispatchProxy
        {
            private IClientDataProvider _inner;
            private List<ClientSiteLogBook> _logBooks;

            public List<string> History { get; } = new();
            public List<ClientSiteLogBook> LogBooksCreated { get; } = new();
            public List<ClientSitePeriodicLogUpload> PeriodicUploadsRecorded { get; } = new();
            public List<(int, string)> MarkedUploaded { get; } = new();
            public int EmailRecipientsSeen =>
                _logBooks.Select(x => x.ClientSite).Distinct()
                    .Count(s => !string.IsNullOrEmpty(s.GuardLogEmailTo) || !string.IsNullOrEmpty(s.GuardLogEmailWeeklyLogTo) || !string.IsNullOrEmpty(s.GuardLogEmailMonthlyLogTo));

            public static IClientDataProvider Create(IClientDataProvider inner, List<ClientSiteLogBook> logBooks)
            {
                var proxy = Create<IClientDataProvider, ScheduleClientDataProvider>();
                var self = (ScheduleClientDataProvider)(object)proxy;
                self._inner = inner;
                self._logBooks = logBooks;
                return proxy;
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                switch (targetMethod.Name)
                {
                    case nameof(IClientDataProvider.GetClientSiteLogBooksForDailyLogBookGeneration):
                    case nameof(IClientDataProvider.GetClientSiteLogBooksForPeriodicLogBookGeneration):
                        return _logBooks;

                    case nameof(IClientDataProvider.GetClientSiteKpiSetting) when args.Length == 1 && args[0] is int siteId:
                        var setting = _inner.GetClientSiteKpiSetting(siteId) ?? new ClientSiteKpiSetting { ClientSiteId = siteId };
                        setting.DropboxScheduleisActive = true;
                        if (string.IsNullOrEmpty(setting.DropboxImagesDir))
                            setting.DropboxImagesDir = "/Test";
                        return setting;

                    case nameof(IClientDataProvider.SaveSiteLogUploadHistory):
                        History.Add(((SiteLogUploadHistory)args[0]).LogDeatils);
                        return null;

                    case nameof(IClientDataProvider.MarkClientSiteLogBookAsUploaded):
                        MarkedUploaded.Add(((int)args[0], (string)args[1]));
                        return null;

                    case nameof(IClientDataProvider.SaveClientSiteLogBook):
                        var book = (ClientSiteLogBook)args[0];
                        book.Id = -(LogBooksCreated.Count + 1);
                        LogBooksCreated.Add(book);
                        return book.Id;

                    case nameof(IClientDataProvider.GetClientSiteLogBook):
                        var created = LogBooksCreated.FirstOrDefault(b => b.ClientSiteId == (int)args[0] && b.Type == (LogBookType)args[1] && b.Date == (DateTime)args[2]);
                        // Never "already sent" from a previous real run: the samples must always be produced.
                        return created ?? (PcarIsTracking((LogBookType)args[1]) ? null : targetMethod.Invoke(_inner, args));

                    case nameof(IClientDataProvider.GetPeriodicLogUploads):
                        return new List<ClientSitePeriodicLogUpload>();
                    case nameof(IClientDataProvider.SavePeriodicLogUpload):
                        PeriodicUploadsRecorded.Add((ClientSitePeriodicLogUpload)args[0]);
                        return null;
                    case nameof(IClientDataProvider.GetInProgressPeriodicLogDumpJob):
                        return null;
                    case nameof(IClientDataProvider.SavePeriodicLogDumpJob):
                        return 1;
                    case nameof(IClientDataProvider.SaveSchedulerTaskError):
                        History.Add("SchedulerTaskError: " + ((SchedulerTaskError)args[0]).ErrorMessage);
                        return null;
                }

                if (targetMethod.Name.StartsWith("Save") || targetMethod.Name.StartsWith("Delete") ||
                    targetMethod.Name.StartsWith("Mark") || targetMethod.Name.StartsWith("Update"))
                    throw new InvalidOperationException($"Unexpected write by the scheduler: {targetMethod.Name}");

                try
                {
                    return targetMethod.Invoke(_inner, args);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            private static bool PcarIsTracking(LogBookType type) =>
                CityWatch.Data.Helpers.PcarEntryFilterHelper.IsPcarReportType(type) ||
                type == LogBookType.SmartWandLog || type == LogBookType.FusionLog;
        }

        /// <summary>The real provider, with GetClientSiteLogBooks() limited to the sites under test.</summary>
        public class SiteScopedClientDataProvider : DispatchProxy
        {
            private IClientDataProvider _inner;
            private CityWatchDbContext _context;
            private int[] _siteIds;
            private List<ClientSiteLogBook> _logBooks;

            public static IClientDataProvider Create(IClientDataProvider inner, CityWatchDbContext context, int[] siteIds)
            {
                var proxy = Create<IClientDataProvider, SiteScopedClientDataProvider>();
                var self = (SiteScopedClientDataProvider)(object)proxy;
                self._inner = inner;
                self._context = context;
                self._siteIds = siteIds;
                return proxy;
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == nameof(IClientDataProvider.GetClientSiteLogBooks) && args.Length == 0)
                {
                    // Same query as ClientDataProvider.GetClientSiteLogBooks, for the test sites.
                    return _logBooks ??= _context.ClientSiteLogBooks
                        .Where(x => x.ClientSite.IsActive == true && _siteIds.Contains(x.ClientSiteId))
                        .Include(x => x.ClientSite)
                        .Include(x => x.ClientSite.ClientType)
                        .ToList();
                }

                try
                {
                    return targetMethod.Invoke(_inner, args);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }
        }

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
