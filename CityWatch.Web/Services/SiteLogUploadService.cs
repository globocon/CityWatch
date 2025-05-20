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
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    public interface ISiteLogUploadService
    {
        void ProcessDailyGuardLogs();
        void ProcessDailyGuardLogsSecondRun();
        public void ProcessDailyGuardLogsNew();
        public void ProcessDailyGuardLogsSecondRunNew();

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

        public SiteLogUploadService(IClientDataProvider clientDataProvider,
            IGuardLogReportGenerator guardLogReportGenerator,
            IKeyVehicleLogReportGenerator keyVehicleLogReportGenerator,
            IDropboxService dropboxService,
            IOptions<EmailOptions> emailOptions,
            IWebHostEnvironment webHostEnvironment,
            ILogger<SiteLogUploadService> logger,
            IOptions<Settings> settings)
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
        }

        public void ProcessDailyGuardLogs()
        {
            var siteLogBooksToUpload = _clientDataProvider.GetClientSiteLogBooks().Where(z => z.ClientSite.UploadGuardLog && z.Date == DateTime.Now.AddDays(-1).Date && !z.DbxUploaded);
            foreach (var siteLogBook in siteLogBooksToUpload)
            {
                try
                {
                    string logFileName = GetLogFilePath(siteLogBook);
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
                    string logFileName = GetLogFilePath(siteLogBook);
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
            //var yesterday = DateTime.Now.AddDays(-1).Date;
            var yesterday = DateTime.Now.AddDays(-5).Date;
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

                        string logFileName = GetLogFilePath(siteLogBook);
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
                    else if(siteLogBook.ClientSite.UploadFusionLog)
                    {
                        _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "----- Start fusion" + siteLogBook.ClientSite.Name + "----" });


                        siteLogBook.Type = LogBookType.FusionLog;
                        string logFileName = GetLogFilePath(siteLogBook);
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



        public void ProcessDailyGuardLogsSecondRunNew()
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

                        string logFileName = GetLogFilePath(siteLogBook);
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
                        string logFileName = GetLogFilePath(siteLogBook);
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

        private string GetLogFilePath(ClientSiteLogBook logBook)
        {
            string fileName = string.Empty;

            if (logBook.Type == LogBookType.DailyGuardLog)
                return _guardLogReportGenerator.GeneratePdfReport(logBook.Id);

            if (logBook.Type == LogBookType.VehicleAndKeyLog)
                return _keyVehicleLogReportGenerator.GeneratePdfReport(logBook.Id);
            if(logBook.Type == LogBookType.FusionLog)
                return _guardLogReportGenerator.GeneratePdfReportFusion(logBook.Id);
            return fileName;
        }

        private void SendEmail(string fileName, ClientSiteLogBook siteLogBook)
        {
            try
            {


                var fromAddress = _emailOptions.FromAddress.Split('|');
                var subject = siteLogBook.Type.ToDisplayName();
                var messageHtml = $"Dear Citywatch Security Client; <br><br>Please find attached {subject.ToLower()}.";
                //to avoid duplicate emails sending-start
                bool flag = false;
                if (!flag)
                {
                    //to avoid duplicate emails sending-end
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                    foreach (var email in siteLogBook.ClientSite.GuardLogEmailTo.Split(","))
                    {
                        if (CommonHelper.IsValidEmail(email))
                            message.To.Add(new MailboxAddress(string.Empty, email.Trim()));
                    }
                    /* Mail Id added Bcc globoconsoftware for checking LB,KV Mail not getting Issue Start(date 17,01,2024) */
                    message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
                    //  message.Bcc.Add(new MailboxAddress("globoconsoftware2", "jishakallani@gmail.com"));
                    /* Mail Id added Bcc globoconsoftware end */
                    message.Subject = $"{subject} - {siteLogBook.ClientSite.Name} - {siteLogBook.Date: yyyyMMdd}";

                    var builder = new BodyBuilder()
                    {
                        HtmlBody = messageHtml
                    };
                    builder.Attachments.Add(fileName);
                    message.Body = builder.ToMessageBody();

                    using var client = new MailKit.Net.Smtp.SmtpClient();
                    client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                        !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                        client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);

                    _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBook :" + siteLogBook.ClientSite.Name + "mail to address" + message.To });
                    client.Send(message);
                    client.Disconnect(true);
                    //to avoid duplicate emails sending-start
                    flag = true;
                    //to avoid duplicate emails sending-end
                }
            }
            catch (Exception ex)
            {
                _clientDataProvider.SaveSiteLogUploadHistory(new SiteLogUploadHistory { LogDeatils = "logBook :" + siteLogBook.ClientSite.Name + "Mail Issue" + ex.Message });
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