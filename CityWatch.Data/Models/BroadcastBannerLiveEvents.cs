using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityWatch.Data.Models
{
    public class BroadcastBannerLiveEvents
    {
        public int id { get; set; }
        public string TextMessage { get; set; }

        public DateTime ExpiryDate { get; set; }
        public string Weblink { get; set; }

        [NotMapped]
        public string FormattedExpiryDate { get { return ExpiryDate.ToString("dd-MMM-yyyy"); } }

    }
}
