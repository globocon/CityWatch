

CREATE TABLE [dbo].[TrainingCourseStatus](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ReferenceNo] [nvarchar](max) NULL,
	[Name] [nvarchar](max) NULL,
	[TrainingCourseStatusColorId] [int] NULL
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[TrainingCourseStatus]  WITH CHECK ADD FOREIGN KEY([TrainingCourseStatusColorId])
REFERENCES [dbo].[TrainingCourseStatusColor] ([Id])
GO


insert into TrainingCourseStatus(ReferenceNo,Name,TrainingCourseStatusColorId)values('01','Assigned',1)
insert into TrainingCourseStatus(ReferenceNo,Name,TrainingCourseStatusColorId)values('02','Progress',2)
insert into TrainingCourseStatus(ReferenceNo,Name,TrainingCourseStatusColorId)values('03','Certificate Hold',3)
