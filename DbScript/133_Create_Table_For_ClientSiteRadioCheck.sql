Create Table dbo.ClientSiteActivityStatus
(
  Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  ClientSiteId INT NOT NULL FOREIGN KEY REFERENCES ClientSites(Id),  
  GuardId INT NULL FOREIGN KEY REFERENCES Guards(Id),
  [Status] BIT NULL,
  LastActiveSrcId INT NULL,
  LastActiveAt DATETIME NULL,
  LastActiveDescription VARCHAR(1024) NULL
)
GO

Create Table dbo.ClientSiteRadioChecks
(
  Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  ClientSiteId INT NOT NULL  FOREIGN KEY REFERENCES ClientSites(Id),
  GuardId INT  NOT NULL FOREIGN KEY REFERENCES Guards(Id),
  CheckedAt DATETIME NOT NULL,
  [Status] VARCHAR(25) NOT NULL
)
GO
