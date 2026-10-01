/* ============================================================================
   365_Alter_TrackingSession_Add_PatrolCar_Position_And_State.sql
   CityWatch.Tracking — the patrol car IS the Position chosen at login.

   Corrects the earlier model, which keyed the tracked unit on the SmartWand
   device. That was wrong for the real workflow: several cars of the same fleet
   (Mobile Patrols Car M1, M2, M3 …) roam the same sites at once and all scan the
   SAME site tags. What tells them apart is the Position the officer picks at
   login — so Position is the car, and Callsign is its radio label.

   Also records where the car is right now (CurrentSite) and whether it is at a
   site or travelling (TravelState), both driven by NFC scans:
       tag belongs to the logged-in fleet site  -> in-car tag -> Transit
       tag belongs to any other site            -> arrived    -> AtSite

   TravelState is for DISPLAY and leg boundaries only. GPS sampling stays
   continuous (adaptive: ~60 s parked, ~10 s driving) so a missed scan can never
   lose a journey.

   Alters only the pack's own table. Idempotent. Run after 360.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.TrackingSession', 'PatrolCarPositionId') IS NULL
    ALTER TABLE dbo.TrackingSession ADD PatrolCarPositionId int NULL;
GO

IF COL_LENGTH('dbo.TrackingSession', 'PatrolCarPositionName') IS NULL
    ALTER TABLE dbo.TrackingSession ADD PatrolCarPositionName nvarchar(120) NULL;
GO

IF COL_LENGTH('dbo.TrackingSession', 'CurrentSiteId') IS NULL
    ALTER TABLE dbo.TrackingSession ADD CurrentSiteId int NULL;
GO

IF COL_LENGTH('dbo.TrackingSession', 'CurrentSiteName') IS NULL
    ALTER TABLE dbo.TrackingSession ADD CurrentSiteName nvarchar(200) NULL;
GO

IF COL_LENGTH('dbo.TrackingSession', 'TravelState') IS NULL
    ALTER TABLE dbo.TrackingSession ADD TravelState nvarchar(20) NOT NULL
        CONSTRAINT DF_TrackingSession_TravelState DEFAULT ('Transit');
GO

IF COL_LENGTH('dbo.TrackingSession', 'TravelStateSinceUtc') IS NULL
    ALTER TABLE dbo.TrackingSession ADD TravelStateSinceUtc datetime2(0) NULL;
GO

/* One active session per CAR, not per device: two officers must never end up
   sharing a Position while both are on shift. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrackingSession_Position_Status')
    CREATE INDEX IX_TrackingSession_Position_Status
        ON dbo.TrackingSession (PatrolCarPositionId, Status);
GO

PRINT '365_Alter_TrackingSession_Add_PatrolCar_Position_And_State: complete.';
GO
