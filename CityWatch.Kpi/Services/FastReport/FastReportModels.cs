using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Services.FastReport
{
    /// <summary>
    /// Lifecycle states for a fast-report job.
    /// </summary>
    public enum FastReportStatus
    {
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// What the caller wants out of the run.
    ///
    /// <see cref="Download"/> mirrors <c>SendScheduleService.ProcessDownload</c>: no KPI data
    /// import, no email, the merged PDF is streamed to the browser.
    ///
    /// <see cref="Email"/> mirrors <c>SendScheduleService.ProcessSchedule</c> as invoked by
    /// "Run Now": the per-site KPI data import runs first and the merged PDF is emailed to
    /// the schedule's recipients instead of being offered as a download.
    /// </summary>
    public enum FastReportMode
    {
        Download,
        Email
    }

    /// <summary>
    /// The coarse pipeline stages a job moves through. Percentages are allocated
    /// across these so the progress bar advances smoothly and monotonically.
    ///
    /// Declaration order matters: <see cref="FastReportJob.EstimateRemainingSeconds"/>
    /// compares stages with &gt;=.
    /// </summary>
    public enum FastReportStage
    {
        Preparing,
        LoadingSchedule,
        GeneratingSiteReports,
        BuildingSummary,
        MergingDocuments,
        SendingEmail,
        Finalising,
        Completed
    }

    /// <summary>
    /// Immutable snapshot of a job's progress, serialised straight to the polling client.
    /// </summary>
    public class FastReportProgress
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Stage { get; set; }

        /// <summary>Human-readable headline, e.g. "Generating site reports".</summary>
        public string StageLabel { get; set; }

        /// <summary>Fine-grained detail, e.g. "Site 3 of 12 - Loading guard compliance".</summary>
        public string CurrentStep { get; set; }

        public int PercentComplete { get; set; }

        public int SitesTotal { get; set; }
        public int SitesCompleted { get; set; }

        public double ElapsedSeconds { get; set; }

        /// <summary>Null until enough work has completed to project a meaningful estimate.</summary>
        public double? EstimatedRemainingSeconds { get; set; }

        public bool IsTerminal { get; set; }
        public bool CanDownload { get; set; }

        /// <summary>"Download" or "Email" - the client renders a different outcome for each.</summary>
        public string Mode { get; set; }

        /// <summary>Email runs only: the message has been handed to the SMTP server.</summary>
        public bool EmailSent { get; set; }

        public string ErrorMessage { get; set; }

        public FastReportMetricsSnapshot Metrics { get; set; }
    }

    /// <summary>
    /// Performance counters captured during a run, used for the old-vs-new benchmark.
    /// </summary>
    public class FastReportMetricsSnapshot
    {
        public long TotalMilliseconds { get; set; }

        /// <summary>Wall-clock spent inside data-provider calls that actually hit the database.</summary>
        public long DataAccessMilliseconds { get; set; }

        /// <summary>Provider calls that were served from the per-job memo cache.</summary>
        public int CacheHits { get; set; }

        /// <summary>Provider calls that fell through to the real provider (i.e. real queries).</summary>
        public int CacheMisses { get; set; }

        /// <summary>Provider calls deliberately not cached (write paths, unkeyable arguments).</summary>
        public int PassThroughCalls { get; set; }

        public long PeakManagedMemoryBytes { get; set; }
        public long OutputFileBytes { get; set; }
        public int OutputPageCount { get; set; }

        public double CacheHitRatio =>
            (CacheHits + CacheMisses) == 0 ? 0d : Math.Round((double)CacheHits / (CacheHits + CacheMisses) * 100d, 1);

        /// <summary>Per-method breakdown, most expensive first. Diagnostics only.</summary>
        public List<FastReportMethodStat> TopMethods { get; set; } = new();
    }

    public class FastReportMethodStat
    {
        public string Method { get; set; }
        public int Calls { get; set; }
        public int Hits { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// The request that produced a job. Kept on the job so a failed run can be retried verbatim.
    /// </summary>
    public class FastReportRequest
    {
        public int ScheduleId { get; set; }
        public int ReportYear { get; set; }
        public int ReportMonth { get; set; }
        public bool IgnoreRecipients { get; set; }

        /// <summary>Defaults to Download, so an older client that omits it behaves as before.</summary>
        public FastReportMode Mode { get; set; } = FastReportMode.Download;

        public DateTime ReportStartDate => new DateTime(ReportYear, ReportMonth, 1);

        public override string ToString() =>
            $"schedule={ScheduleId} period={ReportYear}-{ReportMonth:00} mode={Mode} ignoreRecipients={IgnoreRecipients}";
    }

    /// <summary>
    /// Mutable server-side job record. All mutation goes through the lock in
    /// <see cref="FastReportJobStore"/> or the job's own <see cref="SyncRoot"/>.
    /// </summary>
    public class FastReportJob
    {
        public string JobId { get; init; } = Guid.NewGuid().ToString("N");
        public FastReportRequest Request { get; init; }

        public object SyncRoot { get; } = new();

        public FastReportStatus Status { get; set; } = FastReportStatus.Queued;
        public FastReportStage Stage { get; set; } = FastReportStage.Preparing;
        public string CurrentStep { get; set; } = "Queued";

        public int SitesTotal { get; set; }
        public int SitesCompleted { get; set; }

        /// <summary>
        /// How far through the *current* site we are, 0..1. Without this the bar would sit
        /// still for the whole of each site, which reads as a hang. Estimated from how many
        /// data-access calls this site has made against the average for a completed site.
        /// </summary>
        public double CurrentSiteFraction { get; set; }

        /// <summary>Provider-call count observed at the moment the current site started.</summary>
        public int CurrentSiteStartCallCount { get; set; }

        /// <summary>Rolling average of provider calls per site; seeded with a rough guess.</summary>
        public double AverageCallsPerSite { get; set; } = 400d;

        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
        public DateTime? StartedUtc { get; set; }
        public DateTime? FinishedUtc { get; set; }

        /// <summary>Wall-clock of each completed site, used to project the ETA.</summary>
        public List<double> SiteDurationsSeconds { get; } = new();

        /// <summary>
        /// Absolute path of the finished PDF. Streamed then deleted on download; cleared by an
        /// email run once the message has gone out.
        /// </summary>
        public string OutputFilePath { get; set; }
        public string DownloadFileName { get; set; }

        /// <summary>True once an email run has actually handed the message to the SMTP server.</summary>
        public bool EmailSent { get; set; }

        public string ErrorMessage { get; set; }
        public string ErrorDetail { get; set; }

        /// <summary>Ordered activity log for this job. Surfaced on failure and in the benchmark.</summary>
        public List<string> Log { get; } = new();

        public FastReportMetricsSnapshot Metrics { get; set; } = new();

        public System.Threading.CancellationTokenSource Cancellation { get; init; } = new();

        public double ElapsedSeconds =>
            ((FinishedUtc ?? DateTime.UtcNow) - (StartedUtc ?? CreatedUtc)).TotalSeconds;

        public bool IsTerminal =>
            Status is FastReportStatus.Completed or FastReportStatus.Failed or FastReportStatus.Cancelled;

        public void Append(string message)
        {
            lock (SyncRoot)
            {
                Log.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
            }
        }

        /// <summary>
        /// Percentage is derived rather than stored, so it can never move backwards
        /// and never disagrees with the stage.
        /// </summary>
        public int ComputePercent()
        {
            const int scheduleLoaded = 5;
            const int sitesBudget = 75;   // 5 -> 80
            const int summaryDone = 85;
            const int mergeDone = 92;
            const int emailSent = 96;
            const int finaliseDone = 98;

            return Stage switch
            {
                FastReportStage.Preparing => 2,
                FastReportStage.LoadingSchedule => scheduleLoaded,
                FastReportStage.GeneratingSiteReports =>
                    SitesTotal <= 0
                        ? scheduleLoaded
                        : scheduleLoaded + (int)Math.Round(
                            (SitesCompleted + Math.Clamp(CurrentSiteFraction, 0d, 0.97d)) / SitesTotal * sitesBudget),
                FastReportStage.BuildingSummary => summaryDone,
                FastReportStage.MergingDocuments => mergeDone,
                FastReportStage.SendingEmail => emailSent,
                FastReportStage.Finalising => finaliseDone,
                FastReportStage.Completed => 100,
                _ => 0
            };
        }

        /// <summary>
        /// Projects remaining time from the rolling average of completed sites, plus a
        /// reserve for the summary/merge tail. Returns null until at least one site is done.
        /// </summary>
        public double? EstimateRemainingSeconds()
        {
            if (IsTerminal)
                return null;

            if (Stage >= FastReportStage.BuildingSummary)
            {
                // Tail stages: estimate from what the tail historically costs relative to a site.
                var avgTail = SiteDurationsSeconds.Count > 0 ? SiteDurationsSeconds.Average() * 0.5 : 5d;
                return Math.Max(1d, Math.Round(avgTail, 0));
            }

            if (SiteDurationsSeconds.Count == 0 || SitesTotal <= 0)
                return null;

            var average = SiteDurationsSeconds.Average();
            var sitesRemaining = Math.Max(0d, SitesTotal - SitesCompleted - Math.Clamp(CurrentSiteFraction, 0d, 1d));

            // Reserve ~50% of one site's cost for summary + merge + finalise.
            var estimate = (average * sitesRemaining) + (average * 0.5);
            return Math.Max(1d, Math.Round(estimate, 0));
        }

        public FastReportProgress ToProgress()
        {
            lock (SyncRoot)
            {
                return new FastReportProgress
                {
                    JobId = JobId,
                    Status = Status.ToString(),
                    Stage = Stage.ToString(),
                    StageLabel = StageLabelFor(Stage, Request?.Mode ?? FastReportMode.Download),
                    CurrentStep = CurrentStep,
                    PercentComplete = Status == FastReportStatus.Completed ? 100 : ComputePercent(),
                    SitesTotal = SitesTotal,
                    SitesCompleted = SitesCompleted,
                    ElapsedSeconds = Math.Round(ElapsedSeconds, 1),
                    EstimatedRemainingSeconds = EstimateRemainingSeconds(),
                    IsTerminal = IsTerminal,
                    // Email runs delete the merged PDF once it has been sent, so this is
                    // false for them and the client never offers a download.
                    CanDownload = Status == FastReportStatus.Completed && !string.IsNullOrEmpty(OutputFilePath),
                    Mode = (Request?.Mode ?? FastReportMode.Download).ToString(),
                    EmailSent = EmailSent,
                    ErrorMessage = ErrorMessage,
                    Metrics = Metrics
                };
            }
        }

        public static string StageLabelFor(FastReportStage stage, FastReportMode mode = FastReportMode.Download) => stage switch
        {
            FastReportStage.Preparing => "Preparing report",
            FastReportStage.LoadingSchedule => "Loading schedule",
            FastReportStage.GeneratingSiteReports => "Generating site reports",
            FastReportStage.BuildingSummary => "Building summary cover page",
            FastReportStage.MergingDocuments => "Merging documents",
            FastReportStage.SendingEmail => "Sending email",
            FastReportStage.Finalising => mode == FastReportMode.Email ? "Finishing up" : "Preparing download",
            FastReportStage.Completed => "Completed",
            _ => stage.ToString()
        };
    }
}
