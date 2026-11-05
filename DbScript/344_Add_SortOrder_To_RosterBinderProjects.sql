-- Add SortOrder column to RosterBinderProjects table
-- Sequential ID: 344

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[RosterBinderProjects]') AND name = 'SortOrder')
BEGIN
    ALTER TABLE [dbo].[RosterBinderProjects] ADD [SortOrder] INT DEFAULT ((0)) NOT NULL;
END
GO
