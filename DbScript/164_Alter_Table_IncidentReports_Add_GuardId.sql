ALTER TABLE [dbo].[IncidentReports] add GuardId int
ALTER TABLE [dbo].[IncidentReports]  WITH CHECK ADD FOREIGN KEY([GuardId])
REFERENCES [dbo].[Guards] ([Id])
