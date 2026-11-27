Alter Table VehicleKeyLogs add IsBDM bit not null default 1


Alter Table VehicleKeyLogs add IndividualTitle nvarchar(max)
Alter Table VehicleKeyLogs add Gender nvarchar(max)
Alter Table VehicleKeyLogs add CompanyABN nvarchar(max)
Alter Table VehicleKeyLogs add CompanyLandline nvarchar(max)
Alter Table VehicleKeyLogs add Email nvarchar(max)
Alter Table VehicleKeyLogs add Website nvarchar(max)
Alter Table VehicleKeyLogs add CRMId nvarchar(max)
Alter Table VehicleKeyLogs add BDMList nvarchar(max)
Select * from VehicleKeyLogs


Alter Table KeyVehicleLogVisitorPersonalDetails add IsBDM bit not null default 1
Alter Table KeyVehicleLogVisitorPersonalDetails add IndividualTitle nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add Gender nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add CompanyABN nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add CompanyLandline nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add Email nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add Website nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add CRMId nvarchar(max)
Alter Table KeyVehicleLogVisitorPersonalDetails add BDMList nvarchar(max)

