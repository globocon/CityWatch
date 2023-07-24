USE [CityWatchDb]
GO

CREATE TABLE [dbo].[ClientTypes](
	[Id] 	[int] 			IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Name] 	[varchar](75) 	NOT NULL
)
GO

CREATE TABLE [dbo].[ClientSites](
	[Id] 		[int] 			IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[TypeId] 	[int] 			NOT NULL FOREIGN KEY REFERENCES ClientTypes (Id) ON DELETE CASCADE,
	[Name] 		[varchar](75) 	NOT NULL
)
GO
