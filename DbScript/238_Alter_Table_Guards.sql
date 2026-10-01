Alter table Guards add IsAdminPowerUser bit default 0
Alter table Guards add IsAdminGlobal bit default 0

update Guards set IsAdminPowerUser=0
update Guards set IsAdminGlobal=0