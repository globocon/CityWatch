﻿using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    public interface IGuardLogZipGenerator
    {
        Task<string> GenerateZipFile(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate, LogBookType logBookType);
        Task<string> GenerateFusionZipFile(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate, LogBookType logBookType);
        string GenerateZipFile(KeyVehicleLogAuditLogRequest kvlAuditLogRequest);
    }

    public class GuardLogZipGenerator : IGuardLogZipGenerator
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardLogReportGenerator _guardLogReportGenerator;
        private readonly IKeyVehicleLogReportGenerator _keyVehicleLogReportGenerator;
        private readonly IDropboxService _dropboxService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Settings _settings;
        private readonly string _downloadsFolderPath;
        private readonly IGuardLogDataProvider _guardLogDataProvider;

        public GuardLogZipGenerator(IClientDataProvider clientDataProvider,
            IGuardLogReportGenerator guardLogReportGenerator,
            IKeyVehicleLogReportGenerator keyVehicleLogReportGenerator,
            IDropboxService dropboxService,
            IWebHostEnvironment webHostEnvironment,
             IGuardLogDataProvider guardLogDataProvider,
            IOptions<Settings> settings)
        {
            _clientDataProvider = clientDataProvider;
            _guardLogReportGenerator = guardLogReportGenerator;
            _keyVehicleLogReportGenerator = keyVehicleLogReportGenerator;
            _dropboxService = dropboxService;
            _webHostEnvironment = webHostEnvironment;
            _guardLogDataProvider = guardLogDataProvider;
            _settings = settings.Value;
            _downloadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
        }

        public async Task<string> GenerateZipFile(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate, LogBookType logBookType)
        {
            if (clientSiteIds.Length <= 0)
            {
                return string.Empty;
            }
            var zipFolderPath = GetZipFolderPath();
            var fileNamePart = string.Empty;
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds).Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir)).ToList();
            if (!clientSiteKpiSettings.Any())
            {
                //return string.Empty;
                /* No DropboxImagesDir set for these sites 06102023*/
                var clientSiteDetails = _clientDataProvider.GetClientSiteDetails(clientSiteIds);
                fileNamePart = clientSiteDetails[0].Name;
                foreach (var clientSiteDetail in clientSiteDetails)
                {
                    var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooks(clientSiteDetail.Id, logBookType, logFromDate, logToDate);
                    if (!clientSiteLogBooks.Any())
                        continue;
                    var logbooksToCreate = GetLogBooksFailedToDownload(clientSiteLogBooks, zipFolderPath);
                    CreateLogBookReports(logbooksToCreate, zipFolderPath);
                }
            }
            else
            {
                /* DropboxImagesDir set for these sites*/
                fileNamePart = clientSiteKpiSettings[0].ClientSite.Name;
                foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
                {
                    var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooks(clientSiteKpiSetting.ClientSiteId, logBookType, logFromDate, logToDate);
                    if (!clientSiteLogBooks.Any())
                        continue;
                    if (clientSiteKpiSetting.DropboxImagesDir != string.Empty)
                    {
                        await DownloadLogBooksFromDropbox(clientSiteLogBooks, zipFolderPath, clientSiteKpiSetting.DropboxImagesDir);
                    }

                    var logbooksToCreate = GetLogBooksFailedToDownload(clientSiteLogBooks, zipFolderPath);
                    CreateLogBookReports(logbooksToCreate, zipFolderPath);
                }

            }

            return GetZipFileName(zipFolderPath, logFromDate, logToDate, fileNamePart);
        }

        public string GenerateZipFile(KeyVehicleLogAuditLogRequest kvlAuditLogRequest)
        {
            if (kvlAuditLogRequest.ClientSiteIds.Length <= 0)
            {
                return string.Empty;
            }

            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(kvlAuditLogRequest.ClientSiteIds).ToList();
            if (!clientSiteKpiSettings.Any())
            {
                return string.Empty;
            }

            var zipFolderPath = GetZipFolderPath();
            var fileNamePart = clientSiteKpiSettings.Count > 1 ? "Multiple Sites" : clientSiteKpiSettings[0].ClientSite.Name;

            foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
            {
                var clientSiteLogBooks = _clientDataProvider.GetClientSiteLogBooks(clientSiteKpiSetting.ClientSiteId, kvlAuditLogRequest.LogBookType, kvlAuditLogRequest.LogFromDate, kvlAuditLogRequest.LogToDate);
                if (!clientSiteLogBooks.Any())
                    continue;

                CreateLogBookReports(clientSiteLogBooks.Select(z => z.Id).ToList(), zipFolderPath, kvlAuditLogRequest);
            }

            return GetZipFileName(zipFolderPath, kvlAuditLogRequest.LogFromDate, kvlAuditLogRequest.LogToDate, fileNamePart);
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

        private async Task DownloadLogBooksFromDropbox(List<ClientSiteLogBook> clientSiteLogBooks, string zipFolderPath, string dropboxImagesDir)
        {
            var filesToDownload = clientSiteLogBooks
                            .Where(z => !string.IsNullOrEmpty(z.FileName))
                            .Select(z => GetDailyLogBookName(dropboxImagesDir, z))
                            .ToList();

            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                _settings.DropboxRefreshToken, _settings.DropboxUserEmail);
            await _dropboxService.Download(dropboxSettings, zipFolderPath, filesToDownload.ToArray());
        }

        private static List<ClientSiteLogBook> GetLogBooksFailedToDownload(List<ClientSiteLogBook> clientSiteLogBooks, string zipFolderPath)
        {
            var logBooksToCreate = clientSiteLogBooks
                            .Where(z => string.IsNullOrEmpty(z.FileName))
                            .ToList();

            var logBooksToDownload = clientSiteLogBooks
                            .Where(z => !string.IsNullOrEmpty(z.FileName))
                            .ToList();

            foreach (var logBook in logBooksToDownload)
            {
                if (!File.Exists(Path.Combine(zipFolderPath, logBook.FileName)))
                {
                    logBooksToCreate.Add(logBook);
                }
            }

            return logBooksToCreate;
        }

        private void CreateLogBookReports(List<ClientSiteLogBook> logBooksToCreate, string zipFolderPath)
        {
            foreach (var logBook in logBooksToCreate)
            {
                var fileName = GetLogFileName(logBook);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                    File.Copy(reportFilePath, Path.Combine(zipFolderPath, fileName));
                    File.Delete(reportFilePath);
                }
            }
        }

        private void CreateLogBookReports(List<int> logBookIds, string zipFolderPath, KeyVehicleLogAuditLogRequest kvlAuditLogRequest)
        {
            foreach (var logBookId in logBookIds)
            {
                var fileName = GetLogFileName(logBookId, kvlAuditLogRequest);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                    File.Copy(reportFilePath, Path.Combine(zipFolderPath, fileName));
                    File.Delete(reportFilePath);
                }
            }
        }

        private static string GetDailyLogBookName(string dropboxImagesDir, ClientSiteLogBook clientSiteLogBook)
        {
            return $"{dropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{clientSiteLogBook.Date.Year}/{clientSiteLogBook.Date:yyyyMM} - {clientSiteLogBook.Date.ToString("MMMM").ToUpper()} DATA/{clientSiteLogBook.Date:yyyyMMdd}/{clientSiteLogBook.FileName}";
        }

        private string GetLogFileName(ClientSiteLogBook logBook)
        {
            string fileName = string.Empty;

            if (logBook.Type == LogBookType.DailyGuardLog)
                return _guardLogReportGenerator.GeneratePdfReport(logBook.Id);

            if (logBook.Type == LogBookType.VehicleAndKeyLog)
                return _keyVehicleLogReportGenerator.GeneratePdfReport(logBook.Id);

            return fileName;
        }

        private string GetLogFileName(int logBookId, KeyVehicleLogAuditLogRequest kvlAuditLogRequest)
        {
            return _keyVehicleLogReportGenerator.GeneratePdfReport(logBookId, kvlAuditLogRequest);
        }


        /* Fusion Report download */
        public async Task<string> GenerateFusionZipFile(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate, LogBookType logBookType)
        {
            if (clientSiteIds.Length <= 0)
            {
                return string.Empty;
            }
            var zipFolderPath = GetZipFolderPath();
            var fileNamePart = string.Empty;
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteIds).Where(z => !string.IsNullOrEmpty(z.DropboxImagesDir)).ToList();
            //Old Code Start
            //if (!clientSiteKpiSettings.Any())
            //{
            //    //return string.Empty;
            //    /* No DropboxImagesDir set for these sites 06102023*/
            //    var clientSiteDetails = _clientDataProvider.GetClientSiteDetails(clientSiteIds);
            //    fileNamePart = clientSiteDetails[0].Name;
            //    foreach (var clientSiteDetail in clientSiteDetails)
            //    {
            //        var clientSiteLogBooks = _clientDataProvider.GetClientSiteFunsionLogBooks(clientSiteDetail.Id, logBookType, logFromDate, logToDate);
            //        //if (!clientSiteLogBooks.Any())
            //        //    continue;
            //        //var logbooksToCreate = GetLogBooksFailedToDownloadFusion(clientSiteLogBooks, zipFolderPath);
            //        CreateLogBookReportsFusion(clientSiteLogBooks, zipFolderPath);
            //    }
            //}
            //else
            //{
            //    /* DropboxImagesDir set for these sites*/
            //    fileNamePart = clientSiteKpiSettings[0].ClientSite.Name;
            //    foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
            //    {
            //        var clientSiteLogBooks = _clientDataProvider.GetClientSiteFunsionLogBooks(clientSiteKpiSetting.ClientSiteId, logBookType, logFromDate, logToDate);
            //        //if (!clientSiteLogBooks.Any())
            //        //    continue;
            //        //if (clientSiteKpiSetting.DropboxImagesDir != string.Empty)
            //        //{
            //        //    await DownloadLogBooksFromDropbox(clientSiteLogBooks, zipFolderPath, clientSiteKpiSetting.DropboxImagesDir);
            //        //}

            //        //var logbooksToCreate = GetLogBooksFailedToDownloadFusion(clientSiteLogBooks, zipFolderPath);
            //        CreateLogBookReportsFusion(clientSiteLogBooks, zipFolderPath);
            //    }

            //}
            //Old Code end
            var clientSiteDetails = _clientDataProvider.GetClientSiteDetails(clientSiteIds);
            fileNamePart = clientSiteDetails[0].Name;
            var clientSiteLogBooks = _guardLogDataProvider.GetGuardFusionLogs(clientSiteIds, logFromDate, logToDate, false);
            CreateLogBookReportsFusion(clientSiteLogBooks, zipFolderPath);
           
            return GetZipFileName(zipFolderPath, logFromDate, logToDate, fileNamePart);
        }

        private void CreateLogBookReportsFusion(List<ClientSiteRadioChecksActivityStatus_History> logBooksToCreate, string zipFolderPath)
        {

            var checkGMT = logBooksToCreate
                 .Where(x => x.ActivityType != "SW" && x.EventDateTimeZoneShort != null)
                 .Select(x => x.EventDateTimeZoneShort)
                 .FirstOrDefault();

            if (checkGMT != null)
            {
                logBooksToCreate.ForEach(x =>
                {
                    if (x.EventDateTimeZoneShort == null)
                    {
                        x.EventDateTimeZoneShort = checkGMT;
                        x.EventDateTime = x.LastSWCreatedTime ?? x.EventDateTime;
                        x.EventDateTimeLocal = x.LastSWCreatedTime ?? x.EventDateTime;
                    }
                });

            }

            //notificationCreatedTime

            logBooksToCreate = logBooksToCreate.OrderBy(z => z.EventDateTime).ToList();
            var distinctDatetoCreate = logBooksToCreate.Select(m => m.EventDateTime.Date).Distinct();
            foreach (var eachdate in distinctDatetoCreate)
            {
                var fusionLogToCreate= logBooksToCreate.Where(x=>x.EventDateTime.Date== eachdate).ToList();

                var fileName = GetFusionLogFileName(fusionLogToCreate);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var reportFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                    File.Copy(reportFilePath, Path.Combine(zipFolderPath, fileName));
                    File.Delete(reportFilePath);
                }
            }
        }


        private string GetFusionLogFileName(List<ClientSiteRadioChecksActivityStatus_History> logBook)
        {
            

            
           return _guardLogReportGenerator.GeneratePdfReportForFusion(logBook);

           
        }

    }
}
