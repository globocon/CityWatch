CREATE TABLE [dbo].[TestQuestionSettings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CourseDurationId] [int] NULL,
	[TestDurationId] [int] NULL,
	[PassMarkId] [int] NULL,
	[AttemptsId] [int] NULL,
	[IsCertificateExpiry] [bit] not null default 0,
	[CertificateExpiryId] [int] null,
	[IsCertificateWithQAndADump] [bit] not null default 0,
	[IsCertificateHoldUntilPracticalTaken] [bit] not null default 0,
	[IsAnonymousFeedback][bit]not null default 0,
	[IsDeleted] [bit] not NULL default 0,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 
GO

alter table TestQuestionSettings add HRSettingsId int NULL

ALTER TABLE [dbo].[TestQuestionSettings]  WITH CHECK ADD FOREIGN KEY([HRSettingsId])
REFERENCES [dbo].[HrSettings] ([Id])