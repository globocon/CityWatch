-- Database Migration Script
-- Objective: Add ReasonOther fields to RosterSchedules and GuardUnavailabilities
-- Created: 2026-04-17

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = N'ReliefReasonOther')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules] ADD [ReliefReasonOther] NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GuardUnavailabilities]') AND name = N'ReasonOther')
BEGIN
    ALTER TABLE [dbo].[GuardUnavailabilities] ADD [ReasonOther] NVARCHAR(MAX) NULL;
END
GO
