Alter table ClientSiteSmartWandTagsHitLogs
add IsScanFromLinkedSite bit not null default 0

Alter table [ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced]
add IsScanFromLinkedSite bit not null default 0