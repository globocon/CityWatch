/* ============================================================================
   362_Rollback_Tracking_Schema.sql
   CityWatch.Tracking feature pack — Level-4 uninstall (ADD §17)

   Drops everything 360/361 created, in dependency order. Clean and complete
   BECAUSE the feature pack's tables have no foreign keys in or out and no
   existing table gained a column — that discipline (D7) is what makes this
   script safe. Idempotent.

   ⚠ DESTROYS tracking history. Level 1 (Tracking:Enabled=false) and Level 2
   (UPDATE TrackingUnitEnrolment SET IsEnabled=0) are the operational rollbacks;
   run this only for a deliberate, final uninstall.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingAccessAudit'   AND schema_id = SCHEMA_ID('dbo')) DROP TABLE dbo.TrackingAccessAudit;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingModeCommand'   AND schema_id = SCHEMA_ID('dbo')) DROP TABLE dbo.TrackingModeCommand;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingUnitEnrolment' AND schema_id = SCHEMA_ID('dbo')) DROP TABLE dbo.TrackingUnitEnrolment;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackingSession'       AND schema_id = SCHEMA_ID('dbo')) DROP TABLE dbo.TrackingSession;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackSegment'          AND schema_id = SCHEMA_ID('dbo')) DROP TABLE dbo.TrackSegment;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TrackPoint'            AND schema_id = SCHEMA_ID('dbo')) DROP TABLE dbo.TrackPoint;
GO

IF EXISTS (SELECT 1 FROM sys.partition_schemes   WHERE name = 'PS_TrackPoint_Monthly') DROP PARTITION SCHEME PS_TrackPoint_Monthly;
IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'PF_TrackPoint_Monthly') DROP PARTITION FUNCTION PF_TrackPoint_Monthly;
GO

PRINT '362_Rollback_Tracking_Schema: complete — zero tracking objects remain.';
GO
