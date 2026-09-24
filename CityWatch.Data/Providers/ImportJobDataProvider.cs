using CityWatch.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CityWatch.Data.Providers
{
    public interface IImportJobDataProvider
    {
        [Obsolete]
        List<KpiDataImportJob> GetKpiDataImportJobs();
        KpiDataImportJob GetKpiDataImportJobById(int id);
        KpiDataImportJob GetLatestKpiDataImportJob(int siteId, DateTime reportDate);
        int SaveKpiDataImportJob(KpiDataImportJob importJob);
    }

    public class ImportJobDataProvider : IImportJobDataProvider
    {
        private readonly CityWatchDbContext _context;

        public ImportJobDataProvider(CityWatchDbContext context)
        {
            _context = context;
        }

        public KpiDataImportJob GetLatestKpiDataImportJob(int siteId, DateTime reportDate)
        {
            return _context.KpiDataImportJobs
                .Where(x => x.ClientSiteId == siteId && x.ReportDate == reportDate)
                .OrderByDescending(z => z.CreatedDate)
                .FirstOrDefault();
        }

        public List<KpiDataImportJob> GetKpiDataImportJobs()
        {
            return _context.KpiDataImportJobs
                .Where(z=> z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderByDescending(x => x.CreatedDate)
                .ToList();
        }

        public KpiDataImportJob GetKpiDataImportJobById(int id)
        {
            return _context.KpiDataImportJobs
                .Where(z=> z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .SingleOrDefault(x => x.Id == id);
        }

        public int SaveKpiDataImportJob(KpiDataImportJob importJob)
        {
            if (importJob == null)
                throw new ArgumentNullException("KpiDataImportJob");

            if (importJob.Id != 0)
            {
                var serviceToUpdate = _context.KpiDataImportJobs.SingleOrDefault(x => x.Id == importJob.Id);
                if (serviceToUpdate != null)
                {
                    serviceToUpdate.CompletedDate = importJob.CompletedDate;
                    serviceToUpdate.Success = importJob.Success;
                }
            }
            else
            {
                _context.KpiDataImportJobs.Add(importJob);
            }
            _context.SaveChanges();
            
            return importJob.Id;
        }
    }
}
