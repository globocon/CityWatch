using CityWatch.Data.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Kpi.Models
{
    public class KpiTagHistoryScheduleViewModel
    {
        public int Id { get; set; }

        /// <summary>Shown in the schedules grid - the schedule always covers every site.</summary>
        public string ClientTypes => "All";

        public string ClientSites => "All client sites";

        [Required]
        [Display(Name = "Start Date")]
        [DateEqualOrGreaterThanToday]
        public DateTime? StartDate { get; set; }

        [DateLessThan("StartDate", ErrorMessage = "End Date must be after Start Date")]
        public DateTime? EndDate { get; set; }

        [Required]
        [Display(Name = "Run Day")]
        [Range(0, 6)]
        public int? RunDayOfWeek { get; set; }

        [Required]
        [Display(Name = "Exec. Time")]
        public string Time { get; set; }

        [Required]
        [Display(Name = "Email Recipients")]
        public string EmailTo { get; set; }

        public string EmailBcc { get; set; }

        public DateTime? NextRunOn { get; set; }

        public bool IsPaused { get; set; }

        public string ProjectName { get; set; }

        /// <summary>Weekly - the grid's frequency renderer shows it the same way as other schedules.</summary>
        public SendSchdeuleFrequency Frequency => SendSchdeuleFrequency.Weekly;

        public string RunDayName => RunDayOfWeek.HasValue ? ((DayOfWeek)RunDayOfWeek.Value).ToString() : string.Empty;

        // Outcome of the most recent run, for the grid.
        public DateTime? LastRunOn { get; set; }

        public string LastRunStatus { get; set; }

        public static KpiSendTagHistorySchedules ToDataModel(KpiTagHistoryScheduleViewModel viewModel)
        {
            return new KpiSendTagHistorySchedules()
            {
                Id = viewModel.Id,
                StartDate = viewModel.StartDate.Value,
                EndDate = viewModel.EndDate,
                RunDayOfWeek = viewModel.RunDayOfWeek.Value,
                Time = viewModel.Time,
                EmailTo = viewModel.EmailTo,
                EmailBcc = viewModel.EmailBcc,
                NextRunOn = viewModel.NextRunOn ?? DateTime.MinValue,
                ProjectName = viewModel.ProjectName,
                IsPaused = viewModel.IsPaused,
            };
        }

        public static KpiTagHistoryScheduleViewModel FromDataModel(KpiSendTagHistorySchedules dataModel, KpiSendScheduleJobsTagHistory lastJob = null)
        {
            return new KpiTagHistoryScheduleViewModel()
            {
                Id = dataModel.Id,
                StartDate = dataModel.StartDate,
                EndDate = dataModel.EndDate,
                RunDayOfWeek = dataModel.RunDayOfWeek,
                Time = dataModel.Time,
                EmailTo = dataModel.EmailTo,
                EmailBcc = dataModel.EmailBcc,
                NextRunOn = dataModel.NextRunOn,
                ProjectName = dataModel.ProjectName,
                IsPaused = dataModel.IsPaused,
                LastRunOn = lastJob?.CreatedDate,
                LastRunStatus = lastJob == null ? null : (lastJob.EmailStatus ?? lastJob.ReportStatus),
            };
        }
    }
}
