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
                    dailyGuardLogs.Add(new GuardLogViewModel(patrolCarLogs.OrderBy(x=> x.ClientSitePatrolCar.Model)));
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
        
    }
}
