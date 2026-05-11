IF NOT EXISTS (SELECT * FROM guardaccess WHERE Id = 21)
BEGIN
    SET IDENTITY_INSERT guardaccess ON;
    INSERT INTO guardaccess (Id, AccessName) VALUES (21, 'RO + Editor');
    SET IDENTITY_INSERT guardaccess OFF;
END
GO
