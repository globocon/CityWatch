select * from ClientSiteKpiSettings where ISNULL(TuneDowngradeBuffer, 0) = 0

Update ClientSiteKpiSettings set TuneDowngradeBuffer = 1 where ISNULL(TuneDowngradeBuffer, 0) = 0
GO;
