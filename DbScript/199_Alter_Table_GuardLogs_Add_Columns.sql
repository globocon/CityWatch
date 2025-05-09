

ALTER TABLE GuardLogs
	ADD 
	 EventDateTimeServerOffsetMinute int NULL default (datepart(tzoffset,sysdatetimeoffset())),
	 EventDateTimeLocal datetime NULL,
	 EventDateTimeLocalWithOffset  datetimeoffset(7) NULL,
	 EventDateTimeZone nvarchar(500) NULL,
	 EventDateTimeZoneShort nvarchar(20) NULL,
	 EventDateTimeUtcOffsetMinute int NULL


	 GO

	