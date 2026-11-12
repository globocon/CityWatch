/* 385: Triggers feeding ClientSiteRadioChecksActivityStatus / ClientSiteRadioChecksActivityStatus_History

   Run on prod-citywatch, off-peak. Safe to re-run (CREATE OR ALTER, IF NOT EXISTS / IF EXISTS).

   What was wrong
   --------------
   1. Insert_GuardLogs and Insert_IncidentReports did not read the "inserted" rows. They took
      SELECT MAX(Id) FROM the table and treated that as the new row:
        - when two guards save at the same moment, both trigger runs can pick the same (latest)
          entry, so one guard's activity is recorded twice and the other's not at all - the wrong
          guard can show as active in Radio Check;
        - reading MAX(Id) waits on any other session's uncommitted insert at the end of the index,
          so concurrent logbook saves queue up behind each other;
        - an INSERT of several rows (offline sync, batched saves) only handled the last row.
   2. Insert_VehicleKeyLogs fails the whole insert with "Subquery returned more than 1 value" when
      more than one KV row is inserted in one statement.
   3. Trigger_RcHistory looked every value up with its own sub-select: up to 26 separate lookups
      per row (GuardLogs alone is read 9 times for the same LBId). Now one join per table.
   4. No SET NOCOUNT ON: each trigger sent extra "rows affected" messages back to the app.
   5. ClientSiteRadioChecksActivityStatus_History has foreign keys on LBId, KVId and IRId but no
      index on them. Every delete from GuardLogs / VehicleKeyLogs / IncidentReports (a guard
      deleting a log entry, a logbook being removed) has to scan the whole history table to check
      the key: measured on the dev copy, deleting ONE GuardLogs row read 396,543 pages (3 GB) and
      took 6.6 s, holding its locks the whole time. Filtered indexes below make it a seek.
   6. IX_ClientSiteRadioChecksActivityStatus_History_EventDateTime duplicates
      IX_RadioChecksActivityStatus_History_EventDateTime_ClientSiteId (same key, the other also
      INCLUDEs ClientSiteId): ~140 MB and one more index to maintain on every insert, for nothing.

   What is NOT changed
   -------------------
   The rules themselves: which entries count as activity ('Logbook Logged In' and '%N/A%' notes do
   not), LB vs SW by WAND_TAG_ENTRY_TYPE, removing the guard's rows for other sites, clearing the
   'Off Duty' radio check, and every value written to the history table are as before.

   New: restores
   -------------
   Insert_GuardLogs does nothing when the session sets
       EXEC sp_set_session_context N'CityWatch.SkipActivityTriggers', 1;
   The restore script (Restore-CityWatchArchive.ps1) uses this so re-inserting old log entries
   does not make guards look active today. It only affects the session that sets it.

   Index builds: ONLINE where the edition supports it (Enterprise / Developer / Azure SQL),
   otherwise offline - the history table is then locked for writes while each index builds
   (seconds to a minute on ~5M rows), which holds up logbook saves. Run off-peak.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ---------------------------------------------------------------------------------------------
   1. Indexes
   --------------------------------------------------------------------------------------------- */
DECLARE @Online NVARCHAR(10) =
    CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN N'ON' ELSE N'OFF' END;
