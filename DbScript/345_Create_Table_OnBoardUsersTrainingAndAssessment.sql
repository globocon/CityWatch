USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardTrainingAndAssessment]    Script Date: 15-05-2026 15:53:57 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[OnBoardUsersTrainingAndAssessment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[TrainingCourseStatusId] [int] NULL,
	[HRGroupId] [int] NULL,
	[TrainingCourseId] [int] NULL,
	[Attempts] [int] NULL
) ON [PRIMARY]
GO


GO


