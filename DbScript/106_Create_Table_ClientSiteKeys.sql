CREATE TABLE dbo.ClientSiteKeys
(
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	ClientSiteId int NOT NULL FOREIGN KEY REFERENCES dbo.ClientSites(Id),
	[KeyNo] VARCHAR(15)  NOT NULL,
	Description VARCHAR(50)  NULL 
	CONSTRAINT UQ_keyno_clientsiteid UNIQUE (KeyNo,ClientSiteId)
)