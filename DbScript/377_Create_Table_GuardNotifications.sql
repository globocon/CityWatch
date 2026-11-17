/* 377: GuardNotifications — the mobile app's Notifications tab.

   A notification is DERIVED from something that is already true elsewhere in the system
   (today: a course sitting unfinished in the guard's HR record) but it cannot be purely
   computed on each request, because read/unread is state the guard owns and it has to
   survive a reinstall and be the same on every device they log into. So the derived facts
   are materialised here, and a sync pass reconciles them:

       source row still outstanding  -> row exists and IsActive = 1
       source row completed/removed  -> IsActive = 0  (the notification disappears from the
                                        tab, but the read history is kept for audit)

   TARGETING. A notification is addressed either to ONE GUARD (GuardId set) or to A SITE
   (ClientSiteId set) — never both, never neither; CK_GuardNotifications_Target enforces it.
   A site-targeted notification is shown to every guard currently logged in at that site.

   READ STATE lives in GuardNotificationReads, not in a column here, precisely because of
   site targeting: one row is seen by many guards, and the first guard to read it must not
   clear it for everyone else. "Read" therefore means "this guard has a row in
   GuardNotificationReads"; marking unread deletes that row. For guard-targeted rows the
   child table only ever holds the one guard, which costs a join and keeps one rule instead
   of two.

   ReferenceId is the id of the source row in its own table — for NotificationTypeId 1 that
   is GuardTrainingAndAssessment.Id, and it is 0 for notifications that have no derived
   source (an operator writing to a site). Deliberately NOT a foreign key: the types this
   table will carry point at different tables, and a course row that is hard-deleted must
   leave the notification deactivatable rather than blocking the delete.

   The unique index on (GuardId, ClientSiteId, NotificationTypeId, ReferenceId) is what makes
   the sync idempotent — it runs on every notifications fetch, so "insert if missing" has to
   be safe to attempt concurrently from two devices. SQL Server treats NULLs as equal in a
   unique index, which is what makes it work across both targeting shapes.

   Rollback:
       DROP TABLE dbo.GuardNotificationReads;
       DROP TABLE dbo.GuardNotifications;
       DROP TABLE dbo.NotificationTypes;
*/

