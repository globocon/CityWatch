
CREATE TABLE [MobileLogActivityProfile](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProfileName] [nvarchar](250) NOT NULL
) ON [PRIMARY]
GO


Alter Table [DuressAppField]
Add ProfileId int NULL

Alter Table [DuressSettings]
Add LogProfileId int NULL