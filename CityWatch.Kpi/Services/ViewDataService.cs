using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Services
{
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
    }

    public class ViewDataService : IViewDataService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IKpiDataProvider _kpiDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;

        public ViewDataService(IClientDataProvider clientDataProvider,
            IKpiDataProvider kpiDataProvider,
            IGuardDataProvider guardDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _kpiDataProvider = kpiDataProvider;
            _guardDataProvider = guardDataProvider;
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

        public List<SelectListItem> GetClientSites(string type = "")
        {
            var sites = new List<SelectListItem>();
            var mapping = _clientDataProvider.GetClientSites(null).Where(x => x.ClientType.Name == type);
            foreach (var item in mapping)
            {
                sites.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }
            return sites;
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
            return dailyClientSiteKpis.Select(z => new DailyKpiGuard(z, guardLogins.Where(y => y.OnDuty.ToString("yyyyMMdd") == z.Date.ToString("yyyyMMdd")))).ToList();            
        }

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