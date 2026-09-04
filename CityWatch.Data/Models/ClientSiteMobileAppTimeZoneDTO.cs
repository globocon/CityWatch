using System;
using System.ComponentModel.DataAnnotations;

namespace CityWatch.Data.Models
{   
    public class ClientSiteMobileAppTimeZoneDTO
    {
        public int ClientSiteId { get; set; }
        public string TimezoneString { get; set; }
        public string UTC { get; set; }

    }
}
