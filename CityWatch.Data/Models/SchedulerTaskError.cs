using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// One failure from a scheduled task.
    ///
    /// SiteLogUploadHistory already records a running commentary of the daily run, but it is a
    /// single free-text column with successes, progress and failures mixed together and no site,
    /// period or log type to filter on. This records failures in columns instead, so "what failed
    /// last Monday, and for which site" is a query rather than a read-through.
    /// </summary>
    public class SchedulerTaskError
    {
        [Key]
        public int Id { get; set; }

        /// <summary>e.g. "Weekly Log Dump".</summary>
        public string TaskName { get; set; }

        /// <summary>Null when the failure is not tied to a period - the run failing to start, say.</summary>
        public LogDumpPeriodType? PeriodType { get; set; }

        /// <summary>
        /// Null when the failure is not specific to one site. Deliberately not a foreign key: this
        /// is a log, and it must never be the reason a site cannot be removed.
        /// </summary>
        public int? ClientSiteId { get; set; }

        public LogBookType? LogBookType { get; set; }

        public DateTime? PeriodStartDate { get; set; }

        public DateTime? PeriodEndDate { get; set; }

        public DateTime OccurredOn { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>Stack trace and inner exceptions.</summary>
        public string ErrorDetails { get; set; }
    }
}
