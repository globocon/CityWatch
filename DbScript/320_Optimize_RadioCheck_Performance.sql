USE [prod-citywatch]
GO

/****** 1. Create Indexes for Performance Optimization ******/

/* Index 1: Main Status Table */
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientSiteRadioChecksActivityStatus_Performance' AND object_id = OBJECT_ID('ClientSiteRadioChecksActivityStatus'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ClientSiteRadioChecksActivityStatus_Performance] ON [dbo].[ClientSiteRadioChecksActivityStatus]
    (
        [ClientSiteId] ASC,
        [GuardId] ASC,
        [ActivityType] ASC
    )
    INCLUDE ([GuardLoginTime]) 
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END
GO

/* Index 2: Smart Wand Tags */
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientSiteSmartWandTags_Performance' AND object_id = OBJECT_ID('ClientSiteSmartWandTags'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ClientSiteSmartWandTags_Performance] ON [dbo].[ClientSiteSmartWandTags]
    (
        [ClientSiteId] ASC,
        [IsDeleted] ASC,
        [FqBypass] ASC
    )
    INCLUDE ([UId])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END
GO

/* Index 3: Smart Wand Hit Logs */
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClientSiteSmartWandTagsHitLogs_Performance' AND object_id = OBJECT_ID('ClientSiteSmartWandTagsHitLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ClientSiteSmartWandTagsHitLogs_Performance] ON [dbo].[ClientSiteSmartWandTagsHitLogs]
    (
        [LoggedInClientSiteId] ASC,
        [TagUId] ASC
    )
    INCLUDE ([HitUtcDateTime])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
END
GO


