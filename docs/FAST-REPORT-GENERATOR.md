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
| `CityWatch.Kpi/Services/SendScheduleService.cs` | Legacy orchestration + email + Dropbox |
| `CityWatch.Data/Helpers/PdfHelper.cs` | Merges the PDFs |
| `CityWatch.Kpi/Pages/Admin/Settings.cshtml.cs` | Legacy `OnGetDownloadPdf` handler |
| `CityWatch.Kpi/wwwroot/js/site.js` | Legacy `#btnScheduleDownload` handler |
| Every data provider in `CityWatch.Data/Providers/` | Unchanged |

The only edits to pre-existing files are three additive lines:

1. `Pages/Admin/_SchedulePopup.cshtml` — one new `<button>` next to the existing one.
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
| Merging documents | 94% |
| Preparing download | 98% |
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
2. Click **Download Now** (existing) and keep the PDF.
3. Click **Download Now (Fast Beta)** and keep that PDF.
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

**Endpoint authorisation matches the existing page.** `Program.cs` applies
`AllowAnonymousToFolder("/")` and there is no global authorisation filter, so the legacy
`/Admin/Settings?handler=DownloadPdf` handler is already effectively anonymous. The new
controller matches that posture rather than weakening or unilaterally changing it. If
report endpoints should require authentication, that is a separate change that should
cover both paths together.

**One legacy side effect is reproduced on purpose.** The legacy download path inserts a
`KpiDataImportJobs` row per site and never completes it (the import call on
`SendScheduleService.cs:709` is commented out), leaving orphan rows that
`KpiReportController.Send()` later has to clear. The fast path writes the same row so the
two are directly comparable. It is a candidate for cleanup, but changing it here would
have meant the benchmark was not comparing like with like.

---

## 10. Rollback

Delete the button in `_SchedulePopup.cshtml`. The fast path becomes unreachable; nothing
else references it. Full removal is the four `Program.cs` registrations, the script tag,
and the `Services/FastReport/` folder plus its controller and JS file.
