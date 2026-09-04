
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardDetailsForRC]    Script Date: 13-10-2023 16:36:53 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Dileep>
-- Create date: <Create Date,12/10/2023,>
-- Description:	<Description,forgetting active guard details,>
-- =============================================
create PROCEDURE [dbo].[sp_GetActiveGuardDetailsForRC]
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

	select   a.ClientSiteId ,GuardId,'<i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+b.LandLine as SiteName,c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName   ,sum(case when ActivityType='LB' then 1 else 0 end) as LogBook,
	sum(case when ActivityType='KV' then 1 else 0 end) as KeyVehicle,
	sum(case when ActivityType='IR' then 1 else 0 end) as IncidentReport ,
	b.LandLine,'' as SmartWands
	from ClientSiteRadioChecksActivityStatus as  A
	left join ClientSites as b on A.ClientSiteId=b.Id
	left join Guards as c on a.GuardId=c.Id	
	group by a.ClientSiteId,b.Name,c.Name,c.Initial,GuardId,b.LandLine

END
