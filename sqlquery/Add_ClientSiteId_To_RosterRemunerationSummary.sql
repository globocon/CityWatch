-- Migration script to add ClientSiteId to RosterRemunerationSummaries and migrate existing data

-- 1. Add ClientSiteId column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterRemunerationSummaries]') AND name = 'ClientSiteId')
BEGIN
    ALTER TABLE [dbo].[RosterRemunerationSummaries] ADD [ClientSiteId] INT NULL;
END
GO

-- 2. Migrate existing records for Guards (assign to the site where they worked the most hours in that week)
WITH GuardSiteHours AS (
    SELECT 
        rrs.Id AS SummaryId,
        rs.ClientSiteId,
        SUM(DATEDIFF(minute, rs.ShiftStart, rs.ShiftEnd)) AS TotalMinutes,
        ROW_NUMBER() OVER(PARTITION BY rrs.Id ORDER BY SUM(DATEDIFF(minute, rs.ShiftStart, rs.ShiftEnd)) DESC) as rn
    FROM [dbo].[RosterRemunerationSummaries] rrs
    JOIN [dbo].[RosterSchedules] rs ON rs.GuardId = rrs.GuardId 
        AND rs.ShiftStart >= rrs.WeekStartDate 
        AND rs.ShiftStart < DATEADD(day, 7, rrs.WeekStartDate)
    WHERE rrs.ClientSiteId IS NULL AND rrs.GuardId IS NOT NULL AND rs.IsDeleted = 0
    GROUP BY rrs.Id, rs.ClientSiteId
)
UPDATE rrs
SET rrs.ClientSiteId = gsh.ClientSiteId
FROM [dbo].[RosterRemunerationSummaries] rrs
JOIN GuardSiteHours gsh ON gsh.SummaryId = rrs.Id
WHERE gsh.rn = 1;
GO

-- 3. Migrate existing records for Providers (no GuardId)
WITH ProviderSiteHours AS (
    SELECT 
        rrs.Id AS SummaryId,
        rs.ClientSiteId,
        SUM(DATEDIFF(minute, rs.ShiftStart, rs.ShiftEnd)) AS TotalMinutes,
        ROW_NUMBER() OVER(PARTITION BY rrs.Id ORDER BY SUM(DATEDIFF(minute, rs.ShiftStart, rs.ShiftEnd)) DESC) as rn
    FROM [dbo].[RosterRemunerationSummaries] rrs
    JOIN [dbo].[RosterSchedules] rs ON (rs.ProviderName = rrs.ProviderName OR rs.ReliefProviderName = rrs.ProviderName)
        AND rs.ShiftStart >= rrs.WeekStartDate 
        AND rs.ShiftStart < DATEADD(day, 7, rrs.WeekStartDate)
    WHERE rrs.ClientSiteId IS NULL AND rrs.GuardId IS NULL AND rrs.ProviderName IS NOT NULL AND rs.IsDeleted = 0
    GROUP BY rrs.Id, rs.ClientSiteId
)
UPDATE rrs
SET rrs.ClientSiteId = psh.ClientSiteId
FROM [dbo].[RosterRemunerationSummaries] rrs
JOIN ProviderSiteHours psh ON psh.SummaryId = rrs.Id
WHERE psh.rn = 1;
GO
