
CREATE TABLE [MobileAppUpgrade](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AppType] [nvarchar](250) NOT NULL,
	[AppVersionMajor] [int] NOT NULL,
	[AppVersionMinor] [int] NOT NULL,
	[AppVersionPatch] [int] NOT NULL,
	[AppDownloadUrl] [nvarchar](max) NULL,
	[AppVersionNotes] [nvarchar](max) NULL,
	[RecordCreateDTM] [datetime] NOT NULL DEFAULT (getdate()) ,
	[TotalDownloadCount] [int] NOT NULL DEFAULT ((0)),
	[IsActive] [bit] Not Null DEFAULT 0,
	[FileName] [nvarchar](max)  NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO




