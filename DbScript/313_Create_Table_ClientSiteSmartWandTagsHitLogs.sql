
CREATE TABLE [ClientSiteSmartWandTagsHitLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LoggedInClientSiteId] [int] NOT NULL,
	[LoggedInUserId] [int] NOT NULL,
	[LoggedInGuardId] [int] NOT NULL,
	[TagUId] [nvarchar](30) NOT NULL,
	[TagsTypeId] [int] NULL,
	[LabelDescription] [nvarchar](max) NULL,
	[TagLinkedClientSiteId] [int] NULL,
	[HitUtcDateTime] [datetime] NOT NULL,
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


