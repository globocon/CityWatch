
alter table GuardAccess add IsDeleted bit default 0
update GuardAccess set IsDeleted=0
update GuardAccess set IsDeleted=1 where id=15
update GuardAccess set AccessName='LB,KV,IR,MA' where id=1
alter table GuardAccess add CredentialOrder int
update GuardAccess set CredentialOrder=1 where id=1
update GuardAccess set CredentialOrder=2 where id=16
update GuardAccess set CredentialOrder=3 where id=17
update GuardAccess set CredentialOrder=4 where id=5
update GuardAccess set CredentialOrder=5 where id=6
update GuardAccess set CredentialOrder=6 where id=7
update GuardAccess set CredentialOrder=7 where id=8
update GuardAccess set CredentialOrder=8 where id=2
update GuardAccess set CredentialOrder=9 where id=3
update GuardAccess set CredentialOrder=10 where id=4
update GuardAccess set CredentialOrder=11 where id=9
update GuardAccess set CredentialOrder=12 where id=10
update GuardAccess set CredentialOrder=13 where id=11
update GuardAccess set CredentialOrder=14 where id=12
update GuardAccess set CredentialOrder=15 where id=13
update GuardAccess set CredentialOrder=16 where id=14