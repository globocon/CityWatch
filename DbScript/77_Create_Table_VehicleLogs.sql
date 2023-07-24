CREATE TABLE dbo.VehicleKeyLogs
(
	Id					int Not Null IDENTITY(1,1) PRIMARY KEY,
    ClientSiteLogBookId	int Not Null FOREIGN KEY References ClientSiteLogBooks(Id) ON DELETE CASCADE,
	GuardLoginId		int	Not Null,	
	EntryTime			datetime Not Null,	
	SentInTime			datetime Null,	
	ExitTime			datetime Null,	
	VehicleRego			varchar(20) Null,
	Trailer1Rego		varchar(20) Null,
	Trailer2Rego		varchar(20) Null,
	Trailer3Rego		varchar(20) Null,
	Plate				varchar(10) Null,
	KeyNo				varchar(20) Null,	
	CompanyName			varchar(100) Null,
	PersonName			varchar(100) Null,	
	PersonType			int	Null,	
	MobileNumber		varchar(20) Null,	
	PurposeOfEntry		varchar(256) Null,	
	InWeight			decimal(6,2) Null,	
	OutWeight			decimal(6,2) Null,	
	TareWeight			decimal(6,2) Null,	
	Notes				varchar(512) Null
)