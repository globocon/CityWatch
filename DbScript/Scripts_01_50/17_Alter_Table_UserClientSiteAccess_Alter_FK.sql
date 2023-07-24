ALTER TABLE [dbo].[UserClientSiteAccess] DROP CONSTRAINT [FK__UserClien__Clien__17036CC0]
GO

ALTER TABLE [dbo].[UserClientSiteAccess]  WITH CHECK ADD FOREIGN KEY([ClientSiteId]) 
REFERENCES [dbo].[ClientSites] ([Id]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserClientSiteAccess] DROP CONSTRAINT [FK__UserClien__UserI__160F4887]
GO

ALTER TABLE [dbo].[UserClientSiteAccess]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
GO



