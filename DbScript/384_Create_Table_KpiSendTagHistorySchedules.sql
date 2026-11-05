/*
    Weekly Tag History email (Kpi > Settings > Schedules > Tag History).

    KpiSendTagHistorySchedules - one row per schedule. No site list: every run reports every
    site's changes in ClientSiteSmartWandTagsHist, so new sites are picked up automatically.
    RunDayOfWeek follows .NET DayOfWeek (Sunday = 0 ... Saturday = 6).

    KpiSendScheduleJobsTagHistory - one row per run (scheduler or Run Now): the week covered,
    the number of changes, whether the report was generated and whether the email was sent.
    A week with no changes is recorded with EmailSent = 0 and no email goes out.

    Safe to run more than once.
*/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.KpiSendTagHistorySchedules', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KpiSendTagHistorySchedules](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [StartDate] [datetime] NOT NULL,
        [EndDate] [datetime] NULL,
        [RunDayOfWeek] [int] NOT NULL,
        [Time] [nvarchar](10) NOT NULL,
        [EmailTo] [nvarchar](max) NULL,
        [EmailBcc] [nvarchar](max) NULL,
        [NextRunOn] [datetime] NOT NULL,
        [ProjectName] [nvarchar](100) NULL,
        [IsPaused] [bit] NOT NULL CONSTRAINT [DF_KpiSendTagHistorySchedules_IsPaused] DEFAULT (0),
     CONSTRAINT [PK_KpiSendTagHistorySchedules] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

IF OBJECT_ID('dbo.KpiSendScheduleJobsTagHistory', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KpiSendScheduleJobsTagHistory](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ScheduleId] [int] NULL,
        [CreatedDate] [datetime] NOT NULL,
        [CompletedDate] [datetime] NULL,
        [RunType] [nvarchar](20) NULL,
        [ReportFromDate] [datetime] NULL,
        [ReportToDate] [datetime] NULL,
        [RecordCount] [int] NOT NULL CONSTRAINT [DF_KpiSendScheduleJobsTagHistory_RecordCount] DEFAULT (0),
        [ReportGenerated] [bit] NOT NULL CONSTRAINT [DF_KpiSendScheduleJobsTagHistory_ReportGenerated] DEFAULT (0),
        [ReportStatus] [nvarchar](500) NULL,
        [EmailSent] [bit] NOT NULL CONSTRAINT [DF_KpiSendScheduleJobsTagHistory_EmailSent] DEFAULT (0),
        [EmailStatus] [nvarchar](1000) NULL,
        [EmailTo] [nvarchar](max) NULL,
        [Success] [bit] NULL,
        [StatusMessage] [nvarchar](max) NULL,
     CONSTRAINT [PK_KpiSendScheduleJobsTagHistory] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_KpiSendScheduleJobsTagHistory_ScheduleId' AND object_id = OBJECT_ID('dbo.KpiSendScheduleJobsTagHistory'))
    CREATE NONCLUSTERED INDEX [IX_KpiSendScheduleJobsTagHistory_ScheduleId]
        ON [dbo].[KpiSendScheduleJobsTagHistory] ([ScheduleId], [CreatedDate] DESC)
GO
