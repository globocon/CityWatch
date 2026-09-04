CREATE TABLE HandoverNotes (
    ID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Notes varchar(max),
	ClientSiteID int
);