

CREATE TABLE [dbo].[RCActionListMessages](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Notifications] [nvarchar](max) NULL,
	[Subject] [nvarchar](max) NULL,
	[AlarmKeypadCode] [nvarchar](max) NULL,
	[Action1] [nvarchar](max) NULL,
	[Physicalkey] [nvarchar](max) NULL,
	[Action2] [nvarchar](max) NULL,
	[SiteCombinationLook] [nvarchar](max) NULL,
	[Action4] [nvarchar](max) NULL,
	[Action3] [nvarchar](max) NULL,
	[Action5] [nvarchar](max) NULL,
	[CommentsForControlRoomOperator] [nvarchar](max) NULL,
	[messagetime] DateTime NULL,
	[IsState] bit default 0,
	[IsNational] bit default 0,
	[IsClientType]  bit default 0,
	[IsSMSPersonal] bit default 0,
	[IsSMSSmartWand] bit default 0,
	[IsPersonalEmail]  bit default 0,
	[IsDeleted] bit default 0
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


 
CREATE TABLE [dbo].[RCActionListMessagesGuardLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RCActionListMessagesId] [int] NOT NULL,
	[EventDateTime] [datetime] NOT NULL,
	[EventDateTimeLocal] [datetime] NULL,
	[EventDateTimeLocalWithOffset] [datetimeoffset](7) NULL,
	[EventDateTimeZone] [nvarchar](500) NULL,
	[EventDateTimeZoneShort] [nvarchar](20) NULL,
	[EventDateTimeUtcOffsetMinute] [int] NULL,
	[RemoteIPAddress] [nvarchar](500) NULL,
	
	[GuardId] [int] NOT NULL,
	[IsDeleted] bit default 0
) ON [PRIMARY] 
CREATE TABLE [dbo].[RCActionListMessagesClientsites](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RCActionListMessagesId] [int] NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[IsDeleted] [bit] NOT NULL
) ON [PRIMARY] 

