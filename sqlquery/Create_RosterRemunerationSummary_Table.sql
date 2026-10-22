IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RosterRemunerationSummaries] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [WeekStartDate] DATE NOT NULL,
        [GuardId] INT NULL,
        [ProviderName] NVARCHAR(255) NULL,
        [IsPaid] BIT NOT NULL DEFAULT 0,
        [Notes] NVARCHAR(MAX),
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME NULL
    );

    CREATE INDEX IX_RosterRemunerationSummaries_WeekStart ON [dbo].[RosterRemunerationSummaries] ([WeekStartDate]);
END
GO
