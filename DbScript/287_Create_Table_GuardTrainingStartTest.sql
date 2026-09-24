USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardTrainingAndAssessment]    Script Date: 14-02-2025 10:49:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardTrainingStartTest](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	
	[TrainingCourseId] [int] NULL,

	[ClassroomLocationId] [int] NULL,

) ON [PRIMARY] 
GO


alter table GuardTrainingStartTest add TestDate datetime

