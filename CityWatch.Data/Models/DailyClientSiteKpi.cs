using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class DailyClientSiteKpi
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public DateTime Date { get; set; }

        public decimal? EmployeeHours { get; set; }

        public decimal? ActualEmployeeHours { get; set; }

        [NotMapped]
        public decimal? EffectiveEmployeeHours
        {
            get
            {
                return ActualEmployeeHours ?? EmployeeHours;
            }
        }

        public int? ImageCount { get; set; }

        public int? WandScanCount { get; set; }

        public int? IncidentCount { get; set; }

        public int? FireOrAlarmCount { get; set; }

        public bool? IsAcceptableLogFreq { get; set; }
    }
}
