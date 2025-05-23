USE [prod-citywatch]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardDetailsForRC]    Script Date: 23/07/2024 4:14:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_GetActiveGuardDetailsForRC]
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	select   a.ClientSiteId ,a.GuardId,
	'<a ><i class="fa fa-envelope clickenvelope" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'"></i> </a>
	<i class="fa fa-building clickbuilding" style="cursor: pointer;" data-target="#pushNoTificationsControlRoomModal" data-toggle="modal" data-id="'+ cast(a.ClientSiteId as varchar)+'" aria-hidden="true"></i> '+b.Name +'&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class="fa fa-phone" aria-hidden="true"></i>'+case when b.LandLine is not null then b.LandLine else '' end as  SiteName,
	'<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
	b.Gps as GPS,
	c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName   ,
	sum(case when ActivityType='LB' then 1 else 0 end) as LogBook,
	sum(case when ActivityType='KV' then 1 else 0 end) as KeyVehicle,
	sum(case when ActivityType='IR' then 1 else 0 end) as IncidentReport ,
	sum(case when ActivityType='SW' then 1 else 0 end) as SmartWands ,
	b.LandLine,(Select g.Id from RadioCheckStatus g where g.Id= d.RadioCheckStatusId) as RcStatus,d.Status as Status,
	(Select f.Name from RadioCheckStatus e inner join RadioCheckStatusColor f on f.Id=e.RadioCheckStatusColorId where e.Id=d.RadioCheckStatusId) as RcColor,
	(Select f.Id from RadioCheckStatus e inner join RadioCheckStatusColor f on f.Id=e.RadioCheckStatusColorId where e.Id=d.RadioCheckStatusId) as RcColorId,
	b.Name as OnlySiteName,
	/* Show latest activity min start*/
	(select top 1 DATEDIFF(MINUTE,[Latest Date], GETDATE()) [Latest Date] from (
	SELECT   
	CASE WHEN (LastIRCreatedTime > LastKVCreatedTime OR LastKVCreatedTime IS NULL) 
	AND (LastIRCreatedTime > LastLBCreatedTime OR LastLBCreatedTime IS NULL)
	AND (LastIRCreatedTime > LastSWCreatedTime OR LastSWCreatedTime IS NULL)
	THEN LastIRCreatedTime
	WHEN (LastKVCreatedTime > LastLBCreatedTime OR LastLBCreatedTime IS NULL) and (LastKVCreatedTime > LastSWCreatedTime OR LastSWCreatedTime IS NULL)
	THEN LastKVCreatedTime
	WHEN (LastLBCreatedTime > LastSWCreatedTime OR LastSWCreatedTime IS NULL)
	THEN LastLBCreatedTime
	ELSE LastSWCreatedTime END AS [Latest Date]
	FROM ClientSiteRadioChecksActivityStatus as LA  where LA.ClientSiteId=A.ClientSiteId and  LA.GuardId=A.GuardId ) as tbl where tbl.[Latest Date] is not null
	order by tbl.[Latest Date] desc) as LatestDate,

	(select top 1  case when (DATEDIFF(MINUTE,[Latest Date], GETDATE()))>80 then 1 else 0 end    from (
	SELECT
	CASE WHEN (LastIRCreatedTime > LastKVCreatedTime OR LastKVCreatedTime IS NULL) 
	AND (LastIRCreatedTime > LastLBCreatedTime OR LastLBCreatedTime IS NULL)
	AND (LastIRCreatedTime > LastSWCreatedTime OR LastSWCreatedTime IS NULL)
	THEN LastIRCreatedTime
	WHEN (LastKVCreatedTime > LastLBCreatedTime OR LastLBCreatedTime IS NULL) and (LastKVCreatedTime > LastSWCreatedTime OR LastSWCreatedTime IS NULL)
	THEN LastKVCreatedTime
	WHEN (LastLBCreatedTime > LastSWCreatedTime OR LastSWCreatedTime IS NULL)
	THEN LastLBCreatedTime
	ELSE LastSWCreatedTime END AS [Latest Date]
	FROM ClientSiteRadioChecksActivityStatus as LA  where LA.ClientSiteId=A.ClientSiteId and  LA.GuardId=A.GuardId ) as tbl where tbl.[Latest Date] is not null
	order by tbl.[Latest Date] desc) as ShowColor

	/* Show latest activity min end*/ 
	/* New field*/
	,0 as hasmartwand,'Grey' as HR1,'Grey' as HR2,'Grey' as HR3
	/* New field*/


	from ClientSiteRadioChecksActivityStatus as  A
	left join ClientSites as b on A.ClientSiteId=b.Id
	left join Guards as c on a.GuardId=c.Id	
	left join clientSiteRadioChecks as d on d.ClientSiteId=A.ClientSiteId and d.GuardId=a.GuardId and d.Active=1
	where a.ClientSiteId is not null and c.IsActive=1 and  a.Id not in (select b.id
	from ClientSiteRadioChecksActivityStatus as b
	where b.GuardLoginTime is not null  and ((select count(id) from ClientSiteRadioChecksActivityStatus as A 
	where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0))
	group by a.ClientSiteId,b.Name,c.Name,c.Initial,a.GuardId,b.LandLine
	,b.Address,b.Gps,d.RadioCheckStatusId,d.Status

END