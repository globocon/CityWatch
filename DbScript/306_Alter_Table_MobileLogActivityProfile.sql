
ALTER TABLE MobileLogActivityProfile
ADD IsDefault bit Not Null default 0

Update MobileLogActivityProfile
Set IsDefault = 1
Where [ProfileName] = 'Security Guard (Default)'

Update DuressSettings
Set [LogProfileId] = (Select Top 1 Id from MobileLogActivityProfile Where IsDefault = 1)
Where [LogProfileId] is null