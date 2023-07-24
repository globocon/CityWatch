ALTER TABLE [dbo].[IncidentReports]
ADD [OccurNo] VARCHAR(15) NULL,
	[ActionTaken] VARCHAR(MAX) NULL,
	[IsPatrol] BIT NOT NULL DEFAULT 0,
	[Position] VARCHAR(50) NULL
GO

ALTER TABLE [dbo].[IncidentReports]
ADD ClientArea VARCHAR(256) NULL
GO