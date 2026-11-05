# Fast Report Generator (Parallel Implementation)

A second, independent path for generating the monthly KPI report, added alongside the
existing one so both can run side by side and be compared. The production path is
unchanged and remains the source of truth.

---

## 1. What was NOT touched

No existing report logic was modified. Specifically, these files are byte-for-byte
unchanged:

| File | Role |
|---|---|
| `CityWatch.Kpi/Services/ReportGenerator.cs` | Renders each site's PDF pages |
| `CityWatch.Kpi/Services/MonthlySummaryReportGenerator.cs` | Renders the cover sheet |
| `CityWatch.Kpi/Services/WeeklySummaryReportGenerator.cs` | Weekly cover sheet variant |
| `CityWatch.Data/Helpers/PdfHelper.cs` | Merges the PDFs |
| `CityWatch.Kpi/Pages/Admin/Settings.cshtml.cs` | Legacy `OnGetDownloadPdf` / `OnPostRunSchedule` handlers |
| `CityWatch.Kpi/wwwroot/js/site.js` | Legacy `#btnScheduleDownload` / `#btnScheduleRun` handlers |
| Every data provider in `CityWatch.Data/Providers/` | Unchanged |

`SendScheduleService.cs` has exactly one addition: a public `SendScheduleEmail(...)` that
forwards to the existing private `SendEmail(...)`. Every legacy method — `ProcessSchedule`,
`ProcessDownload` and the private email/upload helpers — is otherwise unchanged. The fast
path calls that one method rather than re-implementing the recipient and attachment rules,
so a Run Now sends a byte-identical message.

The remaining edits to pre-existing files are additive:

1. `Pages/Admin/_SchedulePopup.cshtml` — two new `<button>`s; the legacy pair is kept in the
   DOM with `d-none` so their handlers and server flow survive as a fallback.
2. `Pages/Shared/_Layout.cshtml` — one `<script>` tag for the new JS file.
3. `Program.cs` — four DI registrations in a clearly marked block at the end.

---

## 2. The core design decision

The obvious way to build a faster generator is to copy `ReportGenerator.cs` (2,580 lines
of iText layout code) and optimise the copy. **That was rejected**, because the moment
the layout code is duplicated, the two versions can drift and "identical output" becomes
something you have to keep proving forever.

Instead, the fast path **reuses the existing generator unchanged** and makes it faster by
changing only what it is fed:

```
FastReportService  (new orchestration + progress)
        │
        ├── resolves IReportGenerator ────────────► existing ReportGenerator (untouched)
        │                                                    │
        │                                                    ▼
        │                                          IViewDataService, IGuardDataProvider,
        │                                          IClientDataProvider, ...
        │                                                    │
        └── ...from a child DI container where those interfaces are wrapped in
            MemoizingProxy<T>, which returns the *same values* from a per-run cache
            instead of re-querying the database.
```

Identical rendering code + identical input values = identical document. The speed comes
entirely from not asking the database the same question thousands of times.

### Why the redundant queries exist

`DailyKpiGuard.LEDStatusForLoginUser` (`CityWatch.Kpi/Models/DailyKpiGuard.cs:386`) issues
two queries every time it is called:

- `GetHRDescFull()` — the whole `HrSettings` table with three `Include`s
- `GetGuardLicensesandcompliance(guardId)` — runs its query twice, the second with a
  correlated `HrSettings.FirstOrDefault` using `.ToLower().Trim()` (non-sargable)

It is called from the `ShiftNGuardHrN` property getters, which are recomputed on every
access and have no caching. `ReportGenerator.CreateGuardReportData` then wraps the whole
grid in `for (guardIndex = 0; guardIndex < maxTables; guardIndex++)`, so the work is
repeated once per concurrent guard:

```
maxTables x 31 days x 3 shifts x 3 HR columns x guards-per-shift x 2 queries
```

With 3 tables and 2 guards per shift that is roughly **3,300 database round-trips per
site**. The memo cache reduces the distinct queries to one per `(method, arguments)` pair
— typically two, plus one per distinct guard.

Other duplicates the cache removes: `GetGuards()` (a full `Guards` table load, called four
times per site by the HR chart builders), `GetAllGuardLicensesAndCompliances()`,
`GetClientSites(null)` (all sites loaded to read one `State`), and
`GetClientSiteKpiSetting(siteId)`.

---

## 3. Files added

