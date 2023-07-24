CREATE TABLE [dbo].[ClientSiteDayKpiSettings]
(
	[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	SettingsId INT NOT NULL FOREIGN KEY References ClientSiteKpiSettings(Id) ON DELETE CASCADE,
	[WeekDay] INT NOT NULL,
	PatrolFrequency INT NOT NULL Default 0,
	NoOfPatrols INT NULL,
	EmpHours DECIMAL(5,2) NULL,
	ImagesTarget DECIMAL(5,2) NULL,
	WandScansTarget DECIMAL(5,2) NULL
)