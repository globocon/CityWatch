CREATE TABLE [dbo].[DailyClientSiteKpis]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ClientSiteId] [int] NOT NULL FOREIGN KEY REFERENCES [ClientSites] ([Id]),
	[Date] [date] NOT NULL,
	[EmployeeHours] [decimal](18, 0) NULL,
	[ImageCount] [int] NULL,
	[WandScanCount] [int] NULL,
	[IncidentCount] [int] NULL
)