DECLARE @With NVARCHAR(200) = N' WITH (ONLINE = ' + @Online + N', SORT_IN_TEMPDB = ON, MAXDOP = 2)';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RadioChecksActivityStatus_History_LBId'
               AND object_id = OBJECT_ID('dbo.ClientSiteRadioChecksActivityStatus_History'))
    EXEC (N'CREATE NONCLUSTERED INDEX IX_RadioChecksActivityStatus_History_LBId
            ON dbo.ClientSiteRadioChecksActivityStatus_History (LBId) WHERE LBId IS NOT NULL' + @With);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RadioChecksActivityStatus_History_KVId'
               AND object_id = OBJECT_ID('dbo.ClientSiteRadioChecksActivityStatus_History'))
    EXEC (N'CREATE NONCLUSTERED INDEX IX_RadioChecksActivityStatus_History_KVId
            ON dbo.ClientSiteRadioChecksActivityStatus_History (KVId) WHERE KVId IS NOT NULL' + @With);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RadioChecksActivityStatus_History_IRId'
               AND object_id = OBJECT_ID('dbo.ClientSiteRadioChecksActivityStatus_History'))
    EXEC (N'CREATE NONCLUSTERED INDEX IX_RadioChecksActivityStatus_History_IRId
            ON dbo.ClientSiteRadioChecksActivityStatus_History (IRId) WHERE IRId IS NOT NULL' + @With);

-- The triggers delete a guard's rows for other sites by GuardId; the existing index leads with ClientSiteId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClientSiteRadioChecksActivityStatus_GuardId'
               AND object_id = OBJECT_ID('dbo.ClientSiteRadioChecksActivityStatus'))
    EXEC (N'CREATE NONCLUSTERED INDEX IX_ClientSiteRadioChecksActivityStatus_GuardId
            ON dbo.ClientSiteRadioChecksActivityStatus (GuardId) INCLUDE (ClientSiteId)' + @With);

-- Redundant: IX_RadioChecksActivityStatus_History_EventDateTime_ClientSiteId has the same key
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RadioChecksActivityStatus_History_EventDateTime_ClientSiteId'
           AND object_id = OBJECT_ID('dbo.ClientSiteRadioChecksActivityStatus_History'))
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClientSiteRadioChecksActivityStatus_History_EventDateTime'
               AND object_id = OBJECT_ID('dbo.ClientSiteRadioChecksActivityStatus_History'))
    DROP INDEX IX_ClientSiteRadioChecksActivityStatus_History_EventDateTime
        ON dbo.ClientSiteRadioChecksActivityStatus_History;
GO

