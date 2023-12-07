using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Models
{
    public class DailyPatrolData
    {
        private readonly IncidentReport _incidentReport;
        private readonly List<ClientSite> _clientSites;

        public DailyPatrolData(IncidentReport incidentReport, List<ClientSite> clientSites)
        {
            _incidentReport = incidentReport;
            _clientSites = clientSites;
        }

        public string NameOfDay
        {
            get
            {
                return _incidentReport.ReportDateTime.HasValue ?
                    _incidentReport.ReportDateTime.Value.DayOfWeek.ToString() :
                    string.Empty;
            }
        }

        public string Date
        {
            get
            {
                return _incidentReport.ReportDateTime.HasValue ?
                    _incidentReport.ReportDateTime.Value.ToString("dd MMM yyyy") :
                    string.Empty;
            }
        }

        public string ControlRoomJobNo
        {
            get
            {
                return _incidentReport.JobNumber;
            }
        }

        public string SiteName
        {
            get
            {
                var siteName = string.Empty;
                if (_incidentReport.ClientSiteId.HasValue)
                    siteName = _clientSites.SingleOrDefault(x => x.Id == _incidentReport.ClientSiteId.Value)?.Name;
                return siteName;
            }
        }

        public string SiteAddress
        {
            get
            {
                var address = string.Empty;
                if (_incidentReport.ClientSiteId.HasValue)
                    address = _clientSites.SingleOrDefault(x => x.Id == _incidentReport.ClientSiteId.Value)?.Address;
                return address;
            }
        }

        public string DespatchTime
        {
            get
            {
                return _incidentReport.JobTime ?? "n/a";
            }
        }

        public string ArrivalTime
        {
            get
            {
                return _incidentReport.IncidentDateTime.HasValue ?
                    _incidentReport.IncidentDateTime.Value.ToString("HH:mm") :
                    string.Empty;
            }
        }

        public string DepartureTime
        {
            get
            {
                return _incidentReport.ReportDateTime.HasValue ?
                    _incidentReport.ReportDateTime.Value.ToString("HH:mm") :
                    string.Empty;
            }
        }

        public string SerialNo
        {
            get
            {
                return _incidentReport.SerialNo;
            }
        }

        public string TotalMinsOnsite
        {
            get
            {
                return _incidentReport.IncidentDateTime.HasValue && _incidentReport.ReportDateTime.HasValue ?
                    (_incidentReport.ReportDateTime.Value - _incidentReport.IncidentDateTime.Value).TotalMinutes.ToString() :
                    string.Empty;
            }
        }

        public string ResponseTime
        {
            get
            {
                if (!string.IsNullOrEmpty(_incidentReport.JobTime) && _incidentReport.IncidentDateTime.HasValue)
                {
                    var tsJob = TimeSpan.Parse(_incidentReport.JobTime);
                    var dtJob = new DateTime(_incidentReport.IncidentDateTime.Value.Year,
                        _incidentReport.IncidentDateTime.Value.Month, _incidentReport.IncidentDateTime.Value.Day,
                        tsJob.Hours, tsJob.Minutes, 0);
                    if (dtJob > _incidentReport.IncidentDateTime.Value)
                        dtJob = dtJob.AddDays(-1);

                    return (_incidentReport.IncidentDateTime.Value - dtJob).TotalMinutes.ToString();
                }
                return string.Empty;
            }
        }

        public string Alarm
        {
            get
            {
                var isFireOrAlarm = _incidentReport.IsEventFireOrAlarm ? "Yes" : string.Empty;
                return $"{isFireOrAlarm} {Environment.NewLine}{_incidentReport.ClientArea}";
            }
        }

        public string ClientArea
        {
            get
            {
                return _incidentReport.ClientArea;
            }
        }

        public string PatrolAttented
        {
            get
            {
                return _incidentReport.CallSign;
            }
        }

        public string ActionTaken
        {
            get
            {
                return _incidentReport.ActionTaken;
            }
        }

        public string NotifiedBy
        {
            get
            {
                return _incidentReport.NotifiedBy;
            }
        }

        public string Billing
        {
            get
            {
                return _incidentReport.Billing;
            }
        }

        public ICollection<IncidentReportEventType> IncidentReportEventTypes
        {
            get
            {
                return _incidentReport.IncidentReportEventTypes;
            }
        }

        public int? ColorCode
        {
            get
            {
                return _incidentReport.ColourCode;
            }
        }

        public string fileNametodownload {
            get
            {
                return _incidentReport.FileName;
            }

        }
    }
}
