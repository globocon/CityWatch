
Alter table [ClientSiteToggle]
Add IsISO bit null,
IsVin bit null, 
IsTrailerRego bit null,
IsCarsStock bit null;

GO

  INSERT INTO KeyVehcileLogFields
  VALUES (10,'Blue',0), (10,'Green',0), (10,'Red',0);