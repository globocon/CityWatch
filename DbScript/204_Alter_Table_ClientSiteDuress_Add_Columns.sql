

ALTER TABLE [ClientSiteDuress]
ADD
EnabledDateTimeLocal datetime null,
EnabledDateTimeLocalWithOffset datetimeoffset(7) NULL,
EnabledDateTimeZone nvarchar(500) NULL,
EnabledDateTimeZoneShort nvarchar(20) NULL,
EnabledDateTimeUtcOffsetMinute int NULL,
EnabledDateTimeServerOffsetMinute int NULL default (datepart(tzoffset,sysdatetimeoffset()))

GO