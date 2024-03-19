USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[ClientSiteKeys]    Script Date: 13-03-2024 12:16:44 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ClientSiteToggle](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[ToggleTypeId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL DEFAULT 0)

ALTER TABLE [dbo].[ClientSiteToggle]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id])
GO


