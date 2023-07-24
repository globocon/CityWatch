using CityWatch.Data.Helpers;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace CityWatch.Web.Services
{
    public interface ICleanupService
    {
        void DeleteArchivedPdfs();

        void RecreateGpsImage();

        void DeleteLogs();

        void DeletePatrolDataExcels();
    }

    public class CleanupService : ICleanupService
    {
        private const int ARCHIVE_DAYS = 7;
        private const int LOG_FILE_DAYS = 14;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IClientDataProvider _clientDataProvider;

        public CleanupService(IWebHostEnvironment webHostEnvironment,
                              IConfiguration configuration,
                              IClientDataProvider clientDataProvider)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _clientDataProvider = clientDataProvider;
        }

        public void DeleteArchivedPdfs()
        {
            var archiveFolder = new DirectoryInfo(Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Archive"));
            var pdfFiles = archiveFolder.GetFiles("*.pdf").Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * ARCHIVE_DAYS));
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

        public void DeletePatrolDataExcels()
        {
            var dataFolder = new DirectoryInfo(Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output"));
            var patrolDataExcel = dataFolder.GetFiles("*.xlsx").Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * ARCHIVE_DAYS));
            if (patrolDataExcel.Any())
            {
                foreach (var file in patrolDataExcel)
                    file.Delete();
            }
        }

        public void RecreateGpsImage()
        {
            var clients = _clientDataProvider.GetClientSites(null).Where(x => !string.IsNullOrEmpty(x.Gps));
            var gpsImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage");
            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            foreach (var client in clients)
            {
                try
                {
                    GoogleMapHelper.DownloadGpsImage(gpsImageDir, client, mapSettings);
                }
                catch
                { }
            }
        }
    }
}
