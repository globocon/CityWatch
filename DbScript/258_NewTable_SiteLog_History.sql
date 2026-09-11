
/****** Object:  Table [dbo].[SiteLogUploadHistory]    Script Date: 24-09-2024 16:11:14 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SiteLogUploadHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[LogDeatils] [nvarchar](max) NULL,
	[Date] [datetime] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


