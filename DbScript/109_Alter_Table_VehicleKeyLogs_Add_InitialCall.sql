ALTER TABLE [dbo].[VehicleKeyLogs]
ADD InitialCallTime datetime NULL;
Go

ALTER TABLE [dbo].[VehicleKeyLogs] 
ALTER COLUMN [EntryTime] [datetime] NULL;
Go
