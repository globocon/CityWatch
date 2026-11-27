using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CityWatch.Data.Services
{
    public interface IWandStrikeReportDataService
    {
        List<WandStrikeAuditLogViewModel> GetWandStrikeAuditLogIncludingSmartWandStrike(WandStrikeAuditLogRequest wsRequest);
        List<WandStrikeAuditLogExcelViewModel> GetWandStrikeAuditLogIncludingSmartWandStrikeAndAllTags(WandStrikeAuditLogRequest wsRequest);
    }

    public class WandStrikeReportDataService : IWandStrikeReportDataService
    {
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;

        public WandStrikeReportDataService(IGuardLogDataProvider guardLogDataProvider, IClientSiteWandDataProvider clientSiteWandDataProvider, IGuardDataProvider guardDataProvider)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardDataProvider = guardDataProvider;
        }

        public List<WandStrikeAuditLogViewModel> GetWandStrikeAuditLogIncludingSmartWandStrike(WandStrikeAuditLogRequest wsRequest)
        {
            var filterLogs = GetWandStrikeAuditLogDataIncludingSmartWandStrike(wsRequest);
            var filteredLogs = filterLogs.Select(z => new WandStrikeAuditLogViewModel()
            {
                clientSiteSmartWandTagsHitLog = z
            }).ToList();

            return filteredLogs;
        }
        public List<WandStrikeAuditLogExcelViewModel> GetWandStrikeAuditLogIncludingSmartWandStrikeAndAllTags(WandStrikeAuditLogRequest wsRequest)
        {
            var filterLogs = GetWandStrikeAuditLogDataIncludingSmartWandStrike(wsRequest);

            // First convert logs to ViewModel
            List<ClientSiteSmartWandTagsHitLogViewModel> logViewModels = filterLogs
                .Select(z => new ClientSiteSmartWandTagsHitLogViewModel(z))
                .ToList();

            List<ClientSiteSmartWandTags> smartwandtags = _clientSiteWandDataProvider.GetAllSmartwandTags();
            smartwandtags = smartwandtags.Where(x => wsRequest.ClientSiteIds.Contains(x.ClientSiteId) && x.IsDeleted == false).ToList();

            // Existing ClientSiteId + TagUid combinations
            var existingKeys = logViewModels
                .Select(x => $"{x.LoggedInClientSiteId}_{x.TagUId}")
                .ToHashSet();

            // Missing tags
            var missingTags = smartwandtags
                .Where(tag => !existingKeys.Contains($"{tag.ClientSiteId}_{tag.UId}"))
                .ToList();

            // Convert missing tags into ViewModel
            var missingLogViewModels = missingTags
                .Select(tag => new ClientSiteSmartWandTagsHitLogViewModel
                {
                    LoggedInClientSiteId = tag.ClientSiteId,
                    TagUId = tag.UId,
                    TagsTypeId = tag.TagsTypeId,
                    LabelDescription = tag.LabelDescription,

                    LoggedInClientSite = tag.ClientSite,
                    SmartWandTagsType = tag.SmartWandTagsType,

                    // Optional defaults
                    HitUtcDateTime = null,
                    HitLocalDateTime = null,
                    LoggedInGuard = null,
                    LoggedInUser = null
                })
                .ToList();

            // Add missing tags
            logViewModels.AddRange(missingLogViewModels);

            // Final conversion
            List<WandStrikeAuditLogExcelViewModel> filteredLogs = logViewModels
                .Select(z => new WandStrikeAuditLogExcelViewModel
                {
                    clientSiteSmartWandTagsHitLog = z
                })
                .ToList();

            return filteredLogs;
        }
        public List<ClientSiteSmartWandTagsHitLog> GetWandStrikeAuditLogDataIncludingSmartWandStrike(WandStrikeAuditLogRequest wsRequest)
        {

            List<ClientSiteSmartWandTagsHitLog> strikeLogs = new List<ClientSiteSmartWandTagsHitLog>();
            List<ClientSiteSmartWandTagsHitLog> filterLogs = new List<ClientSiteSmartWandTagsHitLog>();
            List<ClientSiteSmartWand> smartWands = new List<ClientSiteSmartWand>();
            List<IncidentReportPosition> patrolCars = new List<IncidentReportPosition>();
            List<ClientSiteSmartWandTags> smartwandtags = new List<ClientSiteSmartWandTags>();

            List<Guard> guards = new List<Guard>();
            List<ClientSite> clientSites = new List<ClientSite>();

            guards = _guardDataProvider.GetGuards();
            int? Id = null;
            clientSites = _guardLogDataProvider.GetClientSites(Id);

            strikeLogs = _clientSiteWandDataProvider.GetClientSiteSmartWandTagsHitLogs(wsRequest.ClientSiteIds, wsRequest.LogFromDate, wsRequest.LogToDate);

            if (!wsRequest.IspatrolCarToggleOn && strikeLogs.Any())
            {
                strikeLogs = strikeLogs.Where(x => x.LoggedInClientSite.PatrolTourMode == PatrolTouringMode.STND).ToList();
            }

            if (strikeLogs.Any())
            {
                smartWands = _clientSiteWandDataProvider.GetClientSiteSmartWands();
                //patrolCars = _clientSiteWandDataProvider.GetPatrolCarsForSite(wsRequest.ClientSiteIds);
                patrolCars = _clientSiteWandDataProvider.GetPatrolCars();
                smartwandtags = _clientSiteWandDataProvider.GetAllSmartwandTags();

                foreach (var item in strikeLogs)
                {
                    var LabelNotDeleted = smartwandtags?.OrderByDescending(x => x.Id)?.FirstOrDefault(z => z.UId == item.TagUId && z.IsDeleted == false)?.LabelDescription ?? null;
                    var LabelDeleted = smartwandtags?.OrderByDescending(x => x.Id)?.FirstOrDefault(z => z.UId == item.TagUId && z.IsDeleted == true)?.LabelDescription ?? null;
                    item.LabelDescription = LabelNotDeleted ?? LabelDeleted ?? item.LabelDescription;
                    item.LoggedInUser.Password = null; // Hide user password

                    if (item.SmartWandId.HasValue && item.SmartWandId.Value > 0)
                    {
                        var smartWand = smartWands.FirstOrDefault(z => z.Id == item.SmartWandId.Value);
                        if (smartWand != null)
                        {
                            item.SmartWandNameId = smartWand.SmartWandId;
                            item.PatrolCarId = smartWand.PatrolCarId;
                            item.PatrolCarName = patrolCars?.FirstOrDefault(z => z.Id == smartWand.PatrolCarId)?.Name;
                            //item.GPScoordinates = _guardLogDataProvider.GetTagScanGpsFromLogBook(item.Id);                            
                        }
                    }
                }

                filterLogs = strikeLogs.Where(z =>
                   (string.IsNullOrEmpty(wsRequest.TagId) || wsRequest.TagIds.Contains(z.TagUId)) &&
                   (string.IsNullOrEmpty(wsRequest.TagTypeId) || wsRequest.TagTypeIds.Contains(Convert.ToInt16(z.TagsTypeId))) &&
                   (string.IsNullOrEmpty(wsRequest.TagLabel) || wsRequest.TagLabelIds.Contains(z.LabelDescription)) &&
                   (string.IsNullOrEmpty(wsRequest.SmartWandId) || wsRequest.SmartWandIds.Contains(z.SmartWandNameId)) &&
                   (string.IsNullOrEmpty(wsRequest.PatrolCarId) || wsRequest.PatrolCarIds.Contains(Convert.ToInt16(z.PatrolCarId))) &&
                   (string.IsNullOrEmpty(wsRequest.GuardName) || z.LoggedInGuard.Name.Contains(wsRequest.GuardName, StringComparison.OrdinalIgnoreCase)) &&
                   (string.IsNullOrEmpty(wsRequest.GuardLicenceNoId) || string.Equals(z.LoggedInGuard.SecurityNo, wsRequest.GuardLicenceNoId, StringComparison.OrdinalIgnoreCase))
               ).ToList();

                if (wsRequest.IspatrolCarToggleOn && wsRequest.PatrolCarIds.Count() > 0)
                {
                    filterLogs = filterLogs.Where(x => x.LoggedInClientSite.PatrolTourMode != PatrolTouringMode.STND).ToList();
                }
            }

            return filterLogs;

        }

    }
}
