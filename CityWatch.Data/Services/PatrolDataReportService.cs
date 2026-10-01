using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using iText.Kernel.Pdf.Annot;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace CityWatch.Data.Services
{
    public interface IPatrolDataReportService
    {
        PatrolDataReport GetDailyPatrolData(PatrolRequest patrolRequest);
        List<ClientSiteRadioChecksActivityStatus_History> GetAuditGuardFusionLogs(PatrolRequest patrolRequest, DateTime FromDate, DateTime ToDate);
        List<ClientSiteRadioCheck> GetClientSiteRadioChecks(int clientsiteid, DateTime FromDate, DateTime ToDate);
        PatrolDataReport GetDailyPatrolDataNew(PatrolRequest patrolRequest);
    }

    public class PatrolDataReportService : IPatrolDataReportService
    {
        private readonly IIrDataProvider _irDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IConfiguration _configuration;

        public PatrolDataReportService(IClientDataProvider clientDataProvider, IIrDataProvider irDataProvider, IConfigDataProvider configDataProvider,IGuardLogDataProvider guardLogDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _irDataProvider = irDataProvider;
            _configDataProvider = configDataProvider;
            _guardLogDataProvider = guardLogDataProvider;


        }

        //public PatrolDataReport GetDailyPatrolData(PatrolRequest patrolRequest)
        //{
        //    var incidentReports = _irDataProvider.GetIncidentReports(patrolRequest.FromDate, patrolRequest.ToDate)
        //        .Where(z => patrolRequest.DataFilter == PatrolDataFilter.All ||
        //                    (patrolRequest.DataFilter == PatrolDataFilter.PatrolOnly && z.IsPatrol) ||
        //                    (patrolRequest.DataFilter == PatrolDataFilter.Custom &&
        //                        (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
        //                        (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name)) &&
        //                        (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&
        //                        // New Code Added for ColourCode filter
        //                        (patrolRequest.ColourCode ==0 || z.ColourCode == patrolRequest.ColourCode) &&
        //                        // New Code Added for Serial number
        //                        (patrolRequest.SerialNo == null || z.SerialNo == patrolRequest.SerialNo)

        //                        ));
        //    var clientSites = _clientDataProvider.GetClientSites(null);
        //    //var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == FeedbackType.ColourCodes);

        //    //To get the feedback id for Colour Codes -start
        //    var feedbackTypes= _configDataProvider.GetFeedbackTypes().Where(x => x.Name == "Colour Codes").Select(x=> x.Id).FirstOrDefault();
        //    var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == feedbackTypes);
        //    //To get the feedback id for Colour Codes -end

        //    return new PatrolDataReport(patrolRequest.ClientSites, incidentReports.Select(x => new DailyPatrolData(x, clientSites, _configDataProvider)), feedbackTemplates);
        //}

        public PatrolDataReport GetDailyPatrolData(PatrolRequest patrolRequest)
        {
            // ✅ Ensure patrolRequest is not null
            if (patrolRequest == null)
                throw new ArgumentNullException(nameof(patrolRequest));

            // ✅ Get reports safely (avoid null collection)
            var reports = _irDataProvider
                .GetIncidentReports(patrolRequest.FromDate, patrolRequest.ToDate)
                ?? new List<IncidentReport>();

            // ✅ Apply filtering safely
            var incidentReports = reports.Where(z =>
                z != null && (
                    patrolRequest.DataFilter == PatrolDataFilter.All ||

                    (patrolRequest.DataFilter == PatrolDataFilter.PatrolOnly && z.IsPatrol) ||

                    (patrolRequest.DataFilter == PatrolDataFilter.Custom &&

                        // Client Type filter
                        (patrolRequest.ClientTypes == null ||
                            (z.ClientSiteId.HasValue &&
                             z.ClientSite?.ClientType?.Name != null &&
                             patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name))) &&

                        // Client Site filter
                        (patrolRequest.ClientSites == null ||
                            (z.ClientSiteId.HasValue &&
                             z.ClientSite?.Name != null &&
                             patrolRequest.ClientSites.Contains(z.ClientSite.Name))) &&

                        // Position filter
                        (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&

                        // ColourCode filter
                        (patrolRequest.ColourCode == 0 || z.ColourCode == patrolRequest.ColourCode) &&

                        // SerialNo filter
                        (patrolRequest.SerialNo == null || z.SerialNo == patrolRequest.SerialNo)
                    )
                )
            ).ToList(); // ✅ Execute query here (better debugging)

            // ✅ Get client sites safely
            var clientSites = _clientDataProvider.GetClientSites(null) ?? new List<ClientSite>();

            // ✅ Get feedback type id safely
            var feedbackTypeId = _configDataProvider
                .GetFeedbackTypes()?
                .FirstOrDefault(x => x.Name == "Colour Codes")?.Id ?? 0;

            // ✅ Get feedback templates safely
            var feedbackTemplates = _configDataProvider
                .GetFeedbackTemplates()?
                .Where(x => x.Type == feedbackTypeId)
                .ToList() ?? new List<FeedbackTemplate>();

            // ✅ Final result
            return new PatrolDataReport(
                patrolRequest.ClientSites,
                incidentReports.Select(x => new DailyPatrolData(x, clientSites, _configDataProvider)),
                feedbackTemplates
            );
        }
        public List<ClientSiteRadioChecksActivityStatus_History> GetAuditGuardFusionLogs(PatrolRequest patrolRequest, DateTime FromDate, DateTime ToDate)
        {
            
            var dailyGuardLogGroups = _guardLogDataProvider.GetGuardFusionLogsWithToDate(FromDate,ToDate).Where(z => (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                                (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name))
                               // && (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ") )
                                );
            


            
                return dailyGuardLogGroups.ToList();
                

        }

        public List<ClientSiteRadioCheck> GetClientSiteRadioChecks(int clientsiteid , DateTime FromDate, DateTime ToDate)
        {

            var dailyGuardLogGroups = _guardLogDataProvider.GetClientSiteRadioChecksWithDate(FromDate, ToDate).Where(z=>
                z.ClientSiteId==clientsiteid 
                                );




            return dailyGuardLogGroups.ToList();


        }
        public PatrolDataReport GetDailyPatrolDataNew(PatrolRequest patrolRequest)
        {
           
            IEnumerable<IncidentReport> incidentReports;
            //p3-42--Dockets-start
            IEnumerable<KeyVehicleLogDocketHistory> docketHistories;
            IEnumerable<IncidentReportsPlatesLoaded> incidentReportsPlatesLoaded;
            IEnumerable<KeyVehicleLog> keyVehicleLogs;
            int[] irIdsFromPlates = null;
            //if (patrolRequest.DataFilter == PatrolDataFilter.DocketOnly)
            //{
            //    docketHistories = _irDataProvider.GetKeyVehicleLogsWithDockets(patrolRequest.FromDate, patrolRequest.ToDate).ToList();
            //    int[] keyVehicleLogIds = docketHistories.Select(x => x.KeyVehicleLogId).Distinct().ToArray();
            //    keyVehicleLogs = _irDataProvider.GetKeyVehicleLogByIds(keyVehicleLogIds);
            //    incidentReportsPlatesLoaded = _irDataProvider.GetIncidentReportsPlates().Where(x => keyVehicleLogs.Select(z => z.PlateId).ToArray().Contains(x.PlateId) && keyVehicleLogs.Select(z => z.VehicleRego).ToArray().Contains(x.TruckNo)).ToList();
            //    irIdsFromPlates = incidentReportsPlatesLoaded.Select(x => x.IncidentReportId).Distinct().ToArray();
            //}
            //p3-42--Dockets-end

            if (patrolRequest.SerialNo == null)
            {
                incidentReports = _irDataProvider.GetIncidentReports(patrolRequest.FromDate, patrolRequest.ToDate)
               .Where(z => patrolRequest.DataFilter == PatrolDataFilter.All ||
                           (patrolRequest.DataFilter == PatrolDataFilter.PatrolOnly && z.IsPatrol) ||
                           (patrolRequest.DataFilter == PatrolDataFilter.Custom &&
                               (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                               (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name)) &&
                               (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&
                               // New Code Added for ColourCode filter
                               (patrolRequest.ColourCode == 0 || z.ColourCode == patrolRequest.ColourCode)
                                   // &&
                                   // New Code Added for Serial number
                                   //(patrolRequest.SerialNo == null || z.SerialNo == patrolRequest.SerialNo)

                                   )
                                   //p3-42--Dockets-start
                                   ||
                            (patrolRequest.DataFilter == PatrolDataFilter.DocketOnly && 
                            //irIdsFromPlates.Contains(z.Id) &&
                        (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                        (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name)) &&
                        (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&
                        // New Code Added for ColourCode filter
                        (patrolRequest.ColourCode == 0 || z.ColourCode == patrolRequest.ColourCode)
                            // &&
                            // New Code Added for Serial number
                            //(patrolRequest.SerialNo == null || z.SerialNo == patrolRequest.SerialNo)

                            )

                                   //p3-42--Dockets-end

                                   );
                if (patrolRequest.DataFilter == PatrolDataFilter.DocketOnly)
                {
                    docketHistories = _irDataProvider.GetKeyVehicleLogsWithDocketsWithoutDate().ToList();
                   
                    int[] keyVehicleLogIds = docketHistories.Select(x => x.KeyVehicleLogId).Distinct().ToArray();
                    keyVehicleLogs = _irDataProvider.GetKeyVehicleLogByIds(keyVehicleLogIds);
                    var plateIds = keyVehicleLogs.Select(z => z.PlateId).ToHashSet();
                    var vehicleRegos = keyVehicleLogs.Select(z => z.VehicleRego).ToHashSet();

                    incidentReportsPlatesLoaded =
                        _irDataProvider.GetIncidentReportsPlates()
                        .Where(x => plateIds.Contains(x.PlateId) && vehicleRegos.Contains(x.TruckNo))
                        .ToList();
                    irIdsFromPlates = incidentReportsPlatesLoaded.Select(x => x.IncidentReportId).Distinct().ToArray();

                    if (irIdsFromPlates != null && irIdsFromPlates.Length > 0)
                    {
                        incidentReports = incidentReports.Where(x => irIdsFromPlates.Contains(x.Id));
                    }
                    else
                    {
                        incidentReports = Enumerable.Empty<IncidentReport>();
                    }
                }
            }
            else
            {
                incidentReports = _irDataProvider.GetIncidentReports()
                .Where(z => z.SerialNo == patrolRequest.SerialNo);
            }
            var clientSites = _clientDataProvider.GetClientSites(null);
            //var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == FeedbackType.ColourCodes);

            //To get the feedback id for Colour Codes -start
            var feedbackTypes = _configDataProvider.GetFeedbackTypes().Where(x => x.Name == "Colour Codes").Select(x => x.Id).FirstOrDefault();
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == feedbackTypes);
            //To get the feedback id for Colour Codes -end

            return new PatrolDataReport(patrolRequest.ClientSites, incidentReports.Select(x => new DailyPatrolData(x, clientSites, _configDataProvider)), feedbackTemplates);
        }

    }
}
