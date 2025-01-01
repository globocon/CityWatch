CREATE TABLE [dbo].[TrainingCertificateExpiryYears](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[IsDeleted] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
insert into TrainingCertificateExpiryYears(Name) values('1 year')
insert into TrainingCertificateExpiryYears(Name) values('2 years')
insert into TrainingCertificateExpiryYears(Name) values('3 years')
update TrainingCertificateExpiryYears set IsDeleted=0