```
CityWatch.Kpi/Services/FastReport/
├── FastReportModels.cs         Job, progress, metrics, ETA and percentage maths
├── FastReportJobStore.cs       In-memory job registry with TTL sweep
├── ReportScopeCache.cs         Per-run memo store + performance counters
├── MemoizingProxy.cs           DispatchProxy decorator + the caching allow-list
├── FastReportScopeFactory.cs   Child DI container with decorated providers
├── FastReportService.cs        Orchestration, progress, cancellation, error handling
└── FastReportComparer.cs       Structural PDF equality check (used by the benchmark)

CityWatch.Kpi/API/FastKpiReportController.cs   start / progress / download / cancel / log / benchmark
CityWatch.Kpi/wwwroot/js/fast-report.js        Button handler + progress overlay
```

---

## 4. Safety rules in the caching layer

The cache is the only thing that could change output, so it is deliberately conservative.

**Allow-list only.** `FastReportCachePolicy` (in `MemoizingProxy.cs`) names every method
that may be cached. Each was checked to be a pure read on the report path. Anything not
listed — including every write method — passes straight through to the real provider,
untouched and uncounted.

**Unkeyable arguments disable caching.** The cache key is built structurally from the
method name and arguments (complex objects such as `PatrolRequest` are serialised). If a
key cannot be produced, the call is treated as a pass-through rather than risking a wrong
hit.

**Cache hits return a fresh list.** Methods returning `List<T>` hand back a new list
instance over the same elements. Today each call already returns a new list over EF's
identity-mapped entities, so this preserves the existing semantics exactly — a caller that
mutates the returned list (and one does: `GetYearofOnBoardingGuardHrReportBarchart`
rewrites `guard.DateEnrolled`) cannot corrupt a later caller in a way it could not
already.

**Scope is one run.** The cache lives in the DI scope created for a single report and is
discarded with it. It is never shared between jobs, so a report can never read data
captured before it started. This matches the existing generator, which does all its
reading inside one request anyway.

**Exceptions are rethrown faithfully.** `ExceptionDispatchInfo.Capture(...).Throw()`
preserves the original stack, so a failure looks the same as it would without the proxy.

---

## 5. Progress reporting

Coarse stages, with intra-site interpolation so the bar never appears frozen:

| Stage | Percentage |
|---|---|
| Preparing | 2% |
| Loading schedule | 5% |
| Generating site reports | 5% → 80% |
| Building summary cover page | 85% |
| Merging documents | 92% |
| Sending email *(Run Now only)* | 96% |
| Preparing download / Finishing up | 98% |
| Completed | 100% |

Within the site stage, progress is interpolated from the number of data-provider calls the
current site has made against the rolling average for a completed site. The step text
comes from the same interception point, which is why it can say things like
`Site 3 of 12 - Loading guard compliance` without any instrumentation inside the
untouched generator.

The percentage is **derived**, never stored, so it cannot disagree with the stage; the
client additionally clamps it so it can never move backwards.

**ETA** is a rolling average of completed site durations, times the sites remaining, plus a
reserve of half a site for the summary/merge tail. It returns null (displayed as
"calculating...") until at least one site has finished, rather than showing a fabricated
number.

### 5a. The two run modes

Both live buttons on the Run Schedule popup use this pipeline. They differ only by
`FastReportRequest.Mode`, and each mode reproduces its own legacy counterpart exactly:

| | `Mode=Download` — "Download Now" | `Mode=Email` — "Run Now" |
|---|---|---|
| Mirrors | `SendScheduleService.ProcessDownload` | `SendScheduleService.ProcessSchedule` (`upload: false`) |
| Per-site KPI data import (`IImportDataService.Run`) | no | **yes**, before each site renders |
| Critical-document downselect | schedule's setting | **off** — `ProcessSchedule` omits both arguments |
| Cover sheet, merge, file name | identical | identical |
| Outcome | PDF streamed to the browser | PDF emailed via `SendScheduleEmail`, then deleted |
| `NextRunOn` update / SharePoint upload | no | no (`upload: false`, same as the button today) |

The downselect row is a **deliberate preservation of an existing inconsistency**, not an
oversight: `ProcessSchedule` calls `GeneratePdfReport` without the `IsDownselect` /
`CriticalDocumentID` arguments, so today's emailed report never applies the downselect even
when the schedule has it enabled. Applying it in the new path would silently change what
clients receive. If that inconsistency should be fixed, it is a separate decision — remove
the `isEmailRun` conditional in `FastReportService` and re-run the side-by-side check.

