/* =============================================================================
   422 - Protect patrol car base-site login anchors from the cross-site cleanup.

   PROBLEM
   Insert_GuardLogs deletes a guard's Radio Check rows at every OTHER site on
   each log entry, enforcing "one guard = one site". A patrol car crew is
   legitimately at two places at once: their base site, and the client site they
   are visiting. Every client-site scan therefore deleted the crew's base-site
   row, and they disappeared from the base site in Radio Check.

   Confirmed on site 625 (Citywatch M1 - Romeo Patrol Cars): six crews logged in,
   two or three visible, and the missing ones were in neither the active nor the
   inactive list.

   CHANGE
   The three cross-site DELETE statements now skip login anchors that belong to a
   patrol car base site. Protection is keyed on what the row IS - a row carrying
   GuardLoginTime at a PatrolTourMode = 1 site - not on flags carried by the
   entry that fired the trigger, so it holds however the crew logged in.

   LEFT JOIN is deliberate: rows whose site no longer exists stay deletable
   exactly as before.

   SCOPE
   Standard sites (PatrolTourMode = 0) are completely unaffected - verified on
   test-citywatch by moving a standard guard A -> B and confirming both rows at
   site A are still removed, including a login anchor.

   NOT INCLUDED - see separate ticket
   This trigger also uses "SELECT Max(Id) FROM GuardLogs" instead of the inserted
   pseudo-table, which is unsafe under concurrent inserts and handles only one
   row of a multi-row insert. That defect is deliberately left untouched here; it
   affects every log entry in the system and needs its own change and testing.
   ============================================================================= */

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
	declare @NFCBLE as int =(Select WAND_TAG_ENTRY_TYPE from GuardLogs where Id=@Id)
	if(@Notes!='Logbook Logged In' AND @Notes NOT LIKE '%N/A%' )
	begin
		if(@clientSiteId!=0)
		begin

			if(@NFCBLE=0)
			begin 
				insert into ClientSiteRadioChecksActivityStatus(ClientSiteId ,GuardId,LastLBCreatedTime,LBId,ActivityType,ActivityDescription )
				Select @clientSiteId,@GuardId,GETDATE(),@Id,'LB','Added New Notes'
			

				/* -----------------------------------------------------------------------
   19-Aug-2026 - PCAR base-site login anchors are permanent for the shift.
   A patrol car is legitimately at BOTH its base site and the client site it
   is visiting, so the usual "one guard = one site" cleanup must skip those
   rows. Protection is based on what the row IS (a login anchor at a
   PatrolTourMode = 1 site), not on flags carried by the entry that fired
   this trigger. LEFT JOIN keeps orphan rows deletable exactly as before.
   ----------------------------------------------------------------------- */
			delete a
			from ClientSiteRadioChecksActivityStatus a
			left join ClientSites cs on cs.Id = a.ClientSiteId
			where a.GuardId = @GuardId
			  and a.ClientSiteId != @clientSiteId
			  and not (isnull(cs.PatrolTourMode, 0) = 1 and a.GuardLoginTime is not null) 

				/* Show active that gaurd if gaurd enter any notes after login*/
				DELETE FROM ClientSiteRadioChecks WHERE ClientSiteId = @clientSiteId 
				AND GuardId = @GuardId AND RadioCheckStatusId = 1 AND Status IN ('Off Duty (RC automatic logoff)', 'Off Duty');
			end
			else 
			begin 
				insert into ClientSiteRadioChecksActivityStatus(ClientSiteId ,GuardId,LastSWCreatedTime,LBId,ActivityType,ActivityDescription )
				Select @clientSiteId,@GuardId,GETDATE(),@Id,'SW',@Notes
			

				/* -----------------------------------------------------------------------
   19-Aug-2026 - PCAR base-site login anchors are permanent for the shift.
   A patrol car is legitimately at BOTH its base site and the client site it
   is visiting, so the usual "one guard = one site" cleanup must skip those
   rows. Protection is based on what the row IS (a login anchor at a
   PatrolTourMode = 1 site), not on flags carried by the entry that fired
   this trigger. LEFT JOIN keeps orphan rows deletable exactly as before.
   ----------------------------------------------------------------------- */
			delete a
			from ClientSiteRadioChecksActivityStatus a
			left join ClientSites cs on cs.Id = a.ClientSiteId
			where a.GuardId = @GuardId
			  and a.ClientSiteId != @clientSiteId
			  and not (isnull(cs.PatrolTourMode, 0) = 1 and a.GuardLoginTime is not null) 

				/* Show active that gaurd if gaurd enter any notes after login*/
				DELETE FROM ClientSiteRadioChecks WHERE ClientSiteId = @clientSiteId 
				AND GuardId = @GuardId AND RadioCheckStatusId = 1 AND Status IN ('Off Duty (RC automatic logoff)', 'Off Duty');
			end 

		end
		
	end
	else
	begin
		/* -----------------------------------------------------------------------
   19-Aug-2026 - PCAR base-site login anchors are permanent for the shift.
   A patrol car is legitimately at BOTH its base site and the client site it
   is visiting, so the usual "one guard = one site" cleanup must skip those
   rows. Protection is based on what the row IS (a login anchor at a
   PatrolTourMode = 1 site), not on flags carried by the entry that fired
   this trigger. LEFT JOIN keeps orphan rows deletable exactly as before.
   ----------------------------------------------------------------------- */
			delete a
			from ClientSiteRadioChecksActivityStatus a
			left join ClientSites cs on cs.Id = a.ClientSiteId
			where a.GuardId = @GuardId
			  and a.ClientSiteId != @clientSiteId
			  and not (isnull(cs.PatrolTourMode, 0) = 1 and a.GuardLoginTime is not null) 
	end


END

