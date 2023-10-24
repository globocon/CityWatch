USE [CityWatchDb]
GO
/****** Object:  Table [dbo].[GuardAccess]    Script Date: 24/10/2023 8:27:00 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GuardAccess](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AccessName] [varchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[GuardAccess] ON 

INSERT [dbo].[GuardAccess] ([Id], [AccessName]) VALUES (1, N'LB,KV,IR')
INSERT [dbo].[GuardAccess] ([Id], [AccessName]) VALUES (2, N'STATS')
INSERT [dbo].[GuardAccess] ([Id], [AccessName]) VALUES (3, N'KPI')
INSERT [dbo].[GuardAccess] ([Id], [AccessName]) VALUES (4, N'RC')
SET IDENTITY_INSERT [dbo].[GuardAccess] OFF
GO

ALTER TABLE Guards
ADD IsLB_KV_IR bit;

ALTER TABLE Guards
ADD IsSTATS bit;
