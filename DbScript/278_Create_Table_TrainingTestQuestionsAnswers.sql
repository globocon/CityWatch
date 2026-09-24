CREATE TABLE [dbo].[TrainingTestQuestionsAnswers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TrainingTestQuestionsId] [int] NULL,
	
	[Options] [nvarchar](max) null,
	[IsAnswer] [bit] not NULL default 0,
	[IsDeleted] [bit] not NULL default 0,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO


ALTER TABLE [dbo].[TrainingTestQuestionsAnswers]  WITH CHECK ADD FOREIGN KEY([TrainingTestQuestionsId])
REFERENCES [dbo].[TrainingTestQuestions] ([Id])

