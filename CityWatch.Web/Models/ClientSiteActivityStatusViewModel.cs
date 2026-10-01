using CityWatch.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CityWatch.Web.Models
{
    public class ClientSiteActivityStatusViewModel
    {
        private readonly ClientSiteActivityStatus _clientSiteActivityStatus;

        public ClientSiteActivityStatusViewModel(ClientSiteActivityStatus clientSiteActivityStatus)
        {
            _clientSiteActivityStatus = clientSiteActivityStatus;
        }

        public ClientSiteActivityStatus ActivityStatus { get { return _clientSiteActivityStatus; } }

        public string GuardLicenseNo
        { 
            get 
            {
                return _clientSiteActivityStatus.Guard?.SecurityNo ?? "-";
            } 
        }

        public string RecentActivity
        {
            get
            {
                var message = new StringBuilder();
                var siteIsActive = _clientSiteActivityStatus.Status.GetValueOrDefault();
                var source = _clientSiteActivityStatus.LastActiveSrcId;
                if (siteIsActive && source != null)
                {
                    var eventTime = _clientSiteActivityStatus.LastActiveAt?.ToString("dd MMM yyyy HH:mm");
                    var note = _clientSiteActivityStatus.LastActiveDescription;
                    switch (source)
                    {
                        case LastActivitySource.RadioCheck:
                            message.AppendFormat("RadioCheck at {0}. Status: {1}", eventTime, note);
                            break;
                        case LastActivitySource.DailyGuardLog:
                            message.AppendFormat("LB entry at {0}. Note: {1}", eventTime, note);
                            break;
                    }
                }
                return message.ToString();
            }
        }
    }

    public class ClientSiteActivityStatusViewModelComparer : IComparer<ClientSiteActivityStatusViewModel>
    {
        public int Compare(ClientSiteActivityStatusViewModel x, ClientSiteActivityStatusViewModel y)
        {
            var xValue = x.ActivityStatus.Status.HasValue ? Convert.ToInt32(x.ActivityStatus.Status) : 2;
            var yValue = y.ActivityStatus.Status.HasValue ? Convert.ToInt32(y.ActivityStatus.Status) : 2;

            return xValue.CompareTo(yValue);
        }
    }
}

