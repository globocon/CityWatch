CREATE TABLE KPITelematicsField (
    ID int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name varchar(max),
	Mobile varchar(max),
	Email varchar(max),
	TypeId int
);