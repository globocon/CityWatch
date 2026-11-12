/* =====================================================================
   TEST-ONLY: Same phone or separate phones for the 6 Romeo PCAR guards?

   Three independent fingerprints, strongest first:
     1. TrackingDeviceToken.FcmToken — unique per PHYSICAL app install.
        One row per phone, re-homed to whatever unit last used it.
        Only phones that successfully started tracking appear here.
     2. GuardLogins.IPAddress — recorded on EVERY login. Two guards on
        the same IP within minutes of each other = very likely the same
        phone/SIM. (Caveat: carrier CG-NAT can share an IP between SIMs,
        so same-IP is strong evidence, not absolute proof; different IPs
        also don't prove different phones because mobile IPs rotate.)
     3. ClientSiteSmartWands — the fleet phones REGISTERED for site 625
        (PhoneNumber / IMEI / DeviceId): how many phones the Romeo fleet
        is supposed to have.

   Read-only. Run against prod-citywatch.
   ===================================================================== */

DECLARE @Day date = CAST(GETDATE() AS date);   -- <-- the LOCAL day being investigated
DECLARE @RomeoSiteId int = 625;                -- Citywatch M1 - Romeo Patrol Cars

DECLARE @Guards TABLE (GuardId int PRIMARY KEY);
INSERT INTO @Guards VALUES (1224), (882), (763), (1889), (838), (1260);

/* ---------------------------------------------------------------------
   QUERY 1: every login by the 6 guards that day, in time order, with
   the IP. Read it top to bottom: the same IP appearing for DIFFERENT
   guards back-to-back = a handed-over phone.
   --------------------------------------------------------------------- */
SELECT
    gl.OnDuty,
    gl.OffDuty,
    gl.GuardId,
    g.[Name] + ISNULL(' [' + g.Initial + ']', '') AS Guard,
    gl.ClientSiteId,
    cs.[Name]                                     AS Site,
    gl.IPAddress,
    COUNT(DISTINCT gl2.GuardId)                   AS GuardsOnThisIpThatDay,
    CASE WHEN COUNT(DISTINCT gl2.GuardId) > 1
         THEN 'SHARED - other guards logged in from this same IP that day'
         ELSE 'only this guard on this IP' END    AS PhoneSharingSignal
FROM dbo.GuardLogins gl
JOIN @Guards f          ON f.GuardId = gl.GuardId
JOIN dbo.Guards g       ON g.Id = gl.GuardId
LEFT JOIN dbo.ClientSites cs ON cs.Id = gl.ClientSiteId
LEFT JOIN dbo.GuardLogins gl2
       ON gl2.IPAddress = gl.IPAddress
      AND gl2.IPAddress IS NOT NULL AND gl2.IPAddress <> ''
      AND gl2.OnDuty >= @Day AND gl2.OnDuty < DATEADD(day, 1, @Day)
WHERE gl.OnDuty >= @Day AND gl.OnDuty < DATEADD(day, 1, @Day)
GROUP BY gl.OnDuty, gl.OffDuty, gl.GuardId, g.[Name], g.Initial,
         gl.ClientSiteId, cs.[Name], gl.IPAddress
ORDER BY gl.OnDuty;

/* ---------------------------------------------------------------------
   QUERY 2: IP -> guards summary. One row per IP used that day by any
   of the 6; GuardList shows exactly who shared it.
   --------------------------------------------------------------------- */
SELECT
    gl.IPAddress,
    COUNT(DISTINCT gl.GuardId) AS DistinctGuards,
    STUFF((SELECT ', ' + g2.[Name]
           FROM dbo.GuardLogins glx
           JOIN dbo.Guards g2 ON g2.Id = glx.GuardId
           WHERE glx.IPAddress = gl.IPAddress
             AND glx.GuardId IN (SELECT GuardId FROM @Guards)
             AND glx.OnDuty >= @Day AND glx.OnDuty < DATEADD(day, 1, @Day)
           GROUP BY g2.[Name]
           FOR XML PATH('')), 1, 2, '') AS GuardList
FROM dbo.GuardLogins gl
JOIN @Guards f ON f.GuardId = gl.GuardId
WHERE gl.OnDuty >= @Day AND gl.OnDuty < DATEADD(day, 1, @Day)
  AND gl.IPAddress IS NOT NULL AND gl.IPAddress <> ''
GROUP BY gl.IPAddress
ORDER BY DistinctGuards DESC, gl.IPAddress;

/* ---------------------------------------------------------------------
   QUERY 3: physical phones known to tracking (FCM tokens). Each row is
   ONE real phone; UnitId is the unit it last tracked as. Count the rows
   homed to the Romeo units: that is how many DISTINCT phones ever
   tracked for this fleet. Token shown truncated - full value not needed.
   --------------------------------------------------------------------- */
SELECT
    t.UnitId,
    CASE
        WHEN t.UnitId >= 2000000 THEN 'CAR position ' + CAST(t.UnitId - 2000000 AS varchar(10))
        WHEN t.UnitId >= 1000000 THEN 'GUARD ' + CAST(t.UnitId - 1000000 AS varchar(10))
        ELSE 'legacy wand'
    END                                     AS UnitKind,
    g.[Name]                                AS GuardIfGuardUnit,
    p.[Name]                                AS CarIfCarUnit,
    LEFT(t.FcmToken, 24) + '...'            AS PhoneToken,
    t.Platform,
    t.IsActive,
    t.CreatedUtc,
    t.UpdatedUtc,
    t.LastSeenUtc
FROM dbo.TrackingDeviceToken t
LEFT JOIN dbo.Guards g ON t.UnitId >= 1000000 AND t.UnitId < 2000000 AND g.Id = t.UnitId - 1000000
LEFT JOIN dbo.IncidentReportPositions p ON t.UnitId >= 2000000 AND p.Id = t.UnitId - 2000000
WHERE t.UnitId IN (1001224, 1000882, 1000763, 1001889, 1000838, 1001260)   /* the 6 as guard units */
   OR t.UnitId BETWEEN 2000010 AND 2000024                                  /* all car positions */
ORDER BY t.LastSeenUtc DESC;

/* ---------------------------------------------------------------------
   QUERY 4: the phones REGISTERED to the Romeo site - how many the fleet
   officially has, with their numbers/IMEIs and car allocation.
   --------------------------------------------------------------------- */
SELECT
    w.Id            AS SmartWandRowId,
    w.SmartWandId,
    w.PhoneNumber,
    w.SIMProvider,
    w.IMEI,
    w.DeviceType,
    w.DeviceId,
    w.DeviceName,
    w.PatrolCarId,
    w.IsDeleted
FROM dbo.ClientSiteSmartWands w
WHERE w.ClientSiteId = @RomeoSiteId
ORDER BY w.IsDeleted, w.PhoneNumber;
