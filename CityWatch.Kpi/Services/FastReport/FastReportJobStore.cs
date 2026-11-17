using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CityWatch.Kpi.Services.FastReport
{
    public interface IFastReportJobStore
    {
        FastReportJob Create(FastReportRequest request);
        FastReportJob Get(string jobId);
        IReadOnlyCollection<FastReportJob> All();
        void Remove(string jobId);
    }

    /// <summary>
    /// In-memory registry of fast-report jobs.
    ///
    /// Deliberately not persisted: a job is only meaningful to the browser tab that started
    /// it, and a generated PDF is cheap to recreate. Finished jobs (and their temp files)
    /// are swept after <see cref="RetentionMinutes"/> so a long-running site cannot
    /// accumulate them.
    ///
    /// Note this is per-process. Behind a load balancer without sticky sessions, progress
    /// polling would need a shared store (Redis / SQL) - called out in the design doc.
    /// </summary>
    public sealed class FastReportJobStore : IFastReportJobStore
    {
        private const int RetentionMinutes = 30;
        private const int MaxJobs = 200;

        private readonly ConcurrentDictionary<string, FastReportJob> _jobs = new();
        private readonly ILogger<FastReportJobStore> _logger;

        public FastReportJobStore(ILogger<FastReportJobStore> logger)
        {
            _logger = logger;
        }

        public FastReportJob Create(FastReportRequest request)
        {
            Sweep();

            var job = new FastReportJob { Request = request };
            _jobs[job.JobId] = job;
            job.Append($"Job created ({request}).");
            return job;
        }

        public FastReportJob Get(string jobId) =>
            string.IsNullOrWhiteSpace(jobId) ? null : _jobs.TryGetValue(jobId, out var job) ? job : null;

        public IReadOnlyCollection<FastReportJob> All() => _jobs.Values.ToList();

        public void Remove(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return;

            if (_jobs.TryRemove(jobId, out var job))
                TryDeleteOutput(job);
        }

        private void Sweep()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-RetentionMinutes);

            var expired = _jobs.Values
                .Where(j => j.IsTerminal && (j.FinishedUtc ?? j.CreatedUtc) < cutoff)
                .ToList();

            // Hard cap as a backstop in case something is creating jobs faster than they expire.
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
                    TryDeleteOutput(removed);
            }

            if (expired.Count > 0)
                _logger.LogInformation("FastReport: swept {Count} expired job(s).", expired.Count);
        }

        private void TryDeleteOutput(FastReportJob job)
        {
            try { job.Cancellation?.Dispose(); } catch { /* already disposed */ }

            try
            {
                if (!string.IsNullOrEmpty(job.OutputFilePath) && File.Exists(job.OutputFilePath))
                    File.Delete(job.OutputFilePath);

                var dir = string.IsNullOrEmpty(job.OutputFilePath) ? null : Path.GetDirectoryName(job.OutputFilePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("FastReport: could not clean up job {JobId} output. {Message}", job.JobId, ex.Message);
            }
        }
    }
}
