USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardRadioCheckStatusForRC]    Script Date: 09-05-2024 12:06:36 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_GetActiveGuardRadioCheckStatusForRC]
(@ClientSiteId int,@GuardId int )
as
begin
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.Id as LogBookId,'CRO' as Activity,CONVERT(varchar,A.CheckedAt,108) as LogBookCreatedTime,
	Replace(Replace(Replace(Replace(Replace(A.Status,'<h4>',''),'</h4>',''),'<h6>','</br>'),'</h6>','') ,'<b>','')as Notes,null as GpsCoordinates
	from ClientSiteRadioChecks A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	where a.Id is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
end