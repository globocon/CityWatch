using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Services
{
    public interface IClientSiteViewDataService
    {
        
        List<SelectListItem> GetClientSiteLocations();
        List<SelectListItem> GetClientSiteLocationsNew(int[] clientSiteIds);


    }
    public class ClientSiteViewDataService: IClientSiteViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;

        public ClientSiteViewDataService(IClientDataProvider clientDataProvider,
            IGuardSettingsDataProvider guardSettingsDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
        }
        public List<SelectListItem> GetClientSiteLocations()
        {
            var siteLocatoins = new List<SelectListItem>() { new SelectListItem("Select", string.Empty) };

            siteLocatoins.AddRange(_guardSettingsDataProvider.GetClientSiteLocations()
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return siteLocatoins;
        }
        public List<SelectListItem> GetClientSiteLocationsNew(int[] clientSiteIds)
        {
            var siteLocatoins = new List<SelectListItem>();

            siteLocatoins.AddRange(_guardSettingsDataProvider.GetClientSiteLocations(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return siteLocatoins;
        }
        
    }
}
