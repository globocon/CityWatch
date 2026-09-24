IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Guards') AND name = 'IsROEditorAccess')
BEGIN
    ALTER TABLE Guards ADD IsROEditorAccess BIT NOT NULL DEFAULT 0;
END
GO
