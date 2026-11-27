/* ============================================================================
   361_Seed_Tracking_Enrolment.sql
   CityWatch.Tracking feature pack — enrolment seed (ADD §3.4)

   Creates one DISABLED, consent-pending enrolment row per existing patrol-car
   wand, so the /Admin/TrackingEnrolment page has the fleet listed and an
   administrator only has to record consent and flip IsEnabled.

   NOTHING is enabled by this script. Idempotent — inserts only missing rows.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

INSERT INTO dbo.TrackingUnitEnrolment (UnitId, IsEnabled, EnrolledUtc, EnrolledByUserId, Notes)
SELECT
    csw.Id,
    0,                              -- disabled until consent is recorded (§13.5)
    SYSUTCDATETIME(),
    0,                              -- system-seeded; no administrator has acted yet
    N'Seeded by 361: pending consent + explicit enablement'
FROM dbo.ClientSiteSmartWands csw
WHERE csw.IsDeleted = 0
  AND csw.PatrolCarId IS NOT NULL   -- patrol-car wands only: the tracking unit population
  AND NOT EXISTS (SELECT 1 FROM dbo.TrackingUnitEnrolment e WHERE e.UnitId = csw.Id);

PRINT CONCAT('361_Seed_Tracking_Enrolment: ', @@ROWCOUNT, ' unit(s) seeded (all disabled).');
GO
