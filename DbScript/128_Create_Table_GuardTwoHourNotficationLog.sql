

CREATE TABLE GuardTwoHourNoActivityNotificationLog (
    Id INT IDENTITY PRIMARY KEY,
    GuardId INT NOT NULL,
    ClientSiteId INT NOT NULL,
    NotificationTime DATETIME NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);

