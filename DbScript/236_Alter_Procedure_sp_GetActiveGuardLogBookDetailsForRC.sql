USE [CityWatchDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardLogBookDetailsForRC]    Script Date: 14-05-2024 09:10:55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_GetActiveGuardLogBookDetailsForRC]
(@ClientSiteId int,@GuardId int )
as
begin
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.LBId as LogBookId,A.ActivityDescription as Activity,
	CASE WHEN d.EventDateTimeLocal is not null 
		then concat(CONVERT(varchar,d.EventDateTimeLocal,108) ,' (', d.EventDateTimeZoneShort , ')')
		else
			CONVERT(varchar,A.LastLBCreatedTime,108) 
		end as LogBookCreatedTime,
	Replace(Replace(Replace(Replace(Replace(D.Notes,'<h4>',''),'</h4>',''),'<h6>','</br>'),'</h6>','') ,'<b>','')as Notes,d.GpsCoordinates as GpsCoordinates
	from ClientSiteRadioChecksActivityStatus A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	inner join GuardLogs D on D.Id=A.LBId
	where a.LBId is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
end;


/* 207_ALTER PROCEDURE [dbo].[sp_GetActiveGuardIncidentReportsDetailsForRC].sql */
SET ANSI_NULLS ON
