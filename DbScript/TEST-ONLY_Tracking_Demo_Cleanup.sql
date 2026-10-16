/* ============================================================================
   TEST-ONLY_Tracking_Demo_Cleanup.sql
   Removes everything TEST-ONLY_Tracking_Demo_Units.sql created (units 9001/9002).
   Leaves the tracking schema and any real data untouched.
   ============================================================================ */
SET NOCOUNT ON;

DELETE FROM dbo.TrackPoint            WHERE UnitId IN (9001, 9002);
DELETE FROM dbo.TrackSegment          WHERE UnitId IN (9001, 9002);
DELETE FROM dbo.TrackingModeCommand   WHERE UnitId IN (9001, 9002);
DELETE FROM dbo.TrackingAccessAudit   WHERE UnitId IN (9001, 9002);
DELETE FROM dbo.TrackingSession       WHERE UnitId IN (9001, 9002);
DELETE FROM dbo.TrackingUnitEnrolment WHERE UnitId IN (9001, 9002);
DELETE FROM dbo.ClientSiteSmartWands  WHERE Id = 9001 AND SmartWandId = N'DEMO-WAND-CAR';

SELECT 'demo units removed' AS status;
