-- ================================================
-- Template generated from Template Explorer using:
-- Create Trigger (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- See additional Create Trigger templates for more
-- examples of different Trigger statements.
--
-- This block of comments will not be included in
-- the definition of the function.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
create TRIGGER Insert_IncidentReports 
   ON  IncidentReports
   AFTER INSERT
AS 
BEGIN
	Declare @Id as int=(SELECT Max(Id)  from IncidentReports);
	Declare @clientSiteId as int=(Select ClientSiteId from IncidentReports where Id=@Id)
	Declare @GuardId as int=(Select GuardId from IncidentReports where Id=@Id)
	
	if(@GuardId!=0)
	BEGIN 
	
	
		insert into ClientSiteRadioChecksActivityStatus(ClientSiteId ,GuardId,LastIRCreatedTime,IRId,ActivityType,ActivityDescription )
	Select @clientSiteId,@GuardId,GETDATE(),@Id,'IR','Created'

	END
END
GO
