
/****** Object:  Table [dbo].[ClientSiteManningKpiSettings]    Script Date: 17-08-2023 09:43:53 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Drop table [dbo].[ClientSiteManningKpiSettings]

CREATE TABLE [dbo].[ClientSiteManningKpiSettings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SettingsId] [int] NOT NULL,
	[WeekDay] [int] NOT NULL,
	[NoOfPatrols] [int] NULL,
	[EmpHoursStart] [varchar](10) NULL,
	[EmpHoursEnd] [varchar](10) NULL,
	[Type] [varchar](10) NULL,
	[PositionId] [int] NULL,
	[OrderId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ClientSiteManningKpiSettings]  WITH CHECK ADD FOREIGN KEY([PositionId])
REFERENCES [dbo].[IncidentReportPositions] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteManningKpiSettings]  WITH CHECK ADD FOREIGN KEY([SettingsId])
REFERENCES [dbo].[ClientSiteKpiSettings] ([Id])
ON DELETE CASCADE
GO


