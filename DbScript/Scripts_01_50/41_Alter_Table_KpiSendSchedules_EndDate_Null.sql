ALTER TABLE [dbo].[KpiSendSchedules] DROP CONSTRAINT [FK__KpiSendSc__Clien__2CF2ADDF]
GO

ALTER TABLE [dbo].[KpiSendSchedules]
ALTER COLUMN [EndDate] date null
GO

ALTER TABLE [dbo].[KpiSendSchedules]
ALTER COLUMN [ClientSiteId] int null
GO



