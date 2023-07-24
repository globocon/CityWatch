-- This is a onetime data script to order days in [ClientSiteDayKpiSettings]
-- For some old settings weekdays are in format SUN,MON..SAT. This should be changed to MON,TUE..SAT,SUN

-- Find all settings id
SELECT SettingsId FROM [ClientSiteDayKpiSettings] WHERE Id IN (SELECT MIN(Id) FROM [ClientSiteDayKpiSettings] GROUP BY SettingsId) AND [WeekDay] = 0


-- Process
DECLARE @SettingId int
SELECT @SettingId = 0 -- change id

SELECT * FROM [ClientSiteDayKpiSettings] WHERE SettingsId = @SettingId

DECLARE @ClientSiteDayKpiSettings TABLE 
(
	Id INT, 
	SettingsId INT, 
	[WeekDay] INT, 
	EmpHours DECIMAL(5, 2), 
	PatrolFrequency INT, 
	NoOfPatrols INT, 
	ImagesTarget DECIMAL(5, 2), 
	WandScansTarget DECIMAL(5, 2)
)

INSERT INTO @ClientSiteDayKpiSettings (Id, SettingsId, [WeekDay], EmpHours, PatrolFrequency, NoOfPatrols, ImagesTarget, WandScansTarget) 
SELECT Id, SettingsId, [WeekDay], EmpHours, PatrolFrequency, NoOfPatrols, ImagesTarget, WandScansTarget FROM [dbo].[ClientSiteDayKpiSettings] WHERE SettingsId = @SettingId AND [Weekday] > 0 and [WeekDay] <= 6
UNION  
SELECT Id, SettingsId, [WeekDay], EmpHours, PatrolFrequency, NoOfPatrols, ImagesTarget, WandScansTarget FROM [dbo].[ClientSiteDayKpiSettings] WHERE SettingsId = @SettingId AND [weekday] <= 0

DELETE FROM [dbo].[ClientSiteDayKpiSettings] WHERE SettingsId = @SettingId

INSERT INTO  [dbo].[ClientSiteDayKpiSettings] (SettingsId, [WeekDay], EmpHours, PatrolFrequency, NoOfPatrols, ImagesTarget, WandScansTarget)
SELECT SettingsId, [WeekDay], EmpHours, PatrolFrequency, NoOfPatrols, ImagesTarget, WandScansTarget FROM @ClientSiteDayKpiSettings

SELECT * FROM [ClientSiteDayKpiSettings] WHERE SettingsId = @SettingId
