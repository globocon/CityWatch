using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
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
        }

        [BindProperty]
        public KeyVehicleLog KeyVehicleLog { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public void OnGet()
        {
            KeyVehicleLog = GetKeyVehicleLog();
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
                    ViewData["KeyVehicleLog_Attachments"] = _viewDataService.GetKeyVehicleLogAttachments(
                        IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads"), keyVehicleLogInDb.ReportReference?.ToString())
                        .ToList();
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
                KeyVehicleLogAuditHistory keyVehicleLogAuditHistory = null;
                keyVehicleLogAuditHistory = GetKvlAuditHistory(KeyVehicleLog);
                _guardLogDataProvider.SaveKeyVehicleLog(KeyVehicleLog);

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
            return new JsonResult(_viewDataService.GetKeyVehicleLogProfilesByRego(truckRego).OrderBy(z => z, new KeyVehicleLogProfileViewModelComparer()));
        }

        public JsonResult OnGetProfileById(int id)
        {
            return new JsonResult(_guardLogDataProvider.GetKeyVehicleLogProfileWithPersonalDetails(id));
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


            return new JsonResult(new { status = 0});
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
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        var uploadFileName = IO.Path.GetFileName(file.FileName);

                        var folderPath = IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads", reportReference);
                        if (!IO.Directory.Exists(folderPath))
                            IO.Directory.CreateDirectory(folderPath);
                        using (var stream = IO.File.Create(IO.Path.Combine(folderPath, uploadFileName)))
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

        public JsonResult OnPostDeleteAttachment(string reportReference, string fileName)
        {
            var success = false;
            if (!string.IsNullOrEmpty(reportReference) && !string.IsNullOrEmpty(fileName))
            {
                var filePath = IO.Path.Combine(_WebHostEnvironment.WebRootPath, "KvlUploads", reportReference, fileName);
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
            return new JsonResult(_viewDataService.GetCompanyAndSenderNames(companyNamePart).ToList());
        }

        public JsonResult OnGetVehicleRegos(string regoPart)
        {
            return new JsonResult(_guardLogDataProvider.GetVehicleRegos(regoPart).ToList());
        }

        public async Task<JsonResult> OnPostGenerateManualDocket(int id, ManualDocketReason option, string otherReason, string stakeholderEmails, int clientSiteId)
        {
            var fileName = string.Empty;

            try
            {
                var serialNo = GetNextDocketSequenceNumber(id);
                fileName = _keyVehicleLogDocketGenerator.GeneratePdfReport(id, GetManualDocketReason(option, otherReason), serialNo);
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
                await UploadToDropbox(clientSiteId, fileName);
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

            var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBooks().SingleOrDefault(z => z.Id == logBookId && z.Type == LogBookType.VehicleAndKeyLog);
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

        public JsonResult OnPostSaveKvClientSiteLogBookDuress(int clientSiteId, int GuardId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var logBookId = KeyVehicleLog.ClientSiteLogBookId;
                var GuardLoginId = KeyVehicleLog.GuardLoginId;
                var clientSiteLogBookDuress = _guardLogDataProvider.GetClientSiteDuress(clientSiteId);
                if (clientSiteLogBookDuress != null)
                {
                    var isActive = clientSiteLogBookDuress.IsEnabled;
                    if (isActive)
                    {
                        return new JsonResult(new { success = false, status = false });
                    }
                }
                _guardLogDataProvider.SaveClientSiteDuress(clientSiteId, GuardId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message });
        }

        public JsonResult OnGetKvDuressAlarmIsActive(int clientSiteId)
        {
            bool isActive = false;
            var clientSiteLogBookDuress = _guardLogDataProvider.GetClientSiteDuress(clientSiteId);
            if (clientSiteLogBookDuress != null)
            {
                isActive = clientSiteLogBookDuress.IsEnabled;
            }
            return new JsonResult(isActive);
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
            if (keyVehicleLog.DocketSerialNo != null)
            {
                var serialNo = keyVehicleLog.DocketSerialNo;
                var sufix = Regex.Replace(serialNo, @"[^A-Z]+", String.Empty);
                int index = GetSuffixNumber(sufix);
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
            return lastSequenceNumber.ToString().PadLeft(5, '0');
        }

        private int GetSuffixNumber(string suffix)
        {
            int index = 0;
            string alphabet = suffix.ToUpper();
            for (int iChar = alphabet.Length - 1; iChar >= 0; iChar--)
            {
                char colPiece = alphabet[iChar];
                int colNum = colPiece - 64;
                index = index + colNum * (int)Math.Pow(26, alphabet.Length - (iChar + 1));
            }
            return index;
        }

        private string GetSequenceNumberSuffix(int index)
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
    }
}