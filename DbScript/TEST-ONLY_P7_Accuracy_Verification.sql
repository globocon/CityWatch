/* TEST-ONLY (#153 Part 7): accuracy evidence report - run BEFORE trusting the
   rendering thresholds, on the LIVE (or test) database. Read-only: nothing is
   written, nothing is deleted. It answers, from data the pack already stores:

     1. How accurate are the fixes overall (percentiles)?
     2. How many are flagged LowAccuracy / Implausible / Mock / Backfilled?
     3. How do the accuracy bands look per site - especially the spike sites
        (Prixcar Webb Dock, Coolaroo, Consisten, Smithfield, Channel-NSW)?
     4. THE PROOF: do the far-from-centre vertices (the blue star spikes)
        correlate with LowAccuracy / Implausible / large accuracy values?

   Flags are a bitmask: 1 Mock, 2 LowAccuracy, 4 Implausible, 8 Backfilled. */

SET NOCOUNT ON;

/* ---- 1. Overall shape ------------------------------------------------- */
SELECT COUNT(*)                              AS TotalPoints,
       MIN(RecordedUtc)                      AS FirstUtc,
       MAX(RecordedUtc)                      AS LastUtc,
       AVG(CAST(AccuracyM AS FLOAT))         AS AvgAccuracyM,
       MAX(AccuracyM)                        AS MaxAccuracyM,
       SUM(CASE WHEN AccuracyM IS NULL THEN 1 ELSE 0 END) AS PointsWithoutAccuracy
FROM dbo.TrackPoints;

SELECT DISTINCT
       PERCENTILE_CONT(0.50) WITHIN GROUP (ORDER BY CAST(AccuracyM AS FLOAT)) OVER () AS P50,
       PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY CAST(AccuracyM AS FLOAT)) OVER () AS P75,
       PERCENTILE_CONT(0.90) WITHIN GROUP (ORDER BY CAST(AccuracyM AS FLOAT)) OVER () AS P90,
       PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY CAST(AccuracyM AS FLOAT)) OVER () AS P99
FROM dbo.TrackPoints
WHERE AccuracyM IS NOT NULL;

/* ---- 2. Flag counts ---------------------------------------------------- */
SELECT SUM(CASE WHEN Flags & 1 > 0 THEN 1 ELSE 0 END) AS MockProvider,
       SUM(CASE WHEN Flags & 2 > 0 THEN 1 ELSE 0 END) AS LowAccuracy,
       SUM(CASE WHEN Flags & 4 > 0 THEN 1 ELSE 0 END) AS Implausible,
       SUM(CASE WHEN Flags & 8 > 0 THEN 1 ELSE 0 END) AS Backfilled,
       SUM(CASE WHEN Flags = 0     THEN 1 ELSE 0 END) AS Clean
FROM dbo.TrackPoints;

/* ---- 3. Accuracy bands per login site (spike sites float to the top) --- */
SELECT TOP 30
       cs.Name                                                      AS SiteName,
       COUNT(*)                                                     AS Points,
       SUM(CASE WHEN tp.AccuracyM <= 30                        THEN 1 ELSE 0 END) AS Band_0_30m,
       SUM(CASE WHEN tp.AccuracyM > 30  AND tp.AccuracyM <= 100 THEN 1 ELSE 0 END) AS Band_30_100m,
       SUM(CASE WHEN tp.AccuracyM > 100 AND tp.AccuracyM <= 300 THEN 1 ELSE 0 END) AS Band_100_300m,
       SUM(CASE WHEN tp.AccuracyM > 300                         THEN 1 ELSE 0 END) AS Band_Over300m,
       SUM(CASE WHEN tp.Flags & 6 > 0                           THEN 1 ELSE 0 END) AS Flagged,
       AVG(CAST(tp.AccuracyM AS FLOAT))                             AS AvgAccM,
       SUM(CASE WHEN tp.SpeedKph IS NULL OR tp.SpeedKph < 2     THEN 1 ELSE 0 END) AS Stationaryish,
       SUM(CASE WHEN tp.SpeedKph >= 2                           THEN 1 ELSE 0 END) AS Moving
FROM dbo.TrackPoints tp
JOIN dbo.TrackingSessions s ON s.Id = tp.SessionId
LEFT JOIN dbo.ClientSites cs ON cs.Id = s.ClientSiteId
GROUP BY cs.Name
ORDER BY SUM(CASE WHEN tp.Flags & 6 > 0 THEN 1 ELSE 0 END) DESC;

/* ---- 4. THE PROOF: spike vertices vs confidence ------------------------
   For each session, measure every point's distance from the session's own
   centre (planar approximation - fine at site scale). A "spike vertex" is a
   point more than 250 m from centre in a session that barely moved. If the
   diagnosis is right, spike vertices are overwhelmingly flagged / inaccurate
   and calm vertices are overwhelmingly clean. */
WITH Centred AS (
    SELECT tp.SessionId, tp.Latitude, tp.Longitude, tp.AccuracyM, tp.Flags,
           AVG(CAST(tp.Latitude  AS FLOAT)) OVER (PARTITION BY tp.SessionId) AS CLat,
           AVG(CAST(tp.Longitude AS FLOAT)) OVER (PARTITION BY tp.SessionId) AS CLon
    FROM dbo.TrackPoints tp
),
Measured AS (
    SELECT SessionId, AccuracyM, Flags,
           SQRT(POWER((CAST(Latitude AS FLOAT) - CLat) * 111320.0, 2) +
                POWER((CAST(Longitude AS FLOAT) - CLon) * 111320.0 * COS(RADIANS(CLat)), 2)) AS OffCentreM
    FROM Centred
)
SELECT CASE WHEN OffCentreM > 250 THEN 'SPIKE (>250m off centre)' ELSE 'calm (<=250m)' END AS Vertex,
       COUNT(*)                                            AS Points,
       SUM(CASE WHEN Flags & 6 > 0 THEN 1 ELSE 0 END)      AS FlaggedLowAccOrImplausible,
       SUM(CASE WHEN AccuracyM > 100 THEN 1 ELSE 0 END)    AS AccuracyWorseThan100m,
       AVG(CAST(AccuracyM AS FLOAT))                       AS AvgAccM,
       CAST(100.0 * SUM(CASE WHEN Flags & 6 > 0 OR AccuracyM > 100 THEN 1 ELSE 0 END)
            / COUNT(*) AS DECIMAL(5,1))                    AS PctUntrusted
FROM Measured
GROUP BY CASE WHEN OffCentreM > 250 THEN 'SPIKE (>250m off centre)' ELSE 'calm (<=250m)' END;

/* ---- 5. Approximate-permission phones: every fix ~coarse ---------------
   A phone granted only APPROXIMATE location reports ~2000 m accuracy on
   every fix. Any unit whose MEDIAN accuracy is worse than 500 m is one. */
SELECT s.UnitId,
       COUNT(*) AS Points,
       AVG(CAST(tp.AccuracyM AS FLOAT)) AS AvgAccM,
       MIN(tp.AccuracyM) AS BestAccM
FROM dbo.TrackPoints tp
JOIN dbo.TrackingSessions s ON s.Id = tp.SessionId
WHERE tp.AccuracyM IS NOT NULL
GROUP BY s.UnitId
HAVING MIN(tp.AccuracyM) > 400        -- even its BEST fix is coarse = approximate permission
ORDER BY AvgAccM DESC;
