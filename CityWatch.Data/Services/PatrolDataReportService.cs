using CityWatch.Data.Enums;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using System;
using System.Linq;

namespace CityWatch.Data.Services
{
    public interface IPatrolDataReportService
    {
        PatrolDataReport GetDailyPatrolData(PatrolRequest patrolRequest);
    }

    public class PatrolDataReportService : IPatrolDataReportService
    {
        private readonly IIrDataProvider _irDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;

        public PatrolDataReportService(IClientDataProvider clientDataProvider, IIrDataProvider irDataProvider, IConfigDataProvider configDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _irDataProvider = irDataProvider;
            _configDataProvider = configDataProvider;
        }

        public PatrolDataReport GetDailyPatrolData(PatrolRequest patrolRequest)
        {
            var incidentReports = _irDataProvider.GetIncidentReports(patrolRequest.FromDate, patrolRequest.ToDate)
                .Where(z => patrolRequest.DataFilter == PatrolDataFilter.All ||
                            (patrolRequest.DataFilter == PatrolDataFilter.PatrolOnly && z.IsPatrol) ||
                            (patrolRequest.DataFilter == PatrolDataFilter.Custom &&
                                (patrolRequest.ClientTypes == null || z.ClientSiteId.HasValue && patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                                (patrolRequest.ClientSites == null || z.ClientSiteId.HasValue && patrolRequest.ClientSites.Contains(z.ClientSite.Name)) &&
                                (patrolRequest.Position == null || z.Position == patrolRequest.Position)));
            var clientSites = _clientDataProvider.GetClientSites(null);
            //var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == FeedbackType.ColourCodes);

            //To get the feedback id for Colour Codes -start
            var feedbackTypes= _configDataProvider.GetFeedbackTypes().Where(x => x.Name == "Colour Codes").Select(x=> x.Id).FirstOrDefault();
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == feedbackTypes);
            //To get the feedback id for Colour Codes -end

            return new PatrolDataReport(patrolRequest.ClientSites, incidentReports.Select(x => new DailyPatrolData(x, clientSites)), feedbackTemplates);
        }
    }
}
