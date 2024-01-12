using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteDuress
    {
        [Key]
        public int Id { get; set; }

        public int ClientSiteId { get; set; }

        public bool IsEnabled { get; set; }

        public int EnabledBy { get; set; }

        public DateTime EnabledDate { get; set; }
        public string GpsCoordinates { get; set; }
        
        public string EnabledAddress { get; set; }

        public bool? PlayDuressAlarm { get; set; }
    }
}
