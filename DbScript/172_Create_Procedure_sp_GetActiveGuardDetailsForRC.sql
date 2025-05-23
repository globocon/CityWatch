USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardDetailsForRC]    Script Date: 01-11-2023 09:12:03 ******/
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

	select   a.ClientSiteId ,GuardId,'<i class="fa fa-envelope"></i> <i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+b.LandLine as SiteName,
	'<i class="fa fa-location" aria-hidden="true"></i> '+ b.Address as Address,
	c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName   ,sum(case when ActivityType='LB' then 1 else 0 end) as LogBook,
	sum(case when ActivityType='KV' then 1 else 0 end) as KeyVehicle,
	sum(case when ActivityType='IR' then 1 else 0 end) as IncidentReport ,
	b.LandLine,'' as SmartWands
	from ClientSiteRadioChecksActivityStatus as  A
	left join ClientSites as b on A.ClientSiteId=b.Id
	left join Guards as c on a.GuardId=c.Id	
	where a.ClientSiteId is not null and a.Id not in (select b.id
	from ClientSiteRadioChecksActivityStatus as b
	where b.GuardLoginTime is not null  and ((select count(id) from ClientSiteRadioChecksActivityStatus as A 
	where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0))
	group by a.ClientSiteId,b.Name,c.Name,c.Initial,GuardId,b.LandLine,b.Address

END
