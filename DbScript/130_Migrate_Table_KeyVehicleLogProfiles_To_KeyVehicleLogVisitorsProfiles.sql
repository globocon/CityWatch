
INSERT INTO  dbo.KeyVehicleLogVisitorProfiles
(
	[VehicleRego],
	[Trailer1Rego] ,
	[Trailer2Rego] ,
	[Trailer3Rego],
	[Trailer4Rego],
	[Plate],
	[TruckConfig],
	[TrailerType],
	[MaxWeight],
	[MobileNumber],
	[Product],
	[EntryReason],
	[CreatedLogId],
	[PlateId],
	[IsSender],
	[Sender] 
)
SELECT
	[VehicleRego],
	[Trailer1Rego] ,
	[Trailer2Rego] ,
	[Trailer3Rego],
	[Trailer4Rego],
	[Plate],
	[TruckConfig],
	[TrailerType],
	[MaxWeight],
	[MobileNumber],
	[Product],
	[EntryReason],
	[CreatedLogId],
	[PlateId],
	[IsSender],
	[Sender] 
FROM dbo.KeyVehicleLogProfiles 
WHERE Id IN (SELECT MAX(Id) FROM KeyVehicleLogProfiles GROUP BY VehicleRego)

INSERT INTO dbo.KeyVehicleLogVisitorPersonalDetails 
(
	CompanyName, PersonName, PersonType, ProfileId
)
SELECT CompanyName, PersonName, PersonType, vp.Id
FROM dbo.KeyVehicleLogProfiles p
JOIN dbo.KeyVehicleLogVisitorProfiles vp
ON  vp.VehicleRego = p.VehicleRego