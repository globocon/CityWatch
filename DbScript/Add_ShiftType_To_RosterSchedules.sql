IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterSchedules]') AND name = 'ShiftType')
BEGIN
    ALTER TABLE [dbo].[RosterSchedules] ADD [ShiftType] NVARCHAR(50) DEFAULT 'Regular';
END
GO
