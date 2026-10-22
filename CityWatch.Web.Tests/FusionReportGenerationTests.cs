using CityWatch.Data;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CityWatch.Web.Tests
{
    /// <summary>
    /// Integration tests for the Fusion logbook PDF, covering the linked-duress-site merge added to
    /// GuardLogReportGenerator.GeneratePdfReportFusion.
    ///
    /// These run against a real SQL Server database because the behaviour under test is a data join
    /// across ClientSiteRadioChecksActivityStatus_History and the RCLinkedDuress* tables - mocking the
    /// provider would only assert the mock. Override the target database with the
    /// CITYWATCH_TEST_CONNECTION environment variable; the default points at the instance currently
    /// holding the dev copy.
    ///
    /// Deliberately calls the generator directly rather than SiteLogUploadService.ProcessDailyGuardLogsNew(),
    /// which is what keeps email sending, the Dropbox upload and the post-upload file delete out of the
    /// picture - all three live in the service, not the generator. The generated PDF is therefore left
    /// in wwwroot/Pdf/Output for manual inspection.
    /// </summary>
    [TestClass]
    public class FusionReportGenerationTests
    {
        private const int TestClientSiteId = 61;
        private static readonly DateTime TestLogDate = new DateTime(2026, 8, 30);

        private const string DefaultConnection =
            "Server=.\\SQLSERVER2025;Database=prod-citywatch;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        private static ServiceProvider _services;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            var connectionString = Environment.GetEnvironmentVariable("CITYWATCH_TEST_CONNECTION") ?? DefaultConnection;

            // wwwroot of the web app: the generator writes the PDF under <WebRootPath>/Pdf/Output and
            // reads logos/images from <WebRootPath>/images, so it must be the real folder.
            var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "CityWatch.Web", "wwwroot"));

            // Load the web app's real appsettings.json so Settings (KpiWebUrl, image folders, ...)
            // match production behaviour, then override only the connection string.
            var webProjectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "CityWatch.Web"));

            var configuration = new ConfigurationBuilder()
                .SetBasePath(webProjectDir)
                .AddJsonFile("appsettings.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            // CityWatchDbContext takes IHubContext<MobileAppSignalRHub> for its SaveChanges
            // notifications. Registered exactly as the web app does; these tests only read.
            services.AddSignalR();
            services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(webRoot));
            services.Configure<Settings>(configuration.GetSection(Settings.Name));
            services.AddDbContext<CityWatchDbContext>(o => o.UseSqlServer(connectionString));

            // Same registrations the web app uses, limited to what the Fusion generator resolves.
            services.AddScoped<IClientDataProvider, ClientDataProvider>();
            services.AddScoped<IConfigDataProvider, ConfigDataProvider>();
            services.AddScoped<IClientSiteWandDataProvider, ClientSiteWandDataProvider>();
            services.AddScoped<IGuardLogDataProvider, GuardLogDataProvider>();
            services.AddScoped<IGuardDataProvider, GuardDataProvider>();
            services.AddScoped<IGuardLoginDetailService, GuardLoginDetailService>();
            services.AddScoped<ILogbookDataService, LogbookDataService>();
            services.AddScoped<IGuardLogReportGenerator, GuardLogReportGenerator>();

            _services = services.BuildServiceProvider();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _services?.Dispose();
        }

        /// <summary>
        /// Generates the Fusion PDF for site 61 / 30-Aug-2026 and leaves it on disk.
        /// No email, no Dropbox upload, no delete.
        /// </summary>
        [TestMethod]
        public void GenerateFusionReport_Site61_30Aug2026()
        {
            using var scope = _services.CreateScope();
            var clientDataProvider = scope.ServiceProvider.GetRequiredService<IClientDataProvider>();
            var generator = scope.ServiceProvider.GetRequiredService<IGuardLogReportGenerator>();
            var webRoot = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().WebRootPath;

            // Any logbook of the site on that date works: the generator keys the Fusion query off
            // ClientSite.Id and Date, exactly as SiteLogUploadService does when it passes the
            // DailyGuardLog/VehicleAndKeyLog book rather than the Fusion one.
            var logBook = clientDataProvider.GetClientSiteLogBooks()
                .Where(z => z.ClientSiteId == TestClientSiteId && z.Date == TestLogDate)
                .OrderBy(z => z.Id)
                .FirstOrDefault();

            Assert.IsNotNull(logBook, $"No logbook found for site {TestClientSiteId} on {TestLogDate:dd-MM-yyyy}.");

            var fileName = generator.GeneratePdfReportFusion(logBook.Id);

            Assert.IsFalse(string.IsNullOrEmpty(fileName), "Fusion PDF generation returned no file name.");

            var fullPath = Path.Combine(webRoot, "Pdf", "Output", fileName);
            Assert.IsTrue(File.Exists(fullPath), $"Fusion PDF was not written to {fullPath}.");
            Assert.IsTrue(new FileInfo(fullPath).Length > 0, "Fusion PDF is empty.");

            Console.WriteLine($"Fusion PDF generated (left on disk): {fullPath}");
            Console.WriteLine($"Size: {new FileInfo(fullPath).Length:N0} bytes");
        }

        /// <summary>
        /// Verifies the merge itself: the Fusion log set for site 61 must contain the linked duress
        /// sites' logs for the same date, must not stray onto other dates, and must not duplicate rows.
        /// </summary>
        [TestMethod]
        public void FusionLogs_IncludeLinkedDuressSites_ForSameDateOnly()
        {
            using var scope = _services.CreateScope();
            var guardLogDataProvider = scope.ServiceProvider.GetRequiredService<IGuardLogDataProvider>();

            var groupSiteIds = guardLogDataProvider.getallClientSitesLinkedDuress(TestClientSiteId)
                .Select(z => z.ClientSiteId)
                .Distinct()
                .ToArray();

            Assert.IsTrue(groupSiteIds.Length > 1,
                $"Site {TestClientSiteId} has no linked duress sites, so this test proves nothing.");

            var primaryOnly = guardLogDataProvider.GetGuardFusionLogs(
                new[] { TestClientSiteId }, TestLogDate, TestLogDate, false);

            var merged = guardLogDataProvider.GetGuardFusionLogs(
                groupSiteIds, TestLogDate, TestLogDate, false);

            // The merged set is a strict superset of the primary site's logs.
            Assert.IsTrue(merged.Count > primaryOnly.Count,
                $"Merged log count ({merged.Count}) should exceed primary-only ({primaryOnly.Count}).");

            // Same date only - no linked-site logs from other days leak in.
            Assert.IsTrue(merged.All(z => z.EventDateTime.Date == TestLogDate.Date),
                "Merged set contains logs from a date other than the logbook date.");

            // Only sites in the duress group contribute.
            Assert.IsTrue(merged.All(z => z.ClientSiteId.HasValue && groupSiteIds.Contains(z.ClientSiteId.Value)),
                "Merged set contains a site outside the linked duress group.");

            // No duplicated rows.
            Assert.AreEqual(merged.Select(z => z.Id).Distinct().Count(), merged.Count,
                "Merged set contains duplicate log rows.");

            // Every primary-site log is still present.
            Assert.IsTrue(primaryOnly.Select(z => z.Id).All(id => merged.Any(m => m.Id == id)),
                "A primary-site log went missing from the merged set.");

            Console.WriteLine($"Linked duress group sites : {string.Join(", ", groupSiteIds)}");
            Console.WriteLine($"Primary-only log rows     : {primaryOnly.Count}");
            Console.WriteLine($"Merged log rows           : {merged.Count}");
            foreach (var g in merged.GroupBy(z => z.ClientSiteId).OrderBy(x => x.Key))
                Console.WriteLine($"  site {g.Key}: {g.Count()} rows");
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
