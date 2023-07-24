using CityWatch.Data.Models;
using System;

namespace CityWatch.Web.Models
{
    public class DayRadioStatus
    {
        public DayRadioStatus(ClientSiteRadioStatus clientSiteRadioStatus)
        {
            Date = clientSiteRadioStatus.CheckDate;
            Check1 = clientSiteRadioStatus.Check1;
            Check2 = clientSiteRadioStatus.Check2;
            Check3 = clientSiteRadioStatus.Check3;
        }

        public DateTime Date { get; set; }

        public string Check1 { get; set; }

        public string Check2 { get; set; }

        public string Check3 { get; set; }
    }
}
