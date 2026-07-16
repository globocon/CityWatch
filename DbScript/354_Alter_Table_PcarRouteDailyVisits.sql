Alter Table [PcarRouteDailyVisits]
Add [Status] int null,
[VisitDate] DATETIME NOT NULL,
[ParentVisitId] INT NULL,
[IsVisitPickedUp] bit NOT NULL DEFAULT 0;


ALTER TABLE [PcarRouteDailyVisits]
ALTER COLUMN [GuardId] INT NULL;

ALTER TABLE [PcarRouteDailyVisits]
ALTER COLUMN [LoginUserId] INT NULL;

ALTER TABLE [PcarRouteDailyVisits]
ALTER COLUMN [LoginSiteId] INT NULL;
