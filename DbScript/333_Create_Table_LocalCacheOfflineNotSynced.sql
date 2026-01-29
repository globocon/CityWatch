CREATE TABLE PatrolCarLogRequestLocalCacheOfflineNotSynced (
    SyncId INT IDENTITY(1,1) PRIMARY KEY,
    CacheId INT NOT NULL,
    SiteId INT NOT NULL,
    Id INT NOT NULL,
    ClientSiteLogBookId INT NOT NULL,
    Mileage DECIMAL(18,2) NOT NULL,
    MileageText NVARCHAR(350) NULL,
    PatrolCar NVARCHAR(350) NULL,

    EventDateTimeLocal DATETIME NULL,
    EventDateTimeLocalWithOffset DATETIMEOFFSET(7) NULL,
    EventDateTimeZone NVARCHAR(100) NULL,
    EventDateTimeZoneShort NVARCHAR(20) NULL,
    EventDateTimeUtcOffsetMinute INT NULL,

    IsSynced BIT NOT NULL DEFAULT 0,
    UniqueRecordId UNIQUEIDENTIFIER NOT NULL,
    DeviceId NVARCHAR(300) NULL,
    DeviceName NVARCHAR(350) NULL,

    PatrolCarId INT NOT NULL,
    Model NVARCHAR(100) NULL,
    Rego NVARCHAR(50) NULL,
    ClientSiteId INT NOT NULL,

    SyncTime DATETIME NOT NULL DEFAULT SYSDATETIME(),
    NotSyncError NVARCHAR(MAX) NULL
);


CREATE TABLE CustomFieldLogRequestHeadLocalCacheOfflineNotSynced (
    SyncId INT IDENTITY(1,1) PRIMARY KEY,   -- Identity PK

    Id INT NOT NULL,                         -- Original server Id
    SiteId INT NOT NULL,

    EventDateTimeLocal DATETIME NULL,
    EventDateTimeLocalWithOffset DATETIMEOFFSET(7) NULL,
    EventDateTimeZone NVARCHAR(100) NULL,
    EventDateTimeZoneShort NVARCHAR(20) NULL,
    EventDateTimeUtcOffsetMinute INT NULL,

    IsSynced BIT NOT NULL DEFAULT 0,
    UniqueRecordId UNIQUEIDENTIFIER NOT NULL,
    DeviceId NVARCHAR(300) NULL,
    DeviceName NVARCHAR(350) NULL,

    SyncTime DATETIME NOT NULL DEFAULT GETDATE(),
    NotSyncError NVARCHAR(MAX) NULL
);




CREATE TABLE CustomFieldLogRequestDetailCacheOfflineNotSynced (
    SyncId INT IDENTITY(1,1) PRIMARY KEY,   
    SyncIdHeadRef INT NOT NULL, -- FK -> Head.SyncId
    Id INT NOT NULL,                         
    HeadId INT NOT NULL,                    

    DictKey NVARCHAR(MAX) NOT NULL,
    DictValue NVARCHAR(MAX) NULL,

    CONSTRAINT FK_CustomFieldLogDetailOffline_Head
        FOREIGN KEY (SyncIdHeadRef)
        REFERENCES CustomFieldLogRequestHeadLocalCacheOfflineNotSynced(SyncId)
        ON DELETE CASCADE

);