IF OBJECT_ID(N'dbo.NotificationTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationTypes
    (
        Id          INT           NOT NULL,   -- fixed ids: the sync code matches on them
        Name        NVARCHAR(100) NOT NULL,

        CONSTRAINT PK_NotificationTypes PRIMARY KEY CLUSTERED (Id)
    );

    -- Matches CityWatch.Data.Enums.GuardNotificationType.
    INSERT INTO dbo.NotificationTypes (Id, Name) VALUES
        (1, N'Training Course'),
        (2, N'Site Message');

    PRINT '377: dbo.NotificationTypes created.';
END
ELSE
BEGIN
    /* Re-runnable: pick up type 2 on an instance that already took an earlier copy of 377. */
    IF NOT EXISTS (SELECT 1 FROM dbo.NotificationTypes WHERE Id = 2)
        INSERT INTO dbo.NotificationTypes (Id, Name) VALUES (2, N'Site Message');

    PRINT '377: dbo.NotificationTypes already exists - types checked.';
END
GO

IF OBJECT_ID(N'dbo.GuardNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GuardNotifications
    (
        Id                 INT IDENTITY(1,1) NOT NULL,

        /* Exactly one of these two is set - see CK_GuardNotifications_Target below.
           GuardId       => addressed to that guard alone.
           ClientSiteId  => addressed to everyone working that site. */
        GuardId            INT            NULL,
        ClientSiteId       INT            NULL,

        NotificationTypeId INT            NOT NULL,

        /* Id of the row this notification was derived from, in the table that
           NotificationTypeId names. Type 1 => GuardTrainingAndAssessment.Id.
           0 when the notification has no derived source. */
        ReferenceId        INT            NOT NULL CONSTRAINT DF_GuardNotifications_ReferenceId DEFAULT (0),

        Title              NVARCHAR(200)  NOT NULL,
        Message            NVARCHAR(1000) NOT NULL,

        CreatedOn          DATETIME       NOT NULL CONSTRAINT DF_GuardNotifications_CreatedOn DEFAULT (getdate()),

        /* 0 once the underlying source is resolved (the course was completed) or an operator
           withdraws a site message: hidden from the tab, kept for audit. */
        IsActive           BIT            NOT NULL CONSTRAINT DF_GuardNotifications_IsActive DEFAULT (1),
        DeactivatedOn      DATETIME       NULL,

        CONSTRAINT PK_GuardNotifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_GuardNotifications_Guards
            FOREIGN KEY (GuardId) REFERENCES dbo.Guards (Id),
        CONSTRAINT FK_GuardNotifications_ClientSites
            FOREIGN KEY (ClientSiteId) REFERENCES dbo.ClientSites (Id),
        CONSTRAINT FK_GuardNotifications_NotificationTypes
            FOREIGN KEY (NotificationTypeId) REFERENCES dbo.NotificationTypes (Id),

        /* One target, and only one. A row with both set would be delivered twice; a row with
           neither would be delivered to nobody and sit invisible forever. */
        CONSTRAINT CK_GuardNotifications_Target CHECK
        (
            (GuardId IS NOT NULL AND ClientSiteId IS NULL)
            OR
            (GuardId IS NULL AND ClientSiteId IS NOT NULL)
        )
    );

    /* One notification per target per source row - the reconcile pass relies on this to
       stay idempotent when two devices fetch at the same moment. NULLs compare equal in a
       SQL Server unique index, so this covers guard-targeted and site-targeted rows alike.

       Filtered to ReferenceId <> 0 because uniqueness is a property of DERIVED rows only:
       ReferenceId 0 means "no source", and an operator must be able to send a site more than
       one message without the second one colliding with the first. */
    CREATE UNIQUE NONCLUSTERED INDEX UX_GuardNotifications_Source
        ON dbo.GuardNotifications (GuardId, ClientSiteId, NotificationTypeId, ReferenceId)
        WHERE ReferenceId <> 0;

    /* The tab's two queries: this guard's live notifications, and this site's. */
    CREATE NONCLUSTERED INDEX IX_GuardNotifications_Guard_Active
        ON dbo.GuardNotifications (GuardId, IsActive)
        INCLUDE (CreatedOn);

    CREATE NONCLUSTERED INDEX IX_GuardNotifications_Site_Active
        ON dbo.GuardNotifications (ClientSiteId, IsActive)
        INCLUDE (CreatedOn);

    PRINT '377: dbo.GuardNotifications created.';
END
ELSE
    PRINT '377: dbo.GuardNotifications already exists - nothing to do.';
GO

IF OBJECT_ID(N'dbo.GuardNotificationReads', N'U') IS NULL
BEGIN
    /* Which guard has read which notification. Presence of the row IS "read"; marking
       something unread deletes the row rather than flagging it, so the table only ever holds
       facts and there is no third "was read then unread" state to reason about. */
    CREATE TABLE dbo.GuardNotificationReads
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        GuardNotificationId INT      NOT NULL,
        GuardId             INT      NOT NULL,
        ReadOn              DATETIME NOT NULL CONSTRAINT DF_GuardNotificationReads_ReadOn DEFAULT (getdate()),

        CONSTRAINT PK_GuardNotificationReads PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_GuardNotificationReads_GuardNotifications
            FOREIGN KEY (GuardNotificationId) REFERENCES dbo.GuardNotifications (Id),
        CONSTRAINT FK_GuardNotificationReads_Guards
            FOREIGN KEY (GuardId) REFERENCES dbo.Guards (Id)
    );

    /* A guard reads a notification once. Also the lookup the list query joins on. */
    CREATE UNIQUE NONCLUSTERED INDEX UX_GuardNotificationReads_Notification_Guard
        ON dbo.GuardNotificationReads (GuardNotificationId, GuardId);

    /* "Everything this guard has read", for the unread count. */
    CREATE NONCLUSTERED INDEX IX_GuardNotificationReads_Guard
        ON dbo.GuardNotificationReads (GuardId)
        INCLUDE (GuardNotificationId);

    PRINT '377: dbo.GuardNotificationReads created.';
END
ELSE
    PRINT '377: dbo.GuardNotificationReads already exists - nothing to do.';
GO
