/*
    Weekly and monthly log dumps that have been produced.

    The daily dump records that it is done by setting ClientSiteLogBooks.DbxUploaded on the log book
    it sent. A weekly or monthly dump covers many log books and produces one merged document, so it
    has nothing of its own to mark - and marking the days it covered would tell the daily run those
    days had already been sent. This table is the periodic equivalent: one row per site, per log
    type, per period, written once the dump has been produced and delivered.

    That row is also what stops a second run of the scheduler re-sending the same period, which is
    why the unique index below is on the period rather than on the file. FileName/FilePath record
    what was produced and where it was filed in Dropbox.
*/

CREATE TABLE [dbo].[ClientSitePeriodicLogUploads](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [ClientSiteId] [int] NOT NULL,

    -- 1 = Weekly, 2 = Monthly. Matches CityWatch.Data.Models.LogDumpPeriodType.
    [PeriodType] [int] NOT NULL,

    -- 1 = Daily Guard Log, 2 = Key & Vehicle Log, 3 = Fusion Log, 4 = Smart Wand Log.
    -- Matches CityWatch.Data.Models.LogBookType.
    [LogBookType] [int] NOT NULL,

    -- Monday and Sunday of the week, or the first and last day of the month.
    [PeriodStartDate] [date] NOT NULL,
    [PeriodEndDate] [date] NOT NULL,

    [UploadedOn] [datetime] NOT NULL,

    [FileName] [nvarchar](500) NULL,

    -- Full Dropbox path the document was filed under.
    [FilePath] [nvarchar](1000) NULL,

    -- How many days of the period actually produced pages; a short week is visible here.
    [DaysIncluded] [int] NOT NULL,

    -- The document was produced either way; this says whether Dropbox accepted it.
    [DropboxUploaded] [bit] NOT NULL,

    [EmailedTo] [nvarchar](2000) NULL,

 CONSTRAINT [PK_ClientSitePeriodicLogUploads] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ClientSitePeriodicLogUploads] ADD CONSTRAINT [DF_ClientSitePeriodicLogUploads_UploadedOn] DEFAULT (getdate()) FOR [UploadedOn]
GO

ALTER TABLE [dbo].[ClientSitePeriodicLogUploads] ADD CONSTRAINT [DF_ClientSitePeriodicLogUploads_DaysIncluded] DEFAULT ((0)) FOR [DaysIncluded]
GO

ALTER TABLE [dbo].[ClientSitePeriodicLogUploads] ADD CONSTRAINT [DF_ClientSitePeriodicLogUploads_DropboxUploaded] DEFAULT ((0)) FOR [DropboxUploaded]
GO

ALTER TABLE [dbo].[ClientSitePeriodicLogUploads] WITH CHECK ADD CONSTRAINT [FK_ClientSitePeriodicLogUploads_ClientSites] FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id])
GO

/*
    One dump per site, per log type, per period. The scheduler checks for the row before it does any
    work; this index is the backstop that keeps two runs firing at once from both inserting.
*/
CREATE UNIQUE NONCLUSTERED INDEX [UQ_ClientSitePeriodicLogUploads_Period]
ON [dbo].[ClientSitePeriodicLogUploads]([ClientSiteId] ASC, [PeriodType] ASC, [LogBookType] ASC, [PeriodStartDate] ASC)
GO

-- The scheduler's own lookup: "has this period already been done?"
CREATE NONCLUSTERED INDEX [IX_ClientSitePeriodicLogUploads_PeriodLookup]
ON [dbo].[ClientSitePeriodicLogUploads]([PeriodType] ASC, [PeriodStartDate] ASC)
GO
