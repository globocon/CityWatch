/* Archive of GuardLogs, ClientSiteRadioChecksActivityStatus_History and GuardLogsDocumentImages.

   Backup-CityWatchArchive.ps1 moves rows older than 18 months here each night (archive + delete
   from prod-citywatch in one transaction per batch), so for those rows THIS DATABASE IS THE ONLY
   LIVE COPY. Include it in the instance's regular full backups: the script will not run until it
   has a full backup less than 7 days old. It keeps everything; nothing is ever purged from it.

   Run once, on the same SQL Server instance as prod-citywatch (the backup / restore scripts copy
   between the two databases with three-part names, so both must be on one instance).
   Safe to re-run: it only creates what is missing.

   - SIMPLE recovery: it only changes once a night, so a nightly full backup (taken after the
     archive job) loses nothing, and SIMPLE keeps the log small during the big first load.
   - Tables have prod's columns (no IDENTITY, so prod's Id is kept as is) plus ArchivedAtUtc, the
     time the row was archived. No foreign keys: the archive must keep rows even after their
     logbook / guard / site is gone from prod.
   - PAGE compression: the history rows repeat site names, guard names and time zones heavily;
     compressed they take roughly half the space. Available in every edition since SQL 2016 SP1.
   - If prod gets a new column later, Backup-CityWatchArchive.ps1 adds it here automatically.

   Sizes (see README.md): the first night moves ~1.4M log + ~1.4M history rows (~0.6 GB PAGE
   compressed); after that ~18k rows a night at today's volume (~0.8 GB a year). The database is
   pre-sized at 4 GB data + 1 GB log.
*/
USE [master]
GO

IF DB_ID(N'prod_citywatch_backup') IS NULL
BEGIN
    -- Same collation as prod-citywatch, which can differ from the instance default: text must
    -- compare and sort the same in both copies, and the scripts compare across the two.
    -- Files go to the instance's default data / log folders. To put them elsewhere, add
    -- ON PRIMARY (NAME = ..., FILENAME = '...') LOG ON (...) here.
    IF DB_ID(N'prod-citywatch') IS NULL
        THROW 50000, 'prod-citywatch not found on this instance - the archive must be on the same instance', 1;
    -- Asked from inside the database: DATABASEPROPERTYEX gives NULL for a database that is closed (AUTO_CLOSE)
    DECLARE @collation SYSNAME;
    EXEC [prod-citywatch].sys.sp_executesql N'SELECT @c = CAST(DATABASEPROPERTYEX(DB_NAME(), ''Collation'') AS SYSNAME);',
        N'@c SYSNAME OUTPUT', @c = @collation OUTPUT;
    EXEC (N'CREATE DATABASE [prod_citywatch_backup] COLLATE ' + @collation + N';');
END
ELSE IF (SELECT collation_name FROM sys.databases WHERE name = N'prod_citywatch_backup')
        <> (SELECT collation_name FROM sys.databases WHERE name = N'prod-citywatch')
    PRINT 'WARNING: prod_citywatch_backup has a different collation from prod-citywatch';
GO

ALTER DATABASE [prod_citywatch_backup] SET RECOVERY SIMPLE;
ALTER DATABASE [prod_citywatch_backup] SET AUTO_SHRINK OFF;
GO

