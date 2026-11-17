

/****** Object:  Table [dbo].[GuardComplianceLicense]    Script Date: 30-12-2024 12:04:34 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[GuardTrainingAndAssessment](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GuardId] [int] NULL,
	[HrGroup] [int] NULL,
	[Description] [varchar](max) NULL,
	[CourseId] [int] NULL
	
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO




