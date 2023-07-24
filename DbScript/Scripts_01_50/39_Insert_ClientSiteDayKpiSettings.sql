CREATE TABLE TempDaySettings
(
	SettingsId INT,
	[WeekDay] INT,
	NoOfPatrols INT,
	EmpHours decimal(5,2),
	ImagesTarget decimal(5,2),
	WandScansTarget decimal(5,2)
)

INSERT INTO TempDaySettings 
SELECT Id, 1, PatrolsPerHour,  MonEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 2, PatrolsPerHour,  TueEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 3, PatrolsPerHour,  WedEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 4, PatrolsPerHour,  ThuEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 5, PatrolsPerHour,  FriEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 6, PatrolsPerHour,  SatEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]
UNION ALL
SELECT Id, 0, PatrolsPerHour,  SunEmpHrs, DailyImagesTarget, DailyWandScansTarget FROM [ClientSiteKpiSettings]

INSERT INTO [ClientSiteDayKpiSettings] (SettingsId, [WeekDay], NoOfPatrols, EmpHours, ImagesTarget, WandScansTarget )
SELECT SettingsId, [WeekDay], NoOfPatrols, EmpHours, ImagesTarget, WandScansTarget FROM TempDaySettings ORDER BY SettingsId, [WeekDay]

DROP TABLE TempDaySettings