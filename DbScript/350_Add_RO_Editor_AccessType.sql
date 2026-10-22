-- First, fix any existing nulls that might be causing the exception
UPDATE guardaccess SET IsDeleted = 0 WHERE IsDeleted IS NULL;
UPDATE guardaccess SET CredentialOrder = Id WHERE CredentialOrder IS NULL;
GO

-- Now ensure the RO + Editor record is correctly fully populated
IF NOT EXISTS (SELECT * FROM guardaccess WHERE Id = 21)
BEGIN
    SET IDENTITY_INSERT guardaccess ON;
    INSERT INTO guardaccess (Id, AccessName, IsDeleted, CredentialOrder) 
    VALUES (21, 'RO + Editor', 0, 21);
    SET IDENTITY_INSERT guardaccess OFF;
END
ELSE
BEGIN
    UPDATE guardaccess 
    SET AccessName = 'RO + Editor', 
        IsDeleted = 0, 
        CredentialOrder = 21 
    WHERE Id = 21;
END
GO
