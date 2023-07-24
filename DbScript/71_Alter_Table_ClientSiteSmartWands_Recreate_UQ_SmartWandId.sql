ALTER TABLE [dbo].[ClientSiteSmartWands]
DROP CONSTRAINT UQ_SmartWandId
GO

ALTER TABLE [dbo].[ClientSiteSmartWands]
ADD CONSTRAINT UQ_SmartWandId UNIQUE(ClientSiteId, SmartWandId)
GO