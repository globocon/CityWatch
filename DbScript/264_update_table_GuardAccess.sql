select * from GuardAccess

update GuardAccess set AccessName='STATS + Charts' where id=3
update GuardAccess set AccessName='KPI' where id=4
update GuardAccess set AccessName='RC' where id=5
update GuardAccess set AccessName='RC + Fusion' where id=6
insert into GuardAccess(AccessName) values('ADMIN - Power User')
insert into GuardAccess(AccessName) values('ADMIN - SOP & Tools')
insert into GuardAccess(AccessName) values('ADMIN - Auditor')
insert into GuardAccess(AccessName) values('ADMIN - Investigator')
insert into GuardAccess(AccessName) values('ADMIN - 3rd Party')
insert into GuardAccess(AccessName) values('ADMIN - Global')

select * from Guards

alter table Guards add IsSTATSChartsAccess bit
update Guards set IsSTATSChartsAccess=0 

alter table Guards add IsRCFusionAccess bit
update Guards set IsRCFusionAccess=0 

alter table Guards add IsAdminSOPToolsAccess bit
update Guards set IsAdminSOPToolsAccess=0 

alter table Guards add IsAdminAuditorAccess bit
update Guards set IsAdminAuditorAccess=0 

alter table Guards add IsAdminInvestigatorAccess bit
update Guards set IsAdminInvestigatorAccess=0 

alter table Guards add IsAdminThirdPartyAccess bit
update Guards set IsAdminThirdPartyAccess=0 
