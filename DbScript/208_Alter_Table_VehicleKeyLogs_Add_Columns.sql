
ALTER TABLE [VehicleKeyLogs]
ADD
EntryCreatedDateTimeLocal datetime null,
EntryCreatedDateTimeLocalWithOffset datetimeoffset(7) NULL,
EntryCreatedDateTimeZone nvarchar(500) NULL,
EntryCreatedDateTimeZoneShort nvarchar(20) NULL,
EntryCreatedDateTimeUtcOffsetMinute int NULL,
EntryCreatedDateTimeServer datetime null default (getdate()),
EntryCreatedDateTimeServerOffsetMinute int NULL default (datepart(tzoffset,sysdatetimeoffset()))
GO