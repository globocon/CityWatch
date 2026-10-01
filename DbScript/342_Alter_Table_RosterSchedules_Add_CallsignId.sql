IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = 'CallsignId')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules] ADD [CallsignId] INT NULL;
    
    ALTER TABLE [dbo].[RosterSchedules] WITH CHECK ADD CONSTRAINT [FK_RosterSchedules_IncidentReportFields_CallsignId] 
    FOREIGN KEY([CallsignId]) REFERENCES [dbo].[IncidentReportFields] ([Id]);
    
    ALTER TABLE [dbo].[RosterSchedules] CHECK CONSTRAINT [FK_RosterSchedules_IncidentReportFields_CallsignId];
END
GO
