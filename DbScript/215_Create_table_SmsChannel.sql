
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

INSERT INTO [SmsChannel] ([CompanyId],[SmsProvider],[ApiKey],[ApiSecret],[SmsSender])
     VALUES (1,'SMS Global','6ef008a5d1e5b18784237e608689e2ac','3a889ce06ce04646e928af123ca56f2d','CWS IR')
GO
