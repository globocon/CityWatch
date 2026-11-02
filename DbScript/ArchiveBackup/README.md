# GuardLogs / RC history archive (`prod_citywatch_backup`)

Keeps `prod-citywatch` to the last **18 months** of `GuardLogs` and `ClientSiteRadioChecksActivityStatus_History`.
Every night, rows that have passed 18 months are **moved** into `prod_citywatch_backup` (on the same
SQL Server instance), together with the log entries' photo records (`GuardLogsDocumentImages`), and
deleted from prod. A manual script restores them if they're needed.

| File | What |
|---|---|
| `01_Create_prod_citywatch_backup.sql` | One-off: creates the database (prod's collation), the three tables (PAGE compressed), `ArchiveRunLog`, `ProdDeleteHold` |
| `Backup-CityWatchArchive.ps1` | Nightly: archive rows older than 18 months and delete them from prod |
| `Restore-CityWatchArchive.ps1` | Manual: put archived rows back into prod (report-only unless `-Execute`) |

## How a row is moved

The job does history first, then log entries (history rows point at log entries), in batches of
1,000 rows. **Each batch is a single transaction across both databases:**

1. It picks the batch's rows older than the cutoff and locks them so they can't change.
2. It copies exactly those rows into the archive. For log entries, it also copies their photo records,
   because prod deletes those along with the entry.
3. It deletes them from prod, but only rows whose archive copy now exists.
4. If the picked, archived and deleted counts don't all match, the whole batch rolls back and the
   run stops.

So a row is only ever deleted from prod by the same transaction that put it in the archive. A
failure leaves both databases exactly as they were before that batch.

The job leaves two kinds of row in prod:
- log entries still referenced from prod;
- rows on hold after a restore (`dbo.ProdDeleteHold`).

The **first night** moves the whole backlog. If it hits the time limit, it carries on the next night.
After that, each night moves only the day that has just passed 18 months: about 8,000 log rows and
10,000 history rows at today's volume.

The archive keeps everything; nothing is ever removed from it.

## Not affecting prod

- **Batches of 1,000 rows** keep each transaction short and stay below SQL Server's lock-escalation
  point, so the app's tables are never locked as a whole. There is a 300 ms pause between batches
  (`-DelayMs`), and each batch runs at MAXDOP 1.
- `LOCK_TIMEOUT 15 s` and `DEADLOCK_PRIORITY LOW`: if the app is holding locks, the job waits,
  gives way and retries.
- Only rows nobody uses any more are touched (older than 18 months), so the app's current pages,
  which work on today's logbooks, don't compete with it.
- **Prod's transaction log:** deletes are logged. The job pauses while the log is over 70% full
  (`-MaxLogPercent`), and stops for the night if it stays full. If prod uses FULL recovery, make sure
  log backups run during the first night's big move.
- It stops after `-MaxRunMinutes` (240) and carries on the next night. Only one run (or restore) at a
  time.
- Measured on the dev copy while the first night's move was running: app-style logbook saves took
  27 ms on average (329 ms at worst), with no table locks.

## Safety checks: the job does nothing if any of these fail

- **The archive database has no full backup in the last 7 days.** Once rows leave prod, the archive
  is their only live copy. Include `prod_citywatch_backup` in the instance's nightly full backups,
  scheduled after this job. Change the window with `-RequireArchiveBackupDays`; 0 turns the check off.
- **Prod is missing DbScript/385** (its `LBId` index). Without it, every log-entry delete scans the
  whole 3 GB history table.
- The archive tables are missing: run the setup script.

## Set up (once)

1. Apply `DbScript/385_Improve_Triggers_GuardLogs_RadioChecksActivityStatus.sql` to prod.
2. Take a **full backup of prod-citywatch**: the old rows are about to leave it.
3. Run `01_Create_prod_citywatch_backup.sql` in SSMS on the prod instance.
4. Back up the new `prod_citywatch_backup` once, so the first run is allowed. Add it to the nightly
   full backups, scheduled after the archive job.
5. Copy the two `.ps1` files to the server, e.g. `D:\CityWatch\Archive\`.
6. Give the Windows account that will run the job these permissions:
   - on `prod-citywatch`: `db_datareader`, `db_datawriter` and `VIEW DATABASE STATE`;
   - on `prod_citywatch_backup`: `db_owner`;
   - `msdb`: read on `dbo.backupset`.

   The restore is run by a DBA and needs INSERT + ALTER on the three prod tables.
7. See what the first run would move, then run it by hand, off-peak:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File D:\CityWatch\Archive\Backup-CityWatchArchive.ps1 -SqlServer localhost -ReportOnly
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File D:\CityWatch\Archive\Backup-CityWatchArchive.ps1 -SqlServer localhost
   ```

8. Schedule it daily (elevated PowerShell on the server; change the account / time):

   ```powershell
   $action  = New-ScheduledTaskAction -Execute 'powershell.exe' `
       -Argument '-NoProfile -ExecutionPolicy Bypass -File "D:\CityWatch\Archive\Backup-CityWatchArchive.ps1" -SqlServer localhost'
   $trigger = New-ScheduledTaskTrigger -Daily -At 2:30am
   $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 5) -MultipleInstances IgnoreNew -StartWhenAvailable
   Register-ScheduledTask -TaskName 'CityWatch archive' -Action $action -Trigger $trigger -Settings $settings `
       -User 'DOMAIN\svc-citywatch' -Password '<password>' -RunLevel Highest
   ```

   Pick the quietest time in **Australian** time: guards log 24/7, and the server's clock may be UTC.
   Exit code 1 means the run failed. Check `logs\` next to the script, or:

   ```sql
   SELECT TOP 20 * FROM prod_citywatch_backup.dbo.ArchiveRunLog ORDER BY Id DESC;
   ```

## Restore

Always run it without `-Execute` first. It prints how many rows match, how many are already in prod,
how many would be restored, and how many can't be (because their logbook, guard, site, KV or IR no
longer exists in prod).

```powershell
# Report: site 887, 1-15 March 2025 (EventDateTime; ToDate is exclusive)
.\Restore-CityWatchArchive.ps1 -SqlServer localhost -FromDate 2025-03-01 -ToDate 2025-03-16 -ClientSiteId 887

