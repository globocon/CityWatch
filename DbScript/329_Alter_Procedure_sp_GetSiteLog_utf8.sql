

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [sp_GetSiteLog]
    @ClientSiteId INT,
    @LastLogId INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @LogbookId INT;
    DECLARE @TodayDate Date;
    SET @TodayDate = CAST(GETDATE() AS DATE);
    -- Get Today's Logbook
    SELECT TOP 1 @LogbookId = Id
    FROM ClientSiteLogBooks
    WHERE ClientSiteId = @ClientSiteId
      AND Type = 1  -- DailyGuardLog
      AND [Date] = @TodayDate;
    IF @LogbookId IS NULL
    BEGIN
        SELECT -1 AS Id, 'No logbook found' AS Notes;
        RETURN;
    END
    SELECT 
        gl.Id,
        gl.EventDateTime,
		 -- Local time + GMT Offset (handle nulls)
    -- Local time + GMT Offset (with null handling)
FORMAT(gl.EventDateTimeLocalWithOffset, 'HH:mm') 
    + ' Hrs ' 
    + COALESCE(gl.EventDateTimeZoneShort, 'N/A') AS EventDateTimeLocal,
 
-- Short GMT Offset only (with null handling)
COALESCE(gl.EventDateTimeZoneShort, 'N/A') AS EventDateTimeZoneShort,
        ---- FORMAT Local DateTime and offset for direct frontend usage
        --FORMAT(gl.EventDateTimeLocalWithOffset, 'HH:mm') 
        --    + ' Hrs ' 
        --    + COALESCE('GMT' 
        --        + CASE WHEN DATEPART(TZOFFSET, gl.EventDateTimeLocalWithOffset) >= 0 
        --               THEN '+' ELSE '-' END
        --        + FORMAT(DATEADD(MINUTE, 
        --                DATEPART(TZOFFSET, gl.EventDateTimeLocalWithOffset), 
        --                gl.EventDateTimeLocalWithOffset), 'HH:mm'), '') 
        --    AS EventDateTimeLocal,
        ---- Timezone Short Format
        --COALESCE(
        --    CASE 
        --        WHEN DATEPART(TZOFFSET, gl.EventDateTimeLocalWithOffset) >= 0 
        --            THEN '+'
        --        ELSE '-' 
        --    END + FORMAT(DATEADD(MINUTE, DATEPART(TZOFFSET, gl.EventDateTimeLocalWithOffset), gl.EventDateTimeLocalWithOffset), 'HH:mm'),
        --    'N/A'
        --) AS EventDateTimeZoneShort,
        ISNULL(gl.Notes, '') AS Notes,
        g.Initial AS GuardInitials,
        gl.IrEntryType,
        gl.IsSystemEntry,
        gl.RcPushMessageId,
        glog.GuardId,
        img.ImagePath,
        img.IsTwentyfivePercentfile,
        img.IsRearfile
    FROM GuardLogs gl
    LEFT JOIN GuardLogsDocumentImages img ON gl.Id = img.GuardLogId
    LEFT JOIN GuardLogins glog ON gl.GuardLoginId = glog.Id
    LEFT JOIN Guards g ON glog.GuardId = g.Id
    WHERE gl.ClientSiteLogBookId = @LogbookId
      --AND gl.Id > @LastLogId
    ORDER BY gl.EventDateTimeLocal DESC, gl.Id DESC;
END
