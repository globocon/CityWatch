CREATE TABLE PostActivityRequestLocalCacheOfflineNotSynced
(
    SyncId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,   -- [Key] + Identity
    Id INT NOT NULL,
    guardId INT NOT NULL,
    clientsiteId INT NOT NULL,
    userId INT NOT NULL,
    activityString NVARCHAR(MAX) NULL,
    gps NVARCHAR(200) NULL,
    systemEntry BIT NOT NULL DEFAULT(1),
    scanningType INT NOT NULL DEFAULT(0),
    tagUID NVARCHAR(100) NOT NULL DEFAULT('NA'),
    EventDateTimeLocal DATETIME NULL,
    EventDateTimeLocalWithOffset DATETIMEOFFSET(7) NULL,
    EventDateTimeZone NVARCHAR(100) NULL,
    EventDateTimeZoneShort NVARCHAR(20) NULL,
    EventDateTimeUtcOffsetMinute INT NULL,
    IsNewGuard BIT NOT NULL DEFAULT(0),
    IsSynced BIT NOT NULL DEFAULT(0),
    UniqueRecordId UNIQUEIDENTIFIER NOT NULL,
    DeviceId NVARCHAR(300) NULL,
    DeviceName NVARCHAR(350) NULL,
    SyncTime DATETIME NOT NULL DEFAULT (SYSDATETIME()),
    NotSyncError NVARCHAR(MAX) NULL
);

CREATE TABLE ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced
(
    SyncId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,   -- [Key] + Identity
    Id INT NOT NULL,
    LoggedInClientSiteId INT NOT NULL,
    LoggedInUserId INT NOT NULL,
    LoggedInGuardId INT NOT NULL,
    TagUId NVARCHAR(100) NULL,
    TagsTypeId INT NOT NULL,
    HitUtcDateTime DATETIME NOT NULL,
    HitLocalDateTime DATETIME NOT NULL,
    LastModifiedUtc DATETIME NOT NULL,
    SmartWandId INT NULL,
    GPScoordinates NVARCHAR(200) NULL,
    IsSynced BIT NOT NULL DEFAULT(0),
    UniqueRecordId UNIQUEIDENTIFIER NOT NULL,
    EventDateTimeLocal DATETIME NULL,
    EventDateTimeLocalWithOffset DATETIMEOFFSET(7) NULL,
    EventDateTimeZone NVARCHAR(100) NULL,
    EventDateTimeZoneShort NVARCHAR(20) NULL,
    EventDateTimeUtcOffsetMinute INT NULL,
    DeviceId NVARCHAR(300) NULL,
    DeviceName NVARCHAR(350) NULL,
    SyncTime DATETIME NOT NULL DEFAULT (SYSDATETIME()),
    NotSyncError NVARCHAR(MAX) NULL
);

CREATE TABLE OfflineFilesRecordsNotSynced
(
    SyncId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,   -- [Key] + Identity
    Id INT NOT NULL,
    RecordLabel NVARCHAR(150) NULL,
    FileNameActual NVARCHAR(260) NULL,
    FileNameCache NVARCHAR(260) NULL,
    FileNameWithPathCache NVARCHAR(500) NULL,
    EventDateTimeLocal DATETIME NULL,
    EventDateTimeLocalWithOffset DATETIMEOFFSET(7) NULL,
    EventDateTimeZone NVARCHAR(100) NULL,
    EventDateTimeZoneShort NVARCHAR(20) NULL,
    EventDateTimeUtcOffsetMinute INT NULL,
    IsSynced BIT NOT NULL DEFAULT(0),
    UniqueRecordId UNIQUEIDENTIFIER NOT NULL,
    FileType NVARCHAR(50) NULL,         -- rear / twentyfive / etc
    IsNew BIT NOT NULL DEFAULT(0),      -- true when picked offline
    LogBookId INT NULL,                 -- null for new files
    guardId INT NOT NULL,
    clientsiteId INT NOT NULL,
    userId INT NOT NULL,
    gps NVARCHAR(200) NULL,
    FileGroupId UNIQUEIDENTIFIER NOT NULL,
    DeviceId NVARCHAR(100) NULL,
    DeviceName NVARCHAR(150) NULL,
    SyncTime DATETIME NOT NULL DEFAULT (SYSDATETIME()),
    NotSyncError NVARCHAR(MAX) NULL
);

