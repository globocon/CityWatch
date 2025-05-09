using CityWatch.Data.Models;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class KeyVehicleLogAuditLogRequest
    {
        public LogBookType LogBookType { get; set; }

        public DateTime LogFromDate { get; set; }

        public DateTime LogToDate { get; set; }

        public string ClientSiteId { get; set; }

        public int[] ClientSiteIds 
        { 
            get 
            { 
                return ClientSiteId?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>(); 
            } 
        }

        public string VehicleRego { get; set; }

        public string CompanyName { get; set; }

        public string PersonName { get; set; }

        public int? PersonType { get; set; }

        public int? EntryReason { get; set; }

        public string Product { get; set; }

        public int? TruckConfig { get; set; }

        public int? TrailerType { get; set; }

        public int? ClientSitePocId { get; set; }
        public string ClientSitePocIdNew { get; set; }
        public int[] ClientSitePocIds
        {
            get
            {
                return ClientSitePocIdNew?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }
        public int? ClientSiteLocationId { get; set; }
        public string ClientSiteLocationIdNew { get; set; }
        public int[] ClientSiteLocationIds
        {
            get
            {
                return ClientSiteLocationIdNew?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }
        public string KeyNo { get; set; }
        //public int? PersonOfInterest { get; set; }
        public string PersonOfInterest { get; set; }
        public int[] PersonOfInterestIds
        {
            get
            {
                return PersonOfInterest?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }
        [NotMapped]
        public string DateRange { get; set; }
        [NotMapped]
        public int RecordCount { get; set; }
    }
}
