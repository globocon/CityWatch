/* =====================================================================
   TEST-ONLY: PCAR identity health watchdog — run any time, read-only.

   The 25 Aug collision (all Romeo cars keyed to one shared position,
   superseding each other off the globe) is cured by the callsign→car
   re-key at session/start + session-anchored ingest. This watchdog
   catches every way the problem could CREEP BACK — new cars without
   positions, renamed positions, config drift — before the control room
   notices. All four checks returning zero rows = healthy.
   ===================================================================== */

/* CHECK 1 — the collision itself: two ACTIVE sessions on one unit.
   Must always be empty: the takeover rule closes the older one. */
SELECT s.UnitId, COUNT(*) AS ActiveSessions,
       STRING_AGG(CONCAT(s.Callsign, ' (guard ', s.GuardId, ')'), ', ') AS Who
FROM dbo.TrackingSession s
WHERE s.[Status] = 'Active'
GROUP BY s.UnitId
HAVING COUNT(*) > 1;

/* CHECK 2 — active car sessions whose callsign names a car they are NOT keyed to.
   Non-empty = the re-key is not running (old web build?) or was bypassed. */
SELECT s.UnitId, s.Callsign, s.PatrolCarPositionName,
       p.Id + 2000000 AS ExpectedUnit, p.[Name] AS ExpectedCar, s.StartedUtc
FROM dbo.TrackingSession s
JOIN dbo.IncidentReportPositions p
  ON p.IsPatrolCar = 1
 AND RTRIM(p.[Name]) LIKE '%) ' + RTRIM(s.Callsign)
WHERE s.[Status] = 'Active' AND s.IsPatrolCar = 1
  AND s.Callsign IS NOT NULL AND s.Callsign NOT LIKE '%[%_[]%'
  AND s.UnitId <> p.Id + 2000000
  AND 1 = (SELECT COUNT(*) FROM dbo.IncidentReportPositions p2
           WHERE p2.IsPatrolCar = 1
             AND RTRIM(p2.[Name]) LIKE '%) ' + RTRIM(s.Callsign));

/* CHECK 3 — config drift, the way it creeps back: car sessions (last 7 days)
   whose callsign names NO car position. A new car (R7?) was put on the road
   without creating 'Mobile Patrols (Car) R7' — its crews will share whatever
   position their phones default to. Fix: clone the position (DbScript 372
   pattern) and enrol it. */
SELECT s.Callsign, COUNT(*) AS Sessions, MAX(s.StartedUtc) AS LastSeenUtc,
       MIN(s.UnitId) AS SampleUnit
FROM dbo.TrackingSession s
WHERE s.IsPatrolCar = 1
  AND s.Callsign IS NOT NULL AND s.Callsign NOT LIKE '%[%_[]%'
  AND s.StartedUtc >= DATEADD(DAY, -7, GETUTCDATE())
  AND NOT EXISTS (SELECT 1 FROM dbo.IncidentReportPositions p
                  WHERE p.IsPatrolCar = 1
                    AND RTRIM(p.[Name]) LIKE '%) ' + RTRIM(s.Callsign))
GROUP BY s.Callsign;

/* CHECK 4 — ambiguity: one callsign matched by MORE than one car position.
   The re-key refuses to guess, so these crews silently keep their phone's
   unit until the duplicate name is fixed. */
SELECT RIGHT(RTRIM(p.[Name]), CHARINDEX(' (', REVERSE(RTRIM(p.[Name])) + ' (') ) AS SuffixHint,
       p.Id, p.[Name]
FROM dbo.IncidentReportPositions p
WHERE p.IsPatrolCar = 1
  AND EXISTS (SELECT 1 FROM dbo.IncidentReportPositions p2
              WHERE p2.IsPatrolCar = 1 AND p2.Id <> p.Id
                AND RTRIM(p2.[Name]) LIKE '%' + SUBSTRING(RTRIM(p.[Name]),
                        LEN(RTRIM(p.[Name])) - CHARINDEX(' (', REVERSE(RTRIM(p.[Name])) + ' (') + 1, 100))
ORDER BY p.[Name];

/* CHECK 5 — phones that track nothing: active sessions with zero fixes for
   30+ minutes (location permission / battery optimisation — the Rizwan/R1
   case). Code cannot fix these; the handset needs a visit. */
SELECT s.UnitId, s.Callsign, g.[Name] AS Guard, s.StartedUtc, s.LastFixUtc
FROM dbo.TrackingSession s
LEFT JOIN dbo.Guards g ON g.Id = s.GuardId
WHERE s.[Status] = 'Active'
  AND s.StartedUtc < DATEADD(MINUTE, -30, GETUTCDATE())
  AND s.LastFixUtc IS NULL;
