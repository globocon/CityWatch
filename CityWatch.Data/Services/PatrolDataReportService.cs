using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
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

        public PatrolDataReport GetDailyPatrolData(PatrolRequest patrolRequest)
        {
            var incidentReports = _irDataProvider.GetIncidentReports(patrolRequest.FromDate, patrolRequest.ToDate)
                .Where(z => patrolRequest.DataFilter == PatrolDataFilter.All ||
                            (patrolRequest.DataFilter == PatrolDataFilter.PatrolOnly && z.IsPatrol) ||
                            (patrolRequest.DataFilter == PatrolDataFilter.Custom &&
                                (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                                (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name)) &&
                                (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&
                                // New Code Added for ColourCode filter
                                (patrolRequest.ColourCode ==0 || z.ColourCode == patrolRequest.ColourCode) &&
                                // New Code Added for Serial number
                                (patrolRequest.SerialNo == null || z.SerialNo == patrolRequest.SerialNo)

                                ));
            var clientSites = _clientDataProvider.GetClientSites(null);
            //var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == FeedbackType.ColourCodes);

            //To get the feedback id for Colour Codes -start
            var feedbackTypes= _configDataProvider.GetFeedbackTypes().Where(x => x.Name == "Colour Codes").Select(x=> x.Id).FirstOrDefault();
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == feedbackTypes);
            //To get the feedback id for Colour Codes -end

            return new PatrolDataReport(patrolRequest.ClientSites, incidentReports.Select(x => new DailyPatrolData(x, clientSites, _configDataProvider)), feedbackTemplates);
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

                                   ) ||
                                    (patrolRequest.DataFilter == PatrolDataFilter.DocketOnly &&
                               (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                               (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name)) &&
                               (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&
                               // New Code Added for ColourCode filter
                               (patrolRequest.ColourCode == 0 || z.ColourCode == patrolRequest.ColourCode)
                                

                                   )
                                   );
                if(patrolRequest.DataFilter == PatrolDataFilter.DocketOnly)
                {
                    var plateIds = _irDataProvider.GetKeyVehicleLogWithDocket(patrolRequest.FromDate, patrolRequest.ToDate).Where(v => v.DocketSerialNo != null).Select(z =>

                        z.PlateId   // or z.TruckNo, depending on your property name
                    ).ToArray();
                    var incidentreportIds = _irDataProvider.GetIncidentReportsPlatesLoadedWithPlateIds(plateIds).Select(z => z.IncidentReportId).ToArray() ;
                    incidentReports = incidentReports.Where(x => incidentreportIds.Contains(x.Id));
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
