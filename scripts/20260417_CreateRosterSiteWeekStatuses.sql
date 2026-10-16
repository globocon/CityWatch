IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RosterSiteWeekStatuses')
BEGIN
    CREATE TABLE [dbo].[RosterSiteWeekStatuses] (
        [Id]           INT            IDENTITY (1, 1) NOT NULL,
        [ClientSiteId] INT            NOT NULL,
        [StartDate]    DATETIME2 (7)  NOT NULL,
        [Status]       NVARCHAR (50)  NULL,
        [UpdatedDate]  DATETIME2 (7)  NOT NULL,
        [UpdatedBy]    NVARCHAR (255) NULL,
        CONSTRAINT [PK_RosterSiteWeekStatuses] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RosterSiteWeekStatuses_ClientSites_ClientSiteId] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_RosterSiteWeekStatuses_ClientSiteId] ON [dbo].[RosterSiteWeekStatuses]([ClientSiteId] ASC);
END
GO
