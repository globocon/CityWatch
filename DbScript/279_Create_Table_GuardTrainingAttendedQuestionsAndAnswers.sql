USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardTrainingAndAssessment]    Script Date: 06-02-2025 11:24:59 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardTrainingAttendedQuestionsAndAnswers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	
	[TrainingCourseId] [int] NULL,
	[TrainingTestQuestionsId] [int] NULL,
	[TrainingTestQuestionsAnswersId] [int] NULL,
	[IsCorrect][bit] default 0,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO


