CREATE TABLE [dbo].[TestDuration](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into TestDuration(Name) values('0')
insert into TestDuration(Name) values('15')
insert into TestDuration(Name) values('30')
insert into TestDuration(Name) values('45')
insert into TestDuration(Name) values('60')
insert into TestDuration(Name) values('90')
insert into TestDuration(Name) values('120')
insert into TestDuration(Name) values('150')
insert into TestDuration(Name) values('240')
