using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Kpi.Services.FastReport
{
    public interface IFastReportService
    {
        /// <summary>Queues a report and returns immediately. Never blocks the caller.</summary>
        FastReportJob Start(FastReportRequest request);

        /// <summary>Runs a report to completion on the calling thread. Used by the benchmark.</summary>
        Task<FastReportJob> RunSynchronouslyAsync(FastReportRequest request, CancellationToken cancellationToken);

        void Cancel(string jobId);
    }

    /// <summary>
    /// Orchestrates the fast report pipeline.
    ///
    /// This class owns *only* the orchestration: which sites to render, in what order, how
    /// to merge them, and how to report progress. Every byte of the actual PDF is produced
    /// by the existing, untouched <see cref="IReportGenerator"/>,
    /// <see cref="MonthlySummaryReportGenerator"/> / <see cref="WeeklySummaryReportGenerator"/>
    /// and <see cref="PdfHelper.CombinePdfReports"/>.
    ///
    /// That split is the whole safety argument: identical inputs to identical rendering code
    /// produce an identical document. The speed comes from the memoising decorators injected
    /// by <see cref="IFastReportScopeFactory"/>, which remove duplicate database round-trips
    /// without changing a single returned value.
    ///
    /// The orchestration itself mirrors <c>SendScheduleService.ProcessDownload</c> step for
    /// step, including its side effects, so the two paths remain comparable.
    /// </summary>
    public sealed class FastReportService : IFastReportService
    {
        private readonly IFastReportScopeFactory _scopeFactory;
        private readonly IFastReportJobStore _jobStore;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FastReportService> _logger;

        public FastReportService(
            IFastReportScopeFactory scopeFactory,
            IFastReportJobStore jobStore,
            IWebHostEnvironment webHostEnvironment,
            ILogger<FastReportService> logger)
        {
            _scopeFactory = scopeFactory;
            _jobStore = jobStore;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public FastReportJob Start(FastReportRequest request)
        {
            var job = _jobStore.Create(request);

            // Fire and forget: the HTTP request returns immediately and the browser polls.
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteAsync(job, job.Cancellation.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // ExecuteAsync handles its own failures; this is the last line of defence
                    // so a background exception can never take the process down.
                    _logger.LogError(ex, "FastReport: unhandled failure in job {JobId}.", job.JobId);
                    MarkFailed(job, "The report failed unexpectedly.", ex);
                }
            });

            return job;
        }

        public async Task<FastReportJob> RunSynchronouslyAsync(FastReportRequest request, CancellationToken cancellationToken)
        {
            var job = _jobStore.Create(request);
            await ExecuteAsync(job, cancellationToken).ConfigureAwait(false);
            return job;
        }

        public void Cancel(string jobId)
        {
            var job = _jobStore.Get(jobId);
            if (job == null || job.IsTerminal)
                return;

            job.Append("Cancellation requested.");
            try { job.Cancellation.Cancel(); } catch (ObjectDisposedException) { /* already finished */ }
        }

        // ------------------------------------------------------------------
        // Pipeline
        // ------------------------------------------------------------------

        private async Task ExecuteAsync(FastReportJob job, CancellationToken cancellationToken)
        {
            var totalStopwatch = Stopwatch.StartNew();
            long peakManagedBytes = 0;

            // Temp files produced along the way; removed in the finally block whatever happens.
            var siteReportFiles = new List<string>();
            string summaryFile = null;

            job.StartedUtc = DateTime.UtcNow;
            SetStage(job, FastReportStage.Preparing, "Preparing report");
            job.Status = FastReportStatus.Running;

            using var scope = _scopeFactory.CreateReportScope();
            var services = scope.ServiceProvider;

            var cache = services.GetRequiredService<ReportScopeCache>();

            try
            {
                var schedulesProvider = services.GetRequiredService<IKpiSchedulesDataProvider>();
                var importJobProvider = services.GetRequiredService<IImportJobDataProvider>();
                var reportGenerator = services.GetRequiredService<IReportGenerator>();
                var viewDataService = services.GetRequiredService<IViewDataService>();
                var patrolDataReportService = services.GetRequiredService<IPatrolDataReportService>();

                // -------- Stage: load schedule --------
                SetStage(job, FastReportStage.LoadingSchedule, "Loading schedule");
                cancellationToken.ThrowIfCancellationRequested();

                var schedule = schedulesProvider.GetSendScheduleById(job.Request.ScheduleId)
                    ?? throw new InvalidOperationException($"Schedule {job.Request.ScheduleId} was not found.");

                var reportStartDate = job.Request.ReportStartDate;
                var reportEndDate = reportStartDate.AddMonths(1).AddDays(-1);

                // Captured BEFORE generation, because the legacy path builds the download
                // name from the schedule's original ProjectName and only afterwards
                // overwrites it via GetScheduleIdentifier. Same order here, same file name.
                job.DownloadFileName = BuildDownloadFileName(schedule, job.Request);

                var siteIds = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSiteId).ToList();
                job.SitesTotal = siteIds.Count;
                job.Append($"Schedule loaded: '{schedule.ProjectName}' with {siteIds.Count} site(s).");

                if (siteIds.Count == 0)
                    throw new InvalidOperationException("This schedule has no client sites assigned.");

                var outputDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output");
                Directory.CreateDirectory(outputDir);

                // -------- Stage: per-site reports --------
                SetStage(job, FastReportStage.GeneratingSiteReports, "Starting site reports");

                var isDownselect = schedule.IsCriticalDocumentDownselect;
                var criticalDocumentId = Convert.ToInt32(schedule.CriticalGroupNameID);

                for (var index = 0; index < siteIds.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var siteId = siteIds[index];
                    var position = index + 1;
                    var siteStopwatch = Stopwatch.StartNew();

                    lock (job.SyncRoot)
                    {
                        job.CurrentSiteStartCallCount = cache.TotalCalls;
                        job.CurrentSiteFraction = 0d;
                    }

                    // Narrate whatever the data layer is doing right now, and advance the bar
                    // within the site so it never looks stalled.
                    cache.OnDataAccess = label =>
                    {
                        lock (job.SyncRoot)
                        {
                            var callsThisSite = cache.TotalCalls - job.CurrentSiteStartCallCount;
                            job.CurrentSiteFraction = job.AverageCallsPerSite > 0
                                ? callsThisSite / job.AverageCallsPerSite
                                : 0d;

                            if (!string.IsNullOrEmpty(label))
                                job.CurrentStep = $"Site {position} of {siteIds.Count} - {label}";
                        }
                    };
                    SetStep(job, $"Site {position} of {siteIds.Count} - Starting");

                    // Same audit row the legacy path writes before rendering a site.
                    var importJob = new KpiDataImportJob
                    {
                        ClientSiteId = siteId,
                        ReportDate = reportStartDate,
                        CreatedDate = DateTime.Now
                    };
                    importJobProvider.SaveKpiDataImportJob(importJob);

                    SetStep(job, $"Site {position} of {siteIds.Count} - Rendering PDF");

                    var fileName = reportGenerator.GeneratePdfReport(
                        siteId,
                        reportStartDate,
                        reportEndDate,
                        schedule.IsHrTimerPaused,
                        isDownselect,
                        criticalDocumentId);

                    siteStopwatch.Stop();

                    if (string.IsNullOrEmpty(fileName))
                    {
                        // Matches the legacy behaviour: log and carry on with the other sites.
                        job.Append($"Site {siteId}: no PDF produced (KPI settings missing?) - skipped.");
                        _logger.LogWarning("FastReport {JobId}: site {SiteId} produced no PDF.", job.JobId, siteId);
                    }
                    else
                    {
                        siteReportFiles.Add(Path.Combine(outputDir, fileName));
                        job.Append($"Site {siteId} rendered in {siteStopwatch.Elapsed.TotalSeconds:0.0}s.");
                    }

                    lock (job.SyncRoot)
                    {
                        job.SitesCompleted = position;
                        job.SiteDurationsSeconds.Add(siteStopwatch.Elapsed.TotalSeconds);
                        job.CurrentSiteFraction = 0d;

                        // Replace the seeded guess with what this workload actually costs.
                        var callsThisSite = cache.TotalCalls - job.CurrentSiteStartCallCount;
                        if (callsThisSite > 0)
                        {
                            job.AverageCallsPerSite = position == 1
                                ? callsThisSite
                                : ((job.AverageCallsPerSite * (position - 1)) + callsThisSite) / position;
                        }
                    }

                    peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(false));
                }

                cache.OnDataAccess = null;

                if (siteReportFiles.Count == 0)
                    throw new InvalidOperationException("No site reports could be generated for this schedule.");

                cancellationToken.ThrowIfCancellationRequested();

                // -------- Stage: summary cover page --------
                SetStage(job, FastReportStage.BuildingSummary, "Building summary cover page");

                // Legacy order: ProjectName is normalised first, then the cover sheet is built.
                schedule.ProjectName = GetScheduleIdentifier(schedule);
                summaryFile = CreateSummaryReport(schedule, reportStartDate, reportEndDate, viewDataService, patrolDataReportService);
                job.Append("Summary cover page built.");

                // -------- Stage: merge --------
                cancellationToken.ThrowIfCancellationRequested();
                SetStage(job, FastReportStage.MergingDocuments, $"Merging {siteReportFiles.Count + 1} document(s)");

                var jobDir = Path.Combine(outputDir, "fast", job.JobId);
                Directory.CreateDirectory(jobDir);

                // Written into a per-job folder so a concurrent legacy run - which uses a
                // fixed, schedule-derived file name in Pdf/Output - can never collide with it.
                var combinedPath = Path.Combine(
                    jobDir,
                    $"{FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Daily KPI Reports - {reportStartDate:MMM} {reportStartDate.Year}.pdf");

                PdfHelper.CombinePdfReports(combinedPath, siteReportFiles, summaryFile);
                job.Append("Documents merged.");

                // -------- Stage: finalise --------
                SetStage(job, FastReportStage.Finalising, "Preparing download");

                var fileInfo = new FileInfo(combinedPath);
                job.OutputFilePath = combinedPath;

                totalStopwatch.Stop();
                peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(false));

                job.Metrics = new FastReportMetricsSnapshot
                {
                    TotalMilliseconds = totalStopwatch.ElapsedMilliseconds,
                    DataAccessMilliseconds = cache.DataAccessMilliseconds,
                    CacheHits = cache.Hits,
                    CacheMisses = cache.Misses,
                    PassThroughCalls = cache.PassThrough,
                    PeakManagedMemoryBytes = peakManagedBytes,
                    OutputFileBytes = fileInfo.Exists ? fileInfo.Length : 0,
                    OutputPageCount = CountPages(combinedPath),
                    TopMethods = cache.TopMethods()
                };

                lock (job.SyncRoot)
                {
                    job.Stage = FastReportStage.Completed;
                    job.Status = FastReportStatus.Completed;
                    job.CurrentStep = "Completed";
                    job.FinishedUtc = DateTime.UtcNow;
                }

                job.Append(
                    $"Completed in {totalStopwatch.Elapsed.TotalSeconds:0.0}s. " +
                    $"{job.Metrics.OutputPageCount} pages, {job.Metrics.OutputFileBytes:N0} bytes. " +
                    $"Data access {job.Metrics.DataAccessMilliseconds}ms across {job.Metrics.CacheMisses} query call(s), " +
                    $"{job.Metrics.CacheHits} served from cache ({job.Metrics.CacheHitRatio}% hit rate).");

                _logger.LogInformation(
                    "FastReport {JobId}: completed in {Seconds:0.0}s ({Pages} pages, {Hits} cache hits / {Misses} misses).",
                    job.JobId, totalStopwatch.Elapsed.TotalSeconds, job.Metrics.OutputPageCount,
                    job.Metrics.CacheHits, job.Metrics.CacheMisses);

                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                lock (job.SyncRoot)
                {
                    job.Status = FastReportStatus.Cancelled;
                    job.CurrentStep = "Cancelled";
                    job.FinishedUtc = DateTime.UtcNow;
                }
                job.Append("Cancelled by user.");
                _logger.LogInformation("FastReport {JobId}: cancelled.", job.JobId);
            }
            catch (Exception ex)
            {
                MarkFailed(job, DescribeFailure(ex), ex);
                _logger.LogError(ex, "FastReport {JobId}: failed.", job.JobId);
            }
            finally
            {
                cache.OnDataAccess = null;
                CleanupTempFiles(job, siteReportFiles, summaryFile);
            }
        }

        // ------------------------------------------------------------------
        // Faithful copies of the legacy private helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Mirrors <c>SendScheduleService.GetSchduleIdentifier</c>. Kept byte-identical so the
        /// merged report's title and file name match the legacy output exactly.
        /// </summary>
        private static string GetScheduleIdentifier(KpiSendSchedule schedule)
        {
            if (!string.IsNullOrEmpty(schedule.ProjectName))
                return schedule.ProjectName;

            if (schedule.KpiSendScheduleClientSites.Count == 1)
                return schedule.KpiSendScheduleClientSites.Single().ClientSite.Name;

            return string.Join(", ", schedule.KpiSendScheduleClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct());
        }

        /// <summary>
        /// Mirrors <c>SendScheduleService.CreateSummaryReport</c>, including the rule that a
        /// report for a past month always uses the monthly cover sheet.
        /// </summary>
        private string CreateSummaryReport(
            KpiSendSchedule schedule,
            DateTime reportStartDate,
            DateTime reportEndDate,
            IViewDataService viewDataService,
            IPatrolDataReportService patrolDataReportService)
        {
            var coverSheetType = schedule.CoverSheetType;
            if (reportStartDate.Month != DateTime.Today.Month)
                coverSheetType = CoverSheetType.Monthly;

            ISummaryReportGenerator summaryReportGenerator = coverSheetType == CoverSheetType.Weekly
                ? new WeeklySummaryReportGenerator(_webHostEnvironment, viewDataService, patrolDataReportService)
                : new MonthlySummaryReportGenerator(_webHostEnvironment, viewDataService, patrolDataReportService);

            var summaryFromDate = coverSheetType == CoverSheetType.Weekly ? DateTime.Today.AddDays(-6) : reportStartDate;
            var summaryToDate = coverSheetType == CoverSheetType.Weekly ? DateTime.Today : reportEndDate;

            var summaryFileName = summaryReportGenerator.GeneratePdfReport(schedule, summaryFromDate, summaryToDate);
            return Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", summaryFileName);
        }

        /// <summary>
        /// Mirrors the name built in <c>SettingsModel.OnGetDownloadPdf</c>, which uses the
        /// schedule's ProjectName as loaded - before generation overwrites it.
        /// </summary>
        private static string BuildDownloadFileName(KpiSendSchedule schedule, FastReportRequest request)
        {
            var date = request.ReportStartDate;
            var stem = $"{request.ReportYear}{request.ReportMonth.ToString("00")} - " +
                       $"{FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - " +
                       $"Monthly Report - {date.ToString("MMM").ToUpper()} {request.ReportYear}";

            // The legacy handler returns File(..., filename + ".pdf").
            return stem + ".pdf";
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static int CountPages(string pdfPath)
        {
            try
            {
                using var reader = new iText.Kernel.Pdf.PdfReader(pdfPath);
                using var doc = new iText.Kernel.Pdf.PdfDocument(reader);
                return doc.GetNumberOfPages();
            }
            catch
            {
                return 0;
            }
        }

        private void CleanupTempFiles(FastReportJob job, IEnumerable<string> siteReportFiles, string summaryFile)
        {
            foreach (var path in siteReportFiles.Concat(new[] { summaryFile }))
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception ex)
                {
                    // Never let cleanup failure mask the real outcome.
                    job.Append($"Could not delete temp file '{Path.GetFileName(path)}': {ex.Message}");
                    _logger.LogWarning("FastReport {JobId}: temp cleanup failed for {Path}. {Message}", job.JobId, path, ex.Message);
                }
            }
        }

        private static string DescribeFailure(Exception ex) => ex switch
        {
            InvalidOperationException => ex.Message,
            IOException => "The report file could not be written. The server may be out of disk space.",
            UnauthorizedAccessException => "The server does not have permission to write the report file.",
            _ => "The report failed to generate. See the details below."
        };

        private void MarkFailed(FastReportJob job, string message, Exception ex)
        {
            lock (job.SyncRoot)
            {
                job.Status = FastReportStatus.Failed;
                job.CurrentStep = "Failed";
                job.FinishedUtc = DateTime.UtcNow;
                job.ErrorMessage = message;
                job.ErrorDetail = ex?.ToString();
            }
            job.Append($"FAILED: {ex?.Message}");
        }

        private static void SetStage(FastReportJob job, FastReportStage stage, string step)
        {
            lock (job.SyncRoot)
            {
                job.Stage = stage;
                job.CurrentStep = step;
            }
        }

        private static void SetStep(FastReportJob job, string step)
        {
            lock (job.SyncRoot)
            {
                job.CurrentStep = step;
            }
        }
    }
}
