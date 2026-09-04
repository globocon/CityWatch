alter table ReportTemplates add DefaultEmail varchar(max)

UPDATE  ReportTemplates set DefaultEmail='cws-ir@citywatchsecurity.com.au'