/* ---------------------------------------------------------------------------------------------
   2. Insert_GuardLogs
   --------------------------------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER [dbo].[Insert_GuardLogs]
   ON [dbo].[GuardLogs]
   AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted)
        RETURN;

    -- Restores of archived entries are not new activity (see header)
    IF CAST(SESSION_CONTEXT(N'CityWatch.SkipActivityTriggers') AS INT) = 1
        RETURN;

    DECLARE @Rows TABLE
    (
        Id           INT PRIMARY KEY,
        ClientSiteId INT NULL,
        GuardId      INT NULL,
        Notes        VARCHAR(2048) NULL,
        IsWand       BIT NOT NULL,
        IsActivity   BIT NOT NULL
    );

    -- Entries without a guard login never did anything here, so they are left out
    INSERT INTO @Rows (Id, ClientSiteId, GuardId, Notes, IsWand, IsActivity)
    SELECT i.Id, gl.ClientSiteId, gl.GuardId, i.Notes,
           CASE WHEN i.WAND_TAG_ENTRY_TYPE = 0 THEN 0 ELSE 1 END,
           CASE WHEN i.Notes <> 'Logbook Logged In' AND i.Notes NOT LIKE '%N/A%' THEN 1 ELSE 0 END
    FROM inserted i
    INNER JOIN GuardLogins gl ON gl.Id = i.GuardLoginId;

    IF NOT EXISTS (SELECT 1 FROM @Rows)
        RETURN;

    -- New notes (LB) or wand scan (SW) for the guard at this site
    INSERT INTO ClientSiteRadioChecksActivityStatus
        (ClientSiteId, GuardId, LastLBCreatedTime, LastSWCreatedTime, LBId, ActivityType, ActivityDescription)
    SELECT r.ClientSiteId,
           r.GuardId,
           CASE WHEN r.IsWand = 0 THEN GETDATE() END,
           CASE WHEN r.IsWand = 1 THEN GETDATE() END,
           r.Id,
           CASE WHEN r.IsWand = 0 THEN 'LB' ELSE 'SW' END,
           CASE WHEN r.IsWand = 0 THEN N'Added New Notes' ELSE r.Notes END
    FROM @Rows r
    WHERE r.IsActivity = 1 AND r.ClientSiteId <> 0
    ORDER BY r.Id;

    -- The guard is only active at one site: drop their rows for any other site (the site of their
    -- latest entry in this insert, if there are several)
    ;WITH LatestPerGuard AS
    (
        SELECT r.GuardId, r.ClientSiteId,
               ROW_NUMBER() OVER (PARTITION BY r.GuardId ORDER BY r.Id DESC) AS rn
        FROM @Rows r
        WHERE r.IsActivity = 0 OR r.ClientSiteId <> 0
    )
    DELETE a
    FROM ClientSiteRadioChecksActivityStatus a
    INNER JOIN LatestPerGuard l ON l.rn = 1 AND a.GuardId = l.GuardId AND a.ClientSiteId <> l.ClientSiteId;

    -- Show the guard as active again if they enter notes after being logged off
    DELETE rc
    FROM ClientSiteRadioChecks rc
    INNER JOIN (SELECT DISTINCT ClientSiteId, GuardId FROM @Rows WHERE IsActivity = 1 AND ClientSiteId <> 0) r
        ON rc.ClientSiteId = r.ClientSiteId AND rc.GuardId = r.GuardId
    WHERE rc.RadioCheckStatusId = 1
      AND rc.Status IN ('Off Duty (RC automatic logoff)', 'Off Duty');
END
GO

/* ---------------------------------------------------------------------------------------------
   3. Insert_IncidentReports (same MAX(Id) problem)
   --------------------------------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER [dbo].[Insert_IncidentReports]
   ON [dbo].[IncidentReports]
   AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Rows TABLE (Id INT PRIMARY KEY, ClientSiteId INT NOT NULL, GuardId INT NOT NULL);

    INSERT INTO @Rows (Id, ClientSiteId, GuardId)
    SELECT i.Id, i.ClientSiteId, i.GuardId
    FROM inserted i
    WHERE i.GuardId <> 0 AND i.ClientSiteId <> 0;

    IF NOT EXISTS (SELECT 1 FROM @Rows)
        RETURN;

    INSERT INTO ClientSiteRadioChecksActivityStatus
        (ClientSiteId, GuardId, LastIRCreatedTime, IRId, ActivityType, ActivityDescription)
    SELECT r.ClientSiteId, r.GuardId, GETDATE(), r.Id, 'IR', 'Created'
    FROM @Rows r
    ORDER BY r.Id;

    ;WITH LatestPerGuard AS
    (
        SELECT r.GuardId, r.ClientSiteId,
               ROW_NUMBER() OVER (PARTITION BY r.GuardId ORDER BY r.Id DESC) AS rn
        FROM @Rows r
    )
    DELETE a
    FROM ClientSiteRadioChecksActivityStatus a
    INNER JOIN LatestPerGuard l ON l.rn = 1 AND a.GuardId = l.GuardId AND a.ClientSiteId <> l.ClientSiteId;
END
GO

/* ---------------------------------------------------------------------------------------------
   4. Insert_VehicleKeyLogs (failed on multi-row inserts)
   --------------------------------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER [dbo].[Insert_VehicleKeyLogs]
   ON [dbo].[VehicleKeyLogs]
   AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Rows TABLE (Id INT PRIMARY KEY, ClientSiteId INT NULL, GuardId INT NULL);

    INSERT INTO @Rows (Id, ClientSiteId, GuardId)
    SELECT i.Id, lb.ClientSiteId, gl.GuardId
    FROM inserted i
    LEFT JOIN ClientSiteLogBooks lb ON lb.Id = i.ClientSiteLogBookId
    LEFT JOIN GuardLogins gl ON gl.Id = i.GuardLoginId;

    IF NOT EXISTS (SELECT 1 FROM @Rows)
        RETURN;

    INSERT INTO ClientSiteRadioChecksActivityStatus
        (ClientSiteId, GuardId, LastKVCreatedTime, KVId, ActivityType, ActivityDescription)
    SELECT r.ClientSiteId, r.GuardId, GETDATE(), r.Id, 'KV', 'Created'
    FROM @Rows r
    ORDER BY r.Id;

    ;WITH LatestPerGuard AS
    (
        SELECT r.GuardId, r.ClientSiteId,
               ROW_NUMBER() OVER (PARTITION BY r.GuardId ORDER BY r.Id DESC) AS rn
        FROM @Rows r
    )
    DELETE a
    FROM ClientSiteRadioChecksActivityStatus a
    INNER JOIN LatestPerGuard l ON l.rn = 1 AND a.GuardId = l.GuardId AND a.ClientSiteId <> l.ClientSiteId;
END
GO

/* ---------------------------------------------------------------------------------------------
   5. Trigger_RcHistory: one join per source table instead of a sub-select per value
   --------------------------------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER [dbo].[Trigger_RcHistory] ON [dbo].[ClientSiteRadioChecksActivityStatus]
FOR INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ClientSiteRadioChecksActivityStatus_History
        (ClientSiteId, GuardId, LastIRCreatedTime, LastKVCreatedTime, LastLBCreatedTime,
         GuardLoginTime, GuardLogoutTime, ActivityType, LBId, IRId, KVId, LastSWCreatedTime, SWId,
         ActivityDescription, NotificationCreatedTime, OnDuty, OffDuty, NotificationType,
         GuardLoginTimeLocal, GuardLoginTimeLocalWithOffset, GuardLoginTimeZone, GuardLoginTimeZoneShort,
         GuardLoginTimeUtcOffsetMinute, GuardLoginTimeServerOffsetMinute, CRMSupplier,
         LogBookNotes, KVNotes, IRNotes, SwNotes, SiteName, GuardName,
         EventDateTime, EventDateTimeServerOffsetMinute, EventDateTimeLocal, EventDateTimeLocalWithOffset,
         EventDateTimeZone, EventDateTimeZoneShort, EventDateTimeUtcOffsetMinute, Notes)
    SELECT
        i.ClientSiteId, i.GuardId, i.LastIRCreatedTime, i.LastKVCreatedTime, i.LastLBCreatedTime,
        i.GuardLoginTime, i.GuardLogoutTime,
        ISNULL(i.ActivityType, 'LB'),
        i.LBId, i.IRId, i.KVId, i.LastSWCreatedTime, i.SWId,
        i.ActivityDescription, i.NotificationCreatedTime, i.OnDuty, i.OffDuty, i.NotificationType,
        i.GuardLoginTimeLocal, i.GuardLoginTimeLocalWithOffset, i.GuardLoginTimeZone, i.GuardLoginTimeZoneShort,
        i.GuardLoginTimeUtcOffsetMinute, i.GuardLoginTimeServerOffsetMinute, i.CRMSupplier,

        -- LogBookNotes, KVNotes, IRNotes, SwNotes
        CASE WHEN i.LBId IS NOT NULL THEN gl.Notes END,
        CASE WHEN i.KVId IS NOT NULL THEN kv.VehicleRego END,
        CASE WHEN i.IRId IS NOT NULL THEN ir.FileName END,
        CASE WHEN i.SWId IS NOT NULL THEN '[' + sw.SmartWandId + '] ' + sw.LocationScan END,

        cs.Name,
        CASE WHEN i.NotificationType = 1 THEN 'System' ELSE '[' + g.Initial + '] ' + g.Name END,

        -- Event time: from the logbook entry / KV / IR, else the login, else now
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTime
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeServer
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOn
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTime
             ELSE GETDATE() END,
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTimeServerOffsetMinute
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeServerOffsetMinute
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOnDateTimeServerOffsetMinute
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTimeServerOffsetMinute END,
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTimeLocal
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeLocal
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOnDateTimeLocal
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTimeLocal END,
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTimeLocalWithOffset
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeLocalWithOffset
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOnDateTimeLocalWithOffset
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTimeLocalWithOffset END,
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTimeZone
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeZone
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOnDateTimeZone
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTimeZone END,
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTimeZoneShort
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeZoneShort
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOnDateTimeZoneShort
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTimeZoneShort END,
        CASE WHEN i.LBId IS NOT NULL THEN gl.EventDateTimeUtcOffsetMinute
             WHEN i.KVId IS NOT NULL THEN kv.EntryCreatedDateTimeUtcOffsetMinute
             WHEN i.IRId IS NOT NULL THEN ir.CreatedOnDateTimeUtcOffsetMinute
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN i.GuardLoginTimeUtcOffsetMinute END,

        -- Notes
        CASE WHEN i.LBId IS NOT NULL THEN gl.Notes
             WHEN i.KVId IS NOT NULL THEN kv.VehicleRego
             WHEN i.IRId IS NOT NULL THEN ir.FileName
             WHEN i.SWId IS NOT NULL THEN '[' + sw.SmartWandId + '] ' + sw.LocationScan
             WHEN i.ActivityType IS NULL AND i.NotificationType IS NULL THEN 'Guard Login'
             WHEN i.ActivityType IS NULL AND i.NotificationType = 1 THEN 'No Guard on Duty' END
    FROM inserted i
    LEFT JOIN GuardLogs gl ON gl.Id = i.LBId
    LEFT JOIN VehicleKeyLogs kv ON kv.Id = i.KVId
    LEFT JOIN IncidentReports ir ON ir.Id = i.IRId
    LEFT JOIN RadioChecksSmartWandScanResults sw ON sw.Id = i.SWId
    LEFT JOIN ClientSites cs ON cs.Id = i.ClientSiteId
    LEFT JOIN Guards g ON g.Id = i.GuardId;
END
GO
