/* ============================================================================
   360_Create_Tracking_Schema.sql
   CityWatch.Tracking feature pack — schema install (ADD §8, D1/D3/D7)

   Additive only. Creates six new tables and one partition function/scheme.
   Touches NO existing table, column or index. Idempotent — safe to re-run.
   Rollback: 362_Rollback_Tracking_Schema.sql

   Design notes
   - TrackPoint has NO foreign keys (D7): insert cost, evidentiary history must
     survive a deleted wand, and no schema coupling to stable tables.
     UnitId = ClientSiteSmartWand.Id, enforced logically at ingest.
   - TrackPoint ships UNPARTITIONED (20-vehicle fleet), but the monthly partition
     function and scheme are created now so moving the table onto them at the
     500-vehicle mark is a maintenance operation, not a migration (§8.2).
   - PK on TrackPoint is NONCLUSTERED; the clustered index is (UnitId, RecordedUtc)
     because every read is "this unit, this window".
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---- Partition function + scheme (created ahead of need, §8.2) ------------ */
IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'PF_TrackPoint_Monthly')
BEGIN
    -- Seed boundaries: 12 months from first deployment month. MaintenanceJob
    -- (M1.4+) extends boundaries monthly ahead of the calendar.
    CREATE PARTITION FUNCTION PF_TrackPoint_Monthly (datetime2(0))
    AS RANGE RIGHT FOR VALUES (
        '2026-09-01T00:00:00', '2026-10-01T00:00:00', '2026-11-01T00:00:00',
        '2026-12-01T00:00:00', '2027-01-01T00:00:00', '2027-02-01T00:00:00',
        '2027-03-01T00:00:00', '2027-04-01T00:00:00', '2027-05-01T00:00:00',
        '2027-06-01T00:00:00', '2027-07-01T00:00:00', '2027-08-01T00:00:00'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'PS_TrackPoint_Monthly')
BEGIN
    CREATE PARTITION SCHEME PS_TrackPoint_Monthly
    AS PARTITION PF_TrackPoint_Monthly ALL TO ([PRIMARY]);
END
GO

/* ---- TrackPoint (§8.2) ---------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackPoint' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TrackPoint (
        Id              bigint IDENTITY(1,1) NOT NULL,
        UnitId          int              NOT NULL,   -- ClientSiteSmartWand.Id (no FK: D7)
        SessionId       uniqueidentifier NOT NULL,
        Seq             int              NOT NULL,
        RecordedUtc     datetime2(0)     NOT NULL,   -- device clock
        ReceivedUtc     datetime2(0)     NOT NULL,   -- server clock
        Latitude        decimal(9,6)     NOT NULL,
        Longitude       decimal(9,6)     NOT NULL,
        SpeedKph        smallint         NULL,
        HeadingDeg      smallint         NULL,
        AccuracyM       smallint         NULL,
        BatteryPct      tinyint          NULL,
        SourceType      tinyint          NOT NULL,   -- 1 NfcAnchor 2 Transit 3 Live 4 Duress
        ModeAtCapture   tinyint          NOT NULL,
        Flags           tinyint          NOT NULL CONSTRAINT DF_TrackPoint_Flags DEFAULT (0),
        AnchorTagUid    varchar(64)      NULL,
        CONSTRAINT PK_TrackPoint PRIMARY KEY NONCLUSTERED (Id)
    );

    CREATE CLUSTERED INDEX CX_TrackPoint_Unit_Time
        ON dbo.TrackPoint (UnitId, RecordedUtc);

    /* IGNORE_DUP_KEY: a retried upload batch (same unit/session/seq) is silently
       discarded by the engine instead of failing the bulk copy. This is what makes
       aggressive client-side retry safe by construction (§9.1). */
    CREATE UNIQUE INDEX UX_TrackPoint_Dedupe
        ON dbo.TrackPoint (UnitId, SessionId, Seq)
        WITH (IGNORE_DUP_KEY = ON);
END
GO

