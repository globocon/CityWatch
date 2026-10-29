using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    /* ---------------------------------------------------------------------------------------
       Progress tracking for the Bulk Certificate Release.

       Same shape as CityWatch.Kpi's FastKpiReport job: the work runs on a background task, the
       browser is handed a job id and polls a progress snapshot roughly once a second. That is
       what makes the progress real - every number the bar shows comes from a pairing the server
       has actually finished, not from a timer.

       Issuing a certificate takes seconds (PDF generation, a Dropbox upload and an email each
       time), so a release of twenty guards across three courses is a multi-minute job. Before
       this the request simply hung until every one of them was done.
       --------------------------------------------------------------------------------------- */

    public enum BulkCertificateStatus
    {
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>One guard/course pairing to issue. Built once, up front, so the total is known.</summary>
    public class BulkCertificatePairing
    {
        public int GuardId { get; init; }
        public string GuardLabel { get; init; }
        public int HrSettingsId { get; init; }
        public string CourseDescription { get; init; }

        /// <summary>
        /// The course is set up for RPL, so this pairing records the run's assessment against the
        /// guard before issuing. Decided on the server from the course's certificate document, not
        /// from anything the browser sent.
        /// </summary>
        public bool RequiresRpl { get; init; }
    }

    /// <summary>The outcome of one pairing, as shown in the modal's result list.</summary>
    public class BulkCertificateResultRow
    {
        public int GuardId { get; init; }
        public string Guard { get; init; }
        public int CourseId { get; init; }
        public string Course { get; init; }
        public string Status { get; init; }
        public bool Success { get; init; }
    }

    /// <summary>
    /// What the server intends to do, worked out before anything is issued. An invalid plan
    /// carries the message the operator sees and no pairings.
    /// </summary>
    public class BulkCertificatePlan
    {
        public bool IsValid => Message == null;
        public string Message { get; init; }
        public IReadOnlyList<BulkCertificatePairing> Pairings { get; init; } = Array.Empty<BulkCertificatePairing>();

        /// <summary>The validated assessment to apply to every RPL pairing, or null when there are none.</summary>
        public RplAssessmentDetails RplDetails { get; init; }
    }

    /// <summary>Immutable snapshot serialised straight to the polling client.</summary>
    public class BulkCertificateProgress
    {
        public string JobId { get; init; }
        public string Status { get; init; }
        public string StageLabel { get; init; }

        /// <summary>e.g. "Bruno Timpano [B.T] - Thermal Camera (FLIR Ti)".</summary>
        public string CurrentStep { get; init; }

        public int PercentComplete { get; init; }
        public int Total { get; init; }
        public int Completed { get; init; }
        public int Issued { get; init; }
        public int Failed { get; init; }

        public double ElapsedSeconds { get; init; }

        /// <summary>Null until at least one pairing has finished and an average exists.</summary>
        public double? EstimatedRemainingSeconds { get; init; }

        public bool IsTerminal { get; init; }
        public string ErrorMessage { get; init; }

        /// <summary>Every pairing finished so far, oldest first, so the list can be re-rendered as-is.</summary>
        public IReadOnlyList<BulkCertificateResultRow> Results { get; init; } = Array.Empty<BulkCertificateResultRow>();
    }

    /// <summary>Mutable server-side job record. All reads and writes go through <see cref="SyncRoot"/>.</summary>
    public class BulkCertificateJob
    {
        public string JobId { get; init; } = Guid.NewGuid().ToString("N");
        public object SyncRoot { get; } = new();

        public IReadOnlyList<BulkCertificatePairing> Pairings { get; init; } = Array.Empty<BulkCertificatePairing>();

        /// <summary>
        /// The RPL assessment captured once for the whole run, or null when no selected course is
        /// set up for RPL. One instance shared by every RPL pairing - that is what makes
        /// rplDetailsModal a once-per-release prompt rather than a once-per-guard one.
        /// </summary>
        public RplAssessmentDetails RplDetails { get; init; }

        public BulkCertificateStatus Status { get; set; } = BulkCertificateStatus.Queued;
        public string CurrentStep { get; set; } = "Queued";
        public int Issued { get; set; }
        public int Failed { get; set; }

        public DateTime CreatedUtc { get; } = DateTime.UtcNow;
        public DateTime? StartedUtc { get; set; }
        public DateTime? FinishedUtc { get; set; }

        public string ErrorMessage { get; set; }

        public List<BulkCertificateResultRow> Results { get; } = new();

        public CancellationTokenSource Cancellation { get; init; } = new();

        public int Total => Pairings.Count;
        public int Completed => Issued + Failed;

        public double ElapsedSeconds => ((FinishedUtc ?? DateTime.UtcNow) - (StartedUtc ?? CreatedUtc)).TotalSeconds;

        public bool IsTerminal => Status is BulkCertificateStatus.Completed
                                          or BulkCertificateStatus.Failed
                                          or BulkCertificateStatus.Cancelled;

        /// <summary>
        /// Held at 99 until the job is actually terminal - a bar that reads 100% while the last
        /// certificate is still uploading is worse than one that sits at 99 for a moment.
        /// </summary>
        public int ComputePercent()
        {
            if (Status == BulkCertificateStatus.Completed) return 100;
            if (Total <= 0) return 0;
            return Math.Min(99, (int)Math.Round((double)Completed / Total * 100d));
        }

        /// <summary>
        /// Projected from the average pairing so far. Certificates vary (a course with a Q&amp;A
        /// dump takes longer than one without), so this settles down as the run progresses.
        /// </summary>
        public double? EstimateRemainingSeconds()
        {
            if (IsTerminal || Completed == 0) return null;

            var average = ElapsedSeconds / Completed;
            return Math.Max(1d, Math.Round(average * (Total - Completed), 0));
        }

        public BulkCertificateProgress ToProgress()
        {
            lock (SyncRoot)
            {
                return new BulkCertificateProgress
                {
                    JobId = JobId,
                    Status = Status.ToString(),
                    StageLabel = StageLabelFor(Status),
                    CurrentStep = CurrentStep,
                    PercentComplete = ComputePercent(),
                    Total = Total,
                    Completed = Completed,
                    Issued = Issued,
                    Failed = Failed,
                    ElapsedSeconds = Math.Round(ElapsedSeconds, 1),
                    EstimatedRemainingSeconds = EstimateRemainingSeconds(),
                    IsTerminal = IsTerminal,
                    ErrorMessage = ErrorMessage,
                    Results = Results.ToList()
                };
            }
        }

        private static string StageLabelFor(BulkCertificateStatus status) => status switch
        {
            BulkCertificateStatus.Queued => "Preparing",
            BulkCertificateStatus.Running => "Issuing certificates",
            BulkCertificateStatus.Completed => "Completed",
            BulkCertificateStatus.Failed => "Failed",
            BulkCertificateStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// The release itself, with no dependency on DI or on a job store, so the synchronous page
    /// handler and the background job run byte-for-byte the same code.
    /// </summary>
    public static class BulkCertificateRelease
    {
        /// <summary>
        /// Validates the ids the browser posted against the real active-guard list and course
        /// library - nothing from the client is trusted - and expands them into one pairing per
        /// guard per course. Distinct() on both sides so a repeated id cannot issue twice.
        /// </summary>
        /// <param name="rplCourseIds">
        /// Courses that are set up for RPL, resolved on the server. Pairings on these courses are
        /// flagged so the run records <paramref name="rplDetails"/> against the guard first.
        /// </param>
        /// <param name="rplDetails">
        /// The assessment captured once by rplDetailsModal. Required when the selection contains an
        /// RPL course: without it those guards would receive a score-card certificate for a test
        /// they never sat, which is the behaviour this replaces.
        /// </param>
        public static BulkCertificatePlan BuildPlan(IEnumerable<Guard> activeGuards, IEnumerable<HrSettings> courses,
            int[] guardIds, int[] hrSettingsIds, ISet<int> rplCourseIds = null, RplAssessmentDetails rplDetails = null)
        {
            if (guardIds == null || guardIds.Length == 0)
                return new BulkCertificatePlan { Message = "Please select at least one guard." };

            if (hrSettingsIds == null || hrSettingsIds.Length == 0)
                return new BulkCertificatePlan { Message = "Please select a course certificate." };

            var courseList = courses?.ToList() ?? new List<HrSettings>();
            var selectedCourses = hrSettingsIds.Distinct()
                .Select(id => courseList.FirstOrDefault(x => x.Id == id))
                .Where(x => x != null)
                .ToList();

            if (selectedCourses.Count == 0)
                return new BulkCertificatePlan { Message = "The selected course certificates no longer exist." };

            var guardList = activeGuards?.ToList() ?? new List<Guard>();
            var selectedGuards = guardIds.Distinct()
                .Select(id => guardList.FirstOrDefault(g => g.Id == id))
                .Where(g => g != null)
                .ToList();

            if (selectedGuards.Count == 0)
                return new BulkCertificatePlan { Message = "None of the selected guards are active." };

            var rplCourses = selectedCourses.Where(c => rplCourseIds != null && rplCourseIds.Contains(c.Id)).ToList();

            if (rplCourses.Count > 0)
            {
                var rejection = ValidateRplDetails(rplDetails, rplCourses);
                if (rejection != null)
                    return new BulkCertificatePlan { Message = rejection };
            }

            var pairings = new List<BulkCertificatePairing>();
            foreach (var guard in selectedGuards)
            {
                var guardLabel = string.IsNullOrWhiteSpace(guard.Initial)
                    ? guard.Name
                    : $"{guard.Name} [{guard.Initial}]";

                foreach (var course in selectedCourses)
                {
                    pairings.Add(new BulkCertificatePairing
                    {
                        GuardId = guard.Id,
                        GuardLabel = guardLabel,
                        HrSettingsId = course.Id,
                        CourseDescription = course.Description,
                        RequiresRpl = rplCourseIds != null && rplCourseIds.Contains(course.Id)
                    });
                }
            }

            return new BulkCertificatePlan { Pairings = pairings, RplDetails = rplDetails };
        }

        /// <summary>
        /// The one place a bulk RPL assessment is checked, or null when it is usable.
        ///
        /// The single-guard modal saves whatever it is given, so these checks are new rather than
        /// copied. They exist because the blast radius differs: one bad assessment there affects
        /// one guard and an admin sees it immediately, whereas here the same values are written
        /// against every guard in the release. Only the fields the certificate actually depends on
        /// are checked - the end date dates the file, and the instructor and locations are what the
        /// assessment attests to. The compliance document stays optional, as it is in the modal.
        /// </summary>
        internal static string ValidateRplDetails(RplAssessmentDetails details, IEnumerable<HrSettings> rplCourses)
        {
            var courseNames = string.Join(", ", rplCourses.Select(c => c.Description));

            if (details == null)
                return $"RPL details are required for {courseNames}.";

            if (details.AssessmentStartDate == default || details.AssessmentEndDate == default)
                return "Please enter both the theory start date and the practical end date.";

            if (details.AssessmentEndDate.Date < details.AssessmentStartDate.Date)
                return "The practical end date cannot be earlier than the theory start date.";

            if (details.TrainingTheoryLocationId <= 0)
                return "Please select the location of the theory assessment.";

            if (details.TrainingPracticalLocationId <= 0)
                return "Please select the location of the practical assessment.";

            if (details.TrainingInstructorId <= 0)
                return "Please select the instructor signing off the assessment.";

            return null;
        }

        /// <summary>
        /// Issues every pairing on the job, updating it as each one finishes so a poller sees
        /// real progress. Each pairing is isolated: one failure must not stop the rest, matching
        /// how the RPL scheduled run treats its own loop.
        /// </summary>
        public static void Run(BulkCertificateJob job, IRPLCertificateGeneratorService certificateService, ILogger logger)
        {
            lock (job.SyncRoot)
            {
                job.Status = BulkCertificateStatus.Running;
                job.StartedUtc = DateTime.UtcNow;
                job.CurrentStep = job.Total == 0 ? "Nothing to issue" : "Starting...";
            }

            try
            {
                foreach (var pairing in job.Pairings)
                {
                    if (job.Cancellation.IsCancellationRequested)
                    {
                        lock (job.SyncRoot)
                        {
                            job.Status = BulkCertificateStatus.Cancelled;
                            job.CurrentStep = "Cancelled";
                            job.FinishedUtc = DateTime.UtcNow;
                        }
                        return;
                    }

                    // Set before the call, so the modal names the certificate currently being built.
                    lock (job.SyncRoot)
                    {
                        job.CurrentStep = $"{pairing.GuardLabel} - {pairing.CourseDescription}";
                    }

                    BulkCertificateResultRow row;
                    try
                    {
                        /* The same run's assessment for every RPL pairing, so all of them are
                           issued from the details the operator entered once. Non-RPL pairings take
                           the original two-argument call unchanged - a release with no RPL course
                           in it behaves exactly as it did before. */
                        if (pairing.RequiresRpl && job.RplDetails != null)
                            certificateService.IssueCertificateForGuard(pairing.GuardId, pairing.HrSettingsId, job.RplDetails);
                        else
                            certificateService.IssueCertificateForGuard(pairing.GuardId, pairing.HrSettingsId);

                        row = new BulkCertificateResultRow
                        {
                            GuardId = pairing.GuardId,
                            Guard = pairing.GuardLabel,
                            CourseId = pairing.HrSettingsId,
                            Course = pairing.CourseDescription,
                            Status = pairing.RequiresRpl ? "RPL certificate issued successfully" : "Certificate issued successfully",
                            Success = true
                        };
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, $"Bulk certificate release failed. GuardId: {pairing.GuardId}, HrSettingsId: {pairing.HrSettingsId}");
                        row = new BulkCertificateResultRow
                        {
                            GuardId = pairing.GuardId,
                            Guard = pairing.GuardLabel,
                            CourseId = pairing.HrSettingsId,
                            Course = pairing.CourseDescription,
                            Status = $"Failed - {ex.Message}",
                            Success = false
                        };
                    }

                    lock (job.SyncRoot)
                    {
                        job.Results.Add(row);
                        if (row.Success) job.Issued++; else job.Failed++;
                    }
                }

                lock (job.SyncRoot)
                {
                    job.Status = BulkCertificateStatus.Completed;
                    job.CurrentStep = "Finished";
                    job.FinishedUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                // Only something outside a single pairing can land here - the per-pairing catch
                // above handles the expected failures.
                logger?.LogError(ex, "Bulk certificate release aborted.");
                lock (job.SyncRoot)
                {
                    job.Status = BulkCertificateStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    job.CurrentStep = "Aborted";
                    job.FinishedUtc = DateTime.UtcNow;
                }
            }
        }
    }

    public interface IBulkCertificateJobStore
    {
        void Add(BulkCertificateJob job);
        BulkCertificateJob Get(string jobId);
    }

    /// <summary>
    /// In-memory registry of release jobs, swept once they have been finished for a while.
    ///
    /// Per-process, like the KPI report job store: behind a load balancer without sticky
    /// sessions the browser could poll a node that never saw the job. Single-node today.
    /// </summary>
    public sealed class BulkCertificateJobStore : IBulkCertificateJobStore
    {
        private const int RetentionMinutes = 30;
        private const int MaxJobs = 50;

        private readonly ConcurrentDictionary<string, BulkCertificateJob> _jobs = new();

        public void Add(BulkCertificateJob job)
        {
            Sweep();
            _jobs[job.JobId] = job;
        }

        public BulkCertificateJob Get(string jobId) =>
            string.IsNullOrWhiteSpace(jobId) ? null : _jobs.TryGetValue(jobId, out var job) ? job : null;

        private void Sweep()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-RetentionMinutes);

            var expired = _jobs.Values
                .Where(j => j.IsTerminal && (j.FinishedUtc ?? j.CreatedUtc) < cutoff)
                .ToList();

            if (_jobs.Count - expired.Count > MaxJobs)
            {
                expired.AddRange(_jobs.Values
                    .Where(j => j.IsTerminal && !expired.Contains(j))
                    .OrderBy(j => j.FinishedUtc ?? j.CreatedUtc)
                    .Take(_jobs.Count - expired.Count - MaxJobs));
            }

            foreach (var job in expired)
            {
                if (_jobs.TryRemove(job.JobId, out var removed))
                {
                    try { removed.Cancellation?.Dispose(); } catch { /* already disposed */ }
                }
            }
        }
    }

    /// <summary>A selected course that needs an RPL assessment before its certificates can be issued.</summary>
    public class BulkCertificateRplCourse
    {
        public int HrSettingsId { get; init; }
        public string Description { get; init; }

        /// <summary>The TrainingCourseCertificate the assessment is recorded against.</summary>
        public int CertificateId { get; init; }
    }

    public class BulkCertificateStartResult
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        public string JobId { get; init; }
        public int Total { get; init; }
    }

    public interface IBulkCertificateReleaseService
    {
        /// <summary>Validates and queues the release. Returns in milliseconds with a job id to poll.</summary>
        BulkCertificateStartResult Start(int[] guardIds, int[] hrSettingsIds, RplAssessmentDetails rplDetails = null);

        /// <summary>
        /// Which of the selected courses are set up for RPL, so the modal knows whether to prompt
        /// for an assessment before producing. The same question the release itself asks, answered
        /// from the same source.
        /// </summary>
        IReadOnlyList<BulkCertificateRplCourse> GetRplCourses(int[] hrSettingsIds);

        /// <summary>Progress snapshot, or null when the job id is unknown or has been swept.</summary>
        BulkCertificateProgress GetProgress(string jobId);

        /// <summary>Stops the run after the certificate currently in flight. Already-issued ones stand.</summary>
        void Cancel(string jobId);
    }

    /// <summary>
    /// Singleton: the background task outlives the request that started it, so it cannot hold
    /// the request's scoped services. It creates its own scope for the run instead.
    /// </summary>
    public class BulkCertificateReleaseService : IBulkCertificateReleaseService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBulkCertificateJobStore _jobStore;
        private readonly ILogger<BulkCertificateReleaseService> _logger;

        public BulkCertificateReleaseService(IServiceScopeFactory scopeFactory, IBulkCertificateJobStore jobStore,
            ILogger<BulkCertificateReleaseService> logger)
        {
            _scopeFactory = scopeFactory;
            _jobStore = jobStore;
            _logger = logger;
        }

        /// <inheritdoc />
        public IReadOnlyList<BulkCertificateRplCourse> GetRplCourses(int[] hrSettingsIds)
        {
            if (hrSettingsIds == null || hrSettingsIds.Length == 0)
                return Array.Empty<BulkCertificateRplCourse>();

            using var scope = _scopeFactory.CreateScope();
            var configDataProvider = scope.ServiceProvider.GetRequiredService<IConfigDataProvider>();
            var certificateService = scope.ServiceProvider.GetRequiredService<IRPLCertificateGeneratorService>();

            var courses = configDataProvider.GetHRSettings();

            return hrSettingsIds.Distinct()
                .Select(id => courses.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null)
                .Select(c => new { Course = c, Document = certificateService.GetCertificateDocumentForCourse(c.Id) })
                .Where(x => x.Document != null && x.Document.isRPLEnabled)
                .Select(x => new BulkCertificateRplCourse
                {
                    HrSettingsId = x.Course.Id,
                    Description = x.Course.Description,
                    CertificateId = x.Document.Id
                })
                .ToList();
        }

        public BulkCertificateStartResult Start(int[] guardIds, int[] hrSettingsIds, RplAssessmentDetails rplDetails = null)
        {
            BulkCertificatePlan plan;

            /* Resolved here rather than taken from the browser: the modal asks GetRplCourses which
               courses need an assessment, but the release must not trust the answer it gets back -
               a post that simply omitted the RPL ids would otherwise skip the assessment entirely. */
            var rplCourseIds = GetRplCourses(hrSettingsIds).Select(c => c.HrSettingsId).ToHashSet();

            // The plan is built inside the caller's request so a bad selection is rejected
            // straight away, before a job exists and before the modal switches to progress.
            using (var scope = _scopeFactory.CreateScope())
            {
                var guardDataProvider = scope.ServiceProvider.GetRequiredService<IGuardDataProvider>();
                var configDataProvider = scope.ServiceProvider.GetRequiredService<IConfigDataProvider>();

                plan = BulkCertificateRelease.BuildPlan(
                    guardDataProvider.GetActiveGuards(), configDataProvider.GetHRSettings(), guardIds, hrSettingsIds,
                    rplCourseIds, rplDetails);
            }

            if (!plan.IsValid)
                return new BulkCertificateStartResult { Success = false, Message = plan.Message };

            var job = new BulkCertificateJob { Pairings = plan.Pairings, RplDetails = plan.RplDetails };
            _jobStore.Add(job);

            _logger.LogInformation("Bulk certificate release: queued job {JobId} with {Total} certificate(s).",
                job.JobId, job.Total);

            /* Long-running by nature - each certificate is a PDF build, a Dropbox upload and an
               email - so it runs detached and the browser polls. Its own scope, because the
               request scope (and its DbContext) is disposed the moment Start returns. */
            _ = Task.Run(() =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var certificateService = scope.ServiceProvider.GetRequiredService<IRPLCertificateGeneratorService>();
                    BulkCertificateRelease.Run(job, certificateService, _logger);
                }
                catch (Exception ex)
                {
                    // A failure to even build the scope: record it on the job so the modal can
                    // show something rather than polling a job that never moves.
                    _logger.LogError(ex, "Bulk certificate release job {JobId} could not be started.", job.JobId);
                    lock (job.SyncRoot)
                    {
                        job.Status = BulkCertificateStatus.Failed;
                        job.ErrorMessage = ex.Message;
                        job.FinishedUtc = DateTime.UtcNow;
                    }
                }
            });

            return new BulkCertificateStartResult { Success = true, JobId = job.JobId, Total = job.Total };
        }

        public BulkCertificateProgress GetProgress(string jobId) => _jobStore.Get(jobId)?.ToProgress();

        public void Cancel(string jobId)
        {
            var job = _jobStore.Get(jobId);
            if (job == null || job.IsTerminal)
                return;

            try { job.Cancellation.Cancel(); } catch (ObjectDisposedException) { /* already swept */ }
        }
    }
}
