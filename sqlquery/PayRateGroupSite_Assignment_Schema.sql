-- Create table for Pay Rate Group Site Assignments
CREATE TABLE [dbo].[PayRateGroupSite] (
    [Id]             INT IDENTITY (1, 1) NOT NULL,
    [PayRateGroupId] INT NOT NULL,
    [ClientSiteId]   INT NOT NULL,
    CONSTRAINT [PK_PayRateGroupSite] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PayRateGroupSite_PayRateGroups] FOREIGN KEY ([PayRateGroupId]) REFERENCES [dbo].[PayRateGroups] ([Id]),
    CONSTRAINT [FK_PayRateGroupSite_ClientSites] FOREIGN KEY ([ClientSiteId]) REFERENCES [dbo].[ClientSites] ([Id])
);
GO

-- Optional: Add index for performance on filtering by site
CREATE INDEX [IX_PayRateGroupSite_ClientSiteId] ON [dbo].[PayRateGroupSite] ([ClientSiteId]);
GO

-- Optional: Add index for performance on filtering by group
CREATE INDEX [IX_PayRateGroupSite_PayRateGroupId] ON [dbo].[PayRateGroupSite] ([PayRateGroupId]);
GO
