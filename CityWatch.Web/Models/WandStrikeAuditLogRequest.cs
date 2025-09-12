using System;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class WandStrikeAuditLogRequest
    {        
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

        public string TagId { get; set; }

        public string[] TagIds
        {
            get
            {
                return TagId?.Split(",").ToArray() ?? Array.Empty<string>();
            }
        }

        public string TagTypeId { get; set; }
        public int[] TagTypeIds
        {
            get
            {
                return TagTypeId?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }

        public string TagLabel { get; set; }

        public int[] TagLabelIds
        {
            get
            {
                return TagLabel?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }

        public string SmartWandId { get; set; }

        public int[] SmartWandIds
        {
            get
            {
                return SmartWandId?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }

        public string GuardLicenceNoId { get; set; }

        //public int[] GuardLicenceNoIds
        //{
        //    get
        //    {
        //        return GuardLicenceNoId?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
        //    }
        //}
        public string GuardName { get; set; }
                
    }
}
