USE [prod-citywatch]
GO

/****** Object:  Table [dbo].[RCLinkedDuressMaster]    Script Date: 03-06-2024 23:38:46 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[RCLinkedDuressMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GroupName] [varchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


/****** Object:  Table [dbo].[RCLinkedDuressClientSites]    Script Date: 03-06-2024 23:38:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[RCLinkedDuressClientSites](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RCLinkedId] [int] NOT NULL,
	[ClientSiteId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[RCLinkedDuressClientSites]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RCLinkedDuressClientSites]  WITH CHECK ADD FOREIGN KEY([RCLinkedId])
REFERENCES [dbo].[RCLinkedDuressMaster] ([Id])
ON DELETE CASCADE
GO


ALTER TABLE ClientSiteDuress ADD [LinkedDuressParentSiteId] int ;
ALTER TABLE ClientSiteDuress ADD [IsLinkedDuressParentSite] int ;