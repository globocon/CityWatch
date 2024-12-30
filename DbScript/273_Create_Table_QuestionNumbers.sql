CREATE TABLE [dbo].[QuestionNumbers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into QuestionNumbers(Name) values('01')
insert into QuestionNumbers(Name) values('02')
insert into QuestionNumbers(Name) values('03')
insert into QuestionNumbers(Name) values('04')
insert into QuestionNumbers(Name) values('05')
insert into QuestionNumbers(Name) values('06')
insert into QuestionNumbers(Name) values('07')
insert into QuestionNumbers(Name) values('08')
insert into QuestionNumbers(Name) values('09')
insert into QuestionNumbers(Name) values('10')