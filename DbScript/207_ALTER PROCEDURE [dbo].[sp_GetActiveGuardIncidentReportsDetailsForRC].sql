USE [prod-citywatch]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha James
-- Create date: 17/10/2023
-- Description:	to get incident reports
-- Modification
	--16-01-2024 - Added case statement for IncidentReportCreatedTime to include timezone
-- =============================================
ALTER PROCEDURE [dbo].[sp_GetActiveGuardIncidentReportsDetailsForRC]
	(@ClientSiteId int,@GuardId int )
	as
BEGIN
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.IRId as IncidentReportId,A.ActivityDescription as Activity,
	CASE WHEN d.CreatedOnDateTimeLocal is not null 
		then concat(CONVERT(varchar,d.CreatedOnDateTimeLocal,108) ,' (', d.CreatedOnDateTimeZoneShort , ')')
		else
			CONVERT(varchar,A.LastIRCreatedTime,108) 
		end as IncidentReportCreatedTime,
	D.VehicleRego as TruckNo,D.FileName as FileName
	from ClientSiteRadioChecksActivityStatus A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	inner join IncidentReports D on D.Id=A.IRId
	where a.IRId is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
END
