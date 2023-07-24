--select * from VehicleKeyLogs where Plate is null
--select * from KeyVehicleLogProfiles where Plate is null
--select * from KeyVehcileLogFields where TypeId = 6

update vl 
set vl.PlateId = f.Id
from VehicleKeyLogs vl
inner join KeyVehcileLogFields f
on f.Name = vl.Plate

update p 
set p.PlateId = f.Id
from KeyVehicleLogProfiles p
inner join KeyVehcileLogFields f
on f.Name = p.Plate