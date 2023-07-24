ALTER TABLE ClientTypes ADD CONSTRAINT UQ_Client_Type_Name UNIQUE ([Name])
GO

ALTER TABLE ClientSites ADD CONSTRAINT UQ_Client_Site_Name UNIQUE ([Name])
GO