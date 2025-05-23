USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetNotAvailableGuardDetailsForRC]    Script Date: 09-11-2023 10:10:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_GetNotAvailableGuardDetailsForRC]
	
AS
BEGIN
	select top 10 A.Id,A.ClientSiteId,A.GuardId,'<a ><i class="fa fa-envelope" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"li></i> </a> <i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end  as SiteName,c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName,
    '<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
	b.Gps as GPS,
	CONVERT(varchar,a.LoginDate,103) as GuardLastLoginDate
	from GuardLogins A
	inner join ClientSites as b on A.ClientSiteId=b.Id
    inner join Guards as c on a.GuardId=c.Id    
	where a.LoginDate in(select max(b.LoginDate) from GuardLogins b where b.GuardId=A.GuardId and b.ClientSiteId=A.ClientSiteId)
	and A.GuardId not in(select  b.GuardId from ClientSiteRadioChecksActivityStatus b)
	--and A.ClientSiteId not in (select b.ClientSiteId from ClientSiteRadioChecksActivityStatus b)
	and c.IsActive=1
	
END