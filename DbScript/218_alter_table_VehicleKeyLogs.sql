
alter table vehiclekeylogs add IsReels bit default 1
update vehiclekeylogs set IsReels=1

alter table vehiclekeylogs add IsVWI bit default 1
update vehiclekeylogs set IsVWI=1