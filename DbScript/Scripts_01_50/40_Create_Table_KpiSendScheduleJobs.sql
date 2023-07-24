CREATE TABLE dbo.KpiSendScheduleJobs
(
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	CreatedDate DateTime NOT NULL,
	CompletedDate DateTime NULL,
	Success BIT NULL,
	StatusMessage VARCHAR(max) NULL
)