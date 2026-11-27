
alter table VehicleKeyLogs drop column IsPOIAlert
alter table VehicleKeyLogs add  PersonOfInterest int
alter table KeyVehicleLogVisitorPersonalDetails drop column IsPOIAlert
alter table KeyVehicleLogVisitorPersonalDetails add  PersonOfInterest int