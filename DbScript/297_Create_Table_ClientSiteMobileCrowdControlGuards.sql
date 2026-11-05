
CREATE TABLE [ClientSiteMobileCrowdControlGuards](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CrowdControlId] [int] NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[GuardId] [int] NOT NULL,
	[PCount] [int] NOT NULL,
	[CrowdControlDate] [date] NULL,
	[GuardLastUpdateTime] [datetime] NULL,
 CONSTRAINT [PK_ClientSiteMobileCrowdControlGuards] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [ClientSiteMobileCrowdControlGuards] ADD  CONSTRAINT [DF_ClientSiteMobileCrowdControlGuards_PCount]  DEFAULT ((0)) FOR [PCount]
GO


