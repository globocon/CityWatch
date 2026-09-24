
select * from GuardAccess
update GuardAccess set AccessName='RC' where id=5
update GuardAccess set AccessName='RC + HR' where id=6
update GuardAccess set AccessName='RC + Fusion' where id=7

update GuardAccess set AccessName='ADMIN - Power User' where id=8
update GuardAccess set AccessName='ADMIN - SOP & Tools' where id=9
update GuardAccess set AccessName='ADMIN - Auditor' where id=10

update GuardAccess set AccessName='ADMIN - Investigator' where id=11
update GuardAccess set AccessName='ADMIN - 3rd Party' where id=12
insert into GuardAccess (AccessName) values('ADMIN - Global') 

select * from Guards
alter table Guards add IsRCHRAccess bit
update Guards set IsRCHRAccess=0