/* ---- TrackSegment (§8.3) — all reporting reads this, never TrackPoint ----- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackSegment' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TrackSegment (
        Id              bigint IDENTITY(1,1) NOT NULL,
        UnitId          int              NOT NULL,
        SessionId       uniqueidentifier NOT NULL,
        FromSiteId      int              NULL,
        ToSiteId        int              NULL,
        StartUtc        datetime2(0)     NOT NULL,
        EndUtc          datetime2(0)     NOT NULL,
        DistanceM       int              NOT NULL,
        DurationSec     int              NOT NULL,
        MaxSpeedKph     smallint         NULL,
        AvgSpeedKph     smallint         NULL,
        PointCount      int              NOT NULL,
        AnchorScanCount int              NOT NULL,
        AdherenceScore  tinyint          NULL,
        Flags           tinyint          NOT NULL CONSTRAINT DF_TrackSegment_Flags DEFAULT (0),
        CONSTRAINT PK_TrackSegment PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_TrackSegment_Unit_Start ON dbo.TrackSegment (UnitId, StartUtc);
    CREATE INDEX IX_TrackSegment_Session    ON dbo.TrackSegment (SessionId);
END
GO

/* ---- TrackingSession (§6.5) — no session, no tracking --------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingSession' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TrackingSession (
        Id           uniqueidentifier NOT NULL,
        UnitId       int              NOT NULL,
        GuardId      int              NOT NULL,
        ClientSiteId int              NOT NULL,
        PcarRouteId  int              NULL,
        StartedUtc   datetime2(0)     NOT NULL,
        EndedUtc     datetime2(0)     NULL,
        Status       nvarchar(20)     NOT NULL CONSTRAINT DF_TrackingSession_Status DEFAULT ('Active'),
        EndReason    nvarchar(30)     NULL,
        LastFixUtc   datetime2(0)     NULL,
        CONSTRAINT PK_TrackingSession PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_TrackingSession_Unit_Status ON dbo.TrackingSession (UnitId, Status);
    CREATE INDEX IX_TrackingSession_Guard       ON dbo.TrackingSession (GuardId);
END
GO

/* ---- TrackingUnitEnrolment (§3.4, §13.5) ----------------------------------
   Consent is a column, not a document: ingest refuses units with
   ConsentRecordedUtc IS NULL regardless of IsEnabled.                        */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingUnitEnrolment' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TrackingUnitEnrolment (
        UnitId             int           NOT NULL,   -- ClientSiteSmartWand.Id
        IsEnabled          bit           NOT NULL CONSTRAINT DF_TrackingUnitEnrolment_IsEnabled DEFAULT (0),
        EnrolledUtc        datetime2(0)  NOT NULL,
        EnrolledByUserId   int           NOT NULL,
        ConsentRecordedUtc datetime2(0)  NULL,
        ConsentReference   nvarchar(200) NULL,
        DisabledUtc        datetime2(0)  NULL,
        Notes              nvarchar(200) NULL,
        CONSTRAINT PK_TrackingUnitEnrolment PRIMARY KEY CLUSTERED (UnitId)
    );
END
GO

/* ---- TrackingModeCommand (§5.3, D5) ---------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingModeCommand' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TrackingModeCommand (
        Id              int IDENTITY(1,1) NOT NULL,
        UnitId          int           NOT NULL,
        CommandSeq      int           NOT NULL,
        DesiredMode     tinyint       NOT NULL,
        IssuedByUserId  int           NULL,       -- null = system (TTL expiry, duress)
        IssuedUtc       datetime2(0)  NOT NULL,
        ExpiresUtc      datetime2(0)  NULL,       -- null only for Duress
        AcknowledgedUtc datetime2(0)  NULL,
        Status          nvarchar(20)  NOT NULL CONSTRAINT DF_TrackingModeCommand_Status DEFAULT ('Pending'),
        EndReason       nvarchar(30)  NULL,
        CONSTRAINT PK_TrackingModeCommand PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX UX_TrackingModeCommand_Unit_Seq ON dbo.TrackingModeCommand (UnitId, CommandSeq);
    CREATE INDEX IX_TrackingModeCommand_Unit_Status     ON dbo.TrackingModeCommand (UnitId, Status);
END
GO

/* ---- TrackingAccessAudit (§13.4) ------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingAccessAudit' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TrackingAccessAudit (
        Id            bigint IDENTITY(1,1) NOT NULL,
        UserId        int           NOT NULL,
        Action        nvarchar(20)  NOT NULL,   -- ViewLive|ViewHistory|CommandLive|CommandCancel|Export|BreakGlass
        UnitId        int           NULL,
        WindowFromUtc datetime2(0)  NULL,
        WindowToUtc   datetime2(0)  NULL,
        AccessedUtc   datetime2(0)  NOT NULL,
        IpAddress     nvarchar(45)  NULL,
        Justification nvarchar(500) NULL,
        CONSTRAINT PK_TrackingAccessAudit PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_TrackingAccessAudit_User_Time ON dbo.TrackingAccessAudit (UserId, AccessedUtc);
    CREATE INDEX IX_TrackingAccessAudit_Unit_Time ON dbo.TrackingAccessAudit (UnitId, AccessedUtc);
END
GO

PRINT '360_Create_Tracking_Schema: complete.';
GO
