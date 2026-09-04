ALTER TABLE ClientSiteKpiSettings
ADD TimezoneString varchar(max);

ALTER TABLE ClientSiteKpiSettings
ADD UTC varchar(500);

ALTER TABLE ClientSiteRadioChecksActivityStatus
ADD UTCOffset varchar(500);
UPDATE       ClientSiteKpiSettings
SET                TimezoneString ='AUS Eastern Standard Time', UTC ='+10:00'
WHERE        (TimezoneString IS NULL)


GO
/****** Object:  StoredProcedure [dbo].[sp_GetInActiveGuardDetailsForRC]    Script Date: 23-10-2024 11:40:21 ******/
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
    select   a.ClientSiteId ,a.GuardId,'<a ><i class="fa fa-envelope clickenvelope" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"></i> </a>
	<i class="fa fa-building clickbuilding" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'" aria-hidden="true"></i> '+b.Name +'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end  as SiteName,
    '<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
    b.Gps as GPS,
    c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName,
    CONVERT(varchar,FORMAT(a.GuardLoginTime,'dd MMM yyyy HH:mm'),13) as GuardLoginTime,case when DATEDIFF(hour, a.GuardLoginTime, GETDATE())<2 then 'Green' else ' Red' end  AS TwoHrAlert,d.Status as RcStatus,NotificationType,
    CONVERT(varchar,FORMAT(a.NotificationCreatedTime,'dd MMM yyyy HH:mm'),13) as LastEvent,d.RadioCheckStatusId as StatusId, 0  as IsEnabled,
    '' as GpsCoordinates,'' as EnabledAddress, 0 as PlayDuressAlarm,
	Trim(Replace(Replace(a.GuardLoginTimeZoneShort,'GMT', ''),'UTC','')) as LoginTimeZone
    from ClientSiteRadioChecksActivityStatus as  A
    left join ClientSites as b on A.ClientSiteId=b.Id
    left join Guards as c on a.GuardId=c.Id
    left join clientSiteRadioChecks as d on d.ClientSiteId=A.ClientSiteId and d.GuardId=a.GuardId and d.Active=1 
	
    where  a.NotificationType is null and c.IsActive=1  and c.IsRCBypass =0 
    and a.Id in (select b.id
    from ClientSiteRadioChecksActivityStatus as b
    where b.GuardLoginTime is not null  and ((select count(id) from ClientSiteRadioChecksActivityStatus as A
    where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0)) and b.Status!=2
	and a.ClientSiteId not in (select ClientSiteID from RCActionList where IsRCBypass=1)



  union


  select   a.ClientSiteId ,a.GuardId,'<a ><i class="fa fa-envelope clickenvelope" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"></i> </a>
    <i class="fa fa-building clickbuilding" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'" aria-hidden="true"></i> '+b.Name +'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end  as SiteName,
    '<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
    b.Gps as GPS,
	 case when a.CRMSupplier is not null then 
    '' +  'No Guard on Duty -'+a.UTCOffset+' CRM : '+ case WHEN len(a.CRMSupplier) > 14 THEN SUBSTRING(a.CRMSupplier, 1, 14) + '...'
        ELSE a.CRMSupplier end+ '<a><i class="fa fa-vcard-o text-info ml-2" data-toggle="modal" data-target="#crmSupplierDetailsModal" data-id="'+a.CRMSupplier+'" title="'+a.CRMSupplier+'"></i></a>'
	else
	'No Guard on Duty'

	end

	as GuardName,
    CONVERT(varchar,FORMAT(a.GuardLoginTime,'dd MMM yyyy HH:mm'),13) as GuardLoginTime,'Red' AS TwoHrAlert,d.Status as RcStatus,NotificationType,
    CONVERT(varchar,FORMAT(a.NotificationCreatedTime,'dd MMM yyyy HH:mm'),13) as LastEvent,d.RadioCheckStatusId as StatusId,0 as IsEnabled,
    '' as GpsCoordinates,'' as EnabledAddress, 0 as PlayDuressAlarm,
	Trim(Replace(Replace(a.GuardLoginTimeZoneShort,'GMT', ''),'UTC','')) as LoginTimeZone
    from ClientSiteRadioChecksActivityStatus as  A
    left join ClientSites as b on A.ClientSiteId=b.Id
    left join Guards as c on a.GuardId=c.Id    
    left join clientSiteRadioChecks as d on d.ClientSiteId=A.ClientSiteId and d.GuardId=a.GuardId and d.Active=1
    where  a.NotificationType=1  and a.Id in (select b.id
    from ClientSiteRadioChecksActivityStatus as b
    where b.GuardLoginTime is not null  and c.IsActive=1     and ((select count(id) from ClientSiteRadioChecksActivityStatus as A
    where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0)) and b.Status!=2
	and a.ClientSiteId not in (select ClientSiteID from RCActionList where IsRCBypass=1)

    union
    /* duress button query only */
     select  a.ClientSiteId ,a.EnabledBy,
	 '<a ><i class="fa fa-envelope clickenvelope" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"></i> </a>
	 <i class="fa fa-building clickbuilding" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'" aria-hidden="true"></i> '+b.Name +'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end  as SiteName,
    '<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
    b.Gps as GPS,
        c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName,
    CONVERT(varchar,FORMAT(a.EnabledDate,'dd MMM yyyy HH:mm'),13) as GuardLoginTime,'Red' AS TwoHrAlert,null as RcStatus,2 as NotificationType,
    CONVERT(varchar,FORMAT(a.EnabledDate,'dd MMM yyyy HH:mm'),13) as LastEvent,null as StatusId,case when a.IsEnabled is not null then a.IsEnabled else 0 end as IsEnabled,
    a.GpsCoordinates,a.EnabledAddress,case when a.PlayDuressAlarm is not null then a.PlayDuressAlarm else 0 end as PlayDuressAlarm,
	Trim(Replace(Replace(a.EnabledDateTimeZoneShort,'GMT', ''),'UTC','')) as LoginTimeZone
    from ClientSiteDuress as A
    left join ClientSites as b on A.ClientSiteId=b.Id
    left join Guards as c on a.EnabledBy=c.Id
	where A.IsLinkedDuressParentSite=1 
	--and c.IsRCBypass=0
   ) as tbl
    order by IsEnabled desc,NotificationType desc, DATEDIFF(hour, tbl.GuardLoginTime, GETDATE()) desc


END;

--select TimezoneString, UTC from ClientSiteKpiSettings where TimezoneString is not null

--select * from [dbo].[IncidentReports]

--select * from ClientSiteManningKpiSettings

--select * from ClientSiteRadioChecksActivityStatus
