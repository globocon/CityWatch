using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Data.Models
{
    public enum SendSchdeuleFrequency
    {
        Daily,
        Weekly,
        Monthly
    }

    public enum CoverSheetType
    {
        Weekly,
        Monthly
    }

    public static class KpiSendScheduleRunOnCalculator
    {
        public static DateTime GetNextRunOn(KpiSendSchedule sendSchedule)
        {
            DateTime calculatedNextRunOn;

            // This a new schedule
            if (sendSchedule.Id == 0 || sendSchedule.NextRunOn == DateTime.MinValue)
            {
                var firstRunOn = DateTime.Parse($"{sendSchedule.StartDate.ToShortDateString()} {sendSchedule.Time}");
                calculatedNextRunOn = firstRunOn > DateTime.Now ? firstRunOn : GetFrequencyBasedNextRunOn(firstRunOn, sendSchedule.Frequency);
            }
            else
                calculatedNextRunOn = GetFrequencyBasedNextRunOn(sendSchedule.NextRunOn, sendSchedule.Frequency);

            if (calculatedNextRunOn <= DateTime.Now)
                calculatedNextRunOn = GetFrequencyBasedNextRunOn(DateTime.Parse($"{DateTime.Today.ToShortDateString()} {sendSchedule.Time}"), sendSchedule.Frequency);

            if (sendSchedule.EndDate.HasValue && calculatedNextRunOn > sendSchedule.EndDate.Value)
                calculatedNextRunOn = DateTime.MaxValue;
            return calculatedNextRunOn;
        }

        private static DateTime GetFrequencyBasedNextRunOn(DateTime nextRunOn, SendSchdeuleFrequency frequency) => frequency switch
        {
            SendSchdeuleFrequency.Daily => nextRunOn.AddDays(1),
            SendSchdeuleFrequency.Weekly => nextRunOn.AddDays(7),
            SendSchdeuleFrequency.Monthly => nextRunOn.AddMonths(1),
            _ => nextRunOn,
        };
    }

    public class KpiSendSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public SendSchdeuleFrequency Frequency { get; set; }

        [Required]
        public string Time { get; set; }

        public string EmailTo { get; set; }

        public DateTime NextRunOn { get; set; }

        public bool IsPaused { get; set; }

        public string ProjectName { get; set; }

        public string SummaryNote1 { get; set; }

        public string SummaryNote2 { get; set; }

        public CoverSheetType CoverSheetType { get; set; }

        public string EmailBcc { get; set; }

        public bool IsHrTimerPaused { get; set; }

        public List<KpiSendScheduleSummaryNote> KpiSendScheduleSummaryNotes { get; set; }

        public KpiSendScheduleSummaryNote NoteForThisMonth
        {
            get
            {
                var thisMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var notesThisMonth = KpiSendScheduleSummaryNotes?.SingleOrDefault(z => z.ForMonth == thisMonthDate);
                return notesThisMonth ?? new KpiSendScheduleSummaryNote()
                {
                    ForMonth = thisMonthDate,
                    Notes = string.Empty,
                    ScheduleId = Id
                };
            }
        }

        public ICollection<KpiSendScheduleClientSite> KpiSendScheduleClientSites { get; set; }

        public KpiSendScheduleSummaryImage KpiSendScheduleSummaryImage { get; set; }
    }
}
