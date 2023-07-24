CREATE TABLE [dbo].[KpiDataImportJobs]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ClientSiteId] [int] NOT NULL FOREIGN KEY REFERENCES [ClientSites] ([Id]),
	[ReportDate] [date] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CompletedDate] [datetime] NULL,
	[Success] [bit] NULL
)