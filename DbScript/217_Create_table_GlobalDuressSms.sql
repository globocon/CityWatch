
CREATE TABLE [GlobalDuressSms](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CompanyId] [int] NOT NULL,
	[SmsNumber] [varchar](25) NOT NULL,
	[RecordCreateDTM] [datetime] NOT NULL,
	[RecordCreateUserId] [int] NOT NULL,
 CONSTRAINT [PK_GlobalDuressSms] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [GlobalDuressSms] ADD  CONSTRAINT [DF_GlobalDuressSms_RecordCreateDTM]  DEFAULT (getdate()) FOR [RecordCreateDTM]
GO


