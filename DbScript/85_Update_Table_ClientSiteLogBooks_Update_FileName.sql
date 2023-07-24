declare @versions table 
(
	DateStart date,
	VersionNo varchar(20) 
);

insert into @versions 
select '2022-12-21', '1.14.9.0' union
select '2022-12-20', '1.14.9.0' union
select '2022-12-19', '1.14.9.0' union
select '2022-12-18', '1.14.9.0' union
select '2022-12-17', '1.14.9.0' union
select '2022-12-16', '1.14.8.0' union
select '2022-12-15', '1.14.8.0' union
select '2022-12-14', '1.14.7.1' union
select '2022-12-13', '1.14.7.0' union
select '2022-12-12', '1.14.7.0' union
select '2022-12-11', '1.14.7.0' union
select '2022-12-10', '1.14.7.0' union
select '2022-12-09', '1.14.7.0' union
select '2022-12-08', '1.14.7.0' union
select '2022-12-07', '1.14.7.0' union
select '2022-12-06', '1.14.6.0' union
select '2022-12-05', '1.14.6.0' union
select '2022-12-04', '1.14.6.0' union
select '2022-12-03', '1.14.6.0' union
select '2022-12-02', '1.14.6.0' union
select '2022-12-01', '1.14.6.0' union
select '2022-11-30', '1.14.5.0' union
select '2022-11-29', '1.14.4.0' union
select '2022-11-28', '1.14.3.0' union
select '2022-11-27', '1.14.3.0' union
select '2022-11-26', '1.14.3.0' union
select '2022-11-25', '1.14.3.0' union
select '2022-11-24', '1.14.1.0' union
select '2022-11-23', '1.14.0.0' union
select '2022-11-22', '1.13.10.0' union
select '2022-11-21', '1.13.10.0' union
select '2022-11-20', '1.13.10.0' union
select '2022-11-19', '1.13.10.0' union
select '2022-11-18', '1.13.9.0' union
select '2022-11-17', '1.13.9.0' union
select '2022-11-16', '1.13.9.0' union
select '2022-11-15', '1.13.9.0' union
select '2022-11-14', '1.13.9.0' union
select '2022-11-13', '1.13.9.0' union
select '2022-11-12', '1.13.9.0' union
select '2022-11-11', '1.13.9.0' union
select '2022-11-10', '1.13.8.0' union
select '2022-11-09', '1.13.8.0' union
select '2022-11-08', '1.13.8.0' union
select '2022-11-07', '1.13.8.0' union
select '2022-11-06', '1.13.8.0' union
select '2022-11-05', '1.13.8.0' union
select '2022-11-04', '1.13.7.0' union
select '2022-11-03', '1.13.7.0' union
select '2022-11-02', '1.13.7.0' union
select '2022-11-01', '1.13.6.0'

update lb
set lb.[FileName] = CONVERT(char(8), lb.[Date], 112) + ' - Daily Guard Log - v' + v.VersionNo + '.pdf'
from ClientSiteLogBooks lb 
inner join @versions v
on v.DateStart = lb.[Date] and lb.[type] = 1 and DbxUploaded = 1

delete from @versions

insert into @versions 
select '2022-12-29', '1.14.13.0' union
select '2022-12-28', '1.14.13.0' union
select '2022-12-27', '1.14.13.0' union
select '2022-12-26', '1.14.12.0' union
select '2022-12-25', '1.14.10.0' union
select '2022-12-24', '1.14.10.0' union
select '2022-12-23', '1.14.10.0' union
select '2022-12-22', '1.14.10.0'

update lb
set lb.[FileName] = CONVERT(char(8), lb.[Date], 112) + ' - Daily Guard Log - ' + cs.Name + ' - v' + v.VersionNo + '.pdf'
from ClientSiteLogBooks lb 
inner join @versions v
on v.DateStart = lb.[Date] and lb.[type] = 1 and DbxUploaded = 1
inner join ClientSites cs
on cs.Id = lb.ClientSiteId

select * from ClientSiteLogBooks where [filename] is not null