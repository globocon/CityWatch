USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardTrainingAndAssessment]    Script Date: 11-02-2025 12:51:59 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardTrainingAndAssessmentScore](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	[TrainingCourseId] [int] NULL,
	[TotalQuestions] [int] NULL,
	[guardCorrectQuestionsCount] [int] NULL,
	[guardScore] [nvarchar](max) null,
	[IsPass] [bit] default 0,
	[duration] [nvarchar](max) null,
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[GuardTrainingAndAssessment]  WITH CHECK ADD FOREIGN KEY([GuardId])
REFERENCES [dbo].[Guards] ([Id])
GO





GO

ALTER TABLE [dbo].[GuardTrainingAndAssessment]  WITH CHECK ADD FOREIGN KEY([TrainingCourseId])
REFERENCES [dbo].[TrainingCourses] ([Id])
GO




