
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardIncidentReportsDetailsForRC]    Script Date: 18-10-2023 07:50:45 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha James
-- Create date: 17/10/2023
-- Description:	to get incident reports
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetActiveGuardIncidentReportsDetailsForRC]
	(@ClientSiteId int,@GuardId int )
	as
BEGIN
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.IRId as IncidentReportId,A.ActivityDescription as Activity,CONVERT(varchar,A.LastIRCreatedTime,108) as IncidentReportCreatedTime,
	D.VehicleRego as TruckNo,D.FileName as FileName
	from ClientSiteRadioChecksActivityStatus A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	inner join IncidentReports D on D.Id=A.IRId
	where a.IRId is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
END
