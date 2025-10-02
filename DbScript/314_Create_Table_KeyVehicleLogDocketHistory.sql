USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[KeyVehicleLogAuditHistory]    Script Date: 02-10-2025 09:07:07 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[KeyVehicleLogDocketHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[KeyVehicleLogId] [int] NOT NULL,
[DocketSerialNo] [varchar](50) NULL,
	[FileName] [nvarchar](max) NOT NULL,
	[DocketReason] [nvarchar](max)NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

