
alter table clientsites add  IsActive bit not null default 1
select * from clientsites