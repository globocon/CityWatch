

CREATE TABLE irOfflineCacheNotSynced(
	[SyncId] [int] IDENTITY(1,1) NOT NULL,
	IrId NVARCHAR(MAX) NULL,
    IncidentRequest NVARCHAR(MAX) NULL,
	[EventDateTimeLocal] [datetime] NULL,
	[EventDateTimeLocalWithOffset] [datetimeoffset](7) NULL,
	[EventDateTimeZone] [nvarchar](100) NULL,
	[EventDateTimeZoneShort] [nvarchar](20) NULL,
	[EventDateTimeUtcOffsetMinute] [int] NULL,
	[IsSynced] BIT NOT NULL DEFAULT 0,
	[UniqueRecordId] [uniqueidentifier] NOT NULL,
	[guardId] INT NOT NULL,
    [clientsiteId] INT NOT NULL,
    [userId] INT NOT NULL,
    [gps] NVARCHAR(200) NULL,
	[DeviceId] [nvarchar](300) NULL,
	[DeviceName] [nvarchar](350) NULL,
	[SyncTime] DATETIME NOT NULL DEFAULT SYSDATETIME(),
    [NotSyncError] NVARCHAR(MAX) NULL
) ;

CREATE TABLE irOfflineFilesAttachmentsCacheNotSynced
(
    [SyncId] [int] IDENTITY(1,1) NOT NULL,
    [UniqueRecordId] UNIQUEIDENTIFIER NOT NULL,
    [IrId] NVARCHAR(MAX) NULL,
    [FileNameActual] NVARCHAR(MAX) NULL,
    [FileNameCache] NVARCHAR(MAX) NULL,
    [FileNameWithPathCache] NVARCHAR(MAX) NULL,
    [EventDateTimeLocal] [datetime] NULL,
	[EventDateTimeLocalWithOffset] [datetimeoffset](7) NULL,
	[EventDateTimeZone] [nvarchar](100) NULL,
	[EventDateTimeZoneShort] [nvarchar](20) NULL,
	[EventDateTimeUtcOffsetMinute] [int] NULL,
	[IsSynced] BIT NOT NULL DEFAULT 0,
    [guardId] INT NOT NULL,
    [clientsiteId] INT NOT NULL,
    [userId] INT NOT NULL,
    [gps] NVARCHAR(200) NULL,
	[DeviceId] [nvarchar](300) NULL,
	[DeviceName] [nvarchar](350) NULL,
    [ServerFileNameWithPath] NVARCHAR(500) NULL,
	[SyncTime] DATETIME NOT NULL DEFAULT SYSDATETIME(),
    [NotSyncError] NVARCHAR(MAX) NULL
);



    

    


    
   