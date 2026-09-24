-- Migration script to update RosterRemunerationSummaries table

-- 1. Make GuardId Nullable
ALTER TABLE [dbo].[RosterRemunerationSummaries] ALTER COLUMN [GuardId] INT NULL;

-- 2. Add ProviderName if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND name = 'ProviderName')
BEGIN
    ALTER TABLE [dbo].[RosterRemunerationSummaries] ADD [ProviderName] NVARCHAR(255) NULL;
END

-- 3. Add TotalAmount if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND name = 'TotalAmount')
BEGIN
    ALTER TABLE [dbo].[RosterRemunerationSummaries] ADD [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0;
END

-- 4. Add CreatedDate if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE [dbo].[RosterRemunerationSummaries] ADD [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE();
END

-- 5. Add UpdatedDate if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND name = 'UpdatedDate')
BEGIN
    ALTER TABLE [dbo].[RosterRemunerationSummaries] ADD [UpdatedDate] DATETIME NULL;
END
GO
