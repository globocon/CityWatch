/* 387: Patrol Randomisation (Kpi > Admin > Settings > Mobile App > Wand Strikes) and its scan log
   (Web > Admin > Audit Site Log > Randomisation).

   On large sites a fixed patrol path (A-B-C-D...) is predictable, so anyone watching can learn
   where the guard goes and how long until they come back. With randomisation on, each wand
   strike is answered with the next checkpoint the guard must go to, picked at random from the
   site's registered tags.

   Rules (enforced in PatrolRandomisationHelper, used by Kpi and Web):
     - only for Standard (STND) touring mode; PCAR and INSP switch it off;
     - off by default;
     - any number of days a week, one window per day of at most 2 hours, start before end
       (same day), held in the site's local time (ClientSiteKpiSettings.TimezoneString).

   ClientSitePatrolRandomisationSettings  - one row per site: the on/off switch.
   ClientSitePatrolRandomisationSchedules - the windows, one row per site and day of week.
   ClientSitePatrolRandomisationLogs      - the scan log: one row per instruction handed to a guard
       ("go to tag X next") and the strike that answered it.
       Status 0 Pending    - instruction given, guard has not struck a tag yet
              1 Completed  - the next strike was the tag the guard was sent to
              2 Skipped    - the next strike was a different tag (the assigned one was skipped)
              3 Missed     - the window closed before the guard struck any tag
              4 Cancelled  - randomisation was switched off / deleted for the site
              5 Unguided   - a strike made in the window with no instruction to follow (e.g. an
                             offline scan synced later); AssignedTag* is empty on these rows
       Times are kept twice, UTC and site local (with the offset in force), per the house rule.

   Safe to re-run. */

IF OBJECT_ID('dbo.ClientSitePatrolRandomisationSettings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClientSitePatrolRandomisationSettings]
    (
        [Id]                   INT IDENTITY(1,1) NOT NULL,
        [ClientSiteId]         INT NOT NULL,
        [IsEnabled]            BIT NOT NULL CONSTRAINT [DF_ClientSitePatrolRandomisationSettings_IsEnabled] DEFAULT (0),
        [LastUpdatedUtc]       DATETIME2(0) NOT NULL CONSTRAINT [DF_ClientSitePatrolRandomisationSettings_LastUpdatedUtc] DEFAULT (SYSUTCDATETIME()),
        [LastUpdatedByUserId]  INT NULL,
        [LastUpdatedByGuardId] INT NULL,
        CONSTRAINT [PK_ClientSitePatrolRandomisationSettings] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ClientSitePatrolRandomisationSettings_ClientSites] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id])
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ClientSitePatrolRandomisationSettings_ClientSiteId' AND object_id = OBJECT_ID('dbo.ClientSitePatrolRandomisationSettings'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_ClientSitePatrolRandomisationSettings_ClientSiteId]
        ON [dbo].[ClientSitePatrolRandomisationSettings] ([ClientSiteId])
END
GO

IF OBJECT_ID('dbo.ClientSitePatrolRandomisationSchedules', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClientSitePatrolRandomisationSchedules]
    (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [SettingId]   INT NOT NULL,
        [DayOfWeek]   INT NOT NULL,       -- System.DayOfWeek: 0 = Sunday ... 6 = Saturday
        [StartTime]   TIME(0) NOT NULL,   -- site local
        [EndTime]     TIME(0) NOT NULL,   -- site local, exclusive
        CONSTRAINT [PK_ClientSitePatrolRandomisationSchedules] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ClientSitePatrolRandomisationSchedules_Settings] FOREIGN KEY ([SettingId])
            REFERENCES [dbo].[ClientSitePatrolRandomisationSettings] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_ClientSitePatrolRandomisationSchedules_DayOfWeek] CHECK ([DayOfWeek] BETWEEN 0 AND 6),
        CONSTRAINT [CK_ClientSitePatrolRandomisationSchedules_Window] CHECK ([EndTime] > [StartTime] AND DATEDIFF(MINUTE, [StartTime], [EndTime]) <= 120)
    )
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ClientSitePatrolRandomisationSchedules_Setting_Day' AND object_id = OBJECT_ID('dbo.ClientSitePatrolRandomisationSchedules'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_ClientSitePatrolRandomisationSchedules_Setting_Day]
        ON [dbo].[ClientSitePatrolRandomisationSchedules] ([SettingId], [DayOfWeek])
