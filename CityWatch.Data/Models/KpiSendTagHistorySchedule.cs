using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    /// <summary>
    /// A weekly Tag History email (Kpi > Settings > Schedules > Tag History). Unlike the Custom
    /// Wand schedule it has no site list: every run reports every site's tag changes, so a site
    /// created after the schedule was set up is included as soon as it has tags.
    /// </summary>
    public class KpiSendTagHistorySchedules
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        /// <summary>The day of the week the report runs on - <see cref="System.DayOfWeek"/> as an int, Sunday = 0.</summary>
        [Required]
        public int RunDayOfWeek { get; set; }

        /// <summary>Execution time, "HH:mm".</summary>
        [Required]
        public string Time { get; set; }

        /// <summary>Comma separated recipients.</summary>
        public string EmailTo { get; set; }

        /// <summary>Comma separated BCC recipients.</summary>
        public string EmailBcc { get; set; }

        public DateTime NextRunOn { get; set; }

        public string ProjectName { get; set; }

        public bool IsPaused { get; set; }
    }

    /// <summary>
    /// One run of a Tag History schedule, automatic or manual: which week it covered, whether the
    /// report was generated and whether the email went. A week with no tag changes is recorded
    /// here with EmailSent = false and nothing is sent.
    /// </summary>
    public class KpiSendScheduleJobsTagHistory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Null for the job-level row the scheduler writes around a batch.</summary>
        public int? ScheduleId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        /// <summary>"Auto" when run by the scheduler, "Manual" from Run Now.</summary>
        public string RunType { get; set; }

        public DateTime? ReportFromDate { get; set; }

        public DateTime? ReportToDate { get; set; }

        public int RecordCount { get; set; }

        public bool ReportGenerated { get; set; }

        public string ReportStatus { get; set; }

        public bool EmailSent { get; set; }

        public string EmailStatus { get; set; }

        /// <summary>Who the email went to (after any test-mode redirect is applied).</summary>
        public string EmailTo { get; set; }

        public bool? Success { get; set; }

        public string StatusMessage { get; set; }
    }

    public static class KpiTagHistoryScheduleRunOnCalculator
    {
        /// <summary>
        /// The next time the schedule should run after <paramref name="after"/> (default now): the
        /// first <see cref="KpiSendTagHistorySchedules.RunDayOfWeek"/> at
        /// <see cref="KpiSendTagHistorySchedules.Time"/> that is no earlier than the start date and
        /// later than <paramref name="after"/>. DateTime.MaxValue once it would pass the end date.
        /// </summary>
        public static DateTime GetNextRunOn(KpiSendTagHistorySchedules schedule, DateTime? after = null)
        {
            var now = after ?? DateTime.Now;
            var time = TimeSpan.TryParse(schedule.Time, out var parsed) ? parsed : TimeSpan.Zero;

            var day = schedule.StartDate.Date > now.Date ? schedule.StartDate.Date : now.Date;
            var diff = ((schedule.RunDayOfWeek - (int)day.DayOfWeek) % 7 + 7) % 7;
            var nextRunOn = day.AddDays(diff).Add(time);

            if (nextRunOn <= now)
                nextRunOn = nextRunOn.AddDays(7);

            if (schedule.EndDate.HasValue && nextRunOn.Date > schedule.EndDate.Value.Date)
                return DateTime.MaxValue;

            return nextRunOn;
        }

        /// <summary>
        /// The week a run reports on: the seven full days before <paramref name="runDate"/>, so a
        /// Wednesday run covers the previous Wednesday to Tuesday.
        /// </summary>
        public static (DateTime fromDate, DateTime toDate) GetReportPeriod(DateTime runDate)
        {
            var toDate = runDate.Date.AddDays(-1);
            return (toDate.AddDays(-6), toDate);
        }
    }
}
