ALTER TABLE [dbo].[IncidentReports]
ADD IncidentDateTime dateTime NULL,	
	ReportDateTime dateTime NULL,	
	JobNumber varchar(15) NULL,
	JobTime char(5) NULL,	
	CallSign varchar(15) NULL,
	NotifiedBy varchar(25) NULL,
	Billing varchar(25) NULL