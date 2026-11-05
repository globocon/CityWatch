ALTER TABLE [GuardLogs]
ADD EventServerUtcDateTime DATETIME NOT NULL
CONSTRAINT DF_GuardLogs_EventServerUtcDateTime 
DEFAULT GETUTCDATE(),
EventMobileUtcDateTime DATETIME NULL,
TagScanHitLogRefId INT NULL;


UPDATE t
SET [EventServerUtcDateTime] =
    DATEADD(MINUTE, -ISNULL([EventDateTimeServerOffsetMinute],0), [EventDateTime])
FROM [GuardLogs] t;

