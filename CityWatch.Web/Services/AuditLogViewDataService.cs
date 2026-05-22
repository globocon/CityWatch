using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using NuGet.Protocol;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace CityWatch.Web.Services
{
    public interface IAuditLogViewDataService
    {
        List<GuardLogViewModel> GetAuditGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);
        List<KeyVehicleLogViewModel> GetKeyVehicleLogs(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest);
        List<KeyVehicleLogViewModel> GetKeyVehicleLogsWithPOI(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest);
        public List<ClientSiteRadioChecksActivityStatus_History> GetAuditGuardFusionLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);
        public List<ClientSiteRadioChecksActivityStatus_History> GetAuditGuardFusionLogs(int[] clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);
        List<WandStrikeAuditLogViewModel> GetWandStrikeAuditLogIncludingSmartWandStrike(WandStrikeAuditLogRequest wsRequest);
        List<WandStrikeAuditLogExcelViewModel> GetWandStrikeAuditLogIncludingSmartWandStrikeAndAllTags(WandStrikeAuditLogRequest wsRequest);
    }

    public class AuditLogViewDataService : IAuditLogViewDataService
    {
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;

        public AuditLogViewDataService(IGuardLogDataProvider guardLogDataProvider, IClientSiteWandDataProvider clientSiteWandDataProvider, IGuardDataProvider guardDataProvider)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardDataProvider = guardDataProvider;
        }

        public List<GuardLogViewModel> GetAuditGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            var dailyGuardLogGroups = _guardLogDataProvider.GetGuardLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs).Where(x => x.WAND_TAG_ENTRY_TYPE == ScanningType.Normal)
                .GroupBy(z => z.ClientSiteLogBookId);
            var patrolCarLogGroups = _guardLogDataProvider.GetPatrolCarLogs(clientSiteId, logFromDate, logToDate);
            var customFieldLogGroups = _guardLogDataProvider.GetCustomFieldLogs(clientSiteId, logFromDate, logToDate);

            var dailyGuardLogs = new List<GuardLogViewModel>();
            foreach (var group in dailyGuardLogGroups)
            {
                //p6-102 add photo-start
                foreach (var guardlog in group)
                {
                    var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes(guardlog.Id);
                    foreach (var guardLogImage in guardlogImages)
                    {
                        if (guardLogImage.IsRearfile == true)
                        {
                            guardlog.Notes = guardlog.Notes + "</br>See attached file <a href =\"" + guardLogImage.ImagePath + "\" target=\"_blank\">" + Path.GetFileName(guardLogImage.ImagePath) + "</a>";
                        }
                        if (guardLogImage.IsTwentyfivePercentfile == true)
                        {
                            guardlog.Notes = guardlog.Notes + "</br> <a href =\"" + guardLogImage.ImagePath + " \" target=\"_blank\"><img src =\"" + guardLogImage.ImagePath + "\"height=\"200px\" width=\"200px\" class=\"mt-2\"/></a>";
                        }
                        else if (guardLogImage.IsVideo == true)
                        {
                            guardlog.Notes +=
                                "</br><video width=\"320\" height=\"240\" controls class=\"mt-2\">" +
                                $"<source src=\"{guardLogImage.ImagePath}\" type=\"video/mp4\">" +
                                "Your browser does not support the video tag." +
                                "</video>";

                            guardlog.NotesNew +=
                                "</br><video width=\"320\" height=\"240\" controls class=\"mt-2\">" +
                                $"<source src=\"{guardLogImage.ImagePath}\" type=\"video/mp4\">" +
                                "Your browser does not support the video tag." +
                                "</video>";
                        }
                    }
                }
                //p6-102 add photo-end
                var patrolCarLogs = patrolCarLogGroups.Where(z => z.ClientSiteLogBookId == group.Key);
                if (patrolCarLogs.Any())
                {
                    dailyGuardLogs.Add(new GuardLogViewModel(patrolCarLogs));
                }
                var customFieldLogs = customFieldLogGroups.Where(z => z.ClientSiteLogBookId == group.Key);
                if (customFieldLogs.Any())
                {
                    dailyGuardLogs.Add(new GuardLogViewModel(customFieldLogs));
                }
                dailyGuardLogs.AddRange(group.Select(z => new GuardLogViewModel(z)));
            }

            return dailyGuardLogs.ToList();
        }

        public List<KeyVehicleLogViewModel> GetKeyVehicleLogs(KeyVehicleLogAuditLogRequest kvlRequest)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            return _guardLogDataProvider.GetKeyVehicleLogs(kvlRequest.ClientSiteIds, kvlRequest.LogFromDate, kvlRequest.LogToDate)
                .Where(z =>
                    (string.IsNullOrEmpty(kvlRequest.VehicleRego) || string.Equals(z.VehicleRego, kvlRequest.VehicleRego, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(kvlRequest.CompanyName) || string.Equals(z.CompanyName, kvlRequest.CompanyName, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(kvlRequest.PersonName) || string.Equals(z.PersonName, kvlRequest.PersonName, StringComparison.OrdinalIgnoreCase)) &&
                    (!kvlRequest.PersonType.HasValue || z.PersonType == kvlRequest.PersonType) &&
                    (!kvlRequest.EntryReason.HasValue || z.EntryReason == kvlRequest.EntryReason) &&
                    (string.IsNullOrEmpty(kvlRequest.Product) || z.Product == kvlRequest.Product) &&
                    (!kvlRequest.TruckConfig.HasValue || z.TruckConfig == kvlRequest.TruckConfig) &&
                    (!kvlRequest.TrailerType.HasValue || z.TrailerType == kvlRequest.TrailerType) &&
                    (!kvlRequest.ClientSitePocId.HasValue || z.ClientSitePocId == kvlRequest.ClientSitePocId) &&
                    (!kvlRequest.ClientSiteLocationId.HasValue || z.ClientSiteLocationId == kvlRequest.ClientSiteLocationId) &&
                    (string.IsNullOrEmpty(kvlRequest.KeyNo) || (!string.IsNullOrEmpty(z.KeyNo) && z.KeyNo.Contains(kvlRequest.KeyNo)))
                     && (string.IsNullOrEmpty(kvlRequest.KeyVehicleDownselect) || (!string.IsNullOrEmpty(z.Notes) && z.Notes.Contains(kvlRequest.KeyVehicleDownselect, StringComparison.OrdinalIgnoreCase))))
                .Select(z => new KeyVehicleLogViewModel(z, kvlFields))
                .ToList();
        }
        public List<KeyVehicleLogViewModel> GetKeyVehicleLogsWithPOI(KeyVehicleLogAuditLogRequest kvlRequest)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            return _guardLogDataProvider.GetKeyVehicleLogs(kvlRequest.ClientSiteIds, kvlRequest.LogFromDate, kvlRequest.LogToDate)
                .Where(z =>
                    (string.IsNullOrEmpty(kvlRequest.VehicleRego) || string.Equals(z.VehicleRego, kvlRequest.VehicleRego, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(kvlRequest.CompanyName) || string.Equals(z.CompanyName, kvlRequest.CompanyName, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(kvlRequest.PersonName) || string.Equals(z.PersonName, kvlRequest.PersonName, StringComparison.OrdinalIgnoreCase)) &&
                    (!kvlRequest.PersonType.HasValue || z.PersonType == kvlRequest.PersonType) &&
                    (!kvlRequest.EntryReason.HasValue || z.EntryReason == kvlRequest.EntryReason) &&
                    (string.IsNullOrEmpty(kvlRequest.Product) || z.Product == kvlRequest.Product) &&
                    (!kvlRequest.TruckConfig.HasValue || z.TruckConfig == kvlRequest.TruckConfig) &&
                    (!kvlRequest.TrailerType.HasValue || z.TrailerType == kvlRequest.TrailerType) &&
                    (string.IsNullOrEmpty(kvlRequest.ClientSitePocIdNew) || kvlRequest.ClientSitePocIds.Contains(Convert.ToInt16(z.ClientSitePocId))) &&
                    (string.IsNullOrEmpty(kvlRequest.ClientSiteLocationIdNew) || kvlRequest.ClientSiteLocationIds.Contains(Convert.ToInt16(z.ClientSiteLocationId))) &&
                    (string.IsNullOrEmpty(kvlRequest.PersonOfInterest) || kvlRequest.PersonOfInterestIds.Contains(Convert.ToInt16(z.PersonOfInterest))) &&
                    (string.IsNullOrEmpty(kvlRequest.KeyNo) || (!string.IsNullOrEmpty(z.KeyNo) && z.KeyNo.Contains(kvlRequest.KeyNo)))
                    && (string.IsNullOrEmpty(kvlRequest.KeyVehicleDownselect) || (!string.IsNullOrEmpty(z.Notes) && z.Notes.Contains(kvlRequest.KeyVehicleDownselect, StringComparison.OrdinalIgnoreCase)))
                    )
                .Select(z => new KeyVehicleLogViewModel(z, kvlFields))
                .ToList();
        }


        public List<ClientSiteRadioChecksActivityStatus_History> GetAuditGuardFusionLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            var dailyGuardLogGroups = _guardLogDataProvider.GetGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs);
            return dailyGuardLogGroups.ToList();
        }

        public List<ClientSiteRadioChecksActivityStatus_History> GetAuditGuardFusionLogs(int[] clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            var dailyGuardLogGroups = _guardLogDataProvider.GetGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs);
            return dailyGuardLogGroups.ToList();
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
                    item.LabelDescription = smartwandtags?.FirstOrDefault(z => z.UId == item.TagUId)?.LabelDescription ?? item.LabelDescription;
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
