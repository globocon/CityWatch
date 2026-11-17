USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[TrainingCourses]    Script Date: 21-01-2025 16:12:10 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TrainingCourseCertificate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FileName] [varchar](1024) NOT NULL,
	[LastUpdated] [datetime] NOT NULL,
	[HRSettingsId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[TrainingCourses]  WITH CHECK ADD FOREIGN KEY([HRSettingsId])
REFERENCES [dbo].[HrSettings] ([Id])
GO


GO





