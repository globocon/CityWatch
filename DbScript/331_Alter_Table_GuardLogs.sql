ALTER TABLE GuardLogs
ADD IsOfflineRecord bit default 0 Not Null,
OfflineRecordSyncDateTime DateTime Null
