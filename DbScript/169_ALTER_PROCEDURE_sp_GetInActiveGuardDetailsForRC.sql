USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetInActiveGuardDetailsForRC]    Script Date: 30-10-2023 20:39:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Dileep>
-- Create date: <Create Date,12/10/2023,>
-- Description:	<Description,forgetting active guard details,>
-- =============================================
ALTER PROCEDURE [dbo].[sp_GetInActiveGuardDetailsForRC]
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	select   a.ClientSiteId ,GuardId,'<i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+b.LandLine as SiteName,c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName,
	CONVERT(varchar,FORMAT(a.GuardLoginTime,'dd MMM yyyy HH:MM'),13) as GuardLoginTime
	from ClientSiteRadioChecksActivityStatus as  A
	left join ClientSites as b on A.ClientSiteId=b.Id
	left join Guards as c on a.GuardId=c.Id	
	where a.Id in (select b.id
	from ClientSiteRadioChecksActivityStatus as b
	where b.GuardLoginTime is not null  and ((select count(id) from ClientSiteRadioChecksActivityStatus as A 
	where a.ClientSiteId=b.ClientSiteId and a.GuardId=b.GuardId and ActivityType is not null)=0))
	

END


