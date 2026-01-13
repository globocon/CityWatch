using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Kpi.Models
{
    public class RCLinkedDuressViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client Sites")]        
        public int[] ClientSiteIds { get; set; }

        public string ClientTypes { get; set; }

        public string ClientSites { get; set; }


        [Required]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; }

        public string[] LinkedServices { get; set; }

        public static RCLinkedDuressMaster ToDataModel(RCLinkedDuressViewModel viewModel)
        {
            return new RCLinkedDuressMaster()
            {
                Id = viewModel.Id,
                GroupName= viewModel.GroupName,
                RCLinkedDuressClientSites = viewModel.ClientSiteIds.Select(z => new RCLinkedDuressClientSites() { RCLinkedId = viewModel.Id, ClientSiteId = z }).ToList(),
                IsLB = viewModel.LinkedServices != null && viewModel.LinkedServices.Any(s => string.Equals(s, "LB", StringComparison.OrdinalIgnoreCase)),
                IsKV = viewModel.LinkedServices != null && viewModel.LinkedServices.Any(s => string.Equals(s, "KV", StringComparison.OrdinalIgnoreCase)),
                IsSW = viewModel.LinkedServices != null && viewModel.LinkedServices.Any(s => string.Equals(s, "SW", StringComparison.OrdinalIgnoreCase)),

            };
        }

        public static RCLinkedDuressViewModel FromDataModel(RCLinkedDuressMaster dataModel)
        {
            //return new RCLinkedDuressViewModel()
            //{
            //    Id = dataModel.Id,
            //    ClientSiteIds = dataModel.RCLinkedDuressClientSites.Select(z => z.ClientSiteId).ToArray(),
            //    ClientTypes = string.Join(", ", dataModel.RCLinkedDuressClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct()),
            //    ClientSites = string.Join(", ", dataModel.RCLinkedDuressClientSites.Select(z => z.ClientSite.Name).Distinct()),
            //    GroupName = dataModel.GroupName,

            //};
            var linkedServices = new List<string>();

            if (dataModel.IsLB)
                linkedServices.Add("LB");
            if (dataModel.IsKV)
                linkedServices.Add("KV");
            if (dataModel.IsSW)
                linkedServices.Add("SW");

            return new RCLinkedDuressViewModel()
            {
                Id = dataModel.Id,
                ClientSiteIds = dataModel.RCLinkedDuressClientSites.Select(z => z.ClientSiteId).ToArray(),
                ClientTypes = string.Join(", ", dataModel.RCLinkedDuressClientSites.Select(z => z.ClientSite.ClientType.Name).Distinct()),
                ClientSites = string.Join(", ", dataModel.RCLinkedDuressClientSites.Select(z => z.ClientSite.Name).Distinct()),
                GroupName = dataModel.GroupName,
                LinkedServices = linkedServices.ToArray()
            };

        }
    }

   

    
}
