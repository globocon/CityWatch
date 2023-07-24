using CityWatch.Data.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{
    public class PatrolRequest
    {
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; }

        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        public PatrolDataFilter DataFilter { get; set; }

        public string[] ClientTypes { get; set; }

        public string[] ClientSites { get; set; }

        public string Position { get; set; }
    }
}
