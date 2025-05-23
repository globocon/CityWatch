﻿using CityWatch.Data;
using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Models;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static Dropbox.Api.Files.WriteMode;


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

        List<SelectListItem> GetOfficerPositions(OfficerPositionFilterManning positionFilter = OfficerPositionFilterManning.SecurityOnly);
        List<SelectListItem> ClientTypesUsingLoginUserId(int guardId);
        List<SelectListItem> GetClientSitesUsingLoginUserId(int userId, string type = "");
        List<GuardLogin> GetKpiGuardDetailsData(int clientSiteId, DateTime fromDate, DateTime toDate);
        List<GuardCompliance> GetKpiGuardDetailsCompliance(int guardId);
        List<GuardCompliance> GetKpiGuardDetailsComplianceData(int[] guardIds);
        int GetClientTypeCount(int? typeId);
        List<SelectListItem> ClientTypesUsingLoginUserIdCount(int guardId);
        List<GuardComplianceAndLicense> GetKpiGuardDetailsComplianceAndLicense(int guardIds);
        List<GuardComplianceAndLicense> GetKpiGuardDetailsComplianceAndLicenseHR(int guardIds, HrGroup hrGroup);
        List<GuardComplianceAndLicense> GetKpiGuardDetailsComplianceAndLicenseHRList(string hrGroup);
        List<HrSettings> GetHRSettings(int HRID);
        List<HRGroups> GetKpiGuardHRGroup();
        List<ClientType> GetUserClientTypesHavingAccess(int? userId);
        List<HRGroups> GetHRGroupslist();
        List<SelectListItem> ProviderList();
        List<SelectListItem> CriticalGroupNameList();
        List<HrSettings> GetHRSettingsCriticalDoc(int HRID,int CriticalDocumentID);

        public List<SelectListItem> ClientTypesUsingLoginMainUserId(int userId);

        public List<SelectListItem> GetClientSitesUsingLoginUserIdNew(int userId, string type = "");
        public List<SelectListItem> ClientTypesUsingLoginMainUserIdWithClientTypeId(int userId, int ClientTypeId);
        public string ClientSitesUsingId(int ClientSiteId);
        public List<SelectListItem> KPITelematicsList();
        public KPITelematicsField GetMobileNo(int Id);
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
        public List<SelectListItem> CriticalGroupNameList()
        {
            var GroupName = _context.CriticalDocuments.Where(x => x.IsCriticalDocumentDownselect == true).ToList();
            var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            foreach (var item in GroupName)
            {
                if (!items.Any(cus => cus.Text == item.GroupName))
                {

                    items.Add(new SelectListItem($"{item.GroupName}", item.Id.ToString()));
                }
            }

            return items;

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



        public List<SelectListItem> ClientTypesUsingLoginMainUserId(int userId)
        {
            if (userId == 0)
            {
                var clientTypes = _clientDataProvider.GetClientTypes();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in clientTypes)
                {
                    var countClientType = GetClientTypeCount(item.Id);
                    items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name));
                    //items.Add(new SelectListItem(item.Name, item.Name));
                }

                return items;
            }
            else
            {

                var allUserAccess = _clientDataProvider.GetUserClientSiteAccess(userId);
                var distinctType= allUserAccess.Select(x=>x.ClientSite.ClientType).Distinct().OrderBy(x=>x.Name);
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in distinctType)
                {
                   
                        var countClientType = GetClientTypeCount(item.Id);
                        items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name));
                        
                    
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



        public List<SelectListItem> GetClientSitesUsingLoginUserIdNew(int userId, string type = "")
        {
            if (userId == 0)
            {
                var sites = new List<SelectListItem>();
                sites = GetClientSites(null)
               .Where(z => (string.IsNullOrEmpty(type)))
                .ToList();
                return sites;

            }
            else
            {
                var sites = new List<SelectListItem>();

                var mapping = _context.UserClientSiteAccess
               .Where(x => x.ClientSite.ClientType.Name.Trim() == type.Trim() && x.ClientSite.IsActive == true && x.UserId==userId)
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
            var guardCompliances = _guardDataProvider.GetAllGuardLicensesAndCompliances();
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
            return dailyClientSiteKpis.Select(z => new DailyKpiGuard(z, guardLogins.Where(y => y.LoginDate.ToString("yyyyMMdd") == z.Date.ToString("yyyyMMdd")), guardCompliances, _guardDataProvider)).ToList();
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
        public List<HRGroups> GetKpiGuardHRGroup()
        {
            var hrGroups = _guardDataProvider.GetHRGroups();
            return hrGroups;



        }
        public List<HrSettings> GetHRSettings(int HRID)
        {

            return _context.HrSettings.Include(z => z.HRGroups)
            .Include(z => z.hrSettingsClientSites)
            .Include(z => z.hrSettingsClientStates)
            .Include(z => z.ReferenceNoNumbers)
            .Include(z => z.ReferenceNoAlphabets)
            .OrderBy(x => x.HRGroups.Name).ThenBy(x => x.ReferenceNoNumbers.Name).
            ThenBy(x => x.ReferenceNoAlphabets.Name).Where(z => z.HRGroups.Id == HRID).ToList();


        }
        public List<HrSettings> GetHRSettingsCriticalDoc(int HRID,int CriticalDocumentID)
        {
            var result = _context.CriticalDocuments.Include(z => z.CriticalDocumentsClientSites)
                .Include(z => z.CriticalDocumentDescriptions)
                .Where(x => x.IsCriticalDocumentDownselect == true && x.Id == CriticalDocumentID).ToList();
            var hrSettings = _context.HrSettings
                .Include(z => z.HRGroups)
                .Include(z => z.hrSettingsClientSites)
                .Include(z => z.hrSettingsClientStates)
                .Include(z => z.ReferenceNoNumbers)
                .Include(z => z.ReferenceNoAlphabets)
                .OrderBy(x => x.HRGroups.Name)
                .ThenBy(x => x.ReferenceNoNumbers.Name)
                .ThenBy(x => x.ReferenceNoAlphabets.Name)
                .Where(z => z.HRGroups.Id == HRID)
                .ToList();
            var matchingHrSettings = new List<HrSettings>();
            foreach (var document in result)
            {
                foreach (var description in document.CriticalDocumentDescriptions)
                {
                    var matchedHrSettings = hrSettings
                 .Where(h => h.Id == description.DescriptionID)
                 .ToList();

                    matchingHrSettings.AddRange(matchedHrSettings);
                }
            }
            return matchingHrSettings;
            

        }

        public List<GuardComplianceAndLicense> GetKpiGuardDetailsComplianceAndLicense(int guardIds)
        {

            var guardCompliance = _guardDataProvider.GetGuardCompliancesAndLicense(guardIds).ToList();

            return guardCompliance.ToList();
        }
        public List<GuardComplianceAndLicense> GetKpiGuardDetailsComplianceAndLicenseHR(int guardIds, HrGroup hrGroup)
        {

            var guardCompliance = _guardDataProvider.GetGuardCompliancesAndLicenseHR(guardIds, hrGroup).ToList();

            return guardCompliance.ToList();
        }
        public List<GuardComplianceAndLicense> GetKpiGuardDetailsComplianceAndLicenseHRList(string hrGroup)
        {

            var guardCompliance = _guardDataProvider.GetGuardCompliancesAndLicenseList(hrGroup).ToList();

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
        public List<ClientType> GetUserClientTypesHavingAccess(int? userId)
        {
            var clientTypes = _clientDataProvider.GetClientTypes();
            if (userId == null)
                return clientTypes;

            var allUserAccess = _clientDataProvider.GetUserClientSiteAccess(userId);
            var clientTypeIds = allUserAccess.Select(x => x.ClientSite.TypeId).Distinct().ToList();
            return clientTypes.Where(x => clientTypeIds.Contains(x.Id)).ToList();
        }
        public List<HRGroups> GetHRGroupslist()
        {
            var HRList = _clientDataProvider.GetHRGroups();
            return HRList;
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
        public List<SelectListItem> ClientTypesUsingLoginMainUserIdWithClientTypeId(int userId,int ClientTypeId)
        {
            if (userId == 0)
            {
                var clientTypesNew = _clientDataProvider.GetClientTypes().Where(z=> z.Id==ClientTypeId).FirstOrDefault().Name;
                var clientTypes = _clientDataProvider.GetClientTypes();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", false) };
                foreach (var item in clientTypes)
                {
                    var countClientType = GetClientTypeCount(item.Id);
                    if (clientTypesNew == item.Name)
                    {
                        items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name, true));
                    }
                    else
                    {
                        items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name, false));
                    }
                    //items.Add(new SelectListItem(item.Name, item.Name));
                }

                return items;
            }
            else
            {

                var allUserAccess = _clientDataProvider.GetUserClientSiteAccess(userId);
                var distinctTypeNew = _clientDataProvider.GetClientTypes().Where(z => z.Id == ClientTypeId).FirstOrDefault().Name;
                var distinctType = allUserAccess.Select(x => x.ClientSite.ClientType).Distinct().OrderBy(x => x.Name);
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", false) };
                foreach (var item in distinctType)
                {

                    var countClientType = GetClientTypeCount(item.Id);
                    if (distinctTypeNew == item.Name)
                    {
                        items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name, true));
                    }
                    else
                    { items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name, false)); }


                }

                return items;

            }

        }
        public string ClientSitesUsingId(int ClientSiteId)
        {
            var distinctType = _clientDataProvider.GetClientSiteDetailsWithId(ClientSiteId).FirstOrDefault().Name;
            return distinctType;
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

        public KPITelematicsField GetMobileNo(int Id)
        {
            return _configDataProvider.GetTelematicsMobileNo(Id);
        }
    }
}