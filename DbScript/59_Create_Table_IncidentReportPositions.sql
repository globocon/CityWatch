CREATE TABLE [dbo].[IncidentReportPositions]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,	
	[Name] VARCHAR(50) NOT NULL,
	[EmailTo] VARCHAR(1024) NULL,
	[IsPatrolCar] BIT NOT NULL DEFAULT 0,
	[DropboxDir] VARCHAR(1024) NULL,
	CONSTRAINT UQ_PositionName UNIQUE([Name])
)