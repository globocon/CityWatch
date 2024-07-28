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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    public interface ISiteLogUploadService
    {
        void ProcessDailyGuardLogs();
        void ProcessDailyGuardLogsSecondRun();
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
        private readonly IEmailLogDataProvider _emailLogDataProvider;
        public SiteLogUploadService(IClientDataProvider clientDataProvider,
            IGuardLogReportGenerator guardLogReportGenerator,
            IKeyVehicleLogReportGenerator keyVehicleLogReportGenerator,
            IDropboxService dropboxService,
            IOptions<EmailOptions> emailOptions,
            IWebHostEnvironment webHostEnvironment,
            ILogger<SiteLogUploadService> logger,
            IOptions<Settings> settings,
            IEmailLogDataProvider emailLogDataProvider)
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
            _emailLogDataProvider = emailLogDataProvider;
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

        private string GetLogFilePath(ClientSiteLogBook logBook)
        {
            string fileName = string.Empty;

            if (logBook.Type == LogBookType.DailyGuardLog)
                return _guardLogReportGenerator.GeneratePdfReport(logBook.Id);

            if (logBook.Type == LogBookType.VehicleAndKeyLog)
                return _keyVehicleLogReportGenerator.GeneratePdfReport(logBook.Id);

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
                    /* Save log email Start 24072024 manju*/
                    string toAddressForSplit = string.Join(", ", message.To.Select(a => a.ToString()));
                    string bccAddressForSplit = string.Join(", ", message.Bcc.Select(a => a.ToString()));
                    _emailLogDataProvider.SaveEmailLog(
                        new EmailAuditLog()
                        {
                            UserID = 1,
                            GuardID = 1,
                            IPAddress = string.Empty,
                            ToAddress = toAddressForSplit,
                            BCCAddress = bccAddressForSplit,
                            Module = "Site Log Upload Service",
                            Type = "CityWtch-Services-Site Log Upload",
                            EmailSubject = message.Subject,
                            AttachmentFileName = string.Empty,
                            SendingDate = DateTime.Now
                        }
                     );
                    /* Save log for email end*/

                    using var client = new MailKit.Net.Smtp.SmtpClient();
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
                    //to avoid duplicate emails sending-start
                    flag = true;
                    //to avoid duplicate emails sending-end
                }
            }
            catch (Exception ex)
            {
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

            try
            {
                var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
                var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{clientSiteLogBook.Date.Year}/{clientSiteLogBook.Date:yyyyMM} - {clientSiteLogBook.Date.ToString("MMMM").ToUpper()} DATA/{clientSiteLogBook.Date.ToString(dayPathFormat).ToUpper()}/" + Path.GetFileName(fileToUpload);
                return Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

    }
}