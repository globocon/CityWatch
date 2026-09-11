IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterGroups]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[RosterGroups] (
    [Id]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (255) NOT NULL,
    [IsDeleted] BIT            DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RosterGroups] PRIMARY KEY CLUSTERED ([Id] ASC)
);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterGroupSites]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[RosterGroupSites] (
    [Id]            INT IDENTITY (1, 1) NOT NULL,
    [RosterGroupId] INT NOT NULL,
    [ClientSiteId]  INT NOT NULL,
    CONSTRAINT [PK_RosterGroupSites] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RosterGroupSites_RosterGroups_RosterGroupId] FOREIGN KEY ([RosterGroupId]) REFERENCES [dbo].[RosterGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RosterGroupSites_ClientSites_ClientSiteId] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE
);
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[RosterSchedules] (
    [Id]            INT            IDENTITY (1, 1) NOT NULL,
    [RosterGroupId] INT            NOT NULL,
    [ClientSiteId]  INT            NOT NULL,
    [GuardId]       INT            NULL,
    [ProviderName]  NVARCHAR (255) NULL,
    [ShiftStart]    DATETIME2 (7)  NOT NULL,
    [ShiftEnd]      DATETIME2 (7)  NOT NULL,
    [Status]        INT            NOT NULL,
    [IsDeleted]     BIT            DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RosterSchedules] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RosterSchedules_RosterGroups_RosterGroupId] FOREIGN KEY ([RosterGroupId]) REFERENCES [dbo].[RosterGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RosterSchedules_ClientSites_ClientSiteId] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RosterSchedules_Guards_GuardId] FOREIGN KEY ([GuardId]) REFERENCES [dbo].[Guards] ([Id])
);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterGroupSites_RosterGroupId' AND object_id = OBJECT_ID('[dbo].[RosterGroupSites]'))
    CREATE INDEX [IX_RosterGroupSites_RosterGroupId] ON [dbo].[RosterGroupSites] ([RosterGroupId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterGroupSites_ClientSiteId' AND object_id = OBJECT_ID('[dbo].[RosterGroupSites]'))
    CREATE INDEX [IX_RosterGroupSites_ClientSiteId] ON [dbo].[RosterGroupSites] ([ClientSiteId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterSchedules_RosterGroupId' AND object_id = OBJECT_ID('[dbo].[RosterSchedules]'))
    CREATE INDEX [IX_RosterSchedules_RosterGroupId] ON [dbo].[RosterSchedules] ([RosterGroupId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterSchedules_ClientSiteId' AND object_id = OBJECT_ID('[dbo].[RosterSchedules]'))
    CREATE INDEX [IX_RosterSchedules_ClientSiteId] ON [dbo].[RosterSchedules] ([ClientSiteId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterSchedules_GuardId' AND object_id = OBJECT_ID('[dbo].[RosterSchedules]'))
    CREATE INDEX [IX_RosterSchedules_GuardId] ON [dbo].[RosterSchedules] ([GuardId] ASC);
