ALTER TABLE ClientSites ADD 

UploadGuardWeeklyLog bit NOT NULL default 0,
UploadFusionWeeklyLog bit NOT NULL default 0,
UploadKVWeeklyLog bit NOT NULL default 0,
UploadSWWeeklyLog bit NOT NULL default 0,
GuardLogEmailWeeklyLogTo varchar(5000) NULL,

UploadGuardMonthlyLog bit NOT NULL default 0,
UploadFusionMonthlyLog bit NOT NULL default 0,
UploadKVMonthlyLog bit NOT NULL default 0,
UploadSWMonthlyLog bit NOT NULL default 0,
GuardLogEmailMonthlyLogTo varchar(5000) NULL

