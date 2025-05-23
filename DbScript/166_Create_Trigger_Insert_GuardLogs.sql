USE [CityWatchDb]
GO
/****** Object:  Trigger [dbo].[Insert_GuardLogs]    Script Date: 18-10-2023 12:49:56 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jisha Jmaes
-- Create date: 14/10/2023
-- Description:	to insert into rc automatically after insertinf in kv
-- =============================================
CREATE TRIGGER [dbo].[Insert_GuardLogs] 
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
		end
	end


END
