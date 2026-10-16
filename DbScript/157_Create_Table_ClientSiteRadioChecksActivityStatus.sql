

/****** Object:  Table [dbo].[ClientSiteRadioChecksActivityStatus]    Script Date: 13-10-2023 13:00:37 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE TABLE [dbo].[ClientSiteRadioChecksActivityStatus](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NULL,
	[GuardId] [int] NULL,
	[LastIRCreatedTime] [datetime] NULL,
	[LastKVCreatedTime] [datetime] NULL,
	[LastLBCreatedTime] [datetime] NULL,
	[GuardLoginTime] [datetime] NULL,
	[GuardLogoutTime] [datetime] NULL,
	[ActivityType] [nchar](10) NULL,
	[LBId] [int] NULL,
	[IRId] [int] NULL,
	[KVId] [int] NULL,
	[LastSWCreatedTime] [datetime] NULL,
	[SWId] [int] NULL,
	[ActivityDescription] [nvarchar](max) NULL,
 CONSTRAINT [PK_ClientSiteRadioChecksActivityStatus] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus]  WITH CHECK ADD  CONSTRAINT [FK_ClientSiteId] FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus] CHECK CONSTRAINT [FK_ClientSiteId]
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus]  WITH CHECK ADD  CONSTRAINT [FK_GuardId] FOREIGN KEY([GuardId])
REFERENCES [dbo].[Guards] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus] CHECK CONSTRAINT [FK_GuardId]
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus]  WITH CHECK ADD  CONSTRAINT [FK_IRId] FOREIGN KEY([IRId])
REFERENCES [dbo].[IncidentReports] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus] CHECK CONSTRAINT [FK_IRId]
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus]  WITH CHECK ADD  CONSTRAINT [FK_KVId] FOREIGN KEY([KVId])
REFERENCES [dbo].[VehicleKeyLogs] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus] CHECK CONSTRAINT [FK_KVId]
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus]  WITH CHECK ADD  CONSTRAINT [FK_LBId] FOREIGN KEY([LBId])
REFERENCES [dbo].[GuardLogs] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteRadioChecksActivityStatus] CHECK CONSTRAINT [FK_LBId]
GO


