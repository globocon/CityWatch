

insert into  guardaccess (AccessName ) values ('ADMIN - Roster')
update guardaccess set CredentialOrder=13 where id=18
update guardaccess set CredentialOrder=14 where id=11
update guardaccess set CredentialOrder=15 where id=12
update guardaccess set CredentialOrder=16 where id=13
update guardaccess set CredentialOrder=17 where id=17



alter table Guards add IsAdminRosterAccess bit default 0
update Guards set IsAdminRosterAccess=0