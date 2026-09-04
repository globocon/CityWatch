ALTER TABLE [dbo].[ClientSiteManningKpiSettingsADHOC]
ADD WeekAdhocToBeValid datetime 

ALTER TABLE [dbo].[ClientSiteManningKpiSettingsADHOC]
ADD IsExtraShiftEnabled bit NOT NULL DEFAULT(1)