-- Add AccessKey column to RosterBinders table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RosterBinders') AND name = 'AccessKey')
BEGIN
    ALTER TABLE RosterBinders ADD AccessKey UNIQUEIDENTIFIER DEFAULT NEWID();
END
GO

-- Populate existing records with a Guid if they are NULL (though DEFAULT NEWID() handles this for new records, 
-- depending on the SQL version and existing data, we might need an explicit update)
UPDATE RosterBinders SET AccessKey = NEWID() WHERE AccessKey IS NULL;
GO
