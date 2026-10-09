CREATE TABLE DuressSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ClientSiteId INT NOT NULL,
    PositionFilter NVARCHAR(50) NOT NULL,
    SelectedPosition INT NOT NULL,
    SiteDuressNumber INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL
);

