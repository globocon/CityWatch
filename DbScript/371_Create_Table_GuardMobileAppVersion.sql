/* 371: GuardMobileAppVersion - which mobile app build each guard is running (P4#153).

   Why its own table and not a column on GuardLogins: the login flow must be untouchable.
   New APK builds report their version through a separate fire-and-forget call AFTER
   login; old builds never call it at all. One row per guard per platform, updated in
   place on every report. A guard with NO row is therefore running a build from before
   the app started reporting - which is exactly the "this user is on an old version"
   signal the control room needs when triaging an issue.

   Idempotent. Rollback: DROP TABLE dbo.GuardMobileAppVersions. */

IF OBJECT_ID(N'dbo.GuardMobileAppVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuardMobileAppVersions
    (
        Id         INT IDENTITY(1,1) NOT NULL,
        GuardId    INT               NOT NULL,   -- Guards.Id, no FK: telemetry must never block a guard delete
        AppVersion NVARCHAR(32)      NOT NULL,   -- "1.54.2" as the APK reports itself
        Platform   NVARCHAR(16)      NOT NULL CONSTRAINT DF_GuardMobileAppVersions_Platform DEFAULT ('android'),
        DeviceInfo NVARCHAR(200)     NULL,       -- optional: "Samsung SM-A155F, Android 14"
        FirstSeen  DATETIME          NOT NULL,
        LastSeen   DATETIME          NOT NULL,

        CONSTRAINT PK_GuardMobileAppVersions PRIMARY KEY CLUSTERED (Id)
    );

    /* The upsert's seek, and the natural key: one row per guard per platform. */
    CREATE UNIQUE NONCLUSTERED INDEX UX_GuardMobileAppVersions_Guard_Platform
        ON dbo.GuardMobileAppVersions (GuardId, Platform);

    PRINT '371: dbo.GuardMobileAppVersions created.';
END
ELSE
    PRINT '371: dbo.GuardMobileAppVersions already exists - nothing to do.';
