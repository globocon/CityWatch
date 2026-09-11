USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[KPITelematicsField]    Script Date: 19-12-2025 14:37:01 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SiteEquipmentsDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Brand] [varchar](max) NULL,
	[ClientSiteId] [int],
	[SerialNo] [varchar](max) NULL,
	[EquipmentId] [int] NULL,
	[IsDeleted] [bit] default 0
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


