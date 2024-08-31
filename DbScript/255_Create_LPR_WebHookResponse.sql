USE [prod-citywatch]
GO

/****** Object:  Table [dbo].[LprWebhookResponse]    Script Date: 31-08-2024 22:51:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LprWebhookResponse](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[license_plate_number] [nvarchar](max) NULL,
	[created] [nvarchar](50) NULL,
	[camera_id] [nvarchar](50) NULL,
	[webhook_id] [nvarchar](50) NULL,
	[CrDateTime] [datetime] NULL,
	[ReadStatus] [int] NULL,
 CONSTRAINT [PK_LprWebhookResponse] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


