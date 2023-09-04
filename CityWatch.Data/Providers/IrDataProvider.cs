using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    public class IrDataProvider : IIrDataProvider
    {
        private readonly CityWatchDbContext _dbContext;

        public IrDataProvider(CityWatchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<IncidentReport> GetIncidentReports(DateTime fromReportDate, DateTime toReportDate)
        {
            return _dbContext.IncidentReports
                .Include(n => n.IncidentReportEventTypes)
                .Where(x => x.ReportDateTime >= fromReportDate
                            && x.ReportDateTime < toReportDate.AddDays(1))
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



    }
}
