alter table IncidentReports add PlateId int
alter table IncidentReports add VehicleRego nvarchar(Max)
alter table IncidentReports add LogId int
update IncidentReports set PlateId=0

