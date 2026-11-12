/* 370: TrackingSiteVisit — one row per time a tracked unit was AT a client site.

   Why a table and not a flag on the session: the control room's bell must still show
   "M1 entered Martha Cove at 21:14" after a page refresh, after an app pool recycle, and
   on a second operator's screen — and it must be recorded even when nobody has the map
   open. A transient client-side alert satisfies none of that.

   ENTRY IS DETECTED FROM GPS, not from scans: an officer who does not tag the site would
   otherwise never show as arrived. A row is created on the first fix inside the radius but
   stays UNCONFIRMED (ConfirmedUtc NULL) until the unit is still inside after the dwell
   window — that is what stops a car driving past a site on the main road from raising an
   alert. Only confirmed visits reach the bell. Drive-pasts are closed unconfirmed and kept,
   because "we saw you pass" is evidence too, and deleting them would hide detector faults.

   NFC scans still count: they confirm on the spot (Source='Nfc'), which is also how sites
   with no GPS coordinate on file can still raise an arrival.

   Idempotent, like every script in this pack. Rollback: DROP TABLE dbo.TrackingSiteVisit. */

IF OBJECT_ID(N'dbo.TrackingSiteVisit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrackingSiteVisit
    (
        Id           INT IDENTITY(1,1)  NOT NULL,
        UnitId       INT                NOT NULL,   -- TrackingUnitKey (car/guard), no FK: D7
        SessionId    UNIQUEIDENTIFIER   NOT NULL,
        SiteId       INT                NOT NULL,   -- ClientSites.Id, no FK: pack owns no platform keys
        SiteName     NVARCHAR(200)      NOT NULL,   -- denormalised: the bell must read the same
                                                    -- name the operator saw, even if the site is
                                                    -- later renamed or deactivated
        EnteredUtc   DATETIME2(0)       NOT NULL,
        ConfirmedUtc DATETIME2(0)       NULL,       -- NULL = candidate/drive-past, never alerted
        ExitedUtc    DATETIME2(0)       NULL,       -- NULL = still on site
        Source       NVARCHAR(10)       NOT NULL CONSTRAINT DF_TrackingSiteVisit_Source DEFAULT ('Gps'),
        EnteredLat   DECIMAL(9,6)       NULL,
        EnteredLon   DECIMAL(9,6)       NULL,
        DistanceM    INT                NULL,       -- how close the confirming fix was: the
                                                    -- honest measure of this detection

        CONSTRAINT PK_TrackingSiteVisit PRIMARY KEY CLUSTERED (Id)
    );

    /* The bell feed: "confirmed arrivals since X", newest first. */
    CREATE NONCLUSTERED INDEX IX_TrackingSiteVisit_Confirmed
        ON dbo.TrackingSiteVisit (ConfirmedUtc) INCLUDE (UnitId, SiteId, SiteName, EnteredUtc, ExitedUtc, Source);

    /* The detector's own read on every batch: "is this session already inside somewhere?" */
    CREATE NONCLUSTERED INDEX IX_TrackingSiteVisit_Session_Open
        ON dbo.TrackingSiteVisit (SessionId, ExitedUtc);

    CREATE NONCLUSTERED INDEX IX_TrackingSiteVisit_Unit_Entered
        ON dbo.TrackingSiteVisit (UnitId, EnteredUtc);

    PRINT '370: dbo.TrackingSiteVisit created.';
END
ELSE
    PRINT '370: dbo.TrackingSiteVisit already exists - nothing to do.';
