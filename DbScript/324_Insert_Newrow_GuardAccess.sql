INSERT INTO GuardAccess (AccessName)
VALUES ('PCAR');

ALTER TABLE [dbo].[Guards]
ADD IsPCARAccess BIT NOT NULL DEFAULT 0;


CREATE TABLE PcarRoute
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Pcarroutename NVARCHAR(max) NOT NULL,
    Smartwandallocation INT NOT NULL,
    CreatedOn DATETIME NOT NULL DEFAULT GETDATE()
);




-- Create new table
CREATE TABLE dbo.PcarRouteDetails
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PcarRouteId INT NOT NULL,
    ClientSiteId INT NOT NULL,
	CreatedOn DATETIME NOT NULL DEFAULT GETDATE(),
    -- Daily Schedule Times as strings
    StartMon VARCHAR(5), EndMon VARCHAR(5), VisitMon INT,
    StartTue VARCHAR(5), EndTue VARCHAR(5), VisitTue INT,
    StartWed VARCHAR(5), EndWed VARCHAR(5), VisitWed INT,
    StartThu VARCHAR(5), EndThu VARCHAR(5), VisitThu INT,
    StartFri VARCHAR(5), EndFri VARCHAR(5), VisitFri INT,
    StartSat VARCHAR(5), EndSat VARCHAR(5), VisitSat INT,
    StartSun VARCHAR(5), EndSun VARCHAR(5), VisitSun INT,
    StartPho VARCHAR(5), EndPho VARCHAR(5), VisitPho INT,

    CONSTRAINT FK_PcarRouteDetails_PcarRoute FOREIGN KEY (PcarRouteId) REFERENCES dbo.PcarRoute(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PcarRouteDetails_ClientSite FOREIGN KEY (ClientSiteId) REFERENCES dbo.ClientSites(Id)
);
GO



