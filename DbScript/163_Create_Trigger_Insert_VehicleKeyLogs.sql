
GO
/****** Object:  Trigger [dbo].[Insert_VehicleKeyLogs]    Script Date: 18-10-2023 08:06:01 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha Jmaes
-- Create date: 14/10/2023
-- Description:	to insert into rc automatically after insertinf in kv
-- =============================================
CREATE TRIGGER [dbo].[Insert_VehicleKeyLogs]
   ON  [dbo].[VehicleKeyLogs]
   AFTER INSERT
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Declare @Id as int=(SELECT Max(Id)  from VehicleKeyLogs);

	Declare @GuardLoginId as int=(Select guardLoginId from VehicleKeyLogs where Id=@Id)
    Declare @clientSiteId as int=(Select ClientSiteId from GuardLogins where Id=@GuardLoginId)
	Declare @GuardId as int=(Select GuardId from GuardLogins where Id=@GuardLoginId)

	insert into ClientSiteRadioChecksActivityStatus(ClientSiteId ,GuardId,LastKVCreatedTime,KVId,ActivityType,ActivityDescription )
	Select @clientSiteId,@GuardId,GETDATE(),@Id,'KV','Created'
END
