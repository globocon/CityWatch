
namespace CityWatch.Data.Models
{
    public class WandStrikeAuditLogViewModel
    {             
        public ClientSiteSmartWandTagsHitLog clientSiteSmartWandTagsHitLog { get; set; }
        public string GroupText { get { return $"{clientSiteSmartWandTagsHitLog.HitLocalDateTime.Date.ToString("dd MMM yyyy")}"; } }
        public string SmartWandType { get { return clientSiteSmartWandTagsHitLog.SmartWandTagsType?.value ?? "Smart Wand"; } }
        public string DateTimeSort { get { return $"{clientSiteSmartWandTagsHitLog.HitLocalDateTime.Date}"; } }
        public string EndUserSort { get { return $"{clientSiteSmartWandTagsHitLog.LoggedInGuard.Name}"; } }
        public string EndUser { get { return $"{clientSiteSmartWandTagsHitLog.LoggedInGuard.Name} [{clientSiteSmartWandTagsHitLog.LoggedInGuard.Initial}]"; } }
    }

    public class WandStrikeAuditLogExcelViewModel
    {
        public ClientSiteSmartWandTagsHitLogViewModel clientSiteSmartWandTagsHitLog { get; set; }
        public string GroupText { get { return $"{(clientSiteSmartWandTagsHitLog.HitLocalDateTime.HasValue ? clientSiteSmartWandTagsHitLog.HitLocalDateTime.Value.Date.ToString("dd MMM yyyy") : "")}"; } }
        public string SmartWandType { get { return clientSiteSmartWandTagsHitLog?.SmartWandTagsType?.value ?? "Smart Wand"; } }
        public string DateTimeSort { get { return $"{(clientSiteSmartWandTagsHitLog.HitLocalDateTime.HasValue ? clientSiteSmartWandTagsHitLog.HitLocalDateTime.Value.Date : "") }"; } }
        public string EndUserSort { get { return $"{clientSiteSmartWandTagsHitLog?.LoggedInGuard?.Name}"; } }
        public string EndUser { get { return $"{clientSiteSmartWandTagsHitLog?.LoggedInGuard?.Name} [{clientSiteSmartWandTagsHitLog?.LoggedInGuard?.Initial}]"; } }
    }
}
