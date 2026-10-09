using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using static Dropbox.Api.TeamLog.SpaceCapsType;

namespace CityWatch.Data.Providers
{
    public interface IIrDataProvider
    {
        List<IncidentReport> GetIncidentReports(DateTime fromDate, DateTime toDate);
        List<IncidentReport> GetIncidentReports(DateTime fromDate, DateTime toDate, int clientSiteId);
        void SaveReport(IncidentReport incidentReport);
        
        void MarkAsUploaded(int id);
        List<IncidentReport> GetIncidentReportsByJobNumber(string jobNumber);
        void UpdateReport(int incidentreportid, int Id);

        public void UpdateTheSiteExpiringToExpired();
        List<IncidentReport> GetIncidentReports();
        List<IncidentReportsPlatesLoaded> GetIncidentReportsPlates();
        List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDockets(DateTime logFromDate, DateTime logToDate);
        List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids);
        List<IncidentReport> GetIncidentReportsForDockets(DateTime fromReportDate, DateTime toReportDate);
        List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDocketsWithoutDate();
        List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDocketNumber(string DocketNo);
    }

    public class IrDataProvider : IIrDataProvider
    {
        private readonly CityWatchDbContext _dbContext;
        private readonly IClientDataProvider _clientDataProvider;
        public IrDataProvider(CityWatchDbContext dbContext, IClientDataProvider clientDataProvider )
        {
            _dbContext = dbContext;
            _clientDataProvider = clientDataProvider;
        }

        public List<IncidentReport> GetIncidentReports(DateTime fromReportDate, DateTime toReportDate)
        {
            return _dbContext.IncidentReports
                .Include(n => n.IncidentReportEventTypes)
                .Include(n => n.ClientSite)
                .ThenInclude(c => c.ClientType)
                .Where(x => x.ReportDateTime >= fromReportDate
                            && x.ReportDateTime < toReportDate.AddDays(1) && x.ClientSite.IsActive==true)
                .ToList();
        }

        public List<IncidentReport> GetIncidentReports(DateTime fromDate, DateTime toDate, int clientSiteId)
        {
            return _dbContext.IncidentReports
                .Where(x => x.ClientSiteId.GetValueOrDefault() == clientSiteId && 
                            x.CreatedOn >= fromDate.ToUniversalTime() 
                            && x.CreatedOn < toDate.ToUniversalTime().AddDays(1))
                .ToList();
        }

        public List<IncidentReport> GetIncidentReportsByJobNumber(string jobNumber)
        {
            return _dbContext.IncidentReports
                .Where(x => x.JobNumber == jobNumber)
                .ToList();
        }

        public void SaveReport(IncidentReport incidentReport)
        {
            if (incidentReport.Id == 0)
            {
                _dbContext.Add(incidentReport);
            }
            _dbContext.SaveChanges();
        }

        public void MarkAsUploaded(int id)
        {
            var incidentReportsToUpdate = _dbContext.IncidentReports.SingleOrDefault(x => x.Id == id);
            if (incidentReportsToUpdate != null)
            {
                incidentReportsToUpdate.DbxUploaded = true;
                _dbContext.SaveChanges();
            }
        }
        public void UpdateReport(int incidentreportid, int Id)
        {
            //if (incidentReport.Id == 0)
            //{
            //    _dbContext.Add(incidentReport);
            //}
            //_dbContext.SaveChanges();
            var updateGuard = _dbContext.IncidentReportsPlatesLoaded.SingleOrDefault(x => x.Id == Id);
            updateGuard.IncidentReportId = incidentreportid;
            _dbContext.SaveChanges();
        }


        public void UpdateTheSiteExpiringToExpired()
        {
            var today = DateTime.Now.Date;

            // Fetch ClientSites that need updating ie expiring to expired
            var clientSitesToUpdate = _dbContext.ClientSites
                .Where(x => x.Status == 1 && x.StatusDate < today)
                .ToList();

            // Fetch corresponding KPI settings
            var siteIds = clientSitesToUpdate.Select(x => x.Id).ToList();
            var kpiSettingsToUpdate = _dbContext.ClientSiteKpiSettings
                .Where(kpi => siteIds.Contains(kpi.ClientSite.Id))
                .ToList();

            // Update the ClientSites
            foreach (var site in clientSitesToUpdate)
            {
                site.Status = 2;
            }
            _dbContext.SaveChanges();
            // Update the KPI settings
            var clientSitesToUpdate2 = _dbContext.ClientSites
               .Where(x => x.Status == 2 )
               .ToList();
            var siteIds2 = clientSitesToUpdate2.Select(x => x.Id).ToList();
            var kpiSettingsToUpdate2 = _dbContext.ClientSiteKpiSettings
                .Where(kpi => siteIds2.Contains(kpi.ClientSite.Id))
                .ToList();


            foreach (var kpi in kpiSettingsToUpdate2)
            {
               
                updateKpiSettings(kpi.Id);
                // Save all changes in one go
                updateClientSite(kpi.ClientSite.Id);
            }
           

        }
        public void updateKpiSettings(int kpisettingsId)
        {
            var kpisettings = _dbContext.ClientSiteKpiSettings.SingleOrDefault(z => z.Id == kpisettingsId);
            if (kpisettings != null)
            {
                kpisettings.ScheduleisActive = false;
                kpisettings.DropboxScheduleisActive = false;
            }
            _dbContext.SaveChanges();

        }

        public void updateClientSite(int ClientSite)
        {
            var clientSite = _dbContext.ClientSites.SingleOrDefault(z => z.Id == ClientSite);
            if (clientSite != null)
            {
                clientSite.UploadGuardLog = false;
                clientSite.UploadKVLog = false;
                clientSite.UploadSWLog = false;
                clientSite.UploadFusionLog = false;
            }
            _dbContext.SaveChanges();

        }
        public List<IncidentReport> GetIncidentReports()
        {
            return _dbContext.IncidentReports
                .Include(n => n.IncidentReportEventTypes)
                .Include(n => n.ClientSite)
                .ThenInclude(c => c.ClientType)
                .ToList();
        }
        public List<IncidentReportsPlatesLoaded> GetIncidentReportsPlates()
        {
            return _dbContext.IncidentReportsPlatesLoaded.ToList();

        }
        public List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDockets(DateTime logFromDate, DateTime logToDate)
        {
            var results = _dbContext.KeyVehicleLogDocketHistory
               .Where(z => z.KeyVehicleLog.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog
                            && z.KeyVehicleLog.EntryTime >= logFromDate && z.KeyVehicleLog.EntryTime < logToDate.AddDays(1))
               .Include(z => z.KeyVehicleLog)
               .Include(z => z.KeyVehicleLog.GuardLogin.Guard)
               .Include(x => x.KeyVehicleLog.ClientSiteLocation)
               .Include(x => x.KeyVehicleLog.ClientSitePoc);

            results.Include(x => x.KeyVehicleLog.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .Load();

            return results.OrderBy(z => z.KeyVehicleLog.EntryTime).ToList();
        }
        public List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids)
        {
            return _dbContext.KeyVehicleLogs.Where(x => ids.Contains(x.Id))
                .Include(z => z.GuardLogin.Guard)
                .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)
                .Include(z => z.ClientSitePoc)
                .Include(z => z.ClientSiteLocation).ToList();
        }
        public List<IncidentReport> GetIncidentReportsForDockets(DateTime fromReportDate, DateTime toReportDate)
        {
            return _dbContext.IncidentReports
                .Include(n => n.IncidentReportEventTypes)
                .Include(n=>n.ClientSite)
                .Include(n => n.ClientSite.ClientType)
                .Where(x => x.ReportDateTime >= fromReportDate
                            && x.ReportDateTime < toReportDate.AddDays(1) && x.ClientSite.IsActive == true)
                .ToList();
        }
        public List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDocketsWithoutDate()
        {
            var results = _dbContext.KeyVehicleLogDocketHistory
               .Where(z => z.KeyVehicleLog.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog)
               .Include(z => z.KeyVehicleLog)
               .Include(z => z.KeyVehicleLog.GuardLogin.Guard)
               .Include(x => x.KeyVehicleLog.ClientSiteLocation)
               .Include(x => x.KeyVehicleLog.ClientSitePoc);

            results.Include(x => x.KeyVehicleLog.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .Load();

            return results.OrderBy(z => z.KeyVehicleLog.EntryTime).ToList();
        }

        public List<KeyVehicleLogDocketHistory> GetKeyVehicleLogsWithDocketNumber(string DocketNo)
        {
            var results = _dbContext.KeyVehicleLogDocketHistory
               .Where(z => z.KeyVehicleLog.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog && z.DocketSerialNo == DocketNo)
               .Include(z => z.KeyVehicleLog)
               .Include(z => z.KeyVehicleLog.GuardLogin.Guard)
               .Include(x => x.KeyVehicleLog.ClientSiteLocation)
               .Include(x => x.KeyVehicleLog.ClientSitePoc);

            results.Include(x => x.KeyVehicleLog.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .Load();

            return results.OrderBy(z => z.KeyVehicleLog.EntryTime).ToList();
        }

    }


}
