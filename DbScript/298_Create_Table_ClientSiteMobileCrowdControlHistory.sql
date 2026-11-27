


CREATE TABLE [ClientSiteMobileCrowdControlHistory](
	[HistoryId] [int] IDENTITY(1,1) NOT NULL,
	[Id] [int] NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[Tcount] [int] NOT NULL,
	[Ccount] [int] NOT NULL,
	[CrowdControlDate] [date] NULL,
	[LastUpdateTime] [datetime] NULL,
	[ArchivedOn] [datetime] NULL,
	[ArchivedMode] [varchar](250) NULL,
	[ArchivedUserId] [int] NULL,
	[ArchivedGuardId] [int] NULL,
 CONSTRAINT [PK_ClientSiteMobileCrowdControlHistory] PRIMARY KEY CLUSTERED 
(
	[HistoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO




