-- Create RosterRemunerationSummaries table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RosterRemunerationSummaries] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [WeekStartDate] DATE NOT NULL,
        [GuardId] INT NOT NULL,
        [IsPaid] BIT NOT NULL DEFAULT 0,
        [Notes] NVARCHAR(MAX) NULL,
        [TotalAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0,
        CONSTRAINT [FK_RosterRemunerationSummaries_Guards] FOREIGN KEY ([GuardId]) REFERENCES [dbo].[Guards]([Id])
    );

    CREATE UNIQUE INDEX [IX_RosterRemunerationSummaries_Week_Guard] ON [dbo].[RosterRemunerationSummaries]([WeekStartDate], [GuardId]);
END
GO
