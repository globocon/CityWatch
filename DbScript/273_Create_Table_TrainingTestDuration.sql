CREATE TABLE [dbo].[TrainingTestDuration](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into TrainingTestDuration(Name) values('0 minutes')
insert into TrainingTestDuration(Name) values('15 minutes')
insert into TrainingTestDuration(Name) values('30 minutes')
insert into TrainingTestDuration(Name) values('45 minutes')
insert into TrainingTestDuration(Name) values('60 minutes')
insert into TrainingTestDuration(Name) values('90 minutes')
insert into TrainingTestDuration(Name) values('120 minutes')
insert into TrainingTestDuration(Name) values('150 minutes')
insert into TrainingTestDuration(Name) values('240 minutes')


update TrainingTestDuration set IsDeleted=0

