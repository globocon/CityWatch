using CityWatch.Data;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Kpi.Services.FastReport
{
    /// <summary>
    /// Read-only variant of <see cref="IrDataProvider"/> used only by the fast report path.
    ///
    /// Why this exists. Measured on test (schedule 81, Jul 2026):
    /// <c>GetIncidentReports(from, to)</c> took <b>20.4 seconds</b> - 59% of the whole
    /// report - yet the underlying SQL returns in well under a second in SSMS
    /// (1,291 reports, 1,960 joined rows, from a 45,693-row table).
    ///
    /// The gap is Entity Framework, not the database. By the time this query runs the
    /// report has already loaded tens of thousands of entities (every guard, every
    /// compliance record) into the change tracker, and materialising more tracked entities
    /// with includes into a tracker that large is where the time goes. The report never
    /// modifies these rows, so all of that tracking work is wasted.
    ///
    /// This class overrides exactly one method - the expensive read - with an identical
    /// query that skips change tracking. Every other member delegates unchanged to the real
    /// provider, including all write paths.
    ///
    /// <c>AsNoTrackingWithIdentityResolution</c> is used rather than plain
    /// <c>AsNoTracking</c> deliberately: it still returns one shared instance per entity
    /// key, matching what the tracked query does today. Plain no-tracking would hand out
    /// duplicate ClientSite instances for reports on the same site - harmless for the values
    /// rendered, but a semantic change, and the whole point of this path is that nothing
    /// about the output changes.
    /// </summary>
    public sealed class ReadOnlyIrDataProvider : IIrDataProvider
    {
        private readonly IIrDataProvider _inner;
        private readonly CityWatchDbContext _dbContext;

        public ReadOnlyIrDataProvider(IIrDataProvider inner, CityWatchDbContext dbContext)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Mirrors <c>IrDataProvider.GetIncidentReports(fromDate, toDate)</c> exactly - same
        /// includes, same predicate, same ordering semantics - with change tracking off.
        /// If that method ever changes, this must change with it.
        /// </summary>
        public List<IncidentReport> GetIncidentReports(DateTime fromReportDate, DateTime toReportDate)
        {
            return _dbContext.IncidentReports
                .AsNoTrackingWithIdentityResolution()
                .Include(n => n.IncidentReportEventTypes)
                .Include(n => n.ClientSite)
                .ThenInclude(c => c.ClientType)
                .Where(x => x.ReportDateTime >= fromReportDate
                            && x.ReportDateTime < toReportDate.AddDays(1)
                            && x.ClientSite.IsActive == true)
                .ToList();
        }

        // ------------------------------------------------------------------
        // Everything below is untouched pass-through to the real provider.
        // ------------------------------------------------------------------

        public List<IncidentReport> GetIncidentReports(DateTime fromDate, DateTime toDate, int clientSiteId)
            => _inner.GetIncidentReports(fromDate, toDate, clientSiteId);

        public List<IncidentReport> GetIncidentReports()
            => _inner.GetIncidentReports();

        public List<IncidentReport> GetIncidentReportsByJobNumber(string jobNumber)
            => _inner.GetIncidentReportsByJobNumber(jobNumber);

        public List<IncidentReport> GetIncidentReportsForDockets(DateTime fromReportDate, DateTime toReportDate)
            => _inner.GetIncidentReportsForDockets(fromReportDate, toReportDate);

        public List<IncidentReportsPlatesLoaded> GetIncidentReportsPlates()
            => _inner.GetIncidentReportsPlates();

        public List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDockets(DateTime logFromDate, DateTime logToDate)
            => _inner.GetKeyVehicleLogsWithDockets(logFromDate, logToDate);

        public List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDocketsWithoutDate()
            => _inner.GetKeyVehicleLogsWithDocketsWithoutDate();

        public List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids)
            => _inner.GetKeyVehicleLogByIds(ids);

        public void SaveReport(IncidentReport incidentReport)
            => _inner.SaveReport(incidentReport);

        public void MarkAsUploaded(int id)
            => _inner.MarkAsUploaded(id);

        public void UpdateReport(int incidentreportid, int id)
            => _inner.UpdateReport(incidentreportid, id);

        public void UpdateTheSiteExpiringToExpired()
            => _inner.UpdateTheSiteExpiringToExpired();
    }
}
