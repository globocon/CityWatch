ALTER TABLE [dbo].[ClientSiteKpiSettings]
ADD [PhotoPointsPerPatrol] INT NULL,
    [WandPointsPerPatrol] INT NULL,
    [PatrolsPerHour] INT NULL,
    [TuneDowngradeBuffer] DECIMAL(5, 2) NULL,	 
    [IsThermalCameraSite] BIT NOT NULL DEFAULT 0,
    [SiteImage] VARCHAR(1024) NULL