Cancellation is offered until the email stage begins and is then withdrawn, because a sent
message cannot be recalled. If SMTP fails, the job reports that the report was generated but
the email could not be sent — the PDF is deleted rather than orphaned on disk.

---

## 6. Non-blocking behaviour

`POST /start` queues the job on a background task and returns a job id immediately. The
browser polls `GET /progress/{jobId}` roughly once a second. Nothing blocks a request
thread for the duration of the report, which also removes the request-timeout risk the
legacy synchronous handler carries on large schedules.

The finished PDF is streamed from disk with `FileOptions.DeleteOnClose`, so memory stays
flat regardless of report size and the temp file cleans itself up.

---

## 7. Error handling

- Failures are captured on the job, not thrown into a dead request. Status becomes
  `Failed` with a plain-language message.
- The full activity log and exception detail are available at `GET /log/{jobId}` and are
  surfaced in the UI behind a "View log" button.
- Every failure path still runs temp-file cleanup in a `finally`.
- Retry re-submits the same request from the client with one click; the original request
  is preserved on the job too.
- A background exception cannot take the process down — `Start` wraps the task body.

---

## 8. How to verify on the test server

### 8a. Side-by-side check

1. Open a schedule popup on `/Admin/Settings`.
2. Click **Download Now** — this is now the fast generator — and keep the PDF.
3. Produce the legacy PDF for comparison: either un-hide `#btnScheduleDownload`
   (remove `d-none` in `_SchedulePopup.cshtml`) or use the benchmark endpoint below.
4. Compare: page count, totals, guard grid, charts, ordering.

### 8b. Automated comparison + benchmark

The benchmark endpoint runs **both** generators over the same schedule and month, then
reports timings plus a structural comparison of the two documents:

```bash
curl -X POST https://test.c4i-system.com/api/FastKpiReport/benchmark -d "ScheduleId=46&ReportYear=2026&ReportMonth=1&IgnoreRecipients=true"
```

Response includes:

```
identical             true/false  - the verdict
comparison            page count, per-page text differences, per-page image differences
performance           legacyMilliseconds, fastMilliseconds, speedupFactor, percentFaster
                      fastQueryCalls    - queries the fast path actually issued
                      fastCacheHits     - duplicate queries avoided
                      fastTopMethods    - most expensive providers, for further tuning
```

**On raw byte equality:** it is not a usable test and is deliberately not the verdict.
iText stamps every document with a creation timestamp, a modification timestamp and a
random document ID, so two runs of the *same* generator one second apart already differ at
the byte level. `FastReportComparer` therefore compares page count, normalised extracted
text page by page, and the count of embedded images per page — the last of which catches a
chart that silently failed to render. File size is reported for information only.

### 8c. Recommended coverage

Run the benchmark against at least: a single-site schedule, a multi-site schedule
(schedule 46 has 4 sites), a month with dense guard data, and a month with no data. The
last one matters because both paths must handle the empty case the same way.

### 8d. Email test mode — send everything to yourself first

`Email:TestModeRedirectTo` in `appsettings.json` is the safety valve for testing the send
without any client receiving anything. It is currently set:

```json
"TestModeRedirectTo": "addileepsebastian@gmail.com",
```

While it holds one or more addresses (comma separated), every KPI schedule email —
monthly, timesheet, key-vehicle and custom-wand — has its **To, CC and BCC discarded** and
goes only to those addresses, with `[TEST]` prefixed to the subject. That includes the
standing `globoconsoftware@gmail.com` BCC and any address a schedule defines.

**To go back to live sending:** set the value to `""` and restart. Nothing else changes.

```json
"TestModeRedirectTo": "",
```

Three things make it hard to leave switched on by accident:

- the app logs `EMAIL TEST MODE IS ACTIVE` as a warning at startup;
- pressing **Run Now** shows *"Test mode. This report will be emailed only to …"* in the
  overlay as soon as the job is queued — minutes before the send, while Cancel still works.
  If it instead says *"Live send"*, the setting is not in effect on that server;
- the job log records `TEST MODE: report emailed only to …` on the run itself.

The one trap: this lives in the **deployed** `appsettings.json`. If a deployment does not
overwrite that file on the server, the key will not exist there and the send will be live.
Trust the overlay banner, not the repo.

