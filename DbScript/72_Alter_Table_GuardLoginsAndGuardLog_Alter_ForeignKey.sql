ALTER TABLE [dbo].[GuardLogins]  
DROP CONSTRAINT FK__GuardLogi__Guard__2D12A970
GO

ALTER TABLE [dbo].[GuardLogins]  
ADD FOREIGN KEY([GuardId]) REFERENCES [dbo].[Guards] ([Id]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[GuardLogins]  
DROP CONSTRAINT FK__GuardLogi__Smart__30E33A54
GO

ALTER TABLE [dbo].[GuardLogins]  
ADD FOREIGN KEY([SmartWandId]) REFERENCES [dbo].[ClientSiteSmartWands] ([Id]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[GuardLogs]
DROP CONSTRAINT FK__GuardLogs__Guard__35A7EF71
GO