USE [prod-citywatch]
GO
/****** Object:  Trigger [dbo].[Insert_GuardLogs]    Script Date: 04-02-2025 20:17:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER TRIGGER [dbo].[Insert_GuardLogs] 
   ON  [dbo].[GuardLogs] 
   AFTER INSERT
AS 
BEGIN
	Declare @Id as int=(SELECT Max(Id)  from GuardLogs);


	Declare @GuardLoginId as int=(Select guardLoginId from GuardLogs where Id=@Id)
    Declare @clientSiteId as int=(Select ClientSiteId from GuardLogins where Id=@GuardLoginId)
	Declare @GuardId as int=(Select GuardId from GuardLogins where Id=@GuardLoginId)
	Declare @Notes as nvarchar(max)=(Select Notes from GuardLogs where Id=@Id)
	if(@Notes!='Logbook Logged In')
	begin
		if(@clientSiteId!=0)
		begin
			insert into ClientSiteRadioChecksActivityStatus(ClientSiteId ,GuardId,LastLBCreatedTime,LBId,ActivityType,ActivityDescription )
			Select @clientSiteId,@GuardId,GETDATE(),@Id,'LB','Added New Notes'
			

			if (Exists(select * from ClientSiteRadioChecksActivityStatus where GuardId=@GuardId and ClientSiteId!=@clientSiteId))
			begin 
				delete from ClientSiteRadioChecksActivityStatus where GuardId=@GuardId and ClientSiteId!=@clientSiteId
			end 

			/* Show active that gaurd if gaurd enter any notes after login*/
			DELETE FROM ClientSiteRadioChecks WHERE ClientSiteId = @clientSiteId 
			AND GuardId = @GuardId AND RadioCheckStatusId = 1AND Status IN ('Off Duty (RC automatic logoff)', 'Off Duty');


		end
		
	end
	else
	begin
		if (Exists(select * from ClientSiteRadioChecksActivityStatus where GuardId=@GuardId and ClientSiteId!=@clientSiteId))
		begin 
			delete from ClientSiteRadioChecksActivityStatus where GuardId=@GuardId and ClientSiteId!=@clientSiteId
		end 
	end


END