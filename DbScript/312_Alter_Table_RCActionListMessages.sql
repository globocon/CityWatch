
alter table RCActionListMessages add Endmessagetime datetime
alter table RCActionListMessages add Radiofrequencystatus nvarchar(max)
update RCActionListMessages set Radiofrequencystatus='OnceOff'


CREATE TABLE RCActionListMessagesDailyLog
(
    Id INT IDENTITY PRIMARY KEY,
    RCActionListMessagesId INT NOT NULL,
    SentDate DATE NOT NULL
)