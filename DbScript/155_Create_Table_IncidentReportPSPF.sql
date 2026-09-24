CREATE TABLE IncidentReportPSPF (
    Id int IDENTITY(1,1) PRIMARY KEY,
    ReferenceNo varchar(50),
    Name varchar(255),
    IsDefault bit,
);

alter table IncidentReports add PSPFId int

ALTER TABLE [dbo].[IncidentReports]  WITH CHECK ADD FOREIGN KEY([PSPFId])
REFERENCES [dbo].[IncidentReportPSPF] ([Id])
ON DELETE SET NULL

