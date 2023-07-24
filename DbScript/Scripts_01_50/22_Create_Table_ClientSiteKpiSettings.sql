CREATE TABLE [dbo].[ClientSiteKpiSettings]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ClientSiteId] [int] NOT NULL FOREIGN KEY REFERENCES [ClientSites] ([Id]) ON DELETE CASCADE,
	[KoiosClientSiteId] [int] NULL,
	[DropboxImagesDir] VARCHAR (1024) NULL,
	[DailyImagesTarget] DECIMAL(5,2) NULL,
	[DailyWandScansTarget] DECIMAL(5,2) NULL,	
	[MonEmpHrs] DECIMAL(5, 2) NULL,
	[TueEmpHrs] DECIMAL(5, 2) NULL,
	[WedEmpHrs] DECIMAL(5, 2) NULL,
	[ThuEmpHrs] DECIMAL(5, 2) NULL,
	[FriEmpHrs] DECIMAL(5, 2) NULL,
	[SatEmpHrs] DECIMAL(5, 2) NULL,
	[SunEmpHrs] DECIMAL(5, 2) NULL
)