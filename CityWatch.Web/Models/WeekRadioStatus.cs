using CityWatch.Data.Models;
using CityWatch.Web.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Web.Models
{
    public class WeekRadioStatus
    {
        public WeekRadioStatus(List<ClientSiteRadioStatus> csRadioStatuses)
        {
            ClientSite = csRadioStatuses.First().ClientSite;
            Days = new Dictionary<DayOfWeek, DayRadioStatus>();
            foreach (var radioStatus in csRadioStatuses)
            {
                Days.Add(radioStatus.CheckDate.DayOfWeek, new DayRadioStatus(radioStatus));
            }
        }

        public ClientSite ClientSite { get; set; }

        public Dictionary<DayOfWeek, DayRadioStatus> Days { get; set; }

        public int LastEditableColumnIndex
        {
            get
            {
                if (Days.All(z => z.Value.Date > DateTime.Today))
                    return 0;

                if (Days.All(z => z.Value.Date < DateTime.Today.StartOfWeek()))
                    return 21;

                var lastEditableColumnIndex = 0;
                for (int i = 0; i <= (DateTime.Now.Date - DateTime.Today.StartOfWeek().Date).Days; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        lastEditableColumnIndex++;
                    }
                }
                return lastEditableColumnIndex;
            }
        }
    }
}
