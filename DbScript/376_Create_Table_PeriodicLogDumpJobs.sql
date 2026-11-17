/*
    One row per execution of the weekly or monthly log dump.

    Same shape and lifecycle as KpiSendScheduleJobs, which does this for the KPI schedule sender:
    a row is inserted when the run starts and completed when it finishes, so an in-progress run is
    one with a null CompletedDate. Deliberately a separate table rather than reusing that one -
    KpiReportController.Send reads KpiSendScheduleJobs whole, with no task discriminator, and
    RemoveAllKpiSendScheduleJobsOldNotComplete deletes any incomplete row it finds. A log dump
    writing there would read to the KPI sender as a KPI job in progress, and have its own row
    deleted out from under it.

    This sits alongside the other two periodic tables rather than replacing either:
      - ClientSitePeriodicLogUploads is per site and log type - what was produced,
      - SchedulerTaskErrors is per failure - what went wrong,
      - this is per run - when it started, when it finished, and how it went overall.
*/

-- The filtered index below requires both of these; sqlcmd does not set them by default.
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PeriodicLogDumpJobs](
    [Id] [int] IDENTITY(1,1) NOT NULL,

    -- 'Weekly Log Dump' or 'Monthly Log Dump'. The in-progress guard is per task name, so the
    -- weekly and monthly runs never block one another.
    [TaskName] [nvarchar](200) NOT NULL,

    -- 1 = Weekly, 2 = Monthly. Matches CityWatch.Data.Models.LogDumpPeriodType.
    [PeriodType] [int] NOT NULL,

    [PeriodStartDate] [date] NOT NULL,
    [PeriodEndDate] [date] NOT NULL,

    [CreatedDate] [datetime] NOT NULL,

    -- Null while the run is in progress. This is what the guard tests.
    [CompletedDate] [datetime] NULL,

    [Success] [bit] NULL,

    [StatusMessage] [nvarchar](max) NULL,

    -- Run totals, so "the weekly run did 11 sites" is a query rather than string parsing.
    [DumpsProduced] [int] NOT NULL,
    [DumpsSkipped] [int] NOT NULL,
    [DumpsFailed] [int] NOT NULL,

 CONSTRAINT [PK_PeriodicLogDumpJobs] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PeriodicLogDumpJobs] ADD CONSTRAINT [DF_PeriodicLogDumpJobs_CreatedDate] DEFAULT (getdate()) FOR [CreatedDate]
GO

ALTER TABLE [dbo].[PeriodicLogDumpJobs] ADD CONSTRAINT [DF_PeriodicLogDumpJobs_DumpsProduced] DEFAULT ((0)) FOR [DumpsProduced]
GO

ALTER TABLE [dbo].[PeriodicLogDumpJobs] ADD CONSTRAINT [DF_PeriodicLogDumpJobs_DumpsSkipped] DEFAULT ((0)) FOR [DumpsSkipped]
GO

ALTER TABLE [dbo].[PeriodicLogDumpJobs] ADD CONSTRAINT [DF_PeriodicLogDumpJobs_DumpsFailed] DEFAULT ((0)) FOR [DumpsFailed]
GO

/*
    The guard's lookup: the in-progress row for one task. Filtered, because the rows that matter are
    the handful with no completion date - not the row per week accumulating behind them.
*/
CREATE NONCLUSTERED INDEX [IX_PeriodicLogDumpJobs_InProgress]
ON [dbo].[PeriodicLogDumpJobs]([TaskName] ASC, [CreatedDate] ASC)
WHERE [CompletedDate] IS NULL
GO

-- "How did the last few runs go", which is how this table is actually read.
CREATE NONCLUSTERED INDEX [IX_PeriodicLogDumpJobs_History]
ON [dbo].[PeriodicLogDumpJobs]([TaskName] ASC, [CreatedDate] DESC)
GO
