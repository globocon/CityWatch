USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardLogBookDetailsForRC]    Script Date: 16-11-2023 18:22:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_GetActiveGuardLogBookDetailsForRC]
(@ClientSiteId int,@GuardId int )
as
begin
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.LBId as LogBookId,A.ActivityDescription as Activity,CONVERT(varchar,A.LastLBCreatedTime,108) as LogBookCreatedTime,
	Replace(Replace(Replace(Replace(Replace(D.Notes,'<h4>',''),'</h4>',''),'<h6>','</br>'),'</h6>','') ,'<b>','')as Notes
	from ClientSiteRadioChecksActivityStatus A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	inner join GuardLogs D on D.Id=A.LBId
	where a.LBId is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
end