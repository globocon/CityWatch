using System;
using System.Linq;

namespace CityWatch.Data.Models
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

        public string[] TagLabelIds
        {
            get
            {
                return TagLabel?.Split(",").ToArray() ?? Array.Empty<string>();
            }
        }

        public string SmartWandId { get; set; }

        public string[] SmartWandIds
        {
            get
            {
                return SmartWandId?.Split(",").ToArray() ?? Array.Empty<string>();
            }
        }

        public string GuardLicenceNoId { get; set; }               
        public string GuardName { get; set; }
        public string PatrolCarId { get; set; }

        public int[] PatrolCarIds
        {
            get
            {
                return PatrolCarId?.Split(",").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            }
        }

        public bool IspatrolCarToggleOn { get; set; }

        public bool IncludeAllTagsInStrike { get; set; } = false;

    }
}
