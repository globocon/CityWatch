/* 378: Remove duplicate rows from dbo.CustomFieldLogs.

   A CustomFieldLog is one custom field's value for one logbook day, so
   (CustomFieldId, ClientSiteLogBookId) is its real identity - but nothing enforced that, and
   ViewDataService.GetCustomFieldLogs seeded the table from a READ with a check-then-act:

       if (!customFieldLogs.Any())  ->  insert one row per configured field

   Two readers hitting a logbook that has no rows yet - a guard opening the web logbook while
   the mobile app fetches the same site, which is shift changeover - both saw "none", and both
   inserted the whole set. Three readers, three sets.

   This script keeps ONE row per (CustomFieldId, ClientSiteLogBookId) and deletes the rest.

   WHICH ROW SURVIVES. The duplicates are near-identical: same field, same logbook, and the
   value is whatever the guard later typed into one of them. Preference order:
       1. a row with a DayValue  - real data beats a blank placeholder
       2. among those, the most recently written (highest Id)
       3. if every copy is blank, the lowest Id - the original
   Anything deleted is therefore a blank placeholder, or an older value superseded by a newer
   one on the same field and day.

   RUN THIS BEFORE 379. The unique index in 379 cannot be created while duplicates exist;
   379 checks and stops with a readable message rather than failing on the index itself.

   Re-runnable: on a clean table it reports 0 and changes nothing.
   Rollback: none - the deleted rows are duplicates. Take a backup of the table first if you
   want one (the SELECT below shows exactly what will go).
*/

SET NOCOUNT ON;

/* What is about to be deleted, for the record. Run the script with results captured if you
   want this kept - it is the only trace of the removed rows. */
PRINT '378: rows that will be deleted (field / logbook / id / value):';

;WITH Ranked AS
(
    SELECT
        l.Id,
        l.CustomFieldId,
        l.ClientSiteLogBookId,
        l.DayValue,
        ROW_NUMBER() OVER (
            PARTITION BY l.CustomFieldId, l.ClientSiteLogBookId
            ORDER BY
                /* 0 sorts first: a row that carries a value wins over a blank one. */
                CASE WHEN l.DayValue IS NULL OR LTRIM(RTRIM(l.DayValue)) = '' THEN 1 ELSE 0 END,
                /* Among rows with a value, the newest write wins. Among blanks, this is
                   reversed below by the Id tiebreak so the original survives. */
                CASE WHEN l.DayValue IS NULL OR LTRIM(RTRIM(l.DayValue)) = '' THEN l.Id ELSE -l.Id END
        ) AS rn
    FROM dbo.CustomFieldLogs AS l
)
SELECT Id, CustomFieldId, ClientSiteLogBookId, DayValue
FROM Ranked
WHERE rn > 1
ORDER BY ClientSiteLogBookId, CustomFieldId, Id;

DECLARE @deleted INT;

;WITH Ranked AS
(
    SELECT
        l.Id,
        ROW_NUMBER() OVER (
            PARTITION BY l.CustomFieldId, l.ClientSiteLogBookId
            ORDER BY
                CASE WHEN l.DayValue IS NULL OR LTRIM(RTRIM(l.DayValue)) = '' THEN 1 ELSE 0 END,
                CASE WHEN l.DayValue IS NULL OR LTRIM(RTRIM(l.DayValue)) = '' THEN l.Id ELSE -l.Id END
        ) AS rn
    FROM dbo.CustomFieldLogs AS l
)
DELETE FROM Ranked
WHERE rn > 1;

SET @deleted = @@ROWCOUNT;

PRINT '378: deleted ' + CAST(@deleted AS VARCHAR(20)) + ' duplicate CustomFieldLogs row(s).';

/* Prove it: this must come back empty before 379 will run. */
IF EXISTS
(
    SELECT 1
    FROM dbo.CustomFieldLogs
    GROUP BY CustomFieldId, ClientSiteLogBookId
    HAVING COUNT(*) > 1
)
    PRINT '378: WARNING - duplicates still present. Investigate before running 379.';
ELSE
    PRINT '378: no duplicates remain - safe to run 379.';
GO
