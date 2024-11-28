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
            foreach (var kpi in kpiSettingsToUpdate)
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
            }
            _dbContext.SaveChanges();

        }

    }

  
}
