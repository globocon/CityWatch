ALTER TABLE [dbo].[VehicleKeyLogs]
ADD TimeSlotNo VARCHAR(50) NULL
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
ADD TruckConfig INT NULL
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
Add TrailerType INT NULL
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
ADD MaxWeight DECIMAL(6,2) NULL
GO


ALTER TABLE [dbo].[VehicleKeyLogs]
ADD Trailer4Rego VARCHAR(20) NULL
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
ADD EntryReason INT NULL
GO