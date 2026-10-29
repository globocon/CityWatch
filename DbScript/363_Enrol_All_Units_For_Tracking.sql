/* ============================================================================
   363_Enrol_All_Units_For_Tracking.sql
   CityWatch.Tracking feature pack — bulk enrolment (management direction:
   enable GPS tracking for all staff, no per-unit admin step).

   Enrols EVERY active SmartWand (patrol-car wands AND guard wands) with
   tracking ENABLED and the consent/notice date stamped.

   ⚠ LEGAL PREREQUISITE (ADD §13.8): employees must receive WRITTEN NOTICE
   before tracking starts (NSW: ~14 days). Running this script records that
   the notice was given — set @NoticeReference to the real memo/notice id
   and only run it in production AFTER the notice has actually gone out.

   Idempotent: inserts missing units, re-enables previously disabled ones,
   never overwrites an existing consent date.
   Rollback (all units, instant):
       UPDATE dbo.TrackingUnitEnrolment SET IsEnabled = 0;
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @now datetime2(0) = SYSUTCDATETIME();
DECLARE @NoticeReference nvarchar(200) = N'Company-wide tracking notice — management direction 07 Aug 2026';

/* New enrolments: every active wand not yet known to tracking. */
INSERT INTO dbo.TrackingUnitEnrolment
    (UnitId, IsEnabled, EnrolledUtc, EnrolledByUserId, ConsentRecordedUtc, ConsentReference, Notes)
SELECT
    csw.Id, 1, @now, 0, @now, @NoticeReference,
    CASE WHEN csw.PatrolCarId IS NOT NULL THEN N'Bulk enrolment (363): patrol car'
         ELSE N'Bulk enrolment (363): guard wand' END
FROM dbo.ClientSiteSmartWands csw
WHERE csw.IsDeleted = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.TrackingUnitEnrolment e WHERE e.UnitId = csw.Id);

DECLARE @inserted int = @@ROWCOUNT;

/* Re-enable any previously disabled enrolments; keep their original consent
   date if one exists (never rewrite history), stamp it only where missing. */
UPDATE e SET
    e.IsEnabled = 1,
    e.DisabledUtc = NULL,
    e.ConsentRecordedUtc = COALESCE(e.ConsentRecordedUtc, @now),
    e.ConsentReference   = COALESCE(e.ConsentReference, @NoticeReference)
FROM dbo.TrackingUnitEnrolment e
JOIN dbo.ClientSiteSmartWands csw ON csw.Id = e.UnitId AND csw.IsDeleted = 0
WHERE e.IsEnabled = 0;

PRINT CONCAT('363_Enrol_All_Units_For_Tracking: ', @inserted, ' new unit(s) enrolled, ',
             @@ROWCOUNT, ' re-enabled. All active wands are now tracking-enabled.');
GO
