CREATE TABLE [dbo].[ANPR](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[profile] [varchar](50) NULL,
	[Apicalls] [nvarchar](max) NULL,
	[LaneLabel] [nvarchar](max) NULL,
	[IsDisabled] [bit] NULL,
	[IsSingleLane] [bit] NULL,
	[IsSeperateEntryAndExitLane] [bit] NULL,
)
