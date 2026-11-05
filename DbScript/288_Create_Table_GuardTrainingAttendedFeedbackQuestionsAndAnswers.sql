USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardTrainingAttendedQuestionsAndAnswers]    Script Date: 26-02-2025 12:38:11 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardTrainingAttendedFeedbackQuestionsAndAnswers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	[HrSettingsId] [int] NULL,
	[TrainingTestFeedbackQuestionsId] [int] NULL,
	[TrainingTestFeedbackQuestionsAnswersId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO






