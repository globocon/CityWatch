-- One time script
-- to update new column

Update ah 
set ah.ProfileId = p.Id
from KeyVehicleLogAuditHistory ah
inner join VehicleKeyLogs kvl
ON kvl.Id = ah.KeyVehicleLogId
inner join KeyVehicleLogVisitorProfiles p
on p.VehicleRego = kvl.VehicleRego

UPDATE KeyVehicleLogAuditHistory 
SET ProfileId = -1 
where ProfileId = 0 