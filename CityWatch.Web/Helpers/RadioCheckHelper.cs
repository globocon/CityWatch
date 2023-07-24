using CityWatch.Data.Models;
using System.Collections.Generic;
using System;

namespace CityWatch.Web.Helpers
{
    public static class RadioCheckHelper
    {
        public static List<ClientSiteRadioStatus> GetEmptyClientSiteRadioStatus(ClientSite clientSite, DateTime weekStart)
        {
            var clientSiteRadioStatus = new List<ClientSiteRadioStatus>();

            for (int i = 0; i < 7; i++)
            {
                clientSiteRadioStatus.Add(new ClientSiteRadioStatus()
                {
                    ClientSiteId = clientSite.Id,
                    ClientSite = clientSite,
                    CheckDate = weekStart.AddDays(i),
                    Check1 = "N/A",
                    Check2 = "N/A",
                    Check3 = "N/A",
                });
            }

            return clientSiteRadioStatus;
        }
    }
}
