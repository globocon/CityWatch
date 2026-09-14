using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// One execution of the weekly or monthly log dump.
    ///
    /// Same shape and lifecycle as <see cref="KpiSendScheduleJob"/>, which does this for the KPI
    /// schedule sender: the row is inserted when the run starts and completed when it finishes, so
    /// an in-progress run is one with no <see cref="CompletedDate"/>.
    ///
    /// Deliberately its own table rather than reusing KpiSendScheduleJobs. KpiReportController.Send
    /// reads that table whole, with no task discriminator, and deletes any incomplete row it finds -
    /// a log dump writing there would read as a KPI job in progress and have its own row deleted
    /// out from under it.
    ///
    /// Sits alongside the other two periodic tables rather than replacing either:
    /// ClientSitePeriodicLogUpload is per site and log type, SchedulerTaskError is per failure, and
    /// this is per run.
    /// </summary>
    public class PeriodicLogDumpJob
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// "Weekly Log Dump" or "Monthly Log Dump". The in-progress guard is per task name, so the
        /// weekly and monthly runs never block one another.
        /// </summary>
        public string TaskName { get; set; }

        public LogDumpPeriodType PeriodType { get; set; }

        public DateTime PeriodStartDate { get; set; }

        public DateTime PeriodEndDate { get; set; }

        public DateTime CreatedDate { get; set; }

        /// <summary>Null while the run is in progress. This is what the guard tests.</summary>
        public DateTime? CompletedDate { get; set; }

        public bool? Success { get; set; }

        public string StatusMessage { get; set; }

        public int DumpsProduced { get; set; }

        public int DumpsSkipped { get; set; }

        public int DumpsFailed { get; set; }
    }
}
