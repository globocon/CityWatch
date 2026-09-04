USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[KpiSendTimesheetSchedules]    Script Date: 18-08-2025 11:27:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[KpiSendKVSchedules](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StartDate] [date] NOT NULL,
	[EndDate] [date] NULL,
	[Frequency] [int] NOT NULL,
	[Time] [char](5) NULL,
	[EmailTo] [varchar](5000) NULL,
	[NextRunOn] [datetime] NOT NULL,
	[ProjectName] [varchar](50) NULL,
	[EmailBcc] [varchar](5000) NULL,
	[VehicleRego] [varchar](50) null,
	[KeyNo] [varchar](1024) NULL,
	[CompanyName] [varchar](100) NULL,
	[ClientSiteLocationId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


