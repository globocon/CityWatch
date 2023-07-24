INSERT INTO [dbo].[IncidentReportPositions]
SELECT [Name], EmailTo, 0, NULL FROM [dbo].[IncidentReportFields] WHERE TypeId = 1

GO

Update IncidentReportPositions set IsPatrolCar = 1 where name like 'Mobile Patrols (Car)%'

GO