# Do it
.\Restore-CityWatchArchive.ps1 -SqlServer localhost -FromDate 2025-03-01 -ToDate 2025-03-16 -ClientSiteId 887 -Execute

# Specific log entries (with their photo records)
.\Restore-CityWatchArchive.ps1 -SqlServer localhost -Table GuardLogs -Id 1234567,1234568 -Execute
```

- It inserts only rows missing from prod and keeps their original Ids. It never updates or deletes
  anything, so it is safe to repeat.
- Log entries are restored first, then their photo records, then history.
- Restored rows are **held for 30 days** (`-HoldDays`), so the nightly job doesn't move them straight
  back. When the hold ends, they go back to the archive.
- Restored log entries don't count as new guard activity in Radio Check (the 385 trigger checks a
  session flag). The script refuses to run against the old trigger.

## Size and time

Tested end to end on a full copy of the dev database (data to 29-10-2026, so the 18-month cutoff
was 02-05-2025), on a dev laptop (i3, 8 GB RAM, one disk). A production server should be faster.

| | History | GuardLogs | Photo records |
|---|---|---|---|
| In prod before | 5,080,326 | 4,300,909 | 203,227 |
| Older than 18 months: moved on the first night | **1,428,722** | **1,443,110** | **13,313** |
| Left in prod (the last 18 months) | 3,651,604 | 2,857,799 | 189,914 |
| First night, time | 11 min | 34 min | (with GuardLogs) |
| Nightly from then on | ~10,000 rows | ~8,000 rows | |

- **First night: 45 minutes in total.** The archive grew by 0.5 GB (PAGE compressed).
- **Nightly runs:** a few seconds when there's nothing to move; about a minute for a normal day's
  ~18,000 rows.
- **Integrity:** every moved table has the same row count and checksum in the archive as it had in
  prod before the move. A restore followed by a re-archive left the archive checksums unchanged.
- 5 history rows were left in prod. They are IR rows with event times a few hours before the cutoff
  but Ids just after it, and they move the next night.
