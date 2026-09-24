CREATE TABLE [dbo].[GuardUnavailabilities](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NOT NULL,
	[Reason] [nvarchar](max) NULL,
	[FromDate] [datetime2](7) NOT NULL,
	[ToDate] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_GuardUnavailabilities] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[GuardUnavailabilities]  WITH CHECK ADD  CONSTRAINT [FK_GuardUnavailabilities_Guards_GuardId] FOREIGN KEY([GuardId])
REFERENCES [dbo].[Guards] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[GuardUnavailabilities] CHECK CONSTRAINT [FK_GuardUnavailabilities_Guards_GuardId]
GO
