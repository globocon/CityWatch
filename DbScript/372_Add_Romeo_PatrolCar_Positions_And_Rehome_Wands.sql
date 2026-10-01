/* =====================================================================
   372: One tracking identity per Romeo patrol car (P4 GLOBE, 24 Aug 2026).

   Root cause fixed here: the only patrol-car Position was "Mobile
   Patrols (Car) M1" (Id 10), so all six Romeo cars logged in as the SAME
   tracking unit (2000010) and superseded each other off the live map all
   shift, and all six fleet phones (wands R01-R06) were registered with
   PatrolCarId = 10 — the whole fleet configured as one car.

   This script:
     1. Creates positions R1-R6, cloning every attribute except the name
        from M1 (Id 10) so email routing / logbook / site linkage behave
        identically.
     2. Enrols each new position for tracking (same shape as DbScript
        366 — one consent notice reference).
     3. Rehomes each fleet wand to its own car: wand 'R01' -> position
        'Mobile Patrols (Car) R1', ... 'R06' -> R6. Spare wands (R69,
        R99 — PatrolCarId already NULL) are untouched.

   ClientSiteSmartWands.PatrolCarId references IncidentReportPositions.Id
   (see ClientSiteWandDataProvider.GetPatrolCarPositions).

   No APK required. Idempotent. Rollback: DELETE the R1-R6 positions and
   their TrackingUnitEnrolment rows; restore wand PatrolCarId to 10.
   ===================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @Cars TABLE (WandCode nvarchar(10), PositionName nvarchar(100));
INSERT INTO @Cars (WandCode, PositionName) VALUES
    (N'R01', N'Mobile Patrols (Car) R1'),
    (N'R02', N'Mobile Patrols (Car) R2'),
    (N'R03', N'Mobile Patrols (Car) R3'),
    (N'R04', N'Mobile Patrols (Car) R4'),
    (N'R05', N'Mobile Patrols (Car) R5'),
    (N'R06', N'Mobile Patrols (Car) R6');

/* 1. Positions: clone every attribute of M1 (Id 10) except the name. */
INSERT INTO dbo.IncidentReportPositions
    (Name, EmailTo, IsPatrolCar, DropboxDir, IsLogbook, ClientsiteId, ClientsiteName, IsSmartwandbypass)
SELECT c.PositionName, m1.EmailTo, 1, m1.DropboxDir, m1.IsLogbook, m1.ClientsiteId, m1.ClientsiteName, m1.IsSmartwandbypass
FROM @Cars c
CROSS JOIN dbo.IncidentReportPositions m1
WHERE m1.Id = 10   /* 'Mobile Patrols (Car) M1' - the template */
  AND NOT EXISTS (SELECT 1 FROM dbo.IncidentReportPositions p WHERE p.Name = c.PositionName);

PRINT CONCAT('372: positions inserted: ', @@ROWCOUNT);

/* 2. Enrol every patrol-car position that lacks an enrolment (covers the
      new R cars; also self-heals any future position added without one). */
DECLARE @now datetime2(0) = SYSUTCDATETIME();
DECLARE @offset int = 2000000;
DECLARE @NoticeReference nvarchar(200) = N'Company-wide tracking notice - management direction 07 Aug 2026';

INSERT INTO dbo.TrackingUnitEnrolment
    (UnitId, IsEnabled, EnrolledUtc, EnrolledByUserId, ConsentRecordedUtc, ConsentReference, Notes)
SELECT p.Id + @offset, 1, @now, 0, @now, @NoticeReference,
       CONCAT(N'Patrol car position: ', p.Name)
FROM dbo.IncidentReportPositions p
WHERE p.IsPatrolCar = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.TrackingUnitEnrolment e WHERE e.UnitId = p.Id + @offset);

PRINT CONCAT('372: positions enrolled for tracking: ', @@ROWCOUNT);

/* 3. Rehome the fleet wands: each R-phone points at its own car. Only
      rows currently on the shared car (10) are touched, so a manually
      corrected wand is never overwritten. */
UPDATE w SET w.PatrolCarId = p.Id
FROM dbo.ClientSiteSmartWands w
JOIN @Cars c ON c.WandCode = w.SmartWandId
JOIN dbo.IncidentReportPositions p ON p.Name = c.PositionName
WHERE w.IsDeleted = 0
  AND ISNULL(w.PatrolCarId, 10) = 10;

PRINT CONCAT('372: wands rehomed to their own cars: ', @@ROWCOUNT);
GO

/* 4. Confirm: every patrol-car position with enrolment state + its wand. */
SELECT p.Id AS PositionId, p.[Name] AS PatrolCar, p.Id + 2000000 AS UnitId,
       CASE WHEN e.UnitId IS NULL THEN 'NOT ENROLLED - will refuse'
            WHEN e.IsEnabled = 0 THEN 'DISABLED'
            WHEN e.ConsentRecordedUtc IS NULL THEN 'NO CONSENT - will refuse'
            ELSE 'OK - trackable' END AS EnrolmentState,
       w.SmartWandId AS Wand, w.PhoneNumber
FROM dbo.IncidentReportPositions p
LEFT JOIN dbo.TrackingUnitEnrolment e ON e.UnitId = p.Id + 2000000
LEFT JOIN dbo.ClientSiteSmartWands w ON w.PatrolCarId = p.Id AND w.IsDeleted = 0
WHERE p.IsPatrolCar = 1
ORDER BY p.[Name];
GO
