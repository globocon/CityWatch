using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    public interface IIrUploadService
    {
        Task Process();
    }

    public class IrUploadService : IIrUploadService
    {
        private readonly IIrDataProvider _irDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<IrUploadService> _logger;
        private readonly Settings _settings;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IDropboxService _dropboxService;
        private readonly string _ReportRootDir;
        private readonly IEnumerable<IncidentReportPosition> _incidentReportPositions;
        private readonly IEnumerable<ClientSiteKpiSetting> _clientSiteKpiSettings;

        public IrUploadService(IIrDataProvider irDataProvider,
            IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            ILogger<IrUploadService> logger,
            IOptions<Settings> settings,
            IDropboxService dropboxService,
            IConfigDataProvider configDataProvider)
        {
            _irDataProvider = irDataProvider;
            _clientDataProvider = clientDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _settings = settings.Value;
            _dropboxService = dropboxService;
            _configDataProvider = configDataProvider;

            _ReportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf");
            _incidentReportPositions = _configDataProvider.GetPositions().Where(z => z.IsPatrolCar);
            _clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSettings();
        }

        public async Task Process()
        {
            /* Update client site expiring to expire automatically Start*/
            await UpdateExpirySites();
            /* Update client site expiring to expire automatically end*/

            var irsToProcess = _irDataProvider.GetIncidentReports(DateTime.Now.AddDays(-7), DateTime.Today)
                                    .Where(i => !i.DbxUploaded && i.ClientSiteId.HasValue)
                                    .ToList();

            if (irsToProcess.Any())
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = $"IR Report Upload Started. Records to process: {irsToProcess.Count}" });
            }

            foreach (var incidentReport in irsToProcess)
            {
                if (string.IsNullOrEmpty(incidentReport.FileName) || incidentReport.FileName.Length < 8 || !DateTime.TryParseExact(incidentReport.FileName.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime irDate))
                    continue;

                try
                {
                    await ProcessIncidentReportUpload(incidentReport, irDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError("FATAL Error processing IR {0}: {1}", incidentReport.FileName, ex.Message);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "FATAL Error processing IR " + incidentReport.FileName + ": " + ex.Message });
                }
            }

            if (irsToProcess.Any())
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "IR Report Upload Ended." });
            }
        }

        private async Task ProcessIncidentReportUpload(IncidentReport incidentReport, DateTime irDate)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                        _settings.DropboxRefreshToken, _settings.DropboxUserEmail);
            var fileToUpload = Path.Combine(_ReportRootDir, "ToDropbox", incidentReport.FileName);

            if (incidentReport.IsPatrol)
            {
                var positionDbxBasePath = _incidentReportPositions.SingleOrDefault(z => z.Name == incidentReport.Position)?.DropboxDir;
                if (!string.IsNullOrEmpty(positionDbxBasePath))
                {
                    var patrolsUploadPath = $"{positionDbxBasePath}/{irDate.Year}/{irDate:yyyyMM} - {irDate.ToString("MMMM").ToUpper()} DATA/{incidentReport.FileName}";
                    try
                    {
                        await _dropboxService.Upload(dropboxSettings, fileToUpload, patrolsUploadPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error uploading IR {0} to patrols folder, Message : {1}", incidentReport.FileName, ex.Message);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "IR Patrol Upload Error for " + incidentReport.FileName + ": " + ex.Message });
                    }
                }
            }

            var siteDbxBasePath = _clientSiteKpiSettings.SingleOrDefault(z => z.ClientSiteId == incidentReport.ClientSiteId)?.DropboxImagesDir;
            if (!string.IsNullOrEmpty(siteDbxBasePath))
            {
             
             
                var siteUploadPath = $"{siteDbxBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{irDate.Year}/{irDate:yyyyMM} - {irDate.ToString("MMMM").ToUpper()} DATA/{incidentReport.FileName}";
                try
                {
                    var kpisetting = _clientSiteKpiSettings.SingleOrDefault(z => z.ClientSiteId == incidentReport.ClientSiteId);
                    if (kpisetting.DropboxScheduleisActive)
                    {
                        var irUploaded = await _dropboxService.Upload(dropboxSettings, fileToUpload, siteUploadPath);
                        if (irUploaded)
                        {
                            _irDataProvider.MarkAsUploaded(incidentReport.Id);
                            File.Move(fileToUpload, Path.Combine(_ReportRootDir, "Archive", incidentReport.FileName), true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error uploading IR {0} to client site folder , Message : {1}", incidentReport.FileName, ex.Message);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "IR Client Site Upload Error for " + incidentReport.FileName + ": " + ex.Message });
                }
            }
        }



        private async Task UpdateExpirySites()
        {
             _irDataProvider.UpdateTheSiteExpiringToExpired();

        }
    }
}
