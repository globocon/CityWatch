/*
    Change history for ClientSiteSmartWandTags.

    Why this exists: a wand tag can be added, edited, soft-deleted and re-added, and nothing
    recorded who did it or when. That cost real reporting data - "Locked (NEW)" on Casey Fields
    Oval 2 sat with every one of its registrations deleted for about three weeks, so every scan
    of it was saved as an unrecognised tag and never reached a report. Nobody could tell when it
    was deleted, by whom, or why.

    The rows are written by the application (ClientSiteWandDataProvider), not by a trigger, so
    each one also carries which app made the change and what version it was running. An earlier
    version of this script created Trigger_ClientSiteSmartWandTagsHist on ClientSiteSmartWandTags
    for the same purpose; this script drops it if it is there.

    One row per tag per change. Old* and New* hold the full before and after, so the exact edit
    is recoverable; Note is the same thing in a sentence, for reading rather than querying.

    Safe to run more than once, and safe on a database that already has the earlier version of
    this table: it drops the old trigger and adds only what is missing.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/* The history is written from application code now. */
IF OBJECT_ID('dbo.Trigger_ClientSiteSmartWandTagsHist', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[Trigger_ClientSiteSmartWandTagsHist]
GO

IF OBJECT_ID('dbo.ClientSiteSmartWandTagsHist', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClientSiteSmartWandTagsHist](
        [Id] [int] IDENTITY(1,1) NOT NULL,

        -- The tag this change was made to (ClientSiteSmartWandTags.Id). Deliberately not a
        -- foreign key: the history must survive the tag row being hard-deleted.
        [TagId] [int] NOT NULL,

        -- Added / Changed / Deleted / Restored.
        [Action] [nvarchar](20) NOT NULL,

        [ChangedOn] [datetime] NOT NULL,

        -- Whoever the application said was acting. Null when it could not say.
        [ChangedByUserId] [int] NULL,
        [ChangedByGuardId] [int] NULL,

        -- Readable actor: the user or guard name.
        [ChangedBy] [nvarchar](200) NULL,

        -- Which application the change came from: Web, Android or iOS.
        [AppType] [nvarchar](20) NULL,

        -- The build that application was running, e.g. "1.54.2". Null when it did not say.
        [AppVersion] [nvarchar](50) NULL,

        -- The change in a sentence, e.g. "Label changed from 'Close' to 'Locked (NEW)'".
        [Note] [nvarchar](1000) NULL,

        -- Full before/after. Old* is null on an insert.
        [OldClientSiteId] [int] NULL,
        [NewClientSiteId] [int] NULL,
        [OldUId] [nvarchar](60) NULL,
        [NewUId] [nvarchar](60) NULL,
        [OldTagsTypeId] [int] NULL,
        [NewTagsTypeId] [int] NULL,
        [OldLabelDescription] [nvarchar](max) NULL,
        [NewLabelDescription] [nvarchar](max) NULL,
        [OldIsDeleted] [bit] NULL,
        [NewIsDeleted] [bit] NULL,
        [OldFqBypass] [bit] NULL,
        [NewFqBypass] [bit] NULL,

     CONSTRAINT [PK_ClientSiteSmartWandTagsHist] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) TEXTIMAGE_ON [PRIMARY]
END
GO

/* Present only where the earlier version of this script has already run. */
IF COL_LENGTH('dbo.ClientSiteSmartWandTagsHist', 'AppType') IS NULL
    ALTER TABLE [dbo].[ClientSiteSmartWandTagsHist] ADD [AppType] [nvarchar](20) NULL
GO

IF COL_LENGTH('dbo.ClientSiteSmartWandTagsHist', 'AppVersion') IS NULL
    ALTER TABLE [dbo].[ClientSiteSmartWandTagsHist] ADD [AppVersion] [nvarchar](50) NULL
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_ClientSiteSmartWandTagsHist_ChangedOn')
    ALTER TABLE [dbo].[ClientSiteSmartWandTagsHist] ADD CONSTRAINT [DF_ClientSiteSmartWandTagsHist_ChangedOn] DEFAULT (getdate()) FOR [ChangedOn]
GO

-- "What happened to this tag", which is how the table is read.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClientSiteSmartWandTagsHist_Tag' AND object_id = OBJECT_ID('dbo.ClientSiteSmartWandTagsHist'))
    CREATE NONCLUSTERED INDEX [IX_ClientSiteSmartWandTagsHist_Tag]
    ON [dbo].[ClientSiteSmartWandTagsHist]([TagId] ASC, [ChangedOn] DESC)
GO

-- "What changed recently", and the lookup behind a tag's UId across re-registrations.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ClientSiteSmartWandTagsHist_Recent' AND object_id = OBJECT_ID('dbo.ClientSiteSmartWandTagsHist'))
    CREATE NONCLUSTERED INDEX [IX_ClientSiteSmartWandTagsHist_Recent]
    ON [dbo].[ClientSiteSmartWandTagsHist]([ChangedOn] DESC)
    INCLUDE ([NewUId], [OldUId], [Action])
GO
