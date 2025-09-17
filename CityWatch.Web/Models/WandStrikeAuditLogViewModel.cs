using CityWatch.Data.Models;
using System.Collections.Generic;


namespace CityWatch.Web.Models
{
    public class WandStrikeAuditLogViewModel
    {             
        public ClientSiteSmartWandTagsHitLog clientSiteSmartWandTagsHitLog { get; set; }
        public string GroupText { get { return $"{clientSiteSmartWandTagsHitLog.HitLocalDateTime.Date.ToString("dd MMM yyyy")} - [{clientSiteSmartWandTagsHitLog.TagUId}]"; } }
        public string SmartWandType { get { return clientSiteSmartWandTagsHitLog.SmartWandTagsType?.value ?? "Smart Wand"; } }
        public string EndUser { get { return $"{clientSiteSmartWandTagsHitLog.LoggedInGuard.Name} [{clientSiteSmartWandTagsHitLog.LoggedInGuard.Initial}]"; } }
    }
}
