-- Database updates for Roster Email Settings Feature
-- Run these scripts manually against the CityWatch database.

BEGIN TRANSACTION;

-- 1. Add ROMail to CompanyDetails
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'ROMail' AND Object_ID = Object_ID(N'CompanyDetails'))
BEGIN
    ALTER TABLE CompanyDetails ADD ROMail NVARCHAR(MAX) NULL;
END

-- 2. Add columns to RosterBinder (Projects)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AlertEmailRecipients' AND Object_ID = Object_ID(N'RosterBinder'))
BEGIN
    ALTER TABLE RosterBinder ADD AlertEmailRecipients NVARCHAR(MAX) NULL;
    ALTER TABLE RosterBinder ADD AlertOnRejectedShift BIT NOT NULL DEFAULT 0;
    ALTER TABLE RosterBinder ADD AlertOnReliefGuard BIT NOT NULL DEFAULT 0;
END

-- 3. Add columns to RosterGroup
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AlertEmailRecipients' AND Object_ID = Object_ID(N'RosterGroup'))
BEGIN
    ALTER TABLE RosterGroup ADD AlertEmailRecipients NVARCHAR(MAX) NULL;
    ALTER TABLE RosterGroup ADD AlertOnRejectedShift BIT NOT NULL DEFAULT 0;
    ALTER TABLE RosterGroup ADD AlertOnReliefGuard BIT NOT NULL DEFAULT 0;
END

-- 4. Add columns to ShiftCancellationEmailQueue to track relief assignments
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsReliefAssigned' AND Object_ID = Object_ID(N'ShiftCancellationEmailQueue'))
BEGIN
    ALTER TABLE ShiftCancellationEmailQueue ADD IsReliefAssigned BIT NOT NULL DEFAULT 0;
    ALTER TABLE ShiftCancellationEmailQueue ADD ReliefGuardId INT NULL;
    ALTER TABLE ShiftCancellationEmailQueue ADD ReliefGuardName NVARCHAR(255) NULL;
END

COMMIT TRANSACTION;
