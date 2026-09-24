alter table RCLinkedDuressMaster add IsLB bit default 0,IsKV bit default 0,IsSW bit default 0

update RCLinkedDuressMaster set IsLB = 0,IsKV = 0,IsSW = 0

select * from RCLinkedDuressMaster