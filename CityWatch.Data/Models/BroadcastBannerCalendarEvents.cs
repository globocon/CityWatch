using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class BroadcastBannerCalendarEvents
    {
        public int id { get; set; }
        public string ReferenceNo { get; set; }
        public string TextMessage { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool RepeatYearly { get; set; }
        public bool IsPublicHoliday { get; set; }
        

        [NotMapped]
        public string FormattedStartDate { get { return StartDate.ToString("dd-MMM-yyyy"); } }
        [NotMapped]
        public string FormattedExpiryDate { get { return ExpiryDate.ToString("dd-MMM-yyyy"); } }

    }
}
