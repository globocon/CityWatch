CREATE TABLE [dbo].[CourseDuration](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into CourseDuration(Name) values('0')
insert into CourseDuration(Name) values('15')
insert into CourseDuration(Name) values('30')
insert into CourseDuration(Name) values('45')
insert into CourseDuration(Name) values('60')
insert into CourseDuration(Name) values('90')
insert into CourseDuration(Name) values('120')
insert into CourseDuration(Name) values('150')
insert into CourseDuration(Name) values('240')
