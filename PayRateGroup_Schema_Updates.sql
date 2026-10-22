-- =============================================
-- Author: Antigravity
-- Create date: 2026-04-14
-- Description: Adds PayRateGroup table and updates PayRate table to support grouping.
-- =============================================

-- 1. Create PayRateGroup Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PayRateGroup')
BEGIN
    CREATE TABLE [dbo].[PayRateGroup] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );
END
GO

-- 2. Add PayRateGroupId to PayRate table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[PayRate]') AND name = 'PayRateGroupId')
BEGIN
    ALTER TABLE [dbo].[PayRate] ADD [PayRateGroupId] INT NULL;
    
    -- Add Foreign Key
    ALTER TABLE [dbo].[PayRate] WITH CHECK ADD CONSTRAINT [FK_PayRate_PayRateGroup] FOREIGN KEY([PayRateGroupId])
    REFERENCES [dbo].[PayRateGroup] ([Id]);
END
GO

-- 3. (Optional) Migrate existing descriptions to groups
-- This script extracts the group name if it follows the "Group - Description" pattern
INSERT INTO [dbo].[PayRateGroup] ([Name])
SELECT DISTINCT LEFT(Description, CHARINDEX(' - ', Description) - 1)
FROM [dbo].[PayRate]
WHERE Description LIKE '% - %' 
AND IsDeleted = 0
AND LEFT(Description, CHARINDEX(' - ', Description) - 1) NOT IN (SELECT Name FROM [dbo].[PayRateGroup]);

UPDATE PR
SET PR.PayRateGroupId = PRG.Id
FROM [dbo].[PayRate] PR
JOIN [dbo].[PayRateGroup] PRG ON PRG.Name = LEFT(PR.Description, CHARINDEX(' - ', PR.Description) - 1)
WHERE PR.Description LIKE '% - %'
AND PR.PayRateGroupId IS NULL;
GO
