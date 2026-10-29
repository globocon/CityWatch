Alter Table [IncidentReportFields]
Add StampRcLogbook bit null default 0

GO
update [IncidentReportFields] set StampRcLogbook = 0
where [TypeId] = 2

GO

Alter Table [GuardLogs]
Add RcLogbookStamp bit null default 0

GO

update [GuardLogs] set RcLogbookStamp = 0

GO