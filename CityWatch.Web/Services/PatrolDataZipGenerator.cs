using CityWatch.Web.Models;
using System.IO;
using System;
using CityWatch.Common.Services;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using CityWatch.Data.Models;
using System.Threading.Tasks;
using System.Linq;
using CityWatch.Common.Helpers;
using System.IO.Compression;
using CityWatch.Data.Services;

namespace CityWatch.Web.Services
{
    public interface IPatrolDataZipGenerator
    {
        string GenerateZipFile(PatrolRequest patrolRequest);
        Task<string> GenerateZipFile1(PatrolRequest patrolRequest);

    }
    public class PatrolDataZipGenerator : IPatrolDataZipGenerator
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardLogReportGenerator _guardLogReportGenerator;
        private readonly IKeyVehicleLogReportGenerator _keyVehicleLogReportGenerator;
        private readonly IDropboxService _dropboxService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IPatrolDataReportService _irChartDataService;

        private readonly string _downloadsFolderPath;
        private readonly IGuardLogDataProvider _guardLogDataProvider;

        public PatrolDataZipGenerator(IClientDataProvider clientDataProvider,
            IGuardLogReportGenerator guardLogReportGenerator,
            IKeyVehicleLogReportGenerator keyVehicleLogReportGenerator,
            IDropboxService dropboxService,
            IWebHostEnvironment webHostEnvironment,
             IGuardLogDataProvider guardLogDataProvider,
             IPatrolDataReportService irChartDataService

             )
        {
            _clientDataProvider = clientDataProvider;
            _guardLogReportGenerator = guardLogReportGenerator;
            _keyVehicleLogReportGenerator = keyVehicleLogReportGenerator;
            _dropboxService = dropboxService;
            _webHostEnvironment = webHostEnvironment;
            _guardLogDataProvider = guardLogDataProvider;
            _irChartDataService = irChartDataService;

            _downloadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
        }
        public string GenerateZipFile(PatrolRequest patrolRequest)
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(patrolRequest);
       
            if (patrolDataReport.ResultsCount <= 0)
            {
                return string.Empty;
            }
            var zipFolderPath = GetZipFolderPath();
            var fileNamePart = string.Empty;
            //var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds).Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir)).ToList();
            if (patrolDataReport.ResultsCount > 0)
            {
                var results = patrolDataReport.Results;

                //return string.Empty;
                /* No DropboxImagesDir set for these sites 06102023*/
                // var clientSiteDetails = _clientDataProvider.GetClientSiteDetails(clientSiteIds);
                fileNamePart = results[0].fileNametodownload;
                foreach (var newresults in results)
                {
                    var fileName = newresults.fileNametodownload;
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        

                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                         File.Copy(filePath, Path.Combine(zipFolderPath, fileName));
                         File.Delete(filePath);

                        //var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "PatrolData", fileName);
                        //var directoryPath = Path.GetDirectoryName(filePath);
                        //if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                        //{
                        //    Directory.CreateDirectory(directoryPath);
                        //}
                        //if (File.Exists(filePath))
                        //{
                        //    File.Delete(filePath);
                        //}

                        //if (System.IO.File.Exists(filePath))
                        //{
                        //    var dropBoxFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "ToDropbox");
                        //    if (!Directory.Exists(dropBoxFolderPath))
                        //        Directory.CreateDirectory(dropBoxFolderPath);
                        //    System.IO.File.Move(filePath, Path.Combine(dropBoxFolderPath, fileName), true);
                        //}

                        // File.Copy(reportFilePath, Path.Combine(zipFolderPath, fileName));
                        // File.Delete(reportFilePath);

                        // Now you can create or write to the file without issues
                    }
                }
            }


            return GetZipFileName(zipFolderPath, patrolRequest.FromDate, patrolRequest.ToDate, fileNamePart);
        }

        public async Task<string> GenerateZipFile1(PatrolRequest patrolRequest)
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(patrolRequest);
            if (patrolDataReport.ResultsCount <= 0)
            {
                return string.Empty;
            }
            var zipFolderPath = GetZipFolderPath();
            var fileNamePart = string.Empty;
            //var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds).Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir)).ToList();
            if (patrolDataReport.ResultsCount>0)
            {
                var results = patrolDataReport.Results;

                //return string.Empty;
                /* No DropboxImagesDir set for these sites 06102023*/
               // var clientSiteDetails = _clientDataProvider.GetClientSiteDetails(clientSiteIds);
                fileNamePart = results[0].fileNametodownload;
                foreach (var newresults in results)
                {
                    var fileName = newresults.fileNametodownload;
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var reportFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                        File.Copy(reportFilePath, Path.Combine(zipFolderPath, fileName));
                        File.Delete(reportFilePath);
                    }
                }
            }
           

            return GetZipFileName(zipFolderPath, patrolRequest.FromDate, patrolRequest.ToDate, fileNamePart);
        }

        private string GetZipFolderPath()
        {
            var zipFolderPath = Path.Combine(_downloadsFolderPath, Guid.NewGuid().ToString());
            if (!Directory.Exists(zipFolderPath))
                Directory.CreateDirectory(zipFolderPath);
            return zipFolderPath;
        }
        private string GetZipFileName(string zipFolderPath, DateTime logFromDate, DateTime logToDate, string fileNamePart)
        {
            var zipFileName = $"{FileNameHelper.GetSanitizedFileNamePart(fileNamePart)}_{logFromDate:yyyyMMdd}_{logToDate:yyyyMMdd}_{new Random().Next(100, 999)}.zip";
            ZipFile.CreateFromDirectory(zipFolderPath, Path.Combine(_downloadsFolderPath, zipFileName), CompressionLevel.Optimal, false);

            if (!Directory.Exists(zipFolderPath))
                Directory.Delete(zipFolderPath);

            return zipFileName;
        }
    }
}
