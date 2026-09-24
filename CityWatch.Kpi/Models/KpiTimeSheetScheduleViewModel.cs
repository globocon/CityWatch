using CityWatch.Data.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class KpiTimeSheetScheduleViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client Sites")]
        public int[] ClientSiteIds { get; set; }

        public string ClientTypes { get; set; }

        public string ClientSites { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DateEqualOrGreaterThanToday]
        public DateTime? StartDate { get; set; }

        [DateLessThan("StartDate", ErrorMessage = "End Date must be after Start Date")]
        public DateTime? EndDate { get; set; }

        [Required]
        public SendSchdeuleFrequency? Frequency { get; set; }

        [Required]
        [Display(Name = "Exec. Time")]
        public string Time { get; set; }

        public string EmailTo { get; set; }

        public DateTime? NextRunOn { get; set; }

        public bool IsPaused { get; set; }

        public string ProjectName { get; set; }

        public string SummaryNote1 { get; set; }

        public string SummaryNote2 { get; set; }

        public CoverSheetType CoverSheetType { get; set; }

        public string EmailBcc { get; set; }

        public bool IsHrTimerPaused { get; set; }
        public bool IsCriticalDocumentDownselect { get; set; }
        public string CriticalGroupNameID { get; set; }

        public static KpiSendTimesheetSchedules ToDataModel(KpiTimeSheetScheduleViewModel viewModel)
        {
            return new KpiSendTimesheetSchedules()
            {
                Id = viewModel.Id,
                KpiSendTimesheetClientSites = viewModel.ClientSiteIds.Select(z => new KpiSendTimesheetClientSites() { TimesheetId = viewModel.Id, ClientSiteId = z }).ToList(),
                StartDate = viewModel.StartDate.Value,
                EndDate = viewModel.EndDate,
                Frequency = viewModel.Frequency.Value,
                Time = viewModel.Time,
                EmailTo = viewModel.EmailTo,
                NextRunOn = viewModel.NextRunOn ?? DateTime.MinValue,
               
                ProjectName = viewModel.ProjectName,
              
                EmailBcc = viewModel.EmailBcc,
               
            };
        }

        public static KpiTimeSheetScheduleViewModel FromDataModel(KpiSendTimesheetSchedules dataModel)
        {
            return new KpiTimeSheetScheduleViewModel()
            {
                Id = dataModel.Id,
                ClientSiteIds = dataModel.KpiSendTimesheetClientSites.Select(z => z.ClientSiteId).ToArray(),
                ClientTypes = string.Join(", ", dataModel.KpiSendTimesheetClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct()),
                ClientSites = string.Join(", ", dataModel.KpiSendTimesheetClientSites.Select(z => z.ClientSite.Name).Distinct()),
                StartDate = dataModel.StartDate,
                EndDate = dataModel.EndDate,
                Frequency = dataModel.Frequency,
                Time = dataModel.Time,
                EmailTo = dataModel.EmailTo,
                NextRunOn = dataModel.NextRunOn,
                ProjectName = dataModel.ProjectName,
                EmailBcc = dataModel.EmailBcc,
            };
        }
    }

    
}
