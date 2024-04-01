using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Services
{
    public interface IClientSiteViewDataService
    {
        List<SelectListItem> GetUserClientSitesWithId(string types);
        List<SelectListItem> GetClientSitePocs(int[] clientSiteIds);
        List<SelectListItem> GetClientSitePocsVehicleLog(int[] clientSiteIds);
        
        List<SelectListItem> GetClientSiteLocations(int[] clientSiteIds);
        List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds, string searchKeyNo);
        List<SelectListItem> GetClientSiteLocationsNew(int[] clientSiteIds);
        List<SelectListItem> GetClientSitePocsNew(int[] clientSiteIds);
    }

    public class ClientSiteViewDataService : IClientSiteViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;

        public ClientSiteViewDataService(IClientDataProvider clientDataProvider, 
            IGuardSettingsDataProvider guardSettingsDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
        }

        public List<SelectListItem> GetUserClientSitesWithId(string types)
        {
            if (string.IsNullOrEmpty(types))
                return Enumerable.Empty<SelectListItem>().ToList();

            return _clientDataProvider.GetClientSites(null)
                .Where(z => types.Split(';').Contains(z.ClientType.Name))
                .Select(z => new SelectListItem(z.Name, z.Id.ToString()))
                .ToList();
        }

        public List<SelectListItem> GetClientSitePocs(int[] clientSiteIds)
        {
            var sitePocs = new List<SelectListItem>() { new SelectListItem("Select", string.Empty) };

            sitePocs.AddRange(_guardSettingsDataProvider.GetClientSitePocs(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return sitePocs;
        }
        public List<SelectListItem> GetClientSitePocsVehicleLog(int[] clientSiteIds)
        {
            var sitePocs = new List<SelectListItem>();

            sitePocs.AddRange(_guardSettingsDataProvider.GetClientSitePocs(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return sitePocs;
        }
       

        public List<SelectListItem> GetClientSiteLocations(int[] clientSiteIds)
        {
            var siteLocatoins = new List<SelectListItem>() { new SelectListItem("Select", string.Empty) };

            siteLocatoins.AddRange(_guardSettingsDataProvider.GetClientSiteLocations(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return siteLocatoins;
        }
        public List<SelectListItem> GetClientSitePocsNew(int[] clientSiteIds)
        {
            var sitePocs = new List<SelectListItem>() ;

            sitePocs.AddRange(_guardSettingsDataProvider.GetClientSitePocs(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return sitePocs;
        }

        public List<SelectListItem> GetClientSiteLocationsNew(int[] clientSiteIds)
        {
            var siteLocatoins = new List<SelectListItem>();

            siteLocatoins.AddRange(_guardSettingsDataProvider.GetClientSiteLocations(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return siteLocatoins;
        }

        public List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds, string searchKeyNo)
        {
            return _guardSettingsDataProvider.GetClientSiteKeys(clientSiteIds)
                        .Where(z => string.IsNullOrEmpty(searchKeyNo) || z.KeyNo.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase))
                        .ToList();
        }
    }
}
