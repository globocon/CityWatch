

ALTER TABLE [PostActivityRequestLocalCacheOfflineNotSynced]
ADD LogbookclientsiteId INT NULL,
IsEntryByPCAR BIT DEFAULT 0,
CallSignId INT NULL,
PositionId INT NULL;

ALTER TABLE [OfflineFilesRecordsNotSynced]
ADD LogbookclientsiteId INT NULL,
IsEntryByPCAR BIT DEFAULT 0,
CallSignId INT NULL,
PositionId INT NULL;

ALTER TABLE [GuardLogs]
ADD EntryPassedByPCARclientsiteId INT NULL,
IsEntryByPCAR BIT DEFAULT 0,
CallSignId INT NULL,
PositionId INT NULL;

CREATE TABLE [GuardLogsLinked]
(
Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
GuardLogId INT NOT NULL,
LinkedGuardLogId INT NOT NULL
);

CREATE NONCLUSTERED INDEX IX_GuardLogsLinked_GuardLogId
ON GuardLogsLinked(GuardLogId);
 
CREATE NONCLUSTERED INDEX IX_GuardLogsLinked_LinkedGuardLogId
ON GuardLogsLinked(LinkedGuardLogId);

