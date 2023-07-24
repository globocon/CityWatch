CREATE TABLE [dbo].[KeyVehcileLogFields]
(
	[Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[TypeId] [int] NOT NULL,
	[Name] [varchar](50) NOT NULL,
	IsDeleted BIT DEFAULT 0,
	CONSTRAINT UQ_KvlFields UNIQUE ([TypeId], [Name])
)