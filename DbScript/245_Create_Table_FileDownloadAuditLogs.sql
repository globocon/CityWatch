
CREATE TABLE [FileDownloadAuditLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [int] NULL,
	[GuardID] [int] NULL,
	[IPAddress] [nvarchar](100) NULL,
	[DwnlCatagory] [nvarchar](100) NULL,
	[DwnlFileName] [nvarchar](max) NULL,
	[EventDateTime] [datetime] NULL,
	[EventDateTimeServerOffsetMinute] [int] NULL,
	[EventDateTimeLocal] [datetime] NULL,
	[EventDateTimeLocalWithOffset] [datetimeoffset](7) NULL,
	[EventDateTimeZone] [nvarchar](500) NULL,
	[EventDateTimeZoneShort] [nvarchar](20) NULL,
	[EventDateTimeUtcOffsetMinute] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [FileDownloadAuditLogs] ADD  CONSTRAINT [DF_FileDownloadAuditLogs_EventDateTime]  DEFAULT (getdate()) FOR [EventDateTime]
GO

ALTER TABLE [FileDownloadAuditLogs] ADD  CONSTRAINT [DF_FileDownloadAuditLogs_EventDateTimeServerOffsetMinute]  DEFAULT (datepart(tzoffset,sysdatetimeoffset())) FOR [EventDateTimeServerOffsetMinute]
GO


