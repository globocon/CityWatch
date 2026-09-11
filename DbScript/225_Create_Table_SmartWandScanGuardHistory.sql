


CREATE TABLE [SmartWandScanGuardHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EmployeeId] [int] NULL,
	[EmployeeName] [nvarchar](max) NULL,
	[EmployeePhone] [nvarchar](50) NULL,
	[TemplateId] [int] NULL,
	[TemplateIdentificationNumber] [nvarchar](50) NULL,
	[TemplateName] [nvarchar](50) NULL,
	[ClientId] [int] NULL,
	[SiteId] [nvarchar](50) NULL,
	[SiteName] [nvarchar](50) NULL,
	[LocationId] [nvarchar](50) NULL,
	[LocationName] [nvarchar](50) NULL,
	[InspectionStartDatetimeLocal] [datetime] NULL,
	[InspectionEndDatetimeLocal] [datetime] NULL,
	[ClientSiteId] [int] NULL,
	[GuardId] [int] NULL,
	[SmartWandId] [nvarchar](50) NULL,
	[LocationScan] [nvarchar](500) NULL,
	[RecordCreateTime] [DateTime] NULL,
	[RecordLastUpdateTime] [DateTime] NULL,
 CONSTRAINT [PK_SmartWandScanGuardHistory] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
