/* =====================================================================
   TEST-ONLY (v2): Live tracking map shows fewer PCAR units than RC counts.

   v2 fixes a timezone hole in v1: TrackingSession.StartedUtc is UTC, but
   shifts and GuardLogins are LOCAL (AEST = UTC+10). A Romeo shift that
   starts at local midnight starts at 14:00 UTC the PREVIOUS day, so v1's
   "StartedUtc >= @Day" missed every session of a night shift that had
   already closed. v2 windows on the LOCAL day converted to UTC and keeps
   any session that OVERLAPS that window.

   How the tracking map counts (unchanged from v1):
     - marker = ACTIVE TrackingSession with >= 1 GPS fix
     - car icon when the session says IsPatrolCar=1 (officer's toggle),
       or the unit key is a position (2,000,000+). Guard-keyed sessions
       with IsPatrolCar NULL/0 draw as guards, not cars.
     - one active session per unit; a different guard on the same unit
       force-closes the old session ('SupersededByNewSession')
     - unenrolled unit -> session/start refused 403, phone tracks nothing
     - session with zero fixes is not drawn at all

   Run against prod-citywatch. Read-only.
   ===================================================================== */

DECLARE @Day date = CAST(GETDATE() AS date);   -- <-- the LOCAL day being investigated
DECLARE @TzOffsetHours int = 10;               -- AEST; Melbourne has no DST in August

DECLARE @UtcFrom datetime = DATEADD(HOUR, -@TzOffsetHours, CAST(@Day AS datetime));
DECLARE @UtcTo   datetime = DATEADD(DAY, 1, @UtcFrom);

/* ---------------------------------------------------------------------
   QUERY 1: every tracking session overlapping the local day.
   NOW INCLUDES IsPatrolCar + position + login site, so you can see
   exactly which sessions render as CAR icons on the map and where the
   guard logged in from. Rows sharing a UnitId = takeover chain.
   --------------------------------------------------------------------- */
SELECT
    s.UnitId,
    CASE
        WHEN s.UnitId >= 2000000 THEN 'CAR (position ' + CAST(s.UnitId - 2000000 AS varchar(10)) + ')'
        WHEN s.UnitId >= 1000000 THEN 'GUARD (guard '  + CAST(s.UnitId - 1000000 AS varchar(10)) + ')'
        ELSE 'LEGACY WAND'
    END                                            AS UnitKind,
    s.IsPatrolCar,                                 /* 1 = renders as a CAR icon */
    s.PatrolCarPositionId,
    ISNULL(s.PatrolCarPositionName, p.[Name])      AS PatrolCar,
    s.Callsign,
    g.[Name] + ISNULL(' [' + g.Initial + ']', '')  AS Guard,
    s.GuardId,
    s.ClientSiteId,
    cs.[Name]                                      AS LoginSite,
    s.StartedUtc,
    s.EndedUtc,
    s.[Status],
    s.EndReason,
    pts.Fixes                                      AS GpsFixCount,
    pts.LastFixUtc,
    v.AppVersion,
    CASE
        WHEN s.[Status] = 'Active' AND ISNULL(pts.Fixes, 0) > 0 AND ISNULL(s.IsPatrolCar, 0) = 1
            THEN 'ON MAP as CAR'
        WHEN s.[Status] = 'Active' AND ISNULL(pts.Fixes, 0) > 0
            THEN 'ON MAP as guard dot (IsPatrolCar not set)'
        WHEN s.[Status] = 'Active'
            THEN 'NOT ON MAP - session open but NO GPS fix ever received (location permission / battery optimisation)'
        WHEN s.EndReason = 'SupersededByNewSession'
            THEN 'KICKED OFF MAP - another guard logged in on the SAME unit'
        WHEN s.EndReason = 'Reaper'
            THEN 'Expired - reaper closed it after silence'
        ELSE 'Off map since ' + CONVERT(varchar(19), s.EndedUtc, 120) + ' UTC (' + ISNULL(s.EndReason, 'unknown') + ')'
    END                                            AS MapVerdict
FROM dbo.TrackingSession s
LEFT JOIN dbo.Guards g       ON g.Id = s.GuardId
LEFT JOIN dbo.ClientSites cs ON cs.Id = s.ClientSiteId
LEFT JOIN dbo.IncidentReportPositions p
       ON s.UnitId >= 2000000 AND p.Id = s.UnitId - 2000000
OUTER APPLY (
    SELECT COUNT(*) AS Fixes, MAX(tp.RecordedUtc) AS LastFixUtc
    FROM dbo.TrackPoint tp
    WHERE tp.SessionId = s.Id
) pts
OUTER APPLY (
    SELECT TOP 1 av.AppVersion
    FROM dbo.GuardMobileAppVersions av
    WHERE av.GuardId = s.GuardId
    ORDER BY av.LastSeen DESC
) v
WHERE s.StartedUtc < @UtcTo
  AND (s.EndedUtc IS NULL OR s.EndedUtc >= @UtcFrom)   /* overlaps the local day */
ORDER BY s.UnitId, s.StartedUtc;


/* ---------------------------------------------------------------------
   QUERY 1b: SAME, but only the PCAR fleet guards (first login of the
   local day at a PCAR-mode site) — the six rows that matter, no noise.
   --------------------------------------------------------------------- */
