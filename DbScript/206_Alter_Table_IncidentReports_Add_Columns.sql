

ALTER TABLE [IncidentReports]
ADD
CreatedOnDateTimeLocal datetime null,
CreatedOnDateTimeLocalWithOffset datetimeoffset(7) NULL,
CreatedOnDateTimeZone nvarchar(500) NULL,
CreatedOnDateTimeZoneShort nvarchar(20) NULL,
CreatedOnDateTimeUtcOffsetMinute int NULL,
CreatedOnDateTimeServerOffsetMinute int NULL default (datepart(tzoffset,sysdatetimeoffset()))
