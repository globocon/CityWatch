
/****** Object:  StoredProcedure [dbo].[sp_GetActiveGuardKeyVehicleDetailsForRC]    Script Date: 18-10-2023 07:48:19 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha James
-- Create date: 17/10/2023
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_GetActiveGuardKeyVehicleDetailsForRC] 
	(@ClientSiteId int,@GuardId int )
AS
BEGIN
	Select A.Id as Id,A.ClientSiteId as ClientSiteId,A.GuardId as GuardId,
	B.Name as SiteName,C.Name as GuardName,A.KVId as KeyVehicleId,A.ActivityDescription as Activity,CONVERT(varchar,A.LastKVCreatedTime,108) as KeyVehicleLogCreatedTime,
	D.VehicleRego as TruckNo,D.PersonName as Individual,D.CompanyName as Company
	from ClientSiteRadioChecksActivityStatus A
	inner join ClientSites B on B.Id=A.ClientSiteId
	inner join Guards C on C.Id=a.GuardId
	inner join VehicleKeyLogs D on D.Id=A.KVId
	where a.KVId is not null and A.GuardId=@GuardId and A.ClientSiteId=@ClientSiteId
END
