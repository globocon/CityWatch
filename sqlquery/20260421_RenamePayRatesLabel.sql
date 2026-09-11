-- Rename "Pay Rates" to "Renumeration – Pay Rates" in HR Settings labels
-- This script ensures any database-stored labels match the new UI naming convention

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HRGroups')
BEGIN
    UPDATE [dbo].[HRGroups]
    SET [Name] = 'Renumeration – Pay Rates'
    WHERE [Name] = 'Pay Rates';
    
    PRINT 'Updated HRGroups table labels.';
END

-- Also check if there are any settings descriptions that exactly match "Pay Rates"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HrSettings')
BEGIN
    UPDATE [dbo].[HrSettings]
    SET [Description] = 'Renumeration – Pay Rates'
    WHERE [Description] = 'Pay Rates';

    PRINT 'Updated HrSettings table descriptions.';
END
GO
