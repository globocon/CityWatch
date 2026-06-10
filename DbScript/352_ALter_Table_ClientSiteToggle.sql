
Alter table [ClientSiteToggle]
Add IsISO bit null,
IsVin bit null, 
IsTrailerRego bit null,
IsCarsStock bit null;

GO

  INSERT INTO KeyVehcileLogFields
  VALUES (10,'Blue',0), (10,'Green',0), (10,'Red',0);

GO

Alter table [KeyVehicleLogVisitorProfiles]
Add [Trailer5Rego] Varchar(20) null
    ,[Trailer6Rego] Varchar(20) null
    ,[Trailer7Rego] Varchar(20) null
    ,[Trailer8Rego] Varchar(20) null
    ,[Trailer5PlateId] int null
    ,[Trailer6PlateId] int null
    ,[Trailer7PlateId] int null
    ,[Trailer8PlateId] int null;

GO

Alter table [KeyVehicleLogProfiles]
Add [Trailer5Rego] Varchar(20) null
    ,[Trailer6Rego] Varchar(20) null
    ,[Trailer7Rego] Varchar(20) null
    ,[Trailer8Rego] Varchar(20) null;
	
GO

Alter table [VehicleKeyLogs]
Add [Trailer5Rego] Varchar(20) null
    ,[Trailer6Rego] Varchar(20) null
    ,[Trailer7Rego] Varchar(20) null
    ,[Trailer8Rego] Varchar(20) null
    ,[Trailer5PlateId] int null
    ,[Trailer6PlateId] int null
    ,[Trailer7PlateId] int null
    ,[Trailer8PlateId] int null;

GO

ALTER TABLE VehicleKeyLogs
ADD IsISO bit null,
IsVin bit null, 
IsTrailerRego bit null,
IsCarsStock bit null;
