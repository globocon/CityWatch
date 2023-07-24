
CREATE TABLE dbo.ClientSitePocs
(
	Id int identity(1,1) NOT NULL PRIMARY KEY,
	[Name] varchar(50) NOT NULL,
	ClientSiteId int NOT NULL,
	IsDeleted bit DEFAULT 0,
	CONSTRAINT UQ_SitePoc_Name UNIQUE ([Name], ClientSiteId),
	CONSTRAINT FK_ClientSitePoc_ClientSiteId FOREIGN Key (ClientSiteId) REFERENCES ClientSites (Id)
)
GO



CREATE TABLE dbo.ClientSiteLocations
(
	Id int identity(1,1) NOT NULL PRIMARY KEY,
	[Name] varchar(50) NOT NULL,
	ClientSiteId int NOT NULL,
	IsDeleted bit DEFAULT 0,
	CONSTRAINT UQ_SiteLocation_Name UNIQUE ([Name], ClientSiteId),
	CONSTRAINT FK_ClientSiteLocation_ClientSiteId FOREIGN Key (ClientSiteId) REFERENCES ClientSites (Id)
)
GO