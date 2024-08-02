select * from GuardLicenses

alter table guards add Gender nvarchar(max)
alter table guards add IsRCBypass bit default 0