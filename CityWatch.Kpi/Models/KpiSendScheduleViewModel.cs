using CityWatch.Data.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class KpiSendScheduleViewModel
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

        public static KpiSendSchedule ToDataModel(KpiSendScheduleViewModel viewModel)
        {
            return new KpiSendSchedule()
            {
                Id = viewModel.Id,
                KpiSendScheduleClientSites = viewModel.ClientSiteIds.Select(z => new KpiSendScheduleClientSite() { ScheduleId = viewModel.Id, ClientSiteId = z }).ToList(),
                StartDate = viewModel.StartDate.Value,
                EndDate = viewModel.EndDate,
                Frequency = viewModel.Frequency.Value,
                Time = viewModel.Time,
                EmailTo = viewModel.EmailTo,
                NextRunOn = viewModel.NextRunOn ?? DateTime.MinValue,
                IsPaused = viewModel.IsPaused,
                ProjectName = viewModel.ProjectName,
                SummaryNote1 = viewModel.SummaryNote1,
                SummaryNote2 = viewModel.SummaryNote2,
                CoverSheetType = viewModel.CoverSheetType,
                EmailBcc = viewModel.EmailBcc,
                IsHrTimerPaused = viewModel.IsHrTimerPaused
            };
        }

        public static KpiSendScheduleViewModel FromDataModel(KpiSendSchedule dataModel)
        {
            return new KpiSendScheduleViewModel()
            {
                Id = dataModel.Id,
                ClientSiteIds = dataModel.KpiSendScheduleClientSites.Select(z => z.ClientSiteId).ToArray(),
                ClientTypes = string.Join(", ", dataModel.KpiSendScheduleClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct()),
                ClientSites = string.Join(", ", dataModel.KpiSendScheduleClientSites.Select(z => z.ClientSite.Name).Distinct()),
                StartDate = dataModel.StartDate,
                EndDate = dataModel.EndDate,
                Frequency = dataModel.Frequency,
                Time = dataModel.Time,
                EmailTo = dataModel.EmailTo,
                NextRunOn = dataModel.NextRunOn,
                IsPaused = dataModel.IsPaused,
                ProjectName = dataModel.ProjectName,
                SummaryNote1 = dataModel.SummaryNote1,
                SummaryNote2 = dataModel.SummaryNote2,
                CoverSheetType = dataModel.CoverSheetType,
                EmailBcc = dataModel.EmailBcc,
                IsHrTimerPaused = dataModel.IsHrTimerPaused
            };
        }
    }

    public class DateLessThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateLessThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var currentValue = (DateTime)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException("Property with this name not found");

            var comparisonValue = (DateTime)property.GetValue(validationContext.ObjectInstance);

            if (currentValue <= comparisonValue)
                return new ValidationResult(ErrorMessageString);

            return ValidationResult.Success;
        }
    }

    public class DateEqualOrGreaterThanTodayAttribute : ValidationAttribute
    {
        private const string ID_PROPERTY_NAME = "Id";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var isEditProperty = validationContext.ObjectType.GetProperty(ID_PROPERTY_NAME);
            if (isEditProperty != null && (int)isEditProperty.GetValue(validationContext.ObjectInstance) != 0)
                return ValidationResult.Success;

            DateTime dt = (DateTime)value;
            if (dt >= DateTime.Today)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? "Start Date should be >= today");
        }
    }
}
