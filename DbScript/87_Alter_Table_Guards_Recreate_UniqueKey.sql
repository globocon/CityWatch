
ALTER TABLE dbo.Guards
DROP CONSTRAINT UQ__Guards__737584F6F62ABB51;

ALTER TABLE dbo.Guards
DROP CONSTRAINT UQ__Guards__9F87CC5B0B5C46FB;

ALTER TABLE dbo.Guards
DROP CONSTRAINT UQ__Guards__FA8F7A361E1D9CCE;

/* adding unique key for multiple coloumns */

ALTER TABLE dbo.Guards
ADD UNIQUE ([Name]);

ALTER TABLE dbo.Guards
ADD  UNIQUE (SecurityNo);
