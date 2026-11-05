IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = N'AdhocOffsiteText')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules]
    ADD [AdhocOffsiteText] NVARCHAR(255) NULL;
END
GO
