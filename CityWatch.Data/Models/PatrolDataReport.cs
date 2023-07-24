using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Models
{
    public class PatrolDataReport
    {
        private readonly string[] _requestedClientSites;
        private readonly IEnumerable<DailyPatrolData> _dailyPatrolData;
        private readonly float _totalRecords;
        private readonly Dictionary<IrEventType, double> _eventTypes;
        private readonly float _totalEventTypes;
        private readonly IEnumerable<FeedbackTemplate> _feedbackTemplate;

        public PatrolDataReport(string[] RequestedClientSites, IEnumerable<DailyPatrolData> dailyPatrolData, IEnumerable<FeedbackTemplate> feedbackTemplate)
        {
            _requestedClientSites = RequestedClientSites;
            _dailyPatrolData = dailyPatrolData;
            _feedbackTemplate = feedbackTemplate;

            _totalRecords = _dailyPatrolData.Count();
            var eventTypesInResult = dailyPatrolData.SelectMany(n => n.IncidentReportEventTypes);
            _eventTypes = eventTypesInResult.GroupBy(z => z.EventType).ToDictionary(z => z.Key, z => (double)z.Count());
            _totalEventTypes = eventTypesInResult.Count();
        }

        public List<DailyPatrolData> Results
        {
            get { return _dailyPatrolData.ToList(); }
        }

        public float ResultsCount
        {
            get { return _totalRecords; }
        }

        public Dictionary<string, double> SitePercentage
        {
            get
            {
                var sitePercentageResult = _dailyPatrolData.GroupBy(n => n.SiteName)
                    .ToDictionary(z => string.IsNullOrEmpty(z.Key) ? "N/A" : z.Key, z => Math.Round(z.Count() / _totalRecords * 100, 1));
                var clientSites = _requestedClientSites ?? sitePercentageResult.Select(z => z.Key);
                return clientSites.ToDictionary(z => z, z => sitePercentageResult.ContainsKey(z) ? sitePercentageResult[z] : 0);
            }
        }

        public Dictionary<string, double> AreaWardPercentage
        {
            get
            {
                return _dailyPatrolData.GroupBy(n => n.ClientArea)
                   .ToDictionary(z => string.IsNullOrEmpty(z.Key) ? "N/A" : z.Key, z => Math.Round(z.Count() / _totalRecords * 100, 1));
            }
        }

        public Dictionary<string, double> EventTypePercentage
        {
            get
            {
                return _eventTypes.ToDictionary(z => $"{(int)z.Key:D2} {z.Key.ToDescription()}", z => Math.Round(z.Value / _totalEventTypes * 100, 1));
            }
        }

        public Dictionary<string, double> EventTypeQuantity
        {
            get
            {
                return _eventTypes.ToDictionary(z => ((int)z.Key).ToString("D2"), z => z.Value);
            }
        }

        public Dictionary<string, double> ColorCodePercentage
        {
            get
            {
                return _dailyPatrolData.GroupBy(n => n.ColorCode)
                .ToDictionary(z => z.Key.HasValue ? _feedbackTemplate.SingleOrDefault(x => x.Id == z.Key)?.Name : "N/A", 
                                    z => Math.Round(z.Count() / _totalRecords * 100, 1));
            }
        }
    }
}
