INSERT INTO GuardLicenses(GuardId, LicenseNo, LicenseType)
SELECT g.Id, g.SecurityNo, 0 
FROM Guards g
LEFT OUTER JOIN GuardLicenses l
ON l.GuardId = g.Id
WHERE l.Id IS NULL