

update GuardAccess set AccessName='LB,KV,IR,MA' where id=1
update GuardAccess set AccessName='Mobile App + Tags' where id=2
update GuardAccess set AccessName='PCAR' where id=3
update GuardAccess set AccessName='RC (Lite)' where id=4
update GuardAccess set AccessName='RC' where id=5
update GuardAccess set AccessName='RC + HR' where id=6
update GuardAccess set AccessName='RC + Fusion' where id=7
update GuardAccess set AccessName='STATS' where id=8
update GuardAccess set AccessName='STATS + Charts' where id=9
update GuardAccess set AccessName='KPI' where id=10
update GuardAccess set AccessName='ADMIN - Power User' where id=11
update GuardAccess set AccessName='ADMIN - SOP & Tools' where id=12
update GuardAccess set AccessName='ADMIN - Auditor' where id=13
update GuardAccess set AccessName='ADMIN - Investigator' where id=14
update GuardAccess set AccessName='ADMIN - 3rd Party' where id=15
update GuardAccess set AccessName='ADMIN - Global' where id=16
delete from GuardAccess where id=17


