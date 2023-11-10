CREATE TABLE [dbo].[RadioCheckDuress]
(
	[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	UserID INT,
	IsActive bit,
	CurrentDateTime datetime,
)