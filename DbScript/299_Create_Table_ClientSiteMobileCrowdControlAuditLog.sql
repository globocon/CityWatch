


CREATE TABLE [ClientSiteMobileCrowdControlAuditLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NULL,
	[ActionTimeServer] [datetime] NOT NULL,
	[ActionTimeUTC] [datetime] NOT NULL,
	[ActionTimeLocal] [datetime] NULL,
	[TimeUTC] [varchar](max) NULL,
	[ActionDescription] [varchar](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


