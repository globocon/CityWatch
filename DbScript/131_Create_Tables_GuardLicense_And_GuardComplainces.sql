
CREATE TABLE dbo.GuardLicenses
(
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	GuardId INT NOT NULL FOREIGN KEY REFERENCES Guards(Id),
	LicenseNo VARCHAR(50) NOT NULL,
	LicenseType  INT NOT NULL,
	ExpiryDate DATETIME  NULL,
	Reminder1 INT NULL,
	Reminder2 INT NULL,
	[FileName] VARCHAR(512) NULL,
	CONSTRAINT uc_LicenseNo UNIQUE (LicenseNo) 
)
GO


CREATE TABLE dbo.GuardCompliances
(
	Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	GuardId INT NOT NULL FOREIGN KEY REFERENCES Guards(Id),
	ReferenceNo VARCHAR(50) NOT NULL,
	[Description] VARCHAR(100) NULL,
	ExpiryDate DATETIME  NULL,
	Reminder1 INT NULL,
	Reminder2 INT NULL,
	[FileName] varchar(512) NULL,
	CONSTRAINT uc_ReferenceNo UNIQUE (ReferenceNo) 
)
GO