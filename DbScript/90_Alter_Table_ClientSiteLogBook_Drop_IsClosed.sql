ALTER TABLE [dbo].[ClientSiteLogBooks] 
DROP CONSTRAINT [DF__ClientSit__IsClo__7BE56230]
GO


ALTER TABLE dbo.ClientSiteLogBooks
DROP COLUMN [IsClosed]
GO