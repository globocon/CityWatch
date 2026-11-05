
Alter table [ClientSiteToggle]
Add IsISO bit null,
IsVin bit null, 
IsTrailerRego bit null,
IsCarsStock bit null;

GO

INSERT INTO KeyVehcileLogFields
VALUES (10,'Blue',0), (10,'Green',0), (10,'Red',0),(10,'Black',0), (10,'Silver',0), (10,'Olive',0),(10,'Gold',0), (10,'Yellow',0), (10,'White',0);

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


INSERT INTO KeyVehcileLogFields VALUES
(11,'Toyota',0),
(11,'Volkswagen',0),
(11,'Ford',0),
(11,'Honda',0),
(11,'Hyundai',0),
(11,'Nissan',0),
(11,'Chevrolet',0),
(11,'Kia',0),
(11,'Mercedes-Benz',0),
(11,'BMW',0),
(11,'Audi',0),
(11,'Lexus',0),
(11,'Subaru',0),
(11,'Mazda',0),
(11,'Tesla',0),
(11,'Jeep',0),
(11,'Volvo',0),
(11,'Porsche',0),
(11,'Land Rover',0),
(11,'Jaguar',0),
(11,'Renault',0),
(11,'Peugeot',0),
(11,'Skoda',0),
(11,'SEAT',0),
(11,'Fiat',0),
(11,'Citroen',0),
(11,'Suzuki',0),
(11,'Mitsubishi',0),
(11,'Isuzu',0),
(11,'MG',0),
(11,'Chery',0),
(11,'BYD',0),
(11,'Geely',0),
(11,'Great Wall',0),
(11,'Haval',0),
(11,'Ram',0),
(11,'GMC',0),
(11,'Cadillac',0),
(11,'Buick',0),
(11,'Acura',0),
(11,'Infiniti',0),
(11,'Genesis',0),
(11,'Mini',0),
(11,'Alfa Romeo',0),
(11,'Maserati',0),
(11,'Bentley',0),
(11,'Rolls-Royce',0),
(11,'Aston Martin',0),
(11,'Ferrari',0),
(11,'Lamborghini',0),
(11,'Changan',0),
(11,'SAIC Motor',0),
(11,'Wuling',0),
(11,'GAC Aion',0),
(11,'Jetour',0),
(11,'Li Auto',0),
(11,'Leapmotor',0),
(11,'XPeng',0),
(11,'NIO',0),
(11,'Zeekr',0),
(11,'Deepal',0),
(11,'Avatr',0),
(11,'Hongqi',0),
(11,'Roewe',0),
(11,'Baojun',0),
(11,'Bestune',0),
(11,'Dongfeng',0),
(11,'Voyah',0),
(11,'Forthing',0),
(11,'Trumpchi',0),
(11,'JAC',0),
(11,'JMC',0),
(11,'Maxus',0),
(11,'Tank',0),
(11,'Ora',0),
(11,'Lynk & Co',0),
(11,'Galaxy',0),
(11,'iCAR',0),
(11,'Luxeed',0),
(11,'Arcfox',0),
(11,'Aito',0),
(11,'Onvo',0),
(11,'Firefly',0),
(11,'Yangwang',0),
(11,'Fangchengbao',0),
(11,'Denza',0),
(11,'IM Motors',0),
(11,'Exeed',0),
(11,'Venucia',0),
(11,'Nami',0),
(11,'Yudo',0),
(11,'BAW',0),
(11,'Seres',0),
(11,'Polestones',0),
(11,'Changhe',0),
(11,'Haima',0),
(11,'Soueast',0),
(11,'Cowin',0),
(11,'Landwind',0),
(11,'Dayun',0);

ALTER TABLE [VehicleKeyLogs]
ADD HasLoadVariation bit Default 0 Not Null,
IsLoadVariationDuplicate bit Default 0 Not Null,
CopiedFromKVLogId int Null
