-- Links a staff document to its category (null for the document types that have no categories)
-- Sequential ID: 357

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffDocuments]') AND name = 'CategoryId')
BEGIN
    ALTER TABLE [dbo].[StaffDocuments] ADD [CategoryId] INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StaffDocuments_StaffDocumentCategories_CategoryId')
BEGIN
    ALTER TABLE [dbo].[StaffDocuments]
    ADD CONSTRAINT [FK_StaffDocuments_StaffDocumentCategories_CategoryId]
        FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[StaffDocumentCategories] ([Id]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StaffDocuments_CategoryId' AND object_id = OBJECT_ID('[dbo].[StaffDocuments]'))
    CREATE INDEX [IX_StaffDocuments_CategoryId] ON [dbo].[StaffDocuments] ([CategoryId] ASC);
GO
