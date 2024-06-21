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
    }

    public class ViewDataService : IViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IKpiDataProvider _kpiDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly CityWatchDbContext _context;

        public ViewDataService(IClientDataProvider clientDataProvider,
            IKpiDataProvider kpiDataProvider,
            IConfigDataProvider configDataProvider,
            IGuardDataProvider guardDataProvider, CityWatchDbContext context)
        {
            _clientDataProvider = clientDataProvider;
            _kpiDataProvider = kpiDataProvider;
            _configDataProvider = configDataProvider;
            _guardDataProvider = guardDataProvider;
            _context = context;
        }
        public List<GuardViewModel> GetGuards()
        {
            var guards = _guardDataProvider.GetGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
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
        public List<GuardViewModel> GetActiveGuards()
        {
            var guards = _guardDataProvider.GetActiveGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        }
        //public List<KeyVehicleLog> GetCompanyDetails()
        //{
        //    //var guards = _guardDataProvider.GetEmailPOCVehiclelog( id);
        //    //var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
        //    //return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        //}
    }
}
