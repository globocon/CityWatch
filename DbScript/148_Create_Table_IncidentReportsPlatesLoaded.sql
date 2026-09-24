USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[IncidentReportsPlatesLoaded]    Script Date: 04-09-2023 11:12:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[IncidentReportsPlatesLoaded](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IncidentReportId] [int] NULL,
	[PlateId] [int] NULL,
	[TruckNo] [nvarchar](max) NULL,
	[LogId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


