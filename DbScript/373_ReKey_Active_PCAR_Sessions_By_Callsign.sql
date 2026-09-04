/* =====================================================================
   373: The callsign names the car — re-key ACTIVE patrol-car sessions.

   Companion to the web release that makes session/start re-key a
   patrol-car login to the position its CALLSIGN names, and makes ingest
   resolve batches by SESSION id (the phone's stale unit stamp keeps
   working). Deploy the web release FIRST, then run this once: it moves
   today's already-active car sessions (and their points) onto their own
   cars, so the fleet separates on the live map immediately instead of at
   each crew's next login.

   Why: phones auto-restore the saved Position, and every Romeo handset
   remembers the old shared "Mobile Patrols (Car) M1" (unit 2000010) from
   before DbScript 372 split the fleet — 25 Aug: twelve car logins, one
   unit, one visible car, eleven SupersededByNewSession rows.

   Idempotent: a session already keyed to its callsign's car matches
   nothing. Only ACTIVE, IsPatrolCar=1 sessions whose callsign names
   EXACTLY ONE patrol-car position are touched.
   Rollback: none needed — re-keying back would re-create the collision;
   the pre-run verification output records the original keys.
   ===================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

BEGIN TRAN;

/* The mapping: active car session -> the ONE position its callsign names. */
DECLARE @Map TABLE (SessionId uniqueidentifier PRIMARY KEY, OldUnitId int,
                    NewUnitId int, PositionId int, PositionName nvarchar(100),
                    StartedUtc datetime);
INSERT INTO @Map (SessionId, OldUnitId, NewUnitId, PositionId, PositionName, StartedUtc)
SELECT s.Id, s.UnitId, m.Id + 2000000, m.Id, m.[Name], s.StartedUtc
FROM dbo.TrackingSession s
CROSS APPLY (
    SELECT p.Id, p.[Name]
    FROM dbo.IncidentReportPositions p
    WHERE p.IsPatrolCar = 1
      AND RTRIM(p.[Name]) LIKE '%) ' + RTRIM(s.Callsign)
) m
WHERE s.[Status] = 'Active'
  AND s.IsPatrolCar = 1
  AND s.Callsign IS NOT NULL
  AND s.Callsign NOT LIKE '%[%_[]%'                 /* LIKE-wildcard-safe callsigns only */
  AND s.UnitId <> m.Id + 2000000
  AND 1 = (SELECT COUNT(*) FROM dbo.IncidentReportPositions p2
           WHERE p2.IsPatrolCar = 1
             AND RTRIM(p2.[Name]) LIKE '%) ' + RTRIM(s.Callsign));  /* exactly one car */

PRINT CONCAT('373: active car sessions to re-key: ', (SELECT COUNT(*) FROM @Map));

/* Two active sessions must never land on one unit: if two crews claimed the same
   callsign, the newer login wins — the same takeover rule session/start applies. */
UPDATE s SET s.[Status] = 'Completed', s.EndedUtc = SYSUTCDATETIME(),
             s.EndReason = 'SupersededByNewSession'
FROM dbo.TrackingSession s
JOIN @Map m ON m.SessionId = s.Id
WHERE EXISTS (SELECT 1 FROM @Map newer
              WHERE newer.NewUnitId = m.NewUnitId AND newer.StartedUtc > m.StartedUtc);
PRINT CONCAT('373: duplicate-callsign sessions closed: ', @@ROWCOUNT);
DELETE m FROM @Map m
WHERE EXISTS (SELECT 1 FROM @Map newer
              WHERE newer.NewUnitId = m.NewUnitId AND newer.StartedUtc > m.StartedUtc);

/* Also: if the target unit already has a DIFFERENT active session (correctly keyed
   crew already on that car), the re-key would collide — leave that session alone. */
DELETE m FROM @Map m
WHERE EXISTS (SELECT 1 FROM dbo.TrackingSession o
              WHERE o.UnitId = m.NewUnitId AND o.[Status] = 'Active');
PRINT CONCAT('373: sessions to re-key after collision checks: ', (SELECT COUNT(*) FROM @Map));

/* The evidence moves with the session: replay/history look points up by unit. The
   (UnitId, SessionId, Seq) dedupe key stays unique — seqs are per-session. */
UPDATE tp SET tp.UnitId = m.NewUnitId
FROM dbo.TrackPoint tp
JOIN @Map m ON m.SessionId = tp.SessionId;
PRINT CONCAT('373: track points moved to the callsign''s car: ', @@ROWCOUNT);

UPDATE ts SET ts.UnitId = m.NewUnitId
FROM dbo.TrackSegment ts
JOIN @Map m ON m.SessionId = ts.SessionId;
PRINT CONCAT('373: segments moved: ', @@ROWCOUNT);

UPDATE v SET v.UnitId = m.NewUnitId
FROM dbo.TrackingSiteVisit v
JOIN @Map m ON m.SessionId = v.SessionId;
PRINT CONCAT('373: site visits moved: ', @@ROWCOUNT);

UPDATE s SET s.UnitId = m.NewUnitId,
             s.PatrolCarPositionId = m.PositionId,
             s.PatrolCarPositionName = m.PositionName
FROM dbo.TrackingSession s
JOIN @Map m ON m.SessionId = s.Id;
PRINT CONCAT('373: sessions re-keyed: ', @@ROWCOUNT);

COMMIT;
GO

/* Verify: every active car session, its unit and car — expect one row per callsign,
   distinct units, nobody on the old shared unit unless their callsign names no car. */
SELECT s.UnitId, s.Callsign, s.PatrolCarPositionName, g.[Name] AS Guard,
       s.StartedUtc, s.LastFixUtc
FROM dbo.TrackingSession s
LEFT JOIN dbo.Guards g ON g.Id = s.GuardId
WHERE s.[Status] = 'Active' AND s.IsPatrolCar = 1
ORDER BY s.Callsign;
GO
