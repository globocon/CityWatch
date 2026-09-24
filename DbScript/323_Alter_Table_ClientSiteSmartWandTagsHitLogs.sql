Alter Table ClientSiteSmartWandTagsHitLogs
Add UniqueRecordId uniqueidentifier null,
IsOfflineRecord bit not null default 0,
OfflineRecordSyncUtcDateTime DateTime null

