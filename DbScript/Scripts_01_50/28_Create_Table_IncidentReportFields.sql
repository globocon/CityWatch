CREATE TABLE [dbo].[IncidentReportFields]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[TypeId] [INT] NOT NULL,
	[Name] VARCHAR(50) NOT NULL,
	[EmailTo] VARCHAR(1024) NULL,
	CONSTRAINT UQ_IncidentReportField UNIQUE([TypeId], [Name])
)