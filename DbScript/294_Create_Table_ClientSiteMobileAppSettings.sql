
/****** Object:  Table [dbo].[ClientSiteMobileAppSettings]    Script Date: 08/05/2025 08:49:06 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [ClientSiteMobileAppSettings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[IsCrowdCountEnabled] [bit] NOT NULL,
	[IsDoorEnabled] [bit] NOT NULL,
	[IsGateEnabled] [bit] NOT NULL,
	[IsLevelFloorEnabled] [bit] NOT NULL,
	[IsRoomEnabled] [bit] NOT NULL,
	[CounterQuantity] [int] NOT NULL,
 CONSTRAINT [PK_ClientSiteMobileAppSettings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [ClientSiteMobileAppSettings] ADD  CONSTRAINT [DF_ClientSiteMobileAppSettings_IsCrowdCountEnabled]  DEFAULT ((0)) FOR [IsCrowdCountEnabled]
GO

ALTER TABLE [ClientSiteMobileAppSettings] ADD  CONSTRAINT [DF_ClientSiteMobileAppSettings_IsDoorEnabled]  DEFAULT ((0)) FOR [IsDoorEnabled]
GO

ALTER TABLE [ClientSiteMobileAppSettings] ADD  CONSTRAINT [DF_ClientSiteMobileAppSettings_IsDoorEnabled1]  DEFAULT ((0)) FOR [IsGateEnabled]
GO

ALTER TABLE [ClientSiteMobileAppSettings] ADD  CONSTRAINT [DF_ClientSiteMobileAppSettings_IsGateEnabled1]  DEFAULT ((0)) FOR [IsLevelFloorEnabled]
GO

ALTER TABLE [ClientSiteMobileAppSettings] ADD  CONSTRAINT [DF_ClientSiteMobileAppSettings_IsLevelFloorEnabled1]  DEFAULT ((0)) FOR [IsRoomEnabled]
GO

ALTER TABLE [ClientSiteMobileAppSettings] ADD  CONSTRAINT [DF_ClientSiteMobileAppSettings_CounterQuantity]  DEFAULT ((0)) FOR [CounterQuantity]
GO

ALTER TABLE [ClientSiteMobileAppSettings]  WITH CHECK ADD  CONSTRAINT [FK_ClientSiteMobileAppSettings_ClientSites] FOREIGN KEY([Id])
REFERENCES [ClientSites] ([Id])
GO

ALTER TABLE [ClientSiteMobileAppSettings] CHECK CONSTRAINT [FK_ClientSiteMobileAppSettings_ClientSites]
GO


