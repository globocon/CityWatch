/*
    Change history for ClientSiteSmartWandTags.

    Why this exists: a wand tag can be added, edited, soft-deleted and re-added, and nothing
    recorded who did it or when. That cost real reporting data - "Locked (NEW)" on Casey Fields
    Oval 2 sat with every one of its registrations deleted for about three weeks, so every scan
    of it was saved as an unrecognised tag and never reached a report. Nobody could tell when it
    was deleted, by whom, or why.

    Captured by a trigger rather than from the application, so a change made by any route is
    recorded - the Kpi admin screen, the mobile app, a support script, or someone editing the
    table by hand. The application tells the trigger WHO is acting by setting session context
    immediately before it saves (see ClientSiteWandDataProvider); when that is absent - a direct
    database edit - the row records the SQL login instead, which is still better than nothing.

    One row per tag per change. Old* and New* hold the full before and after, so the exact edit
    is recoverable; Note is the same thing in a sentence, for reading rather than querying.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ClientSiteSmartWandTagsHist](
    [Id] [int] IDENTITY(1,1) NOT NULL,

    -- The tag this change was made to (ClientSiteSmartWandTags.Id). Deliberately not a foreign
    -- key: the history must survive the tag row being hard-deleted.
    [TagId] [int] NOT NULL,

    -- Added / Changed / Deleted / Restored / Removed.
    [Action] [nvarchar](20) NOT NULL,

    [ChangedOn] [datetime] NOT NULL,

    -- Whoever the application said was acting. Null for a change made outside the application.
    [ChangedByUserId] [int] NULL,
    [ChangedByGuardId] [int] NULL,

    -- Readable actor: the user or guard name, or the SQL login for a direct database change.
    [ChangedBy] [nvarchar](200) NULL,

    -- The change in a sentence, e.g. "Label changed from 'Close' to 'Locked (NEW)'".
    [Note] [nvarchar](1000) NULL,

    -- Full before/after. Old* is null on an insert, New* is null on a hard delete.
    [OldClientSiteId] [int] NULL,
    [NewClientSiteId] [int] NULL,
    [OldUId] [nvarchar](60) NULL,
    [NewUId] [nvarchar](60) NULL,
    [OldTagsTypeId] [int] NULL,
    [NewTagsTypeId] [int] NULL,
    [OldLabelDescription] [nvarchar](max) NULL,
    [NewLabelDescription] [nvarchar](max) NULL,
    [OldIsDeleted] [bit] NULL,
    [NewIsDeleted] [bit] NULL,
    [OldFqBypass] [bit] NULL,
    [NewFqBypass] [bit] NULL,

 CONSTRAINT [PK_ClientSiteSmartWandTagsHist] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[ClientSiteSmartWandTagsHist] ADD CONSTRAINT [DF_ClientSiteSmartWandTagsHist_ChangedOn] DEFAULT (getdate()) FOR [ChangedOn]
GO

-- "What happened to this tag", which is how the table is read.
CREATE NONCLUSTERED INDEX [IX_ClientSiteSmartWandTagsHist_Tag]
ON [dbo].[ClientSiteSmartWandTagsHist]([TagId] ASC, [ChangedOn] DESC)
GO

-- "What changed recently", and the lookup behind a tag's UId across re-registrations.
CREATE NONCLUSTERED INDEX [IX_ClientSiteSmartWandTagsHist_Recent]
ON [dbo].[ClientSiteSmartWandTagsHist]([ChangedOn] DESC)
INCLUDE ([NewUId], [OldUId], [Action])
GO


/*
    The recorder.

    FOR INSERT, UPDATE, DELETE in one trigger so an UPDATE is seen as a single row with both
    sides, rather than as a delete plus an insert.

    Set-based throughout: a statement that touches many tags writes one history row per tag, and
    the trigger never assumes a single row. An UPDATE that changes nothing on this table's
    columns is ignored, so a save that rewrites identical values does not fill the history with
    noise.
*/
CREATE TRIGGER [dbo].[Trigger_ClientSiteSmartWandTagsHist]
ON [dbo].[ClientSiteSmartWandTags]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
        RETURN;

    /* Who is acting. The application sets these immediately before it saves; they are null for
       any change made outside it, and ChangedBy then falls back to the SQL login. */
    DECLARE @UserId  int = TRY_CAST(SESSION_CONTEXT(N'cw_TagChangeUserId')  AS int);
    DECLARE @GuardId int = TRY_CAST(SESSION_CONTEXT(N'cw_TagChangeGuardId') AS int);
    DECLARE @Who     nvarchar(200) = TRY_CAST(SESSION_CONTEXT(N'cw_TagChangeBy') AS nvarchar(200));

    IF @UserId  = 0 SET @UserId  = NULL;
    IF @GuardId = 0 SET @GuardId = NULL;
    IF @Who IS NULL OR LTRIM(RTRIM(@Who)) = ''
        SET @Who = N'(direct database change) ' + SUSER_SNAME();

    INSERT INTO dbo.ClientSiteSmartWandTagsHist
        (TagId, Action, ChangedOn, ChangedByUserId, ChangedByGuardId, ChangedBy, Note,
         OldClientSiteId, NewClientSiteId, OldUId, NewUId, OldTagsTypeId, NewTagsTypeId,
         OldLabelDescription, NewLabelDescription, OldIsDeleted, NewIsDeleted, OldFqBypass, NewFqBypass)
    SELECT
        COALESCE(i.Id, d.Id),
        CASE
            WHEN d.Id IS NULL THEN N'Added'
            WHEN i.Id IS NULL THEN N'Removed'                                   -- hard delete
            WHEN ISNULL(d.IsDeleted,0) = 0 AND ISNULL(i.IsDeleted,0) = 1 THEN N'Deleted'
            WHEN ISNULL(d.IsDeleted,0) = 1 AND ISNULL(i.IsDeleted,0) = 0 THEN N'Restored'
            ELSE N'Changed'
        END,
        GETDATE(), @UserId, @GuardId, @Who,

        /* The note. Built from whichever columns actually differ, so it reads as a sentence and
           says nothing that did not change. */
        CASE
            WHEN d.Id IS NULL THEN
                N'Tag ' + ISNULL(i.UId,'') + N' added to site ' + CAST(i.ClientSiteId AS nvarchar(20))
                + N' as ''' + ISNULL(i.LabelDescription,'') + N''''
            WHEN i.Id IS NULL THEN
                N'Tag ' + ISNULL(d.UId,'') + N' deleted from the table outright'
            WHEN ISNULL(d.IsDeleted,0) = 0 AND ISNULL(i.IsDeleted,0) = 1 THEN
                N'Tag ' + ISNULL(i.UId,'') + N' (''' + ISNULL(i.LabelDescription,'') + N''') marked deleted'
            WHEN ISNULL(d.IsDeleted,0) = 1 AND ISNULL(i.IsDeleted,0) = 0 THEN
                N'Tag ' + ISNULL(i.UId,'') + N' (''' + ISNULL(i.LabelDescription,'') + N''') restored'
            ELSE
                STUFF(
                    CASE WHEN ISNULL(d.UId,'') <> ISNULL(i.UId,'')
                         THEN N'; UID changed from ''' + ISNULL(d.UId,'') + N''' to ''' + ISNULL(i.UId,'') + N'''' ELSE N'' END +
                    CASE WHEN ISNULL(d.ClientSiteId,0) <> ISNULL(i.ClientSiteId,0)
                         THEN N'; site changed from ' + CAST(ISNULL(d.ClientSiteId,0) AS nvarchar(20)) + N' to ' + CAST(ISNULL(i.ClientSiteId,0) AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN ISNULL(d.LabelDescription,'') <> ISNULL(i.LabelDescription,'')
                         THEN N'; label changed from ''' + ISNULL(d.LabelDescription,'') + N''' to ''' + ISNULL(i.LabelDescription,'') + N'''' ELSE N'' END +
                    CASE WHEN ISNULL(d.TagsTypeId,0) <> ISNULL(i.TagsTypeId,0)
                         THEN N'; tag type changed from ' + CAST(ISNULL(d.TagsTypeId,0) AS nvarchar(20)) + N' to ' + CAST(ISNULL(i.TagsTypeId,0) AS nvarchar(20)) ELSE N'' END +
                    CASE WHEN ISNULL(d.FqBypass,0) <> ISNULL(i.FqBypass,0)
                         THEN N'; frequency bypass turned ' + CASE WHEN i.FqBypass = 1 THEN N'on' ELSE N'off' END ELSE N'' END
                , 1, 2, N'')    -- drop the leading "; "
        END,

        d.ClientSiteId, i.ClientSiteId,
        d.UId, i.UId,
        d.TagsTypeId, i.TagsTypeId,
        d.LabelDescription, i.LabelDescription,
        d.IsDeleted, i.IsDeleted,
        d.FqBypass, i.FqBypass
    FROM inserted i
    FULL OUTER JOIN deleted d ON d.Id = i.Id
    WHERE
        -- insert or delete: always worth recording
        i.Id IS NULL OR d.Id IS NULL
        -- update: only when something on this table actually changed
        OR ISNULL(d.UId,'') <> ISNULL(i.UId,'')
        OR ISNULL(d.ClientSiteId,0) <> ISNULL(i.ClientSiteId,0)
        OR ISNULL(d.TagsTypeId,0) <> ISNULL(i.TagsTypeId,0)
        OR ISNULL(d.LabelDescription,'') <> ISNULL(i.LabelDescription,'')
        OR ISNULL(d.IsDeleted,0) <> ISNULL(i.IsDeleted,0)
        OR ISNULL(d.FqBypass,0) <> ISNULL(i.FqBypass,0);
END
GO
