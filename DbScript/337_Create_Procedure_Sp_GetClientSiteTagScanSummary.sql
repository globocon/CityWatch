create PROCEDURE [dbo].[Sp_GetClientSiteTagScanSummary]
(
    @ClientId INT,
    @FromDate DATE,
    @ToDate DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    ---------------------------------------------------------------------------------------
    -- 1) CTE: Site tags
    ---------------------------------------------------------------------------------------
    ;WITH SiteTags AS
    (
        SELECT 
            ClientSiteId,
            UId AS TagUId,
            FqBypass,
            LabelDescription,
            CASE 
                WHEN TagsTypeId = 2 THEN 'NFC'
                WHEN TagsTypeId = 1 THEN 'BLE'
                ELSE 'Other'
            END AS TagType
        FROM ClientSiteSmartWandTags
        WHERE IsDeleted = 0
          AND ClientSiteId = @ClientId
    ),

    ---------------------------------------------------------------------------------------
    -- 2) Convert HitUtcDateTime → HitLocalDateTime
    ---------------------------------------------------------------------------------------
    TagHits AS
    (
        SELECT 
            l.LoggedInClientSiteId AS ClientSiteId,
            l.TagUId,
            CAST(
                l.HitUtcDateTime AT TIME ZONE 'UTC'
                AT TIME ZONE ISNULL(NULLIF(s.TimezoneString, ''), 'AUS Eastern Standard Time')
                AS DATETIMEOFFSET
            ) AS HitLocalDateTime
        FROM ClientSiteSmartWandTagsHitLogs l
        INNER JOIN ClientSiteKpiSettings s
            ON l.LoggedInClientSiteId = s.ClientSiteId
        WHERE l.LoggedInClientSiteId = @ClientId
    ),

    ---------------------------------------------------------------------------------------
    -- 3) Filter logs for date range
    ---------------------------------------------------------------------------------------
    DateRangeHits AS
    (
        SELECT *
        FROM TagHits
        WHERE CAST(HitLocalDateTime AS DATE) 
              BETWEEN @FromDate AND @ToDate
    ),

    ---------------------------------------------------------------------------------------
    -- 4) Count scans per tag
    ---------------------------------------------------------------------------------------
    TagScanCounts AS
    (
        SELECT 
            t.ClientSiteId,
            t.TagUId,
            t.FqBypass,
            t.LabelDescription,
            t.TagType,

            CASE 
                WHEN t.FqBypass = 1 THEN 0
                ELSE COUNT(h.TagUId)
            END AS ScanCount,

            (SELECT COUNT(*) 
             FROM DateRangeHits hh 
             WHERE hh.TagUId = t.TagUId 
               AND hh.ClientSiteId = @ClientId) AS MyScans

        FROM SiteTags t
        LEFT JOIN DateRangeHits h
            ON t.TagUId = h.TagUId

        GROUP BY 
            t.ClientSiteId,
            t.TagUId,
            t.FqBypass,
            t.LabelDescription,
            t.TagType
    )

    ---------------------------------------------------------------------------------------
    -- 5) Final result
    ---------------------------------------------------------------------------------------
    SELECT 
        CASE 
            WHEN tsc.FqBypass = 1 
                THEN tsc.LabelDescription + ' (Bypass)'
            ELSE tsc.LabelDescription
        END AS LabelDescription,

        tsc.TagType,

        tsc.ScanCount AS TodayScanCount,

        tsc.MyScans AS MyScans,  -- removed guard specific scan

        CASE 
            WHEN tsc.FqBypass = 1 THEN 0
            ELSE ISNULL(tsc.ScanCount, 0) + 1
        END AS RoundNumber

    FROM TagScanCounts tsc
    ORDER BY tsc.LabelDescription;

END