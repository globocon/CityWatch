USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[GuardTrainingAndAssessment]    Script Date: 03-03-2025 18:20:52 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardTrainingAndAssessmentPractical](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	
	[HRSettingsId] [int] NULL,
	[PracticalocationlId] [int] NULL,
	[PracticalDate] [datetime] NULL,
	[InstructorId] [int] NULL,
) ON [PRIMARY] 
GO


GO


