
-- Create PcarVisitHistory table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PcarVisitHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PcarVisitHistory] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [VisitId] INT NOT NULL,
        [SmartWandId] INT NOT NULL,
        [SiteId] INT NOT NULL,
        [Action] VARCHAR(100) NOT NULL, -- e.g. "Accepted", "Cancelled", "Started", "Completed", "Pushed"
        [ServerUtcTime] DATETIME NOT NULL,
		[EventDateTimeLocal] DATETIME NULL,
		[EventDateTimeLocalWithOffset] DATETIMEOFFSET NULL,
		[EventDateTimeZone] VARCHAR(100) NULL,
		[EventDateTimeUtcOffsetMinute] INT NULL,		
        [EventDateTimeZoneShort] VARCHAR(50) NULL,
		[EventMobileUtcDateTime] DATETIME NULL,
        [CreatedAt] DATETIME DEFAULT GETDATE() NOT NULL
    );
END
GO
