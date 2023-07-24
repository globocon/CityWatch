-- DailyClientSiteKpis
ALTER TABLE [dbo].[DailyClientSiteKpis] DROP CONSTRAINT [FK__DailyClie__Clien__54CB950F]
GO

ALTER TABLE [dbo].[DailyClientSiteKpis]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE
GO

-- KpiDataImportJobs
ALTER TABLE [dbo].[KpiDataImportJobs] DROP CONSTRAINT [FK__KpiDataIm__Clien__57A801BA]
GO

ALTER TABLE [dbo].[KpiDataImportJobs]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE
GO


-- IncidentReports
ALTER TABLE [dbo].[IncidentReports] DROP CONSTRAINT [FK__IncidentR__Clien__0880433F]
GO

ALTER TABLE [dbo].[IncidentReports]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE SET NULL
GO