/*
    P9 Issue 44 Step 1B - PCAR route profiles store their patrol car.

    PcarRoute.PcarId = the patrol car (PCAR) chosen with the profile in Booking > PCAR > Manage PCAR Routes:
    an IncidentReportPositions.Id with IsPatrolCar = 1 (Admin/Settings > C4i Fields > IR > Position,
    "Patrol Cars ?" ticked). Chosen alongside PcarRoute.Smartwandallocation (ClientSiteSmartWands.Id).

    Nullable, because existing routes have no value until 389 fills them from their smart wand, and
    a route whose smart wand has no patrol car keeps NULL until it is edited.
    No foreign key, so a position can still be deleted in C4i Fields (routes then show no PCAR).

    Run this BEFORE deploying the code: CityWatchDbContext maps the column, so every app that reads
    PcarRoute (Web, Kpi, RadioCheck, Tracking) needs it to exist.
*/

IF COL_LENGTH('dbo.PcarRoute', 'PcarId') IS NULL
BEGIN
    ALTER TABLE dbo.PcarRoute ADD PcarId int NULL;
END
GO
