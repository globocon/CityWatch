
GO

/****** Object:  Table [dbo].[ClientSiteLinksDetails]    Script Date: 07-09-2023 12:00:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ClientSiteLinksDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteLinksTypeId] [int] NULL,
	[Title] [nvarchar](500) NULL,
	[Hyperlink] [nvarchar](max) NULL,
	[State] [nvarchar](10) NULL,
 CONSTRAINT [PK_ClientSiteLinksDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[ClientSiteLinksDetails]  WITH CHECK ADD  CONSTRAINT [FK_ClientSite_LinksDetails] FOREIGN KEY([ClientSiteLinksTypeId])
REFERENCES [dbo].[ClientSiteLinksPageType] ([Id])
GO

ALTER TABLE [dbo].[ClientSiteLinksDetails] CHECK CONSTRAINT [FK_ClientSite_LinksDetails]
GO


