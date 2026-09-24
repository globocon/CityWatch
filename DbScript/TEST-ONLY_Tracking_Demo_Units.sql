/* ============================================================================
   TEST-ONLY_Tracking_Demo_Units.sql
   NOT a deployment script. Do NOT run on production.

   Puts two fake units on the Control Room map so the tracking display can be
   verified before the mobile app is available:

     Unit 9001 — patrol CAR, a 20-point trail ending "now"  (car symbol + trail)
     Unit 9002 — GUARD on foot, parked ~45 min              (idle list + badge)

   Both use id 9xxx so they cannot collide with real wands.
   Re-runnable: it clears its own rows first.

   To remove everything afterwards, run TEST-ONLY_Tracking_Demo_Cleanup.sql
   (or just: DELETE FROM TrackPoint WHERE UnitId IN (9001,9002); etc.)
   ============================================================================ */
SET NOCOUNT ON;

DECLARE @now datetime2(0) = SYSUTCDATETIME();
DECLARE @car int = 9001, @guard int = 9002;
DECLARE @carSession uniqueidentifier = 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEFFFF0001';
DECLARE @guardSession uniqueidentifier = 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEFFFF0002';

/* ---- clean previous run ---- */
DELETE FROM dbo.TrackPoint            WHERE UnitId IN (@car, @guard);
DELETE FROM dbo.TrackingSession       WHERE UnitId IN (@car, @guard);
DELETE FROM dbo.TrackingUnitEnrolment WHERE UnitId IN (@car, @guard);
DELETE FROM dbo.TrackingModeCommand   WHERE UnitId IN (@car, @guard);

/* ---- a wand row for the car, so it renders with the CAR symbol ----
   (kind is derived from PatrolCarId; without this it shows as a guard) */
IF NOT EXISTS (SELECT 1 FROM dbo.ClientSiteSmartWands WHERE Id = @car)
BEGIN
    SET IDENTITY_INSERT dbo.ClientSiteSmartWands ON;
    INSERT INTO dbo.ClientSiteSmartWands (Id, ClientSiteId, SmartWandId, PhoneNumber, IsDeleted, PatrolCarId)
    SELECT @car, (SELECT TOP 1 Id FROM dbo.ClientSites ORDER BY Id), N'DEMO-WAND-CAR', N'0000000000', 0,
           (SELECT TOP 1 Id FROM dbo.ClientSitePatrolCars ORDER BY Id);
    SET IDENTITY_INSERT dbo.ClientSiteSmartWands OFF;
END

DECLARE @siteId int  = (SELECT TOP 1 Id FROM dbo.ClientSites ORDER BY Id);
DECLARE @guardId int = (SELECT TOP 1 Id FROM dbo.Guards ORDER BY Id);   -- real name in the idle list

/* ---- enrolments (enabled + consent, or ingest/session would refuse) ---- */
INSERT INTO dbo.TrackingUnitEnrolment (UnitId, IsEnabled, EnrolledUtc, EnrolledByUserId, ConsentRecordedUtc, ConsentReference, Notes)
VALUES (@car,   1, @now, 0, @now, N'TEST-DEMO', N'TEST ONLY - demo patrol car'),
       (@guard, 1, @now, 0, @now, N'TEST-DEMO', N'TEST ONLY - demo guard on foot');

INSERT INTO dbo.TrackingSession (Id, UnitId, GuardId, ClientSiteId, StartedUtc, Status, LastFixUtc)
VALUES (@carSession,   @car,   @guardId, @siteId, DATEADD(MINUTE,-30,@now), 'Active', @now),
       (@guardSession, @guard, @guardId, @siteId, DATEADD(MINUTE,-60,@now), 'Active', @now);

/* ---- CAR: a drive into Sydney CBD, newest fix = now ---- */
;WITH route(seq, minAgo, lat, lon, spd, hdg) AS (SELECT * FROM (VALUES
    (1,19,-33.9173,151.2313,42, 10),(2,18,-33.9106,151.2295,47,  5),
    (3,17,-33.9040,151.2286,51,355),(4,16,-33.8975,151.2260,44,340),
    (5,15,-33.8927,151.2216,38,320),(6,14,-33.8892,151.2166,35,300),
    (7,13,-33.8875,151.2110,40,285),(8,12,-33.8865,151.2065,36,290),
    (9,11,-33.8838,151.2035,30,330),(10,10,-33.8802,151.2035,33,  0),
    (11, 9,-33.8768,151.2049,35, 20),(12, 8,-33.8737,151.2069,31, 25),
    (13, 7,-33.8708,151.2085,28, 20),(14, 6,-33.8688,151.2073,25,340),
    (15, 5,-33.8710,151.2094,22,140),(16, 4,-33.8727,151.2105,27,150),
    (17, 3,-33.8745,151.2098,24,190),(18, 2,-33.8760,151.2085,26,210),
    (19, 1,-33.8773,151.2070,23,215),(20, 0,-33.8781,151.2059,18,220)
) v(seq,minAgo,lat,lon,spd,hdg))
INSERT INTO dbo.TrackPoint (UnitId, SessionId, Seq, RecordedUtc, ReceivedUtc, Latitude, Longitude,
                            SpeedKph, HeadingDeg, AccuracyM, BatteryPct, SourceType, ModeAtCapture, Flags)
SELECT @car, @carSession, seq, DATEADD(MINUTE,-minAgo,@now), DATEADD(MINUTE,-minAgo,@now),
       lat, lon, spd, hdg, 8, 76, 2, 2, 0
FROM route;

/* one NFC anchor mid-route, so Replay shows the green verified-touch dot */
INSERT INTO dbo.TrackPoint (UnitId, SessionId, Seq, RecordedUtc, ReceivedUtc, Latitude, Longitude,
                            AccuracyM, SourceType, ModeAtCapture, Flags, AnchorTagUid)
VALUES (@car, @carSession, -1000, DATEADD(MINUTE,-6,@now), DATEADD(MINUTE,-6,@now),
        -33.8688, 151.2073, 6, 1, 1, 0, '04DEMO01');

/* ---- GUARD: walked in 45 min ago, tiny drift since => idle list ---- */
;WITH stay(seq, minAgo, lat, lon) AS (SELECT * FROM (VALUES
    (1,45,-33.865000,151.210000),(2,40,-33.873090,151.211000),(3,34,-33.873110,151.211050),
    (4,28,-33.873095,151.210980),(5,22,-33.873120,151.211020),(6,16,-33.873100,151.211060),
    (7,10,-33.873085,151.211010),(8, 4,-33.873105,151.211040),(9, 1,-33.873098,151.211025)
) v(seq,minAgo,lat,lon))
INSERT INTO dbo.TrackPoint (UnitId, SessionId, Seq, RecordedUtc, ReceivedUtc, Latitude, Longitude,
                            SpeedKph, HeadingDeg, AccuracyM, BatteryPct, SourceType, ModeAtCapture, Flags)
SELECT @guard, @guardSession, seq, DATEADD(MINUTE,-minAgo,@now), DATEADD(MINUTE,-minAgo,@now),
       lat, lon, 0, NULL, 10, 58, 2, 2, 0
FROM stay;

SELECT 'demo units created' AS status,
       (SELECT COUNT(*) FROM dbo.TrackPoint WHERE UnitId IN (@car,@guard)) AS points,
       'Open the Control Room Map and zoom to Sydney CBD' AS next_step;
