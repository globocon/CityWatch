USE [prod-citywatch]
GO

/****** Object:  Table [dbo].[RCActionList]    Script Date: 20/12/2023 3:31:27 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[RCActionList](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SiteAlarmKeypadCode] [varchar](max) NULL,
	[Action1] [varchar](max) NULL,
	[Sitephysicalkey] [varchar](max) NULL,
	[Action2] [varchar](max) NULL,
	[SiteCombinationLook] [varchar](max) NULL,
	[Action3] [varchar](max) NULL,
	[ControlRoomOperator] [varchar](max) NULL,
	[SettingsId] [int] NOT NULL,
	[Action4] [varchar](max) NULL,
	[Action5] [varchar](max) NULL,
	[Imagepath] [varchar](max) NULL,
	[DateandTimeUpdated] [varchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[RCActionList]  WITH CHECK ADD FOREIGN KEY([SettingsId])
REFERENCES [dbo].[ClientSiteKpiSettings] ([Id])
ON DELETE CASCADE
GO


