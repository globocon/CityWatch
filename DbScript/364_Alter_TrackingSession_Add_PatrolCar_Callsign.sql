/* ============================================================================
   364_Alter_TrackingSession_Add_PatrolCar_Callsign.sql
   CityWatch.Tracking feature pack — capture the guard's own login declarations.

   The mobile login screen already has a "Mobile Patrol Car" toggle and a Callsign
   picker. Those beat any server-side guess about what a unit is: the same wand can
   be in a car today and on foot tomorrow, and the callsign ("Romeo 1") is what
   operators actually use on the radio.

   Alters only the feature pack's OWN table (dbo.TrackingSession) — no platform
   table is touched. Idempotent. Run after 360.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TrackingSession', 'IsPatrolCar') IS NULL
    ALTER TABLE dbo.TrackingSession ADD IsPatrolCar bit NULL;
GO

IF COL_LENGTH('dbo.TrackingSession', 'Callsign') IS NULL
    ALTER TABLE dbo.TrackingSession ADD Callsign nvarchar(50) NULL;
GO

PRINT '364_Alter_TrackingSession_Add_PatrolCar_Callsign: complete.';
GO
