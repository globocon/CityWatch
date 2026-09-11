
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_GetSiteLog]
    @ClientSiteId INT,
    @LastLogId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TodayDate Date = CAST(GETDATE() AS DATE);
    
    -- Tables to store sites and logbook IDs
    DECLARE @LinkedSites TABLE (SiteId INT, IsPrimary BIT);
    DECLARE @LogbookIds TABLE (LogbookId INT, SiteId INT, IsPrimary BIT);
    
    DECLARE @RCLinkedId INT, @IsLB BIT = 0, @IsSW BIT = 0;

    -- Find Linked Group
    SELECT TOP 1 @RCLinkedId = RCLinkedId 
    FROM RCLinkedDuressClientSites 
    WHERE ClientSiteId = @ClientSiteId;

    IF @RCLinkedId IS NOT NULL
    BEGIN
        SELECT @IsLB = IsLB, @IsSW = IsSW 
        FROM RCLinkedDuressMaster 
        WHERE Id = @RCLinkedId;
        
        -- Get all sites in the group
        INSERT INTO @LinkedSites (SiteId, IsPrimary)
        SELECT ClientSiteId, CASE WHEN ClientSiteId = @ClientSiteId THEN 1 ELSE 0 END
        FROM RCLinkedDuressClientSites
        WHERE RCLinkedId = @RCLinkedId;
    END
    ELSE
    BEGIN
        -- No links, just use the primary site
        INSERT INTO @LinkedSites (SiteId, IsPrimary) VALUES (@ClientSiteId, 1);
    END

    -- Get today's logbook IDs for all involved sites
    INSERT INTO @LogbookIds (LogbookId, SiteId, IsPrimary)
    SELECT lb.Id, lb.ClientSiteId, ls.IsPrimary
    FROM ClientSiteLogBooks lb
    INNER JOIN @LinkedSites ls ON lb.ClientSiteId = ls.SiteId
    WHERE lb.Type = 1 AND lb.[Date] = @TodayDate;

    -- If no primary logbook exists, return nothing
    IF NOT EXISTS (SELECT 1 FROM @LogbookIds WHERE IsPrimary = 1)
    BEGIN
        SELECT -1 AS Id, 'No logbook found' AS Notes;
        RETURN;
    END

    -- Main Select incorporating merging logic
    SELECT 
        gl.Id,
        gl.EventDateTime,
        FORMAT(gl.EventDateTimeLocalWithOffset, 'HH:mm') 
            + ' Hrs ' 
            + COALESCE(gl.EventDateTimeZoneShort, 'N/A') AS EventDateTimeLocal,
        COALESCE(gl.EventDateTimeZoneShort, 'N/A') AS EventDateTimeZoneShort,
        ISNULL(gl.Notes, '') + 
            CASE 
                WHEN lbs.IsPrimary = 0 
                THEN ' - ' + ISNULL(g.Name, '') + ' (' + ISNULL(cs.Name, '') + ')' 
                ELSE '' 
            END AS Notes,
        g.Initial AS GuardInitials,
        gl.IrEntryType,
        gl.IsSystemEntry,
        gl.RcPushMessageId,
        glog.GuardId,
        img.ImagePath,
        img.IsTwentyfivePercentfile,
        img.IsRearfile
    FROM GuardLogs gl
    INNER JOIN @LogbookIds lbs ON gl.ClientSiteLogBookId = lbs.LogbookId
    INNER JOIN ClientSites cs ON lbs.SiteId = cs.Id
    LEFT JOIN GuardLogsDocumentImages img ON gl.Id = img.GuardLogId
    LEFT JOIN GuardLogins glog ON gl.GuardLoginId = glog.Id AND gl.ClientSiteLogBookId = glog.ClientSiteLogBookId
    LEFT JOIN Guards g ON glog.GuardId = g.Id
    WHERE 
        (lbs.IsPrimary = 1) OR -- Primary site shows everything
        (lbs.IsPrimary = 0 AND @IsLB = 1 AND gl.WAND_TAG_ENTRY_TYPE = 0) OR -- Linked site Normal logs
        (lbs.IsPrimary = 0 AND @IsSW = 1 AND gl.WAND_TAG_ENTRY_TYPE <> 0)    -- Linked site Scan logs
    ORDER BY gl.EventDateTimeLocal DESC, gl.Id DESC;
END
GO
