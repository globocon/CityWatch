
GO
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardLogBookDetailsForRC]    Script Date: 25-10-2023 23:19:59 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha James
-- Create date: 17-10-2023
-- Description:	to display the log book details of the guard on particular site
-- =============================================
ALTER PROCEDURE [dbo].[sp_GetActiveGuardLogBookDetailsForRC]
(@ClientSiteId int,@GuardId int )
as
begin
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.LBId as LogBookId,A.ActivityDescription as Activity,CONVERT(varchar,A.LastLBCreatedTime,108) as LogBookCreatedTime,D.Notes as Notes
	from ClientSiteRadioChecksActivityStatus A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	inner join GuardLogs D on D.Id=A.LBId
	where a.LBId is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
end


