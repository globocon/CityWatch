/* 379: One CustomFieldLog per field per logbook day, enforced by the database.

   A CustomFieldLog is one custom field's value for one logbook day, so
   (CustomFieldId, ClientSiteLogBookId) is its identity. The Id column is only a surrogate,
   and the code used to decide insert-vs-update on "Id == 0" alone - which is not an identity
   test, so nothing stopped a second copy being written.

   The same schema already solves this problem the same way one table up: ClientSiteLogBooks
   carries UQ_Site_Type_Date (DbScript/89) so a site cannot get two logbooks for one day.
   This is that constraint for the rows beneath it.

   The application fix (an atomic seed, in place of the check-then-act that raced) removes the
   cause. This index is the backstop that makes a recurrence impossible rather than unlikely -
   and it is what lets the code treat a duplicate insert as "someone else already did it"
   instead of having to guess.

   COLUMN ORDER. Declared (ClientSiteLogBookId, CustomFieldId), not the other way round. The
   pair enforces the same uniqueness whichever way it is written, but the leading column is
   the only one an index can seek on, and every read of this table filters by logbook:

       GetCustomFieldLogs(logBookId)   ->  WHERE ClientSiteLogBookId = @id
                                           (every logbook open, every guard log report)

   So this order makes the constraint pay for the read as well, instead of needing a second
   index beside it. The seed's NOT EXISTS supplies both columns and seeks either way.
   DayValue is INCLUDEd so that read is covered and never touches the table itself.

   ORDER OF OPERATIONS:
       1. 378_Dedupe_CustomFieldLogs.sql   - must come first, or this cannot be created
       2. this script
       3. deploy the application change
   Running this BEFORE the application change is safe: the app's seed is written to tolerate
   the constraint. Running it before 378 does nothing except print why.

   Rollback: DROP INDEX UX_CustomFieldLogs_LogBook_Field ON dbo.CustomFieldLogs;
*/

SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CustomFieldLogs_LogBook_Field'
                                       AND object_id = OBJECT_ID(N'dbo.CustomFieldLogs'))
BEGIN
    PRINT '379: UX_CustomFieldLogs_LogBook_Field already exists - nothing to do.';
END
ELSE IF EXISTS
(
    SELECT 1
    FROM dbo.CustomFieldLogs
    GROUP BY CustomFieldId, ClientSiteLogBookId
    HAVING COUNT(*) > 1
)
BEGIN
    /* Stop with a readable reason rather than letting CREATE UNIQUE INDEX fail with
       "The CREATE UNIQUE INDEX statement terminated because a duplicate key was found",
       which says nothing about what to do next. */
    DECLARE @dupes INT =
    (
        SELECT COUNT(*) FROM
        (
            SELECT 1 AS x
            FROM dbo.CustomFieldLogs
            GROUP BY CustomFieldId, ClientSiteLogBookId
            HAVING COUNT(*) > 1
        ) AS d
    );

    PRINT '379: ABORTED - ' + CAST(@dupes AS VARCHAR(20)) +
          ' (CustomFieldId, ClientSiteLogBookId) pair(s) still have more than one row.';
    PRINT '379: run 378_Dedupe_CustomFieldLogs.sql first, then re-run this script.';
END
ELSE
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_CustomFieldLogs_LogBook_Field
        ON dbo.CustomFieldLogs (ClientSiteLogBookId, CustomFieldId)
        INCLUDE (DayValue);

    PRINT '379: UX_CustomFieldLogs_LogBook_Field created.';
END
GO
