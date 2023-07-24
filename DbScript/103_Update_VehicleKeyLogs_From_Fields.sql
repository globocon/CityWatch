
--	VehicleConfig
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'HR') WHERE  TruckConfig = 0
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'HR + DOG') WHERE  TruckConfig = 1
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Semi-20') WHERE  TruckConfig = 2
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Semi-40') WHERE  TruckConfig = 3
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'B-Double') WHERE  TruckConfig = 4
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'STAG B-Double') WHERE  TruckConfig = 5
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'A-Double') WHERE  TruckConfig = 6
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Other' AND TypeId = 1 ) WHERE  TruckConfig = 7
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Car (Sedan)') WHERE  TruckConfig = 8
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Car (4WD / SUV)') WHERE  TruckConfig = 9
UPDATE VehicleKeyLogs SET TruckConfig = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Car (VAN)') WHERE  TruckConfig = 10

--	TrailerType
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'ISO SKEL') WHERE  TrailerType = 0
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Sideloader') WHERE  TrailerType = 1
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Taughtliner') WHERE  TrailerType = 2
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Walking Floor / Hard Walls') WHERE  TrailerType = 3
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Tanker') WHERE  TrailerType = 4
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Flatbed') WHERE  TrailerType = 5
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Pneumatic Tanker') WHERE  TrailerType = 6
UPDATE VehicleKeyLogs SET TrailerType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Other' AND TypeId = 2 ) WHERE  TrailerType = 7

-- PersonType
UPDATE VehicleKeyLogs SET PersonType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Driver') WHERE  PersonType = 0 
UPDATE VehicleKeyLogs SET PersonType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Visitor') WHERE  PersonType = 1 
UPDATE VehicleKeyLogs SET PersonType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Contractor') WHERE  PersonType = 2 
UPDATE VehicleKeyLogs SET PersonType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Other' AND TypeId = 3 ) WHERE  PersonType = 3 
UPDATE VehicleKeyLogs SET PersonType = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Staff') WHERE  PersonType = 4 

-- EntryReason
UPDATE VehicleKeyLogs SET EntryReason = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'P/U') WHERE  EntryReason = 0 
UPDATE VehicleKeyLogs SET EntryReason = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'D/O') WHERE  EntryReason = 1
UPDATE VehicleKeyLogs SET EntryReason = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Overnight Parking') WHERE  EntryReason = 2
UPDATE VehicleKeyLogs SET EntryReason = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Appointment or Meeting') WHERE  EntryReason = 3
UPDATE VehicleKeyLogs SET EntryReason = (SELECT Id FROM KeyVehcileLogFields WHERE NAME = 'Interview') WHERE  EntryReason = 4

