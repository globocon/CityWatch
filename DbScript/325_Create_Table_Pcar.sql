
CREATE TABLE PcarRouteDailyVisits
(
    Id INT IDENTITY(1,1) PRIMARY KEY,

    -- Basic Identifiers
    SmartWandId INT NOT NULL,
    SiteId INT NOT NULL,
    GuardId INT NOT NULL,

    -- Login context (for audit/reporting)
    LoginUserId INT NOT NULL,
    LoginSiteId INT NOT NULL,

    -- Visit Details
    VisitName NVARCHAR(100) NOT NULL,
    VisitNumber INT NOT NULL,
    DayName NVARCHAR(20) NOT NULL,

    PcarRouteId INT NOT NULL,
    PcarRouteDetailsId INT NOT NULL,

    -- Visit Times (string input from app – convert to time if needed)
    TimeOn VARCHAR(10) NULL,
    TimeOff VARCHAR(10) NULL,

    -- GPS from device (single string)
    GpsCoordinates NVARCHAR(100) NULL,

    -- Auto tracking
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
