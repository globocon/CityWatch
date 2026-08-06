using CityWatch.Data.Providers;
using CityWatch.Kpi.Services;
using CityWatch.Kpi.Services.FastReport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Kpi.API
{
    /// <summary>
    /// Endpoints behind the "Download Now" button on the Run Schedule popup.
    ///
    /// Entirely additive: the legacy Razor Page handler
    /// <c>/Admin/Settings?handler=DownloadPdf</c> is untouched and remains the production
    /// path. Nothing here is referenced by the legacy flow.
    ///
    /// The generation itself runs on a background task, so every request below returns in
    /// milliseconds and the browser stays responsive.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FastKpiReportController : ControllerBase
    {
        private readonly IFastReportService _fastReportService;
        private readonly IFastReportJobStore _jobStore;
        private readonly ILogger<FastKpiReportController> _logger;

        public FastKpiReportController(
            IFastReportService fastReportService,
            IFastReportJobStore jobStore,
            ILogger<FastKpiReportController> logger)
        {
            _fastReportService = fastReportService;
            _jobStore = jobStore;
            _logger = logger;
        }

        /// <summary>Queues a report. Returns immediately with a job id to poll.</summary>
        [HttpPost("start")]
        public IActionResult Start([FromForm] FastReportRequest request)
        {
            if (request == null || request.ScheduleId <= 0)
                return BadRequest(new { success = false, message = "A schedule must be selected." });

            if (request.ReportMonth is < 1 or > 12)
                return BadRequest(new { success = false, message = "Report month must be between 1 and 12." });

            if (request.ReportYear is < 2000 or > 2999)
                return BadRequest(new { success = false, message = "Report year is out of range." });

            var job = _fastReportService.Start(request);
            _logger.LogInformation("FastReport: queued job {JobId} for {Request}.", job.JobId, request);

            return Ok(new { success = true, jobId = job.JobId });
        }

        /// <summary>Progress snapshot. Polled by the client roughly once a second.</summary>
        [HttpGet("progress/{jobId}")]
        public IActionResult Progress(string jobId)
        {
            var job = _jobStore.Get(jobId);
            if (job == null)
                return NotFound(new { success = false, message = "This report job has expired. Please start it again." });

            return Ok(job.ToProgress());
        }

        /// <summary>
        /// Streams the finished PDF and deletes it once the response has been written.
        /// Streaming rather than buffering keeps memory flat regardless of report size.
        /// </summary>
        [HttpGet("download/{jobId}")]
        public IActionResult Download(string jobId)
        {
            var job = _jobStore.Get(jobId);
            if (job == null)
                return NotFound(new { success = false, message = "This report job has expired. Please start it again." });

            if (job.Status != FastReportStatus.Completed || string.IsNullOrEmpty(job.OutputFilePath))
                return BadRequest(new { success = false, message = $"The report is not ready (status: {job.Status})." });

            if (!System.IO.File.Exists(job.OutputFilePath))
                return NotFound(new { success = false, message = "The generated file is no longer available. Please run the report again." });

            var stream = new FileStream(
                job.OutputFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                // The file is a one-shot artefact: remove it as soon as the response closes.
                options: FileOptions.SequentialScan | FileOptions.DeleteOnClose);

            var downloadName = string.IsNullOrEmpty(job.DownloadFileName) ? "Monthly Report.pdf" : job.DownloadFileName;

            job.Append($"Downloaded as '{downloadName}'.");
            return File(stream, "application/pdf", downloadName);
        }

        [HttpPost("cancel/{jobId}")]
        public IActionResult Cancel(string jobId)
        {
            _fastReportService.Cancel(jobId);
            return Ok(new { success = true });
        }

        /// <summary>Full activity log plus exception detail. Surfaced by the UI on failure.</summary>
        [HttpGet("log/{jobId}")]
        public IActionResult Log(string jobId)
        {
            var job = _jobStore.Get(jobId);
            if (job == null)
                return NotFound(new { success = false, message = "This report job has expired." });

            lock (job.SyncRoot)
            {
                return Ok(new
                {
                    success = true,
                    jobId = job.JobId,
                    status = job.Status.ToString(),
                    request = job.Request?.ToString(),
                    elapsedSeconds = Math.Round(job.ElapsedSeconds, 1),
                    error = job.ErrorMessage,
                    detail = job.ErrorDetail,
                    metrics = job.Metrics,
                    log = job.Log.ToList()
                });
            }
        }

        /// <summary>
        /// Runs the legacy generator and the fast generator over the same schedule and month,
        /// then reports timings alongside a structural comparison of the two PDFs.
        ///
        /// This is the acceptance test for "the output is identical". It is synchronous and
        /// deliberately slow - it runs the report twice - so it is meant for verification
        /// runs, not for everyday use.
        /// </summary>
        [HttpPost("benchmark")]
        public async Task<IActionResult> Benchmark(
            [FromForm] FastReportRequest request,
            [FromServices] ISendScheduleService legacySendScheduleService,
            [FromServices] IKpiSchedulesDataProvider schedulesDataProvider,
            CancellationToken cancellationToken)
        {
            if (request == null || request.ScheduleId <= 0)
                return BadRequest(new { success = false, message = "A schedule must be selected." });

            var reportStartDate = request.ReportStartDate;

            // ---- Legacy run (untouched production code path) ----
            var legacySchedule = schedulesDataProvider.GetSendScheduleById(request.ScheduleId);
            if (legacySchedule == null)
                return NotFound(new { success = false, message = $"Schedule {request.ScheduleId} was not found." });

            var legacyMemoryBefore = GC.GetTotalMemory(true);
            var legacyStopwatch = Stopwatch.StartNew();
            byte[] legacyBytes;
            try
            {
                legacyBytes = legacySendScheduleService.ProcessDownload(
                    legacySchedule, reportStartDate, request.IgnoreRecipients, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FastReport benchmark: legacy run failed.");
                return Ok(new { success = false, message = "The legacy run failed: " + ex.Message });
            }
            legacyStopwatch.Stop();
            var legacyMemoryDelta = Math.Max(0, GC.GetTotalMemory(false) - legacyMemoryBefore);

            // ---- Fast run ----
            var fastJob = await _fastReportService.RunSynchronouslyAsync(request, cancellationToken);
            if (fastJob.Status != FastReportStatus.Completed)
            {
                return Ok(new
                {
                    success = false,
                    message = "The fast run did not complete: " + (fastJob.ErrorMessage ?? fastJob.Status.ToString()),
                    log = fastJob.Log.ToList()
                });
            }

            var fastBytes = await System.IO.File.ReadAllBytesAsync(fastJob.OutputFilePath, cancellationToken);

            // ---- Compare ----
            var comparison = FastReportComparer.Compare(legacyBytes, fastBytes);

            var legacyMs = legacyStopwatch.ElapsedMilliseconds;
            var fastMs = fastJob.Metrics.TotalMilliseconds;
            var speedup = fastMs > 0 ? Math.Round((double)legacyMs / fastMs, 2) : 0d;
            var saved = legacyMs - fastMs;

            _logger.LogInformation(
                "FastReport benchmark ({Request}): legacy {LegacyMs}ms, fast {FastMs}ms, identical={Identical}.",
                request, legacyMs, fastMs, comparison.IsIdentical);

            return Ok(new
            {
                success = true,
                request = request.ToString(),
                identical = comparison.IsIdentical,
                comparison,
                performance = new
                {
                    legacyMilliseconds = legacyMs,
                    fastMilliseconds = fastMs,
                    savedMilliseconds = saved,
                    speedupFactor = speedup,
                    percentFaster = legacyMs > 0 ? Math.Round((double)saved / legacyMs * 100d, 1) : 0d,
                    legacyManagedMemoryDeltaBytes = legacyMemoryDelta,
                    fastPeakManagedMemoryBytes = fastJob.Metrics.PeakManagedMemoryBytes,
                    fastDataAccessMilliseconds = fastJob.Metrics.DataAccessMilliseconds,
                    fastQueryCalls = fastJob.Metrics.CacheMisses,
                    fastCacheHits = fastJob.Metrics.CacheHits,
                    fastCacheHitRatio = fastJob.Metrics.CacheHitRatio,
                    fastTopMethods = fastJob.Metrics.TopMethods
                },
                log = fastJob.Log.ToList()
            });
        }
    }
}
