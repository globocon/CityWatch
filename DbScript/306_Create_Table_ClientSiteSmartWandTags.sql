USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[ClientSiteSmartWands]    Script Date: 28-07-2025 17:39:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ClientSiteSmartWandTags](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NOT NULL,
	[UId] [nvarchar](max) NOT NULL,
	[TagsTypeId] [int] NOT NULL,
	[LabelDescription] [nvarchar](max) NULL,
	[IsDeleted] [bit] default 0
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [dbo].[SmartWandTagsType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	
	[value] [nvarchar](max) NULL
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into SmartWandTagsType (value) values('Bluetooth')
insert into SmartWandTagsType (value) values('NFC')



