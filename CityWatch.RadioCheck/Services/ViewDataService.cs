using CityWatch.Data.Providers;
using CityWatch.Data;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System.Collections.Generic;
using System.Linq;
using CityWatch.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace CityWatch.RadioCheck.Services
{
    
    public interface IViewDataService
    {
        List<GuardViewModel> GetGuards();
        
        public int GetClientTypeCount(int? typeId);
        public List<SelectListItem> ClientTypesUsingLoginUserId(int guardId);
        public List<SelectListItem> GetClientSites(string type = "");
        public List<SelectListItem> GetClientSitesUsingLoginUserId(int guardId, string type = "");
        List<GuardViewModel> GetActiveGuards();
        List<SelectListItem> GetOfficerPositions(OfficerPositionFilterManning positionFilter = OfficerPositionFilterManning.SecurityOnly);
        List<SelectListItem> ProviderList();
        List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm, string searchTermtwo);
        public List<SelectListItem> KPITelematicsList();
        public Guard GetGuardsDetails(int GuardID);
    }

    public class ViewDataService : IViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IKpiDataProvider _kpiDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly CityWatchDbContext _context;
        private readonly IUserDataProvider _userDataProvider;

        public ViewDataService(IClientDataProvider clientDataProvider,
            IKpiDataProvider kpiDataProvider,
            IUserDataProvider userDataProvider,
            IConfigDataProvider configDataProvider,
            IGuardDataProvider guardDataProvider, CityWatchDbContext context)
        {
            _clientDataProvider = clientDataProvider;
            _kpiDataProvider = kpiDataProvider;
            _userDataProvider = userDataProvider;
            _configDataProvider = configDataProvider;
            _guardDataProvider = guardDataProvider;
            _context = context;
        }
        public List<GuardViewModel> GetGuards()
        {
            var guards = _guardDataProvider.GetGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            var guardLanuages= _guardDataProvider.GetGuardLanguages(guards.Select(z => z.Id).ToArray());

            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id), guardLanuages.Where(y => y.GuardId == z.Id).ToList())).ToList();
        }
        public Guard GetGuardsDetails(int GuardID)
        {
            var guards = _guardDataProvider.GetGuards();
            return guards.Where(x=>x.Id==GuardID).FirstOrDefault();
        }
        public List<SelectListItem> ClientTypes
        {
            get
            {
                var clientTypes = _clientDataProvider.GetClientTypes();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in clientTypes)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }

                return items;
            }
        }
        //To get the count of ClientType start
        public int GetClientTypeCount(int? typeId)
        {
            var result = _clientDataProvider.GetClientSite(typeId);
            return result;
        }
        //To get the count of ClientType stop
        public List<SelectListItem> ClientTypesUsingLoginUserIdCount(int guardId)
        {
            if (guardId == 0)
            {
                var clientTypes = _clientDataProvider.GetClientTypes();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                var sortedClientTypes = clientTypes.OrderByDescending(clientType => GetClientTypeCount(clientType.Id));
                sortedClientTypes = sortedClientTypes.OrderBy(clientType => clientType.Name);

                foreach (var item in sortedClientTypes)
                {
                    var countClientType = GetClientTypeCount(item.Id);
                    items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name));
                }

                return items;
            }
            else
            {
                List<GuardLogin> guardLogins = new List<GuardLogin>();

                var ClientType = _context.GuardLogins
                    .Where(z => z.GuardId == guardId)
                        .Include(x => x.ClientSite.ClientType)
                        .Include(x => x.ClientSite)
                        .ToList();
                var sortedClientTypes = ClientType.OrderByDescending(clientType => GetClientTypeCount(clientType.Id));

                sortedClientTypes = sortedClientTypes.OrderBy(clientType => clientType.ClientSite.Name);
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in sortedClientTypes)
                {
                    if (!items.Any(cus => cus.Text == item.ClientSite.ClientType.Name))
                    {
                        var countClientType = GetClientTypeCount(item.Id);
                        items.Add(new SelectListItem($"{item.ClientSite.ClientType.Name} ({countClientType})", item.ClientSite.ClientType.Name));
                    }
                }

                return items;

            }

        }
        public List<SelectListItem> ClientTypesUsingLoginUserId(int guardId)
        {
            if (guardId == 0)
            {
                var clientTypes = _clientDataProvider.GetClientTypes();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in clientTypes)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }

                return items;
            }
            else
            {
                List<GuardLogin> guardLogins = new List<GuardLogin>();

                var ClientType = _context.GuardLogins
                    .Where(z => z.GuardId == guardId)
                        .Include(x => x.ClientSite.ClientType)
                        .ToList();

                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in ClientType)
                {
                    if (!items.Any(cus => cus.Text == item.ClientSite.ClientType.Name))
                    {
                        items.Add(new SelectListItem(item.ClientSite.ClientType.Name, item.ClientSite.ClientType.Name));
                    }
                }

                return items;

            }

        }

        public List<SelectListItem> GetClientSites(string type = "")
        {
            var sites = new List<SelectListItem>();
            var mapping = _clientDataProvider.GetClientSites(null).Where(x => x.ClientType.Name == type).OrderBy(clientType => clientType.Name);
            foreach (var item in mapping)
            {
                sites.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }
            return sites;
        }

        public List<SelectListItem> GetClientSitesUsingLoginUserId(int guardId, string type = "")
        {
            if (guardId == 0)
            {
                var sites = new List<SelectListItem>();
                //var mapping = _clientDataProvider.GetClientSites(null).Where(x => x.ClientType.Name == type);
                // var mapping = _context.UserClientSiteAccess
                //.Where(x => x.ClientSite.ClientType.Name.Trim() == type.Trim() && x.ClientSite.IsActive == true)
                //.Include(x => x.ClientSite)
                //.Include(x => x.ClientSite.ClientType)
                //.OrderBy(x => x.ClientSite.Name)
                //.ToList();

                var mapping = _context.UserClientSiteAccess
               .Where(x => x.ClientSite.ClientType.Name.Trim() == type.Trim() && x.ClientSite.IsActive == true)
               .Include(x => x.ClientSite)
               .Include(x => x.ClientSite.ClientType)
               .Select(x => new { x.ClientSiteId, x.ClientSite.Name })
            .Distinct().
             OrderBy(x => x.Name).ToList();


                foreach (var item in mapping)
                {
                    sites.Add(new SelectListItem(item.Name, item.ClientSiteId.ToString()));
                }
                return sites;

            }
            else
            {
                var sites = new List<SelectListItem>();

                var mapping = _context.GuardLogins
                .Where(z => z.GuardId == guardId)
                    .Include(z => z.ClientSite)
                    .OrderBy(x => x.ClientSite.Name)
                    .ToList();


                foreach (var item in mapping)
                {
                    if (!sites.Any(cus => cus.Text == item.ClientSite.Name))
                    {
                        sites.Add(new SelectListItem(item.ClientSite.Name, item.ClientSite.Id.ToString()));
                    }
                }
                return sites;

            }
        }


        public List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm, string searchTermtwo)
        {
            var results = new List<ClientSite>();
            var clientSites = _clientDataProvider.GetClientSites(typeId);
            if (userId == null)
                results = clientSites;
            else
            {

                
                var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
                var clientSiteIds = allUserAccess.Select(x => x.ClientSite.Id).Distinct().ToList();
                results = clientSites.Where(x => clientSiteIds.Contains(x.Id)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
                results = results.Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(x.Address) && x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))).ToList();
            if (!string.IsNullOrEmpty(searchTermtwo))
                results = results.Where(x => !string.IsNullOrEmpty(x.Emails) && x.Emails.Contains(searchTermtwo, StringComparison.OrdinalIgnoreCase)).ToList();

            return results;
        }
        public List<GuardViewModel> GetActiveGuards()
        {
            var guards = _guardDataProvider.GetActiveGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            var guardLanuages = _guardDataProvider.GetGuardLanguages(guards.Select(z => z.Id).ToArray());
            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id), guardLanuages.Where(y => y.GuardId == z.Id).ToList())).ToList();
        }
        //public List<KeyVehicleLog> GetCompanyDetails()
        //{
        //    //var guards = _guardDataProvider.GetEmailPOCVehiclelog( id);
        //    //var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
        //    //return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        //}

        public List<SelectListItem> GetOfficerPositions(OfficerPositionFilterManning positionFilter = OfficerPositionFilterManning.All)
        {
            var items = new List<SelectListItem>()
            {
                new SelectListItem("Select", "", true),
            };
            var officerPositions = _configDataProvider.GetPositions();
            foreach (var officerPosition in officerPositions.Where(z => positionFilter == OfficerPositionFilterManning.All ||
                 positionFilter == OfficerPositionFilterManning.PatrolOnly && z.IsPatrolCar ||
                 positionFilter == OfficerPositionFilterManning.NonPatrolOnly && !z.IsPatrolCar ||
                 positionFilter == OfficerPositionFilterManning.SecurityOnly && z.Name.Contains("Security")))
            {
                items.Add(new SelectListItem(officerPosition.Name, officerPosition.Id.ToString()));
            }

            return items;
        }

        public List<SelectListItem> ProviderList()
        {
            var items = new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true)
                };
            var KVID = _configDataProvider.GetKVLogField();
            var providerlist = _configDataProvider.GetProviderList(KVID.Id);
            foreach (var item in providerlist)
            {
                if (item.CompanyName != null)
                {
                    items.Add(new SelectListItem(item.CompanyName, item.CompanyName));
                }

            }
            return items;
        }
        public List<SelectListItem> KPITelematicsList()
        {

            var items = new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true)
                };
            var NamesList = _configDataProvider.GetTelematicsList();

            foreach (var item in NamesList)
            {

                items.Add(new SelectListItem(item.Name, item.Id.ToString()));


            }
            return items;

        }
    }
}
