Create Table dbo.KeyVehicleLogAuditHistory
(
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	KeyVehicleLogId INT NOT NULL FOREIGN KEY REFERENCES VehicleKeyLogs(Id) ON DELETE CASCADE,
	GuardLoginId INT NOT NULL FOREIGN KEY REFERENCES GuardLogins(Id),
	AuditTime DATETIME NOT NULL,
	AuditMessage VARCHAR (512) NOT NULL,
	IsCreate BIT default 0,
 )