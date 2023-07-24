﻿CREATE TABLE [dbo].IncidentReports (
    Id INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [FileName] VARCHAR(1024) NOT NULL,
    CreatedOn DATETIME NOT NULL,    
    ClientSiteId INT NULL FOREIGN KEY REFERENCES ClientSites(Id)
);