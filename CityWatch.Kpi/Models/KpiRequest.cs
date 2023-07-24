using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Kpi.Models
{
    public class KpiRequest
    {
        [Display(Name = "Client Site")]
        public int ClientSiteId { get; set; }

        public DateTime ReportDate { get; set; }
    }
}
