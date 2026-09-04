/* 369: TrackingDeviceToken — FCM registration tokens for tracking units' phones.
   A token is how the server NUDGES a device for a fresh position (manual ping from the
   control room, and later the automatic stale-unit nudge); the position itself always
   arrives on the normal ingest path — FCM is the accelerator, ingest is the guarantee.
   One unit accumulates several tokens over time (reinstalls, replacement phones):
   dead tokens are deactivated in place so the audit trail keeps what was pinged when.
   Idempotent, like every script in this pack. Rollback: DROP TABLE dbo.TrackingDeviceToken. */

IF OBJECT_ID(N'dbo.TrackingDeviceToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrackingDeviceToken
    (
        Id             INT IDENTITY(1,1) NOT NULL,
        UnitId         INT            NOT NULL,   -- TrackingUnitKey (car/guard), no FK: D7
        FcmToken       NVARCHAR(512)  NOT NULL,
        Platform       NVARCHAR(20)   NOT NULL CONSTRAINT DF_TrackingDeviceToken_Platform DEFAULT ('android'),
        CreatedUtc     DATETIME2(0)   NOT NULL,
        UpdatedUtc     DATETIME2(0)   NOT NULL,
        LastSeenUtc    DATETIME2(0)   NULL,
        IsActive       BIT            NOT NULL CONSTRAINT DF_TrackingDeviceToken_IsActive DEFAULT (1),
        InvalidatedUtc DATETIME2(0)   NULL,

        CONSTRAINT PK_TrackingDeviceToken PRIMARY KEY CLUSTERED (Id)
    );

    /* One row per physical token: a phone that logs into a different unit re-homes its
       token instead of leaving a live token pointing at the old unit. (Nonclustered key
       limit is 1700 bytes on supported SQL Server versions; NVARCHAR(512) = 1024 fits.) */
    CREATE UNIQUE NONCLUSTERED INDEX UX_TrackingDeviceToken_Token
        ON dbo.TrackingDeviceToken (FcmToken);

    CREATE NONCLUSTERED INDEX IX_TrackingDeviceToken_Unit_Active
        ON dbo.TrackingDeviceToken (UnitId, IsActive);

    PRINT '369: dbo.TrackingDeviceToken created.';
END
ELSE
    PRINT '369: dbo.TrackingDeviceToken already exists - nothing to do.';
