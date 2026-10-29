/*
    PCAR handling for the site log dump scheduler (Kpi > Admin > Settings > site > LB).

    1. "Disable PCAR" on the Schedule tab, one per period. When on, the site's daily / weekly /
       monthly LB, SW and Fusion dumps leave out every entry made by a patrol car
       (GuardLogs.IsEntryByPCAR = 1). KV has no PCAR marker and is never filtered.
       Ignored for a site that is itself a PCAR site (ClientSites.PatrolTourMode = 1), where every
       entry is a PCAR entry.

    2. The "Schedule PCAR" tab: a separate LB / SW / Fusion dump per period containing ONLY the PCAR
       entries. Sent to the same recipients as the Schedule tab (GuardLogEmailTo,
       GuardLogEmailWeeklyLogTo, GuardLogEmailMonthlyLogTo).

    All default to 0, so no site's dumps change until the settings are switched on.
*/

ALTER TABLE ClientSites ADD

DisablePcarDailyLog bit NOT NULL default 0,
DisablePcarWeeklyLog bit NOT NULL default 0,
DisablePcarMonthlyLog bit NOT NULL default 0,

UploadPcarGuardLog bit NOT NULL default 0,
UploadPcarSWLog bit NOT NULL default 0,
UploadPcarFusionLog bit NOT NULL default 0,

UploadPcarGuardWeeklyLog bit NOT NULL default 0,
UploadPcarSWWeeklyLog bit NOT NULL default 0,
UploadPcarFusionWeeklyLog bit NOT NULL default 0,

UploadPcarGuardMonthlyLog bit NOT NULL default 0,
UploadPcarSWMonthlyLog bit NOT NULL default 0,
UploadPcarFusionMonthlyLog bit NOT NULL default 0
GO

/*
    No new table for tracking. The PCAR reports are tracked under three new LogBookType values,
    the same way the Smart Wand and Fusion dumps are:
        5 = PCAR Daily Guard Log, 6 = PCAR Smart Wand Log, 7 = PCAR Fusion Log
    - daily: a ClientSiteLogBooks row of that type per site per day, DbxUploaded set once sent
      (UQ_Site_Type_Date already allows it);
    - weekly / monthly: ClientSitePeriodicLogUploads.LogBookType, so a PCAR dump never collides
      with the site's normal dump of the same period.
*/
