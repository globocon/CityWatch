CREATE TABLE [dbo].[KeyVehicleLogProfiles]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,	
	[VehicleRego] [varchar](20) NULL,
	[Trailer1Rego] [varchar](20) NULL,
	[Trailer2Rego] [varchar](20) NULL,
	[Trailer3Rego] [varchar](20) NULL,
	[Trailer4Rego] [varchar](20) NULL,
	[Plate] [varchar](10) NULL,	
	[TruckConfig] [int] NULL,
	[TrailerType] [int] NULL,
	[MaxWeight] [decimal](6, 2) NULL,
	[CompanyName] [varchar](100) NULL,
	[PersonName] [varchar](100) NULL,
	[PersonType] [int] NULL,
	[MobileNumber] [varchar](20) NULL,
	[Product] [varchar](256) NULL,	
	[EntryReason] [int] NULL,	
	[CreatedLogId] [int] NOT NULL
)
