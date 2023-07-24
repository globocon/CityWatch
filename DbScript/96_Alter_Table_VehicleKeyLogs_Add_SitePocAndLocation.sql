ALTER TABLE [dbo].[VehicleKeyLogs]
ADD ClientSitePocId INT NULL,
CONSTRAINT FK_ClientSitePocId FOREIGN KEY (ClientSitePocId) REFERENCES ClientSitePocs(Id) 
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
ADD ClientSiteLocationId INT NULL, 
CONSTRAINT FK_ClientSiteLocationId FOREIGN KEY (ClientSiteLocationId) REFERENCES ClientSiteLocations(Id) 
GO

