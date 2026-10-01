CREATE TABLE [dbo].[TrainingTestQuestionNumbers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into TrainingTestQuestionNumbers(Name) values('01')
insert into TrainingTestQuestionNumbers(Name) values('02')
insert into TrainingTestQuestionNumbers(Name) values('03')
insert into TrainingTestQuestionNumbers(Name) values('04')
insert into TrainingTestQuestionNumbers(Name) values('05')
insert into TrainingTestQuestionNumbers(Name) values('06')
insert into TrainingTestQuestionNumbers(Name) values('07')
insert into TrainingTestQuestionNumbers(Name) values('08')
insert into TrainingTestQuestionNumbers(Name) values('09')
insert into TrainingTestQuestionNumbers(Name) values('10')
update TrainingTestQuestionNumbers set IsDeleted=0



