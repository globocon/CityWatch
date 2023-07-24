
CREATE TABLE dbo.KpiSendScheduleSummaryImages
(
	[Id] [int] IDENTITY(1,1) NOT NULL  PRIMARY KEY,
	[ScheduleId] [int] NOT NULL FOREIGN KEY REFERENCES [dbo].[KpiSendSchedules] ([Id]) ON DELETE CASCADE,
	[FileName] [varchar](200) NULL,
	[LastUpdated] [DateTime] NULL,
) 

