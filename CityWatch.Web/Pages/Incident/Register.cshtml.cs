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
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Dropbox.Api.Users;
using static Dropbox.Api.Paper.ListPaperDocsSortBy;

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
        public List<SelectListItem> ClientSites { get; set; }
        public List<SelectListItem> FeedBackTemplates { get; set; }
        public List<SelectListItem> OfficerPosition { get; set; }


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
        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }
        public IActionResult OnGet()
        {
            HttpContext.Session.SetString("ReportReference", Guid.NewGuid().ToString());
            /* Code for Re-create the Ir from already existing one 04102023 Start*/
            HttpContext.Session.Remove("IsIrFromNotifyPage");
            string reuse = Request.Query["reuse"];
            /* Check the Query String */
            if (!string.IsNullOrEmpty(reuse))
            {
                var httpContext = HttpContext;
                // Check if the Referer header is present in the request
                if (httpContext.Request.Headers.ContainsKey("Referer"))
                {
                    // Get the Referer URL
                    string refererUrl = httpContext.Request.Headers["Referer"].ToString();
                    /* check if url contains 'notify'*/
                    if (refererUrl.ToLower().Contains("notify"))
                    {
                        /* check session is null */
                        if (HttpContext.Session.GetString("IRReport") != null)
                        {
                            var serializedObject = HttpContext.Session.GetString("IRReport");
                            var IrPreviousObject = JsonSerializer.Deserialize<IncidentRequest>(serializedObject);
                            HttpContext.Session.SetString("IsIrFromNotifyPage", "yes");
                            Report = new IncidentRequest
                            {
                                EventType = new EventType
                                {
                                    HrRelated = IrPreviousObject.EventType.HrRelated,
                                    OhsMatters = IrPreviousObject.EventType.OhsMatters,
                                    SecurtyBreach = IrPreviousObject.EventType.SecurtyBreach,
                                    EquipmentDamage = IrPreviousObject.EventType.EquipmentDamage,
                                    Thermal = IrPreviousObject.EventType.Thermal,
                                    Emergency = IrPreviousObject.EventType.Emergency,
                                    SiteColour = IrPreviousObject.EventType.SiteColour,
                                    HealthDepart = IrPreviousObject.EventType.HealthDepart,
                                    GeneralSecurity = IrPreviousObject.EventType.GeneralSecurity,
                                    AlarmActive = IrPreviousObject.EventType.AlarmActive,
                                    AlarmDisabled = IrPreviousObject.EventType.AlarmDisabled,
                                    AuthorisedPerson = IrPreviousObject.EventType.AuthorisedPerson,
                                    Equipment = IrPreviousObject.EventType.Equipment,
                                    Other = IrPreviousObject.EventType.Other,
                                },
                                SiteColourCodeId = IrPreviousObject.SiteColourCodeId,
                                WandScannedYes3a = IrPreviousObject.WandScannedYes3a,
                                WandScannedYes3b = IrPreviousObject.WandScannedYes3b,
                                WandScannedNo = IrPreviousObject.WandScannedNo,
                                BodyCameraYes = IrPreviousObject.BodyCameraYes,
                                BodyCameraNo = IrPreviousObject.BodyCameraNo,
                                Officer = new Officer
                                {
                                    FirstName = IrPreviousObject.Officer.FirstName,
                                    LastName = IrPreviousObject.Officer.LastName,
                                    Gender = IrPreviousObject.Officer.Gender,
                                    Phone = IrPreviousObject.Officer.Phone,
                                    Position = IrPreviousObject.Officer.Position,
                                    Email = IrPreviousObject.Officer.Email,
                                    LicenseNumber = IrPreviousObject.Officer.LicenseNumber,
                                    LicenseState = IrPreviousObject.Officer.LicenseState,
                                    CallSign = IrPreviousObject.Officer.CallSign,
                                    GuardMonth = IrPreviousObject.Officer.GuardMonth,
                                    NotifiedBy = IrPreviousObject.Officer.NotifiedBy,
                                    Billing = IrPreviousObject.Officer.Billing,
                                },
                                IsPositionPatrolCar = IrPreviousObject.IsPositionPatrolCar,
                                DateLocation = new DateLocation
                                {
                                    IncidentDate = IrPreviousObject.DateLocation.IncidentDate,
                                    ReportDate = IrPreviousObject.DateLocation.ReportDate,
                                    ReimbursementNo = IrPreviousObject.DateLocation.ReimbursementNo,
                                    ReimbursementYes = IrPreviousObject.DateLocation.ReimbursementYes,
                                    JobNumber = IrPreviousObject.DateLocation.JobNumber,
                                    JobTime = IrPreviousObject.DateLocation.JobTime,
                                    Duration = IrPreviousObject.DateLocation.Duration,
                                    Travel = IrPreviousObject.DateLocation.Travel,
                                    PatrolExternal = IrPreviousObject.DateLocation.PatrolExternal,
                                    PatrolInternal = IrPreviousObject.DateLocation.PatrolInternal,
                                    ClientType = IrPreviousObject.DateLocation.ClientType,
                                    ClientSite = IrPreviousObject.DateLocation.ClientSite,
                                    ClientArea = IrPreviousObject.DateLocation.ClientArea,
                                    ShowIncidentLocationAddress = IrPreviousObject.DateLocation.ShowIncidentLocationAddress,
                                    ClientAddress = IrPreviousObject.DateLocation.ClientAddress,
                                    State = IrPreviousObject.DateLocation.State,
                                    ClientStatus = IrPreviousObject.DateLocation.ClientStatus,
                                    ClientSiteLiveGps = IrPreviousObject.DateLocation.ClientSiteLiveGps,
                                },
                                LinkedSerialNos = IrPreviousObject.LinkedSerialNos,
                                Feedback = IrPreviousObject.Feedback,
                                ReportedBy = IrPreviousObject.ReportedBy,
                                FeedbackType = IrPreviousObject.FeedbackType,
                                FeedbackTemplates = IrPreviousObject.FeedbackTemplates,

                            };

                            ClientSites = _ViewDataService.GetUserClientSites(AuthUserHelper.LoggedInUserId, IrPreviousObject.DateLocation.ClientType);
                            FeedBackTemplates = _ViewDataService.GetFeedbackTemplatesByType((int)IrPreviousObject.FeedbackType);
                            if (IrPreviousObject.IsPositionPatrolCar)
                                OfficerPosition = ViewDataService.GetOfficerPositions(OfficerPositionFilter.PatrolOnly);
                            else
                                OfficerPosition = ViewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly);

                        }
                        else
                        {
                            Report = new IncidentRequest
                            {
                                Officer = new Officer
                                {

                                    Phone = "+61 4",

                                },
                            };
                            FeedBackTemplates = _ViewDataService.GetFeedbackTemplatesByType(ConfigDataProiver.GetFeedbackTypesId("General"));
                            OfficerPosition = ViewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly);
                        }

                    }
                    else
                    {   /*If it's Comming from any other page its clear session*/
                        HttpContext.Session.Remove("IRReport");
                        Report = new IncidentRequest
                        {
                            Officer = new Officer
                            {

                                Phone = "+61 4",

                            },
                        };
                        FeedBackTemplates = _ViewDataService.GetFeedbackTemplatesByType(ConfigDataProiver.GetFeedbackTypesId("General"));
                        OfficerPosition = ViewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly);

                    }

                }
                else
                {
                    /*If it's Comming from any other page its clear session*/
                    HttpContext.Session.Remove("IRReport");
                    Report = new IncidentRequest
                    {
                        Officer = new Officer
                        {

                            Phone = "+61 4",

                        },
                    };
                    FeedBackTemplates = _ViewDataService.GetFeedbackTemplatesByType(ConfigDataProiver.GetFeedbackTypesId("General"));
                    OfficerPosition = ViewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly);
                }

            }
            else
            {
                /*If it's Comming from any other page its clear session*/
                HttpContext.Session.Remove("IRReport");
                Report = new IncidentRequest
                {
                    Officer = new Officer
                    {

                        Phone = "+61 4",

                    },
                };
                FeedBackTemplates = _ViewDataService.GetFeedbackTemplatesByType(ConfigDataProiver.GetFeedbackTypesId("General"));
                OfficerPosition = ViewDataService.GetOfficerPositions(OfficerPositionFilter.NonPatrolOnly);

            }
            // to get the guard details while the guard is logging into the system-start
            if (HttpContext.Session.GetString("GuardId") != null)
            {
                var guardList = _ViewDataService.GetGuards().Where(x => x.Id == Convert.ToInt32(HttpContext.Session.GetString("GuardId")));
                foreach (var item in guardList)
                {
                    Report = new IncidentRequest
                    {
                        Officer = new Officer
                        {

                            Phone = item.Mobile,
                            LicenseNumber = item.SecurityNo,
                            Email = item.Email,

                        },
                    };
                }
            }
            // to get the guard details while the guard is logging into the system-end

            /* Remove the plate loaded((temp) ) for a user start */
            int LogId = AuthUserHelper.LoggedInUserId.GetValueOrDefault();
            var Count = _clientDataProvider.GetIncidentReportsPlatesCountWithOutPlateId(AuthUserHelper.LoggedInUserId.GetValueOrDefault());
            if (Count != 0)
            {
                if (HttpContext.Session.GetString("GuardId") != null)
                {
                    int guardId = Convert.ToInt32(HttpContext.Session.GetString("GuardId"));
                    var platesLoadedList = _clientDataProvider.GetIncidentReportsPlatesLoadedWithGuardId(AuthUserHelper.LoggedInUserId.GetValueOrDefault(), guardId);
                    if(platesLoadedList.Count!=0)
                    {
                        /*delete if a row (temp) exist  with GuardId*/
                        _clientDataProvider.RemoveIncidentReportsPlatesLoadedWithGuardId(platesLoadedList);
                    }
                    else
                    {
                        /*delete all the 0 (temp) entery for the login user */
                        _clientDataProvider.RemoveIncidentReportsPlatesLoadedWithUserId(LogId);
                    }
                }
            }
            /* Remove the plate loaded for a user end */

                        return Page();
            /* Code for Re-create the Ir from already existing one 04102023 end*/
        }

        #region Re-create IR
        /* This function is used to bind UI like Sitelocation Map,isPositionPatrolCar.. */
        public IActionResult OnGetIrDetails()
        {
            var clientSite = new ClientSite();
            var success = false;
            var currentPage = Request.Path;
            var bodyCameraYes = false;
            var reimbursementYes = false;
            var isPositionPatrolCar = false;
            var isSiteColorChecked = false;
            var isWandScannedYes3b = false;
            if (currentPage.ToString().ToLower().Contains("register"))
            {
                if (HttpContext.Session.GetString("IRReport") != null)
                {
                    if (HttpContext.Session.GetString("IsIrFromNotifyPage") != null)
                    {
                        var serializedObject = HttpContext.Session.GetString("IRReport");
                        var IrPreviousObject = JsonSerializer.Deserialize<IncidentRequest>(serializedObject);
                        clientSite = _clientDataProvider.GetClientSitesUsingName(IrPreviousObject.DateLocation.ClientSite);
                        bodyCameraYes = IrPreviousObject.BodyCameraYes;
                        reimbursementYes = IrPreviousObject.DateLocation.ReimbursementYes;
                        isPositionPatrolCar = IrPreviousObject.IsPositionPatrolCar;
                        isSiteColorChecked = IrPreviousObject.EventType.SiteColour;
                        isWandScannedYes3b = IrPreviousObject.WandScannedYes3b;
                        success = true;
                    }
                }
            }

            return new JsonResult(new { success, clientSite, bodyCameraYes, reimbursementYes, isPositionPatrolCar, isSiteColorChecked, isWandScannedYes3b });
        }
        #endregion
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

        //public IActionResult OnGetFeedbackTemplatesByType(FeedbackType type)
        //{
        //    return new JsonResult(_ViewDataService.GetFeedbackTemplatesByType((FeedbackType)type));
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// 

        //To get feedback template by selecting the type-start
        public IActionResult OnGetFeedbackTemplatesByType(int type)
        {

            return new JsonResult(_ViewDataService.GetFeedbackTemplatesByType(type));
        }
        //To get feedback template by selecting the type-end


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
            ///* Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 13,09,2023) */
            //message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            ///* Mail Id added Bcc globoconsoftware end */
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
            /* azure blob Implementation download link add to mail body 25-9-2023* Start*/
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
                    var blobHttpHeader = new BlobHttpHeaders { ContentType = "application/pdf" };
                    /*Commented for local testing ,uncomment when go on live*/
                    blobClient.Upload(fs, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
                    fs.Close();
                    messageHtml = messageHtml + "<p>Where PDF attachment is greater than 12 MB, it may not appear due to your organisation email limits. In this situation simply " +
                    "<a href=\" https://c4istorage1.blob.core.windows.net/irfiles/" + (new string(blobName.Take(8).ToArray()) + "/" + blobName) + "\" target=\"_blank\">" +
                    "click here</a> to download the Incident Report, which are unlimited in size.</p>";
                    messageHtml = messageHtml + "<p>File name : " + blobName + "</p>";
                }

            }
            /* azure blob Implementation 25-9-2023* End*/
            var builder = new BodyBuilder()
            {
                HtmlBody = messageHtml
            };
            /* Add attachment (IR PDF) to mail if Size <=12 MB , the link to download always add to  mail body Start*/
            FileInfo fileInfo = new FileInfo(fileName);
            var fileSizeInMB = (fileInfo.Length) / 1048576d;
            if (fileSizeInMB <= 12) // You can change this limit as needed
            {
                builder.Attachments.Add(fileName);

            }
            /* Add attachment to mail if Size <=12 MB end*/

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
            var PSPFName = _clientDataProvider.GetPSPF().SingleOrDefault(z => z.Name == Report.PSPFName);
            // var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite);
            try
            {
                /* Store the value of the Irresquest Object to seesion for create the Ir from the session start */
                HttpContext.Session.SetString("IRReport", JsonSerializer.Serialize(Report));
                /* Store the value of the Irresquest Object to seesion for create the Ir from the session end */
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
                IncidentReportEventTypes = Report.IrEventTypes.Select(z => new IncidentReportEventType() { EventType = z }).ToList(),
                PSPFId = PSPFName.Id

            };
            if (HttpContext.Session.GetString("GuardId") != null)
            {
                report.GuardId = Convert.ToInt32(HttpContext.Session.GetString("GuardId"));
            }

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


                    var ClientSiteRadioChecksActivityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == report.GuardId && x.ClientSiteId == report.ClientSiteId && x.GuardLoginTime != null);
                    foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                    {
                        ClientSiteRadioChecksActivity.NotificationCreatedTime = DateTime.Now;
                        _guardLogDataProvider.UpdateRadioChecklistEntry(ClientSiteRadioChecksActivity);
                    }


                    //for adding showing the IR information if an IR is created-start
                    //if (HttpContext.Session.GetString("GuardId") != null)
                    //{
                    //    var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                    //    {
                    //        ClientSiteId = Convert.ToInt32(report.ClientSiteId),
                    //        GuardId = Convert.ToInt32(HttpContext.Session.GetString("GuardId")),
                    //        LastIRCreatedTime = DateTime.Now,
                    //        IRId = report.Id,
                    //        ActivityType = "IR"
                    //    };
                    //    _guardLogDataProvider.SaveRadioChecklistEntry(clientsiteRadioCheck);
                    //}

                    //for adding showing the IR information if an IR is created-end
                    HttpContext.Session.Remove("GuardId");
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

                if (HttpContext.Session.GetString("GuardId") != null)
                {
                    report.GuardId = Convert.ToInt32(HttpContext.Session.GetString("GuardId"));
                }

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
        public JsonResult OnGetFeedbackTypesId(string filter)
        {


            var TruckConfigText = _configDataProvider.GetFeedbackTypesId(filter);





            return new JsonResult(new { TruckConfigText });
        }


    }
}
