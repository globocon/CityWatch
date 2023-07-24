using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;

namespace CityWatch.Kpi.Services
{
    public interface ICleanupService
    {
        void DeleteKpiReports();        
        void DeleteLogs();     
    }

    public class CleanupService : ICleanupService
    {
        private const int ARCHIVE_DAYS = 7;
        private const int LOG_FILE_DAYS = 14;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CleanupService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;    
        }

        public void DeleteKpiReports()
        {
            var KpiReports = new DirectoryInfo(Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output"));
            var pdfFiles = KpiReports.GetFiles("*.pdf").Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * ARCHIVE_DAYS));
            if (pdfFiles.Any())
            {
                foreach (var file in pdfFiles)
                    file.Delete();
            }
        }

        public void DeleteLogs()
        {
            var logFolder = new DirectoryInfo(Path.Combine(_webHostEnvironment.ContentRootPath, "Logs"));
            var logFiles = logFolder.GetFiles("*.log").Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * LOG_FILE_DAYS));
            if (logFiles.Any())
            {
                foreach (var file in logFiles)
                    file.Delete();
            }
        }   
    }
}