Scope note: the valve is applied inside `CityWatch.Kpi`'s `SendScheduleService` only.
`CityWatch.Web` and `CityWatch.RadioCheck` have their own email configuration and are not
affected by it. Also note that while it is on, the **automatic scheduled** KPI sends from
that server are redirected too, not just the button — which is what you want on test, and
must never be true on production.

### 8e. Run Now (email mode) — must be verified separately

The benchmark does **not** cover this: it always runs `Mode=Download` and never sends mail.
Email mode has to be exercised through the button, so:

1. Confirm the overlay shows the **test mode** banner (§8d) before letting the run proceed.
   Ticking **Ignore email recipients (CC & BCC)** is a useful second belt: it drops the
   schedule's CC/BCC even when test mode is off.
2. Click **Run Now** on a small schedule. Confirm the overlay reaches *Sending email* and
   then *Completed*, the popup line reads "Done. Report sent via email", and the Cancel
   button disappears at the email stage.
3. Check the received message: the subject should carry the `[TEST]` prefix, and subject,
   attachment name and page count must otherwise match what the same schedule and month
   produce via **Download Now** — with the downselect caveat in §5a for schedules that have
   `IsCriticalDocumentDownselect` enabled.
4. Confirm the per-site KPI import ran: `KpiDataImportJob` rows for the period should show a
   completed status, and `DailyClientSiteKpi` rows should be refreshed. This is the step
   `Download Now` skips and `Run Now` must not.
5. Confirm nothing is left behind under `wwwroot/Pdf/Output/fast/`.
6. Break SMTP deliberately once (bad port in config) and confirm the failure says the report
   was generated but the email could not be sent, and that no PDF is orphaned.

---

## 9. Known limitations and deliberate choices

**Sites are still processed sequentially.** The dominant cost was database round-trips,
which the cache removes without any concurrency. Parallelising sites was deliberately not
done in this version because `ReportGenerator.GetChartImage`
(`CityWatch.Kpi/Services/ReportGenerator.cs:2546`) names its temporary chart PNG with a
**second-resolution timestamp** — two reports rendering charts in the same second
overwrite each other's files. That is a pre-existing bug that would become reachable under
parallelism. The orchestrator is structured so sites can be parallelised later once that
filename is made unique.

**Chart rendering is unchanged.** All 18 `GetChartImage` call sites still invoke Node.js
serially and round-trip a PNG through disk. After the cache lands, this is likely the next
bottleneck. Addressing it means either parallelising those calls or rendering in-process —
the latter would change pixel output and needs sign-off, so it was left alone.

**The job store is per-process.** Behind a load balancer without sticky sessions, progress
polling could hit a node that does not know the job. A shared store (Redis or SQL) would
be needed for a multi-node deployment.

**Endpoint authorisation matches the existing page — and that now matters more.**
`Program.cs` applies `AllowAnonymousToFolder("/")` and there is no global authorisation
filter, so both legacy handlers — `?handler=DownloadPdf` and `?handler=RunSchedule` — are
already effectively anonymous, and `POST /api/FastKpiReport/start` matches that posture
rather than unilaterally changing it. Note what that means with email mode added: an
unauthenticated caller can trigger a client-facing email, exactly as they can today via
`?handler=RunSchedule`. It is not a new exposure, but it is a bigger one than a download.
Putting these endpoints behind authentication is worth doing as its own change, covering
the legacy handlers and this controller together.

**One legacy side effect is reproduced on purpose (download mode).** The legacy download
path inserts a `KpiDataImportJobs` row per site and never completes it (the import call on
`SendScheduleService.cs:709` is commented out), leaving orphan rows that
`KpiReportController.Send()` later has to clear. Download mode writes the same row so the
two are directly comparable; email mode runs the import for real, exactly as
`ProcessSchedule` does. It is a candidate for cleanup, but changing it here would have
meant the benchmark was not comparing like with like.

---

## 10. Rollback

In `_SchedulePopup.cshtml`, move the `d-none` class from the legacy `#btnScheduleRun` /
`#btnScheduleDownload` pair onto `#btnScheduleRunFast` / `#btnScheduleDownloadFast`. The
original buttons, their `site.js` handlers and the original Razor Page handlers are all
still present and unmodified, so that single edit restores the previous behaviour with no
server change and no deployment of anything else.

Full removal is the four `Program.cs` registrations, the script tag, the
`Services/FastReport/` folder plus its controller and JS file, the two fast buttons, and
the `SendScheduleEmail` member on `ISendScheduleService` / `SendScheduleService`.
