-- SQL Script to update RosterSchedules table for Relief Guard feature
-- Run this script in SQL Server Management Studio (SSMS) against your CityWatch database

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = N'ReliefGuardId')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules] ADD [ReliefGuardId] INT NULL;
    
    -- Add Foreign Key constraint
    ALTER TABLE [dbo].[RosterSchedules] WITH CHECK ADD CONSTRAINT [FK_RosterSchedules_Guards_ReliefGuardId] 
    FOREIGN KEY([ReliefGuardId]) REFERENCES [dbo].[Guards] ([Id]);
    
    ALTER TABLE [dbo].[RosterSchedules] CHECK CONSTRAINT [FK_RosterSchedules_Guards_ReliefGuardId];
    
    PRINT 'Added ReliefGuardId and Foreign Key to RosterSchedules.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = N'ReliefProviderName')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules] ADD [ReliefProviderName] NVARCHAR(255) NULL;
    PRINT 'Added ReliefProviderName to RosterSchedules.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = N'ReliefReason')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules] ADD [ReliefReason] NVARCHAR(MAX) NULL;
    PRINT 'Added ReliefReason to RosterSchedules.';
END
GO
