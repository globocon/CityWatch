CREATE TABLE [dbo].[FeedbackType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) 

SET IDENTITY_INSERT [dbo].[FeedbackType] ON 
GO
INSERT [dbo].[FeedbackType] ([Id], [Name]) VALUES (1, N'General')
GO
INSERT [dbo].[FeedbackType] ([Id], [Name]) VALUES (2, N'Patrol Car')
GO
INSERT [dbo].[FeedbackType] ([Id], [Name]) VALUES (3, N'Colour Codes')
GO
SET IDENTITY_INSERT [dbo].[FeedbackType] OFF
GO

truncate table FeedbackType