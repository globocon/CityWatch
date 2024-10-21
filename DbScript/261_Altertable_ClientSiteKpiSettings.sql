ALTER TABLE ClientSiteKpiSettings
ADD TimezoneString varchar(max);

ALTER TABLE ClientSiteKpiSettings
ADD UTC varchar(500);

ALTER TABLE ClientSiteRadioChecksActivityStatus
ADD UTCOffset varchar(500);
UPDATE       ClientSiteKpiSettings
SET                TimezoneString ='AUS Eastern Standard Time', UTC ='10:00:00'
WHERE        (TimezoneString IS NULL)

--select TimezoneString, UTC from ClientSiteKpiSettings where TimezoneString is not null

--select * from [dbo].[IncidentReports]

--select * from ClientSiteManningKpiSettings

--select * from ClientSiteRadioChecksActivityStatus
