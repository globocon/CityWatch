using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;


namespace CityWatch.Web.Pages.Incident
{
    public class RegisterModel : PageModel
    {
        const string LAST_USED_IR_SEQ_NO_CONFIG_NAME = "LastUsedIrSn";

        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly EmailOptions _EmailOptions;
        private readonly IViewDataService _ViewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IIrDataProvider _irDataProvider;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public IncidentRequest Report { get; set; }

        public IViewDataService ViewDataService { get { return _ViewDataService; } }

        public RegisterModel(IWebHostEnvironment webHostEnvironment,
            IOptions<EmailOptions> emailOptions,
            IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            IIrDataProvider irDataProvider,
            IConfigDataProvider configDataProvider,
            IAppConfigurationProvider appConfigurationProvider,
            ILogger<RegisterModel> logger,
            IIncidentReportGenerator incidentReportGenerator,
            IGuardLogDataProvider guardLogDataProvider,
            IConfiguration configuration
            )
        {
            _WebHostEnvironment = webHostEnvironment;
            _EmailOptions = emailOptions.Value;
            _ViewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _irDataProvider = irDataProvider;
            _appConfigurationProvider = appConfigurationProvider;
            _logger = logger;
            _incidentReportGenerator = incidentReportGenerator;
            _guardLogDataProvider = guardLogDataProvider;
            _configuration = configuration;
        }

        public void OnGet()
        {
            HttpContext.Session.SetString("ReportReference", Guid.NewGuid().ToString());
        }

        public IActionResult OnGetClientSites(string type)
        {
            return new JsonResult(_ViewDataService.GetUserClientSites(AuthUserHelper.LoggedInUserId, type));
        }

        public IActionResult OnGetClientSiteByName(string name)
        {
            var clientSite = _clientDataProvider.GetClientSites(null).Single(x => x.Name == name);
            return new JsonResult(new { success = true, clientSite });
        }

        public IActionResult OnGetFeedbackTemplate(int templateId)
        {
            var text = _ViewDataService.GetFeedbackTemplateText(templateId);
            return new JsonResult(new { text });
        }

        public IActionResult OnGetFeedbackTemplatesByType(FeedbackType type)
        {
            return new JsonResult(_ViewDataService.GetFeedbackTemplatesByType((FeedbackType)type));
        }

        public IActionResult OnGetOfficerPositions(OfficerPositionFilter filter)
        {
            return new JsonResult(_ViewDataService.GetOfficerPositions((OfficerPositionFilter)filter));
        }

        public JsonResult OnPostSubmit()
        {
            if (Report == null)
                throw new ArgumentNullException("Incident Report");

            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    success = false,
                    errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        PropertyName = x.Key,
                        ErrorList = x.Value.Errors.Select(y => y.ErrorMessage).ToArray()
                    })
                    .AsEnumerable()
                });
            }

            ProcessIrSubmit();

            return new JsonResult(new { success = true });
        }


        public JsonResult OnPostUpload()
        {
            var success = false;
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        var uploadFileName = Path.GetFileName(file.FileName);
                        string reportReference = HttpContext.Session.GetString("ReportReference");
                        var folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "Uploads", reportReference);
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                        using (var stream = System.IO.File.Create(Path.Combine(folderPath, uploadFileName)))
                        {
                            file.CopyTo(stream);
                        }
                        success = true;
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return new JsonResult(new { attachmentId = Request.Form["attach_id"], success });
        }

        private bool SendEmail(string fileName)
        {
            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var toAddress = _EmailOptions.ToAddress.Split('|');
            var ccAddress = _EmailOptions.CcAddress.Split('|');
            var subject = _EmailOptions.Subject;
            var messageHtml = _EmailOptions.Message;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            foreach (var address in GetToEmailAddressList(toAddress))
                message.To.Add(address);

            if (Report.DateLocation.ReimbursementYes)
            {
                message.Cc.Add(new MailboxAddress(ccAddress[1], ccAddress[0]));
            }
            /* Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 13,09,2023) */
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            /* Mail Id added Bcc globoconsoftware end */
            var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite && x.ClientType.Name == Report.DateLocation.ClientType);

            if (clientSite != null && !string.IsNullOrEmpty(clientSite.Emails))
            {
                foreach (var email in clientSite.Emails.Split(","))
                {
                    if (CommonHelper.IsValidEmail(email))
                        message.Cc.Add(new MailboxAddress(string.Empty, email.Trim()));
                }
            }

            message.Subject = $"{subject} - {Report.DateLocation.ClientType} - {Report.DateLocation.ClientSite}";

            var builder = new BodyBuilder()
            {
                HtmlBody = messageHtml
            };
            builder.Attachments.Add(fileName);
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                    client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

            return true;
        }



        private bool SendEmailWithAzureBlob(string fileName)
        {
            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var toAddress = _EmailOptions.ToAddress.Split('|');
            var ccAddress = _EmailOptions.CcAddress.Split('|');
            var subject = _EmailOptions.Subject;
            var messageHtml = _EmailOptions.Message;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            foreach (var address in GetToEmailAddressList(toAddress))
                message.To.Add(address);

            if (Report.DateLocation.ReimbursementYes)
            {
                message.Cc.Add(new MailboxAddress(ccAddress[1], ccAddress[0]));
            }
            /* Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 13,09,2023) */
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            /* Mail Id added Bcc globoconsoftware end */
            var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite && x.ClientType.Name == Report.DateLocation.ClientType);

            if (clientSite != null && !string.IsNullOrEmpty(clientSite.Emails))
            {
                foreach (var email in clientSite.Emails.Split(","))
                {
                    if (CommonHelper.IsValidEmail(email))
                        message.Cc.Add(new MailboxAddress(string.Empty, email.Trim()));
                }
            }

            message.Subject = $"{subject} - {Report.DateLocation.ClientType} - {Report.DateLocation.ClientSite}";




            /* azure blob Implementation 25-9-2023* Start*/
            var azureStorageConnectionString = _configuration.GetSection("AzureStorage").Get<List<string>>();
            if (azureStorageConnectionString.Count > 0)
            {
                if (azureStorageConnectionString[0] != null)
                {

                    string connectionString = azureStorageConnectionString[0];
                    string blobName = Path.GetFileName(fileName);
                    string containerName = "irfiles";
                    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    containerClient.CreateIfNotExists();
                    /* The container Structure like irfiles/20230925*/
                    BlobClient blobClient = containerClient.GetBlobClient(new string(blobName.Take(8).ToArray()) + "/" + blobName);
                    using FileStream fs = System.IO.File.OpenRead(fileName);
                    blobClient.Upload(fs, true);
                    fs.Close();
                    messageHtml = messageHtml + "<p> Please click below to download the Incident Report</p>" +
                    "<a href=\"http://test.c4i-system.com/DownloadPDF?containerName=irfiles&&fileName=" + (new string(blobName.Take(8).ToArray()) + "/" + blobName) + "\" target=\"_blank\">" +
                    "<button style = \"background-color: #008CBA; color: white; padding: 10px 20px; border: none; cursor: pointer; border-radius: 5px;\">Download Incident Report</button >" +
                    "</a>";

                }

            }
            /* azure blob Implementation 25-9-2023* End*/
            var builder = new BodyBuilder()
            {
                HtmlBody = messageHtml
            };
            builder.Attachments.Add(fileName);
            message.Body = builder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                    client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

            return true;
        }














        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();
            emailAddressList.Add(new MailboxAddress(toAddress[1], toAddress[0]));
            var fields = _configDataProvider.GetReportFields().ToList();

            var positionEmailTo = GetFieldEmailAddress(fields, ReportFieldType.Position, Report.Officer.Position);
            if (!string.IsNullOrEmpty(positionEmailTo))
                emailAddressList.Add(new MailboxAddress(string.Empty, positionEmailTo));

            var notifiedByEmailTo = GetFieldEmailAddress(fields, ReportFieldType.NotifiedBy, Report.Officer.NotifiedBy);
            if (!string.IsNullOrEmpty(notifiedByEmailTo))
                emailAddressList.Add(new MailboxAddress(string.Empty, notifiedByEmailTo));

            var callSignEmailTo = GetFieldEmailAddress(fields, ReportFieldType.CallSign, Report.Officer.CallSign);
            if (!string.IsNullOrEmpty(callSignEmailTo))
                emailAddressList.Add(new MailboxAddress(string.Empty, callSignEmailTo));

            return emailAddressList;
        }

        private static string GetFieldEmailAddress(List<IncidentReportField> fields, ReportFieldType type, string fieldValue)
        {
            return fields.SingleOrDefault(x => x.TypeId == type && x.Name == fieldValue)?.EmailTo;
        }

        private string GetNextIrSequenceNumber()
        {
            var lastSequenceNumber = 0;
            var configuration = _appConfigurationProvider.GetConfigurationByName(LAST_USED_IR_SEQ_NO_CONFIG_NAME);
            if (configuration != null)
            {
                lastSequenceNumber = int.Parse(configuration.Value);
                lastSequenceNumber++;
                configuration.Value = lastSequenceNumber.ToString();
                _appConfigurationProvider.SaveConfiguration(configuration);
            }
            return lastSequenceNumber.ToString().PadLeft(5, '0');
        }

        private string GetIrSerialNumber(IncidentRequest incidentRequest)
        {
            if (incidentRequest.PatrolType == PatrolType.Alarm)
            {
                var incidentReports = _irDataProvider.GetIncidentReportsByJobNumber(incidentRequest.DateLocation.JobNumber);
                if (incidentReports.Any())
                {
                    var numberSuffix = GetJobNumberSuffix(incidentReports.Count - 1);
                    return $"{incidentRequest.OccurrenceNo}-{numberSuffix}";
                }

                return incidentRequest.OccurrenceNo;
            }

            return GetNextIrSequenceNumber();
        }

        private string GetJobNumberSuffix(int index)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var value = "";

            if (index >= letters.Length)
                value += letters[index / letters.Length - 1];
            value += letters[index % letters.Length];

            return value;
        }

        private int GetLogBookId(int clientSiteId)
        {
            int logBookId;
            var logBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, LogBookType.DailyGuardLog, DateTime.Today);
            if (logBook == null)
            {
                logBookId = _clientDataProvider.SaveClientSiteLogBook(new ClientSiteLogBook()
                {
                    ClientSiteId = clientSiteId,
                    Type = LogBookType.DailyGuardLog,
                    Date = DateTime.Today,
                });
            }
            else
            {
                logBookId = logBook.Id;
            }

            return logBookId;
        }

        private void CreateGuardLogEntry(IncidentReport report)
        {
            var logBookId = GetLogBookId(report.ClientSiteId.Value);
            var guardLog = new GuardLog()
            {
                ClientSiteLogBookId = logBookId,
                EventDateTime = DateTime.Now,
                Notes = Path.GetFileNameWithoutExtension(report.FileName),
                IsSystemEntry = true,
                IrEntryType = report.IsEventFireOrAlarm ? IrEntryType.Alarm : IrEntryType.Normal
            };
            _guardLogDataProvider.SaveGuardLog(guardLog);
        }

        private void ProcessIrSubmit()
        {
            var fileName = string.Empty;
            var processResult = new SortedDictionary<int, IrProcessFailure>();
            var reportGenerated = false;

            // TODO: Remove session dependency on attachments
            Report.ReportReference = HttpContext.Session.GetString("ReportReference");
            if (string.IsNullOrEmpty(Report.ReportReference))
                processResult.Add(9000, new IrProcessFailure("Session timeout due to user inactivity. Failed to attach files", string.Empty));

            try
            {
                Report.SerialNumber = GetIrSerialNumber(Report);
            }
            catch (Exception ex)
            {
                processResult.Add(9001, new IrProcessFailure($"Failed to get serial numbers. {ex.Message}", ex.StackTrace));
            }
            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == Report.DateLocation.ClientType);
            var clientSite = _clientDataProvider.GetClientSites(clientType.Id).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite);

            // var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite);
            try
            {
                fileName = _incidentReportGenerator.GeneratePdf(Report, clientSite);
                reportGenerated = true;
                TempData["ReportFileName"] = fileName;
                // TODO: Remove - debug log of GPS issue
                _logger.LogError($"IR GPS LOG | SN: {Report.SerialNumber} | 3b: {Report.WandScannedYes3b} | Show Inc Loc: {Report.DateLocation.ShowIncidentLocationAddress} | Gps: {Report.DateLocation.ClientSiteLiveGps} | Gps Deg: {Report.DateLocation.ClientSiteLiveGpsInDegrees}");
            }
            catch (Exception ex)
            {
                processResult.Add(9002, new IrProcessFailure($"Failed to generate Pdf report. {ex.Message}", ex.StackTrace));
            }

            var report = new IncidentReport()
            {
                FileName = fileName,
                CreatedOn = DateTime.UtcNow,
                ClientSiteId = clientSite?.Id,
                ReportDateTime = Report.DateLocation.ReportDate,
                IncidentDateTime = Report.DateLocation.IncidentDate,
                JobNumber = Report.DateLocation.JobNumber,
                JobTime = Report.DateLocation.JobTime,
                CallSign = Report.Officer.CallSign,
                NotifiedBy = Report.Officer.NotifiedBy,
                Billing = Report.Officer.Billing,
                IsEventFireOrAlarm = Report.EventType.AlarmActive || Report.EventType.AlarmDisabled || Report.EventType.Emergency,
                OccurNo = Report.OccurrenceNo,
                ActionTaken = Report.Feedback,
                IsPatrol = Report.IsPositionPatrolCar,
                Position = Report.Officer.Position,
                ClientArea = Report.DateLocation.ClientArea,
                SerialNo = Report.SerialNumber,
                ColourCode = Report.SiteColourCodeId,
                IsPlateLoaded = Report.PlateLoadedYes,
                PlateId = 0,
                VehicleRego = null,
                LogId = AuthUserHelper.LoggedInUserId.GetValueOrDefault(),
                IncidentReportEventTypes = Report.IrEventTypes.Select(z => new IncidentReportEventType() { EventType = z }).ToList()
            };

            if (!reportGenerated)
            {
                try
                {
                    string jsonString = JsonSerializer.Serialize(report);
                    _logger.LogInformation(jsonString);
                }
                catch (Exception ex)
                {
                    _logger.LogError("IR object serialization failed. " + ex.StackTrace);
                }
            }
            else
            {
                try
                {
                    _irDataProvider.SaveReport(report);
                    if (report.IsPlateLoaded == true)
                    {
                        var incidentreportid = _clientDataProvider.GetMaxIncidentReportId(AuthUserHelper.LoggedInUserId.GetValueOrDefault());
                        var incidentreportsplateid = _clientDataProvider.GetIncidentDetailsKvlReport(AuthUserHelper.LoggedInUserId.GetValueOrDefault());
                        for (int i = 0; i < incidentreportsplateid.Count; i++)
                        {
                            _irDataProvider.UpdateReport(incidentreportid, Convert.ToInt32(incidentreportsplateid[i].Id));
                        }

                    }
                }
                catch (Exception ex)
                {
                    processResult.Add(9003, new IrProcessFailure($"Failed to save IR details. {ex.Message}", ex.StackTrace));
                }

                try
                {
                    if (report.ClientSiteId.HasValue)
                        CreateGuardLogEntry(report);
                }
                catch (Exception ex)
                {
                    processResult.Add(9013, new IrProcessFailure($"Failed to save logbook entry. {ex.Message}", ex.StackTrace));
                }

                try
                {
                    if (!Convert.ToBoolean(Request.Form["Report.DisableEmail"]))
                        SendEmailWithAzureBlob(Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName));
                }
                catch (Exception ex)
                {
                    processResult.Add(9004, new IrProcessFailure($"Failed to send email. {ex.Message}", ex.StackTrace));
                }
            }

            TempData["ReportGenerated"] = reportGenerated;
            if (processResult.Count > 0)
            {
                TempData["Error"] = string.Join(Environment.NewLine, processResult.Select(z => $"{z.Key} - {z.Value.ErrorMessage}"));
                _logger.LogError(string.Join(Environment.NewLine, processResult.Select(z => z.Value.StackTrace)));
            }


            try
            {
                var folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "Uploads", Report.ReportReference);
                if (Directory.Exists(folderPath))
                    Directory.Delete(folderPath, true);

                var filePath = Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    var dropBoxFolderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "ToDropbox");
                    if (!Directory.Exists(dropBoxFolderPath))
                        Directory.CreateDirectory(dropBoxFolderPath);
                    System.IO.File.Move(filePath, Path.Combine(dropBoxFolderPath, fileName), true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
        }
        public JsonResult OnPostPlateLoaded(Data.Models.IncidentReportsPlatesLoaded report)
        {
            var status = true;
            var message = "Success";
            try
            {
                report.LogId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();
                _clientDataProvider.SavePlateLoaded(report);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public IActionResult OnGetPlatesLoaded()
        {
            return new JsonResult(_configDataProvider.GetPlatesLoaded(AuthUserHelper.LoggedInUserId.GetValueOrDefault()));
        }
        public JsonResult OnPostDeletePlateLoaded(Data.Models.IncidentReportsPlatesLoaded report)
        {
            var status = true;
            var message = "Success";
            try
            {
                report.LogId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();
                _clientDataProvider.DeletePlateLoaded(report);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostDeleteFullPlateLoaded(Data.Models.IncidentReportsPlatesLoaded report, int Count)
        {
            var status = true;
            var message = "Success";
            try
            {
                report.LogId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();
                _clientDataProvider.DeleteFullPlateLoaded(report, Count);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetPlateLoaded(int TruckConfig)
        {


            var TruckConfigText = _clientDataProvider.GetKeyVehicleLogFieldsByTruckId(TruckConfig);





            return new JsonResult(new { TruckConfigText });
        }
        public JsonResult OnGetIfPlateExists(int PlateId, string TruckNo)
        {

            int LogId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();

            var Count = _clientDataProvider.GetIncidentReportsPlatesCount(PlateId, TruckNo, AuthUserHelper.LoggedInUserId.GetValueOrDefault());



            return new JsonResult(new { Count });
        }


    }
}
