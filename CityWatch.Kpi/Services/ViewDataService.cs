using CityWatch.Data;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Models;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CityWatch.Kpi.Services
{

    public enum OfficerPositionFilterManning
    {
        All = 0,

        PatrolOnly = 1,

        NonPatrolOnly = 2,

        SecurityOnly = 3
    }
    public interface IViewDataService
    {
        List<SelectListItem> ClientTypes { get; }

        List<SelectListItem> GetClientSites(string type = "");

        List<SelectListItem> GetYears();

        List<SelectListItem> GetMonthsInYear();

        MonthlyKpiResult GetKpiReportData(int clientSiteId, DateTime fromDate, DateTime toDate);

        List<DailyKpiGuard> GetMonthlyKpiGuardData(int clientSiteId, DateTime fromDate, DateTime toDate);

        public Dictionary<int, MonthlyKpiResult> GetMonthlyKpiReportData(int[] clientSiteIds, DateTime fromDate, DateTime toDate);

        List<DailyKpiResult> GetKpiReportData(int[] clientSiteId, DateTime fromDate, DateTime toDate);

        List<SelectListItem> GetOfficerPositions(OfficerPositionFilterManning positionFilter = OfficerPositionFilterManning.SecurityOnly);
        List<SelectListItem> ClientTypesUsingLoginUserId(int guardId);
        List<SelectListItem> GetClientSitesUsingLoginUserId(int userId, string type = "");
        List<GuardLogin> GetKpiGuardDetailsData(int clientSiteId, DateTime fromDate, DateTime toDate);
        List<GuardCompliance> GetKpiGuardDetailsCompliance(int guardId);
        List<GuardCompliance> GetKpiGuardDetailsComplianceData(int[] guardIds);
        int GetClientTypeCount(int? typeId);
        List<SelectListItem> ClientTypesUsingLoginUserIdCount(int guardId);
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

                var ClientType=_context.GuardLogins
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

            public List<SelectListItem> GetYears()
            {
                var years = new List<SelectListItem>();
                foreach (var item in Enumerable.Range(DateTime.Now.Year - 1, 2).OrderByDescending(x => x))
                {
                    years.Add(new SelectListItem(item.ToString(), item.ToString()));
                }
                return years;
            }

            public List<SelectListItem> GetMonthsInYear()
            {
                var months = new List<SelectListItem>();
                var range = Enumerable.Range(0, Int32.MaxValue)
                         .Select(e => new DateTime(DateTime.Now.Year, 1, 1).AddMonths(e))
                         .TakeWhile(e => e <= new DateTime(DateTime.Now.Year, 12, 31))
                         .Select(e => new { value = e.Month, text = e.ToString("MMMM") });

                foreach (var item in range)
                {
                    bool selected = item.text == DateTime.Now.ToString("MMMM");
                    months.Add(new SelectListItem(item.text, item.value.ToString(), selected));
                }
                return months;
            }

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

            public MonthlyKpiResult GetKpiReportData(int clientSiteId, DateTime fromDate, DateTime toDate)
            {
                var dailyClientSiteKpis = _kpiDataProvider.GetDailyClientSiteKpis(clientSiteId, fromDate, toDate);
                var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);

                var dailyKpis = dailyClientSiteKpis.Select(x => new DailyKpiResult(x, clientSiteKpiSetting)).ToList();

                return new MonthlyKpiResult(clientSiteKpiSetting, dailyKpis);
            }
        
            public List<DailyKpiGuard> GetMonthlyKpiGuardData(int clientSiteId, DateTime fromDate, DateTime toDate)
            {
                var dailyClientSiteKpis = _kpiDataProvider.GetDailyClientSiteKpis(clientSiteId, fromDate, toDate).ToList();
                var guardLogins = _guardDataProvider.GetGuardLogins(clientSiteId, fromDate, toDate).ToList();
                var guardCompliances = _guardDataProvider.GetAllGuardCompliances();
                foreach (var guardLogin in guardLogins)
                {
                    // Trim OnDuty and OffDuty dates to login date
                    if (guardLogin.OnDuty.Date < guardLogin.LoginDate.Date)
                    {
                        guardLogin.OnDuty = new DateTime(guardLogin.OnDuty.Year, guardLogin.OnDuty.Month, guardLogin.OnDuty.Day, 00, 01, 00); ;
                    }

                var offDutyValue = guardLogin.OffDuty.Value;
                if (offDutyValue.Date > guardLogin.LoginDate.Date)
                {
                    guardLogin.OffDuty = new DateTime(guardLogin.LoginDate.Year, guardLogin.LoginDate.Month, guardLogin.LoginDate.Day, 23, 59, 00); ;
                }
            }
            return dailyClientSiteKpis.Select(z => new DailyKpiGuard(z, guardLogins.Where(y => y.LoginDate.ToString("yyyyMMdd") == z.Date.ToString("yyyyMMdd")), guardCompliances)).ToList();
        }
        //To  get the details of Gurad in 3rd page of report start
        public List<GuardLogin> GetKpiGuardDetailsData(int clientSiteId, DateTime fromDate, DateTime toDate)
        {
           
            var guardLogins = _guardDataProvider.GetGuardLogins(clientSiteId, fromDate, toDate).ToList();
            
            return guardLogins.ToList();
        }
        public List<GuardCompliance> GetKpiGuardDetailsComplianceData(int[] guardIds)
        {
            
                var guardCompliance = _guardDataProvider.GetGuardCompliancesList(guardIds).ToList();
                
            return guardCompliance.ToList();
        }
        public List<GuardCompliance> GetKpiGuardDetailsCompliance(int guardIds)
        {

            var guardCompliance = _guardDataProvider.GetGuardCompliances(guardIds).ToList();

            return guardCompliance.ToList();
        }
        //To  get the details of Gurad in 3rd page of report stop
        public Dictionary<int, MonthlyKpiResult> GetMonthlyKpiReportData(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
            {
                var dailyClientSiteKpis = _kpiDataProvider.GetDailyClientSiteKpis(clientSiteIds, fromDate, toDate);
                var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds);

                var results = new Dictionary<int, MonthlyKpiResult>();
                foreach (var siteId in clientSiteIds)
                {
                    var clientSiteKpiSetting = clientSiteKpiSettings.SingleOrDefault(z => z.ClientSiteId == siteId);
                    var clientSiteKpis = dailyClientSiteKpis.Where(z => z.ClientSiteId == siteId);
                    var dailyKpiResults = clientSiteKpis.Select(x => new DailyKpiResult(x, clientSiteKpiSetting)).ToList();
                    results.Add(siteId, new MonthlyKpiResult(clientSiteKpiSetting, dailyKpiResults));
                }
                return results;
            }

            public List<DailyKpiResult> GetKpiReportData(int[] clientSiteIds, DateTime fromDate, DateTime toDate)
            {
                var dailyClientSiteKpis = _kpiDataProvider.GetDailyClientSiteKpis(clientSiteIds, fromDate, toDate);
                var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds);

                var dailyKpis = new List<DailyKpiResult>();
                foreach (var clientSiteId in clientSiteIds)
                {
                    var kpiSetting = clientSiteKpiSettings.SingleOrDefault(z => z.ClientSite.Id == clientSiteId);
                    dailyKpis.AddRange(dailyClientSiteKpis.Where(z => z.ClientSiteId == clientSiteId)
                                                            .Select(x => new DailyKpiResult(x, kpiSetting)));
                }

                return dailyKpis;
            }
        }
    }