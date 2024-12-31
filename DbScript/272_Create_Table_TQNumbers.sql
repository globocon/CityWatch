USE [CityWatchDb]
GO

/****** Object:  Table [dbo].[ReferenceNoNumbers]    Script Date: 23-12-2024 17:56:12 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TQNumbers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

insert into TQNumbers(Name) values('01')
insert into TQNumbers(Name) values('02')
insert into TQNumbers(Name) values('03')
insert into TQNumbers(Name) values('04')
insert into TQNumbers(Name) values('05')
insert into TQNumbers(Name) values('06')
insert into TQNumbers(Name) values('07')
insert into TQNumbers(Name) values('08')
insert into TQNumbers(Name) values('09')
insert into TQNumbers(Name) values('10')

update TQNumbers set IsDeleted=0