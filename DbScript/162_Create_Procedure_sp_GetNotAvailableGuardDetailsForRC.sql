USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetNotAvailableGuardDetailsForRC]    Script Date: 18-10-2023 07:56:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha James
-- Create date: 17/10/2023
-- Description:	to get the details of guards not available
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetNotAvailableGuardDetailsForRC]
	
AS
BEGIN
	select  A.Id,A.ClientSiteId,A.GuardId,'<i class="fa fa-building" aria-hidden="true"></i> '+b.Name +' <i class="fa fa-phone" aria-hidden="true"></i>'+b.LandLine as SiteName,c.Name+ case when c.Initial is not null then ' ['+c.Initial+']' else ''end as GuardName,
    CONVERT(varchar,a.LoginDate,103) as GuardLastLoginDate
	from GuardLogins A
	inner join ClientSites as b on A.ClientSiteId=b.Id
    inner join Guards as c on a.GuardId=c.Id    
	where a.LoginDate in(select max(b.LoginDate) from GuardLogins b where b.GuardId=A.GuardId and b.ClientSiteId=A.ClientSiteId)
	and A.GuardId not in(select  b.GuardId from ClientSiteRadioChecksActivityStatus b)
	and A.ClientSiteId not in (select b.ClientSiteId from ClientSiteRadioChecksActivityStatus b)
	and c.IsActive=1
	
END

