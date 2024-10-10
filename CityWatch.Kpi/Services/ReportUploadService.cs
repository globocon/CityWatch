using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Helpers;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace CityWatch.Kpi.Services
{
    public interface IReportUploadService
    {
        Task<bool> ProcessUpload(DateTime reportFromDate);
        Task<bool> ProcessUploadTimesheet(DateTime reportFromDate);
    }

    public class ReportUploadService : IReportUploadService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IReportGenerator _reportGenerator;
        private readonly IDropboxService _dropboxUploadService;
        private readonly IImportJobDataProvider _importJobDataProvider;
        private readonly IImportDataService _importDataService;
        private readonly Settings _settings;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ReportUploadService> _logger;
        private readonly string _reportRootDir;
        private readonly ITimesheetGenerator _kpiTimesheetReportGenerator;

        public ReportUploadService(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IReportGenerator reportGenerator,
            IDropboxService dropboxUploadService,
            IImportJobDataProvider importJobDataProvider,
            IImportDataService importDataService,
            IOptions<Settings> settings,
            ILogger<ReportUploadService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _clientDataProvider = clientDataProvider;
            _reportGenerator = reportGenerator;
            _dropboxUploadService = dropboxUploadService;
            _importJobDataProvider = importJobDataProvider;
            _importDataService = importDataService;
            _settings = settings.Value;
            _logger = logger;
            _reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf");
        }

        public async Task<bool> ProcessUpload(DateTime reportFromDate)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSettings().Where(x => !string.IsNullOrEmpty(x.DropboxImagesDir));
            foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
            {
                try
                {
                    var serviceLog = new KpiDataImportJob()
                    {
                        ClientSiteId = clientSiteKpiSetting.ClientSiteId,
                        ReportDate = reportFromDate,
                        CreatedDate = DateTime.Now,
                    };
                    var jobId = _importJobDataProvider.SaveKpiDataImportJob(serviceLog);
                    await _importDataService.Run(jobId);

                    var fileName = _reportGenerator.GeneratePdfReport(clientSiteKpiSetting.ClientSiteId, reportFromDate, reportFromDate.AddMonths(1).AddDays(-1));
                    var fileToUpload = Path.Combine(_reportRootDir, "Output", fileName);
                    var dbxFilePath = $"{clientSiteKpiSetting.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportFromDate.Date.Year}/{reportFromDate.Date:yyyyMM} - {reportFromDate.Date.ToString("MMMM").ToUpper()} DATA/x - Site KPI Telematics & Statistics/{clientSiteKpiSetting.ClientSite.Name} - Daily KPI Reports - {reportFromDate.Date:MMM yyyy}.pdf";

                    await _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath);

                    await CreateExtraDropboxFolders(clientSiteKpiSetting, dropboxSettings, reportFromDate);
                    await CreateCustomDropboxFolders(clientSiteKpiSetting, dropboxSettings, reportFromDate);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.StackTrace);
                }
            }

            return true;
        }

        public async Task<bool> ProcessUploadTimesheet(DateTime reportFromDate)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSettings().Where(x => !string.IsNullOrEmpty(x.DropboxImagesDir));
            foreach (var clientSiteKpiSetting in clientSiteKpiSettings)
            {
                try
                {
                    var serviceLog = new KpiDataImportJob()
                    {
                        ClientSiteId = clientSiteKpiSetting.ClientSiteId,
                        ReportDate = reportFromDate,
                        CreatedDate = DateTime.Now,
                    };
                    var jobId = _importJobDataProvider.SaveKpiDataImportJob(serviceLog);
                    await _importDataService.Run(jobId);

                    var reportEndDate = reportFromDate.AddMonths(1).AddDays(-1);
                    string StartDate = reportFromDate.ToString("MM/dd/yyyy");
                    string EndDate = "";
                    if (reportEndDate != null)
                    {
                        EndDate = reportEndDate.ToString("MM/dd/yyyy");
                    }

                    var clientSiteDetails = _clientDataProvider.GetGuardDetailsAllTimesheet(clientSiteKpiSetting.ClientSiteId, StartDate, EndDate);
                    var fileName = _kpiTimesheetReportGenerator.GeneratePdfTimesheetReport(reportFromDate, reportEndDate, clientSiteDetails.GuardId);
                    //var fileName = _reportGenerator.GeneratePdfReport(clientSiteKpiSetting.ClientSiteId, reportFromDate, reportFromDate.AddMonths(1).AddDays(-1));
                    var fileToUpload = Path.Combine(_reportRootDir, "Output", fileName);
                    var dbxFilePath = $"{clientSiteKpiSetting.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportFromDate.Date.Year}/{reportFromDate.Date:yyyyMM} - {reportFromDate.Date.ToString("MMMM").ToUpper()} DATA/x - Site KPI Telematics & Statistics/{clientSiteKpiSetting.ClientSite.Name} - Daily KPI Reports - {reportFromDate.Date:MMM yyyy}.pdf";

                    await _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath);

                    await CreateExtraDropboxFolders(clientSiteKpiSetting, dropboxSettings, reportFromDate);
                    await CreateCustomDropboxFolders(clientSiteKpiSetting, dropboxSettings, reportFromDate);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.StackTrace);
                }
            }

            return true;
        }

        private async Task CreateExtraDropboxFolders(ClientSiteKpiSetting clientSiteKpiSetting, DropboxSettings dropboxSettings, DateTime reportFromDate)
        {
            if (clientSiteKpiSetting.MonthlyClientReport == true)
            {
                var extraDbxFolderName = "x - Monthly Client Report";
                var extraDbxFolderPath = $"{clientSiteKpiSetting.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportFromDate.Date.Year}/{reportFromDate.Date:yyyyMM} - {reportFromDate.Date.ToString("MMMM").ToUpper()} DATA/";
                var dbxfldr = $"{extraDbxFolderPath}{extraDbxFolderName}";
                try
                {
                    await _dropboxUploadService.CreateFolder(dropboxSettings, dbxfldr);
                    _logger.LogInformation($"Extra dropbox folder {dbxfldr} created.");
                }
                catch (Exception exp)
                {
                    _logger.LogError(exp.Message);
                    _logger.LogError(exp.InnerException.ToString());
                }
            }

        }
        private async Task CreateCustomDropboxFolders(ClientSiteKpiSetting clientSiteKpiSetting, DropboxSettings dropboxSettings, DateTime reportFromDate)
        {
            var customDbxFolderPath = $"{clientSiteKpiSetting.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportFromDate.Date.Year}/{reportFromDate.Date:yyyyMM} - {reportFromDate.Date.ToString("MMMM").ToUpper()} DATA/";
            var customdropboxfolders = _clientDataProvider.GetKpiSettingsCustomDropboxFolder(clientSiteKpiSetting.ClientSiteId).ToList();
            if (customdropboxfolders.Count > 0)
            {
                foreach (var customdropboxfolder in customdropboxfolders)
                {
                    var dbxfldr = $"{customDbxFolderPath}{customdropboxfolder.DropboxFolderName}";
                    try
                    {
                        await _dropboxUploadService.CreateFolder(dropboxSettings, dbxfldr);
                        _logger.LogInformation($"Custom dropbox folder {dbxfldr} created.");
                    }
                    catch (Exception exp)
                    {
                        _logger.LogError(exp.Message);
                        _logger.LogError(exp.InnerException.ToString());
                    }

                }
            }

        }
    }
}
