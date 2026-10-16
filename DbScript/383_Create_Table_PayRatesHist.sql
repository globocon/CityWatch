/*
    Change history for PayRates (Admin > Settings > C4i HR > HR Settings > Renumeration - Pay Rates).

    Why this exists: on 10 Oct 2026 the Guard Pay on "Prixcar - Sat (2026)" went to $0.00 on live
    and guards were paid wrong. Nobody had edited it on the Pay Rates screen, and nothing recorded
    who changed it, when, or from what - the investigation had to piece it together from roster
    audit rows.

    The cause was the roster shift save (Booking.cshtml.cs OnPostAddShift), which since 11 May 2026
    copied the shift popup's Pay Rate value into the MASTER PayRates row. From 8 Oct 2026 (PR #1841)
    a shift relieved by a company only (e.g. GroupOne) shows a $0 rate, so saving or drag-copying
    such a shift from the Guard Roster popup wrote $0 into the master rate for every guard on it.
    That write-back is removed in the same change as this script.

    Captured by a trigger rather than from the application, so a change made by any route is
    recorded - the Pay Rates screen, any future code path, a support script, or someone editing the
    table by hand. The application tells the trigger WHO is acting by setting session context
    immediately before it saves (see ConfigDataProvider.SavePayRate); when that is absent - a direct
    database edit - the row records the SQL login instead.

    One row per pay rate per change. Old* and New* hold the full before and after; Note is the same
    thing in a sentence, for reading rather than querying.

    Same pattern as 382_Create_Table_ClientSiteSmartWandTagsHist.sql.

    ------------------------------------------------------------------------------------------------
    CHANGE LOG  (every code change is tagged "p7-PayRateOverwriteFix" - search for it)
    ------------------------------------------------------------------------------------------------
    PR #1845 (12 Oct 2026, Dileep)  Stop shift saves overwriting the master pay rate; this script;
                                    History button.
    PR #1846 (12 Oct 2026, Dileep)  Pay rate save failed on test ("does not support user-initiated
                                    transactions") - save now runs inside the execution strategy.
    PR #1847 (12 Oct 2026, Dileep)  History showed the SQL login for admin users - now the logged-in
                                    user (AuthUserHelper.GetLoggedInUserId).
    PR #1848 (12 Oct 2026, Dileep)  Comments / revert notes only, no behaviour change.

    ------------------------------------------------------------------------------------------------
    HOW TO REVERT
    ------------------------------------------------------------------------------------------------
    Each part can be reverted on its own. Every spot has a "REVERT:" note beside it.

    A. Only remove the change history (keep the $0-overwrite fix) - safe:
       1. CityWatch.Web/wwwroot/js/payrates.js       - History button, openPayRateHistory(), column width
       2. CityWatch.Web/Pages/Admin/Settings.cshtml  - payRateHistoryModal
       3. CityWatch.Web/Pages/Admin/Settings.cshtml.cs - OnGetPayRateHistory; SavePayRate/DeletePayRate calls
       4. CityWatch.Data/Providers/ConfigDataProvider.cs - interface lines, changedByUserId params,
          GetPayRateHistory, SaveWithPayRateChangeAttributed, ApplyPayRateChangeContext
       5. CityWatch.Data/CityWatchDbContext.cs (PayRatesHist DbSet) + CityWatch.Data/Models/PayRatesHist.cs
       6. Deploy that Web build FIRST, then run the database undo below.

    B. Bring back editing the master rate from the shift popup - NOT recommended, restores the $0 bug:
       1. CityWatch.Web/Pages/roster/Booking.cshtml.cs - OnPostAddShift parameter + write-back block
       2. CityWatch.Web/Pages/roster/Booking.cshtml    - modalPayRate input + 3 "payRate:" lines
       3. CityWatch.Web/Pages/Guard/_GuardRosterModal.cshtml - 3 "payRate:" lines + payRateValue var

    Or with git, in order: git revert -m 1 <merge of #1847>, <merge of #1846>, <merge of #1845>
    (then run the database undo).

    Database undo (run only AFTER the Web build without the history code is deployed - the old build
    is fine with the table present, the new build fails without it):

        DROP TRIGGER IF EXISTS [dbo].[Trigger_PayRatesHist];
        DROP TABLE IF EXISTS [dbo].[PayRatesHist];

    Dropping the table deletes the recorded history - export it first if it may be needed:
        SELECT * FROM dbo.PayRatesHist ORDER BY ChangedOn;
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PayRatesHist](
    [Id] [int] IDENTITY(1,1) NOT NULL,

    -- The pay rate this change was made to (PayRates.Id). Deliberately not a foreign key: the
    -- history must survive the pay rate row being hard-deleted.
    [PayRateId] [int] NOT NULL,

    -- Added / Changed / Deleted / Restored / Removed.
    [Action] [nvarchar](20) NOT NULL,

    [ChangedOn] [datetime] NOT NULL,

    -- Whoever the application said was acting. Null for a change made outside the application.
    [ChangedByUserId] [int] NULL,

    -- Readable actor: the user name, or the SQL login for a direct database change.
    [ChangedBy] [nvarchar](200) NULL,

    -- The change in a sentence, e.g. "Guard Pay changed from 41.00 to 0.00".
    [Note] [nvarchar](1000) NULL,

    -- Full before/after. Old* is null on an insert, New* is null on a hard delete.
    [OldDescription] [nvarchar](max) NULL,
    [NewDescription] [nvarchar](max) NULL,
    [OldPayRateGroupId] [int] NULL,
    [NewPayRateGroupId] [int] NULL,
    [OldSellRateToClient] [decimal](18, 2) NULL,
    [NewSellRateToClient] [decimal](18, 2) NULL,
    [OldComms1] [decimal](18, 2) NULL,
    [NewComms1] [decimal](18, 2) NULL,
    [OldComms2] [decimal](18, 2) NULL,
    [NewComms2] [decimal](18, 2) NULL,
    [OldGuardPayRate] [decimal](18, 2) NULL,
    [NewGuardPayRate] [decimal](18, 2) NULL,
    [OldCurrency] [nvarchar](max) NULL,
    [NewCurrency] [nvarchar](max) NULL,
    [OldIsDeleted] [bit] NULL,
    [NewIsDeleted] [bit] NULL,

 CONSTRAINT [PK_PayRatesHist] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PayRatesHist] ADD CONSTRAINT [DF_PayRatesHist_ChangedOn] DEFAULT (getdate()) FOR [ChangedOn]
GO

-- "What happened to this pay rate", which is how the table is read (the History button).
CREATE NONCLUSTERED INDEX [IX_PayRatesHist_PayRate]
ON [dbo].[PayRatesHist]([PayRateId] ASC, [ChangedOn] DESC)
GO


/*
    The recorder.

    FOR INSERT, UPDATE, DELETE in one trigger so an UPDATE is seen as a single row with both
    sides, rather than as a delete plus an insert.

    Set-based throughout: never assumes a single row. An UPDATE that changes nothing on this
    table's columns is ignored, so a save that rewrites identical values does not fill the history
    with noise.
*/
CREATE TRIGGER [dbo].[Trigger_PayRatesHist]
ON [dbo].[PayRates]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
        RETURN;

    /* Who is acting. The application sets these immediately before it saves; they are null for
       any change made outside it, and ChangedBy then falls back to the SQL login. */
    DECLARE @UserId int = TRY_CAST(SESSION_CONTEXT(N'cw_PayRateChangeUserId') AS int);
    DECLARE @Who    nvarchar(200) = TRY_CAST(SESSION_CONTEXT(N'cw_PayRateChangeBy') AS nvarchar(200));

    IF @UserId = 0 SET @UserId = NULL;
    IF @Who IS NULL OR LTRIM(RTRIM(@Who)) = ''
        SET @Who = N'(direct database change) ' + SUSER_SNAME();

    INSERT INTO dbo.PayRatesHist
        (PayRateId, Action, ChangedOn, ChangedByUserId, ChangedBy, Note,
         OldDescription, NewDescription, OldPayRateGroupId, NewPayRateGroupId,
         OldSellRateToClient, NewSellRateToClient, OldComms1, NewComms1, OldComms2, NewComms2,
         OldGuardPayRate, NewGuardPayRate, OldCurrency, NewCurrency, OldIsDeleted, NewIsDeleted)
    SELECT
        COALESCE(i.Id, d.Id),
        CASE
            WHEN d.Id IS NULL THEN N'Added'
            WHEN i.Id IS NULL THEN N'Removed'                                   -- hard delete
            WHEN ISNULL(d.IsDeleted,0) = 0 AND ISNULL(i.IsDeleted,0) = 1 THEN N'Deleted'
            WHEN ISNULL(d.IsDeleted,0) = 1 AND ISNULL(i.IsDeleted,0) = 0 THEN N'Restored'
            ELSE N'Changed'
        END,
        GETDATE(), @UserId, @Who,

        /* The note. Built from whichever columns actually differ, so it says nothing that did
           not change. */
        CASE
            WHEN d.Id IS NULL THEN
                N'Pay rate ''' + ISNULL(i.Description,'') + N''' added: Sell ' + CAST(i.SellRateToClient AS nvarchar(20))
                + N', Comms 1 ' + CAST(i.Comms1 AS nvarchar(20)) + N', Comms 2 ' + CAST(i.Comms2 AS nvarchar(20))
                + N', Guard Pay ' + CAST(i.GuardPayRate AS nvarchar(20))
            WHEN i.Id IS NULL THEN
                N'Pay rate ''' + ISNULL(d.Description,'') + N''' deleted from the table outright'
            WHEN ISNULL(d.IsDeleted,0) = 0 AND ISNULL(i.IsDeleted,0) = 1 THEN
                N'Pay rate ''' + ISNULL(i.Description,'') + N''' deleted'
            WHEN ISNULL(d.IsDeleted,0) = 1 AND ISNULL(i.IsDeleted,0) = 0 THEN
                N'Pay rate ''' + ISNULL(i.Description,'') + N''' restored'
            ELSE
                STUFF(
                    CASE WHEN ISNULL(d.Description,'') <> ISNULL(i.Description,'')
                         THEN N'; Description changed from ''' + ISNULL(d.Description,'') + N''' to ''' + ISNULL(i.Description,'') + N'''' ELSE N'' END +
                    CASE WHEN ISNULL(d.PayRateGroupId,0) <> ISNULL(i.PayRateGroupId,0)
                         THEN N'; Group changed from ' + CAST(ISNULL(d.PayRateGroupId,0) AS nvarchar(20)) + N' to ' + CAST(ISNULL(i.PayRateGroupId,0) AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN d.SellRateToClient <> i.SellRateToClient
                         THEN N'; Sell Rate changed from ' + CAST(d.SellRateToClient AS nvarchar(20)) + N' to ' + CAST(i.SellRateToClient AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN d.Comms1 <> i.Comms1
                         THEN N'; Comms 1 changed from ' + CAST(d.Comms1 AS nvarchar(20)) + N' to ' + CAST(i.Comms1 AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN d.Comms2 <> i.Comms2
                         THEN N'; Comms 2 changed from ' + CAST(d.Comms2 AS nvarchar(20)) + N' to ' + CAST(i.Comms2 AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN d.GuardPayRate <> i.GuardPayRate
                         THEN N'; Guard Pay changed from ' + CAST(d.GuardPayRate AS nvarchar(20)) + N' to ' + CAST(i.GuardPayRate AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN ISNULL(d.Currency,'') <> ISNULL(i.Currency,'')
                         THEN N'; Currency changed from ''' + ISNULL(d.Currency,'') + N''' to ''' + ISNULL(i.Currency,'') + N'''' ELSE N'' END
                , 1, 2, N'')    -- drop the leading "; "
        END,

        d.Description, i.Description,
        d.PayRateGroupId, i.PayRateGroupId,
        d.SellRateToClient, i.SellRateToClient,
        d.Comms1, i.Comms1,
        d.Comms2, i.Comms2,
        d.GuardPayRate, i.GuardPayRate,
        d.Currency, i.Currency,
        d.IsDeleted, i.IsDeleted
    FROM inserted i
    FULL OUTER JOIN deleted d ON d.Id = i.Id
    WHERE
        -- insert or delete: always worth recording
        i.Id IS NULL OR d.Id IS NULL
        -- update: only when something on this table actually changed
        OR ISNULL(d.Description,'') <> ISNULL(i.Description,'')
        OR ISNULL(d.PayRateGroupId,0) <> ISNULL(i.PayRateGroupId,0)
        OR d.SellRateToClient <> i.SellRateToClient
        OR d.Comms1 <> i.Comms1
        OR d.Comms2 <> i.Comms2
        OR d.GuardPayRate <> i.GuardPayRate
        OR ISNULL(d.Currency,'') <> ISNULL(i.Currency,'')
        OR ISNULL(d.IsDeleted,0) <> ISNULL(i.IsDeleted,0);
END
GO
