CREATE TABLE dbo.KpiSendScheduleSummaryNotes
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ScheduleId] [int] NOT NULL FOREIGN KEY REFERENCES [dbo].[KpiSendSchedules] ([Id]) ON DELETE CASCADE,
	[ForMonth] [date] NOT NULL,
	[Notes] [varchar](2048) NULL,
)