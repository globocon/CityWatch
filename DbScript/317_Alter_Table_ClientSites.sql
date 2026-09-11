
ALTER TABLE ClientSites
ADD UploadKVLog bit NOT NULL DEFAULT (0),
UploadSWLog  bit NOT NULL DEFAULT (0)

Update ClientSites set [UploadKVLog] = 1
where [UploadGuardLog] = 1