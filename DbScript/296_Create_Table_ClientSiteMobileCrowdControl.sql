

CREATE TABLE [ClientSiteMobileCrowdControl](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[Tcount] [int] NOT NULL,
	[Ccount] [int] NOT NULL,
	[CrowdControlDate] [date] NULL,
	[LastUpdateTime] [datetime] NULL,
 CONSTRAINT [PK_ClientSiteMobileCrowdControl] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [ClientSiteMobileCrowdControl] ADD  DEFAULT ((0)) FOR [Tcount]
GO

ALTER TABLE [ClientSiteMobileCrowdControl] ADD  DEFAULT ((0)) FOR [Ccount]
GO


