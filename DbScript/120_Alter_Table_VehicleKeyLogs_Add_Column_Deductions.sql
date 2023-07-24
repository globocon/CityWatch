ALTER TABLE [dbo].[VehicleKeyLogs]
ADD MoistureDeduction BIT NOT NULL Default 0
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
ADD RubbishDeduction BIT NOT NULL Default 0
GO

ALTER TABLE [dbo].[VehicleKeyLogs]
ADD DeductionPercentage DECIMAL(6,2) NULL
GO

