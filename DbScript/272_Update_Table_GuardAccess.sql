select * from GuardAccess

update GuardAccess set AccessName='RC (Lite)' where id=5
update GuardAccess set AccessName='RC' where id=6
update GuardAccess set AccessName='RC + HR' where id=7

update GuardAccess set AccessName='RC + Fusion' where id=8
update GuardAccess set AccessName='ADMIN - Power User' where id=9
update GuardAccess set AccessName='ADMIN - SOP & Tools' where id=10

update GuardAccess set AccessName='ADMIN - Auditor' where id=11
update GuardAccess set AccessName='ADMIN - Investigator' where id=12
update GuardAccess set AccessName='ADMIN - 3rd Party' where id=13
Insert into GuardAccess(AccessName) values('ADMIN - Global')

Alter table Guards add  IsRCLiteAccess bit

update Guards set IsRCLiteAccess=0


