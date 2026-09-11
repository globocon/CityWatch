/* =====================================================================
   TEST-ONLY: Globe-map vs RC patrol-car mismatch diagnostic (GLOBE)

   Why they differ by design:
     - RC fleet count (OnGetPcarSummary, P4#153): a guard is a patrol car
       when their FIRST GuardLogins row of the day is at a PCAR-mode site.
     - Globe map (globeMap.js): one marker per active-guard row, plotted at
       the CURRENT site's ClientSites.Gps (NEVER the phone's live GPS).
       * Row skipped silently when Gps is NULL/empty.
       * Car icon only when the CURRENT site's PatrolTourMode = PCAR;
         mid-patrol at a STND site renders as a green dot instead.
       * Guards on the same site share identical coordinates -> markers
         stack exactly on top of each other; only the top one is visible.
       * A malformed Gps string throws inside the render loop and kills
         every remaining marker in that batch.

   Run against prod-citywatch. Set @Day to the day being investigated.
   Read-only. No schema changes.
   ===================================================================== */

DECLARE @Day date = CAST(GETDATE() AS date);   -- <-- change to the incident day, e.g. '2026-08-23'

/* ---------------------------------------------------------------------
   QUERY 1: per-guard reconstruction of that day's PCAR fleet, with the
   reason each car did or did not appear on the globe map, plus the
   guard's mobile app version (NULL row = pre-reporting/old APK build).
   --------------------------------------------------------------------- */
;WITH FirstLogin AS (
    SELECT gl.GuardId,
           gl.ClientSiteId,
           gl.OnDuty,
           ROW_NUMBER() OVER (PARTITION BY gl.GuardId ORDER BY gl.OnDuty) AS rn
    FROM GuardLogins gl
    WHERE gl.OnDuty >= @Day
      AND gl.OnDuty <  DATEADD(day, 1, @Day)
),
Fleet AS (
    /* THE fleet definition RC uses: first login of the day at a PCAR site */
    SELECT fl.GuardId,
           fl.ClientSiteId AS BaseSiteId,
           fl.OnDuty       AS FirstLoginTime
    FROM FirstLogin fl
    JOIN ClientSites baseSite ON baseSite.Id = fl.ClientSiteId
    WHERE fl.rn = 1
      AND baseSite.PatrolTourMode = 1            /* 0=STND, 1=PCAR, 2=INSP */
),
LastActivity AS (
    /* Where the map would have drawn each guard: their LAST activity row
       of the day. Live board reads the base table; after logout the
       evidence lives in ClientSiteRadioChecksActivityStatus_History. */
    SELECT h.GuardId,
           h.ClientSiteId,
           h.EventDateTime,
           ROW_NUMBER() OVER (PARTITION BY h.GuardId ORDER BY h.EventDateTime DESC) AS rn
    FROM ClientSiteRadioChecksActivityStatus_History h
    WHERE h.GuardId IS NOT NULL
      AND h.EventDateTime >= @Day
      AND h.EventDateTime <  DATEADD(day, 1, @Day)
)
SELECT
    g.Id                                          AS GuardId,
    g.[Name] + ISNULL(' [' + g.Initial + ']', '') AS Guard,
    baseSite.[Name]                               AS BaseSite_FirstLoginToday,
    f.FirstLoginTime,
    curSite.[Name]                                AS CurrentSite_LastActivity,
    la.EventDateTime                              AS LastActivityTime,
    CASE curSite.PatrolTourMode
         WHEN 0 THEN 'STND' WHEN 1 THEN 'PCAR' WHEN 2 THEN 'INSP'
         ELSE 'Unknown' END                       AS CurrentSiteMode,
    curSite.Gps                                   AS CurrentSiteGps,

    /* Mobile app version (DbScript 371). NULL/no row = APK from before
       version reporting existed, i.e. an OLD build. */
    ISNULL(v.AppVersion, '(no report = pre-1.54 old build)') AS AppVersion,
    v.Platform,
    v.DeviceInfo,
    v.LastSeen                                    AS VersionLastSeen,

    /* Why the globe map hides or disguises this patrol car */
    CASE
        WHEN la.GuardId IS NULL
            THEN 'NOT ON MAP - no activity row that day (the map only draws activity rows)'
        WHEN curSite.Gps IS NULL OR LTRIM(RTRIM(curSite.Gps)) = ''
            THEN 'NOT ON MAP - current site GPS is EMPTY (known bug: admin Settings save wipes Gps when Address is empty)'
        WHEN curSite.Gps NOT LIKE '%[0-9]%,%[0-9]%'
            THEN 'BREAKS MAP - malformed GPS throws in Leaflet and kills every marker rendered after this row'
        WHEN curSite.PatrolTourMode <> 1
            THEN 'ON MAP but as a GREEN DOT, not a car icon (current site is not PCAR mode)'
        WHEN COUNT(*) OVER (PARTITION BY NULLIF(LTRIM(RTRIM(curSite.Gps)), '')) > 1
            THEN 'ON MAP but STACKED - same coordinates as '
                 + CAST(COUNT(*) OVER (PARTITION BY NULLIF(LTRIM(RTRIM(curSite.Gps)), '')) - 1 AS varchar(10))
                 + ' other marker(s); only the topmost is visible'
        ELSE 'Visible as its own car marker'
    END                                           AS WhyGlobeMapDiffers
FROM Fleet f
JOIN Guards g              ON g.Id = f.GuardId
JOIN ClientSites baseSite  ON baseSite.Id = f.BaseSiteId
LEFT JOIN LastActivity la  ON la.GuardId = f.GuardId AND la.rn = 1
LEFT JOIN ClientSites curSite ON curSite.Id = la.ClientSiteId
OUTER APPLY (
    SELECT TOP 1 av.AppVersion, av.Platform, av.DeviceInfo, av.LastSeen
    FROM GuardMobileAppVersions av
    WHERE av.GuardId = f.GuardId
    ORDER BY av.LastSeen DESC
) v
ORDER BY f.FirstLoginTime;


/* ---------------------------------------------------------------------
   QUERY 2: the hard ceiling. The globe map can never show more car
   icons than there are DISTINCT non-empty GPS coordinates among
   PCAR-mode sites. If GLOBE's cars run out of 3 base sites (or only 3
   PCAR sites still have a GPS value), the map maxes out at 3 forever
   while RC counts every guard.
   --------------------------------------------------------------------- */
SELECT
    cs.Id,
    cs.[Name],
    cs.Address,
    cs.Gps,
    CASE
        WHEN cs.Gps IS NULL OR LTRIM(RTRIM(cs.Gps)) = ''
            THEN 'NO MARKER POSSIBLE - GPS empty'
        WHEN cs.Gps NOT LIKE '%[0-9]%,%[0-9]%'
            THEN 'MALFORMED - will break the map render loop'
        ELSE 'OK'
    END AS GpsState,
    (SELECT COUNT(DISTINCT gl.GuardId)
     FROM GuardLogins gl
     WHERE gl.ClientSiteId = cs.Id
       AND gl.OnDuty >= @Day
       AND gl.OnDuty <  DATEADD(day, 1, @Day)) AS GuardsLoggedInThatDay
FROM ClientSites cs
WHERE cs.PatrolTourMode = 1                      /* PCAR sites only */
ORDER BY GpsState, cs.[Name];