END
GO

IF OBJECT_ID('dbo.ClientSitePatrolRandomisationLogs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClientSitePatrolRandomisationLogs]
    (
        [Id]                 INT IDENTITY(1,1) NOT NULL,
        [ClientSiteId]       INT NOT NULL,
        [GuardId]            INT NOT NULL,
        [UserId]             INT NULL,            -- logged-in user on the handset, when there is one
        [SmartWandId]        INT NULL,

        -- The window this row belongs to (site local).
        [BusinessDate]       DATE NOT NULL,
        [DayOfWeek]          INT NOT NULL,
        [WindowStartLocal]   DATETIME2(0) NOT NULL,
        [WindowEndLocal]     DATETIME2(0) NOT NULL,
        [WindowEndUtc]       DATETIME2(0) NOT NULL,
        [SiteUtcOffset]      NVARCHAR(10) NULL,   -- "+11:00"

        -- The strike that produced the instruction (empty when the handset asked for a first checkpoint).
        [FromTagUId]         NVARCHAR(200) NULL,
        [FromTagLabel]       NVARCHAR(500) NULL,
        [FromHitLogId]       INT NULL,            -- ClientSiteSmartWandTagsHitLogs.Id
        [FromScanUtc]        DATETIME2(0) NULL,
        [FromScanLocal]      DATETIME2(0) NULL,
        [FromIsOffline]      BIT NULL,

        -- The instruction.
        [AssignedTagId]      INT NULL,            -- ClientSiteSmartWandTags.Id
        [AssignedTagUId]     NVARCHAR(200) NULL,
        [AssignedTagLabel]   NVARCHAR(500) NULL,
        [AssignedUtc]        DATETIME2(0) NULL,
        [AssignedLocal]      DATETIME2(0) NULL,

        [Status]             TINYINT NOT NULL CONSTRAINT [DF_ClientSitePatrolRandomisationLogs_Status] DEFAULT (0),

        -- The strike that answered it.
        [ScannedTagUId]      NVARCHAR(200) NULL,
        [ScannedTagLabel]    NVARCHAR(500) NULL,
        [ScannedHitLogId]    INT NULL,
        [ScannedUtc]         DATETIME2(0) NULL,
        [ScannedLocal]       DATETIME2(0) NULL,
        [ScannedIsOffline]   BIT NULL,
        [ScannedUserId]      INT NULL,

        [ResolvedUtc]        DATETIME2(0) NULL,
        [CreatedUtc]         DATETIME2(0) NOT NULL CONSTRAINT [DF_ClientSitePatrolRandomisationLogs_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ClientSitePatrolRandomisationLogs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ClientSitePatrolRandomisationLogs_ClientSites] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id]),
        CONSTRAINT [CK_ClientSitePatrolRandomisationLogs_Status] CHECK ([Status] BETWEEN 0 AND 5)
    )
END
GO

-- Per scan: "this guard's pending instruction on this site" and "tags handed out this window".
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClientSitePatrolRandomisationLogs_Site_Guard_Status' AND object_id = OBJECT_ID('dbo.ClientSitePatrolRandomisationLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ClientSitePatrolRandomisationLogs_Site_Guard_Status]
        ON [dbo].[ClientSitePatrolRandomisationLogs] ([ClientSiteId], [GuardId], [Status])
        INCLUDE ([AssignedUtc], [AssignedTagUId], [WindowEndUtc])
END
GO

-- The Randomisation report: sites over a range of business days.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClientSitePatrolRandomisationLogs_Site_BusinessDate' AND object_id = OBJECT_ID('dbo.ClientSitePatrolRandomisationLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ClientSitePatrolRandomisationLogs_Site_BusinessDate]
        ON [dbo].[ClientSitePatrolRandomisationLogs] ([ClientSiteId], [BusinessDate])
END
GO
