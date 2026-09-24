CREATE TABLE [dbo].[TrainingTestFeedbackQuestions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[HRSettingsId] [int] NULL,
	[QuestionNoId] [int] NULL,
	[Question] [nvarchar](max) null,
	[IsDeleted] [bit] not NULL default 0,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO


ALTER TABLE [dbo].[TrainingTestFeedbackQuestions]  WITH CHECK ADD FOREIGN KEY([HRSettingsId])
REFERENCES [dbo].[HrSettings] ([Id])
ALTER TABLE [dbo].[TrainingTestFeedbackQuestions]  WITH CHECK ADD FOREIGN KEY([QuestionNoId])
REFERENCES [dbo].[TrainingTestQuestionNumbers] ([Id])


