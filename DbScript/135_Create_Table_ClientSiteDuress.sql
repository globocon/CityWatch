Create Table dbo.ClientSiteDuress
(
  Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  ClientSiteId  INT NOT NULL FOREIGN KEY REFERENCES ClientSites(Id), 
  [Enabled]  BIT  DEFAULT 0,
  ActivatedBy INT  FOREIGN KEY REFERENCES Guards(Id),
  ActivatedAt  DATETIME
)
GO