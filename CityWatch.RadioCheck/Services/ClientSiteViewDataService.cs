using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.RadioCheck.Services
{
    public interface IClientSiteViewDataService
    {
        List<SelectListItem> GetUserClientSitesWithId(string types);
        List<SelectListItem> GetClientSites();
    }

    public class ClientSiteViewDataService : IClientSiteViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        //private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;

        public ClientSiteViewDataService(IClientDataProvider clientDataProvider
            )
        {
            _clientDataProvider = clientDataProvider;
          
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

        public List<SelectListItem> GetClientSites()
        {
            var sitePocs = new List<SelectListItem>();


            sitePocs.AddRange(_clientDataProvider.GetClientSites(null)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return sitePocs;
        }
    }
}
