USE [prod-citywatch]
GO

/****** Object:  Table [dbo].[ClientSiteManningKpiSettings]    Script Date: 01-01-2025 12:23:11 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ClientSiteManningKpiSettingsADHOC](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SettingsId] [int] NOT NULL,
	[WeekDay] [int] NOT NULL,
	[NoOfPatrols] [int] NULL,
	[EmpHoursStart] [varchar](10) NULL,
	[EmpHoursEnd] [varchar](10) NULL,
	[Type] [varchar](10) NULL,
	[PositionId] [int] NULL,
	[OrderId] [int] NULL,
	[IsPHO] [int] NULL,
	[CrmSupplier] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].ClientSiteManningKpiSettingsADHOC  WITH CHECK ADD FOREIGN KEY([PositionId])
REFERENCES [dbo].[IncidentReportPositions] ([Id])
GO

ALTER TABLE [dbo].ClientSiteManningKpiSettingsADHOC  WITH CHECK ADD FOREIGN KEY([SettingsId])
REFERENCES [dbo].[ClientSiteKpiSettings] ([Id])
GO


ALTER TABLE [dbo].[ClientSiteKpiSettings]
ADD ScheduleisActiveADHOC bit NOT NULL DEFAULT 0;

