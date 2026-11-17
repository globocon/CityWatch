
alter table HrSettings add  IsDeleted bit default 0
update HrSettings set IsDeleted=0 


