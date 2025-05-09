CREATE TABLE [dbo].[LanguageMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Language] [varchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
 
 
ALTER TABLE [dbo].[LanguageMaster]
ADD CONSTRAINT PK_LanguageMaster PRIMARY KEY (Id);

ALTER TABLE [dbo].[LanguageMaster]
ADD IsDeleted bit;
 
update LanguageMaster set IsDeleted=0
 
CREATE TABLE [dbo].[LanguageDetails] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [GuardID] INT,
	[LanguageID] INT,
	[CreatedDate] DateTime,
	[IsDeleted] bit,
	CONSTRAINT FK_LanguageDetails_Guards FOREIGN KEY ([GuardID]) REFERENCES [dbo].[Guards]([Id]),
    CONSTRAINT FK_LanguageDetails_LanguageMaster FOREIGN KEY ([LanguageID]) REFERENCES [dbo].[LanguageMaster]([Id])
);