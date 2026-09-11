USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[VehicleKeyLogs]    Script Date: 03-03-2026 09:05:56 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[VehicleKeyLogsPax](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[KeyVehicleLogId] [int] NOT NULL,

	[PersonName] [varchar](100) NULL,
	[PersonType] [int] NULL,
	[MobileNumber] [varchar](20) NULL,
	
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO

