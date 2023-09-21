using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using iText.IO.Image;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IO = System.IO;

using DocumentFormat.OpenXml.Bibliography;


using iText.IO.Font.Constants;

using iText.Kernel.Colors;
//using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;


namespace CityWatch.Web.Pages.Guard
{
    public class KeyVehicleLogModel : PageModel
    {
        private const string LAST_USED_DOCKET_SEQ_NO_CONFIG_NAME = "LastUsedDocketSn";
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientDataProvider _clientDataProvider;
        public readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly IKeyVehicleLogDocketGenerator _keyVehicleLogDocketGenerator;
        private readonly IDropboxService _dropboxUploadService;
        private readonly EmailOptions _emailOptions;
        private readonly Settings _settings;
        private readonly ILogger<KeyVehicleLogModel> _logger;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly string _imageRootDir;

        public KeyVehicleLogModel(IWebHostEnvironment webHostEnvironment,
            IGuardLogDataProvider guardLogDataProvider,
            IClientDataProvider clientDataProvider,
            IGuardDataProvider guardDataProvider,
            IViewDataService viewDataService,
            IKeyVehicleLogDocketGenerator keyVehicleLogDocketGenerator,
            IOptions<EmailOptions> emailOptions,
            IOptions<Settings> settings,
            IDropboxService dropboxService,
            ILogger<KeyVehicleLogModel> logger,
            IAppConfigurationProvider appConfigurationProvider)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
            _WebHostEnvironment = webHostEnvironment;
            _keyVehicleLogDocketGenerator = keyVehicleLogDocketGenerator;
            _dropboxUploadService = dropboxService;
            _emailOptions = emailOptions.Value;
            _settings = settings.Value;
            _logger = logger;
            _appConfigurationProvider = appConfigurationProvider;
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");


        }

        [BindProperty]
        public KeyVehicleLog KeyVehicleLog { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public void OnGet()
        {
            KeyVehicleLog = GetKeyVehicleLog();
            var logBookId = HttpContext.Session.GetInt32("LogBookId");
            var clientSiteId = _clientDataProvider.GetClientSiteLogBooks(logBookId, LogBookType.VehicleAndKeyLog)
                                .FirstOrDefault()?
                                .ClientSiteId;
            ViewData["IsDuressEnabled"] = clientSiteId != null && _viewDataService.IsClientSiteDuressEnabled(clientSiteId.Value);
        }

        public JsonResult OnGetKeyVehicleLogs(int logbookId, KvlStatusFilter kvlStatusFilter)
        {
            var results = _viewDataService.GetKeyVehicleLogs(logbookId, kvlStatusFilter)
                .OrderByDescending(z => z.Detail.EntryTime)
                .ThenByDescending(z => z.Detail.Id);
            return new JsonResult(results);
        }

        public IActionResult OnGetKeyVehicleLog(int id)
        {
            var keyVehicleLog = GetKeyVehicleLog();
            if (id != 0)
            {
                var keyVehicleLogInDb = _guardLogDataProvider.GetKeyVehicleLogById(id);
                if (keyVehicleLogInDb != null)
                {
                    keyVehicleLog = keyVehicleLogInDb;
                    /* Change in Attachement*/
                    ViewData["KeyVehicleLog_Attachments"] = _viewDataService.GetKeyVehicleLogAttachments(
                        IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), keyVehicleLogInDb.VehicleRego?.ToString())
                        .ToList();
                    ViewData["KeyVehicleLog_FilesPathRoot"] = "/KvlUploads/" + keyVehicleLogInDb.VehicleRego?.ToString();
                    ViewData["KeyVehicleLog_Keys"] = _viewDataService.GetKeyVehicleLogKeys(keyVehicleLogInDb).ToList();
                }
            }

            return new PartialViewResult
            {
                ViewName = "_KeyVehicleLogPopup",
                ViewData = new ViewDataDictionary<KeyVehicleLog>(ViewData, keyVehicleLog)
            };
        }

        public JsonResult OnGetAuditHistory(string vehicleRego)
        {
            return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistory(vehicleRego).ToList());
        }

        public JsonResult OnPostSaveKeyVehicleLog()
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(KeyVehicleLog, new ValidationContext(KeyVehicleLog), results, true))
                return new JsonResult(new { success = false, errors = results.Select(z => z.ErrorMessage) });

            var success = true;
            var message = "success";
            try
            {
                if (KeyVehicleLog.Product == null && KeyVehicleLog.ProductOther != null)
                {
                    if (string.IsNullOrEmpty(KeyVehicleLog.Product))
                    {
                        if (!string.IsNullOrEmpty(KeyVehicleLog.ProductOther))
                        {   /* Custom input saving as product if product not selected */
                            KeyVehicleLog.Product = KeyVehicleLog.ProductOther;
                        }
                    }

                }
                else if (KeyVehicleLog.Product != null && KeyVehicleLog.ProductOther != null)
                {
                    if (KeyVehicleLog.Product != KeyVehicleLog.ProductOther)
                    {
                        if (!string.IsNullOrEmpty(KeyVehicleLog.Product))
                        {
                            if (!string.IsNullOrEmpty(KeyVehicleLog.ProductOther))
                            {   /* Custom input saving as product if product not selected */
                                KeyVehicleLog.Product = KeyVehicleLog.ProductOther;
                            }
                        }
                    }

                }

                KeyVehicleLogAuditHistory keyVehicleLogAuditHistory = null;
                keyVehicleLogAuditHistory = GetKvlAuditHistory(KeyVehicleLog);
                _guardLogDataProvider.SaveKeyVehicleLog(KeyVehicleLog);
                var img = _guardLogDataProvider.GetCompanyDetails();
                string imagepath = null;
                foreach (var item in img)
                {
                    imagepath = item.PrimaryLogoPath;
                    break;
                }
                string toBeSearched = "images";
                string code = imagepath.Substring(imagepath.IndexOf(toBeSearched) + toBeSearched.Length);
                var reportRootDir = IO.Path.Combine(_WebHostEnvironment.WebRootPath, "Images");
                //string imagepath = Path.Combine(reportRootDir, "ziren.png");

                KeyVehicleLog.POIImage = Path.Combine("../images", "ziren.png");
                var profileId = GetKvlProfileId(KeyVehicleLog);
                keyVehicleLogAuditHistory.ProfileId = profileId;
                keyVehicleLogAuditHistory.KeyVehicleLogId = KeyVehicleLog.Id;
                _guardLogDataProvider.SaveKeyVehicleLogAuditHistory(keyVehicleLogAuditHistory);

                _guardLogDataProvider.SaveKeyVehicleLogProfileNotes(KeyVehicleLog.VehicleRego, KeyVehicleLog.Notes);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteKeyVehicleLog(int Id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.DeleteKeyVehicleLog(Id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostKeyVehicleLogQuickExit(int Id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.KeyVehicleLogQuickExit(Id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message });
        }

        public JsonResult OnGetProfileByRego(string truckRego)
        {
            //var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
            //string imagepath = Path.Combine(reportRootDir, "ziren.png");
            string imagepath = "~/images/ziren.png";
            return new JsonResult(_viewDataService.GetKeyVehicleLogProfilesByRegoNew(truckRego, imagepath).OrderBy(z => z, new KeyVehicleLogProfileViewModelComparer()));
        }

        public JsonResult OnGetProfileById(int id)
        {
            /* New Change for attachements#P7#97 Start 19/09/2023 */
            var keyVehicleLogDetails = _guardLogDataProvider.GetKeyVehicleLogProfileWithPersonalDetails(id);
            if (keyVehicleLogDetails != null)
            {


                keyVehicleLogDetails.Attachments = _viewDataService.GetKeyVehicleLogAttachments(
                    IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), keyVehicleLogDetails.KeyVehicleLogProfile.VehicleRego?.ToString())
                    .ToList();





            }
            /* New Change for attachements#P7#97 end 19/09/2023 */
            return new JsonResult(keyVehicleLogDetails);
        }

        public JsonResult OnGetClientSiteKeyDescription(int keyId, int clientSiteId)
        {
            return new JsonResult(_viewDataService.GetClientSiteKeyDescription(keyId, clientSiteId));
        }

        public JsonResult OnGetIsVehicleOnsite(int logbookId, string vehicleRego)
        {
            var isOpenInThisSite = _viewDataService.GetKeyVehicleLogs(logbookId, KvlStatusFilter.Open).Any(x => x.Detail.VehicleRego == vehicleRego);
            if (isOpenInThisSite)
                return new JsonResult(new { status = 1 });

            var keyVehicleLogFromOtherSite = _guardLogDataProvider.GetOpenKeyVehicleLogsByVehicleRego(vehicleRego)
                    .Where(z => z.ClientSiteLogBookId != logbookId)
                    .FirstOrDefault();

            if (keyVehicleLogFromOtherSite != null)
                return new JsonResult(new { status = 2, clientSite = keyVehicleLogFromOtherSite.ClientSiteLogBook.ClientSite.Name });


            return new JsonResult(new { status = 0 });
        }

        public JsonResult OnGetIsKeyAllocated(int logbookId, string keyNo)
        {
            return new JsonResult(_viewDataService.GetKeyVehicleLogs(logbookId, KvlStatusFilter.Open).Where(z => !string.IsNullOrEmpty(z.Detail.KeyNo)).Select(z => z.Detail.KeyNo).Any(x => x.Contains(keyNo)));
        }

        public JsonResult OnGetClientSiteKeys(int clientSiteId, string searchKeyNo, string searchKeyDesc)
        {
            return new JsonResult(_viewDataService.GetClientSiteKeys(clientSiteId, searchKeyNo, searchKeyDesc));
        }

        public JsonResult OnPostUpload()
        {
            var success = false;
            var files = Request.Form.Files;
            var reportReference = Request.Form["report_reference"].ToString();
            var vehicleRego = Request.Form["vehicleRego"].ToString();
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        var uploadFileName = IO.Path.GetFileName(file.FileName);
                        if (vehicleRego.Trim() != string.Empty)
                        {


                            /*var folderPath = IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads", reportReference);*/
                            /* Attachement Change */
                            var folderPath = IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads", vehicleRego);
                            if (!IO.Directory.Exists(folderPath))
                                IO.Directory.CreateDirectory(folderPath);
                            using (var stream = IO.File.Create(IO.Path.Combine(folderPath, uploadFileName)))
                            {
                                file.CopyTo(stream);
                            }
                            success = true;
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            return new JsonResult(new { attachmentId = Request.Form["attach_id"], success });
        }

        public JsonResult OnPostDeleteAttachment(string reportReference, string fileName,string vehicleRego)
        {
            var success = false;
            if (!string.IsNullOrEmpty(vehicleRego) && !string.IsNullOrEmpty(fileName))
            {
                var filePath = IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads", vehicleRego, fileName);
                if (IO.File.Exists(filePath))
                {
                    try
                    {
                        IO.File.Delete(filePath);
                        success = true;
                    }
                    catch
                    {

                    }
                }
            }
            return new JsonResult(success);
        }

        public JsonResult OnGetCompanyNames(string companyNamePart)
        {
            return new JsonResult(_viewDataService.GetCompanyNames(companyNamePart).ToList());
        }

        public JsonResult OnGetCompanyAndSenderNames(string companyNamePart)
        {
            return new JsonResult(_viewDataService.GetCompanyAndSenderNames(companyNamePart).ToList());
        }

        public JsonResult OnGetVehicleRegos(string regoPart)
        {
            //return new JsonResult(_guardLogDataProvider.GetVehicleRegos(regoPart).ToList());

            //the above code is commented beacause  the GetVehicleRegos is used in other pages 
            //along with that we have to check the entry given in any part of the corresponding vehicle rego for ticket no p7-95
            return new JsonResult(_guardLogDataProvider.GetVehicleRegosForKVL(regoPart).ToList());
            
        }

        public async Task<JsonResult> OnPostGenerateManualDocket(int id, ManualDocketReason option, string otherReason, string stakeholderEmails, int clientSiteId, string blankNoteOnOrOff)
        {
            var fileName = string.Empty;

            try
            {
                var serialNo = GetNextDocketSequenceNumber(id);
                fileName = _keyVehicleLogDocketGenerator.GeneratePdfReport(id, GetManualDocketReason(option, otherReason), blankNoteOnOrOff, serialNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            if (string.IsNullOrEmpty(fileName))
                return new JsonResult(new { fileName, message = "Failed to generate pdf", statusCode = -1 });

            var statusCode = 0;
            if (!string.IsNullOrEmpty(stakeholderEmails))
            {
                try
                {
                    var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(id);
                    SendEmail(keyVehicleLog.VehicleRego, stakeholderEmails, fileName);
                }
                catch (Exception ex)
                {
                    statusCode += -2;
                    _logger.LogError(ex.StackTrace);
                }
            }

            try
            {
                var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(id);
                var clientSiteLocation = string.Empty;
                if (keyVehicleLog != null)
                {
                    if (keyVehicleLog.ClientSiteLocation != null)
                        clientSiteLocation = keyVehicleLog.ClientSiteLocation.Name;
                }
                await UploadToDropbox(clientSiteId, fileName, clientSiteLocation);
            }
            catch (Exception ex)
            {
                statusCode += -3;
                _logger.LogError(ex.StackTrace);
            }

            return new JsonResult(new { fileName = @Url.Content($"~/Pdf/Output/{fileName}"), statusCode });
        }

        public JsonResult OnPostResetClientSiteLogBook(int clientSiteId, int guardLoginId)
        {
            var exMessage = new StringBuilder();
            try
            {
                var currentGuardLogin = _guardDataProvider.GetGuardLoginById(guardLoginId);
                var currentGuardLoginOffDutyActual = currentGuardLogin.OffDuty;
                var logOffDateTime = GuardLogBookHelper.GetLogOffDateTime();

                _guardDataProvider.UpdateGuardOffDuty(guardLoginId, logOffDateTime);

                var newLogBookId = _viewDataService.GetNewClientSiteLogBookId(clientSiteId, LogBookType.VehicleAndKeyLog);
                if (newLogBookId <= 0)
                    throw new InvalidOperationException("Failed to get client site log book");

                var newGuardLoginId = _viewDataService.GetNewGuardLoginId(currentGuardLogin, currentGuardLoginOffDutyActual, newLogBookId);
                if (newGuardLoginId <= 0)
                    throw new InvalidOperationException("Failed to login");

                _viewDataService.CopyOpenLogbookEntriesFromPreviousDay(currentGuardLogin.ClientSiteLogBookId, newLogBookId, newGuardLoginId);

                HttpContext.Session.SetInt32("LogBookId", newLogBookId);
                HttpContext.Session.SetInt32("GuardLoginId", newGuardLoginId);

                return new JsonResult(new { success = true, newLogBookId, newLogBookDate = DateTime.Today.ToString("dd MMM yyyy"), guardLoginId });
            }

            catch (Exception ex)
            {
                exMessage.AppendFormat("Error: {0}. ", ex.Message);

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    exMessage.Append("Attempt to create duplicate log book on same date. ");
                }
                exMessage.Append("Please logout and login again.");
            }

            return new JsonResult(new { success = false, message = exMessage.ToString() });
        }

        private KeyVehicleLog GetKeyVehicleLog()
        {
            int? logBookId = HttpContext.Session.GetInt32("LogBookId");
            if (logBookId == null)
                throw new InvalidOperationException("Session timeout due to user inactivity. Failed to get client site log book");

            int? guardLoginId = HttpContext.Session.GetInt32("GuardLoginId");
            if (guardLoginId == null)
                throw new InvalidOperationException("Session timeout due to user inactivity. Failed to get guard details");

            //var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == logBookId && z.Type == LogBookType.VehicleAndKeyLog);
            var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBooks(logBookId, LogBookType.VehicleAndKeyLog).SingleOrDefault();
            var guardLogin = _guardDataProvider.GetGuardLoginById(guardLoginId.Value);
            KeyVehicleLog ??= new KeyVehicleLog()
            {
                ClientSiteLogBookId = logBookId.Value,
                ClientSiteLogBook = clientSiteLogBook,
                GuardLoginId = guardLoginId.Value,
                GuardLogin = guardLogin,
                ReportReference = Guid.NewGuid()
            };
            return KeyVehicleLog;
        }

        public JsonResult OnPostDuplicateKeyVehicleLogProfile(int id, string personName)
        {
            var success = true;
            var message = "success";
            int kvlProfileId = 0;
            try
            {
                if (id == 0 || string.IsNullOrEmpty(personName))
                    throw new ArgumentNullException("Invalid parameters");

                var vehicleKeyLogProfile = _guardLogDataProvider.GetKeyVehicleLogProfileWithPersonalDetails(id);
                if (_guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(vehicleKeyLogProfile.KeyVehicleLogProfile.VehicleRego, personName).Any())
                    throw new ApplicationException("Visitor profile with same detials already exists");

                kvlProfileId = _guardLogDataProvider.SaveKeyVehicleLogVisitorPersonalDetail(new KeyVehicleLogVisitorPersonalDetail()
                {
                    ProfileId = vehicleKeyLogProfile.ProfileId,
                    CompanyName = vehicleKeyLogProfile.CompanyName,
                    PersonType = vehicleKeyLogProfile.PersonType,
                    PersonName = personName
                });
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message, kvlProfileId });
        }

        public JsonResult OnPostSaveClientSiteDuress(int clientSiteId, int guardId, int guardLoginId, int logBookId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var logbookId = _clientDataProvider.GetClientSiteLogBook(clientSiteId, LogBookType.DailyGuardLog, DateTime.Today)?.Id;
                logbookId ??= _clientDataProvider.SaveClientSiteLogBook(new ClientSiteLogBook()
                {
                    ClientSiteId = clientSiteId,
                    Type = LogBookType.DailyGuardLog,
                    Date = DateTime.Today
                });
                _viewDataService.EnableClientSiteDuress(clientSiteId, guardLoginId, logbookId.Value, guardId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message });
        }

        private async Task UploadToDropbox(int clientSiteId, string fileName)
        {
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId) ??
                throw new ArgumentException($"ClientSiteKpiSettings missing for this client site");

            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;
            if (string.IsNullOrEmpty(siteBasePath))
                throw new ArgumentException($"Dropbox directory missing for this client site");

            var fileToUpload = Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
            var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            var dbxFilePath = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{DateTime.Today.Date.Year}/{DateTime.Today.Date:yyyyMM} - {DateTime.Today.Date.ToString("MMMM").ToUpper()} DATA/{DateTime.Today.Date.ToString(dayPathFormat).ToUpper()}/{fileName}";
            var dropBoxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                        _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            await _dropboxUploadService.Upload(dropBoxSettings, fileToUpload, dbxFilePath);
        }


        private async Task UploadToDropbox(int clientSiteId, string fileName, string clientSiteLocation)
        {
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId) ??
                throw new ArgumentException($"ClientSiteKpiSettings missing for this client site");

            var siteBasePath = clientSiteKpiSettings.DropboxImagesDir;
            if (string.IsNullOrEmpty(siteBasePath))
                throw new ArgumentException($"Dropbox directory missing for this client site");

            var fileToUpload = Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName);
            var dayPathFormat = clientSiteKpiSettings.IsWeekendOnlySite ? "yyyyMMdd - ddd" : "yyyyMMdd";
            var folderPathToCreate = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{DateTime.Today.Date.Year}/{DateTime.Today.Date:yyyyMM} - {DateTime.Today.Date.ToString("MMMM").ToUpper()} DATA/{DateTime.Today.Date.ToString(dayPathFormat).ToUpper()}/Dockets - General/{fileName}"; ;
            if (!string.IsNullOrEmpty(clientSiteLocation))
            {
                folderPathToCreate = $"{siteBasePath}/FLIR - Wand Recordings - IRs - Daily Logs/{DateTime.Today.Date.Year}/{DateTime.Today.Date:yyyyMM} - {DateTime.Today.Date.ToString("MMMM").ToUpper()} DATA/{DateTime.Today.Date.ToString(dayPathFormat).ToUpper()}/Dockets - {string.Join("_", clientSiteLocation.Split(Path.GetInvalidFileNameChars()))}/{fileName}";

            }

            var dropBoxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                      _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            await _dropboxUploadService.Upload(dropBoxSettings, fileToUpload, folderPathToCreate);

        }
        private void SendEmail(string vehicleRego, string toAddresses, string fileName)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');
            var messageHtml = "Please find attached Manual Docket PrintOut";
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));

            if (!string.IsNullOrEmpty(toAddresses))
            {
                foreach (var address in toAddresses.Split(","))
                {
                    if (CommonHelper.IsValidEmail(address))
                        message.To.Add(new MailboxAddress(string.Empty, address.Trim()));
                }
            }

            var builder = new BodyBuilder()
            {
                HtmlBody = messageHtml
            };
            builder.Attachments.Add(Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName));
            message.Body = builder.ToMessageBody();
            message.Subject = $"Manual Docket PrintOut for ID No / Car or Truck Rego: {vehicleRego}";

            using (var client = new SmtpClient())
            {
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }
        }

        private static string GetManualDocketReason(ManualDocketReason reason, string otherReason)
        {
            switch (reason)
            {
                case ManualDocketReason.Other:
                    return $"Other: {otherReason}";
                case ManualDocketReason.NoComms:
                    return "Weighbridge Down = No Comms";
                case ManualDocketReason.PhysicalRepair:
                    return "Weighbridge Down = Physical Repair";
                case ManualDocketReason.POD:
                    return "Proof Of Delivery (Receipt)";
                default:
                    return string.Empty;
            }
        }

        private int GetKvlProfileId(KeyVehicleLog keyVehicleLog)
        {
            int profileId;
            var kvlPersonalDetail = new KeyVehicleLogVisitorPersonalDetail(keyVehicleLog);
            var personalDetails = _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(keyVehicleLog.VehicleRego);
            if (!personalDetails.Any() || !personalDetails.Any(z => z.Equals(kvlPersonalDetail)))
            {
                kvlPersonalDetail.KeyVehicleLogProfile.CreatedLogId = keyVehicleLog.Id;
                profileId = _guardLogDataProvider.SaveKeyVehicleLogProfileWithPersonalDetail(kvlPersonalDetail);
            }
            else
            {
                var kvlVisitorProfile = _guardLogDataProvider.GetKeyVehicleLogVisitorProfile(kvlPersonalDetail.KeyVehicleLogProfile.VehicleRego);
                if (personalDetails.Any(z => z.Equals(kvlPersonalDetail)))
                {
                    kvlPersonalDetail.Id = personalDetails.Where(z => z.PersonName == kvlPersonalDetail.PersonName).Max(z => z.Id);
                }
                profileId = _guardLogDataProvider.SaveKeyVehicleLogProfileWithPersonalDetail(kvlPersonalDetail);
                profileId = kvlVisitorProfile.Id;
            }

            return profileId;
        }

        private KeyVehicleLogAuditHistory GetKvlAuditHistory(KeyVehicleLog keyVehicleLog)
        {
            var isNewKvlEntry = keyVehicleLog.Id == 0;
            KeyVehicleLogAuditHistory keyVehicleLogAuditHistory;

            if (isNewKvlEntry)
            {
                keyVehicleLogAuditHistory = new KeyVehicleLogAuditHistory(keyVehicleLog, null);
            }
            else
            {
                var keyVehicleLogToUpdate = _guardLogDataProvider.GetKeyVehicleLogById(keyVehicleLog.Id);
                keyVehicleLogAuditHistory = new KeyVehicleLogAuditHistory(keyVehicleLog, keyVehicleLogToUpdate);
            }

            return keyVehicleLogAuditHistory;
        }

        private string GetNextDocketSequenceNumber(int id)
        {
            var lastSequenceNumber = 0;
            var keyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogById(id);
            if (!string.IsNullOrEmpty(keyVehicleLog.DocketSerialNo))
            {
                var serialNo = keyVehicleLog.DocketSerialNo;
                var suffix = Regex.Replace(serialNo, @"[^A-Z]+", string.Empty);
                int index = GetSuffixNumber(suffix);
                var numberSuffix = GetSequenceNumberSuffix(index);
                var serialnumber = string.Join("", serialNo.ToCharArray().Where(Char.IsDigit));
                return $"{serialnumber}-{numberSuffix}";
            }

            var configuration = _appConfigurationProvider.GetConfigurationByName(LAST_USED_DOCKET_SEQ_NO_CONFIG_NAME);
            if (configuration != null)
            {
                lastSequenceNumber = int.Parse(configuration.Value);
                lastSequenceNumber++;
                configuration.Value = lastSequenceNumber.ToString();
                _appConfigurationProvider.SaveConfiguration(configuration);
            }

            return lastSequenceNumber.ToString().PadLeft(6, '0');
        }

        private static int GetSuffixNumber(string suffix)
        {
            int index = 0;
            // Start suffix from B
            string alphabet = string.IsNullOrEmpty(suffix) ? "A" : suffix.ToUpper();
            for (int iChar = alphabet.Length - 1; iChar >= 0; iChar--)
            {
                char colPiece = alphabet[iChar];
                int colNum = colPiece - 64;
                index += colNum * (int)Math.Pow(26, alphabet.Length - (iChar + 1));
            }
            return index;
        }

        private static string GetSequenceNumberSuffix(int index)
        {
            string value = "";
            decimal number = index + 1;
            while (number > 0)
            {
                decimal currentLetterNumber = (number - 1) % 26;
                char currentLetter = (char)(currentLetterNumber + 65);
                value = currentLetter + value;
                number = (number - (currentLetterNumber + 1)) / 26;
            }
            return value;
        }
        public JsonResult OnGetImageLoad()
        {
            var imagepath = Path.Combine(_imageRootDir, "ziren.png");
            return new JsonResult(imagepath);
        }

    }
}