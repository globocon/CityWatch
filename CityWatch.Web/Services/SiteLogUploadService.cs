using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CityWatch.Web.Services
{
    public interface ISiteLogUploadService
    {
        void ProcessDailyGuardLogs();
        void ProcessDailyGuardLogsSecondRun();
        public void ProcessDailyGuardLogsNew();
        public void ProcessDailyGuardLogsSecondRunNew();

        /// <summary>
        /// The weekly log dump: one PDF per site per enabled log type, covering the whole of the
        /// previous Monday-to-Sunday week, uploaded to Dropbox and emailed to the weekly recipients.
        /// Driven by the four Upload*WeeklyLog flags on the client site.
        /// </summary>
        void ProcessWeeklyGuardLogs();

        /// <summary>
        /// The monthly log dump. Identical to the weekly one but covering the previous calendar
        /// month and driven by the four Upload*MonthlyLog flags.
        /// </summary>
        void ProcessMonthlyGuardLogs();

    }

    /// <summary>The four log types a site can ask for, paired with the flags that enable each.</summary>
    internal class PeriodicLogSelection
    {
        public LogBookType ReportType { get; init; }

        /// <summary>
        /// The log book the report is generated FROM, which is not always the report's own type:
        /// GeneratePdfReportSmartWand and GeneratePdfReportFusion both take the daily guard log book
        /// id, exactly as the daily run passes it. Fusion additionally falls back to the key and
        /// vehicle log book on days a site has no guard log.
        /// </summary>
        public LogBookType SourceType { get; init; }

        public Func<ClientSite, bool> IsEnabledWeekly { get; init; }

        public Func<ClientSite, bool> IsEnabledMonthly { get; init; }

        public bool IsEnabled(ClientSite site, LogDumpPeriodType periodType) =>
            periodType == LogDumpPeriodType.Weekly ? IsEnabledWeekly(site) : IsEnabledMonthly(site);
    }

    public class SiteLogUploadService : ISiteLogUploadService
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly EmailOptions _emailOptions;
        private readonly IGuardLogReportGenerator _guardLogReportGenerator;
        private readonly IKeyVehicleLogReportGenerator _keyVehicleLogReportGenerator;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<SiteLogUploadService> _logger;
        private readonly Settings _settings;
        private readonly string _reportRootDir;
        private readonly IDropboxService _dropboxUploadService;
        private readonly IConfiguration _configuration;

        public SiteLogUploadService(IClientDataProvider clientDataProvider,
            IGuardLogReportGenerator guardLogReportGenerator,
            IKeyVehicleLogReportGenerator keyVehicleLogReportGenerator,
            IDropboxService dropboxService,
            IOptions<EmailOptions> emailOptions,
            IWebHostEnvironment webHostEnvironment,
            ILogger<SiteLogUploadService> logger,
            IOptions<Settings> settings,
             IConfiguration configuration
            )
        {
            _clientDataProvider = clientDataProvider;
            _guardLogReportGenerator = guardLogReportGenerator;
            _keyVehicleLogReportGenerator = keyVehicleLogReportGenerator;
            _dropboxUploadService = dropboxService;
            _emailOptions = emailOptions.Value;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _settings = settings.Value;
            _reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf");
            _configuration = configuration;
        }

        public void ProcessDailyGuardLogs()
        {
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooks().Where(z => z.ClientSite.UploadGuardLog && z.Date == DateTime.Now.AddDays(-1).Date && !z.DbxUploaded);
            foreach (var siteLogBook in siteLogBooksToUpload)
            {
                try
                {
                    string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;

                    var fileToUpload = Path.Combine(_reportRootDir, "Output", logFileName);

                    var uploaded = ProcessDailyGuardLogUpload(siteLogBook, fileToUpload);

                    if (uploaded)
                    {
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);
                        _clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);
                    }

                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Daily Guard Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                }
            }
        }
        public void ProcessDailyGuardLogsSecondRun()
        {
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooks().Where(z => z.ClientSite.UploadGuardLog && z.Date == DateTime.Now.Date && !z.DbxUploaded);
            foreach (var siteLogBook in siteLogBooksToUpload)
            {
                try
                {
                    string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;

                    var fileToUpload = Path.Combine(_reportRootDir, "Output", logFileName);

                    var uploaded = ProcessDailyGuardLogUpload(siteLogBook, fileToUpload);

                    if (uploaded)
                    {
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        //No need to set dbxuploaded to true in second run-26-10-2023
                        //_clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);
                    }


                }
                catch (Exception ex)
                {
                    _logger.LogError($"Daily Guard Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                }
            }
        }


        public void ProcessDailyGuardLogsNew()
        {

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler Start---" });
            // Retrieve and filter the log books to upload only once.
            // Reverted back to -1 to fix the daily guard log scheduling issue
            var yesterday = DateTime.Now.AddDays(-1).Date;
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooksForDailyLogBookGeneration(yesterday);

            // Check if there are any logs to process to avoid unnecessary operations.
            if (!siteLogBooksToUpload.Any())
                return;

            // Cache the report directory path.
            var outputDirectory = Path.Combine(_reportRootDir, "Output");

            var _guardLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadGuardLog && x.Type == LogBookType.DailyGuardLog).ToList();
            var _keyVechileLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadKVLog && x.Type == LogBookType.VehicleAndKeyLog).ToList();
            var _smartWandDailyGuardLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadSWLog && x.Type == LogBookType.DailyGuardLog).ToList(); // Since Smartwand logs are in guard logs
            var _fusionLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadFusionLog && (x.Type == LogBookType.DailyGuardLog || x.Type == LogBookType.VehicleAndKeyLog)).DistinctBy(x => x.ClientSiteId).ToList();

            var _totalLogBooksToProcess = _fusionLogs.Count + _guardLogs.Count + _keyVechileLogs.Count  + _smartWandDailyGuardLogs.Count;

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Number of logbook to upload " + _totalLogBooksToProcess.ToString() });


            //************ Guard LogBook Start ***********

            foreach (var siteLogBook in _guardLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start Guard Logbook" + siteLogBook.ClientSite.Name + "----" });

                    string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "GuardlogBook :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id });
                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    _clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                    // Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);

                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Guard Logbook upload end " + siteLogBook.ClientSite.Name + "----" });

                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Daily Guard Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Guard LogBook End ***********


            //************ Key & Vechile LogBook Start ***********
            foreach (var siteLogBook in _keyVechileLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start Key & Vechile Logbook" + siteLogBook.ClientSite.Name + "----" });

                    string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "KeyVechilelogBook :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id });
                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    _clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                    // Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);

                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Key & Vechile Logbook upload end " + siteLogBook.ClientSite.Name + "----" });

                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Key & Vechile Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Key & Vechile LogBook End ***********


            //************ Smart Wand LogBook Start ***********
            foreach (var siteLogBook in _smartWandDailyGuardLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start Smart Wand Log" + siteLogBook.ClientSite.Name + "----" });

                    var _smartWandLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadSWLog && x.Type == LogBookType.SmartWandLog && x.Date == siteLogBook.Date).FirstOrDefault();
                    if(_smartWandLogs == null)
                    {
                        //Create logbook if not exists
                        var newSWClientSiteLogBook = new ClientSiteLogBook()
                        {
                            ClientSiteId = siteLogBook.ClientSiteId,
                            Type = LogBookType.SmartWandLog,
                            Date = siteLogBook.Date,
                            DbxUploaded = false
                        };
                        var newSWLogBookId = _clientDataProvider.SaveClientSiteLogBook(newSWClientSiteLogBook);
                        _smartWandLogs = _clientDataProvider.GetClientSiteLogBook(siteLogBook.ClientSiteId, LogBookType.SmartWandLog, siteLogBook.Date);
                    }
                    //siteLogBook.Type = LogBookType.SmartWandLog;
                    string logFileName = GetLogFilePath(siteLogBook, LogBookType.SmartWandLog);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBooksmartwand :" + siteLogBook.ClientSite.Name + " LogBookId:" + siteLogBook.Id + " SmartWandLogBookId:" + _smartWandLogs.Id });

                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    _clientDataProvider.MarkClientSiteLogBookAsUploaded(_smartWandLogs.Id, logFileName);



                    //Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);


                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Smart Wand upload end " + siteLogBook.ClientSite.Name + "----" });



                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Fusion Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Smart Wand LogBook End ***********


            //************ Fusion LogBook Start ***********
            foreach (var siteLogBook in _fusionLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start fusion" + siteLogBook.ClientSite.Name + "----" });
                    var _fusionLogbook = siteLogBooksToUpload.Where(x => x.ClientSite.UploadFusionLog && x.Type == LogBookType.FusionLog && x.Date == siteLogBook.Date).FirstOrDefault();
                    if (_fusionLogbook == null)
                    {
                        //Create logbook if not exists
                        var newFusionClientSiteLogBook = new ClientSiteLogBook()
                        {
                            ClientSiteId = siteLogBook.ClientSiteId,
                            Type = LogBookType.FusionLog,
                            Date = siteLogBook.Date,
                            DbxUploaded = false
                        };
                        var newfusionLogBookId = _clientDataProvider.SaveClientSiteLogBook(newFusionClientSiteLogBook);
                        _fusionLogbook = _clientDataProvider.GetClientSiteLogBook(siteLogBook.ClientSiteId, LogBookType.FusionLog, siteLogBook.Date);
                    }

                    string logFileName = GetLogFilePath(siteLogBook, LogBookType.FusionLog);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBookfusion :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id + " FusionLogBookId:" + _fusionLogbook.Id });

                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    _clientDataProvider.MarkClientSiteLogBookAsUploaded(_fusionLogbook.Id, logFileName);



                    //Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);


                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Fusion upload end " + siteLogBook.ClientSite.Name + "----" });



                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Fusion Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Fusion LogBook End ***********


            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler end---" });
        }

        public void ProcessDailyGuardLogsSecondRunNew()
        {

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler Start Second run---" });
            // Retrieve and filter the log books to upload only once.
            var today = DateTime.Now.Date;
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooksForDailyLogBookGeneration(today);
            // Check if there are any logs to process to avoid unnecessary operations.
            if (!siteLogBooksToUpload.Any())
                return;

            // Cache the report directory path.
            var outputDirectory = Path.Combine(_reportRootDir, "Output");

            var _guardLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadGuardLog && x.Type == LogBookType.DailyGuardLog).ToList();
            var _keyVechileLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadKVLog && x.Type == LogBookType.VehicleAndKeyLog).ToList();
            var _smartWandDailyGuardLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadSWLog && x.Type == LogBookType.DailyGuardLog).ToList(); // Since Smartwand logs are in guard logs
            var _fusionLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadFusionLog && (x.Type == LogBookType.DailyGuardLog || x.Type == LogBookType.VehicleAndKeyLog)).DistinctBy(x => x.ClientSiteId).ToList();

            var _totalLogBooksToProcess = _fusionLogs.Count + _guardLogs.Count + _keyVechileLogs.Count + _smartWandDailyGuardLogs.Count;


            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Number of logbook to upload " + _totalLogBooksToProcess.ToString() });
            
            //************ Guard LogBook Start ***********

            foreach (var siteLogBook in _guardLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start Guard Logbook" + siteLogBook.ClientSite.Name + "----" });

                    string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "GuardlogBook :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id });
                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    //_clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                    // Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);

                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Guard Logbook upload end " + siteLogBook.ClientSite.Name + "----" });

                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Daily Guard Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Guard LogBook End ***********


            //************ Key & Vechile LogBook Start ***********
            foreach (var siteLogBook in _keyVechileLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start Key & Vechile Logbook" + siteLogBook.ClientSite.Name + "----" });

                    string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "KeyVechilelogBook :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id });
                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    //_clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                    // Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);

                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Key & Vechile Logbook upload end " + siteLogBook.ClientSite.Name + "----" });

                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Key & Vechile Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Key & Vechile LogBook End ***********


            //************ Smart Wand LogBook Start ***********
            foreach (var siteLogBook in _smartWandDailyGuardLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start Smart Wand Log" + siteLogBook.ClientSite.Name + "----" });

                    var _smartWandLogs = siteLogBooksToUpload.Where(x => x.ClientSite.UploadSWLog && x.Type == LogBookType.SmartWandLog && x.Date == siteLogBook.Date).FirstOrDefault();
                    if (_smartWandLogs == null)
                    {
                        //Create logbook if not exists
                        var newSWClientSiteLogBook = new ClientSiteLogBook()
                        {
                            ClientSiteId = siteLogBook.ClientSiteId,
                            Type = LogBookType.SmartWandLog,
                            Date = siteLogBook.Date,
                            DbxUploaded = false
                        };
                        var newSWLogBookId = _clientDataProvider.SaveClientSiteLogBook(newSWClientSiteLogBook);
                        _smartWandLogs = _clientDataProvider.GetClientSiteLogBook(siteLogBook.ClientSiteId, LogBookType.SmartWandLog, siteLogBook.Date);
                    }
                    //siteLogBook.Type = LogBookType.SmartWandLog;
                    string logFileName = GetLogFilePath(siteLogBook, LogBookType.SmartWandLog);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBooksmartwand :" + siteLogBook.ClientSite.Name + " LogBookId:" + siteLogBook.Id + " SmartWandLogBookId:" + _smartWandLogs.Id });

                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    //_clientDataProvider.MarkClientSiteLogBookAsUploaded(_smartWandLogs.Id, logFileName);



                    //Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);


                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Smart Wand upload end " + siteLogBook.ClientSite.Name + "----" });



                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Fusion Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Smart Wand LogBook End ***********


            //************ Fusion LogBook Start ***********
            foreach (var siteLogBook in _fusionLogs)
            {
                try
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start fusion" + siteLogBook.ClientSite.Name + "----" });
                    var _fusionLogbook = siteLogBooksToUpload.Where(x => x.ClientSite.UploadFusionLog && x.Type == LogBookType.FusionLog && x.Date == siteLogBook.Date).FirstOrDefault();
                    if (_fusionLogbook == null)
                    {
                        //Create logbook if not exists
                        var newFusionClientSiteLogBook = new ClientSiteLogBook()
                        {
                            ClientSiteId = siteLogBook.ClientSiteId,
                            Type = LogBookType.FusionLog,
                            Date = siteLogBook.Date,
                            DbxUploaded = false
                        };
                        var newfusionLogBookId = _clientDataProvider.SaveClientSiteLogBook(newFusionClientSiteLogBook);
                        _fusionLogbook = _clientDataProvider.GetClientSiteLogBook(siteLogBook.ClientSiteId, LogBookType.FusionLog, siteLogBook.Date);
                    }

                    string logFileName = GetLogFilePath(siteLogBook, LogBookType.FusionLog);
                    if (string.IsNullOrEmpty(logFileName))
                        continue;
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBookfusion :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id + " FusionLogBookId:" + _fusionLogbook.Id });

                    var fileToUpload = Path.Combine(outputDirectory, logFileName);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                    var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                    // Process the log upload.


                    // Send email if there is a valid email address.
                    if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                        if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                            SendEmail(fileToUpload, siteLogBook);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                    }

                    // Mark the log book as uploaded.
                    //_clientDataProvider.MarkClientSiteLogBookAsUploaded(_fusionLogbook.Id, logFileName);



                    //Delete the file after processing.
                    if (File.Exists(fileToUpload))
                        File.Delete(fileToUpload);


                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Fusion upload end " + siteLogBook.ClientSite.Name + "----" });



                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Fusion Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            //************ Fusion LogBook End ***********


            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler end second run ---" });
        }


        public void ProcessDailyGuardLogsNew_oldNotUsed()
        {

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler Start---" });
            // Retrieve and filter the log books to upload only once.
            var yesterday = DateTime.Now.AddDays(-1).Date;
            //var yesterday = DateTime.Now.AddDays(-5).Date;
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooks()
            .Where(z =>
                (z.ClientSite.UploadGuardLog || z.ClientSite.UploadFusionLog) // OR condition
                && z.Date == yesterday
                && !z.DbxUploaded)
            .ToList();

            // Check if there are any logs to process to avoid unnecessary operations.
            if (!siteLogBooksToUpload.Any())
                return;

            // Cache the report directory path.
            var outputDirectory = Path.Combine(_reportRootDir, "Output");


            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Number of logbook to upload " + siteLogBooksToUpload.Count.ToString() });



            var check = siteLogBooksToUpload.Where(x => x.ClientSite.UploadFusionLog).ToList();
            var check2 = siteLogBooksToUpload.Where(x => x.ClientSite.UploadGuardLog).ToList();

            foreach (var siteLogBook in siteLogBooksToUpload)
            {
                try
                {
                    //If logbook checked upload logbook else fusion log
                    if (siteLogBook.ClientSite.UploadGuardLog)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start " + siteLogBook.ClientSite.Name + "----" });

                        string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                        if (string.IsNullOrEmpty(logFileName))
                            continue;
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBook :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id });
                        var fileToUpload = Path.Combine(outputDirectory, logFileName);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                        var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                        // Process the log upload.


                        // Send email if there is a valid email address.
                        if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                        {
                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                            if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                                SendEmail(fileToUpload, siteLogBook);

                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                        }

                        // Mark the log book as uploaded.
                        _clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                        // Delete the file after processing.
                        if (File.Exists(fileToUpload))
                            File.Delete(fileToUpload);

                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Logbook upload end " + siteLogBook.ClientSite.Name + "----" });
                    }
                    else if (siteLogBook.ClientSite.UploadFusionLog)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start fusion" + siteLogBook.ClientSite.Name + "----" });


                        siteLogBook.Type = LogBookType.FusionLog;
                        string logFileName = GetLogFilePath(siteLogBook, LogBookType.FusionLog);
                        if (string.IsNullOrEmpty(logFileName))
                            continue;
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBookfusion :" + siteLogBook.ClientSite.Name + "LogBookId" + siteLogBook.Id });

                        var fileToUpload = Path.Combine(outputDirectory, logFileName);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + "File" + fileToUpload });
                        var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "Dropboxupload Status " + uploaded.ToString() });
                        // Process the log upload.


                        // Send email if there is a valid email address.
                        if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                        {
                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send Start--" });
                            if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                                SendEmail(fileToUpload, siteLogBook);

                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + "---Mail send finish--" });

                        }

                        // Mark the log book as uploaded.
                        _clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                        //Delete the file after processing.
                        if (File.Exists(fileToUpload))
                            File.Delete(fileToUpload);


                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Fusion upload end " + siteLogBook.ClientSite.Name + "----" });

                    }

                }
                catch (Exception ex)
                {
                    try
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                        _logger.LogError($"Daily Guard Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                    }
                    catch
                    {

                    }
                }

            }

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler end---" });
        }



        public void ProcessDailyGuardLogsSecondRunNew_oldNotUsed()
        {

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler Start Second run---" });
            // Retrieve and filter the log books to upload only once.
            var today = DateTime.Now.Date;
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooks()
                .Where(z => z.ClientSite.UploadGuardLog && z.Date == today && !z.DbxUploaded)
                .ToList();

            // Check if there are any logs to process to avoid unnecessary operations.
            if (!siteLogBooksToUpload.Any())
                return;

            // Cache the report directory path.
            var outputDirectory = Path.Combine(_reportRootDir, "Output");


            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Number of logbook to upload " + siteLogBooksToUpload.Count.ToString() });
            var IfLogBookChecked = true;
            foreach (var siteLogBook in siteLogBooksToUpload)
            {
                try
                {

                    //If logbook checked upload logbook else fusion log
                    if (IfLogBookChecked)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start " + siteLogBook.ClientSite.Name + "----" });

                        string logFileName = GetLogFilePath(siteLogBook, siteLogBook.Type);
                        if (string.IsNullOrEmpty(logFileName))
                            continue;
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBook :" + siteLogBook.ClientSite.Name + " LogBookId" + siteLogBook.Id });
                        var fileToUpload = Path.Combine(outputDirectory, logFileName);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + " File" + fileToUpload });
                        var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + " Dropboxupload Status " + uploaded.ToString() });
                        // Process the log upload.


                        // Send email if there is a valid email address.
                        if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                        {
                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + " ---Mail send Start--" });
                            if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                                SendEmail(fileToUpload, siteLogBook);

                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + " ---Mail send finish--" });

                        }

                        // Mark the log book as uploaded.
                        //_clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                        // Delete the file after processing.
                        if (File.Exists(fileToUpload))
                            File.Delete(fileToUpload);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- end " + siteLogBook.ClientSite.Name + "----" });
                    }
                    else
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Fusion Start " + siteLogBook.ClientSite.Name + "----" });

                        siteLogBook.Type = LogBookType.FusionLog;
                        string logFileName = GetLogFilePath(siteLogBook, LogBookType.FusionLog);
                        if (string.IsNullOrEmpty(logFileName))
                            continue;
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Fusion logBook :" + siteLogBook.ClientSite.Name + " LogBookId" + siteLogBook.Id });
                        var fileToUpload = Path.Combine(outputDirectory, logFileName);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "File to upload Site : " + siteLogBook.ClientSite.Name + " File" + fileToUpload });
                        var uploaded = ProcessDailyGuardLogUploadNew(siteLogBook, fileToUpload);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + " Dropboxupload Status " + uploaded.ToString() });
                        // Process the log upload.


                        // Send email if there is a valid email address.
                        if (!string.IsNullOrEmpty(fileToUpload) && siteLogBook != null)
                        {
                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + " ---Mail send Start--" });
                            if (!string.IsNullOrEmpty(siteLogBook.ClientSite.GuardLogEmailTo))
                                SendEmail(fileToUpload, siteLogBook);

                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Upload Status for Site : " + siteLogBook.ClientSite.Name + " ---Mail send finish--" });

                        }

                        // Mark the log book as uploaded.
                        //_clientDataProvider.MarkClientSiteLogBookAsUploaded(siteLogBook.Id, logFileName);



                        // Delete the file after processing.
                        if (File.Exists(fileToUpload))
                            File.Delete(fileToUpload);
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "-----Fusion end " + siteLogBook.ClientSite.Name + "----" });

                    }

                }
                catch (Exception ex)
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "Error Message : " + siteLogBook.ClientSite.Name + "---message--" + ex.Message });
                    _logger.LogError($"Daily Guard Log Upload | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
                }

            }

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "---Scheduler end second run ---" });
        }

        /* ---------------------------------------------------------------------------------------
           Weekly and monthly log dumps.

           The settings for these (Admin > site > "Enable Weekly/Monthly Log Dump", the LB/KV/SW/
           Fusion checkboxes and the recipients) have existed since p2-178 along with their columns
           on ClientSites, but nothing ever read them - there was no periodic counterpart to
           ProcessDailyGuardLogsNew and no endpoint to call one. Sites had the boxes ticked and
           received nothing.

           Built on the daily run's pieces rather than beside them: the per-day PDFs come from the
           same GetLogFilePath, so a periodic dump contains exactly the pages the daily dump would
           have sent for those days. What differs is only what a period needs - the days are merged
           into one document, it lands in its own Dropbox folder, and it goes to that period's
           recipients rather than the daily ones.

           Each completed dump is recorded in ClientSitePeriodicLogUploads, which is what makes a
           second run of the scheduler a no-op. That table is the periodic equivalent of the daily
           run's ClientSiteLogBooks.DbxUploaded - a periodic dump covers many log books, so it has
           none of its own to mark, and marking the days it covered would stop the daily dump.
           --------------------------------------------------------------------------------------- */

        private static readonly PeriodicLogSelection[] PeriodicLogSelections = new[]
        {
            new PeriodicLogSelection
            {
                ReportType = LogBookType.DailyGuardLog,
                SourceType = LogBookType.DailyGuardLog,
                IsEnabledWeekly = site => site.UploadGuardWeeklyLog,
                IsEnabledMonthly = site => site.UploadGuardMonthlyLog
            },
            new PeriodicLogSelection
            {
                ReportType = LogBookType.VehicleAndKeyLog,
                SourceType = LogBookType.VehicleAndKeyLog,
                IsEnabledWeekly = site => site.UploadKVWeeklyLog,
                IsEnabledMonthly = site => site.UploadKVMonthlyLog
            },
            new PeriodicLogSelection
            {
                // Smart wand entries live in the guard log, so the report is built from that book.
                ReportType = LogBookType.SmartWandLog,
                SourceType = LogBookType.DailyGuardLog,
                IsEnabledWeekly = site => site.UploadSWWeeklyLog,
                IsEnabledMonthly = site => site.UploadSWMonthlyLog
            },
            new PeriodicLogSelection
            {
                ReportType = LogBookType.FusionLog,
                SourceType = LogBookType.DailyGuardLog,
                IsEnabledWeekly = site => site.UploadFusionWeeklyLog,
                IsEnabledMonthly = site => site.UploadFusionMonthlyLog
            }
        };

        /// <inheritdoc />
        public void ProcessWeeklyGuardLogs() =>
            ProcessPeriodicGuardLogs(LogDumpPeriodType.Weekly, GetPreviousWeek(DateTime.Now.Date));

        /// <inheritdoc />
        public void ProcessMonthlyGuardLogs() =>
            ProcessPeriodicGuardLogs(LogDumpPeriodType.Monthly, GetPreviousMonth(DateTime.Now.Date));

        /// <param name="period">
        /// The period to cover. Always the last completed week or month in production - the two
        /// callers above are the only ones - but taken as an argument rather than computed here so
        /// the run can be exercised against a known past period without moving the clock.
        /// </param>
        private void ProcessPeriodicGuardLogs(LogDumpPeriodType periodType,
            (DateTime PeriodStart, DateTime PeriodEnd) period)
        {
            var taskName = $"{periodType} Log Dump";

            var (periodStart, periodEnd) = period;

            /* Two runs of the same task must not overlap. They would both find the same periods
               unproduced - the tracking row is only written once a dump is finished - and both
               start building the same documents, into the same scratch file names. Per task name,
               so the weekly and monthly runs never block one another. */
            var inProgress = _clientDataProvider.GetInProgressPeriodicLogDumpJob(taskName);
            if (inProgress != null)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName}: job {inProgress.Id} started {inProgress.CreatedDate:yyyy-MM-dd HH:mm} is still running, this run was skipped"
                });
                _logger.LogWarning($"{taskName}: another run ({inProgress.Id}) is in progress.");
                return;
            }

            var job = new PeriodicLogDumpJob
            {
                TaskName = taskName,
                PeriodType = periodType,
                PeriodStartDate = periodStart,
                PeriodEndDate = periodEnd,
                CreatedDate = DateTime.Now
            };
            job.Id = _clientDataProvider.SavePeriodicLogDumpJob(job);

            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
            {
                LogDeatils = $"---{taskName} Start--- job {job.Id}, {periodStart:yyyyMMdd} to {periodEnd:yyyyMMdd}"
            });

            var status = new StringBuilder();
            status.Append($"{taskName} {job.Id}: {periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd}. ");
            var success = true;

            try
            {
                /* Read once for the whole run rather than per site: this is what makes a second run
                   of the scheduler a no-op instead of a second delivery. */
                var alreadyProduced = _clientDataProvider.GetPeriodicLogUploads(periodType, periodStart);

                var logBooksInPeriod = _clientDataProvider
                    .GetClientSiteLogBooksForPeriodicLogBookGeneration(periodType, periodStart, periodEnd);

                if (!logBooksInPeriod.Any())
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                    {
                        LogDeatils = $"{taskName}: no log books found for any site with this dump enabled"
                    });
                    status.Append("No log books found for any site with this dump enabled. ");
                    return;
                }

                var outputDirectory = Path.Combine(_reportRootDir, "Output");

                foreach (var siteLogBooks in logBooksInPeriod.GroupBy(x => x.ClientSiteId))
                {
                    var clientSite = siteLogBooks.First().ClientSite;

                    foreach (var selection in PeriodicLogSelections)
                    {
                        if (!selection.IsEnabled(clientSite, periodType))
                            continue;

                        if (alreadyProduced.Any(x => x.ClientSiteId == clientSite.Id &&
                                                     x.LogBookType == selection.ReportType))
                        {
                            job.DumpsSkipped++;
                            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                            {
                                LogDeatils = $"{taskName} : {clientSite.Name} - {selection.ReportType} already produced for this period, skipped"
                            });
                            continue;
                        }

                        /* Each type is isolated. A site that cannot produce its fusion log still
                           gets its guard log, and one bad site does not end the run for the rest -
                           the same treatment every loop in the daily run gets. */
                        try
                        {
                            if (ProcessPeriodicLogForSite(siteLogBooks.ToList(), clientSite, selection,
                                    periodType, taskName, periodStart, periodEnd, outputDirectory))
                            {
                                job.DumpsProduced++;
                            }
                        }
                        catch (Exception ex)
                        {
                            success = false;
                            job.DumpsFailed++;
                            status.Append($"{clientSite.Name}/{selection.ReportType}: {ex.Message}. ");
                            RecordSchedulerError(taskName, periodType, clientSite.Id, selection.ReportType,
                                periodStart, periodEnd, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // The run itself failed - a bad query, an unreachable database. Recorded with no
                // site against it, so it is distinguishable from a single site failing.
                success = false;
                status.Append($"Run aborted: {ex.Message}. ");
                RecordSchedulerError(taskName, periodType, null, null, periodStart, periodEnd, ex);
            }
            finally
            {
                /* Completed in a finally, so a run that throws its way out still closes its own row.
                   An unclosed row blocks the task until a later run declares it stale, which is a
                   worse failure than the one that caused it. */
                status.Append($"Produced {job.DumpsProduced}, skipped {job.DumpsSkipped}, failed {job.DumpsFailed}.");

                job.CompletedDate = DateTime.Now;
                job.Success = success;
                job.StatusMessage = status.ToString();

                try
                {
                    _clientDataProvider.SavePeriodicLogDumpJob(job);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{taskName}: job {job.Id} could not be marked complete.");
                }

                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"---{taskName} end--- job {job.Id}, {job.StatusMessage}"
                });
            }
        }

        /// <summary>
        /// Builds one site's document for one log type and delivers it, then records that the period
        /// is done. Returns without sending anything when the period produced no pages at all, so a
        /// quiet site is not emailed an empty PDF - and nothing is recorded in that case either,
        /// since no dump exists to avoid repeating.
        /// </summary>
        /// <returns>True when a document was produced and recorded; false when the period was empty.</returns>
        private bool ProcessPeriodicLogForSite(List<ClientSiteLogBook> siteLogBooks, ClientSite clientSite,
            PeriodicLogSelection selection, LogDumpPeriodType periodType, string taskName,
            DateTime periodStart, DateTime periodEnd, string outputDirectory)
        {
            _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
            {
                LogDeatils = $"----- Start {taskName} {selection.ReportType} {clientSite.Name}----"
            });

            var sourceBooks = siteLogBooks
                .Where(x => x.Type == selection.SourceType)
                .OrderBy(x => x.Date)
                .ToList();

            /* Fusion draws on both books, so on a day with no guard log the key and vehicle log is
               used instead - the daily run picks its fusion books the same way. */
            if (selection.ReportType == LogBookType.FusionLog)
            {
                var datesAlreadyCovered = sourceBooks.Select(x => x.Date).ToHashSet();
                sourceBooks.AddRange(siteLogBooks
                    .Where(x => x.Type == LogBookType.VehicleAndKeyLog && !datesAlreadyCovered.Contains(x.Date)));
                sourceBooks = sourceBooks.OrderBy(x => x.Date).ToList();
            }

            if (sourceBooks.Count == 0)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName} {selection.ReportType} : {clientSite.Name} - no log books in the period, nothing sent"
                });
                return false;
            }

            var dailyFiles = new List<string>();
            try
            {
                foreach (var logBook in sourceBooks)
                {
                    // The same call the daily run makes, so the pages are identical to that day's dump.
                    var dailyFileName = GetLogFilePath(logBook, selection.ReportType);
                    if (string.IsNullOrEmpty(dailyFileName))
                        continue;

                    var dailyFile = Path.Combine(outputDirectory, dailyFileName);
                    if (File.Exists(dailyFile))
                        dailyFiles.Add(dailyFile);
                }

                if (dailyFiles.Count == 0)
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                    {
                        LogDeatils = $"{taskName} {selection.ReportType} : {clientSite.Name} - no pages generated, nothing sent"
                    });
                    return false;
                }

                var periodicFileName = GetPeriodicFileName(clientSite, selection.ReportType, periodType, periodStart, periodEnd);
                var periodicFile = Path.Combine(outputDirectory, periodicFileName);

                MergePdfs(dailyFiles, periodicFile);

                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName} {selection.ReportType} : {clientSite.Name} merged {dailyFiles.Count} day(s) into {periodicFileName}"
                });

                var (uploaded, dropboxPath) = ProcessPeriodicGuardLogUpload(clientSite, periodicFile, periodType,
                    taskName, selection.ReportType, periodStart, periodEnd);

                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName} {selection.ReportType} : {clientSite.Name} Dropbox upload status {uploaded}"
                });

                var emailedTo = string.Empty;
                if (!string.IsNullOrEmpty(GetPeriodicRecipients(clientSite, periodType)))
                {
                    emailedTo = SendPeriodicEmail(periodicFile, clientSite, selection.ReportType, periodType,
                        taskName, periodStart, periodEnd);
                }

                /* Recorded last, once the document exists and has been delivered as far as it is
                   going to be. This row - not ClientSiteLogBooks.DbxUploaded, which belongs to the
                   daily run - is what stops a second run repeating this period. */
                _clientDataProvider.SavePeriodicLogUpload(new ClientSitePeriodicLogUpload
                {
                    ClientSiteId = clientSite.Id,
                    PeriodType = periodType,
                    LogBookType = selection.ReportType,
                    PeriodStartDate = periodStart,
                    PeriodEndDate = periodEnd,
                    UploadedOn = DateTime.Now,
                    FileName = periodicFileName,
                    FilePath = dropboxPath,
                    DaysIncluded = dailyFiles.Count,
                    DropboxUploaded = uploaded,
                    EmailedTo = emailedTo
                });

                if (File.Exists(periodicFile))
                    File.Delete(periodicFile);

                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"-----{taskName} {selection.ReportType} end {clientSite.Name}----"
                });

                return true;
            }
            finally
            {
                // The per-day PDFs are scratch; the daily run deletes its own the same way.
                foreach (var dailyFile in dailyFiles)
                {
                    try
                    {
                        if (File.Exists(dailyFile))
                            File.Delete(dailyFile);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// The Monday-to-Sunday week before <paramref name="today"/>. Run on its intended Monday
        /// this is the week just finished; run on any other day it is still the last week that
        /// fully ended, so a scheduler that fires late or is run by hand cannot produce a part-week.
        /// </summary>
        public static (DateTime PeriodStart, DateTime PeriodEnd) GetPreviousWeek(DateTime today)
        {
            // DayOfWeek counts from Sunday, so Sunday belongs to the week that started six days ago.
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var thisWeekMonday = today.Date.AddDays(-daysSinceMonday);

            return (thisWeekMonday.AddDays(-7), thisWeekMonday.AddDays(-1));
        }

        /// <summary>
        /// The calendar month before <paramref name="today"/>. Same rule as the week: always a month
        /// that has fully ended, whichever day the run actually happens on.
        /// </summary>
        public static (DateTime PeriodStart, DateTime PeriodEnd) GetPreviousMonth(DateTime today)
        {
            var firstOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var firstOfLastMonth = firstOfThisMonth.AddMonths(-1);

            return (firstOfLastMonth, firstOfThisMonth.AddDays(-1));
        }

        /// <summary>e.g. "Daily Guard Log - VISY Carrara - 20260907-20260913 (Weekly).pdf".</summary>
        private static string GetPeriodicFileName(ClientSite clientSite, LogBookType reportType,
            LogDumpPeriodType periodType, DateTime periodStart, DateTime periodEnd)
        {
            var name = $"{reportType.ToDisplayName()} - {clientSite.Name} - " +
                       $"{periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd} ({periodType}).pdf";

            // The site name is free text and reaches a file path, so strip anything a path rejects.
            return string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        }

        private static string GetPeriodicRecipients(ClientSite clientSite, LogDumpPeriodType periodType) =>
            periodType == LogDumpPeriodType.Weekly
                ? clientSite.GuardLogEmailWeeklyLogTo
                : clientSite.GuardLogEmailMonthlyLogTo;

        /// <summary>Concatenates the day PDFs, in date order, into one document.</summary>
        private static void MergePdfs(List<string> sourceFiles, string destinationFile)
        {
            if (File.Exists(destinationFile))
                File.Delete(destinationFile);

            using var writer = new iText.Kernel.Pdf.PdfWriter(destinationFile);
            using var merged = new iText.Kernel.Pdf.PdfDocument(writer);

            foreach (var sourceFile in sourceFiles)
            {
                using var source = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(sourceFile));
                source.CopyPagesTo(1, source.GetNumberOfPages(), merged, merged.GetNumberOfPages() + 1);
            }
        }

        /// <summary>
        /// Same Dropbox root as the daily upload, but filed under the period rather than a single
        /// day, so a periodic dump never lands on top of - or is mistaken for - that day's daily one.
        /// Returns whether Dropbox accepted it, and the path it was filed under.
        /// </summary>
        private (bool Uploaded, string DropboxPath) ProcessPeriodicGuardLogUpload(ClientSite clientSite,
            string fileToUpload, LogDumpPeriodType periodType, string taskName, LogBookType reportType,
            DateTime periodStart, DateTime periodEnd)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret,
                _settings.DropboxAccessToken, _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSite.Id);
            if (clientSiteKpiSettings == null)
                return (false, null);

            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;

            if (!clientSiteKpiSettings.DropboxScheduleisActive)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName}: {clientSite.Name} DropboxScheduleisActive not enabled, upload skipped"
                });
                return (false, null);
            }

            if (string.IsNullOrEmpty(siteBasePath) || !File.Exists(fileToUpload))
                return (false, null);

            var periodFolder = periodType == LogDumpPeriodType.Weekly ? "WEEKLY LOGS" : "MONTHLY LOGS";
            var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{periodStart.Year}/" +
                              $"{periodStart:yyyyMM} - {periodStart.ToString("MMMM").ToUpper()} DATA/{periodFolder}/" +
                              Path.GetFileName(fileToUpload);

            try
            {
                var uploaded = Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
                return (uploaded, dbxFilePath);
            }
            catch (Exception ex)
            {
                /* The document exists and the email still goes out, so this is not fatal to the
                   dump - but it is exactly the kind of silent failure the error table is for. */
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = "ClientSite: " + clientSite.Name + $" {taskName} Dropbox Fileupload Error:" + ex.Message
                });
                RecordSchedulerError(taskName, periodType, clientSite.Id, reportType, periodStart, periodEnd, ex);
                return (false, dbxFilePath);
            }
        }

        /// <summary>
        /// The merged document, to that period's recipients. Same oversize handling as the daily
        /// mail - a month of logs is far more likely to pass the 12 MB limit than a single day, so
        /// the Azure link path matters more here than it does there. Returns who it was sent to.
        /// </summary>
        private string SendPeriodicEmail(string fileName, ClientSite clientSite, LogBookType reportType,
            LogDumpPeriodType periodType, string taskName, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                var fileInfo = new FileInfo(fileName);
                var bigSize = (fileInfo.Length / 1048576d) > 12;

                var fromAddress = _emailOptions.FromAddress.Split('|');
                var subject = reportType.ToDisplayName();
                var periodName = periodType.ToString().ToLower();
                var messageHtml = "Dear Citywatch Security Client;<br><br>";

                if (!bigSize)
                {
                    messageHtml += $"Please find attached the {periodName} {subject.ToLower()} " +
                                   $"covering {periodStart:dd MMM yyyy} to {periodEnd:dd MMM yyyy}.";
                }
                else
                {
                    var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();
                    if (azureStorageConnectionString != null && azureStorageConnectionString.Count > 0 &&
                        azureStorageConnectionString[0] != null)
                    {
                        var connectionString = azureStorageConnectionString[0];
                        var blobName = Path.GetFileName(fileName);
                        const string containerName = "irfiles";

                        var blobServiceClient = new BlobServiceClient(connectionString);
                        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                        containerClient.CreateIfNotExists();

                        var blobPath = DateTime.UtcNow.ToString("yyyyMMdd") + "/" + blobName;
                        var blobClient = containerClient.GetBlobClient(blobPath);

                        using (var fs = File.OpenRead(fileName))
                        {
                            var blobHttpHeader = new BlobHttpHeaders { ContentType = "application/pdf" };
                            blobClient.Upload(fs, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
                        }

                        messageHtml += "<p>Where PDF attachment is greater than 12 MB, it may not appear due to your organisation email limits. " +
                                       "In this situation simply " +
                                       $"<a href=\"https://c4istorage1.blob.core.windows.net/{containerName}/{blobPath}\" target=\"_blank\">" +
                                       "click here</a> to download the Site log Report.</p>" +
                                       $"<p>File name: {blobName}</p>";
                    }
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));

                var recipients = new List<string>();
                foreach (var email in GetPeriodicRecipients(clientSite, periodType).Split(","))
                {
                    if (CommonHelper.IsValidEmail(email))
                    {
                        message.To.Add(new MailboxAddress(string.Empty, email.Trim()));
                        recipients.Add(email.Trim());
                    }
                }

                // Nothing addressable in the recipients: do not send a to-nobody message.
                if (message.To.Count == 0)
                {
                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                    {
                        LogDeatils = $"{taskName} : {clientSite.Name} no valid recipient, mail skipped"
                    });
                    return string.Empty;
                }

                message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
                message.Subject = $"{subject} ({periodType}) - {clientSite.Name} - {periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd}";

                var builder = new BodyBuilder() { HtmlBody = messageHtml };
                if (!bigSize)
                    builder.Attachments.Add(fileName);
                message.Body = builder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) && !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);

                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName} : {clientSite.Name} mail to address {message.To}"
                });

                client.Send(message);
                client.Disconnect(true);

                return string.Join(",", recipients);
            }
            catch (Exception ex)
            {
                /* The document has been produced and filed by this point, so a mail server problem
                   must not fail the dump - but it must not vanish either. */
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"{taskName} : {clientSite.Name} Mail Issue {ex.Message}"
                });
                RecordSchedulerError(taskName, periodType, clientSite.Id, reportType, periodStart, periodEnd, ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Records a failure in SchedulerTaskErrors, in columns, so a failed site/log type/period can
        /// be found without reading through SiteLogUploadHistory's free text. Never throws: it runs
        /// inside catch blocks, and a logging failure must not replace the failure being logged.
        /// </summary>
        private void RecordSchedulerError(string taskName, LogDumpPeriodType periodType, int? clientSiteId,
            LogBookType? reportType, DateTime periodStart, DateTime periodEnd, Exception ex)
        {
            try
            {
                _logger.LogError(ex, $"{taskName} | Failed | Site: {clientSiteId}, Type: {reportType}, " +
                                     $"Period: {periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd}");

                _clientDataProvider.SaveSchedulerTaskError(new SchedulerTaskError
                {
                    TaskName = taskName,
                    PeriodType = periodType,
                    ClientSiteId = clientSiteId,
                    LogBookType = reportType,
                    PeriodStartDate = periodStart,
                    PeriodEndDate = periodEnd,
                    OccurredOn = DateTime.Now,
                    ErrorMessage = Truncate(ex.Message, 4000),
                    ErrorDetails = ex.ToString()
                });
            }
            catch
            {
            }
        }

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max);

        private string GetLogFilePath(ClientSiteLogBook logBook, LogBookType logbooktype)
        {
            string fileName = string.Empty;

            if (logbooktype == LogBookType.DailyGuardLog)
                return _guardLogReportGenerator.GeneratePdfReport(logBook.Id, null);

            if (logbooktype == LogBookType.VehicleAndKeyLog)
                return _keyVehicleLogReportGenerator.GeneratePdfReport(logBook.Id);

            if (logbooktype == LogBookType.SmartWandLog)
                return _guardLogReportGenerator.GeneratePdfReportSmartWand(logBook.Id);

            if (logbooktype == LogBookType.FusionLog)
                return _guardLogReportGenerator.GeneratePdfReportFusion(logBook.Id);
            return fileName;
        }

        private void SendEmail(string fileName, ClientSiteLogBook siteLogBook)
        {
            //return;

            try
            {
                bool bigSize = false;
                FileInfo fileInfo = new FileInfo(fileName);
                var fileSizeInMB = (fileInfo.Length) / 1048576d; // bytes to MB
                if (fileSizeInMB > 12)
                    bigSize = true;

                // Common email details
                var fromAddress = _emailOptions.FromAddress.Split('|');
                var subject = siteLogBook.Type.ToDisplayName();
                var messageHtml = $"Dear Citywatch Security Client;<br><br>";
                if (!bigSize)
                {
                    messageHtml += $"Please find attached {subject.ToLower()}.";
                }

                // If large, upload to Azure and add link instead of attachment
                if (bigSize)
                {
                    var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();
                    if (azureStorageConnectionString.Count > 0 && azureStorageConnectionString[0] != null)
                    {
                        string connectionString = azureStorageConnectionString[0];
                        string blobName = Path.GetFileName(fileName);
                        string containerName = "irfiles";

                        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                        containerClient.CreateIfNotExists();

                        // Folder structure: irfiles/yyyyMMdd
                        string blobPath = DateTime.UtcNow.ToString("yyyyMMdd") + "/" + blobName;
                        BlobClient blobClient = containerClient.GetBlobClient(blobPath);

                        using (FileStream fs = File.OpenRead(fileName))
                        {
                            var blobHttpHeader = new BlobHttpHeaders { ContentType = "application/pdf" };
                            blobClient.Upload(fs, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
                        }

                        messageHtml += "<p>Where PDF attachment is greater than 12 MB, it may not appear due to your organisation email limits. " +
                                       "In this situation simply " +
                                       $"<a href=\"https://c4istorage1.blob.core.windows.net/{containerName}/{blobPath}\" target=\"_blank\">" +
                                       "click here</a> to download the Site log Report.</p>" +
                                       $"<p>File name: {blobName}</p>";
                    }
                }


                // Build email
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                foreach (var email in siteLogBook.ClientSite.GuardLogEmailTo.Split(","))
                {
                    if (CommonHelper.IsValidEmail(email))
                        message.To.Add(new MailboxAddress(string.Empty, email.Trim()));
                }
                message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
                message.Subject = $"{subject} - {siteLogBook.ClientSite.Name} - {siteLogBook.Date:yyyyMMdd}";

                var builder = new BodyBuilder() { HtmlBody = messageHtml };
                if (!bigSize)
                {
                    builder.Attachments.Add(fileName); // Only attach if small
                }
                message.Body = builder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) && !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);

                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"logBook : {siteLogBook.ClientSite.Name} mail to address {message.To}"
                });

                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception ex)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory
                {
                    LogDeatils = $"logBook : {siteLogBook.ClientSite.Name} Mail Issue {ex.Message}"
                });
                _logger.LogError($"Daily Guard Log Email | Failed | Log Book Id: {siteLogBook.Id}. Error: {ex.Message}");
            }
        }

        private bool ProcessDailyGuardLogUpload(ClientSiteLogBook clientSiteLogBook, string fileToUpload)
        {

            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteLogBook.ClientSiteId);
            if (clientSiteKpiSettings == null)
                throw new ArgumentException($"ClientSiteKpiSettings missing for Client Site Id: {clientSiteLogBook.ClientSiteId}");

            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;
            if (string.IsNullOrEmpty(siteBasePath))
                throw new ArgumentException($"SiteBasePath missing for Client Site Id: {clientSiteLogBook.ClientSiteId}");

            if (!File.Exists(fileToUpload))
                throw new ArgumentException($"File not found: {fileToUpload} for IR id: {clientSiteLogBook.Id}");
            //27/11/2024
            if (!clientSiteKpiSettings.DropboxScheduleisActive)
            {
                throw new ArgumentException($"DropboxScheduleisActive: not enabled");
            }
            try
            {

                var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
                var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{clientSiteLogBook.Date.Year}/{clientSiteLogBook.Date:yyyyMM} - {clientSiteLogBook.Date.ToString("MMMM").ToUpper()} DATA/{clientSiteLogBook.Date.ToString(dayPathFormat).ToUpper()}/" + Path.GetFileName(fileToUpload);
                return Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
            }
            catch (Exception ex)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "ClientSite: " + clientSiteLogBook.ClientSite.Name + " Dropbox Fileupload Error:" + ex.Message });
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        private bool ProcessDailyGuardLogUploadNew(ClientSiteLogBook clientSiteLogBook, string fileToUpload)
        {

            //return true;

            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteLogBook.ClientSiteId);
            //if (clientSiteKpiSettings == null)
            //    throw new ArgumentException($"ClientSiteKpiSettings missing for Client Site Id: {clientSiteLogBook.ClientSiteId}");

            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;
            //if (string.IsNullOrEmpty(siteBasePath))
            //    throw new ArgumentException($"SiteBasePath missing for Client Site Id: {clientSiteLogBook.ClientSiteId}");

            //if (!File.Exists(fileToUpload))
            //    throw new ArgumentException($"File not found: {fileToUpload} for IR id: {clientSiteLogBook.Id}");
            //27/11/2024
            if (!clientSiteKpiSettings.DropboxScheduleisActive)
            {
                throw new ArgumentException($"DropboxScheduleisActive: not enabled");
            }
            try
            {
                if (clientSiteKpiSettings != null && (!string.IsNullOrEmpty(siteBasePath)) && File.Exists(fileToUpload))
                {
                    var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
                    var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{clientSiteLogBook.Date.Year}/{clientSiteLogBook.Date:yyyyMM} - {clientSiteLogBook.Date.ToString("MMMM").ToUpper()} DATA/{clientSiteLogBook.Date.ToString(dayPathFormat).ToUpper()}/" + Path.GetFileName(fileToUpload);
                    return Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "ClientSite: " + clientSiteLogBook.ClientSite.Name + " Dropbox Fileupload Error:" + ex.Message });
                _logger.LogError(ex.StackTrace);
                return false;
            }
        }

    }
}