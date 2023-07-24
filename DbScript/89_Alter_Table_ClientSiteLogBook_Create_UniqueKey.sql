-- SELECT date, clientsiteid, count(id) FROM ClientSiteLogBooks group by date,clientsiteid having count(id) > 1

ALTER TABLE dbo.ClientSiteLogBooks
ADD CONSTRAINT UQ_Site_Type_Date UNIQUE (ClientSiteId, [Type], [Date])