;WITH FirstLogin AS (
    SELECT gl.GuardId, gl.ClientSiteId, gl.OnDuty,
           ROW_NUMBER() OVER (PARTITION BY gl.GuardId ORDER BY gl.OnDuty) AS rn
    FROM dbo.GuardLogins gl
    WHERE gl.OnDuty >= @Day AND gl.OnDuty < DATEADD(day, 1, @Day)   /* OnDuty is LOCAL */
),
Fleet AS (
    SELECT fl.GuardId, fl.OnDuty AS FirstLoginTime
    FROM FirstLogin fl
    JOIN dbo.ClientSites cs ON cs.Id = fl.ClientSiteId
    WHERE fl.rn = 1 AND cs.PatrolTourMode = 1
)
SELECT
    f.GuardId,
    g.[Name] + ISNULL(' [' + g.Initial + ']', '') AS Guard,
    f.FirstLoginTime,
    s.UnitId,
    s.IsPatrolCar,
    s.Callsign,
    s.PatrolCarPositionName,
    s.StartedUtc,
    s.EndedUtc,
    s.[Status],
    s.EndReason,
    pts.Fixes                                     AS GpsFixCount,
    pts.LastFixUtc,
    ISNULL(v.AppVersion, '(no report = old build)') AS AppVersion,
    CASE
        WHEN s.Id IS NULL
            THEN 'NO TRACKING SESSION for the local day - start refused (enrolment) or the app PCAR flow never called session/start'
        WHEN s.[Status] = 'Active' AND ISNULL(pts.Fixes, 0) > 0 AND ISNULL(s.IsPatrolCar, 0) = 1
            THEN 'Was/is ON MAP as CAR'
        WHEN s.[Status] = 'Active' AND ISNULL(pts.Fixes, 0) > 0
            THEN 'ON MAP but as a guard DOT - IsPatrolCar not sent by the app'
        WHEN ISNULL(pts.Fixes, 0) = 0
            THEN 'INVISIBLE - session existed but zero GPS fixes (permission / battery optimisation)'
        WHEN s.EndReason = 'SupersededByNewSession'
            THEN 'KICKED OFF - same unit taken by another guard'
        ELSE 'Tracked, then off map at ' + CONVERT(varchar(19), s.EndedUtc, 120) + ' UTC (' + ISNULL(s.EndReason, '?') + ')'
    END                                           AS MapVerdict
FROM Fleet f
JOIN dbo.Guards g ON g.Id = f.GuardId
LEFT JOIN dbo.TrackingSession s
       ON s.GuardId = f.GuardId
      AND s.StartedUtc < @UtcTo
      AND (s.EndedUtc IS NULL OR s.EndedUtc >= @UtcFrom)
OUTER APPLY (
    SELECT COUNT(*) AS Fixes, MAX(tp.RecordedUtc) AS LastFixUtc
    FROM dbo.TrackPoint tp
    WHERE tp.SessionId = s.Id
) pts
OUTER APPLY (
    SELECT TOP 1 av.AppVersion FROM dbo.GuardMobileAppVersions av
    WHERE av.GuardId = f.GuardId ORDER BY av.LastSeen DESC
) v
ORDER BY f.FirstLoginTime, s.StartedUtc;


/* ---------------------------------------------------------------------
   QUERY 2: enrolment coverage for every patrol-car position (unchanged;
   your last run showed all 12 OK) plus the GUARD-unit enrolment state of
   the fleet guards — the app currently keys PCAR sessions on the GUARD,
   so a missing/disabled GUARD enrolment row also refuses session/start.
   --------------------------------------------------------------------- */
;WITH FirstLogin AS (
    SELECT gl.GuardId, gl.ClientSiteId,
           ROW_NUMBER() OVER (PARTITION BY gl.GuardId ORDER BY gl.OnDuty) AS rn
    FROM dbo.GuardLogins gl
    WHERE gl.OnDuty >= @Day AND gl.OnDuty < DATEADD(day, 1, @Day)
),
Fleet AS (
    SELECT fl.GuardId
    FROM FirstLogin fl
    JOIN dbo.ClientSites cs ON cs.Id = fl.ClientSiteId
    WHERE fl.rn = 1 AND cs.PatrolTourMode = 1
)
SELECT
    f.GuardId,
    g.[Name]                AS Guard,
    f.GuardId + 1000000     AS GuardUnitId,
    CASE WHEN e.UnitId IS NULL             THEN 'NOT ENROLLED as guard unit - session/start refused (403)'
         WHEN e.IsEnabled = 0              THEN 'DISABLED - refused'
         WHEN e.ConsentRecordedUtc IS NULL THEN 'NO CONSENT DATE - refused'
         ELSE 'OK - trackable as guard unit'
    END                     AS GuardEnrolmentState,
    e.EnrolledUtc,
    e.ConsentRecordedUtc
FROM Fleet f
JOIN dbo.Guards g ON g.Id = f.GuardId
LEFT JOIN dbo.TrackingUnitEnrolment e ON e.UnitId = f.GuardId + 1000000
ORDER BY GuardEnrolmentState, g.[Name];
