ALTER TABLE HrSettings
ADD HRLock bit DEFAULT(0)

UPDATE       HrSettings
SET                HRLock = 0