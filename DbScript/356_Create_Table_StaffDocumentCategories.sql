-- Category lookup for staff documents (Training / Fire Training, General Multimedia / Client Multimedia)
-- DocumentType matches StaffDocuments.DocumentType (2 = Training, 7 = Multimedia)
-- Sequential ID: 356

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StaffDocumentCategories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[StaffDocumentCategories] (
        [Id]           INT            IDENTITY (1, 1) NOT NULL,
        [Name]         NVARCHAR (100) NOT NULL,
        [DocumentType] INT            NOT NULL,
        [SortOrder]    INT            DEFAULT ((0)) NOT NULL,
        [IsActive]     BIT            DEFAULT ((1)) NOT NULL,
        CONSTRAINT [PK_StaffDocumentCategories] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StaffDocumentCategories_DocumentType' AND object_id = OBJECT_ID('[dbo].[StaffDocumentCategories]'))
    CREATE INDEX [IX_StaffDocumentCategories_DocumentType] ON [dbo].[StaffDocumentCategories] ([DocumentType] ASC);
GO
