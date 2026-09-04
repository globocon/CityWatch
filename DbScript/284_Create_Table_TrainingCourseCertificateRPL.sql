
CREATE TABLE [dbo].[TrainingCourseCertificateRPL](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TrainingPracticalLocationId] [int] NULL,
	[AssessmentStartDate] [datetime]  NULL,
	[AssessmentEndDate] [datetime]  NULL,
	[TrainingCourseCertificateId] [int] NULL,
	[TrainingInstructorId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