/****** 2. Optimize Stored Procedure ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_GetActiveGuardDetailsForRC]
AS
BEGIN
    SET NOCOUNT ON;

    /* 
       Optimization Phase 3: Move HR Status Calculation to SQL 
       Calculate Red/Yellow/Green using set-based logic.
       Priorities: Red (3) > Yellow (2) > Green (1) > Grey (0/NULL)
    */
    WITH GuardHRStatus AS (
        SELECT 
            gcl.GuardId,
            /* Aggregation: Take the MAX score to prioritize Red/Yellow issues */
            MAX(CASE WHEN hrg.Name LIKE 'HR1%' THEN 
                CASE 
                    WHEN gcl.DateType = 1 THEN 1 -- Green (No Expiry)
                    WHEN gcl.ExpiryDate < GETDATE() THEN 3 -- Red (Expired)
                    WHEN gcl.ExpiryDate < DATEADD(day, 45, GETDATE()) THEN 2 -- Yellow (Expiring Soon)
                    ELSE 1 -- Green
                END
            END) as HR1_Score,
            
            MAX(CASE WHEN hrg.Name LIKE 'HR2%' THEN 
                CASE 
                    WHEN gcl.DateType = 1 THEN 1
                    WHEN gcl.ExpiryDate < GETDATE() THEN 3
                    WHEN gcl.ExpiryDate < DATEADD(day, 45, GETDATE()) THEN 2
                    ELSE 1
                END
            END) as HR2_Score,

            MAX(CASE WHEN hrg.Name LIKE 'HR3%' THEN 
                CASE 
                    WHEN gcl.DateType = 1 THEN 1
                    WHEN gcl.ExpiryDate < GETDATE() THEN 3
                    WHEN gcl.ExpiryDate < DATEADD(day, 45, GETDATE()) THEN 2
                    ELSE 1
                END
            END) as HR3_Score

        FROM GuardComplianceLicense gcl
        JOIN HRGroups hrg ON gcl.HrGroup = hrg.Id
        WHERE gcl.GuardId IS NOT NULL
        GROUP BY gcl.GuardId
    )
    
    SELECT   
        a.ClientSiteId,
        a.GuardId,
        '<a ><i class="fa fa-envelope clickenvelope" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"></i> </a>
        <i class="fa fa-building clickbuilding" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'" aria-hidden="true"></i> '+b.Name +'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class="fa fa-phone" aria-hidden="true"></i>'+ ISNULL(b.LandLine, '') as SiteName,
        '<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
        b.Gps as GPS,
        c.Name + CASE WHEN c.Initial IS NOT NULL THEN ' ['+c.Initial+']' ELSE '' END as GuardName,
        
        SUM(CASE WHEN RTRIM(ActivityType)='LB' THEN 1 ELSE 0 END) as LogBook,
        SUM(CASE WHEN RTRIM(ActivityType)='KV' THEN 1 ELSE 0 END) as KeyVehicle,
        SUM(CASE WHEN RTRIM(ActivityType)='IR' THEN 1 ELSE 0 END) as IncidentReport,
        SUM(CASE WHEN RTRIM(ActivityType)='SW' THEN 1 ELSE 0 END) as SmartWands,
        
        b.LandLine,
        d.RadioCheckStatusId as RcStatus,
        d.Status as Status,
        rm.Name as RcColor,
        rm.ColorId as RcColorId,
        b.Name as OnlySiteName,
        
        DATEDIFF(MINUTE, MAX(MaxActivity.LatestDate), GETDATE()) as [LatestDate],
        CASE WHEN DATEDIFF(MINUTE, MAX(MaxActivity.LatestDate), GETDATE()) > 80 THEN 1 ELSE 0 END as ShowColor,
        
        0 as hasmartwand, 
        
        /* Map Scores to Colors */
        CASE ISNULL(ghs.HR1_Score, 0)
            WHEN 3 THEN 'Red'
            WHEN 2 THEN 'Yellow'
            WHEN 1 THEN 'Green'
            ELSE 'Grey'
        END as HR1,
        
        CASE ISNULL(ghs.HR2_Score, 0)
            WHEN 3 THEN 'Red'
            WHEN 2 THEN 'Yellow'
            WHEN 1 THEN 'Green'
            ELSE 'Grey'
        END as HR2,

        CASE ISNULL(ghs.HR3_Score, 0)
            WHEN 3 THEN 'Red'
            WHEN 2 THEN 'Yellow'
            WHEN 1 THEN 'Green'
            ELSE 'Grey'
        END as HR3,

        b.State as State,
        
        /* Optimized Inlined Logic for CompletedRounds */
        ISNULL(RoundsCalc.CompletedRounds, 0) as CompletedRounds,
        
        0 as haswandtags,
        
        CASE b.PatrolTourMode
            WHEN 0 THEN 'STND'
            WHEN 1 THEN 'PCAR'
            WHEN 2 THEN 'INSP'
            ELSE 'Unknown'
        END as TourMode

    FROM ClientSiteRadioChecksActivityStatus as A
    LEFT JOIN ClientSites as b ON A.ClientSiteId = b.Id
    LEFT JOIN Guards as c ON a.GuardId = c.Id
    LEFT JOIN clientSiteRadioChecks as d ON d.ClientSiteId = A.ClientSiteId AND d.GuardId = a.GuardId AND d.Active = 1
    
    LEFT JOIN (
        SELECT rcs.Id as StatusId, rcsc.Name, rcsc.Id as ColorId 
        FROM RadioCheckStatus rcs 
        JOIN RadioCheckStatusColor rcsc ON rcsc.Id = rcs.RadioCheckStatusColorId
    ) as rm ON rm.StatusId = d.RadioCheckStatusId
    
    LEFT JOIN ClientSiteKpiSettings kpi ON kpi.ClientSiteId = A.ClientSiteId

    /* Join HR Status Calculation */
    LEFT JOIN GuardHRStatus ghs ON ghs.GuardId = a.GuardId

    /* Inlined GetFqCompletedRounds Logic */
    OUTER APPLY (
        SELECT MIN(ScanCount) as CompletedRounds
        FROM (
            SELECT 
                COUNT(h.TagUId) AS ScanCount
            FROM ClientSiteSmartWandTags t
            LEFT JOIN ClientSiteSmartWandTagsHitLogs h 
                ON t.UId = h.TagUId 
                AND h.LoggedInClientSiteId = t.ClientSiteId 
                AND CAST(h.HitUtcDateTime AT TIME ZONE 'UTC' AT TIME ZONE ISNULL(kpi.TimezoneString, 'AUS Eastern Standard Time') AS DATE) 
                    = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'UTC' AT TIME ZONE ISNULL(kpi.TimezoneString, 'AUS Eastern Standard Time') AS DATE)
            WHERE 
                t.ClientSiteId = A.ClientSiteId
                AND t.IsDeleted = 0 
                AND t.FqBypass = 0
            GROUP BY t.UId
        ) as TagCounts
    ) as RoundsCalc
    
    CROSS APPLY (
        SELECT MAX(v) as LatestDate
        FROM (VALUES 
            (A.LastIRCreatedTime), 
            (A.LastKVCreatedTime), 
            (A.LastLBCreatedTime), 
            (A.LastSWCreatedTime)
        ) as value(v)
    ) as MaxActivity

    WHERE 
        A.ClientSiteId IS NOT NULL 
        AND c.IsActive = 1 
        AND c.IsRCBypass = 0
        AND NOT (
            A.GuardLoginTime IS NOT NULL 
            AND NOT EXISTS (
                SELECT 1 
                FROM ClientSiteRadioChecksActivityStatus ActivityCheck 
                WHERE ActivityCheck.ClientSiteId = A.ClientSiteId 
                  AND ActivityCheck.GuardId = A.GuardId 
                  AND ActivityCheck.ActivityType IS NOT NULL
            )
        )
        AND NOT EXISTS (
            SELECT 1 
            FROM RCActionList rca 
            WHERE rca.ClientSiteID = A.ClientSiteId 
              AND rca.IsRCBypass = 1
        )

    GROUP BY 
        a.ClientSiteId, b.Name, c.Name, c.Initial, a.GuardId, b.LandLine,
        b.Address, b.Gps, d.RadioCheckStatusId, d.Status, b.State,
        rm.Name, rm.ColorId, 
        b.PatrolTourMode,
        RoundsCalc.CompletedRounds,
        ghs.HR1_Score, ghs.HR2_Score, ghs.HR3_Score /* Include HR scores in group by */
END
GO
