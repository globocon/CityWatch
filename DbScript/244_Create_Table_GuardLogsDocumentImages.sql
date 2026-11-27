
create table GuardLogsDocumentImages(Id int primary key identity(1,1), ImagePath nvarchar(max),GuardLogId int)
ALTER TABLE [dbo].[GuardLogsDocumentImages]  WITH CHECK ADD FOREIGN KEY([GuardLogId])
REFERENCES [dbo].[GuardLogs] ([Id])
ON DELETE CASCADE
GO

Alter table GuardLogsDocumentImages add IsRearfile bit default 0
Alter table GuardLogsDocumentImages add IsTwentyfivePercentfile bit default 0