-- Migrate existing strings to int equivalent
UPDATE [PcarRouteDailyVisits] SET [Status] = '1' WHERE [Status] = 'InProgress';
UPDATE [PcarRouteDailyVisits] SET [Status] = '2' WHERE [Status] = 'Completed';
UPDATE [PcarRouteDailyVisits] SET [Status] = '3' WHERE [Status] LIKE 'Pushed%';

-- Alter the column to int
ALTER TABLE [PcarRouteDailyVisits]
ALTER COLUMN [Status] int null;
