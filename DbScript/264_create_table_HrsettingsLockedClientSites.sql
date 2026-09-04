

/****** Object:  Table [dbo].[UserClientSiteAccess]    Script Date: 06-11-2024 13:03:13 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[HrSettingsLockedClientSites](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[HrSettingsId] [int] NOT NULL,
	[ClientSiteId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[HrSettingsLockedClientSites]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[HrSettingsLockedClientSites]  WITH CHECK ADD FOREIGN KEY([HrSettingsId])
REFERENCES [dbo].[HrSettings] ([Id])
ON DELETE CASCADE
GO


