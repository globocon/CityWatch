/* ============================================================================
   366_Enrol_PatrolCar_Positions_For_Tracking.sql
   Enrol the PATROL CARS themselves, not just wand devices.

   Why this exists: patrol officers routinely log in WITHOUT selecting a
   SmartWand (every observed PCAR login has SmartWandId NULL), so a
   wand-only enrolment can never let a patrol car track. The car is the
   Position picked at login, so the Position is what must be enrolled.

   Unit key spaces share the UnitId column and are kept apart by an offset
   (see CityWatch.Tracking.Contracts.TrackingUnitKey):

       UnitId  <  2000000   a SmartWand device   (ClientSiteSmartWands.Id)
       UnitId >= 2000000    a patrol car         (IncidentReportPositions.Id + 2000000)

   So "Mobile Patrols (Car) M1" (position 10) becomes unit 2000010.

   ⚠ LEGAL: as with 363, this records that the written tracking notice was
   given. Set @NoticeReference to the real memo and only run in production
   AFTER the notice has actually been issued to staff.

   Idempotent: inserts what is missing, re-enables what was disabled, and
   never overwrites an existing consent date. Run after 360.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @now datetime2(0) = SYSUTCDATETIME();
DECLARE @offset int = 2000000;
DECLARE @NoticeReference nvarchar(200) = N'Company-wide tracking notice — management direction 07 Aug 2026';

INSERT INTO dbo.TrackingUnitEnrolment
    (UnitId, IsEnabled, EnrolledUtc, EnrolledByUserId, ConsentRecordedUtc, ConsentReference, Notes)
SELECT  p.Id + @offset, 1, @now, 0, @now, @NoticeReference,
        CONCAT(N'Patrol car position: ', p.Name)
FROM    dbo.IncidentReportPositions p
WHERE   p.IsPatrolCar = 1
  AND   NOT EXISTS (SELECT 1 FROM dbo.TrackingUnitEnrolment e WHERE e.UnitId = p.Id + @offset);

DECLARE @inserted int = @@ROWCOUNT;

UPDATE  e SET
        e.IsEnabled          = 1,
        e.DisabledUtc        = NULL,
        e.ConsentRecordedUtc = COALESCE(e.ConsentRecordedUtc, @now),
        e.ConsentReference   = COALESCE(e.ConsentReference, @NoticeReference)
FROM    dbo.TrackingUnitEnrolment e
JOIN    dbo.IncidentReportPositions p ON p.Id + @offset = e.UnitId AND p.IsPatrolCar = 1
WHERE   e.IsEnabled = 0;

PRINT CONCAT('366_Enrol_PatrolCar_Positions_For_Tracking: ', @inserted,
             ' patrol car(s) enrolled, ', @@ROWCOUNT, ' re-enabled.');
GO

/* What is now trackable, for confirmation. */
SELECT  p.Id AS PositionId, p.Name AS PatrolCar, e.UnitId, e.IsEnabled,
        CASE WHEN e.ConsentRecordedUtc IS NOT NULL THEN 'yes' ELSE 'NO - will refuse' END AS Consent
FROM    dbo.IncidentReportPositions p
LEFT JOIN dbo.TrackingUnitEnrolment e ON e.UnitId = p.Id + 2000000
WHERE   p.IsPatrolCar = 1
ORDER BY p.Name;
GO
