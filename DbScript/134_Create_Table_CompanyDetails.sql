
/****** Object:  Table [dbo].[CompanyDetails]    Script Date: 03-08-2023 03:04:26 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[CompanyDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Domain] [nvarchar](max) NULL,
	[LastUploaded] [datetime] NULL,
	[PrimaryLogoUploadedOn] [datetime] NULL,
	[PrimaryLogoPath] [nvarchar](max) NULL,
	[HomePageMessage] [nvarchar](max) NULL,
	[MessageBarColour] [nvarchar](max) NULL,
	[HomePageMessageUploadedOn] [datetime] NULL,
	[BannerMessage] [nvarchar](max) NULL,
	[BannerLogoPath] [nvarchar](max) NULL,
	[Hyperlink] [nvarchar](max) NULL,
	[BannerMessageUploadedOn] [datetime] NULL,
	[EmailMessage] [nvarchar](max) NULL,
	[EmailMessageUploadedOn] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


