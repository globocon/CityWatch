
CREATE TABLE [SmsChannel](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[SmsProvider] [nvarchar](200) NOT NULL,
	[ApiKey] [nvarchar](500) NULL,
	[ApiSecret] [nvarchar](500) NULL,
	[SmsSender] [varchar](11) NULL
) ON [PRIMARY]
GO

ALTER TABLE [SmsChannel]  WITH CHECK ADD  CONSTRAINT [FK_SmsChannel_CompanyDetails] FOREIGN KEY([CompanyId])
REFERENCES [CompanyDetails] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [SmsChannel] CHECK CONSTRAINT [FK_SmsChannel_CompanyDetails]
GO

