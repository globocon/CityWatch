USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetInActiveGuardDetailsForRC]    Script Date: 16-11-2023 15:26:23 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_GetInActiveGuardDetailsForRC]
    
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON;


   -- Insert statements for procedure here


   select * from(
    select   a.ClientSiteId ,a.GuardId,'<a ><i class="fa fa-envelope" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"li></i> </a> <i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end  as SiteName,
	'<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
	b.Gps as GPS,
	c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName,
	CONVERT(varchar,FORMAT(a.GuardLoginTime,'dd MMM yyyy HH:mm'),13) as GuardLoginTime,case when DATEDIFF(hour, a.GuardLoginTime, GETDATE())<2 then 'Green' else ' Red' end  AS TwoHrAlert,d.Status as RcStatus,NotificationType,
	CONVERT(varchar,FORMAT(a.NotificationCreatedTime,'dd MMM yyyy HH:mm'),13) as LastEvent
    from ClientSiteRadioChecksActivityStatus as  A
    left join ClientSites as b on A.ClientSiteId=b.Id
    left join Guards as c on a.GuardId=c.Id
    left join clientSiteRadioChecks as d on d.ClientSiteId=A.ClientSiteId and d.GuardId=a.GuardId and d.Active=0
    where  a.NotificationType is null and c.IsActive=1    
    and a.Id in (select b.id
    from ClientSiteRadioChecksActivityStatus as b
    where b.GuardLoginTime is not null  and ((select count(id) from ClientSiteRadioChecksActivityStatus as A
    where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0))
    


   union


   select   a.ClientSiteId ,a.GuardId,'<a ><i class="fa fa-envelope" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"li></i> </a> <i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end  as SiteName,
	'<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
	b.Gps as GPS,
	'No Guard on Duty' as GuardName,
	CONVERT(varchar,FORMAT(a.GuardLoginTime,'dd MMM yyyy HH:mm'),13) as GuardLoginTime,'Red' AS TwoHrAlert,d.Status as RcStatus,NotificationType,
	CONVERT(varchar,FORMAT(a.NotificationCreatedTime,'dd MMM yyyy HH:mm'),13) as LastEvent
    from ClientSiteRadioChecksActivityStatus as  A
    left join ClientSites as b on A.ClientSiteId=b.Id
    left join Guards as c on a.GuardId=c.Id    
    left join clientSiteRadioChecks as d on d.ClientSiteId=A.ClientSiteId and d.GuardId=a.GuardId and d.Active=0
    where  a.NotificationType=1 and a.Id in (select b.id
    from ClientSiteRadioChecksActivityStatus as b
    where b.GuardLoginTime is not null  and c.IsActive=1     and ((select count(id) from ClientSiteRadioChecksActivityStatus as A
    where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0))


   ) as tbl
    order by NotificationType desc, DATEDIFF(hour, tbl.GuardLoginTime, GETDATE()) desc


END