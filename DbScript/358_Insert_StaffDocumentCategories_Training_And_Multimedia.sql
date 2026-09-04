-- Seed the Training and Multimedia categories and move the existing Training documents
-- into the default "Training" category so nothing disappears from the Downloads page.
-- Re-runnable.
-- Sequential ID: 358

/* Training (StaffDocuments.DocumentType = 2) */
IF NOT EXISTS (SELECT 1 FROM [dbo].[StaffDocumentCategories] WHERE [Name] = 'Training' AND [DocumentType] = 2)
    INSERT INTO [dbo].[StaffDocumentCategories] ([Name], [DocumentType], [SortOrder], [IsActive])
    VALUES ('Training', 2, 1, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[StaffDocumentCategories] WHERE [Name] = 'Fire Training' AND [DocumentType] = 2)
    INSERT INTO [dbo].[StaffDocumentCategories] ([Name], [DocumentType], [SortOrder], [IsActive])
    VALUES ('Fire Training', 2, 2, 1);

/* Multimedia (StaffDocuments.DocumentType = 7) */
IF NOT EXISTS (SELECT 1 FROM [dbo].[StaffDocumentCategories] WHERE [Name] = 'General Multimedia' AND [DocumentType] = 7)
    INSERT INTO [dbo].[StaffDocumentCategories] ([Name], [DocumentType], [SortOrder], [IsActive])
    VALUES ('General Multimedia', 7, 1, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[StaffDocumentCategories] WHERE [Name] = 'Client Multimedia' AND [DocumentType] = 7)
    INSERT INTO [dbo].[StaffDocumentCategories] ([Name], [DocumentType], [SortOrder], [IsActive])
    VALUES ('Client Multimedia', 7, 2, 1);
GO

/* Existing training documents keep working - they are assigned to the default Training category */
DECLARE @TrainingCategoryId INT =
    (SELECT TOP 1 [Id] FROM [dbo].[StaffDocumentCategories] WHERE [Name] = 'Training' AND [DocumentType] = 2);

IF @TrainingCategoryId IS NOT NULL
BEGIN
    UPDATE [dbo].[StaffDocuments]
    SET [CategoryId] = @TrainingCategoryId
    WHERE [DocumentType] = 2 AND [CategoryId] IS NULL;
END
GO
