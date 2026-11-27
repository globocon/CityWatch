/*
    P9 Issue 44 Step 1B - fill PcarRoute.PcarId (added by 388) for existing routes.

    Each route's patrol car is taken from its smart wand:
        PcarRoute.Smartwandallocation = ClientSiteSmartWands.Id
        ClientSiteSmartWands.PatrolCarId = IncidentReportPositions.Id (IsPatrolCar = 1)

    Only rows with PcarId still NULL are updated, so it is safe to run more than once and never
    overwrites a PCAR chosen in the Booking > PCAR popup. Routes whose smart wand has no patrol car
    (or whose car is no longer ticked as a patrol car) stay NULL; set them by editing the route.
*/

SET NOCOUNT ON;

-- Before: what will be set
SELECT r.Id AS RouteId, r.Pcarroutename, w.SmartWandId, w.PatrolCarId AS NewPcarId, p.Name AS PcarName
FROM dbo.PcarRoute r
JOIN dbo.ClientSiteSmartWands w ON w.Id = r.Smartwandallocation
JOIN dbo.IncidentReportPositions p ON p.Id = w.PatrolCarId AND p.IsPatrolCar = 1
WHERE r.PcarId IS NULL;

UPDATE r
SET r.PcarId = w.PatrolCarId
FROM dbo.PcarRoute r
JOIN dbo.ClientSiteSmartWands w ON w.Id = r.Smartwandallocation
JOIN dbo.IncidentReportPositions p ON p.Id = w.PatrolCarId AND p.IsPatrolCar = 1
WHERE r.PcarId IS NULL;

PRINT CAST(@@ROWCOUNT AS varchar(10)) + ' PcarRoute row(s) updated.';

-- After: routes still without a PCAR (edit these in Booking > PCAR)
SELECT r.Id AS RouteId, r.Pcarroutename, r.Smartwandallocation, w.SmartWandId, w.PatrolCarId
FROM dbo.PcarRoute r
LEFT JOIN dbo.ClientSiteSmartWands w ON w.Id = r.Smartwandallocation
WHERE r.PcarId IS NULL;
GO
