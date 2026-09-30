/* 380: sp_GetSiteLogPage - the mobile Log Activity page, one page at a time.

   sp_GetSiteLog returns the WHOLE of today's logbook (every entry, every image row) and the
   app rebuilds the page from it on every GuardLogChanged push. On 29-30 Sep 2026 the shared
   Romeo PCAR logbook reached ~700 entries and 150+ photos a day, and the phones ran out of
   memory. This procedure serves the same rows, with the same site/linked-site rules and the
   same columns, but for a bounded set of entries:

     - @LogIds NULL, @BeforeLogId 0   -> the newest @PageSize entries
     - @LogIds NULL, @BeforeLogId N   -> the @PageSize entries after entry N in display order
                                         (older), for "load more" as the guard scrolls
     - @LogIds '12,34'                -> exactly those entries (live updates), when they are
                                         still in today's logbooks for the site; an id that is
                                         not returned was deleted or moved, and the app drops it

   Display order is sp_GetSiteLog's: EventDateTimeLocal DESC, Id DESC. The page cursor is the
   (EventDateTimeLocal, Id) pair of @BeforeLogId, so an entry synced late with an earlier time
   still lands in its proper place instead of being skipped.

   sp_GetSiteLog itself is unchanged - older app versions keep calling it.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_GetSiteLogPage]
    @ClientSiteId INT,
    @BeforeLogId  INT = 0,
    @PageSize     INT = 10,
    @LogIds       NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TodayDate DATE = CAST(GETDATE() AS DATE);
    DECLARE @NoTime DATETIME = '19000101';

    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 10;
    IF @PageSize > 100 SET @PageSize = 100;

    DECLARE @LinkedSites TABLE (SiteId INT, IsPrimary BIT);
    DECLARE @LogbookIds  TABLE (LogbookId INT, SiteId INT, IsPrimary BIT);
    DECLARE @Page        TABLE (Id INT PRIMARY KEY);

    DECLARE @RCLinkedId INT, @IsLB BIT = 0, @IsSW BIT = 0;

    -- Linked group: exactly as sp_GetSiteLog
    SELECT TOP 1 @RCLinkedId = RCLinkedId
    FROM RCLinkedDuressClientSites
    WHERE ClientSiteId = @ClientSiteId;

    IF @RCLinkedId IS NOT NULL
    BEGIN
        SELECT @IsLB = IsLB, @IsSW = IsSW
        FROM RCLinkedDuressMaster
        WHERE Id = @RCLinkedId;

        INSERT INTO @LinkedSites (SiteId, IsPrimary)
        SELECT ClientSiteId, CASE WHEN ClientSiteId = @ClientSiteId THEN 1 ELSE 0 END
        FROM RCLinkedDuressClientSites
        WHERE RCLinkedId = @RCLinkedId;
    END
    ELSE
    BEGIN
        INSERT INTO @LinkedSites (SiteId, IsPrimary) VALUES (@ClientSiteId, 1);
    END

    INSERT INTO @LogbookIds (LogbookId, SiteId, IsPrimary)
    SELECT lb.Id, lb.ClientSiteId, ls.IsPrimary
    FROM ClientSiteLogBooks lb
    INNER JOIN @LinkedSites ls ON lb.ClientSiteId = ls.SiteId
    WHERE lb.Type = 1 AND lb.[Date] = @TodayDate;

    -- No logbook yet today: unlike sp_GetSiteLog (which returns a one-column "No logbook
    -- found" row the API cannot map), fall through with an empty page, so the caller gets
    -- zero rows in the normal shape and can tell "nothing yet" from a failure.
    IF NOT EXISTS (SELECT 1 FROM @LogbookIds WHERE IsPrimary = 1)
    BEGIN
        DELETE FROM @LogbookIds;
    END
    ELSE IF @LogIds IS NOT NULL
    BEGIN
        -- Specific entries. XML split rather than STRING_SPLIT so the compatibility level
        -- does not matter; the API builds this list from integers only.
        DECLARE @Xml XML = CAST('<i>' + REPLACE(@LogIds, ',', '</i><i>') + '</i>' AS XML);

        INSERT INTO @Page (Id)
        SELECT DISTINCT gl.Id
        FROM @Xml.nodes('/i') AS t(v)
        INNER JOIN GuardLogs gl ON gl.Id = TRY_CAST(t.v.value('.', 'NVARCHAR(20)') AS INT)
        INNER JOIN @LogbookIds lbs ON gl.ClientSiteLogBookId = lbs.LogbookId
        WHERE
            (lbs.IsPrimary = 1) OR
            (lbs.IsPrimary = 0 AND @IsLB = 1 AND gl.WAND_TAG_ENTRY_TYPE = 0) OR
            (lbs.IsPrimary = 0 AND @IsSW = 1 AND gl.WAND_TAG_ENTRY_TYPE <> 0);
    END
    ELSE
    BEGIN
        DECLARE @CursorTime DATETIME = NULL, @CursorId INT = NULL;

        IF @BeforeLogId > 0
        BEGIN
            SELECT @CursorTime = ISNULL(EventDateTimeLocal, @NoTime), @CursorId = Id
            FROM GuardLogs
            WHERE Id = @BeforeLogId;
        END

        INSERT INTO @Page (Id)
        SELECT TOP (@PageSize) gl.Id
        FROM GuardLogs gl
        INNER JOIN @LogbookIds lbs ON gl.ClientSiteLogBookId = lbs.LogbookId
        WHERE
            (
                (lbs.IsPrimary = 1) OR
                (lbs.IsPrimary = 0 AND @IsLB = 1 AND gl.WAND_TAG_ENTRY_TYPE = 0) OR
                (lbs.IsPrimary = 0 AND @IsSW = 1 AND gl.WAND_TAG_ENTRY_TYPE <> 0)
            )
            AND
            (
                @CursorId IS NULL OR
                ISNULL(gl.EventDateTimeLocal, @NoTime) < @CursorTime OR
                (ISNULL(gl.EventDateTimeLocal, @NoTime) = @CursorTime AND gl.Id < @CursorId)
            )
        ORDER BY ISNULL(gl.EventDateTimeLocal, @NoTime) DESC, gl.Id DESC;
    END

    -- The page's rows: the same columns as sp_GetSiteLog, one row per image
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
    FROM @Page p
    INNER JOIN GuardLogs gl ON gl.Id = p.Id
    INNER JOIN @LogbookIds lbs ON gl.ClientSiteLogBookId = lbs.LogbookId
    INNER JOIN ClientSites cs ON lbs.SiteId = cs.Id
    LEFT JOIN GuardLogsDocumentImages img ON gl.Id = img.GuardLogId
    LEFT JOIN GuardLogins glog ON gl.GuardLoginId = glog.Id AND gl.ClientSiteLogBookId = glog.ClientSiteLogBookId
    LEFT JOIN Guards g ON glog.GuardId = g.Id
    ORDER BY ISNULL(gl.EventDateTimeLocal, @NoTime) DESC, gl.Id DESC;
END
GO