-- Grow in big steps rather than the default small ones during the first load
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER DATABASE [prod_citywatch_backup] MODIFY FILE (NAME = N''' + name + N''', '
             + CASE WHEN type = 0 THEN N'SIZE = 4096MB, FILEGROWTH = 1024MB' ELSE N'SIZE = 1024MB, FILEGROWTH = 512MB' END
             + N');'
FROM sys.master_files
WHERE database_id = DB_ID(N'prod_citywatch_backup')
  AND ((type = 0 AND size < 4096 * 128) OR (type = 1 AND size < 1024 * 128));
EXEC (@sql);
GO

USE [prod_citywatch_backup]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.GuardLogs') IS NULL
BEGIN
    CREATE TABLE dbo.GuardLogs
    (
        Id                              INT            NOT NULL,
        ClientSiteLogBookId             INT            NOT NULL,
        EventDateTime                   DATETIME       NOT NULL,
        Notes                           VARCHAR(2048)  NOT NULL,
        GuardLoginId                    INT            NULL,
        IsSystemEntry                   BIT            NOT NULL,
        IrEntryType                     INT            NULL,
        RcPushMessageId                 INT            NULL,
        PlayNotificationSound           BIT            NULL,
        EventDateTimeServerOffsetMinute INT            NULL,
        EventDateTimeLocal              DATETIME       NULL,
        EventDateTimeLocalWithOffset    DATETIMEOFFSET NULL,
        EventDateTimeZone               NVARCHAR(500)  NULL,
        EventDateTimeZoneShort          NVARCHAR(20)   NULL,
        EventDateTimeUtcOffsetMinute    INT            NULL,
        GpsCoordinates                  VARCHAR(MAX)   NULL,
        IsIRReportTypeEntry             BIT            NULL,
        RcLogbookStamp                  BIT            NULL,
        EventType                       INT            NOT NULL,
        WAND_TAG_ENTRY_TYPE             INT            NOT NULL,
        IsOfflineRecord                 BIT            NOT NULL,
        OfflineRecordSyncDateTime       DATETIME       NULL,
        EventServerUtcDateTime          DATETIME       NOT NULL,
        EventMobileUtcDateTime          DATETIME       NULL,
        TagScanHitLogRefId              INT            NULL,
        EntryPassedByPCARclientsiteId   INT            NULL,
        IsEntryByPCAR                   BIT            NULL,
        CallSignId                      INT            NULL,
        PositionId                      INT            NULL,
        ArchivedAtUtc                   DATETIME2(0)   NOT NULL CONSTRAINT DF_GuardLogs_ArchivedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_GuardLogs PRIMARY KEY CLUSTERED (Id) WITH (DATA_COMPRESSION = PAGE)
    );

    -- Retention purge and restore-by-date; ClientSiteLogBookId for restore-by-site
    CREATE NONCLUSTERED INDEX IX_GuardLogs_EventDateTime ON dbo.GuardLogs (EventDateTime) WITH (DATA_COMPRESSION = PAGE);
    CREATE NONCLUSTERED INDEX IX_GuardLogs_ClientSiteLogBookId ON dbo.GuardLogs (ClientSiteLogBookId) WITH (DATA_COMPRESSION = PAGE);
END
GO

IF OBJECT_ID(N'dbo.ClientSiteRadioChecksActivityStatus_History') IS NULL
BEGIN
    CREATE TABLE dbo.ClientSiteRadioChecksActivityStatus_History
    (
        Id                               INT            NOT NULL,
        ClientSiteId                     INT            NULL,
        GuardId                          INT            NULL,
        LastIRCreatedTime                DATETIME       NULL,
        LastKVCreatedTime                DATETIME       NULL,
        LastLBCreatedTime                DATETIME       NULL,
        GuardLoginTime                   DATETIME       NULL,
        GuardLogoutTime                  DATETIME       NULL,
        ActivityType                     NCHAR(10)      NULL,
        LBId                             INT            NULL,
        IRId                             INT            NULL,
        KVId                             INT            NULL,
        LastSWCreatedTime                DATETIME       NULL,
        SWId                             INT            NULL,
        ActivityDescription              NVARCHAR(MAX)  NULL,
        NotificationCreatedTime          DATETIME       NULL,
        OnDuty                           DATETIME       NULL,
        OffDuty                          DATETIME       NULL,
        NotificationType                 INT            NULL,
        GuardLoginTimeLocal              DATETIME       NULL,
        GuardLoginTimeLocalWithOffset    DATETIMEOFFSET NULL,
        GuardLoginTimeZone               NVARCHAR(500)  NULL,
        GuardLoginTimeZoneShort          NVARCHAR(20)   NULL,
        GuardLoginTimeUtcOffsetMinute    INT            NULL,
        GuardLoginTimeServerOffsetMinute INT            NULL,
        CRMSupplier                      NVARCHAR(MAX)  NULL,
        LogBookNotes                     NVARCHAR(MAX)  NULL,
        KVNotes                          NVARCHAR(MAX)  NULL,
        IRNotes                          NVARCHAR(MAX)  NULL,
        SwNotes                          NVARCHAR(MAX)  NULL,
        SiteName                         NVARCHAR(500)  NULL,
        GuardName                        NVARCHAR(500)  NULL,
        EventDateTime                    DATETIME       NOT NULL,
        EventDateTimeServerOffsetMinute  INT            NULL,
        EventDateTimeLocal               DATETIME       NULL,
        EventDateTimeLocalWithOffset     DATETIMEOFFSET NULL,
        EventDateTimeZone                NVARCHAR(500)  NULL,
        EventDateTimeZoneShort           NVARCHAR(20)   NULL,
        EventDateTimeUtcOffsetMinute     INT            NULL,
        Notes                            NVARCHAR(MAX)  NULL,
        ArchivedAtUtc                    DATETIME2(0)   NOT NULL CONSTRAINT DF_History_ArchivedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ClientSiteRadioChecksActivityStatus_History PRIMARY KEY CLUSTERED (Id) WITH (DATA_COMPRESSION = PAGE)
    );

    CREATE NONCLUSTERED INDEX IX_History_EventDateTime ON dbo.ClientSiteRadioChecksActivityStatus_History (EventDateTime) INCLUDE (ClientSiteId) WITH (DATA_COMPRESSION = PAGE);
END
GO

IF OBJECT_ID(N'dbo.GuardLogsDocumentImages') IS NULL
BEGIN
    -- Photo / attachment records of log entries (the files themselves stay where they are)
    CREATE TABLE dbo.GuardLogsDocumentImages
    (
        Id                      INT           NOT NULL,
        ImagePath               NVARCHAR(MAX) NULL,
        GuardLogId              INT           NULL,
        IsRearfile              BIT           NULL,
        IsTwentyfivePercentfile BIT           NULL,
        IsVideo                 BIT           NOT NULL,
        ArchivedAtUtc           DATETIME2(0)  NOT NULL CONSTRAINT DF_GuardLogsDocumentImages_ArchivedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_GuardLogsDocumentImages PRIMARY KEY CLUSTERED (Id) WITH (DATA_COMPRESSION = PAGE)
    );

    CREATE NONCLUSTERED INDEX IX_GuardLogsDocumentImages_GuardLogId ON dbo.GuardLogsDocumentImages (GuardLogId) WITH (DATA_COMPRESSION = PAGE);
END
GO

-- Rows put back by Restore-CityWatchArchive.ps1: the nightly job does not delete them from prod
-- again until HoldUntilUtc
IF OBJECT_ID(N'dbo.ProdDeleteHold') IS NULL
BEGIN
    CREATE TABLE dbo.ProdDeleteHold
    (
        Id           INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ProdDeleteHold PRIMARY KEY,
        TableName    SYSNAME       NOT NULL,
        FromId       INT           NOT NULL,
        ToId         INT           NOT NULL,
        HoldUntilUtc DATETIME2(0)  NOT NULL,
        CreatedAtUtc DATETIME2(0)  NOT NULL CONSTRAINT DF_ProdDeleteHold_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        RestoreRunId UNIQUEIDENTIFIER NULL
    );
END
GO

-- One row per table per run: what the backup / restore scripts did
IF OBJECT_ID(N'dbo.ArchiveRunLog') IS NULL
BEGIN
    CREATE TABLE dbo.ArchiveRunLog
    (
        Id             INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ArchiveRunLog PRIMARY KEY,
        RunId          UNIQUEIDENTIFIER NOT NULL,
        Operation      VARCHAR(20)    NOT NULL,   -- Archive / Restore
        TableName      SYSNAME        NOT NULL,
        StartedAtUtc   DATETIME2(0)   NOT NULL,
        FinishedAtUtc  DATETIME2(0)   NULL,
        FromId         INT            NULL,
        ToId           INT            NULL,
        RowsCopied     BIGINT         NOT NULL CONSTRAINT DF_ArchiveRunLog_RowsCopied DEFAULT (0),
        RowsPurged     BIGINT         NOT NULL CONSTRAINT DF_ArchiveRunLog_RowsPurged DEFAULT (0),
        RowsSkipped    BIGINT         NOT NULL CONSTRAINT DF_ArchiveRunLog_RowsSkipped DEFAULT (0),
        Status         VARCHAR(20)    NOT NULL,   -- Running / Succeeded / Stopped / Failed
        Message        NVARCHAR(MAX)  NULL
    );
END
GO
