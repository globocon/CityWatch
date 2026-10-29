alter table vehicleKeylogs add IsDocketNo bit
update VehicleKeyLogs set IsDocketNo=0

alter table vehicleKeylogs add LoaderName nvarchar(Max)
alter table vehicleKeylogs add DispatchName nvarchar(Max)