/* ============================================================================
   367_Enrol_Guards_For_Tracking.sql
   Enrol GUARDS (foot patrol), completing the unit model.

   The device is never the tracked unit — a "SmartWand" record is just a
   registered phone. What is tracked is a car or a person:

       patrol car -> the Position picked at login   (366 enrols these)
       foot guard -> the guard themselves           (this script)

   Unit key spaces (see CityWatch.Tracking.Contracts.TrackingUnitKey):

       UnitId >= 2000000    patrol car   (IncidentReportPositions.Id + 2000000)
       UnitId >= 1000000    foot guard   (Guards.Id + 1000000)

   So guard 4 (Bruno Timpano) becomes unit 1000004.

   ⚠ LEGAL: as with 363/366 this records that the written tracking notice was
   given. Set @NoticeReference to the real memo and only run in production
   AFTER the notice has actually been issued to staff.

   Idempotent: inserts what is missing, re-enables what was disabled, never
   overwrites an existing consent date. Run after 360.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @now datetime2(0) = SYSUTCDATETIME();
DECLARE @offset int = 1000000;
DECLARE @NoticeReference nvarchar(200) = N'Company-wide tracking notice — management direction 07 Aug 2026';

INSERT INTO dbo.TrackingUnitEnrolment
    (UnitId, IsEnabled, EnrolledUtc, EnrolledByUserId, ConsentRecordedUtc, ConsentReference, Notes)
SELECT  g.Id + @offset, 1, @now, 0, @now, @NoticeReference,
        CONCAT(N'Foot guard: ', g.Name)
FROM    dbo.Guards g
WHERE   NOT EXISTS (SELECT 1 FROM dbo.TrackingUnitEnrolment e WHERE e.UnitId = g.Id + @offset);

DECLARE @inserted int = @@ROWCOUNT;

UPDATE  e SET
        e.IsEnabled          = 1,
        e.DisabledUtc        = NULL,
        e.ConsentRecordedUtc = COALESCE(e.ConsentRecordedUtc, @now),
        e.ConsentReference   = COALESCE(e.ConsentReference, @NoticeReference)
FROM    dbo.TrackingUnitEnrolment e
JOIN    dbo.Guards g ON g.Id + @offset = e.UnitId
WHERE   e.IsEnabled = 0;

PRINT CONCAT('367_Enrol_Guards_For_Tracking: ', @inserted,
             ' guard(s) enrolled, ', @@ROWCOUNT, ' re-enabled.');
GO

/* Summary of everything now trackable. */
SELECT  CASE WHEN UnitId >= 2000000 THEN 'Patrol cars'
             WHEN UnitId >= 1000000 THEN 'Foot guards'
             ELSE 'Legacy device-keyed (no longer issued)' END AS UnitKind,
        COUNT(*)                                              AS Total,
        SUM(CASE WHEN IsEnabled = 1 AND ConsentRecordedUtc IS NOT NULL
                 THEN 1 ELSE 0 END)                           AS ReadyToTrack
FROM    dbo.TrackingUnitEnrolment
GROUP BY CASE WHEN UnitId >= 2000000 THEN 'Patrol cars'
              WHEN UnitId >= 1000000 THEN 'Foot guards'
              ELSE 'Legacy device-keyed (no longer issued)' END
ORDER BY UnitKind;
GO
