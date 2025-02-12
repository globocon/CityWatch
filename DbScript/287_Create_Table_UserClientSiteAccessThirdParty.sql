CREATE TABLE UserClientSiteAccessThirdparty (
    ID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserID int,
	ClientSiteID int
);

ALTER TABLE [dbo].[UserClientSiteAccessThirdparty]  WITH CHECK ADD FOREIGN KEY([ClientSiteId])
REFERENCES [dbo].[ClientSites] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserClientSiteAccessThirdparty]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
