-- Rename existing "ADMIN - Roster" to "ADMIN - ROSTER G$S"
UPDATE guardaccess SET AccessName = 'ADMIN - ROSTER G$S' WHERE AccessName = 'ADMIN - Roster';

-- Insert the new roles
INSERT INTO guardaccess (AccessName) VALUES ('ADMIN - ROSTER');
INSERT INTO guardaccess (AccessName) VALUES ('ADMIN - ROSTER G$');

-- Update CredentialOrder for correct ordering
-- The user specified: "Admin-Roster" first, then "Admin-Roster G$", then "Admin-Roster G$S"
UPDATE guardaccess SET CredentialOrder=13 WHERE AccessName = 'ADMIN - ROSTER';
UPDATE guardaccess SET CredentialOrder=14 WHERE AccessName = 'ADMIN - ROSTER G$';
UPDATE guardaccess SET CredentialOrder=15 WHERE AccessName = 'ADMIN - ROSTER G$S';

-- Alter Guards table to add the new access flags
ALTER TABLE Guards ADD IsAdminRosterBaseAccess bit default 0;
ALTER TABLE Guards ADD IsAdminRosterGSAccess bit default 0;

-- Set default values
UPDATE Guards SET IsAdminRosterBaseAccess = 0, IsAdminRosterGSAccess = 0;
