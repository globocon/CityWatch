


ALTER TABLE [ClientSiteRadioChecksActivityStatus]
ADD
GuardLoginTimeLocal datetime null,
GuardLoginTimeLocalWithOffset datetimeoffset(7) NULL,
GuardLoginTimeZone nvarchar(500) NULL,
GuardLoginTimeZoneShort nvarchar(20) NULL,
GuardLoginTimeUtcOffsetMinute int NULL,
GuardLoginTimeServerOffsetMinute int NULL default (datepart(tzoffset,sysdatetimeoffset()))

GO
