
CREATE TABLE [KpiSendCustomWandSchedules](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClientSiteId] [int] NULL,
	[StartDate] [date] NOT NULL,
	[EndDate] [date] NULL,
	[Frequency] [int] NOT NULL,
	[CustomWandReportType] [int] NOT NULL,
	[Time] [char](5) NULL,
	[EmailTo] [varchar](5000) NULL,
	[NextRunOn] [datetime] NOT NULL,
	[ProjectName] [varchar](50) NULL,
	[EmailBcc] [varchar](5000) NULL,
) ;

CREATE TABLE [KpiSendCustomWandClientSites](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomWandScheduleId] [int] NOT NULL,
	[ClientSiteId] [int] NOT NULL
) ;

CREATE TABLE [KpiSendScheduleJobsCustomWand](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[CompletedDate] [datetime] NULL,
	[Success] [bit] NULL,
	[StatusMessage] [varchar](max) NULL,
);