-- Create RosterBinders and RosterBinderProjects tables for Grouping functionality
-- Sequential ID: 343

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterBinders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RosterBinders] (
        [Id]        INT            IDENTITY (1, 1) NOT NULL,
        [Name]      NVARCHAR (255) NOT NULL,
        [IsDeleted] BIT            DEFAULT ((0)) NOT NULL,
        CONSTRAINT [PK_RosterBinders] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterBinderProjects]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RosterBinderProjects] (
        [Id]             INT IDENTITY (1, 1) NOT NULL,
        [RosterBinderId] INT NOT NULL,
        [RosterGroupId]  INT NOT NULL,
        CONSTRAINT [PK_RosterBinderProjects] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RosterBinderProjects_RosterBinders_BinderId] FOREIGN KEY ([RosterBinderId]) REFERENCES [dbo].[RosterBinders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RosterBinderProjects_RosterGroups_GroupId] FOREIGN KEY ([RosterGroupId]) REFERENCES [dbo].[RosterGroups] ([Id]) ON DELETE CASCADE
    );
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterBinderProjects_RosterBinderId' AND object_id = OBJECT_ID('[dbo].[RosterBinderProjects]'))
    CREATE INDEX [IX_RosterBinderProjects_RosterBinderId] ON [dbo].[RosterBinderProjects] ([RosterBinderId] ASC);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RosterBinderProjects_RosterGroupId' AND object_id = OBJECT_ID('[dbo].[RosterBinderProjects]'))
    CREATE INDEX [IX_RosterBinderProjects_RosterGroupId] ON [dbo].[RosterBinderProjects] ([RosterGroupId] ASC);
