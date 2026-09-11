

CREATE TABLE [SiteEventLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	[SiteId] [int] NULL,
	[GuardName] [nvarchar](max) NULL,
	[SiteName] [nvarchar](max) NULL,
	[ProjectName] [nvarchar](50) NULL,
	[ActivityType] [nvarchar](50) NULL,
	[Module] [nvarchar](50) NULL,
	[SubModule] [nvarchar](50) NULL,
	[GoogleMapCoordinates] [nvarchar](500) NULL,
	[IPAddress] [nvarchar](50) NULL,
	[EventTime] [datetime] NULL,
	[ToAddress] [nvarchar](max) NULL,
	[ToMessage] [nvarchar](max) NULL,
	[FromAddress] [nvarchar](max) NULL,
	[EventServerTimeZone] [nvarchar](50) NULL,
	[EventServerOffsetMinute] [int] NULL,
	[EventLocalTime] [datetime] NULL,
	[EventLocalTimeZone] [nvarchar](50) NULL,
	[EventLocalOffsetMinute] [int] NULL,
	[EventChannel] [nvarchar](100) NULL,
	[EventStatus] [nvarchar](100) NULL,
	[EventErrorMsg] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [SiteEventLog] ADD  CONSTRAINT [DF_SiteEventLog_EventTime]  DEFAULT (getdate()) FOR [EventTime]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Channel used for communication like Email, SMS etc..' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SiteEventLog', @level2type=N'COLUMN',@level2name=N'EventChannel'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Status of the Event. Failed or Success or any other' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SiteEventLog', @level2type=N'COLUMN',@level2name=N'EventStatus'
GO


