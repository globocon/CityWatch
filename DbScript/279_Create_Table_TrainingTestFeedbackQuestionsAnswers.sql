CREATE TABLE [dbo].[TrainingTestFeedbackQuestionsAnswers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TrainingTestFeedbackQuestionsId] [int] NULL,
	
	[Options] [nvarchar](max) null,
	[IsDeleted] [bit] not NULL default 0,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO


ALTER TABLE [dbo].[TrainingTestFeedbackQuestionsAnswers]  WITH CHECK ADD FOREIGN KEY([TrainingTestFeedbackQuestionsId])
REFERENCES [dbo].[TrainingTestFeedbackQuestions] ([Id])

