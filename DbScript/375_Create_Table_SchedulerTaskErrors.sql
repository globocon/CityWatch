/*
    Failures from the scheduled tasks, weekly and monthly log dumps to begin with.

    SiteLogUploadHistory already exists, but it is a single free-text column that the daily run
    writes a running commentary into - successes, progress and failures all in the same stream, with
    no site id, no period and no way to query "what failed last Monday". This table records only
    failures, in columns, so a failed site/log type/period can be found without reading prose.

    Deliberately no foreign key on ClientSiteId: this is a log, and it must never be the reason a
    site cannot be removed. ClientSiteId is null when the failure was not specific to one site -
    the run failing to start, for instance.
*/

CREATE TABLE [dbo].[SchedulerTaskErrors](
    [Id] [int] IDENTITY(1,1) NOT NULL,

    -- e.g. 'Weekly Log Dump', 'Monthly Log Dump'.
    [TaskName] [nvarchar](200) NOT NULL,

    -- 1 = Weekly, 2 = Monthly. Null when the failure is not tied to a period.
    [PeriodType] [int] NULL,

    [ClientSiteId] [int] NULL,

    -- Matches CityWatch.Data.Models.LogBookType; null when not tied to one log type.
    [LogBookType] [int] NULL,

    [PeriodStartDate] [date] NULL,
    [PeriodEndDate] [date] NULL,

    [OccurredOn] [datetime] NOT NULL,

    [ErrorMessage] [nvarchar](4000) NULL,

    -- Stack trace / inner exceptions.
    [ErrorDetails] [nvarchar](max) NULL,

 CONSTRAINT [PK_SchedulerTaskErrors] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[SchedulerTaskErrors] ADD CONSTRAINT [DF_SchedulerTaskErrors_OccurredOn] DEFAULT (getdate()) FOR [OccurredOn]
GO

-- "What failed, most recent first" - the way this table is actually read.
CREATE NONCLUSTERED INDEX [IX_SchedulerTaskErrors_OccurredOn]
ON [dbo].[SchedulerTaskErrors]([OccurredOn] DESC, [TaskName] ASC)
GO
