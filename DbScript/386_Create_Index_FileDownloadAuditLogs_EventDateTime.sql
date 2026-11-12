/* 386: p1-373 every /StaffDocs download is now logged, so FileDownloadAuditLogs grows much faster.
   The Download Logs report filters by EventDateTime, and each download checks the last 2 minutes for a
   row already written by the page - both scan the whole table without this index.
   Safe to re-run. */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FileDownloadAuditLogs_EventDateTime' AND object_id = OBJECT_ID('FileDownloadAuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FileDownloadAuditLogs_EventDateTime]
        ON [FileDownloadAuditLogs] ([EventDateTime])
        INCLUDE ([IPAddress], [UserID], [GuardID])
END
GO
