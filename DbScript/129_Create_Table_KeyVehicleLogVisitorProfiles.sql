CREATE TABLE [dbo].[KeyVehicleLogVisitorProfiles]
(
	[Id] [int] PRIMARY KEY IDENTITY(1,1) NOT NULL,
	[VehicleRego] [varchar](50) NOT NULL,
	[Trailer1Rego] [varchar](20) NULL,
	[Trailer2Rego] [varchar](20) NULL,
	[Trailer3Rego] [varchar](20) NULL,
	[Trailer4Rego] [varchar](20) NULL,
	[Plate] [varchar](10) NULL,
	[TruckConfig] [int] NULL,
	[TrailerType] [int] NULL,
	[MaxWeight] [decimal](6, 2) NULL,
	[MobileNumber] [varchar](20) NULL,
	[Product] [varchar](256) NULL,
	[EntryReason] [int] NULL,
	[CreatedLogId] [int] NOT NULL,
	[PlateId] [int] NULL,
	[IsSender] [bit] NOT NULL DEFAULT 1,
	[Sender] [varchar](100) NULL,
	[Notes] [varchar](4096) NULL
)
GO

CREATE TABLE [dbo].[KeyVehicleLogVisitorPersonalDetails]
(
	[Id] [int] PRIMARY KEY IDENTITY(1,1) NOT NULL,
	[ProfileId] [INT] NOT NULL FOREIGN KEY REFERENCES [KeyVehicleLogVisitorProfiles] ([Id]),
	[CompanyName] [varchar](100) NULL,
	[PersonName] [varchar](100) NULL,
	[PersonType] [int] NULL,
)
GO


