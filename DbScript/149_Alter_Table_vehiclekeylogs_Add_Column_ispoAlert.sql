alter table vehiclekeylogs add  IsPOIAlert bit
update vehiclekeylogs set IsPOIAlert=0
alter table KeyVehicleLogVisitorPersonalDetails add IsPOIAlert bit
update KeyVehicleLogVisitorPersonalDetails set IsPOIAlert=0