/* 368: GeocodeCache — one short address per ~110 m grid cell (lat/lon × 1000, floored).
   The cache is what makes reverse geocoding affordable AND polite: the provider is only
   consulted the first time anybody visits a cell within the cache window, and a failed
   lookup is cached too (Address NULL) so a provider outage never becomes a retry storm.
   Idempotent, like every script in this pack. Rollback: DROP TABLE dbo.GeocodeCache. */

IF OBJECT_ID(N'dbo.GeocodeCache', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GeocodeCache
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL,
        CellLat      INT            NOT NULL,
        CellLon      INT            NOT NULL,
        Address      NVARCHAR(300)  NULL,
        ResolvedUtc  DATETIME2(0)   NOT NULL,

        CONSTRAINT PK_GeocodeCache PRIMARY KEY NONCLUSTERED (Id)
    );

    CREATE UNIQUE CLUSTERED INDEX UX_GeocodeCache_Cell
        ON dbo.GeocodeCache (CellLat, CellLon);

    PRINT '368: dbo.GeocodeCache created.';
END
ELSE
    PRINT '368: dbo.GeocodeCache already exists - nothing to do.';
