using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Models.DTO;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Pages.Incident;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.SignalR;
using CityWatch.Common.Models;
using CityWatch.Data.Services;
using ConvertApiDotNet;
using Dropbox.Api.Files;

//using iText.Kernel.Geom;
using iText.Layout;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Office.Interop.Access;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using CityWatch.Data;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CityWatch.Web.API
{


    [Route("api/[controller]")]
    [ApiController]
    public class GuardSecurityNumberController : ControllerBase
    {
        //public IncidentRequest Report { get; set; }
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientDataProvider _clientDataProvider;
        public readonly IMobileAppDataServices _mobileAppDataServices;
        private readonly ISiteEventLogDataProvider _SiteEventLogDataProvider;
        private readonly EmailOptions _emailOptions;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly ISmsSenderProvider _smsSenderProvider;
        private readonly IConfiguration _configuration;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly CityWatchDbContext _context;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        private readonly IIrDataProvider _irDataProvider;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IUserDataProvider _userDataProvider;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly IUserAuthenticationService _userAuthentication;
        private readonly IAlertEmailServices _alertEmailServices;
        private readonly IHubContext<UpdateHub> _webHubContext;
        private readonly IHubContext<MobileAppSignalRHub> _mobileHubContext;
        const string LAST_USED_IR_SEQ_NO_CONFIG_NAME = "LastUsedIrSn";


        /// <summary>
        /// Constructor for GuardSecurityNumberController.
        /// Injects caching, resilience, and data providers for optimized guard operations.
        /// </summary>
        public GuardSecurityNumberController(IGuardDataProvider guardDataProvider, IViewDataService viewDataService,
            ILogbookDataService logbookDataService, IGuardLogDataProvider guardLogDataProvider,
            IClientDataProvider clientDataProvider, ISiteEventLogDataProvider siteEventLogDataProvider,
            IWebHostEnvironment webHostEnvironment, ISmsSenderProvider smsSenderProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration, IConfigDataProvider configDataProvider, IIrDataProvider irDataProvider,
            ILogger<RegisterModel> logger, IUserDataProvider userDataProvider, IIncidentReportGenerator incidentReportGenerator,
            IAppConfigurationProvider appConfigurationProvider, IUserAuthenticationService userAuthentication,
            IMobileAppDataServices mobileAppDataServices, IAlertEmailServices alertEmailServices,
            Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache, CityWatchDbContext context,
            IHubContext<UpdateHub> webHubContext, IHubContext<MobileAppSignalRHub> mobileHubContext)
        {
            _context = context;
            _memoryCache = memoryCache;
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
            _logbookDataService = logbookDataService;
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
            _SiteEventLogDataProvider = siteEventLogDataProvider;
            _WebHostEnvironment = webHostEnvironment;
            _smsSenderProvider = smsSenderProvider;
            _emailOptions = emailOptions.Value;
            _configuration = configuration;
            _configDataProvider = configDataProvider;
            _irDataProvider = irDataProvider;
            _logger = logger;
            _userDataProvider = userDataProvider;
            _incidentReportGenerator = incidentReportGenerator;
            _appConfigurationProvider = appConfigurationProvider;
            _userAuthentication = userAuthentication;
            _mobileAppDataServices = mobileAppDataServices;
            _alertEmailServices = alertEmailServices;
            _webHubContext = webHubContext;
            _mobileHubContext = mobileHubContext;
        }

        /// <summary>
        /// Retrieves guard profile details by security license number.
        /// [Optimization]: Uses targeted DB lookup and .AsNoTracking() for high performance.
        /// </summary>
        [HttpGet("GetGuardDetails/{securityNumber}")]
        public IActionResult GetGuardDetails(string securityNumber)
        {
            if (string.IsNullOrWhiteSpace(securityNumber))
                return BadRequest(new { message = "Security number is required." });

            // [Optimization]: Switched from in-memory collection filtering 
            // to a targeted database query via GetGuardBySecurityNo.
            var guard = _guardDataProvider.GetGuardBySecurityNo(securityNumber);

            if (guard == null)
            {
                return NotFound("User not found. Please check if input is correct.\n If you are new, Please click Register.");
            }

            if (!guard.IsActive)
            {
                //return Unauthorized(new
                //{
                //    message = "A guard with given security license number is disabled. Please contact admin to activate.",
                //    isActive = false
                //});
                return Unauthorized("A guard with given security license number is disabled.\n Please contact admin to activate.");
            }

            if (!guard.IsMobileAppAccess && !guard.IsMobileAppPlusTags)
            {
                return Unauthorized("Access denied !!!. Please contact admin.");
            }

            var CalendarEvents = _configDataProvider.GetBroadcastCalendarEventsByDate();
            var LiveEventsNotExpired = _configDataProvider.GetBroadcastLiveEventsNotExpired();
            var LiveEventsNotExpiredUrls = _configDataProvider.GetUrlsInsideBroadcastLiveEventsNotExpired();
            var LiveEventsweblink = _configDataProvider.GetBroadcastLiveEventsWeblink();


            //HRList Status start 
            var HR1 = "Grey";
            var HR2 = "Grey";
            var HR3 = "Grey";
            bool guardLockStatusBasedOnRedDoc = false;
            if (guard != null)
            {
                var hrGroupStatusesNew = LEDStatusForLoginUser(guard.Id);
                if (hrGroupStatusesNew != null && hrGroupStatusesNew.Count > 0)
                {
                    if (hrGroupStatusesNew != null || hrGroupStatusesNew.Count != 0)
                    {


                        // Group document statuses by GroupName for faster lookups
                        var statusLookup = hrGroupStatusesNew.ToLookup(x => x.GroupName.Trim());


                        // Set HR1Status
                        var HR1List = statusLookup["HR 1 (C4i)"];
                        if (HR1List.Any())
                        {
                            HR1 = HR1List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                 HR1List.Any(x => x.ColourCodeStatus == "Orange") ? "Orange" :
                                              HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }

                        // Set HR2Status
                        var HR2List = statusLookup["HR 2 (Client)"];
                        if (HR2List.Any())
                        {
                            HR2 = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                HR2List.Any(x => x.ColourCodeStatus == "Orange") ? "Orange" :
                                              HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }

                        // Set HR3Status
                        var HR3List = statusLookup["HR 3 (Special)"];
                        if (HR3List.Any())
                        {
                            HR3 = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                HR3List.Any(x => x.ColourCodeStatus == "Orange") ? "Orange" :
                                              HR3List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }






                    }
                }


            }

            return Ok(new
            {
                GuardId = guard.Id,
                Name = guard.Name,
                SecurityNo = guard.SecurityNo,
                isActive = true,
                HR1Status = HR1,
                HR2Status = HR2,
                HR3Status = HR3,
                GuardLockStatusBasedOnRedDoc = guardLockStatusBasedOnRedDoc,
                CalendarEvents = CalendarEvents,
                LiveEventsNotExpired = LiveEventsNotExpired,
                LiveEventsNotExpiredUrls = LiveEventsNotExpiredUrls,
                LiveEventsweblink = LiveEventsweblink
            });
        }

        [HttpGet("GetClientSiteDetails/{clientsiteid}")]
        public IActionResult GetClientSiteDetails(string clientsiteid)
        {
            if (string.IsNullOrWhiteSpace(clientsiteid) || clientsiteid == "0")
                return BadRequest("Client Site is required.");

            int clientid = int.Parse(clientsiteid);
            var site = _clientDataProvider.GetClientSiteDetailsWithId(clientid).FirstOrDefault();
            ClientSiteDto _clientSite = new ClientSiteDto();
            if (site != null)
            {
                _clientSite = new ClientSiteDto
                {
                    Id = site.Id,
                    TypeId = site.TypeId,
                    Name = site.Name,
                    Address = site.Address,
                    State = site.State,
                    Gps = site.Gps,
                    Billing = site.Billing,
                    Status = site.Status,
                    StatusDate = site.StatusDate,
                    SiteEmail = site.SiteEmail,
                    LandLine = site.LandLine,
                    DuressEmail = site.DuressEmail,
                    DuressSms = site.DuressSms,
                    UploadGuardLog = site.UploadGuardLog,
                    UploadFusionLog = site.UploadFusionLog,
                    GuardLogEmailTo = site.GuardLogEmailTo,
                    DataCollectionEnabled = site.DataCollectionEnabled,
                    IsActive = site.IsActive,
                    IsDosDontList = site.IsDosDontList,
                    MobAppShowClientTypeandSite = site.MobAppShowClientTypeandSite
                };
            }

            return Ok(_clientSite);
        }

        private List<HRGroupStatusNew> LEDStatusForLoginUser(int GuardID)
        {
            // Retrieve guard document details in one call
            var guardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            // Iterate through each document detail
            foreach (var item in guardDocumentDetails)
            {
                // Directly use the item without filtering again
                hrGroupStatusesNew.Add(new HRGroupStatusNew
                {
                    documentDescription = item.Description,
                    Status = 1,
                    GroupName = item.HrGroupText.Trim(), // Assuming HrGroupText replaces GroupName
                                                         // Generate the color code based on the current item
                    ColourCodeStatus = GuardledColourCodeGenerator(new List<GuardComplianceAndLicense> { item })
                });
            }

            return hrGroupStatusesNew;
        }

        [HttpGet("CheckIfPINSetForTheGuard")]
        public IActionResult CheckIfPINSetForTheGuard(int guardId)
        {
            var AccessPermission = false;
            string SuccessMessage = string.Empty;
            var guard = _guardDataProvider.GetGuardDetailsUsingId(guardId);
            var firstGuard = guard.FirstOrDefault();
            if (firstGuard != null && firstGuard.Pin != null)
            {
                SuccessMessage = "Pin alerady Set ";
            }
            else
            {
                AccessPermission = true;
                SuccessMessage = "No PIN Set for you";
            }

            return Ok(new { data = AccessPermission, message = SuccessMessage });
        }

        public class SaveNewPINRequest
        {
            public int guardId { get; set; }
            public string newPin { get; set; }
        }

        [HttpPost("SaveNewPINSetForTheGuard")]
        public IActionResult SaveNewPINSetForTheGuard([FromBody] SaveNewPINRequest request)
        {
            var AccessPermission = false;
            string SuccessMessage = string.Empty;
            if (!string.IsNullOrEmpty(request.newPin))
            {
                var guard = _guardDataProvider.GetGuardDetailsUsingId(request.guardId);
                var firstGuard = guard.FirstOrDefault();
                if (firstGuard != null && firstGuard.Pin != null)
                {
                    SuccessMessage = "Pin alerady Set ";
                }
                else
                {
                    _guardDataProvider.SetGuardNewPIN(request.guardId, request.newPin);
                    AccessPermission = true;
                    SuccessMessage = "New PIN Set for you";
                }
            }
            else
            {
                SuccessMessage = "Enter your New PIN";
            }

            return Ok(new { data = AccessPermission, message = SuccessMessage });
        }

        public class ResetPinRequest
        {
            public int guardId { get; set; }
            public string siteName { get; set; }
        }

        [HttpPost("ResetGaurdHrPin")]
        public IActionResult ResetGaurdHrPin([FromBody] ResetPinRequest request)
        {
            var message = string.Empty;
            var success = false;
            var guard = _guardDataProvider.GetGuards().FirstOrDefault(z => z.Id == request.guardId);

            if (guard != null && !string.IsNullOrEmpty(guard.Email) && !string.IsNullOrEmpty(guard.Pin))
            {
                var emailBody = GetPasswordResetEmail(guard.Name, guard.Pin, request.siteName);
                SendEmailNew(emailBody, guard.Email);

                message = $"PIN sent to the email ID: {guard.Email}";
                success = true;
            }
            else
            {
                message = "Invalid guard details or missing email/PIN.";
                success = false;
            }

            return Ok(new { data = success, message = message });
        }

        private void SendEmailNew(string mailBodyHtml, string ToAddress)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');
            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));

            var message = new MimeMessage();
            if (fromAddress.Length > 1)
            {
                message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            }
            else
            {
                message.From.Add(new MailboxAddress(fromAddress[0], fromAddress[0]));
            }

            if (emailAddresses != null && emailAddresses != "")
            {
                var toAddressNew = emailAddresses.Split(',');
                foreach (var address in toAddressNew)
                {
                    if (!string.IsNullOrWhiteSpace(address) && MailboxAddress.TryParse(address.Trim(), out var mailbox))
                        message.To.Add(mailbox);
                }
            }
            if (ToAddress != null && ToAddress != "")
            {
                var toAddressNew = ToAddress.Split(',');
                foreach (var address in toAddressNew)
                {
                    if (!string.IsNullOrWhiteSpace(address) && MailboxAddress.TryParse(address.Trim(), out var mailbox))
                    {
                        if (!message.To.Contains(mailbox))
                        {
                            message.To.Add(mailbox);
                        }
                    }
                }
            }

            message.Subject = "HR Document PIN Reset";
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            var builder = new BodyBuilder()
            {
                HtmlBody = mailBodyHtml
            };
            message.Body = builder.ToMessageBody();
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

        public string GetPasswordResetEmail(string userName, string temporaryPassword, string siteName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body {");
            sb.AppendLine("    font-family: Arial, sans-serif;");
            sb.AppendLine("    line-height: 1.6;");
            sb.AppendLine("    color: #333;");
            sb.AppendLine("    background-color: #f9f9f9;");
            sb.AppendLine("    margin: 0;");
            sb.AppendLine("    padding: 0;");
            sb.AppendLine("}");
            sb.AppendLine(".email-container {");
            sb.AppendLine("    width: 100%;");
            sb.AppendLine("    max-width: 600px;");
            sb.AppendLine("    margin: 20px auto;");
            sb.AppendLine("    background-color: #ffffff;");
            sb.AppendLine("    border: 1px solid #ddd;");
            sb.AppendLine("    padding: 20px;");
            sb.AppendLine("    border-radius: 8px;");
            sb.AppendLine("    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);");
            sb.AppendLine("}");
            sb.AppendLine(".email-header {");
            sb.AppendLine("    text-align: center;");
            sb.AppendLine("    font-size: 18px;");
            sb.AppendLine("    font-weight: bold;");
            sb.AppendLine("    margin-bottom: 20px;");
            sb.AppendLine("}");
            sb.AppendLine(".temporary-password {");
            sb.AppendLine("    font-weight: bold;");
            sb.AppendLine("    background-color: #f2f2f2;");
            sb.AppendLine("    padding: 5px 10px;");
            sb.AppendLine("    border-radius: 5px;");
            sb.AppendLine("    display: inline-block;");
            sb.AppendLine("    margin-left: 5px;"); // Slight spacing after the label
            sb.AppendLine("}");
            sb.AppendLine(".footer {");
            sb.AppendLine("    margin-top: 20px;");
            sb.AppendLine("    font-size: 12px;");
            sb.AppendLine("    color: #666;");
            sb.AppendLine("    text-align: center;");
            sb.AppendLine("}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"email-container\">");
            sb.AppendLine("    <div class=\"email-header\">");
            sb.AppendLine("        HR PIN Reset Request");
            sb.AppendLine("    </div>");
            sb.AppendLine($"    <p>Hi {userName},</p>");
            sb.AppendLine($"    <p>Here is your HR PIN: <span class=\"temporary-password\">{temporaryPassword}</span></p>");
            sb.AppendLine($"    <p>Logged in Site: <span class=\"temporary-password\">{siteName}</span></p>");
            sb.AppendLine("    <div class=\"footer\">");
            sb.AppendLine("        <p>If you have any questions, please contact our support team.</p>");
            sb.AppendLine($"        <p>&copy; {DateTime.Today.Year} C4i System. All rights reserved.</p>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }
        private string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> selectedList)
        {
            var today = DateTime.Now;
            var colourCode = "Green"; // Default to green

            if (selectedList.Count > 0)
            {
                // Check if any entry has DateType == true
                var hasDateTypeTrue = selectedList.Any(x => x.DateType == true);

                if (hasDateTypeTrue)
                {
                    return "Green"; // Return immediately if DateType == true exists
                }

                // Get the first non-null expiry date (if any)
                var firstItem = selectedList.OrderBy(x => x.IsPending).FirstOrDefault(x => x.ExpiryDate != null);

                if (firstItem != null)
                {
                    var expiryDate = firstItem.ExpiryDate.Value; // Assuming ExpiryDate is not null here
                    var daysAfterExpiry = (today.Date - expiryDate.Date).TotalDays;
                    // Compare expiry date with today's date
                    if (expiryDate < today)
                    {
                        // EXPLANATION: If the record is expired but marked as "Pending" (toggle ON), 
                        // it will show an ORANGE clock to indicate a grace period.
                        // After 99 days past the expiry date, this grace period expires and it forcefully turns RED.
                        if (firstItem.IsPending && daysAfterExpiry <= 99)
                        {
                            return "Orange";
                        }
                        else
                        {
                            return "Red";
                        }
                    }
                    else if ((expiryDate - today).Days < 45)
                    {
                        return "Yellow";
                    }
                }
            }

            return colourCode; // Default return is green
        }
        public class HRGroupStatusNew
        {

            public int Status { get; set; }

            public string GroupName { get; set; }
            public string ColourCodeStatus { get; set; }

            public string documentDescription { get; set; }
        }

        [HttpGet("GetUserClientTypes")]
        public IActionResult GetUserClientTypes(int userId, int? clientTypeId = null)
        {
            try
            {
                var clientTypes = GetUserClientTypesWithId(userId);

                if (clientTypes == null || !clientTypes.Any())
                    return NotFound(new { message = "No client types found." });

                return Ok(clientTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }


        [HttpGet("GetClientSitesByClientType")]
        public IActionResult GetClientSitesByClientType(int userId, int clientTypeId)
        {
            try
            {
                var clientSites = _viewDataService.GetUserClientSitesUsingId(userId, clientTypeId);

                if (clientSites == null || !clientSites.Any())
                    return NotFound(new { message = "No client sites found." });

                return Ok(clientSites);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        //This to be removed after ios update
        [HttpPost("EnterGuardLogin")]
        public IActionResult EnterGuardLogin([FromBody] PostActivityRequest request)
        {
            try
            {

                if (request.guardId <= 0 || request.clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });

                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(request.clientsiteId, logBookType);

                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                // Get Guard Login ID
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, request.guardId, request.clientsiteId, request.userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Default GPS coordinates (should be replaced with actual values if available)
                var gpsCoordinates = request.gps;

                // Create a log entry
                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = request.activityString ?? "Logbook Logged In (Mob App)",
                    IsSystemEntry = request.systemEntry,
                    EventDateTimeLocal = request.EventDateTimeLocal ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = request.EventDateTimeLocalWithOffset ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = request.EventDateTimeZone ?? TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = request.EventDateTimeZoneShort ?? TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = request.EventDateTimeUtcOffsetMinute ?? TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gpsCoordinates,
                    EventMobileUtcDateTime = request.EventMobileUtcDateTime
                };

                _guardLogDataProvider.SaveGuardLog(signInEntry);

                //Predefined Activity for client site refer GetActivities in this page if this is modified
                // ################### Start ################
                List<ActivityModel>? activity = new();
                try
                {
                    activity = _viewDataService.GetDressAppFields(2, request.clientsiteId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while fetching activities: " + ex.Message);
                }
                // ################### End ################

                //GetPatrolCarLogs for client site
                // ################### Start ################
                List<PatrolCarLog> patrolCarLogs = new();
                try
                {
                    patrolCarLogs = _viewDataService.GetPatrolCarLogs(logBookId, request.clientsiteId);
                }
                catch (Exception ex)
                {

                    Console.WriteLine("An error occurred while fetching patrolCarLogs: " + ex.Message);
                }
                // ################### End ################

                //GetCustomFieldLogs for client site
                // ################### Start ################
                List<Dictionary<string, string>> customFieldLogs = new();
                try
                {
                    customFieldLogs = _viewDataService.GetCustomFieldLogs(logBookId, request.clientsiteId);
                }
                catch (Exception ex)
                {

                    Console.WriteLine("An error occurred while fetching customFieldLogs: " + ex.Message);
                }
                // ################### End ################

                //RCLinkedDuressClientSites for client site
                // ################### Start ################
                List<RCLinkedDuressClientSites> _rcLinkedClientSites = new();
                try
                {
                    var getallRCLinkedDuressMaster = _guardLogDataProvider.getallRCLinkedDuressMaster();
                    _rcLinkedClientSites = _guardLogDataProvider.getallClientSitesLinkedDuress(request.clientsiteId);
                    var _check = getallRCLinkedDuressMaster.Where(x => x.Id == _rcLinkedClientSites?.FirstOrDefault()?.RCLinkedId)?.FirstOrDefault();
                    if (_check != null)
                    {
                        if (!_check.IsSW)
                        {
                            //allow only if smartwand is enabled in linked sites
                            _rcLinkedClientSites = new List<RCLinkedDuressClientSites>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while fetching Rc Linked ClientSites: " + ex.Message);
                }
                // ################### End ##################


                // Data for Offline IR creation
                // ################### Start ################
                string cacheKey = $"OfflineData_{request.userId}_{request.clientsiteId}";

                // [Optimization]: Memory Cache Pattern
                // Metadata lists like ClientSites and FeedbackTemplates are served from RAM 
                // to reduce SQL load during high-concurrency login events.
                if (!_memoryCache.TryGetValue(cacheKey, out (
                    List<DropdownItem> clientTypes,
                    List<ClientSiteDto> clientSites,
                    List<Data.Providers.FeedbackTemplateViewModel> feedbackTemplates,
                    List<string> notifiedByList,
                    List<SelectListItem> areas,
                    List<Mp3File> audio,
                    List<Mp3File> multimedia) cachedData))
                {
                    List<DropdownItem> cache_clientTypes = new List<DropdownItem>();
                    try { cache_clientTypes = GetUserClientTypesWithId(request.userId); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<ClientSiteDto> cache_clientSites = new List<ClientSiteDto>();
                    try { var unFilteredClientSites = GetClientSitesForIR(); cache_clientSites = unFilteredClientSites.Where(cs => cache_clientTypes.Any(ct => ct.Id == cs.TypeId)).ToList(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<Data.Providers.FeedbackTemplateViewModel> cache_feedbackTemplates = new List<Data.Providers.FeedbackTemplateViewModel>();
                    try { cache_feedbackTemplates = GetAndReturnFeedbackTemplates(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<string> cache_notifiedByList = new List<string>();
                    try { cache_notifiedByList = GetNotifiedReportFieldsByType(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<SelectListItem> cache_areas = new List<SelectListItem>();
                    try { cache_areas = GetClientSiteArea(-1); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<Mp3File> cache_audio = new List<Mp3File>();
                    try { cache_audio = GetAudioForMobileApp(1); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<Mp3File> cache_multimedia = new List<Mp3File>();
                    try { cache_multimedia = GetAudioForMobileApp(3); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    cachedData = (cache_clientTypes, cache_clientSites, cache_feedbackTemplates, cache_notifiedByList, cache_areas, cache_audio, cache_multimedia);
                    _memoryCache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(10));
                }

                List<DropdownItem> clientTypes = cachedData.clientTypes;
                List<ClientSiteDto> clientSites = cachedData.clientSites;
                List<Data.Providers.FeedbackTemplateViewModel> feedbackTemplates = cachedData.feedbackTemplates;
                List<string> notifiedByList = cachedData.notifiedByList;
                List<SelectListItem> areas = cachedData.areas;
                List<Mp3File> audio = cachedData.audio;
                List<Mp3File> multimedia = cachedData.multimedia;
                // ################### End ##################


                var clientsiteDetails = _clientDataProvider.GetClientSiteDetailsWithId(request.clientsiteId).FirstOrDefault();

                try
                {
                    if (request.IsNewGuard)
                    {
                        var _nwGuard = _guardDataProvider.GetGuardDetailsUsingId(request.guardId).FirstOrDefault();
                        _alertEmailServices.SendNewGuardRegisterAlertMail(_nwGuard, clientsiteDetails.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending new guard registration email: " + ex.Message);
                }

                return Ok(new
                {
                    message = "Guard successfully logged in.",
                    guardLoginId,
                    TourMode = (int)clientsiteDetails.PatrolTourMode,
                    Activity = activity,
                    PatrolCarLog = patrolCarLogs,
                    CustomFieldLog = customFieldLogs.ToArray(),
                    rcLinkedClientSites = _rcLinkedClientSites,
                    irClientTypes = clientTypes,
                    irClientSites = clientSites,
                    irFeedbackTemplates = feedbackTemplates,
                    irNotifiedByList = notifiedByList,
                    irAreas = areas,
                    audioList = audio,
                    multimediaList = multimedia
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }

        }

        [HttpPost("EnterGuardLoginNew")]
        public IActionResult EnterGuardLoginNew([FromBody] PostActivityRequest request)
        {
            try
            {

                if (request.guardId <= 0 || request.clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });

                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(request.clientsiteId, logBookType);

                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                // Get Guard Login ID
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, request.guardId, request.clientsiteId, request.userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Default GPS coordinates (should be replaced with actual values if available)
                var gpsCoordinates = request.gps;

                // Create a log entry
                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = request.activityString ?? "Logbook Logged In (Mob App)",
                    IsSystemEntry = request.systemEntry,
                    EventDateTimeLocal = request.EventDateTimeLocal ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = request.EventDateTimeLocalWithOffset ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = request.EventDateTimeZone ?? TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = request.EventDateTimeZoneShort ?? TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = request.EventDateTimeUtcOffsetMinute ?? TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gpsCoordinates,
                    EventMobileUtcDateTime = request.EventMobileUtcDateTime
                };

                _guardLogDataProvider.SaveGuardLog(signInEntry);

                //Predefined Activity for client site refer GetActivities in this page if this is modified
                // ################### Start ################
                List<ActivityModelDTO>? activity = new();
                try
                {
                    activity = _viewDataService.GetPreDefinedActivitesFields();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while fetching activities: " + ex.Message);
                }
                // ################### End ################

                //GetPatrolCarLogs for client site
                // ################### Start ################
                List<PatrolCarLog> patrolCarLogs = new();
                try
                {
                    patrolCarLogs = _viewDataService.GetPatrolCarLogs(logBookId, request.clientsiteId);
                }
                catch (Exception ex)
                {

                    Console.WriteLine("An error occurred while fetching patrolCarLogs: " + ex.Message);
                }
                // ################### End ################

                //GetCustomFieldLogs for client site
                // ################### Start ################
                List<Dictionary<string, string>> customFieldLogs = new();
                try
                {
                    customFieldLogs = _viewDataService.GetCustomFieldLogs(logBookId, request.clientsiteId);
                }
                catch (Exception ex)
                {

                    Console.WriteLine("An error occurred while fetching customFieldLogs: " + ex.Message);
                }
                // ################### End ################

                //RCLinkedDuressClientSites for client site
                // ################### Start ################
                List<RCLinkedDuressClientSites> _rcLinkedClientSites = new();
                try
                {
                    var getallRCLinkedDuressMaster = _guardLogDataProvider.getallRCLinkedDuressMaster();
                    _rcLinkedClientSites = _guardLogDataProvider.getallClientSitesLinkedDuress(request.clientsiteId);
                    var _check = getallRCLinkedDuressMaster.Where(x => x.Id == _rcLinkedClientSites?.FirstOrDefault()?.RCLinkedId)?.FirstOrDefault();
                    if (_check != null)
                    {
                        if (!_check.IsSW)
                        {
                            //allow only if smartwand is enabled in linked sites
                            _rcLinkedClientSites = new List<RCLinkedDuressClientSites>();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while fetching Rc Linked ClientSites: " + ex.Message);
                }
                // ################### End ##################


                // Data for Offline IR creation
                // ################### Start ################
                string cacheKey = $"OfflineData_{request.userId}_{request.clientsiteId}";

                // [Optimization]: Memory Cache Pattern
                // Metadata lists like ClientSites and FeedbackTemplates are served from RAM 
                // to reduce SQL load during high-concurrency login events.
                if (!_memoryCache.TryGetValue(cacheKey, out (
                    List<DropdownItem> clientTypes,
                    List<ClientSiteDto> clientSites,
                    List<Data.Providers.FeedbackTemplateViewModel> feedbackTemplates,
                    List<string> notifiedByList,
                    List<SelectListItem> areas,
                    List<Mp3File> audio,
                    List<Mp3File> multimedia) cachedData))
                {
                    List<DropdownItem> cache_clientTypes = new List<DropdownItem>();
                    try { cache_clientTypes = GetUserClientTypesWithId(request.userId); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<ClientSiteDto> cache_clientSites = new List<ClientSiteDto>();
                    try { var unFilteredClientSites = GetClientSitesForIR(); cache_clientSites = unFilteredClientSites.Where(cs => cache_clientTypes.Any(ct => ct.Id == cs.TypeId)).ToList(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<Data.Providers.FeedbackTemplateViewModel> cache_feedbackTemplates = new List<Data.Providers.FeedbackTemplateViewModel>();
                    try { cache_feedbackTemplates = GetAndReturnFeedbackTemplates(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<string> cache_notifiedByList = new List<string>();
                    try { cache_notifiedByList = GetNotifiedReportFieldsByType(); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<SelectListItem> cache_areas = new List<SelectListItem>();
                    try { cache_areas = GetClientSiteArea(-1); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<Mp3File> cache_audio = new List<Mp3File>();
                    try { cache_audio = GetAudioForMobileApp(1); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    List<Mp3File> cache_multimedia = new List<Mp3File>();
                    try { cache_multimedia = GetAudioForMobileApp(3); } catch (Exception ex) { Console.WriteLine(ex.ToString()); }

                    cachedData = (cache_clientTypes, cache_clientSites, cache_feedbackTemplates, cache_notifiedByList, cache_areas, cache_audio, cache_multimedia);
                    _memoryCache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(10));
                }

                List<DropdownItem> clientTypes = cachedData.clientTypes;
                List<ClientSiteDto> clientSites = cachedData.clientSites;
                List<Data.Providers.FeedbackTemplateViewModel> feedbackTemplates = cachedData.feedbackTemplates;
                List<string> notifiedByList = cachedData.notifiedByList;
                List<SelectListItem> areas = cachedData.areas;
                List<Mp3File> audio = cachedData.audio;
                List<Mp3File> multimedia = cachedData.multimedia;
                // ################### End ##################


                var clientsiteDetails = _clientDataProvider.GetClientSiteDetailsWithId(request.clientsiteId).FirstOrDefault();

                try
                {
                    if (request.IsNewGuard)
                    {
                        var _nwGuard = _guardDataProvider.GetGuardDetailsUsingId(request.guardId).FirstOrDefault();
                        _alertEmailServices.SendNewGuardRegisterAlertMail(_nwGuard, clientsiteDetails.Name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error sending new guard registration email: " + ex.Message);
                }

                return Ok(new
                {
                    message = "Guard successfully logged in.",
                    guardLoginId,
                    TourMode = (int)clientsiteDetails.PatrolTourMode,
                    Activity = activity,
                    PatrolCarLog = patrolCarLogs,
                    CustomFieldLog = customFieldLogs.ToArray(),
                    rcLinkedClientSites = _rcLinkedClientSites,
                    irClientTypes = clientTypes,
                    irClientSites = clientSites,
                    irFeedbackTemplates = feedbackTemplates,
                    irNotifiedByList = notifiedByList,
                    irAreas = areas,
                    audioList = audio,
                    multimediaList = multimedia
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }

        }


        [HttpGet("GetActivities")]
        public IActionResult GetActivities([FromQuery] int type, [FromQuery] int? siteid = 0)
        {
            //Predefined Activity for client site refer EnterGuardLogin function in this same page
            try
            {
                var activity = _viewDataService.GetDressAppFields(type, siteid);

                if (activity == null || !activity.Any())
                {
                    return NotFound(new
                    {
                        message = "No client sites found."
                    });
                }

                return Ok(activity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching activities.",
                    error = ex.Message
                });
            }
        }


        [HttpGet("GetActivitiesAudio")]
        public IActionResult GetActivitiesAudio([FromQuery] int type)
        {
            try
            {
                var activity = GetAudioForMobileApp(type); // _viewDataService.GetDressAppFieldsAudio(type);

                if (activity == null || !activity.Any())
                {
                    return NotFound(new
                    {
                        message = "No client sites found."
                    });
                }

                return Ok(activity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching activities.",
                    error = ex.Message
                });
            }
        }



        //private int GetGuardLoginId(int logBookId, int guardId, int clientsiteId, int userId)
        //{
        //    // Get all guard logins associated with the logBookId
        //    var guardLoginList = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).ToList();

        //    // Check if a guard login exists for the current day
        //    var existingGuardLogin = guardLoginList.FirstOrDefault(x => x.GuardId == guardId && x.OnDuty.Date == DateTime.Now.Date);

        //    if (existingGuardLogin != null)
        //    {
        //        return existingGuardLogin.Id; // Return existing login ID
        //    }

        //    // Create a new GuardLogin entry
        //    var newGuardLogin = new GuardLogin
        //    {
        //        LoginDate = DateTime.Now,
        //        GuardId = guardId,
        //        ClientSiteId = clientsiteId,
        //        ClientSiteLogBookId = logBookId,
        //        PositionId = null,
        //        SmartWandId = null,
        //        OnDuty = DateTime.Now,
        //        OffDuty = DateTime.Now.AddHours(1),
        //        UserId = userId,
        //        IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        //    };




        //    // Save and return new login ID
        //    return _guardDataProvider.SaveGuardLogin(newGuardLogin);
        //}

        // To be deleted after ios update
        [HttpPost("PostActivity")]
        public IActionResult PostActivity([FromBody] PostActivityRequest request, int guardId, int clientsiteId, int userId, string activityString, string gps, bool systemEntry = true,
            int scanningType = 0, string tagUID = "NA")
        {
            try
            {
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var (IsSuccessR, msgR, guardLoginIdR) = _mobileAppDataServices.PostMobileLogActivity(request, IPAddress);

                if (!IsSuccessR)
                {
                    return BadRequest(new { message = msgR });
                }

                return Ok(new { message = msgR, guardLoginId = guardLoginIdR });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("PostActivityNew")]
        public IActionResult PostActivityNew([FromBody] PostActivityRequest request)
        {
            try
            {
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var (IsSuccessR, msgR, guardLoginIdR) = _mobileAppDataServices.PostMobileLogActivity(request, IPAddress);

                if (!IsSuccessR)
                {
                    return BadRequest(new { message = msgR });
                }

                return Ok(new { message = msgR, guardLoginId = guardLoginIdR });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("SyncOfflinePostActivityLogData")]
        public IActionResult SyncOfflinePostActivityLogData([FromBody] List<PostActivityRequestLocalCacheOffline> offlineRecords)
        {
            //try
            //{
            //    var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            //    var (IsSuccessR, msgR, guardLoginIdR) = _mobileAppDataServices.PostMobileLogActivity(request, IPAddress);

            //    if (!IsSuccessR)
            //    {
            //        return BadRequest(new { message = msgR });
            //    }

            //    return Ok(new { message = msgR, guardLoginId = guardLoginIdR });
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            //}

            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            if (offlineRecords != null && offlineRecords.Count > 0)
            {
                foreach (var offlineRecord in offlineRecords)
                {
                    try
                    {
                        PostActivityRequest request = new PostActivityRequest()
                        {
                            guardId = offlineRecord.guardId,
                            clientsiteId = offlineRecord.clientsiteId,
                            userId = offlineRecord.userId,
                            activityString = offlineRecord.activityString,
                            gps = offlineRecord.gps,
                            systemEntry = offlineRecord.systemEntry,
                            scanningType = offlineRecord.scanningType,
                            tagUID = offlineRecord.tagUID,
                            EventDateTimeLocal = offlineRecord.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = offlineRecord.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = offlineRecord.EventDateTimeZone,
                            EventDateTimeZoneShort = offlineRecord.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = offlineRecord.EventDateTimeUtcOffsetMinute,
                            IsOfflineRecord = true,
                            OfflineRecordSyncDateTime = DateTime.Now,
                            TagScanHitLogRefId = offlineRecord.TagScanHitLogRefId,
                            EventMobileUtcDateTime = offlineRecord.EventMobileUtcDateTime,
                            LogbookclientsiteId = offlineRecord.LogbookclientsiteId,
                            IsEntryByPCAR = offlineRecord.IsEntryByPCAR,
                            CallSignId = offlineRecord.CallSignId,
                            PositionId = offlineRecord.PositionId

                        };

                        //Create Logbook entries 
                        var (IsSuccessR, msgR, guardLoginIdR) = _mobileAppDataServices.PostMobileLogActivity(request, IPAddress);
                        if (!IsSuccessR)
                        {
                            // Save the record in DB to process later.
                            SaveSyncOfflinePostActivityLogDataError(offlineRecord, msgR);
                        }

                        offlineRecord.IsSynced = true;

                        Thread.Sleep(500); //wait a while since signalR pushes the refresh signal for logbook refresh

                    }
                    catch (Exception ex)
                    {
                        SaveSyncOfflinePostActivityLogDataError(offlineRecord, ex.ToString());
                        offlineRecord.IsSynced = true;
                    }
                }
            }

            return Ok(offlineRecords);

        }

        //[HttpGet("PostActivity")]
        //public IActionResult PostActivity(int guardId, int clientsiteId, int userId, string activityString, string gps, bool systemEntry = true,
        //    int scanningType = 0, string tagUID = "NA")
        //{
        //    try
        //    {
        //        var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        //        var (IsSuccessR, msgR, guardLoginIdR) = _mobileAppDataServices.PostMobileLogActivity(guardId, clientsiteId, userId, activityString,
        //            gps, IPAddress, DateTime.Today, systemEntry, scanningType, tagUID);

        //        if (!IsSuccessR)
        //        {
        //            return BadRequest(new { message = msgR });
        //        }

        //        return Ok(new { message = msgR, guardLoginId = guardLoginIdR });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        //    }

        //}




        //[HttpPost("UploadFile")]
        //public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        //{
        //    try
        //    {
        //        if (file == null || file.Length == 0)
        //        {
        //            return BadRequest("No file uploaded.");
        //        }

        //        string fileExtension = Path.GetExtension(file.FileName);
        //        string newFileName = $"{Guid.NewGuid()}{fileExtension}";
        //        string filePath = Path.Combine(_uploadFolder, newFileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await file.CopyToAsync(stream);
        //        }

        //        string fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{newFileName}";

        //        return Ok(new { message = "File uploaded successfully!", fileUrl });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}


        [HttpGet("SaveClientSiteDuress")]
        public async Task<IActionResult> SaveClientSiteDuress(int guardId, int clientsiteId, int userId, string gps)
        {
            try
            {

                if (guardId <= 0 || clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });

                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType);

                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                // Get Guard Login ID
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Validate request parameters
                if (clientsiteId <= 0 || guardId <= 0 || guardLoginId <= 0 || logBookId <= 0)
                {
                    return BadRequest(new { message = "Invalid input parameters." });
                }


                var gpsCoordinates = gps;
                var enabledAddress = string.Empty;
                var status = true;
                var message = "Success";


                if (!string.IsNullOrEmpty(gpsCoordinates) && gpsCoordinates.Contains(","))
                {
                    var parts = gpsCoordinates.Split(',');

                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
                    {
                        string address = await GetAddressFromCoordinatesAsync(lat, lng);

                        enabledAddress = address;
                        // Use the address as needed
                        Console.WriteLine(address);
                    }
                    else
                    {
                        Console.WriteLine("Invalid GPS format.");
                    }
                }
                else
                {
                    Console.WriteLine("GPS coordinates are missing or invalid.");
                }





                var tmdata = new GuardLog()
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = string.Empty,
                    IsSystemEntry = true,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),

                };


                var logbookId = _clientDataProvider.GetClientSiteLogBook(clientsiteId, LogBookType.DailyGuardLog, DateTime.Today)?.Id;
                logbookId ??= _clientDataProvider.SaveClientSiteLogBook(new ClientSiteLogBook()
                {
                    ClientSiteId = clientsiteId,
                    Type = LogBookType.DailyGuardLog,
                    Date = DateTime.Today
                });

                var ClientsiteDetails = _clientDataProvider.GetClientSiteName(clientsiteId);
                enabledAddress = string.IsNullOrWhiteSpace(enabledAddress) ? ClientsiteDetails.Address : enabledAddress;
                var Emails = _clientDataProvider.GetGlobalDuressEmail().ToList();
                var GuradDetails = _clientDataProvider.GetGuradName(guardId);
                _viewDataService.EnableClientSiteDuress(clientsiteId, guardLoginId, logbookId.Value, guardId, gpsCoordinates, enabledAddress, tmdata, ClientsiteDetails.Name, GuradDetails.Name);
                /* Save log for duress button enable Start 02032024 dileep*/
                _SiteEventLogDataProvider.SaveSiteEventLogData(
                    new SiteEventLog()
                    {
                        GuardId = guardId,
                        SiteId = clientsiteId,
                        GuardName = GuradDetails.Name,
                        SiteName = ClientsiteDetails.Name,
                        ProjectName = "ClientPortal",
                        ActivityType = "Duress Button Enable",
                        Module = "Guard",
                        SubModule = "Key Vehicle",
                        GoogleMapCoordinates = gpsCoordinates,
                        IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                        EventTime = DateTime.Now,
                        EventLocalTime = DateTime.Now,
                        ToAddress = string.Empty,
                        ToMessage = string.Empty,
                    }
                 );
                /* Save log for duress button enable end*/


                #region GlobalDuressEmailAndSms
                var Subject = "Global Duress Alert";
                var Notifications = "C4i Duress Button Activated By:" +
                         (string.IsNullOrEmpty(GuradDetails.Name) ? string.Empty : GuradDetails.Name) + "[" + GuradDetails.Initial + "]" + "<br/>" +
                         (string.IsNullOrEmpty(GuradDetails.Mobile) ? string.Empty : "Mobile No: " + GuradDetails.Mobile) + "<br/>" +
                        (string.IsNullOrEmpty(ClientsiteDetails.Name) ? string.Empty : "From: " + ClientsiteDetails.Name) + "<br/>" +
                        (string.IsNullOrEmpty(ClientsiteDetails.Address) ? string.Empty : "Address:: " + ClientsiteDetails.Address) + "<br/>" +
                        (string.IsNullOrEmpty(ClientsiteDetails.LandLine) ? string.Empty : "Mobile No: " + ClientsiteDetails.LandLine);
                var SmsNotifications = Notifications.Replace("<br/>", "\n");
                if (gpsCoordinates != null)
                {
                    var googleMapsLink = "https://www.google.com/maps?q=" + HttpUtility.UrlEncode(gpsCoordinates);
                    Notifications += "\n<a href=\"" + googleMapsLink + "\" target=\"_blank\" data-toggle=\"tooltip\" title=\"View on Google Maps\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> Location</a>";
                    SmsNotifications += "\n" + googleMapsLink;
                }

                var emailAddresses = string.Join(",", Emails.Select(email => email.Email));
                //Commneted for testing dileep
                EmailSender(emailAddresses, Subject, Notifications, GuradDetails.Name, ClientsiteDetails.Name, gpsCoordinates);

                var GlobalDuressSmsNumbers = _clientDataProvider.GetDuressSms();
                if (ClientsiteDetails.DuressSms != null)
                {// Adding Site Duress Sms number.
                    GlobalDuressSms SiteDuressSmsNumbers = new GlobalDuressSms() { SmsNumber = ClientsiteDetails.DuressSms };
                    GlobalDuressSmsNumbers.Add(SiteDuressSmsNumbers);
                }
                if (_WebHostEnvironment.IsDevelopment())
                {
                    string smsnumber = "+61 (0) 423 404 982"; // Sending to Jino sir number for testing purpose
                    GlobalDuressSmsNumbers = new List<GlobalDuressSms>();
                    GlobalDuressSms gd = new GlobalDuressSms() { SmsNumber = smsnumber };
                    GlobalDuressSmsNumbers.Add(gd);
                }
                if (GlobalDuressSmsNumbers != null)
                {
                    List<SmsChannelEventLog> _smsChannelEventLogList = new List<SmsChannelEventLog>();
                    foreach (var item in GlobalDuressSmsNumbers)
                    {
                        if (item.SmsNumber != null)
                        {
                            SmsChannelEventLog smslog = new SmsChannelEventLog();
                            smslog.GuardId = guardId != 0 ? guardId : null; // ID of guard who is sending the message
                            smslog.GuardName = GuradDetails.Name.Length > 0 ? GuradDetails.Name : null; // Name of guard who is sending the message
                            smslog.GuardNumber = item.SmsNumber;
                            smslog.SiteId = clientsiteId;
                            smslog.SiteName = ClientsiteDetails.Name;
                            _smsChannelEventLogList.Add(smslog);
                        }
                    }
                    SiteEventLog svl = new SiteEventLog();
                    svl.ProjectName = "ClientPortal";
                    svl.ActivityType = "C4i Duress Enable - Global Duress SMS Alert";
                    svl.Module = "Guard";
                    svl.SubModule = "Key Vehicle";
                    svl.GoogleMapCoordinates = gpsCoordinates;
                    svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    svl.EventLocalTime = tmdata.EventDateTimeLocal.Value;
                    svl.EventLocalOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute;
                    svl.EventLocalTimeZone = tmdata.EventDateTimeZoneShort;
                    svl.IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    //Commneted for testing dileep
                    _smsSenderProvider.SendSms(_smsChannelEventLogList, Subject + " : " + SmsNotifications, svl);
                }
                else
                {
                    _SiteEventLogDataProvider.SaveSiteEventLogData(
                      new SiteEventLog()
                      {
                          GuardName = GuradDetails.Name,
                          SiteName = ClientsiteDetails.Name,
                          ProjectName = "ClientPortal",
                          ActivityType = "C4i Duress Enable - Global Duress SMS Alert",
                          Module = "Guard",
                          SubModule = "Key Vehicle",
                          GoogleMapCoordinates = gpsCoordinates,
                          IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                          EventTime = DateTime.Now,
                          EventLocalTime = DateTime.Now,
                          ToAddress = null,
                          ToMessage = Subject + " : " + SmsNotifications,
                          EventStatus = "Error",
                          EventErrorMsg = "No global duress sms numbers configured.",
                          EventServerOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                          EventServerTimeZone = TimeZoneHelper.GetCurrentTimeZoneShortName()
                      }
                   );
                }
                #endregion

                return Ok(new { message = "Duress status saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }

        }


        public JsonResult EmailSender(string Email, string Subject, string Notifications, string GuradName, string Name, string gpsCoordinates)
        {
            var success = true;
            var message = "success";
            #region Email
            if (Email != null)
            {
                var fromAddress = _emailOptions.FromAddress.Split('|');
                var toAddress = Email.Split(','); ;
                var subject = Subject;
                var messageHtml = Notifications;

                var messagenew = new MimeMessage();
                messagenew.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
                foreach (var address in GetToEmailAddressList(toAddress))
                    messagenew.To.Add(address);

                messagenew.Subject = $"{subject}";

                var builder = new BodyBuilder()
                {
                    HtmlBody = messageHtml
                };

                messagenew.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                        !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                        client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                    client.Send(messagenew);
                    client.Disconnect(true);
                    _SiteEventLogDataProvider.SaveSiteEventLogData(
                    new SiteEventLog()
                    {
                        GuardName = GuradName,
                        SiteName = Name,
                        ProjectName = "ClientPortal",
                        ActivityType = "Duress Button Enable",
                        Module = "Guard",
                        SubModule = "Key Vehicle",
                        GoogleMapCoordinates = gpsCoordinates,
                        IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                        EventTime = DateTime.Now,
                        EventLocalTime = DateTime.Now,
                        ToAddress = Email,
                        ToMessage = "Global Duress Alert",
                    }
                 );
                }
            }
            #endregion

            return new JsonResult(new { success, message });
        }
        public async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {

            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            var apiKey = mapSettings.ApiKey;
            string requestUri = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<GoogleGeocodeResponse>(json);

                    if (result.status == "OK" && result.results.Count > 0)
                    {
                        return result.results[0].formatted_address;
                    }
                }
            }

            return string.Empty;
        }



        public (double? Latitude, double? Longitude) GetCoordinatesFromAddress(string address)
        {
            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            var apiKey = mapSettings.ApiKey;
            string requestUri = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}";

            using (HttpClient client = new HttpClient())
            {
                var response = client.GetAsync(requestUri).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var result = JsonSerializer.Deserialize<GoogleGeocodeResponse2>(json);

                    if (result != null && result.status == "OK" && result.results.Count > 0)
                    {
                        var location = result.results[0].geometry.location;
                        return (location.lat, location.lng);
                    }
                }
            }

            return (null, null);
        }




        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();

            foreach (var item in toAddress)
            {
                if (!string.IsNullOrWhiteSpace(item) && MailboxAddress.TryParse(item.Trim(), out var mailbox))
                {
                    emailAddressList.Add(mailbox);
                }

            }

            return emailAddressList;
        }


        private List<MailboxAddress> GetToEmailAddressListIr(string[] toAddress, IncidentRequest Report)
        {
            var emailAddressList = new List<MailboxAddress>();


            if (toAddress != null && toAddress.Length >= 2 &&
                !string.IsNullOrWhiteSpace(toAddress[0]) &&
                MailboxAddress.TryParse(toAddress[0].Trim(), out var mainMailbox))
            {
                emailAddressList.Add(new MailboxAddress(toAddress[1]?.Trim() ?? string.Empty, toAddress[0].Trim()));
            }

            var fields = _configDataProvider?.GetReportFields()?.ToList() ?? new List<IncidentReportField>();


            void AddIfValid(string emailAddresses)
            {
                if (!string.IsNullOrWhiteSpace(emailAddresses))
                {
                    foreach (var email in emailAddresses.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmedEmail = email.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmedEmail))
                        {
                            emailAddressList.Add(new MailboxAddress(string.Empty, trimmedEmail));
                        }
                    }
                }
            }

            var positionEmailTo = GetFieldEmailAddress(fields, ReportFieldType.Position, Report?.Officer?.Position);
            AddIfValid(positionEmailTo);
            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.NotifiedBy, Report?.Officer?.NotifiedBy));
            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.CallSign, Report?.Officer?.CallSign));
            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.ClientArea, Report?.DateLocation?.ClientArea));

            return emailAddressList;
        }

        private static string GetFieldEmailAddress(List<IncidentReportField> fields, ReportFieldType type, string fieldValue)
        {
            if (string.IsNullOrWhiteSpace(fieldValue)) return null;
            return fields.FirstOrDefault(x => x.TypeId == type && string.Equals(x.Name?.Trim(), fieldValue.Trim(), StringComparison.OrdinalIgnoreCase))?.EmailTo;
        }

        [HttpGet("GetDuressStatus")]
        public async Task<IActionResult> GetDuressStatus(int clientsiteId)
        {
            try
            {
                if (clientsiteId <= 0)
                {
                    return BadRequest(new { message = "Invalid input parameters." });
                }

                // Fetch the actual duress status
                bool isDuressEnabled = _viewDataService.IsClientSiteDuressEnabled(clientsiteId);

                // Return the duress status
                return Ok(new { status = isDuressEnabled ? "Active" : "Normal" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }



        [HttpGet("GetSiteName")]
        public IActionResult GetSiteName(int clientsiteId)
        {
            try
            {
                var site = _clientDataProvider.GetClientSiteName(clientsiteId); // Fetch site name

                if (site == null || string.IsNullOrEmpty(site.Name))
                {
                    return NotFound(new
                    {
                        message = "No site found for the given ID."
                    });
                }

                return Ok(new { siteName = site.Name }); // Return site name in JSON format
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching the site name.",
                    error = ex.Message
                });
            }
        }



        [HttpGet("UpdateGuardLogNotes")]
        public IActionResult UpdateGuardLogNotes(int id, string notes)
        {
            try
            {
                if (id <= 0)
                    return BadRequest("Invalid log ID.");

                if (string.IsNullOrWhiteSpace(notes))
                    return BadRequest("Notes cannot be empty.");

                // Create a minimal GuardLog object
                var guardLog = new GuardLog
                {
                    Id = id,
                    Notes = notes.Trim()
                };

                // Call your existing SaveGuardLog method
                _guardLogDataProvider.SaveGuardLog(guardLog);

                return Ok("Log updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating guard log: {ex.Message}");
            }
        }



        //old working fine start //
        //[HttpGet("GetSiteLog")]
        //public IActionResult GetSiteLog(int clientsiteId)
        //{
        //    try
        //    {
        //        // Fetch site name (optional usage)
        //        var site = _clientDataProvider.GetClientSiteName(clientsiteId);

        //        // Get today's logbook
        //        var logbook = _clientDataProvider.GetClientSiteLogBook(clientsiteId, LogBookType.DailyGuardLog, DateTime.Today);
        //        if (logbook == null)
        //        {
        //            return NotFound(new { message = "No logbook found for today." });
        //        }

        //        // Get guard logs
        //        var guardLogs = _guardLogDataProvider.GetGuardLogswithKvLogData(logbook.Id, DateTime.Today)
        //            .OrderByDescending(z => z.Id)
        //            .ThenByDescending(z => z.EventDateTime)
        //            .ToList();

        //        var result = new List<GuardLogDto>();

        //        foreach (var guardlog in guardLogs)
        //        {
        //            var imageUrls = new List<string>();
        //            var notes = guardlog.Notes ?? "";

        //            // Process images
        //            var images = _guardLogDataProvider.GetGuardLogDocumentImaes(guardlog.Id);
        //            foreach (var img in images)
        //            {
        //                if (img.IsTwentyfivePercentfile == true && !string.IsNullOrEmpty(img.ImagePath))
        //                    imageUrls.Add(img.ImagePath);

        //                if (img.IsRearfile == true && !string.IsNullOrEmpty(img.ImagePath))
        //                {
        //                    var filename = Path.GetFileName(img.ImagePath);
        //                    notes += $"</br>See attached file <a href=\"{img.ImagePath}\" target=\"_blank\">{filename}</a>";
        //                }
        //            }

        //            string formattedDisplayTime = string.Empty;

        //            if (guardlog.EventDateTimeLocalWithOffset.HasValue)
        //            {
        //                var dateandOffset = guardlog.EventDateTimeLocalWithOffset.Value;

        //                var offsetSign = dateandOffset.Offset.TotalMinutes >= 0 ? "+" : "-";
        //                var formattedOffset = offsetSign + dateandOffset.Offset.ToString(@"hh\:mm");

        //                formattedDisplayTime = dateandOffset.ToString("HH:mm") + " Hrs GMT" + formattedOffset;
        //            }
        //            else
        //            {
        //                // fallback if value is null
        //                formattedDisplayTime = "N/A";
        //            }



        //            var dto = new GuardLogDto
        //            {
        //                Id = guardlog.Id,
        //                EventDateTime = guardlog.EventDateTime,
        //                EventDateTimeLocal = formattedDisplayTime,
        //                Notes = notes,
        //                ImageUrls = imageUrls,
        //                GuardInitials = guardlog.GuardLogin?.Guard?.Initial ?? "N/A",
        //                IrEntryType = guardlog.IrEntryType.HasValue ? (int)guardlog.IrEntryType.Value : 0,
        //                IsSystemEntry = guardlog.IsSystemEntry,
        //                rcPushMessageId= guardlog.RcPushMessageId
        //            };



        //            result.Add(dto);
        //        }

        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "An error occurred while fetching the site log.",
        //            error = ex.Message
        //        });
        //    }
        //}

        //end //


        [HttpGet("GetSiteLog")]
        public async Task<IActionResult> GetSiteLog(int clientsiteId, int lastLogId = 0)
        {
            try
            {
                var result = await _guardLogDataProvider.GetSiteLogAsync(clientsiteId, lastLogId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error loading logs",
                    error = ex.Message
                });
            }
        }


        [HttpGet("GetStaffDocuments")]
        public IActionResult GetStaffDocuments(int type, int UserId, string query = "")
        {
            var domain = IsThirdParty(UserId);
            var thirdpartyId = 0;

            if (domain != null)
            {
                thirdpartyId = domain.Id;
            }

            IEnumerable<StaffDocument> result;

            if (thirdpartyId != 0)
            {
                result = _configDataProvider
                    .GetStaffDocumentsUsingType(type, query)
                    .Where(x => x.SubDomainId == thirdpartyId);
            }
            else
            {
                result = _configDataProvider.GetStaffDocumentsUsingType(type, query);
            }

            return Ok(result);

        }

        [HttpGet("GetStaffTools")]
        public IActionResult GetStaffTools(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return BadRequest("Type parameter is required.");

            var trimmedType = type.Trim();
            var id = _clientDataProvider.GetSiteLinksTypeUsingTypeText(trimmedType);

            if (id <= 0)
                return NotFound($"No link type found for '{trimmedType}'.");

            var result = _clientDataProvider.GetSiteLinkDetailsUsingTypeAndState(id);

            return Ok(result);
        }


        [HttpGet("GetStaffDocumentSOP")]
        public IActionResult GetStaffDocumentSOP(int clientSiteId)
        {
            var result = _configDataProvider.GetStaffDocumentSOPDocDetails(clientSiteId);

            // Ensure it's always a list
            return Ok(result ?? new List<StaffDocument>());
        }





        [HttpGet("UpdateOffDuty")]
        public IActionResult UpdateOffDuty(int guardId, int clientsiteId, int userId)
        {
            var status = true;
            var message = "Success";
            var now = DateTime.Now;

            try
            {
                if (guardId <= 0 || clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });

                var logBookType = LogBookType.DailyGuardLog;
                var clientSiteLogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType);

                if (clientSiteLogBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(clientSiteLogBookId, guardId, clientsiteId, userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                AuthUserHelper.IsAdminPowerUser = false;
                AuthUserHelper.IsAdminGlobal = false;

                var signOffEntry = new GuardLog
                {
                    ClientSiteLogBookId = clientSiteLogBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = now,
                    Notes = "Guard Off Duty (Logbook Signout)",
                    IsSystemEntry = true,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute()
                };

                _guardLogDataProvider.SaveGuardLog(signOffEntry);
                _guardDataProvider.UpdateGuardOffDuty(guardLoginId, now);

                var guardlogins = _guardLogDataProvider.GetGuardLogins(guardLoginId);
                foreach (var item in guardlogins)
                {
                    var activityDetails = _guardLogDataProvider.GetClientSiteRadioChecksActivityDetails()
                        .Where(x => x.GuardId == item.GuardId && x.ClientSiteId == item.ClientSiteId && x.GuardLoginTime != null);

                    foreach (var activity in activityDetails)
                    {
                        activity.GuardLogoutTime = now;
                        _guardLogDataProvider.UpdateRadioChecklistLogOffEntry(activity);
                    }
                }

                var firstLogin = guardlogins?.FirstOrDefault();
                if (firstLogin != null)
                {
                    _guardLogDataProvider.SaveClientSiteRadioCheckStatusFromlogBookNewUpdate(new ClientSiteRadioCheck
                    {
                        ClientSiteId = firstLogin.ClientSiteId,
                        GuardId = firstLogin.GuardId,
                        Status = "Off Duty",
                        RadioCheckStatusId = 1,
                        CheckedAt = now,
                        Active = true
                    });
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error: " + ex.Message;
            }

            return Ok(new { status, message });
        }




        [HttpGet("GetFeedbackTemplates")]
        public IActionResult GetFeedbackTemplates()
        {
            var result = GetAndReturnFeedbackTemplates();
            return Ok(result);
        }





        [HttpGet("TestLogs")]
        public IActionResult TestLogs()
        {
            var sites = _context.ClientSites.Where(x => x.Name.Contains("Romeo") || x.Id == 10).Select(x => new { x.Id, x.Name, x.PatrolTourMode }).ToList();
            return Ok(sites);
        }

        [HttpPost("ProcessIrSubmit")]
        public IActionResult ProcessIrSubmit([FromQuery] string gps, [FromQuery] int UserId, [FromQuery] int IRguardId,
            [FromQuery] int IRclientSiteId, [FromBody] IncidentRequest Report, [FromQuery] string RequestDeviceType = "")
        {
            var (processResult, domain, fileName) = CreateAndSaveIr(gps, UserId, IRguardId, IRclientSiteId, Report, RequestDeviceType);

            return Ok(new
            {
                Success = processResult.Count == 0,
                FileName = fileName,
                Domin = domain?.Domain ?? string.Empty,
                Errors = processResult.Select(p => new { Code = p.Key, Message = p.Value.ErrorMessage })
            });
        }



        private void CreatePositionGuardLogEntry(IncidentReport report, int Guardid, int UserId, string gps)
        {
            // p6#73 timezone bug - Added by binoy 24-01-2024
            var logBookId = GetLogBookId(report.ClientSitePositionId.Value, (int)report.CreatedOnDateTimeUtcOffsetMinute);
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, Guardid, report.ClientSitePositionId.Value, UserId, IPAddress);
            //var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)report.CreatedOnDateTimeUtcOffsetMinute);
            var guardLog = new GuardLog()
            {
                ClientSiteLogBookId = logBookId,
                EventDateTime = DateTime.Now,
                Notes = Path.GetFileNameWithoutExtension(report.FileName),
                IsSystemEntry = true,
                IrEntryType = report.IsEventFireOrAlarm ? IrEntryType.Alarm : IrEntryType.Normal,
                EventDateTimeLocal = report.CreatedOnDateTimeLocal,
                EventDateTimeLocalWithOffset = report.CreatedOnDateTimeLocalWithOffset,
                EventDateTimeZone = report.CreatedOnDateTimeZone,
                EventDateTimeZoneShort = report.CreatedOnDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = report.CreatedOnDateTimeUtcOffsetMinute,
                IsIRReportTypeEntry = true,
                GuardLoginId = guardLoginId,
                GpsCoordinates = gps
            };
            _guardLogDataProvider.SaveGuardLog(guardLog);
        }
        private void CreateControlRoomLogEntry(IncidentReport report, int Guardid, int UserId, string gps)
        {
            var RadioCheckDetails = _guardLogDataProvider.GetRadiocheckLogbookDetails();
            // p6#73 timezone bug - Added by binoy 24-01-2024
            var logBookId = GetLogBookId(RadioCheckDetails.ClientSiteId, (int)report.CreatedOnDateTimeUtcOffsetMinute);
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, Guardid, RadioCheckDetails.ClientSiteId, UserId, IPAddress);
            //var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)report.CreatedOnDateTimeUtcOffsetMinute);

            var StampRcLogbook = _guardLogDataProvider.IsRClogbookStampRequired(report.NotifiedBy);

            if (report.ColourCode != null || report.IsPatrol == true || StampRcLogbook)
            {

                if (report.ColourCode == null)
                {
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        EventDateTime = DateTime.Now,
                        Notes = Path.GetFileNameWithoutExtension(report.FileName),
                        IsSystemEntry = true,
                        IrEntryType = report.IsEventFireOrAlarm ? IrEntryType.Alarm : IrEntryType.Normal,
                        EventDateTimeLocal = report.CreatedOnDateTimeLocal,
                        EventDateTimeLocalWithOffset = report.CreatedOnDateTimeLocalWithOffset,
                        EventDateTimeZone = report.CreatedOnDateTimeZone,
                        EventDateTimeZoneShort = report.CreatedOnDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = report.CreatedOnDateTimeUtcOffsetMinute,
                        IsIRReportTypeEntry = true,
                        RcLogbookStamp = StampRcLogbook,
                        GuardLoginId = guardLoginId,
                        GpsCoordinates = gps
                    };
                    _guardLogDataProvider.SaveGuardLog(guardLog);
                }
                else
                {
                    var feedbackTypes = _configDataProvider.GetFeedbackTypes().Where(x => x.Name == "Colour Codes").Select(x => x.Id).FirstOrDefault();
                    var feedbackTemplatesEnabledColourCodes = _configDataProvider.GetFeedbackTemplates().Where(x => x.Type == feedbackTypes && x.SendtoRC == true);

                    var CheckSttaus = feedbackTemplatesEnabledColourCodes.Where(x => x.Id == report.ColourCode).FirstOrDefault();
                    if (CheckSttaus != null)
                    {
                        if (CheckSttaus.Id != 0)
                        {

                            var guardLog = new GuardLog()
                            {
                                ClientSiteLogBookId = logBookId,
                                EventDateTime = DateTime.Now,
                                Notes = Path.GetFileNameWithoutExtension(report.FileName),
                                IsSystemEntry = true,
                                IrEntryType = report.IsEventFireOrAlarm ? IrEntryType.Alarm : IrEntryType.Normal,
                                EventDateTimeLocal = report.CreatedOnDateTimeLocal,
                                EventDateTimeLocalWithOffset = report.CreatedOnDateTimeLocalWithOffset,
                                EventDateTimeZone = report.CreatedOnDateTimeZone,
                                EventDateTimeZoneShort = report.CreatedOnDateTimeZoneShort,
                                EventDateTimeUtcOffsetMinute = report.CreatedOnDateTimeUtcOffsetMinute,
                                IsIRReportTypeEntry = true,
                                RcLogbookStamp = StampRcLogbook,
                                GuardLoginId = guardLoginId,
                                GpsCoordinates = gps
                            };
                            _guardLogDataProvider.SaveGuardLog(guardLog);

                        }

                    }
                }


            }

        }

        private void CreateGuardLogEntry(IncidentReport report, int Guardid, int UserId, string gps)
        {
            // p6#73 timezone bug - Added by binoy 24-01-2024
            var logBookId = GetLogBookId(report.ClientSiteId.Value, (int)report.CreatedOnDateTimeUtcOffsetMinute);
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, Guardid, report.ClientSiteId.Value, UserId, IPAddress);
            //var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)report.CreatedOnDateTimeUtcOffsetMinute);
            var guardLog = new GuardLog()
            {

                ClientSiteLogBookId = logBookId,
                EventDateTime = DateTime.Now,
                Notes = Path.GetFileNameWithoutExtension(report.FileName),
                IsSystemEntry = true,
                IrEntryType = report.IsEventFireOrAlarm ? IrEntryType.Alarm : IrEntryType.Normal,
                EventDateTimeLocal = report.CreatedOnDateTimeLocal,
                EventDateTimeLocalWithOffset = report.CreatedOnDateTimeLocalWithOffset,
                EventDateTimeZone = report.CreatedOnDateTimeZone,
                EventDateTimeZoneShort = report.CreatedOnDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = report.CreatedOnDateTimeUtcOffsetMinute,
                IsIRReportTypeEntry = true,
                GuardLoginId = guardLoginId,
                GpsCoordinates = gps
            };
            _guardLogDataProvider.SaveGuardLog(guardLog);
        }

        private void CreatePatrolCarGuardLogEntry(IncidentReport report, int patrolClientSiteId, int Guardid, int UserId, string gps)
        {
            var logBookId = GetLogBookId(patrolClientSiteId, (int)report.CreatedOnDateTimeUtcOffsetMinute);
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, Guardid, patrolClientSiteId, UserId, IPAddress);
            var guardLog = new GuardLog()
            {
                ClientSiteLogBookId = logBookId,
                EventDateTime = DateTime.Now,
                Notes = Path.GetFileNameWithoutExtension(report.FileName),
                IsSystemEntry = true,
                IrEntryType = report.IsEventFireOrAlarm ? IrEntryType.Alarm : IrEntryType.Normal,
                EventDateTimeLocal = report.CreatedOnDateTimeLocal,
                EventDateTimeLocalWithOffset = report.CreatedOnDateTimeLocalWithOffset,
                EventDateTimeZone = report.CreatedOnDateTimeZone,
                EventDateTimeZoneShort = report.CreatedOnDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = report.CreatedOnDateTimeUtcOffsetMinute,
                IsIRReportTypeEntry = true,
                GuardLoginId = guardLoginId,
                GpsCoordinates = gps
            };
            _guardLogDataProvider.SaveGuardLog(guardLog);
        }


        public SubDomain IsThirdParty(int userId)
        {
            var access = _userAuthentication.GetUserClientSiteAccessThirdParty(userId);

            if (access?.ThirdPartyID != null && access.ThirdPartyID != 0)
            {
                var subDomain = _configDataProvider.GetSubDomainID(access.ThirdPartyID);
                return subDomain;
            }

            return null;
        }


        private bool SendEmailWithAzureBlob(string fileName, IncidentRequest Report, SubDomain domain)
        {
            var fromAddress = _emailOptions.FromAddress.Split('|');
            var ToAddreddAppset = _emailOptions.ToAddress.Split('|');

            //var toAddressData = _clientDataProvider.GetDefaultEmailAddress() + '|' + ToAddreddAppset[1];

            var toAddressData = string.Empty;
            var thirpartyemail = getClientEmailId(domain);
            var messageHtml = string.Empty; ;
            if (thirpartyemail != string.Empty)
            {
                toAddressData = thirpartyemail + '|' + ToAddreddAppset[1];

                if (domain != null)
                {

                    string messageHtmlnew = _emailOptions.Message;
                    string wordToReplace = "Citywatch Security";
                    string replacementWord = CapitalizeFirstLetter(domain.Domain);

                    // Regex to split into sentences based on punctuation (., !, ?)
                    string[] sentences = Regex.Split(messageHtmlnew, @"(?<=[.!?])\s+");

                    for (int i = 0; i < sentences.Length; i++)
                    {
                        // Replace whole word only (case-insensitive)
                        sentences[i] = Regex.Replace(
                            sentences[i],
                            $@"\b{Regex.Escape(wordToReplace)}\b",
                            replacementWord,
                            RegexOptions.IgnoreCase);
                    }

                    messageHtmlnew = string.Join(" ", sentences);
                    wordToReplace = "control@citywatchsecurity.com.au";
                    replacementWord = thirpartyemail;
                    sentences = Regex.Split(messageHtmlnew, @"(?<=[.!?])\s+");

                    for (int i = 0; i < sentences.Length; i++)
                    {
                        // Replace whole word only (case-insensitive)
                        sentences[i] = Regex.Replace(
                            sentences[i],
                            $@"\b{Regex.Escape(wordToReplace)}\b",
                            replacementWord,
                            RegexOptions.IgnoreCase);
                    }
                    messageHtmlnew = string.Join(" ", sentences);
                    messageHtml = messageHtmlnew;
                }
            }
            else
            {
                toAddressData = _clientDataProvider.GetDefaultEmailAddress() + '|' + ToAddreddAppset[1];
                messageHtml = _emailOptions.Message;
            }

            // Remove unwanted legacy sentences
            messageHtml = messageHtml.Replace("<br><br>Sites with access to the cloud file server will also have a copy stored in the relevant folder.", "");
            messageHtml = messageHtml.Replace("<br><br>Any concerns, please contact your relevant Citywatch Security Account Manager, or email <a href='mailto:control@citywatchsecurity.com.au'>control@citywatchsecurity.com.au</a>", "");

            string incidentTime = (Report.DateLocation.IncidentDate ?? Report.DateLocation.ReportDate).ToString("HH:mm");
            string summaryNotes = Report.Feedback;
            if (!string.IsNullOrEmpty(summaryNotes))
            {
                var lines = summaryNotes.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                summaryNotes = string.Join("<br/>", lines);
            }

            string summaryHtml = $@"
<br/><br/>
<p>*** SUMMARY ***</p>
<br/>
<table style='border-collapse: collapse; width: 100%;'>
    <tr><td style='width: 150px; vertical-align: top;'>Client Site:</td><td style='vertical-align: top;'>{Report.DateLocation.ClientSite}</td></tr>
    <tr><td style='vertical-align: top;'>Time of Incident:</td><td style='vertical-align: top;'>{incidentTime} hrs</td></tr>
    <tr><td style='vertical-align: top;'>Notes:</td><td style='vertical-align: top;'>{summaryNotes}</td></tr>
</table>
<br/>
<p>**** END OF NOTES ***</p>
<br/>";

            messageHtml += summaryHtml;


            var toAddress = toAddressData.Split('|');

            //var toAddress = _EmailOptions.ToAddress.Split('|');
            // var ccAddress = _EmailOptions.CcAddress.Split('|');
            List<IncidentReportField> incidentReportFields = _configDataProvider.GetReportFieldsByType(ReportFieldType.Reimburse);
            string emailAddress = null;
            foreach (var incidentReportField in incidentReportFields)
            {
                emailAddress = incidentReportField.Name;

            }
            string[] ccAddress = new string[] { };
            if (!string.IsNullOrEmpty(emailAddress))
            {
                ccAddress = emailAddress.Split(',');
            }
            var subject = _emailOptions.Subject;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));

            foreach (var address in GetToEmailAddressListIr(toAddress, Report))
                message.To.Add(address);
            if (Report.DateLocation.ReimbursementYes)
            {
                foreach (var address in ccAddress)
                    message.Cc.Add(new MimeKit.MailboxAddress(String.Empty, address));
            }

            // Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 13,09,2023)
            //message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            // message.Bcc.Add(new MailboxAddress("globoconsoftware", "jishakallani@gmail.com"));
            // Mail Id added Bcc globoconsoftware end 
            var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite && x.ClientType.Name == Report.DateLocation.ClientType);

            if (clientSite != null && !string.IsNullOrEmpty(clientSite.Emails))
            {
                foreach (var email in clientSite.Emails.Split(","))
                {
                    if (CommonHelper.IsValidEmail(email))
                        message.Cc.Add(new MailboxAddress(string.Empty, email.Trim()));
                }
            }

            // Add CC from Position if not already added
            var clientSitePosition = _clientDataProvider?.GetClientSitePosition(Report?.Officer?.Position);
            if (clientSitePosition != null && !string.IsNullOrWhiteSpace(clientSitePosition.EmailTo))
            {
                foreach (var email in clientSitePosition.EmailTo.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmedEmail = email.Trim();
                    if (CommonHelper.IsValidEmail(trimmedEmail))
                    {
                        bool existsInTo = message.To.Mailboxes.Any(m => string.Equals(m.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase));
                        bool existsInCc = message.Cc.Mailboxes.Any(m => string.Equals(m.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase));
                        if (!existsInTo && !existsInCc)
                        {
                            message.Cc.Add(new MailboxAddress(string.Empty, trimmedEmail));
                        }
                    }
                }
            }
            if (Report.SiteColourCodeId != 0 && Report.SiteColourCodeId != null)
            {
                string colorcodes = _viewDataService.GetFeedbackTemplatesByTypeByColor(3, Convert.ToInt32(Report.SiteColourCodeId));
                //for DESCRIBING color codes-start
                //if(colorcodes.Contains("Code ORANGE"))
                // { 
                //         Report.SiteColourCode = "Code ORANGE Event";
                // }
                //else if(colorcodes.Contains("Code BLUE"))
                // {
                //     Report.SiteColourCode = "Code BLUE Event";
                // }
                // else if (colorcodes.Contains("Code PINK"))
                // {
                //     Report.SiteColourCode = "Code PINK Event";
                // }
                // else if (colorcodes.Contains("Code PURPLE"))
                // {
                //     Report.SiteColourCode = "Code PURPLE Event";
                // }
                // else if (colorcodes.Contains("Code BLACK"))
                // {
                //     Report.SiteColourCode = "Code BLACK Event";
                // }
                // else if (colorcodes.Contains("Code YELLOW"))
                // {
                //     Report.SiteColourCode = "Code YELLOW Event";
                // }
                // else if (colorcodes.Contains("Code BROWN"))
                // {
                //     Report.SiteColourCode = "Code BROWN Event";
                // }
                // else if (colorcodes.Contains("Code GREY"))
                // {
                //     Report.SiteColourCode = "Code GREY Event";
                // }
                //else if(colorcodes.Contains("SEARCH - CODE GREY BOC"))
                // {
                //     Report.SiteColourCode = "Code GREY Event";
                // }

                // else if (colorcodes.Contains("Code RED"))
                // {
                //     Report.SiteColourCode = "Code RED Event";
                // }

                // else
                //{
                Report.SiteColourCode = colorcodes;
                //}
                //for DESCRIBING color codes - end

                // Report.SiteColourCode = colorcodes;
                //message.Subject = $"{subject} - {Report.DateLocation.ClientType} - {Report.DateLocation.ClientSite}" + " " +  colorcodes;
                message.Subject = "Incident Report -" + " *** " + Report.SiteColourCode.ToUpper() + " *** - " + Report.DateLocation.ClientType + " - " + Report.DateLocation.ClientSite;
            }
            else
            {
                message.Subject = $"{subject} - {Report.DateLocation.ClientType} - {Report.DateLocation.ClientSite}";
            }
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
                    //messageHtml = messageHtml + "<p>Where PDF attachment is greater than 12 MB, it may not appear due to your organisation email limits. In this situation simply " +
                    //"<a href=\" https://c4istorage1.blob.core.windows.net/irfiles/" + (new string(blobName.Take(8).ToArray()) + "/" + blobName) + "\" target=\"_blank\">" +
                    //"click here</a> to download the Incident Report, which are unlimited in size.</p>";
                    //messageHtml = messageHtml + "<p>File name : " + blobName + "</p>";


                    string folder = new string(blobName.Take(8).ToArray());

                    // Encode file name for safe URL
                    string encodedBlobName = Uri.EscapeDataString(blobName);

                    messageHtml += "<p>Where PDF attachment is greater than 12 MB, it may not appear due to your organisation email limits. In this situation simply " +
                        "<a href=\"https://c4istorage1.blob.core.windows.net/irfiles/"
                        + folder + "/" + encodedBlobName + "\" target=\"_blank\">" +
                        "click here</a> to download the Incident Report, which are unlimited in size.</p>";

                    messageHtml += "<p>File name : " + blobName + "</p>";
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
                client.Connect(_emailOptions.SmtpServer, _emailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_emailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_emailOptions.SmtpPassword))
                    client.Authenticate(_emailOptions.SmtpUserName, _emailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

            return true;
        }


        private int GetLogBookId(int clientSiteId, int EventDateTimeUtcOffsetMinute)
        {
            int logBookId;

            var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute(EventDateTimeUtcOffsetMinute);
            var logBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, LogBookType.DailyGuardLog, localDateTime.Date);
            if (logBook == null)
            {
                logBookId = _clientDataProvider.SaveClientSiteLogBook(new ClientSiteLogBook()
                {
                    ClientSiteId = clientSiteId,
                    Type = LogBookType.DailyGuardLog,
                    Date = localDateTime.Date,
                });
            }
            else
            {
                logBookId = logBook.Id;
            }

            return logBookId;
        }

        public static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1);
        }
        public string CheckIfTheUrlIsAThirdPartyUrl(SubDomain domain)
        {
            // Fallback default (when SubDomainId == 0 or domain is null)
            var defaultValue = _userDataProvider.GetThirdPartyDomainOrTemplateDetails()
                                                .FirstOrDefault(x => x.SubDomainId == 0)
                                                ?.FileName ?? string.Empty;

            // If a valid domain is provided, try to get its specific template
            if (domain != null)
            {
                var domainTemplate = _userDataProvider.GetThirdPartyDomainOrTemplateDetails()
                                                      .FirstOrDefault(x => x.SubDomainId == domain.Id);

                if (domainTemplate != null && !string.IsNullOrEmpty(domainTemplate.FileName))
                {
                    return domainTemplate.FileName;
                }
            }

            // Return default if no domain-specific template was found
            return defaultValue;
        }


        private bool AzureBlobUploadIrUploadWithOutMail(string fileName)
        {

            var status = true;
            try
            {
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
                    }

                }
                /* azure blob Implementation 25-9-2023* End*/



            }
            catch (Exception ex)
            {
                status = false;

            }

            return status;
        }

        public string getClientEmailId(SubDomain domain)
        {
            string defaultValue = string.Empty;
            if (domain != null)
            {
                var subDomainIrTemplate = _userDataProvider.GetThirdPartyDomainOrTemplateDetails()
                                                           .FirstOrDefault(x => x.SubDomainId == domain.Id);

                if (subDomainIrTemplate != null)
                {
                    defaultValue = subDomainIrTemplate.DefaultEmail;
                }
            }

            return defaultValue;
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

        private string GenerateHashCode(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
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
        private string GenerateFormattedString()
        {
            string[] segments = new string[5];
            Random random = new Random();

            for (int i = 0; i < segments.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        segments[i] = GenerateRandomAlphanumeric(5, random);
                        break;
                    case 1:
                        segments[i] = GenerateRandomAlphanumeric(8, random);
                        break;
                    case 2:
                        segments[i] = GenerateRandomAlphanumeric(7, random);
                        break;
                    case 3:
                        segments[i] = "fjfjfjjfl9999";
                        break;
                    case 4:
                        segments[i] = "3456";
                        break;
                }
            }

            return string.Join("-", segments);
        }

        private string GenerateRandomAlphanumeric(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpGet("GetClientSiteByName")]
        public IActionResult GetClientSiteByName(string name)
        {
            //var site = _clientDataProvider
            //    .GetClientSites(null)
            //    .FirstOrDefault(x => x.Name == name);

            //if (site == null)
            //    return NotFound();

            //var dto = new ClientSiteDto
            //{
            //    Id = site.Id,
            //    Name = site.Name,
            //    Address = site.Address,
            //    State = site.State,
            //    Gps = site.Gps,
            //    Billing = site.Billing,
            //    Status = site.Status,
            //    StatusDate = site.StatusDate,
            //    SiteEmail = site.SiteEmail,
            //    LandLine = site.LandLine,
            //    DuressEmail = site.DuressEmail,
            //    DuressSms = site.DuressSms,
            //    UploadGuardLog = site.UploadGuardLog,
            //    UploadFusionLog = site.UploadFusionLog,
            //    GuardLogEmailTo = site.GuardLogEmailTo,
            //    DataCollectionEnabled = site.DataCollectionEnabled,
            //    IsActive = site.IsActive,
            //    IsDosDontList = site.IsDosDontList,
            //    MobAppShowClientTypeandSite = site.MobAppShowClientTypeandSite
            //};

            var dto = GetClientSitesForIR(name).FirstOrDefault();

            if (dto == null)
                return NotFound();

            return Ok(dto);
        }





        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile([FromQuery] string reportReference, [FromForm] IFormFile file)
        {
            try
            {
                var (rtn, msg, _filename) = await UploadIrFilesAndReturnName(reportReference, file);

                if (rtn)
                {
                    return Ok(new { success = true, fileName = _filename });
                }
                else
                {
                    return BadRequest(new { success = false, message = msg });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        public async Task<string> ConvertHeicToJpgAsync(string heicFilePath, string outputDirectory)
        {
            try
            {
                var secretkey = string.Empty;
                var companydetail = _userDataProvider.GetCompanyDetails().SingleOrDefault(x => x.Id == 1);
                if (companydetail != null)
                {
                    secretkey = companydetail.ApiSecretkey;
                }
                // Initialize ConvertAPI with your API secret key
                var convertApi = new ConvertApi(secretkey);

                // Check if the HEIC file exists
                if (!System.IO.File.Exists(heicFilePath))
                {
                    throw new FileNotFoundException("The specified HEIC file does not exist.");
                }

                // Convert HEIC to JPG
                var conversionResult = await convertApi.ConvertAsync("heic", "jpg", new ConvertApiFileParam(heicFilePath));

                // Ensure the output directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Path to save the converted JPG file
                //string jpgFilePath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(heicFilePath)}.jpg");

                // Save the converted JPG file
                await conversionResult.SaveFilesAsync(outputDirectory);

                Console.WriteLine($"Conversion successful! JPG saved at: {outputDirectory}");
                return outputDirectory;  // Return the path to the converted file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
                throw;  // Rethrow the exception for further handling if needed
            }
        }

        [HttpGet("areas")]
        public IActionResult Areas([FromQuery] int clientSiteId)
        {

            var items = GetClientSiteArea(clientSiteId);
            //var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            //var clientArea = _configDataProvider.GetReportFieldsByType(ReportFieldType.ClientArea);
            //foreach (var item in clientArea)
            //{
            //    if (!String.IsNullOrEmpty(item.ClientSiteIds))
            //    {
            //        foreach (var clientsiteid in item.ClientSiteIdsNew)
            //        {
            //            if (clientsiteid.Equals(clientSiteId))
            //            {
            //                items.Add(new SelectListItem(item.Name, item.Name));
            //            }
            //        }
            //    }
            //    else
            //    {
            //        items.Add(new SelectListItem(item.Name, item.Name));
            //    }
            //}


            return Ok(items);
        }

        [HttpGet("GetNotifiedByList")]
        public IActionResult GetNotifiedByList()
        {
            var notifiedBy = GetNotifiedReportFieldsByType();
            return Ok(notifiedBy);
        }


        [HttpPost("UploadMultiple")]
        public async Task<IActionResult> UploadMultiple([FromForm] List<IFormFile> files, [FromForm] List<string> types, [FromForm] int guardId,
            [FromForm] int clientsiteId, [FromForm] int userId, [FromForm] string gps, [FromForm] DateTime? eventDateTimeLocal,
            [FromForm] DateTimeOffset? eventDateTimeLocalWithOffset, [FromForm] string? eventDateTimeZone, [FromForm] string? eventDateTimeZoneShort,
            [FromForm] int? eventDateTimeUtcOffsetMinute, [FromForm] int? logbookclientsiteId, [FromForm] bool? isEntryByPCAR, [FromForm] int? callSignId,
            [FromForm] int? positionId
        )
        {
            bool success = false;
            string message = "Uploaded successfully";
            var uploadedFiles = new List<string>();

            int newPcarGuardLogId = 0;
            int newNonPcarGuardLogId = 0;
            bool addNonPcarGuardLogEntry = false;

            try
            {
                if (files == null || files.Count == 0)
                    throw new Exception("No files uploaded");

                if (guardId <= 0 || clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });

                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType);

                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                // Get Guard Login ID
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Default GPS coordinates (should be replaced with actual values if available)
                var gpsCoordinates = gps;



                if (types == null || types.Count != files.Count)
                    throw new Exception("Types count must match files count");

                string[] allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".gif", ".heic", ".png" };

                bool IsEntryByPCAR = isEntryByPCAR ?? false;
                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    /*your message */
                    Notes = "Mob app image upload",
                    IsSystemEntry = false,
                    EventDateTimeLocal = eventDateTimeLocal ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = eventDateTimeZone ?? TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = eventDateTimeZoneShort ?? TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = eventDateTimeUtcOffsetMinute ?? TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gpsCoordinates,
                    IsEntryByPCAR = IsEntryByPCAR,
                    PositionId = positionId,
                    CallSignId = callSignId,
                    EntryPassedByPCARclientsiteId = IsEntryByPCAR ? clientsiteId : null,
                };

                newPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(signInEntry);

                if (IsEntryByPCAR && logbookclientsiteId.HasValue && clientsiteId != logbookclientsiteId)
                {
                    // Make entry in corresponding non PCAR site logbook                    
                    var _NonPcarSiteLogEntry = new GuardLog
                    {
                        Id = 0,
                        ClientSiteLogBookId = 0,
                        GuardLoginId = null,
                        EventDateTime = signInEntry.EventDateTime,
                        Notes = signInEntry.Notes,
                        IsSystemEntry = signInEntry.IsSystemEntry,
                        EventDateTimeLocal = signInEntry.EventDateTimeLocal,
                        EventDateTimeLocalWithOffset = signInEntry.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = signInEntry.EventDateTimeZone,
                        EventDateTimeZoneShort = signInEntry.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = signInEntry.EventDateTimeUtcOffsetMinute,
                        GpsCoordinates = signInEntry.GpsCoordinates,
                        IsEntryByPCAR = signInEntry.IsEntryByPCAR,
                        PositionId = signInEntry.PositionId,
                        CallSignId = signInEntry.CallSignId,
                        EntryPassedByPCARclientsiteId = clientsiteId
                    };

                    var _NonPcarSitelogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(logbookclientsiteId.Value, logBookType);
                    guardLoginId = _mobileAppDataServices.GetGuardLoginId(_NonPcarSitelogBookId, guardId, logbookclientsiteId.Value, userId, IPAddress);

                    _NonPcarSiteLogEntry.ClientSiteLogBookId = _NonPcarSitelogBookId;
                    _NonPcarSiteLogEntry.GuardLoginId = guardLoginId;

                    if (_NonPcarSitelogBookId > 0 && guardLoginId > 0)
                    {
                        newNonPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(_NonPcarSiteLogEntry);
                        // Link the GuardLog entries
                        var linkid = _guardLogDataProvider.LinkGuardLogIds(newNonPcarGuardLogId, newPcarGuardLogId);
                        addNonPcarGuardLogEntry = true;
                    }
                }

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var type = types[i];

                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext))
                        throw new Exception($"Unsupported file type: {ext}");

                    string folderName = type?.ToLower() switch
                    {
                        "rear" => "RearFiles",
                        "twentyfive" => "TwentyfivePercentFiles",
                        _ => "OtherFiles"
                    };

                    string folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", newPcarGuardLogId.ToString(), folderName);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);



                    var dateTick = DateTime.Now.Ticks.ToString().Substring(10);
                    var uploadFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ext;
                    var fullPath = Path.Combine(folderPath, uploadFileName);

                    // Read uploaded file once
                    byte[] fileBytes;

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    // Save first copy
                    await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);

                    var finalFileName = uploadFileName;

                    // HEIC conversion
                    if (ext == ".heic")
                    {
                        var newPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                        await ConvertHeicToJpgAsync(fullPath, folderPath);
                        System.IO.File.Delete(fullPath);
                        finalFileName = Path.GetFileName(newPath);
                    }

                    var publicUrl = "https://cws-ir.com"; // Production Url                    
                    string baseUrl;
                    baseUrl = $"{Request.Scheme}://{Request.Host}";
                    if (_WebHostEnvironment.IsDevelopment())
                    {
                        publicUrl = baseUrl; // Local Url
                    }
                    else
                    {
                        // If test url
                        if (baseUrl.Contains("test."))
                        {
                            publicUrl = baseUrl;
                        }
                    }


                    var publicPath = $"{publicUrl}/DglUploads/{newPcarGuardLogId}/{folderName}/{finalFileName}";

                    var logImage = new GuardLogsDocumentImages
                    {
                        GuardLogId = newPcarGuardLogId,
                        ImagePath = publicPath,
                        IsRearfile = type?.ToLower() == "rear",
                        IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                    };

                    _guardLogDataProvider.SaveGuardLogDocumentImages(logImage);

                    uploadedFiles.Add(publicPath);


                    if (addNonPcarGuardLogEntry)
                    {
                        string folderPath2 = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", newNonPcarGuardLogId.ToString(), folderName);
                        if (!Directory.Exists(folderPath2))
                            Directory.CreateDirectory(folderPath2);

                        var fullPath2 = Path.Combine(folderPath2, uploadFileName);

                        // Save second copy
                        await System.IO.File.WriteAllBytesAsync(fullPath2, fileBytes);

                        var finalFileName2 = uploadFileName;

                        // HEIC conversion
                        if (ext == ".heic")
                        {
                            var newPath2 = Path.Combine(folderPath2, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                            await ConvertHeicToJpgAsync(fullPath2, folderPath2);
                            System.IO.File.Delete(fullPath2);
                            finalFileName2 = Path.GetFileName(newPath2);
                        }

                        var publicPath2 = $"{publicUrl}/DglUploads/{newNonPcarGuardLogId}/{folderName}/{finalFileName2}";
                        var logImage2 = new GuardLogsDocumentImages
                        {
                            GuardLogId = newNonPcarGuardLogId,
                            ImagePath = publicPath2,
                            IsRearfile = type?.ToLower() == "rear",
                            IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                        };

                        _guardLogDataProvider.SaveGuardLogDocumentImages(logImage2);
                    }
                }

                success = true;
                message = $"{uploadedFiles.Count} file(s) uploaded successfully.";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message, files = uploadedFiles });
        }

        [HttpPost("UploadMultipleOffLineSync")]
        public async Task<IActionResult> UploadMultipleOffLineSync([FromForm] List<IFormFile> files, [FromForm] string offlineFilesRecordJsonString)
        {
            bool success = false;
            string message = "Uploaded successfully";
            var uploadedFiles = new List<string>();

            // Deserialize metadata
            var offlineFilesRecords = JsonSerializer.Deserialize<List<OfflineFilesRecords>>(offlineFilesRecordJsonString);

            try
            {
                if (files == null || files.Count == 0)
                    throw new Exception("No files uploaded");

                if (offlineFilesRecords.Count != files.Count)
                    throw new Exception("Types count must match files count");

                var grouped = offlineFilesRecords.GroupBy(x => x.FileGroupId).ToList();
                //grouped will be: List<IGrouping<Guid, OfflineFilesRecords>>
                //Each group contains:
                //group.Key → the FileGroupId
                //group → the collection of OfflineFilesRecords belonging to that group

                foreach (var g in grouped)
                {

                    if (g.FirstOrDefault().guardId <= 0 || g.FirstOrDefault().clientsiteId <= 0)
                    {
                        Console.WriteLine("Invalid guard ID or client site ID.");
                        continue;
                    }


                    var logBookType = LogBookType.DailyGuardLog;
                    var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(g.FirstOrDefault().clientsiteId, logBookType, g.FirstOrDefault().EventDateTimeLocal.Value.Date);

                    if (logBookId <= 0)
                    {
                        Console.WriteLine("Failed to retrieve logbook ID.");
                        continue;
                    }


                    // Get Guard Login ID
                    var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                    var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, g.FirstOrDefault().guardId, g.FirstOrDefault().clientsiteId, g.FirstOrDefault().userId, IPAddress);

                    if (guardLoginId <= 0)
                    {
                        Console.WriteLine("Guard login failed.");
                        continue;
                    }

                    int newPcarGuardLogId = 0;
                    int newNonPcarGuardLogId = 0;
                    bool addNonPcarGuardLogEntry = false;

                    // Default GPS coordinates (should be replaced with actual values if available)
                    var gpsCoordinates = g.FirstOrDefault().gps;

                    // [Fix Date: 24-Jun-2026, Developer: Dileep]
                    // Pre-check if there are any valid files in this group before creating the blank logbook entry
                    bool hasValidFiles = false;
                    string[] allowedExtensionsPrecheck = { ".jpg", ".jpeg", ".bmp", ".gif", ".heic", ".png" };
                    foreach (var o in g)
                    {
                        var file = files.Where(x => x.FileName == o.FileNameCache).FirstOrDefault();
                        if (file != null && file.Length > 0 && allowedExtensionsPrecheck.Contains(Path.GetExtension(file.FileName).ToLower()))
                        {
                            hasValidFiles = true;
                            break;
                        }
                    }

                    if (!hasValidFiles)
                    {
                        // Mark all as synced so the mobile app doesn't keep retrying this broken batch forever
                        foreach (var o in g) o.IsSynced = true;
                        continue; // Skip creating the logbook entry completely!
                    }

                    var signInEntry = new GuardLog
                    {
                        ClientSiteLogBookId = logBookId,
                        GuardLoginId = guardLoginId,
                        EventDateTime = TimeZoneHelper.ConvertToSystemLocalTime(g.FirstOrDefault().EventDateTimeLocal.Value, g.FirstOrDefault().EventDateTimeUtcOffsetMinute.Value),
                        /*your message */
                        Notes = "Mob app image upload",
                        IsSystemEntry = false,
                        EventDateTimeLocal = g.FirstOrDefault().EventDateTimeLocal.Value,
                        EventDateTimeLocalWithOffset = g.FirstOrDefault().EventDateTimeLocalWithOffset.Value,
                        EventDateTimeZone = g.FirstOrDefault().EventDateTimeZone,
                        EventDateTimeZoneShort = g.FirstOrDefault().EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = g.FirstOrDefault().EventDateTimeUtcOffsetMinute.Value,
                        GpsCoordinates = gpsCoordinates,
                        IsOfflineRecord = true,
                        OfflineRecordSyncDateTime = DateTime.Now,
                        IsEntryByPCAR = g.FirstOrDefault().IsEntryByPCAR,
                        PositionId = g.FirstOrDefault().PositionId,
                        CallSignId = g.FirstOrDefault().CallSignId,
                        EntryPassedByPCARclientsiteId = g.FirstOrDefault().IsEntryByPCAR ? g.FirstOrDefault().clientsiteId : null,
                    };

                    newPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(signInEntry);


                    if (g.FirstOrDefault().IsEntryByPCAR && g.FirstOrDefault().LogbookclientsiteId.HasValue && g.FirstOrDefault().clientsiteId != g.FirstOrDefault().LogbookclientsiteId)
                    {
                        // Make entry in corresponding non PCAR site logbook
                        var _NonPcarSiteLogEntry = new GuardLog
                        {
                            Id = 0,
                            ClientSiteLogBookId = 0,
                            GuardLoginId = null,
                            EventDateTime = signInEntry.EventDateTime,
                            Notes = signInEntry.Notes,
                            IsSystemEntry = signInEntry.IsSystemEntry,
                            EventDateTimeLocal = signInEntry.EventDateTimeLocal,
                            EventDateTimeLocalWithOffset = signInEntry.EventDateTimeLocalWithOffset,
                            EventDateTimeZone = signInEntry.EventDateTimeZone,
                            EventDateTimeZoneShort = signInEntry.EventDateTimeZoneShort,
                            EventDateTimeUtcOffsetMinute = signInEntry.EventDateTimeUtcOffsetMinute,
                            GpsCoordinates = signInEntry.GpsCoordinates,
                            IsEntryByPCAR = signInEntry.IsEntryByPCAR,
                            PositionId = signInEntry.PositionId,
                            CallSignId = signInEntry.CallSignId,
                            EntryPassedByPCARclientsiteId = g.FirstOrDefault().clientsiteId
                        };

                        var _NonPcarSitelogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(g.FirstOrDefault().LogbookclientsiteId.Value, logBookType);
                        guardLoginId = _mobileAppDataServices.GetGuardLoginId(_NonPcarSitelogBookId, g.FirstOrDefault().guardId, g.FirstOrDefault().LogbookclientsiteId.Value, g.FirstOrDefault().userId, IPAddress);

                        _NonPcarSiteLogEntry.ClientSiteLogBookId = _NonPcarSitelogBookId;
                        _NonPcarSiteLogEntry.GuardLoginId = guardLoginId;


                        if (_NonPcarSitelogBookId > 0 && guardLoginId > 0)
                        {
                            newNonPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(_NonPcarSiteLogEntry);
                            // Link the GuardLog entries
                            var linkid = _guardLogDataProvider.LinkGuardLogIds(newNonPcarGuardLogId, newPcarGuardLogId);
                            addNonPcarGuardLogEntry = true;
                        }
                    }

                    foreach (var o in g)
                    {
                        o.IsSynced = true; // Marking file as sysnced

                        string[] allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".gif", ".heic", ".png" };

                        var file = files.FirstOrDefault(x => string.Equals(Path.GetFileName(x.FileName).Trim(), Path.GetFileName(o.FileNameCache).Trim(), StringComparison.OrdinalIgnoreCase));                       
                        var type = o.FileType;

                        // [Fix Date: 24-Jun-2026, Developer: Dileep]
                        // Exact Reason: Mobile app may send metadata but not the physical file (e.g., if deleted from device cache), making 'file' null.
                        // How it's fixed: Added a null check 'file == null' before accessing 'file.Length'. Without this, a NullReferenceException 
                        // crashes the loop, leaving a text-only log book entry "Mob app image upload" with no images saved.
                        if (file == null || file.Length == 0) continue;

                        var ext = Path.GetExtension(file.FileName).ToLower();
                        if (!allowedExtensions.Contains(ext))
                        {
                            Console.WriteLine($"Unsupported file type: {ext}");
                            SaveOfflineFilesRecordsError(o, $"Unsupported file type: {ext}");
                            continue;
                        }

                        string folderName = type?.ToLower() switch
                        {
                            "rear" => "RearFiles",
                            "twentyfive" => "TwentyfivePercentFiles",
                            _ => "OtherFiles"
                        };


                        string folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", newPcarGuardLogId.ToString(), folderName);
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);

                        var dateTick = DateTime.Now.Ticks.ToString().Substring(10);
                        var uploadFileName = Path.GetFileNameWithoutExtension(o.FileNameActual) + "_" + dateTick + ext;
                        var fullPath = Path.Combine(folderPath, uploadFileName);

                        // Read uploaded file once
                        byte[] fileBytes;

                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            fileBytes = memoryStream.ToArray();
                        }

                        // Save first copy
                        await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);

                        var finalFileName = uploadFileName;

                        // HEIC conversion
                        if (ext == ".heic")
                        {
                            var newPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(o.FileNameActual) + "_" + dateTick + ".jpg");
                            await ConvertHeicToJpgAsync(fullPath, folderPath);
                            System.IO.File.Delete(fullPath);
                            finalFileName = Path.GetFileName(newPath);
                        }

                        var publicUrl = "https://cws-ir.com"; // Production Url                    
                        string baseUrl;
                        baseUrl = $"{Request.Scheme}://{Request.Host}";
                        if (_WebHostEnvironment.IsDevelopment())
                        {
                            publicUrl = baseUrl; // Local Url
                        }
                        else
                        {
                            // If test url
                            if (baseUrl.Contains("test."))
                            {
                                publicUrl = baseUrl;
                            }
                        }
                        var publicPath = $"{publicUrl}/DglUploads/{newPcarGuardLogId}/{folderName}/{finalFileName}";

                        var logImage = new GuardLogsDocumentImages
                        {
                            GuardLogId = newPcarGuardLogId,
                            ImagePath = publicPath,
                            IsRearfile = type?.ToLower() == "rear",
                            IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                        };

                        _guardLogDataProvider.SaveGuardLogDocumentImages(logImage);

                        uploadedFiles.Add(publicPath);

                        if (addNonPcarGuardLogEntry)
                        {
                            string folderPath2 = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", newNonPcarGuardLogId.ToString(), folderName);
                            if (!Directory.Exists(folderPath2))
                                Directory.CreateDirectory(folderPath2);

                            var fullPath2 = Path.Combine(folderPath2, uploadFileName);

                            // Save second copy
                            await System.IO.File.WriteAllBytesAsync(fullPath2, fileBytes);

                            var finalFileName2 = uploadFileName;

                            // HEIC conversion
                            if (ext == ".heic")
                            {
                                var newPath2 = Path.Combine(folderPath2, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                                await ConvertHeicToJpgAsync(fullPath2, folderPath2);
                                System.IO.File.Delete(fullPath2);
                                finalFileName2 = Path.GetFileName(newPath2);
                            }

                            var publicPath2 = $"{publicUrl}/DglUploads/{newNonPcarGuardLogId}/{folderName}/{finalFileName2}";
                            var logImage2 = new GuardLogsDocumentImages
                            {
                                GuardLogId = newNonPcarGuardLogId,
                                ImagePath = publicPath2,
                                IsRearfile = type?.ToLower() == "rear",
                                IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                            };

                            _guardLogDataProvider.SaveGuardLogDocumentImages(logImage2);
                        }

                        success = true;
                        message = $"{uploadedFiles.Count} file(s) uploaded successfully.";
                    }

                }

                return Ok(offlineFilesRecords);
            }
            catch (Exception ex)
            {
                message = ex.Message;

                foreach (var r in offlineFilesRecords)
                {
                    SaveOfflineFilesRecordsError(r, ex.ToString());
                }
            }

            return Ok(offlineFilesRecords);
        }




        [HttpPost("UploadMultipleEdit")]
        public async Task<IActionResult> UploadMultipleEdit(
             [FromForm] List<IFormFile> files,
             [FromForm] List<string> types,   // <-- multiple types aligned with files
             [FromForm] int logbookId
        )
        {
            bool success = false;
            string message = "Uploaded successfully";
            var uploadedFiles = new List<string>();

            try
            {
                if (files == null || files.Count == 0)
                    throw new Exception("No files uploaded");

                if (types == null || types.Count != files.Count)
                    throw new Exception("Types count must match files count");

                string[] allowedExtensions = { ".jpg", ".jpeg", ".bmp", ".gif", ".heic", ".png" };

                int GuardLogId = logbookId;

                // Get all linked GuardLogIds once
                var linkedLogIds = _guardLogDataProvider
                    .GetLinkGuardLogIds(GuardLogId)
                    .Select(x => x.GuardLogId == GuardLogId
                        ? x.LinkedGuardLogId
                        : x.GuardLogId)
                    .Distinct()
                    .ToList();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var type = types[i];

                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext))
                        throw new Exception($"Unsupported file type: {ext}");

                    string folderName = type?.ToLower() switch
                    {
                        "rear" => "RearFiles",
                        "twentyfive" => "TwentyfivePercentFiles",
                        _ => "OtherFiles"
                    };

                    string folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", GuardLogId.ToString(), folderName);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var dateTick = DateTime.Now.Ticks.ToString().Substring(10);
                    var uploadFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ext;
                    var fullPath = Path.Combine(folderPath, uploadFileName);

                    // Read file once
                    byte[] fileBytes;

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);

                    var finalFileName = uploadFileName;

                    // HEIC conversion
                    if (ext == ".heic")
                    {
                        var newPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                        await ConvertHeicToJpgAsync(fullPath, folderPath);
                        System.IO.File.Delete(fullPath);
                        finalFileName = Path.GetFileName(newPath);
                    }

                    var publicUrl = "https://cws-ir.com"; // Production Url
                    string baseUrl;
                    baseUrl = $"{Request.Scheme}://{Request.Host}";
                    if (_WebHostEnvironment.IsDevelopment())
                    {
                        publicUrl = baseUrl; // Local Url
                    }
                    else
                    {
                        // If test url
                        if (baseUrl.Contains("test."))
                        {
                            publicUrl = baseUrl;
                        }
                    }
                    var publicPath = $"{publicUrl}/DglUploads/{GuardLogId}/{folderName}/{finalFileName}";

                    var logImage = new GuardLogsDocumentImages
                    {
                        GuardLogId = GuardLogId,
                        ImagePath = publicPath,
                        IsRearfile = type?.ToLower() == "rear",
                        IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                    };

                    _guardLogDataProvider.SaveGuardLogDocumentImages(logImage);

                    uploadedFiles.Add(publicPath);

                    // ==========================
                    // SAVE TO LINKED GUARD LOGS
                    // ==========================

                    foreach (var linkedGuardLogId in linkedLogIds)
                    {
                        string folderPath2 = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", linkedGuardLogId.ToString(), folderName);

                        Directory.CreateDirectory(folderPath2);

                        string fullPath2 = Path.Combine(folderPath2, uploadFileName);

                        await System.IO.File.WriteAllBytesAsync(fullPath2, fileBytes);

                        string finalFileName2 = uploadFileName;
                        if (ext == ".heic")
                        {
                            var newPath2 = Path.Combine(folderPath2, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                            await ConvertHeicToJpgAsync(fullPath2, folderPath2);
                            System.IO.File.Delete(fullPath2);
                            finalFileName2 = Path.GetFileName(newPath2);
                        }

                        var publicPath2 = $"{publicUrl}/DglUploads/{linkedGuardLogId}/{folderName}/{finalFileName2}";

                        _guardLogDataProvider.SaveGuardLogDocumentImages(
                            new GuardLogsDocumentImages
                            {
                                GuardLogId = linkedGuardLogId,
                                ImagePath = publicPath2,
                                IsRearfile = type?.ToLower() == "rear",
                                IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                            });

                    }
                }

                success = true;
                message = $"{uploadedFiles.Count} file(s) uploaded successfully.";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message, files = uploadedFiles });
        }



        [HttpPost("UploadMultipleVideos")]
        public async Task<IActionResult> UploadMultipleVideos([FromForm] List<IFormFile> files, [FromForm] int guardId, [FromForm] int clientsiteId,
            [FromForm] int userId, [FromForm] string gps, [FromForm] DateTime? eventDateTimeLocal, [FromForm] DateTimeOffset? eventDateTimeLocalWithOffset,
            [FromForm] string? eventDateTimeZone, [FromForm] string? eventDateTimeZoneShort, [FromForm] int? eventDateTimeUtcOffsetMinute,
            [FromForm] int? logbookclientsiteId, [FromForm] bool? isEntryByPCAR, [FromForm] int? callSignId, [FromForm] int? positionId
        )
        {
            bool success = false;
            string message = "Uploaded successfully";
            var uploadedFiles = new List<string>();

            int newPcarGuardLogId = 0;
            int newNonPcarGuardLogId = 0;
            bool addNonPcarGuardLogEntry = false;

            try
            {
                if (files == null || files.Count == 0)
                    throw new Exception("No videos uploaded");

                if (guardId <= 0 || clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });

                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, LogBookType.DailyGuardLog);
                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);
                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });


                bool IsEntryByPCAR = isEntryByPCAR ?? false;
                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Mob app video upload",
                    IsSystemEntry = false,
                    EventDateTimeLocal = eventDateTimeLocal ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = eventDateTimeLocalWithOffset ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = eventDateTimeZone ?? TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = eventDateTimeZoneShort ?? TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = eventDateTimeUtcOffsetMinute ?? TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gps,
                    IsEntryByPCAR = IsEntryByPCAR,
                    PositionId = positionId,
                    CallSignId = callSignId,
                    EntryPassedByPCARclientsiteId = IsEntryByPCAR ? clientsiteId : null,
                };

                newPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(signInEntry);

                string[] allowedExtensions = { ".mp4", ".mov", ".avi", ".mkv" };

                if (IsEntryByPCAR && logbookclientsiteId.HasValue && clientsiteId != logbookclientsiteId)
                {
                    // Make entry in corresponding non PCAR site logbook                    
                    var _NonPcarSiteLogEntry = new GuardLog
                    {
                        Id = 0,
                        ClientSiteLogBookId = 0,
                        GuardLoginId = null,
                        EventDateTime = signInEntry.EventDateTime,
                        Notes = signInEntry.Notes,
                        IsSystemEntry = signInEntry.IsSystemEntry,
                        EventDateTimeLocal = signInEntry.EventDateTimeLocal,
                        EventDateTimeLocalWithOffset = signInEntry.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = signInEntry.EventDateTimeZone,
                        EventDateTimeZoneShort = signInEntry.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = signInEntry.EventDateTimeUtcOffsetMinute,
                        GpsCoordinates = signInEntry.GpsCoordinates,
                        IsEntryByPCAR = signInEntry.IsEntryByPCAR,
                        PositionId = signInEntry.PositionId,
                        CallSignId = signInEntry.CallSignId,
                        EntryPassedByPCARclientsiteId = clientsiteId
                    };

                    var _NonPcarSitelogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(logbookclientsiteId.Value, LogBookType.DailyGuardLog);
                    guardLoginId = _mobileAppDataServices.GetGuardLoginId(_NonPcarSitelogBookId, guardId, logbookclientsiteId.Value, userId, IPAddress);

                    _NonPcarSiteLogEntry.ClientSiteLogBookId = _NonPcarSitelogBookId;
                    _NonPcarSiteLogEntry.GuardLoginId = guardLoginId;

                    if (_NonPcarSitelogBookId > 0 && guardLoginId > 0)
                    {
                        newNonPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(_NonPcarSiteLogEntry);
                        // Link the GuardLog entries
                        var linkid = _guardLogDataProvider.LinkGuardLogIds(newNonPcarGuardLogId, newPcarGuardLogId);
                        addNonPcarGuardLogEntry = true;
                    }
                }

                string folderName = "Videos";
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext) || !file.ContentType.StartsWith("video"))
                        throw new Exception($"Unsupported video type: {file.FileName}");

                    string folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", newPcarGuardLogId.ToString(), folderName);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var dateTick = DateTime.Now.Ticks.ToString().Substring(10);
                    var uploadFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ext;
                    var fullPath = Path.Combine(folderPath, uploadFileName);

                    // Read uploaded file once
                    byte[] fileBytes;

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    // Save first copy
                    await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);

                    var publicUrl = "https://cws-ir.com"; // Production Url
                    string baseUrl;
                    baseUrl = $"{Request.Scheme}://{Request.Host}";
                    if (_WebHostEnvironment.IsDevelopment())
                    {
                        publicUrl = baseUrl; // Local Url
                    }
                    else
                    {
                        // If test url
                        if (baseUrl.Contains("test."))
                        {
                            publicUrl = baseUrl;
                        }
                    }
                    var publicPath = $"{publicUrl}/DglUploads/{newPcarGuardLogId}/{folderName}/{uploadFileName}";

                    // Save record in the same table
                    var logFile = new GuardLogsDocumentImages
                    {
                        GuardLogId = newPcarGuardLogId,
                        ImagePath = publicPath,
                        IsVideo = true,
                        IsRearfile = false,
                        IsTwentyfivePercentfile = false
                    };

                    _guardLogDataProvider.SaveGuardLogDocumentImages(logFile);

                    uploadedFiles.Add(publicPath);

                    if (addNonPcarGuardLogEntry)
                    {
                        string folderPath2 = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", newNonPcarGuardLogId.ToString(), folderName);
                        if (!Directory.Exists(folderPath2))
                            Directory.CreateDirectory(folderPath2);

                        var fullPath2 = Path.Combine(folderPath2, uploadFileName);

                        // Save second copy
                        await System.IO.File.WriteAllBytesAsync(fullPath2, fileBytes);
                        var publicPath2 = $"{publicUrl}/DglUploads/{newNonPcarGuardLogId}/{folderName}/{uploadFileName}";
                        var logFile2 = new GuardLogsDocumentImages
                        {
                            GuardLogId = newNonPcarGuardLogId,
                            ImagePath = publicPath2,
                            IsVideo = true,
                            IsRearfile = false,
                            IsTwentyfivePercentfile = false
                        };

                        _guardLogDataProvider.SaveGuardLogDocumentImages(logFile2);
                    }
                }

                success = true;
                message = $"{uploadedFiles.Count} video(s) uploaded successfully.";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message, videos = uploadedFiles });
        }




        [HttpPost("SavePushNotificationTestMessage")]
        public IActionResult SavePushNotificationTestMessage(int guardId, int clientsiteId, int userId, string notifications, int rcPushMessageId)
        {
            var status = true;
            var message = "Success";

            try
            {
                if (guardId <= 0 || clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });
                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType);

                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                // Get Guard Login ID
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });
                // Save Guard Log Entry
                var signOffEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = notifications,
                    IrEntryType = IrEntryType.Normal,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute()
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);

                // Update acknowledgement
                _guardLogDataProvider.UpdateIsAcknowledged(rcPushMessageId);

                // Send notification to Citywatch logbook
                var clientSiteForLogbook = _clientDataProvider.GetClientSiteForRcLogBook();
                if (clientSiteForLogbook.Any())
                {
                    var logbookType = LogBookType.DailyGuardLog;
                    var logbookDate = DateTime.Today;

                    var logBookIdNew = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(
                        clientSiteForLogbook.First().Id, logbookType, out logbookDate);

                    var selectedGuardId = _guardLogDataProvider.GetGuardLogins(guardLoginId).FirstOrDefault()?.GuardId ?? 0;
                    var guardDetails = _guardLogDataProvider.GetGuards(selectedGuardId);
                    var guardInitials = guardDetails != null ? $"{guardDetails.Name} [{guardDetails.Initial}]" : "Unknown Guard";

                    var clientSiteId = _guardLogDataProvider.GetGuardLogins(guardLoginId).FirstOrDefault()?.ClientSiteId ?? 0;
                    var clientSiteName = _guardLogDataProvider.GetClientSites(clientSiteId).FirstOrDefault()?.Name ?? "Unknown Site";

                    var notifcationToCitywatch = new GuardLog
                    {
                        ClientSiteLogBookId = logBookIdNew,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = $"{notifications} - {guardInitials} - {clientSiteName}",
                        IrEntryType = IrEntryType.Normal,
                        EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                        EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                        EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                        EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                        EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute()
                    };

                    _guardLogDataProvider.SaveGuardLog(notifcationToCitywatch);
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = $"Error: {ex.Message}";
            }

            return Ok(new { status, message });
        }


        [HttpPost("SavePushNotificationTestMessageV2")]
        public IActionResult SavePushNotificationTestMessageV2([FromForm] int guardId, [FromForm] int clientsiteId, [FromForm] int userId, [FromForm] string notifications, [FromForm] int rcPushMessageId)
        {
            var status = true;
            var message = "Success";

            try
            {
                if (guardId <= 0 || clientsiteId <= 0)
                    return BadRequest(new { message = "Invalid guard ID or client site ID." });
                var logBookType = LogBookType.DailyGuardLog;
                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType);

                if (logBookId <= 0)
                    return BadRequest(new { message = "Failed to retrieve logbook ID." });

                // Get Guard Login ID
                var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });
                // Save Guard Log Entry
                var signOffEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = notifications,
                    IrEntryType = IrEntryType.Normal,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute()
                };
                _guardLogDataProvider.SaveGuardLog(signOffEntry);

                // Update acknowledgement
                _guardLogDataProvider.UpdateIsAcknowledged(rcPushMessageId);

                // Send notification to Citywatch logbook
                var clientSiteForLogbook = _clientDataProvider.GetClientSiteForRcLogBook();
                if (clientSiteForLogbook.Any())
                {
                    var logbookType = LogBookType.DailyGuardLog;
                    var logbookDate = DateTime.Today;

                    var logBookIdNew = _guardLogDataProvider.GetClientSiteLogBookIdByLogBookMaxID(
                        clientSiteForLogbook.First().Id, logbookType, out logbookDate);

                    var selectedGuardId = _guardLogDataProvider.GetGuardLogins(guardLoginId).FirstOrDefault()?.GuardId ?? 0;
                    var guardDetails = _guardLogDataProvider.GetGuards(selectedGuardId);
                    var guardInitials = guardDetails != null ? $"{guardDetails.Name} [{guardDetails.Initial}]" : "Unknown Guard";

                    var clientSiteName = _guardLogDataProvider.GetClientSites(clientsiteId).FirstOrDefault()?.Name ?? "Unknown Site";

                    var notifcationToCitywatch = new GuardLog
                    {
                        ClientSiteLogBookId = logBookIdNew,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = $"{notifications} - {guardInitials} - {clientSiteName}",
                        IrEntryType = IrEntryType.Normal,
                        EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                        EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                        EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                        EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                        EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute()
                    };

                    _guardLogDataProvider.SaveGuardLog(notifcationToCitywatch);
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = $"Error: {ex.Message}";
            }

            return Ok(new { status, message });
        }



        [HttpGet("GetTagStatus")]
        public ActionResult<IEnumerable<SiteTagStatus>> GetTagStatus(int clientId)
        {
            try
            {
                var result = _guardLogDataProvider.GetSiteTagStatus(clientId);
                if (result == null || result.Count == 0)
                {
                    var _ClientSiteTourMode = _clientDataProvider.GetClientSiteDetailsWithId(clientId).FirstOrDefault();
                    if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
                    {

                        List<Data.Providers.SiteTagStatus> _tgsts = new List<Data.Providers.SiteTagStatus>()
                        {
                            new Data.Providers.SiteTagStatus()
                                {
                                    ClientSiteId = clientId,
                                    CompletedRounds = 0,
                                    RemainingTags = 0,
                                    ScannedTags = 0,
                                    TotalTags = 0,
                                    Tour = _ClientSiteTourMode.PatrolTourMode.ToString()
                                }
                        };

                        return Ok(_tgsts);
                    }
                    else
                    {
                        return NotFound($"No tag status found for clientId {clientId}");
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching site tag status: {ex.Message}");
            }
        }


        [HttpGet("GetTagStatusPending")]
        public ActionResult<IEnumerable<SiteTagStatusPending>> GetTagStatusPending(int clientId)
        {
            try
            {
                var result = _guardLogDataProvider.GetTagStatusPending(clientId);
                if (result == null || result.Count == 0)
                    return NotFound($"No tag status found for clientId {clientId}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching site tag status: {ex.Message}");
            }
        }

        [HttpGet("GetClientSitesSmartWands")]
        public IActionResult GetClientSitesSmartWands(int userId, int clientSiteId)
        {
            try
            {
                var clientSites = _viewDataService.GetClientSiteSmartWands(clientSiteId);

                if (clientSites == null || !clientSites.Any())
                    return NotFound(new { message = "No smart wand for client site found." });

                var r = clientSites.Select(cs => new
                {
                    Id = cs.Id,
                    Name = cs.SmartWandId
                }).ToList();

                return Ok(r);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }



        [HttpGet("GetClientSitesByClientTypeWithAdress")]
        public IActionResult GetClientSitesByClientTypeWithAdress(int userId, int clientTypeId)
        {
            try
            {
                var clientSites = _viewDataService.GetUserClientSitesWithAddressUsingId(userId, clientTypeId);

                if (clientSites == null || !clientSites.Any())
                    return NotFound(new { message = "No client sites found." });

                return Ok(clientSites);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("DeleteFile")]
        public IActionResult DeleteFile([FromForm] int logbookId, [FromForm] string fileName)
        {
            try
            {
                _guardLogDataProvider.DeleteGuardLogDocumentImagesByLogId(logbookId, fileName);
                return Ok(new { success = true, message = "File deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetPcarDetails")]
        public IActionResult GetPcarDetails(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BadRequest(new { message = "Device ID is required" });

            var result = _viewDataService.GetPcarDetailsFromDevice(deviceId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPost]
        [Route("SaveVisitTime")]
        public async Task<IActionResult> SaveVisitTime([FromBody] VisitSaveDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request" });
            }

            try
            {
                var visit = new PcarRouteDailyVisits
                {
                    SmartWandId = dto.SmartWandId,
                    SiteId = dto.SiteId,
                    GuardId = dto.GuardId,

                    LoginUserId = dto.LoginUserId,
                    LoginSiteId = dto.LoginSiteId,

                    VisitName = dto.VisitName,
                    VisitNumber = dto.VisitNumber,
                    DayName = dto.DayName,

                    PcarRouteId = dto.PcarRouteId,
                    PcarRouteDetailsId = dto.PcarRouteDetailsId,

                    TimeOn = dto.TimeOn,
                    TimeOff = dto.TimeOff,

                    GpsCoordinates = dto.GpsCoordinates,
                    CreatedAt = DateTime.Now
                };

                await _guardLogDataProvider.SavePcarSaveVisitTimeAsync(visit);


                return Ok(new
                {
                    Success = true,
                    Message = "Saved successfully",
                    Data = visit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error saving data: " + ex.Message
                });
            }
        }


        [HttpGet("GetStates")]
        public IActionResult GetStates()
        {
            var result = _viewDataService.States;
            foreach (var state in result)
            {
                if (state.Value.ToLower() == "select" || state.Text.ToLower() == "select")
                {
                    result.Remove(state);
                    break;
                }
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("RegisterNewGuardFromMobile")]
        public async Task<IActionResult> RegisterNewGuardFromMobile([FromBody] NewGuard request)
        {
            if (request == null)
            {
                return BadRequest(new { Success = false, Message = "Invalid request" });
            }

            var initalsUsed = string.Empty;
            try
            {
                var RegisterGuard = new Guard
                {
                    Id = -1,
                    Name = request.Name,
                    Initial = request.Initial,
                    SecurityNo = request.SecurityNo,
                    Gender = request.Gender,
                    Mobile = request.Mobile,
                    Email = request.Email,
                    State = request.State,
                    IsLB_KV_IR = request.IsLB_KV_IR,
                    IsMobileAppAccess = request.IsMobileAppAccess,
                };

                if (!TryValidateModel(RegisterGuard))
                {
                    var errorMessage = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                    return StatusCode(500, new
                    {
                        IsSuccess = false,
                        message = "Error saving data: " + errorMessage,
                        Data = new NewGuard()
                    });
                }

                var g = _guardDataProvider.UpdateGuard(RegisterGuard, request.State, out initalsUsed);
                var msg = "Guard registered successfully.";
                if (initalsUsed != RegisterGuard.Initial)
                {
                    RegisterGuard.Initial = initalsUsed;
                    msg += $" Initials changed to {initalsUsed} due to duplication.";
                }
                return Ok(new
                {
                    IsSuccess = true,
                    message = msg,
                    Data = RegisterGuard
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    IsSuccess = false,
                    message = "Error saving data: " + ex.Message,
                    Data = new NewGuard()
                });
            }
        }

        public (SortedDictionary<int, IrProcessFailure> _processResult, SubDomain _domain, string _fileName) CreateAndSaveIr(string gps, int UserId,
            int IRguardId, int IRclientSiteId, IncidentRequest Report, string RequestDeviceType, bool isOffline = false)
        {
            var fileName = string.Empty;
            var processResult = new SortedDictionary<int, IrProcessFailure>();
            var reportGenerated = false;

            string input = GenerateFormattedString();
            string hashCode = GenerateHashCode(input);

            var GuardDetails = _clientDataProvider.GetGuradName(IRguardId);
            var domain = IsThirdParty(UserId);
            var nameParts = (GuardDetails.Name ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            string lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            Report.Officer = new Officer
            {
                FirstName = firstName,
                LastName = lastName,
                Gender = GuardDetails.Gender,
                Phone = GuardDetails.Mobile,
                // [FIX]: Preserve the Mobile App's selected Position
                Position = Report.Officer?.Position ?? string.Empty,
                Email = GuardDetails.Email,
                LicenseNumber = GuardDetails.SecurityNo,
                LicenseState = GuardDetails.State,
                // [FIX]: Preserve the Mobile App's inputted Callsign
                CallSign = Report.Officer?.CallSign ?? string.Empty,
                Billing = Report.Officer?.Billing ?? string.Empty,
                GuardMonth = Report.Officer?.GuardMonth,
                NotifiedBy = Report.Officer?.NotifiedBy
            };
            /* specific for mobile app Android*/
            Report.WebVersion = false;
            Report.Android = true;
            Report.iOS = false;
            if (RequestDeviceType == "ios")
            {
                Report.Android = false;
                Report.iOS = true;
            }

            var remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (Report?.SiteColourCodeId != null)
            {
                Report.SiteColourCode = _viewDataService.GetFeedbackTemplatesByTypeByColor(
                    3,
                    Convert.ToInt32(Report.SiteColourCodeId)
                );
            }
            // TODO: Remove session dependency on attachments
            //Report.ReportReference = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(Report.ReportReference))
                processResult.Add(9000, new IrProcessFailure("Session timeout due to user inactivity. Failed to attach files", string.Empty));

            try
            {
                Report.HASH = hashCode + "app";
                Report.IP = remoteIpAddress;
                Report.SerialNumber = GetIrSerialNumber(Report);
            }
            catch (Exception ex)
            {
                processResult.Add(9001, new IrProcessFailure($"Failed to get serial numbers. {ex.Message}", ex.StackTrace));
            }
            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == Report.DateLocation.ClientType);
            var clientSite = _clientDataProvider.GetClientSites(clientType.Id).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite);
            var PSPFName = _clientDataProvider.GetPSPF().SingleOrDefault(z => z.Name == Report.PSPFName);

            var clientSitePosition = _clientDataProvider.GetClientSitePosition(Report?.Officer?.Position);


            //live map settings 
            if (Report?.DateLocation?.ShowIncidentLocationAddress == true && !string.IsNullOrWhiteSpace(Report.DateLocation.ClientAddress))
            {
                var result = GetCoordinatesFromAddress(Report.DateLocation.ClientAddress);
                Report.DateLocation.ClientSiteLiveGps = result.Latitude + "," + result.Longitude;
            }
            else if (Report?.DateLocation?.IsUnknownGpsLocationAddress == true && string.IsNullOrEmpty(Report?.DateLocation?.ClientSiteLiveGps ?? string.Empty))
            {
                Report.DateLocation.ClientSiteLiveGps = gps;
            }
            else if (Report?.DateLocation?.IsClientSiteLocationAddress == true && string.IsNullOrEmpty(Report?.DateLocation?.ClientSiteLiveGps ?? string.Empty) && string.IsNullOrEmpty(clientSite.Gps))
            {
                Report.DateLocation.ClientSiteLiveGps = gps;
            }
            else if (string.IsNullOrEmpty(clientSite.Gps))
            {
                //mobile app current location shows as the map
                Report.DateLocation.ShowIncidentLocationAddress = true;
                Report.DateLocation.ClientSiteLiveGps = gps;

            }


            //To get the clientType oF position stop
            // var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(x => x.Name == Report.DateLocation.ClientSite);
            try
            {

                var templateFilename = CheckIfTheUrlIsAThirdPartyUrl(domain);
                fileName = _incidentReportGenerator.GeneratePdf(Report, clientSite, templateFilename);
                reportGenerated = true;
                //TempData["ReportFileName"] = fileName;
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
                ReportDateTime = Report?.DateLocation?.ReportDate ?? DateTime.MinValue,
                IncidentDateTime = Report?.DateLocation?.IncidentDate,
                JobNumber = Report?.DateLocation?.JobNumber ?? string.Empty,
                JobTime = Report?.DateLocation?.JobTime ?? string.Empty,
                CallSign = Report?.Officer?.CallSign ?? string.Empty,
                NotifiedBy = Report?.Officer?.NotifiedBy ?? string.Empty,
                Billing = Report?.Officer?.Billing ?? string.Empty,
                IsEventFireOrAlarm = (Report?.EventType?.AlarmActive ?? false) ||
                         (Report?.EventType?.AlarmDisabled ?? false) ||
                         (Report?.EventType?.Emergency ?? false),
                OccurNo = Report?.OccurrenceNo ?? string.Empty,
                ActionTaken = Report?.Feedback ?? string.Empty,
                // [FIX]: Automatically apply Patrol flag from DB config
                IsPatrol = (Report?.IsPositionPatrolCar ?? false) || (clientSitePosition?.IsPatrolCar ?? false),
                Position = Report?.Officer?.Position ?? string.Empty,
                ClientArea = Report?.DateLocation?.ClientArea ?? string.Empty,
                SerialNo = Report?.SerialNumber ?? string.Empty,
                ColourCode = Report?.SiteColourCodeId,
                IsPlateLoaded = Report?.PlateLoadedYes ?? false,
                PlateId = 0,
                VehicleRego = null,
                LogId = IRguardId,
                IncidentReportEventTypes = Report?.IrEventTypes?.Select(z => new IncidentReportEventType() { EventType = z }).ToList()
                               ?? new List<IncidentReportEventType>(),
                PSPFId = PSPFName?.Id ?? 0,

                // Time zone info (optional fallback)

                CreatedOnDateTimeLocal = isOffline ? Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeLocal : TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                CreatedOnDateTimeLocalWithOffset = isOffline ? Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeLocalWithOffset : TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                CreatedOnDateTimeZone = isOffline ? Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeZone : TimeZoneHelper.GetCurrentTimeZone(),
                CreatedOnDateTimeZoneShort = isOffline ? Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeZoneShort : TimeZoneHelper.GetCurrentTimeZoneShortName(),
                CreatedOnDateTimeUtcOffsetMinute = isOffline ? Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeUtcOffsetMinute : TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                HASH = hashCode,
                ClientSitePositionId = clientSitePosition?.Id,
                GuardId = IRguardId,

            };



            //var report = new IncidentReport()
            //{

            //    FileName = fileName,
            //    CreatedOn = DateTime.UtcNow,
            //    ClientSiteId = clientSite?.Id,
            //    ReportDateTime = Report.DateLocation.ReportDate,
            //    IncidentDateTime = Report.DateLocation.IncidentDate,
            //    JobNumber = Report.DateLocation.JobNumber,
            //    JobTime = Report.DateLocation.JobTime,
            //    CallSign = Report.Officer.CallSign,
            //    NotifiedBy = Report.Officer.NotifiedBy,
            //    Billing = Report.Officer.Billing,
            //    IsEventFireOrAlarm = Report.EventType.AlarmActive || Report.EventType.AlarmDisabled || Report.EventType.Emergency,
            //    OccurNo = Report.OccurrenceNo,
            //    ActionTaken = Report.Feedback,
            //    IsPatrol = Report.IsPositionPatrolCar,
            //    Position = Report.Officer.Position,
            //    ClientArea = Report.DateLocation.ClientArea,
            //    SerialNo = Report.SerialNumber,
            //    ColourCode = Report.SiteColourCodeId,
            //    IsPlateLoaded = Report.PlateLoadedYes,
            //    PlateId = 0,
            //    VehicleRego = null,
            //    LogId = IRguardId,
            //    IncidentReportEventTypes = Report.IrEventTypes.Select(z => new IncidentReportEventType() { EventType = z }).ToList(),
            //    PSPFId = PSPFName.Id,
            //    CreatedOnDateTimeLocal = Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy -- Start
            //    CreatedOnDateTimeLocalWithOffset = Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeLocalWithOffset,
            //    CreatedOnDateTimeZone = Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeZone,
            //    CreatedOnDateTimeZoneShort = Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeZoneShort,
            //    CreatedOnDateTimeUtcOffsetMinute = Report.ReportCreatedLocalTimeZone.CreatedOnDateTimeUtcOffsetMinute, // Task p6#73_TimeZone issue -- added by Binoy -- End
            //    HASH = hashCode,
            //    ClientSitePositionId = clientSitePosition?.ClientsiteId,
            //    GuardId = IRguardId

            //};





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
                        var incidentreportid = _clientDataProvider.GetMaxIncidentReportId(IRguardId);
                        var incidentreportsplateid = _clientDataProvider.GetIncidentDetailsKvlReport(IRguardId);
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
                        CreateGuardLogEntry(report, IRguardId, UserId, gps);
                    CreateControlRoomLogEntry(report, IRguardId, UserId, gps);//To Save in the control room

                    if (report.ClientSitePositionId.HasValue)
                    {
                        //CreatePositionGuardLogEntry(report, IRguardId, UserId, gps);


                        // Attempt to find the actual Patrol Car site the guard is currently logged into
                        int actualPatrolSiteId = IRclientSiteId;
                        var patrolLogin = _context.GuardLogins
                            .Include(g => g.ClientSiteLogBook.ClientSite)
                            .Where(g => g.GuardId == IRguardId && g.ClientSiteLogBook.ClientSite.PatrolTourMode == CityWatch.Data.Enums.PatrolTouringMode.PCAR)
                            .OrderByDescending(g => g.Id)
                            .FirstOrDefault(g => g.OnDuty >= DateTime.Now.AddHours(-16));

                        if (patrolLogin != null)
                        {
                            actualPatrolSiteId = patrolLogin.ClientSiteId;
                        }

                        if (actualPatrolSiteId > 0 && report.ClientSiteId.HasValue && actualPatrolSiteId != report.ClientSiteId.Value)
                        {
                            CreatePatrolCarGuardLogEntry(report, actualPatrolSiteId, IRguardId, UserId, gps);
                        }
                    }

                    //if (report.ClientSiteId.HasValue)
                    //    CreateGuardLogEntry(report, IRguardId, UserId, gps);

                    //// Attempt to find the actual Patrol Car site the guard is currently logged into
                    //int actualPatrolSiteId = IRclientSiteId;
                    //var patrolLogin = _context.GuardLogins
                    //    .Include(g => g.ClientSiteLogBook.ClientSite)
                    //    .Where(g => g.GuardId == IRguardId && g.ClientSiteLogBook.ClientSite.PatrolTourMode == CityWatch.Data.Enums.PatrolTouringMode.PCAR)
                    //    .OrderByDescending(g => g.Id)
                    //    .FirstOrDefault(g => g.OnDuty >= DateTime.Now.AddHours(-16));

                    //if (patrolLogin != null)
                    //{
                    //    actualPatrolSiteId = patrolLogin.ClientSiteId;
                    //}

                    //if (actualPatrolSiteId > 0 && report.ClientSiteId.HasValue && actualPatrolSiteId != report.ClientSiteId.Value)
                    //{
                    //    CreatePatrolCarGuardLogEntry(report, actualPatrolSiteId, IRguardId, UserId, gps);
                    //}

                    //CreateControlRoomLogEntry(report, IRguardId, UserId, gps);//To Save in the control room


                    //if (report.ClientSitePositionId.HasValue && report.ClientSitePositionId.Value != report.ClientSiteId && report.ClientSitePositionId.Value != actualPatrolSiteId)
                    //{
                    //    CreatePositionGuardLogEntry(report, IRguardId, UserId, gps);
                    //}


                }
                catch (Exception ex)
                {
                    processResult.Add(9013, new IrProcessFailure($"Failed to save logbook entry. {ex.Message}", ex.StackTrace));
                }

                try
                {
                    if (true)
                    {

                        SendEmailWithAzureBlob(Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName), Report, domain);

                        /* Save log for duress button enable Start 02032024 dileep*/
                        var guradDetailsName = "Admin";
                        var guardId = 0;
                        if (IRguardId != 0)
                        {
                            var GuradDetails = _clientDataProvider.GetGuradName(IRguardId);
                            guradDetailsName = GuradDetails.Name;
                            guardId = GuradDetails.Id;
                        }
                        _SiteEventLogDataProvider.SaveSiteEventLogData(
                            new SiteEventLog()
                            {
                                GuardId = guardId,
                                SiteId = report.ClientSiteId,
                                GuardName = guradDetailsName,
                                SiteName = _guardLogDataProvider.GetClientSites(report.ClientSiteId).FirstOrDefault().Name,
                                ProjectName = "ClientPortal",
                                ActivityType = "IR Generated App",
                                Module = "Incident",
                                SubModule = "Register",
                                GoogleMapCoordinates = "",
                                IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                                ToAddress = string.Empty,
                                ToMessage = string.Empty,
                                EventTime = DateTime.Now,
                                EventLocalTime = DateTime.Now,
                                EventStatus = "IR Generated"
                            }
                         );
                        /* Save log for duress button enable end*/
                    }
                    else
                    {
                        /* Store in Azure blobwithout mail send 06/032024 dileep*/
                        AzureBlobUploadIrUploadWithOutMail(Path.Combine(_WebHostEnvironment.WebRootPath, "Pdf", "Output", fileName));

                        /* Save log for duress button enable Start 02032024 dileep*/
                        var guradDetailsName = "Admin";
                        var guardId = 0;
                        if (IRguardId != 0)
                        {
                            var GuradDetails = _clientDataProvider.GetGuradName(IRguardId);
                            guradDetailsName = GuradDetails.Name;
                            guardId = GuradDetails.Id;
                        }
                        _SiteEventLogDataProvider.SaveSiteEventLogData(
                            new SiteEventLog()
                            {
                                GuardId = guardId,
                                SiteId = report.ClientSiteId,
                                GuardName = guradDetailsName,
                                SiteName = _guardLogDataProvider.GetClientSites(report.ClientSiteId).FirstOrDefault().Name,
                                ProjectName = "ClientPortal",
                                ActivityType = "IR Generated",
                                Module = "Incident",
                                SubModule = "Register",
                                GoogleMapCoordinates = "",
                                IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                                ToAddress = string.Empty,
                                ToMessage = string.Empty,
                                EventTime = DateTime.Now,
                                EventLocalTime = DateTime.Now,
                                EventStatus = "IR Generated without email"
                            }
                         );
                        /* Save log for duress button enable end*/
                    }
                }
                catch (Exception ex)
                {
                    processResult.Add(9004, new IrProcessFailure($"Failed to send email. {ex.Message}", ex.StackTrace));
                }
            }

            //TempData["ReportGenerated"] = reportGenerated;
            if (processResult.Count > 0)
            {
                //TempData["Error"] = string.Join(Environment.NewLine, processResult.Select(z => $"{z.Key} - {z.Value.ErrorMessage}"));
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

            return (processResult, domain, fileName);
        }

        #region "HR Records"

        [HttpGet("ValidateGuardPinForHrRecordAccess")]
        public IActionResult ValidateGuardPinForHrRecordAccess(int guardId, string key)
        {
            var AccessPermission = false;
            int? LoggedInUserId = 0;
            string SuccessMessage = string.Empty;
            int? SuccessCode = 0;
            int? GuId = 0;
            try
            {

                (AccessPermission, LoggedInUserId, GuId, SuccessCode, SuccessMessage) = _viewDataService.ValidateGuardHrPin(guardId, key);
                return Ok(new { issuccess = true, message = SuccessMessage, data = AccessPermission });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    issuccess = false,
                    message = $"An error occurred while validating the PIN.{ex.Message}",
                    data = AccessPermission
                });
            }
        }

        [HttpGet("GetGuardHrRecords")]
        public IActionResult GetGuardHrRecords(int guardId)
        {
            try
            {
                var result = _viewDataService.GetGuardLicenseAndComplianceData(guardId);
                var returnResult = result.Select(x => new GuardComplianceAndLicenseDTO
                {
                    Id = x.Id,
                    GuardId = x.GuardId,
                    Description = x.Description,
                    ExpiryDate = x.ExpiryDate,
                    FileName = x.FileName,
                    FileUrl = x.FileUrl,
                    HrGroup = x.HrGroup,
                    HrGroupText = x.HrGroupText,
                    CurrentDateTime = x.CurrentDateTime,
                    Reminder1 = x.Reminder1,
                    Reminder2 = x.Reminder2,
                    LicenseNo = x.LicenseNo,
                    DateType = x.DateType,
                    IsDateFilterEnabledHidden = x.IsDateFilterEnabledHidden,
                    HRBanEdit = x.HRBanEdit,
                    IsLogin = x.IsLogin,
                    MasterDateType = x.MasterDateType,
                    StatusColor = x.StatusColor
                }).ToList();


                return Ok(new
                {
                    issuccess = true,
                    message = "Successfully retrieved guard compliance details.",
                    data = returnResult
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    issuccess = false,
                    message = $"An error occurred while retriving hr records.{ex.Message}",
                    guardcomplianceandlicense = new List<GuardComplianceAndLicense>()
                });
            }
        }


        [HttpGet("GetHrGroupsList")]
        public IActionResult GetHrGroupsList()
        {
            string SuccessMessage = string.Empty;

            try
            {
                var list = _viewDataService.GetHRGroups();
                return Ok(new { issuccess = true, message = SuccessMessage, data = list });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    issuccess = false,
                    message = $"An error occurred while retrieving HR Groups.{ex.Message}",
                    data = new List<HRGroups>()
                });
            }
        }

        [HttpGet("GetHrGroupDescriptionsList")]
        public IActionResult GetHrGroupDescriptionsList(int HRid, int GuardID)
        {
            string SuccessMessage = string.Empty;

            try
            {
                var list = _viewDataService.GetHRDescription(HRid, GuardID);
                return Ok(new { issuccess = true, message = SuccessMessage, data = list });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    issuccess = false,
                    message = $"An error occurred while retrieving HR Groups Descriptions.{ex.Message}",
                    data = new List<HRGroups>()
                });
            }
        }

        [HttpGet("CheckForHrDescriptionBan")]
        public async Task<IActionResult> CheckForHrDescriptionBan(int DescriptionID)
        {
            bool hrban = false;

            try
            {
                var result = await _viewDataService.GetHRDescriptionBanDetailsAsync(DescriptionID);

                if (result != null)
                {
                    hrban = result.HRBanEdit;
                }

                return Ok(new
                {
                    issuccess = true,
                    message = string.Empty,
                    data = hrban
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    issuccess = false,
                    message = $"An error occurred while checking for HR Description Ban. {ex.Message}",
                    data = hrban
                });
            }
        }



        [HttpPost("SaveHrRecordOfGuard")]
        public async Task<IActionResult> SaveHrRecordOfGuard([FromForm] IFormFile Docfile, [FromForm] GuardComplianceAndLicenseDTO guardComplianceAndLicenseDTO)
        {
            //bool success = false;
            //string message = "Uploaded successfully";
            //var uploadedFiles = new List<string>();

            try
            {
                var _guard = _guardDataProvider.GetGuardDetailsUsingId(guardComplianceAndLicenseDTO.GuardId).FirstOrDefault();

                int? resolvedHrSettingsId = guardComplianceAndLicenseDTO.HrSettingsId > 0 ? guardComplianceAndLicenseDTO.HrSettingsId : null;
                if (resolvedHrSettingsId == null && !string.IsNullOrWhiteSpace(guardComplianceAndLicenseDTO.Description))
                {
                    var cleanDesc = guardComplianceAndLicenseDTO.Description.ToLower().Trim();
                    var hrSettingsList = _context.HrSettings.ToList();
                    var matchingSetting = hrSettingsList.FirstOrDefault(s =>
                        cleanDesc == s.Description.ToLower().Trim() ||
                        Regex.IsMatch(cleanDesc, $@"(?<=^|\s){Regex.Escape(s.Description.ToLower().Trim())}(?=\s|$)")
                    );
                    if (matchingSetting != null)
                    {
                        resolvedHrSettingsId = matchingSetting.Id;
                    }
                }

                var guardComplianceAndLicense = new GuardComplianceAndLicense
                {
                    Id = guardComplianceAndLicenseDTO.Id,
                    GuardId = guardComplianceAndLicenseDTO.GuardId,
                    Description = guardComplianceAndLicenseDTO.Description,
                    ExpiryDate = guardComplianceAndLicenseDTO.ExpiryDate,
                    FileName = guardComplianceAndLicenseDTO.FileName,
                    HrGroup = guardComplianceAndLicenseDTO.HrGroup,
                    Guard = _guard,
                    CurrentDateTime = guardComplianceAndLicenseDTO.CurrentDateTime,
                    Reminder1 = guardComplianceAndLicenseDTO.Reminder1,
                    Reminder2 = guardComplianceAndLicenseDTO.Reminder2,
                    DateType = guardComplianceAndLicenseDTO.DateType,
                    LicenseNo = guardComplianceAndLicenseDTO.LicenseNo,
                    // Mapping HrSettingsId for graceful migration
                    HrSettingsId = resolvedHrSettingsId

                };

                // Upload file to server folder first
                if (Docfile != null && Docfile.Length > 0)
                {
                    var fileuploaded = await _viewDataService.UploadHrDocumentFileToServer(Docfile, guardComplianceAndLicenseDTO.LicenseNo, guardComplianceAndLicenseDTO.FileName);
                    if (!fileuploaded)
                        return Ok(new { issuccess = false, message = $"Could not upload Hr document file.", data = false });
                }
                else if (guardComplianceAndLicenseDTO.Id <= 0)
                {
                    return Ok(new { issuccess = false, message = $"Hr document file is missing.", data = false });
                }

                if (!string.IsNullOrEmpty(guardComplianceAndLicense.Description))
                {
                    guardComplianceAndLicense.Description = Regex.Replace(guardComplianceAndLicense.Description, "[✔️❌]", "").Trim();
                }

                (bool status, bool dbxUploaded, IEnumerable<string> msg) = _viewDataService.SaveOrUpdateGuardComplianceandlicanseNew(guardComplianceAndLicense);
                return Ok(new { issuccess = status, message = string.Join(",", msg.Where(x => !string.IsNullOrWhiteSpace(x))), data = dbxUploaded });
            }
            catch (Exception ex)
            {
                return Ok(new { issuccess = false, message = $"An error occurred while saving HR record of guard\n.{ex.Message}", data = false });
            }



        }

        [HttpPost("DeleteHrRecordOfGuard")]
        public async Task<IActionResult> DeleteHrRecordOfGuard([FromBody] int id)
        {

            var success = true;
            var msg = "Hr Document deleted successfully.";
            try
            {
                _viewDataService.DeleteGuardHrDocument(id);
            }
            catch (Exception ex)
            {
                success = false;
                msg = ex.Message;
            }
            return Ok(new { IsSuccess = success, message = msg });
        }

        #endregion "HR Records"

        [HttpPost("SyncOfflinePatrolCarLogData")]
        public IActionResult SyncOfflinePatrolCarLogData([FromBody] List<PatrolCarLogRequestLocalCacheOffline> offlineRecords)
        {
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            if (offlineRecords != null && offlineRecords.Count > 0)
            {
                bool isSuccess = false;
                foreach (var offlineRecord in offlineRecords.OrderBy(x => x.EventDateTimeLocal))
                {
                    isSuccess = false;
                    try
                    {
                        var logBookType = LogBookType.DailyGuardLog;
                        var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(offlineRecord.SiteId, logBookType, ((DateTime)offlineRecord.EventDateTimeLocal).Date);
                        PatrolCarLog patrolCarLog = new()
                        {
                            Id = offlineRecord.Id,
                            PatrolCarId = offlineRecord.PatrolCarId,
                            ClientSiteLogBookId = offlineRecord.ClientSiteLogBookId,
                            Mileage = offlineRecord.Mileage,
                        };
                        isSuccess = _viewDataService.SavePatrolCarLog(patrolCarLog);
                        if (!isSuccess)
                        {
                            // Save the record in DB to process later.
                            SaveSyncOfflinePatrolCarLogDataError(offlineRecord, "Error occured while saving patrol car log.");
                        }

                        offlineRecord.IsSynced = true;

                        Thread.Sleep(500); //wait a while since signalR pushes the refresh signal for logbook refresh

                    }
                    catch (Exception ex)
                    {
                        SaveSyncOfflinePatrolCarLogDataError(offlineRecord, ex.ToString());
                        offlineRecord.IsSynced = true;
                    }
                }
            }

            return Ok(offlineRecords);

        }

        [HttpPost("SyncOfflineCustomFieldLogData")]
        public IActionResult SyncOfflineCustomFieldLogData([FromBody] List<CustomFieldLogRequestHeadLocalCacheOffline> offlineRecords)
        {
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            if (offlineRecords != null && offlineRecords.Count > 0)
            {
                bool isSuccess = false;
                foreach (var offlineRecord in offlineRecords.OrderBy(x => x.EventDateTimeLocal))
                {
                    isSuccess = false;
                    try
                    {
                        var logBookType = LogBookType.DailyGuardLog;
                        var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(offlineRecord.SiteId, logBookType, ((DateTime)offlineRecord.EventDateTimeLocal).Date);
                        var records = offlineRecord.Details
                            .GroupBy(d => d.DictKey)
                            .ToDictionary(g => g.Key, g => g.First().DictValue);

                        isSuccess = _viewDataService.SaveCustomFieldLog(logBookId, records);
                        if (!isSuccess)
                        {
                            // Save the record in DB to process later.
                            SaveSyncOfflineCustomFieldLogDataError(offlineRecord, "Error occured while saving Custom Field log.");
                        }

                        offlineRecord.IsSynced = true;

                        Thread.Sleep(500); //wait a while since signalR pushes the refresh signal for logbook refresh

                    }
                    catch (Exception ex)
                    {
                        SaveSyncOfflineCustomFieldLogDataError(offlineRecord, ex.ToString());
                        offlineRecord.IsSynced = true;
                    }
                }
            }

            return Ok(offlineRecords);

        }

        [HttpPost("SyncOfflineIrRecords")]
        public async Task<IActionResult> SyncOfflineIrRecords([FromForm] List<IFormFile> files, [FromForm] string irOfflineFilesAttachmentsCacheJsonString,
            [FromForm] string irOfflineCacheJsonString, [FromForm] string irDeviceType)
        {
            //bool success = false;
            //string message = "Uploaded successfully";
            //var uploadedFiles = new List<string>();

            // Deserialize metadata
            var offlineIrRecords = JsonSerializer.Deserialize<List<irOfflineCache>>(irOfflineCacheJsonString);
            var offlineIrAttachmentRecords = JsonSerializer.Deserialize<List<irOfflineFilesAttachmentsCache>>(irOfflineFilesAttachmentsCacheJsonString);


            // 1. upload each file and add filename to report
            foreach (var r in offlineIrAttachmentRecords)
            {
                var file = files.Where(x => x.FileName == r.FileNameCache).FirstOrDefault();

                // [Fix Date: 24-Jun-2026, Developer: Dileep]
                // Exact Reason: If the physical file isn't uploaded by the mobile app, 'file' is null.
                // How it's fixed: Added 'file == null' to prevent NullReferenceException crash during offline IR attachment sync.
                if (file == null || file.Length == 0) continue;

                var (rtn, msg, _filename) = await UploadIrFilesAndReturnName(r.IrId, file);
                r.IsSynced = true;

                if (rtn)
                {
                    r.ServerFileNameWithPath = _filename;

                    var _rcd = offlineIrRecords.Where(x => x.IrId == r.IrId).FirstOrDefault();
                    if (_rcd != null)
                    {
                        _rcd.IncidentRequest.Attachments ??= new List<string>();
                        _rcd.IncidentRequest.Attachments.Add(_filename);
                    }

                }
                else
                {
                    // Save record into not synced table
                    SaveSyncofflineIrAttachmentRecordsDataError(r, msg);
                }
            }

            foreach (var ir in offlineIrRecords)
            {
                var Report = ir.IncidentRequest;
                var (processResult, domain, fileName) = CreateAndSaveIr(ir.gps, ir.userId, ir.guardId, ir.clientsiteId, Report, irDeviceType);
                if (processResult != null)
                {
                    var errors = processResult.Select(p => new { Code = p.Key, Message = p.Value.ErrorMessage });
                    if (errors.Any())
                    {
                        var errorText = string.Join(", ", errors.Select(e => $"{e.Code}: {e.Message}"));
                        SaveSyncofflineIrRecordsDataError(ir, errorText);
                    }
                }
                ir.IsSynced = true;
            }

            return Ok(new { irOfflineCache = offlineIrRecords, irOfflineAttachments = offlineIrAttachmentRecords });

        }

        [HttpGet("GetClientSiteTourMode")]
        public IActionResult GetClientSiteTourMode(int clientSiteId)
        {
            try
            {
                var site = _guardLogDataProvider
                            .GetClientSites(clientSiteId)
                            .FirstOrDefault();

                if (site == null)
                    return NotFound("Client site not found");

                return Ok(site.PatrolTourMode.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetLinkedSitesForRoster")]
        public async Task<IActionResult> GetLinkedSitesForRoster(int siteId)
        {
            try
            {
                var allLinkedSites = _guardLogDataProvider.getallClientSitesLinkedDuress(siteId);
                bool isLinkedSiteGroup = allLinkedSites != null && allLinkedSites.Any();
                var resultSites = new List<object>();

                if (isLinkedSiteGroup)
                {
                    foreach (var linkedSite in allLinkedSites)
                    {
                        var siteDetails = await _context.ClientSites.FirstOrDefaultAsync(x => x.Id == linkedSite.ClientSiteId);
                        if (siteDetails != null)
                        {
                            resultSites.Add(new
                            {
                                SiteId = siteDetails.Id,
                                SiteName = siteDetails.Name
                            });
                        }
                    }
                }
                else
                {
                    var siteDetails = await _context.ClientSites.FirstOrDefaultAsync(x => x.Id == siteId);
                    if (siteDetails != null)
                    {
                        resultSites.Add(new
                        {
                            SiteId = siteDetails.Id,
                            SiteName = siteDetails.Name
                        });
                    }
                }

                return Ok(new
                {
                    isLinkedSiteGroup = isLinkedSiteGroup,
                    sites = resultSites
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching linked sites.", error = ex.Message });
            }
        }

        [HttpGet("GetRoster")]
        public async Task<IActionResult> GetRoster(int guardId, int siteId, string? date = null)
        {
            try
            {
                DateTime selectedDate;
                if (string.IsNullOrEmpty(date))
                {
                    selectedDate = DateTime.Today;
                }
                else
                {
                    if (!DateTime.TryParse(date, out selectedDate))
                    {
                        selectedDate = DateTime.Today;
                    }
                }

                // 1. Calculate Start of Week (Logic matched from GuardRosterAction)
                var timesheet = _clientDataProvider.GetTimesheetDetails();
                DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
                if (timesheet != null && !string.IsNullOrEmpty(timesheet.weekName))
                {
                    if (Enum.TryParse<DayOfWeek>(timesheet.weekName, true, out var parsedDay))
                    {
                        firstDayOfWeek = parsedDay;
                    }
                }

                int diff = (7 + (selectedDate.DayOfWeek - firstDayOfWeek)) % 7;
                var startDate = selectedDate.AddDays(-1 * diff).Date;
                var totalEndDate = startDate.AddDays(7).AddSeconds(-1);

                // 2. Fetch schedules for the specific site
                var schedules = await _context.RosterSchedules
                    .Where(x => x.ClientSiteId == siteId && !x.IsDeleted && x.ShiftStart >= startDate && x.ShiftStart <= totalEndDate)
                    .Include(x => x.Guard)
                    .Include(x => x.ReliefGuard)
                    .Include(x => x.Callsign)
                    .Include(x => x.PayRate)
                    .OrderBy(x => x.ShiftStart)
                    .ToListAsync();

                var site = await _context.ClientSites.Include(s => s.ClientType).FirstOrDefaultAsync(x => x.Id == siteId);

                // 3. Group shifts by day (7 days)
                var days = new List<object>();
                for (int i = 0; i < 7; i++)
                {
                    var loopDate = startDate.AddDays(i).Date;
                    var dayShifts = schedules
                        .Where(s => s.ShiftStart.Date == loopDate)
                        .OrderBy(s => s.ShiftStart)
                        .Select(s => new
                        {
                            s.Id,
                            shiftStart = s.ShiftStart.ToString("HH:mm"),
                            shiftEnd = s.ShiftEnd.ToString("HH:mm"),
                            guardId = s.GuardId,
                            guardName = s.Guard != null ? s.Guard.Name : s.ProviderName,
                            reliefGuardId = s.ReliefGuardId,
                            reliefGuardName = s.ReliefGuard != null ? s.ReliefGuard.Name : s.ReliefProviderName,
                            reliefProviderName = s.ReliefProviderName,
                            reliefReason = s.ReliefReason,
                            guardLicense = s.Guard != null ? s.Guard.SecurityNo : "",
                            reliefGuardLicense = s.ReliefGuard != null ? s.ReliefGuard.SecurityNo : "",
                            guardProvider = !string.IsNullOrEmpty(s.ProviderName) ? s.ProviderName : (s.Guard != null ? (s.Guard.Provider ?? "N/A") : "N/A"),
                            shiftType = s.ShiftType ?? "Regular",
                            status = (int)s.Status,
                            callsignName = s.Callsign != null ? s.Callsign.Name : "",
                            durationHours = DateTimeHelper.CalculateDisplayDuration(s.ShiftStart, s.ShiftEnd),
                            sellRate = s.PayRate != null ? s.PayRate.SellRateToClient : 0,
                            buyRate = s.PayRate != null ? s.PayRate.GuardPayRate : 0
                        })
                        .ToList<object>();
                    days.Add(dayShifts);
                }

                // 4. Fetch Holidays logic (web parity)
                var holidays = await _context.BroadcastBannerCalendarEvents
                    .Where(x => x.IsPublicHoliday && (x.RepeatYearly || (x.ExpiryDate >= startDate && x.StartDate <= totalEndDate)))
                    .Select(x => new
                    {
                        x.id,
                        x.StartDate,
                        x.ExpiryDate,
                        x.RepeatYearly,
                        Reason = x.TextMessage,
                        States = _context.PublicHolidayStates
                            .Where(s => s.CalendarEventId == x.id && !s.IsDeleted)
                            .Select(s => s.State)
                            .ToList()
                    })
                    .ToListAsync();

                // 5. Fetch Roster Status
                var statusObj = await _context.RosterSiteWeekStatuses
                    .FirstOrDefaultAsync(x => x.ClientSiteId == siteId && x.StartDate == startDate);
                var status = statusObj?.Status ?? (schedules.Any() ? "Live" : "");

                return Ok(new
                {
                    startDate = startDate.ToString("yyyy-MM-dd"),
                    endDate = startDate.AddDays(6).ToString("yyyy-MM-dd"),
                    siteName = site?.Name ?? "Unknown Site",
                    clientTypeName = site?.ClientType?.Name ?? "Security Service",
                    siteState = site?.State,
                    status = status,
                    holidays = holidays,
                    days = days
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching roster", error = ex.Message });
            }
        }

        [HttpPost("UpdateShiftStatus")]
        /* 
         * GUARD ROSTER FLOW (Web & Mobile Sync):
         * 1. Orange (Pushed/Pending) -> Green (Accepted) [One-click accept]
         * 2. Green (Accepted) -> Black (Declined) [Requires Reason, stays with original guard]
         * 3. Black (Declined) -> OWNER clicks -> Green (Accepted) [Re-Acceptance]
         * 4. Black (Declined) -> OTHER clicks -> Purple (Relief) [Relief Assignment]
         */
        public async Task<IActionResult> UpdateShiftStatus([FromBody] RosterStatusUpdateModel model)
        {
            try
            {
                // 1. Fetch the shift record from the database
                var shift = await _context.RosterSchedules.FindAsync(model.ShiftId);
                if (shift == null) return NotFound(new { isSuccess = false, message = "Shift not found." });

                // 2. Concurrency and Status checks
                if (shift.Status == RosterShiftStatus.Cancelled || shift.Status == RosterShiftStatus.Missed)
                {
                    return BadRequest(new { isSuccess = false, message = "This shift is finalized and cannot be modified." });
                }

                if (shift.Status != model.ExpectedStatus)
                {
                    return BadRequest(new { isSuccess = false, message = "Shift status has changed. Please refresh the roster." });
                }

                int oldStatus = (int)shift.Status;
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                string platform = Request.Headers["User-Agent"].ToString();

                // 3. Process status update to 'Accepted'
                if (shift.ShiftStart.Date < DateTime.Today)
                {
                    return BadRequest(new { isSuccess = false, message = "You cannot accept or decline shifts from previous days." });
                }

                if (model.NewStatus == RosterShiftStatus.Accepted)
                {
                    if (model.CallingGuardId <= 0)
                    {
                        return BadRequest(new { isSuccess = false, message = "Invalid Guard ID." });
                    }

                    // Primary guard accepting their own shift
                    if (shift.GuardId == model.CallingGuardId)
                    {
                        if (shift.ReliefGuardId.HasValue && shift.ReliefGuardId > 0)
                        {
                            return BadRequest(new { isSuccess = false, message = "This shift is already assigned to a relief guard." });
                        }
                        shift.Status = RosterShiftStatus.Accepted;
                    }
                    // Picking up a declined shift as a Relief Guard
                    else if (shift.Status == RosterShiftStatus.Declined)
                    {
                        // Conflict Validation for Relief Guard
                        var conflict = await _context.RosterSchedules
                            .Include(s => s.ClientSite)
                            .FirstOrDefaultAsync(s => s.Id != shift.Id && !s.IsDeleted &&
                                                      ((s.GuardId == model.CallingGuardId && (s.ReliefGuardId == null || s.ReliefGuardId <= 0)) || s.ReliefGuardId == model.CallingGuardId) &&
                                                      s.ShiftStart < shift.ShiftEnd && s.ShiftEnd > shift.ShiftStart);

                        if (conflict != null)
                        {
                            var siteName = conflict.ClientSite?.Name ?? "another site";
                            var conflictStart = conflict.ShiftStart.ToString("HH:mm");
                            var conflictEnd = conflict.ShiftEnd.ToString("HH:mm");
                            return BadRequest(new { isSuccess = false, message = $"Conflict: You are currently assigned to {siteName} from {conflictStart} to {conflictEnd}." });
                        }

                        shift.ReliefGuardId = model.CallingGuardId;
                        shift.Status = RosterShiftStatus.Accepted;

                        var callingGuard = await _context.Guards.FindAsync(model.CallingGuardId);
                        if (callingGuard != null)
                        {
                            shift.ReliefProviderName = callingGuard.Provider;
                        }

                        // Keep the existing ReliefReason (the reason for cancellation)
                        // but we could append that it was picked up via mobile
                        if (string.IsNullOrEmpty(shift.ReliefReason))
                        {
                            shift.ReliefReason = "Relief Guard assigned via Mobile";
                        }
                    }
                    else
                    {
                        return BadRequest(new { isSuccess = false, message = "You are not authorized to accept this shift." });
                    }
                }
                // 4. Process status update to 'Declined'
                else if (model.NewStatus == RosterShiftStatus.Declined)
                {
                    bool canDecline = false;
                    string unauthorizedMessage = "You are not authorized to decline this shift.";

                    if (shift.ReliefGuardId.HasValue && shift.ReliefGuardId > 0)
                    {
                        // If a relief guard is assigned, ONLY the relief guard can decline it
                        canDecline = (shift.ReliefGuardId == model.CallingGuardId);
                        if (!canDecline)
                        {
                            var reliefGuard = await _context.Guards.FindAsync(shift.ReliefGuardId.Value);
                            string rName = reliefGuard != null ? reliefGuard.Name : "the relief guard";
                            unauthorizedMessage = $"You cannot modify this. Only {rName} can modify this.";
                        }
                    }
                    else
                    {
                        // No relief guard assigned, so only the original assigned guard can decline
                        canDecline = (shift.GuardId == model.CallingGuardId);
                    }

                    if (canDecline)
                    {
                        shift.Status = RosterShiftStatus.Declined;
                        shift.ReliefReason = model.Reason; // Save the guard's reason for cancellation

                        // If the cancelling guard is the relief guard, clear the relief guard details
                        // so the shift becomes open for other guards to accept.
                        if (shift.ReliefGuardId.HasValue && shift.ReliefGuardId == model.CallingGuardId)
                        {
                            shift.ReliefGuardId = null;
                            shift.ReliefProviderName = null;
                        }
                    }
                    else
                    {
                        return BadRequest(new { isSuccess = false, message = unauthorizedMessage });
                    }
                }

                // 5. Save the updated status and reason to DB
                await _context.SaveChangesAsync();

                // 5b. Queue email alert for cancellation
                if (model.NewStatus == RosterShiftStatus.Declined)
                {
                    try
                    {
                        await _alertEmailServices.QueueMobileShiftCancellation(shift, model.Reason);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to queue shift cancellation email: {ex.Message}");
                    }
                }
                else
                {
                    try { await _alertEmailServices.RemoveFromQueue(shift); } catch { }
                }

                // 6. Separately try to log the audit entry
                try
                {
                    string details = "";
                    string action = "";
                    if (model.NewStatus == RosterShiftStatus.Accepted)
                    {
                        action = "Accepted";
                        details = shift.ReliefGuardId == model.CallingGuardId ? "Relief Guard picked up the declined shift." : "Primary guard accepted the shift.";
                    }
                    else if (model.NewStatus == RosterShiftStatus.Declined)
                    {
                        action = "Declined";
                        details = $"Guard declined shift with reason: {model.Reason}";
                        if (shift.ReliefGuardId == null && model.NewStatus == RosterShiftStatus.Declined && oldStatus == (int)RosterShiftStatus.Accepted)
                        {
                            // This is a bit tricky to detect after save, but we can infer
                        }
                    }

                    if (!string.IsNullOrEmpty(action))
                    {
                        _context.RosterScheduleAuditLogs.Add(new RosterScheduleAuditLog
                        {
                            RosterScheduleId = shift.Id,
                            ActionTime = DateTime.Now,
                            GuardId = model.CallingGuardId,
                            ActionSource = "Mobile",
                            Action = action,
                            Details = details,
                            IPAddress = ipAddress,
                            Platform = platform,
                            OldStatus = oldStatus,
                            NewStatus = (int)shift.Status
                        });
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    // Ignore audit logging errors
                }

                // 7. Notify SignalR (this ensures BOTH web and mobile listen and reload)
                try
                {
                    // Broadcast to Web (UpdateHub)
                    await _webHubContext.Clients.All.SendAsync("UpdateRoster", new { shiftId = shift.Id, siteId = shift.ClientSiteId });
                    await _webHubContext.Clients.All.SendAsync("RefreshRoster", shift.GuardId?.ToString()); // Force a refresh like the web code does

                    // Broadcast to Mobile (MobileAppSignalRHub)
                    await _mobileHubContext.Clients.All.SendAsync("RefreshRoster", new { siteId = shift.ClientSiteId });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SignalR Broadcast failed: {ex.Message}");
                }

                return Ok(new { isSuccess = true, message = "Shift status updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = ex.Message });
            }
        }



        private bool SaveOfflineFilesRecordsError(OfflineFilesRecords _oR, string syncError)
        {
            bool IsSuccess = false;
            OfflineFilesRecordsNotSynced _offlineFilesRecordsNotSynced = new OfflineFilesRecordsNotSynced()
            {
                Id = _oR.Id,
                RecordLabel = _oR.RecordLabel,
                FileNameActual = _oR.FileNameActual,
                FileNameCache = _oR.FileNameCache,
                FileNameWithPathCache = _oR.FileNameWithPathCache,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                IsSynced = _oR.IsSynced,
                UniqueRecordId = _oR.UniqueRecordId,
                FileType = _oR.FileType,
                IsNew = _oR.IsNew,
                LogBookId = _oR.LogBookId,
                guardId = _oR.guardId,
                clientsiteId = _oR.clientsiteId,
                userId = _oR.userId,
                gps = _oR.gps,
                FileGroupId = _oR.FileGroupId,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                SyncTime = DateTime.Now,
                NotSyncError = syncError,
                IsEntryByPCAR = _oR.IsEntryByPCAR,
                LogbookclientsiteId = _oR.LogbookclientsiteId,
                CallSignId = _oR.CallSignId,
                PositionId = _oR.PositionId
            };

            try
            {
                IsSuccess = _guardLogDataProvider.SaveOfflineFileRecordError(_offlineFilesRecordsNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

        private bool SaveSyncOfflinePostActivityLogDataError(PostActivityRequestLocalCacheOffline _oR, string syncError)
        {
            bool IsSuccess = false;
            PostActivityRequestLocalCacheOfflineNotSynced _offlineRecordNotSynced = new PostActivityRequestLocalCacheOfflineNotSynced()
            {
                Id = _oR.Id,
                guardId = _oR.guardId,
                clientsiteId = _oR.clientsiteId,
                userId = _oR.userId,
                activityString = _oR.activityString,
                gps = _oR.gps,
                systemEntry = _oR.systemEntry,
                scanningType = _oR.scanningType,
                tagUID = _oR.tagUID,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                IsNewGuard = _oR.IsNewGuard,
                IsSynced = _oR.IsSynced,
                UniqueRecordId = _oR.UniqueRecordId,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                SyncTime = DateTime.Now,
                NotSyncError = syncError,
                LogbookclientsiteId = _oR.LogbookclientsiteId,
                IsEntryByPCAR = _oR.IsEntryByPCAR,
                CallSignId = _oR.CallSignId,
                PositionId = _oR.PositionId
            };

            try
            {
                IsSuccess = _guardLogDataProvider.SaveOfflinePostActivityLogDataError(_offlineRecordNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

        private bool SaveSyncOfflinePatrolCarLogDataError(PatrolCarLogRequestLocalCacheOffline _oR, string syncError)
        {
            bool IsSuccess = false;
            PatrolCarLogRequestLocalCacheOfflineNotSynced _offlineRecordNotSynced = new PatrolCarLogRequestLocalCacheOfflineNotSynced()
            {
                CacheId = _oR.CacheId,
                SiteId = _oR.SiteId,
                Id = _oR.Id,
                ClientSiteLogBookId = _oR.ClientSiteLogBookId,
                Mileage = _oR.Mileage,
                MileageText = _oR.MileageText,
                PatrolCar = _oR.PatrolCar,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                IsSynced = _oR.IsSynced,
                UniqueRecordId = _oR.UniqueRecordId,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                PatrolCarId = _oR.PatrolCarId,
                Model = _oR.Model,
                Rego = _oR.Rego,
                ClientSiteId = _oR.ClientSiteId,
                SyncTime = DateTime.Now,
                NotSyncError = syncError
            };

            try
            {
                IsSuccess = _guardLogDataProvider.SaveOfflinePatrolCarLogDataError(_offlineRecordNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

        private bool SaveSyncOfflineCustomFieldLogDataError(CustomFieldLogRequestHeadLocalCacheOffline _oR, string syncError)
        {
            bool IsSuccess = false;


            List<CustomFieldLogRequestDetailCacheOfflineNotSynced> customFieldLogRequestDetail = new List<CustomFieldLogRequestDetailCacheOfflineNotSynced>();

            foreach (var detail in _oR.Details)
            {
                customFieldLogRequestDetail.Add(new CustomFieldLogRequestDetailCacheOfflineNotSynced()
                {
                    Id = detail.Id,
                    HeadId = detail.HeadId,
                    DictKey = detail.DictKey,
                    DictValue = detail.DictValue
                });
            }

            CustomFieldLogRequestHeadLocalCacheOfflineNotSynced _offlineRecordNotSynced = new CustomFieldLogRequestHeadLocalCacheOfflineNotSynced()
            {
                Id = _oR.Id,
                SiteId = _oR.SiteId,
                Details = customFieldLogRequestDetail,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                IsSynced = _oR.IsSynced,
                UniqueRecordId = _oR.UniqueRecordId,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                SyncTime = DateTime.Now,
                NotSyncError = syncError
            };

            try
            {
                IsSuccess = _guardLogDataProvider.SaveSyncOfflineCustomFieldLogDataError(_offlineRecordNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

        private bool SaveSyncofflineIrAttachmentRecordsDataError(irOfflineFilesAttachmentsCache _oR, string syncError)
        {
            bool IsSuccess = false;


            irOfflineFilesAttachmentsCacheNotSynced _offlineRecordNotSynced = new irOfflineFilesAttachmentsCacheNotSynced()
            {
                UniqueRecordId = _oR.UniqueRecordId,
                IrId = _oR.IrId,
                FileNameActual = _oR.FileNameActual,
                FileNameCache = _oR.FileNameCache,
                FileNameWithPathCache = _oR.FileNameWithPathCache,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                IsSynced = _oR.IsSynced,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                SyncTime = DateTime.Now,
                NotSyncError = syncError
            };

            try
            {
                IsSuccess = _guardLogDataProvider.SaveSyncIrOfflineFilesAttachmentsCacheNotSyncedDataError(_offlineRecordNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

        private bool SaveSyncofflineIrRecordsDataError(irOfflineCache _oR, string syncError)
        {
            bool IsSuccess = false;
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                IncludeFields = true
            };

            string _incidentRequest = JsonSerializer.Serialize(_oR, options);
            irOfflineCacheNotSynced _offlineRecordNotSynced = new irOfflineCacheNotSynced()
            {
                IrId = _oR.IrId,
                IncidentRequest = _incidentRequest,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                IsSynced = _oR.IsSynced,
                UniqueRecordId = _oR.UniqueRecordId,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                SyncTime = DateTime.Now,
                NotSyncError = syncError
            };

            try
            {
                IsSuccess = _guardLogDataProvider.SaveSyncIrOfflineCacheNotSyncedDataError(_offlineRecordNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

        private async Task<(bool rtn, string msg, string _filename)> UploadIrFilesAndReturnName(string reportReference, IFormFile file)
        {
            bool rtn = false;
            string msg = "";
            string _filename = "";
            if (file == null || file.Length == 0)
            {
                msg = "No file provided.";
                return (rtn, msg, _filename);
            }


            if (string.IsNullOrEmpty(reportReference))
            {
                msg = "Missing report reference.";
                return (rtn, msg, _filename);
            }


            var uploadFileName = Path.GetFileName(file.FileName);
            var folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "Uploads", reportReference);

            try
            {
                Directory.CreateDirectory(folderPath);

                var fullFilePath = Path.Combine(folderPath, uploadFileName);

                using (var stream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Handle HEIC conversion
                if (Path.GetExtension(uploadFileName).Equals(".heic", StringComparison.OrdinalIgnoreCase))
                {
                    var jpgPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(uploadFileName) + ".jpg");

                    // Optional: implement HEIC-to-JPG conversion (ImageMagick or Magick.NET)
                    await ConvertHeicToJpgAsync(fullFilePath, jpgPath);

                    System.IO.File.Delete(fullFilePath);
                    uploadFileName = Path.GetFileName(jpgPath);
                }

                rtn = true;
                _filename = uploadFileName;
                return (rtn, msg, _filename);

            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return (rtn, msg, _filename);
            }
        }

        private List<DropdownItem> GetUserClientTypesWithId(int userId, int? clientTypeId = null)
        {
            try
            {
                var clientTypes = _viewDataService.GetUserClientTypesWithId(userId);
                return clientTypes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserClientTypesWithId: {ex.Message}");
                return new List<DropdownItem>();
            }
        }

        private List<Data.Providers.FeedbackTemplateViewModel> GetAndReturnFeedbackTemplates()
        {
            var result = _guardLogDataProvider.GetFeedbackTemplates();
            return result;
        }

        private List<string> GetNotifiedReportFieldsByType()
        {
            var notifiedBy = _configDataProvider.GetReportFieldsByType(ReportFieldType.NotifiedBy);

            // Extract just the names
            var result = notifiedBy
                .Select(item => item.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            return result;
        }

        /// <summary>
        /// Retrieves client sites for the Incident Report module.
        /// [Optimization]: Uses partial name matching (.Contains) and targeted projection.
        /// </summary>
        private List<ClientSiteDto> GetClientSitesForIR(string sitename = "")
        {
            // [Optimization]: We fetch the materialized list and convert to DTOs.
            // Further optimization is applied at the DataProvider level.
            var query = _clientDataProvider.GetClientSites(null).AsQueryable();

            // Apply filter only when sitename is provided (Flexible search)
            if (!string.IsNullOrWhiteSpace(sitename))
            {
                query = query.Where(x => x.Name.Contains(sitename));
            }

            var clientSiteDtos = query
                .Select(site => new ClientSiteDto
                {
                    Id = site.Id,
                    TypeId = site.TypeId,
                    Name = site.Name,
                    Address = site.Address,
                    State = site.State,
                    Gps = site.Gps,
                    Billing = site.Billing,
                    Status = site.Status,
                    StatusDate = site.StatusDate,
                    SiteEmail = site.SiteEmail,
                    LandLine = site.LandLine,
                    DuressEmail = site.DuressEmail,
                    DuressSms = site.DuressSms,
                    UploadGuardLog = site.UploadGuardLog,
                    UploadFusionLog = site.UploadFusionLog,
                    GuardLogEmailTo = site.GuardLogEmailTo,
                    DataCollectionEnabled = site.DataCollectionEnabled,
                    IsActive = site.IsActive,
                    IsDosDontList = site.IsDosDontList,
                    MobAppShowClientTypeandSite = site.MobAppShowClientTypeandSite
                })
                .ToList();

            return clientSiteDtos;
        }

        private List<SelectListItem> GetClientSiteArea(int _ClientSiteId = -1)
        {
            var items = new List<SelectListItem>();

            if (_ClientSiteId > 0)
            {
                items.Add(new SelectListItem("Select", "", true));
            }
            var clientArea = _configDataProvider.GetReportFieldsByType(ReportFieldType.ClientArea);
            foreach (var item in clientArea)
            {
                if (!String.IsNullOrEmpty(item.ClientSiteIds))
                {
                    foreach (var clientsiteid in item.ClientSiteIdsNew)
                    {
                        if (clientsiteid.Equals(_ClientSiteId))
                        {
                            items.Add(new SelectListItem(item.Name, item.Name));
                        }
                        else if (_ClientSiteId == -1)
                        {
                            items.Add(new SelectListItem(item.Name, clientsiteid.ToString()));
                        }
                    }
                }
                else
                {
                    if (_ClientSiteId == -1)
                    {
                        items.Add(new SelectListItem(item.Name, "-1"));
                    }
                    else
                    {
                        items.Add(new SelectListItem(item.Name, item.Name));
                    }
                }
            }

            return items;
        }

        private List<Mp3File> GetAudioForMobileApp(int type)
        {
            var activity = _viewDataService.GetDressAppFieldsAudio(type);
            return activity;
        }

        [HttpGet("GetCallsigns")]
        public IActionResult GetCallsigns()
        {
            try
            {
                var callsigns = _configDataProvider.GetReportFieldsByType(ReportFieldType.CallSign)
                    .Select(x => new { Id = x.Id, Name = x.Name })
                    .ToList();
                return Ok(callsigns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // [FIX]: Added endpoint for Mobile App to retrieve Officer Positions
        [HttpGet("GetOfficerPositions")]
        public IActionResult GetOfficerPositions([FromQuery] bool isPatrolCar = false)
        {
            try
            {
                var filter = isPatrolCar ? CityWatch.Web.Services.OfficerPositionFilter.PatrolOnly : CityWatch.Web.Services.OfficerPositionFilter.NonPatrolOnly;
                var positions = _viewDataService.GetOfficerPositionsNew(filter)
                    .Select(x => new { Id = int.TryParse(x.Value, out int id) ? id : 0, Name = x.Text })
                    .ToList();
                return Ok(positions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }



    public class VisitSaveDto
    {
        public int SmartWandId { get; set; }
        public int SiteId { get; set; }
        public string DayName { get; set; }

        public int PcarRouteId { get; set; }
        public int PcarRouteDetailsId { get; set; }

        public string VisitName { get; set; }
        public int VisitNumber { get; set; }

        public int GuardId { get; set; }

        // NEW FIELDS
        public string GpsCoordinates { get; set; }
        public int LoginUserId { get; set; }
        public int LoginSiteId { get; set; }

        public string TimeOn { get; set; }
        public string TimeOff { get; set; }
    }



    public class SiteTagStatusPending
    {

        public string LabelDescription { get; set; }   // Tag label / description
        public string TagType { get; set; }            // NFC, BLE, Other
        public int RoundNumber { get; set; }           // Round number
        public int TodayScanCount { get; set; }             // How many times scanned today

    }
    public class SiteTagStatus
    {
        public int ClientSiteId { get; set; }
        public int TotalTags { get; set; }
        public int ScannedTags { get; set; }
        public int RemainingTags { get; set; }
        public int CompletedRounds { get; set; }
        public string Tour { get; set; }
    }
    public class AreaDto
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
    }


    // API Project - DTO
    public class ClientSiteDto
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string Gps { get; set; }
        public string Billing { get; set; }
        public int Status { get; set; }
        public DateTime? StatusDate { get; set; }
        public string SiteEmail { get; set; }
        public string LandLine { get; set; }
        public string DuressEmail { get; set; }
        public string DuressSms { get; set; }
        public bool UploadGuardLog { get; set; }
        public bool UploadFusionLog { get; set; }
        public string GuardLogEmailTo { get; set; }
        public bool DataCollectionEnabled { get; set; }
        public bool IsActive { get; set; }
        public bool IsDosDontList { get; set; }

        public bool MobAppShowClientTypeandSite { get; set; }

    }


    public class GuardLogDto
    {
        public int Id { get; set; }
        public DateTime EventDateTime { get; set; }
        public string EventDateTimeLocal { get; set; } // For frontend use
        public string EventDateTimeZoneShort { get; set; } // For frontend use

        public string Notes { get; set; }
        public List<string> ImageUrls { get; set; }
        public string GuardInitials { get; set; }
        public int IrEntryType { get; set; }
        public bool IsSystemEntry { get; set; }

        public int? rcPushMessageId { get; set; }
    }
    public class DuressRequest
    {
        public int ClientSiteId { get; set; }
        public int GuardId { get; set; }
        public int GuardLoginId { get; set; }
        public int LogBookId { get; set; }
        public string GpsCoordinates { get; set; } = string.Empty;
    }


    public class GeocodeResult
    {
        public string formatted_address { get; set; }
    }

    public class GoogleGeocodeResponse
    {
        public string status { get; set; }
        public List<GeocodeResult> results { get; set; }
    }

    public class FeedbackTemplateViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Text { get; set; }
        public int? Type { get; set; }
        public string FeedbackTypeName { get; set; }
        public string BackgroundColour { get; set; }
        public string TextColor { get; set; }
        public int DeleteStatus { get; set; }
        public bool SendtoRC { get; set; }
    }

    public class GoogleGeocodeResponse2
    {
        public string status { get; set; }
        public List<Result> results { get; set; }
    }

    public class Result
    {
        public Geometry geometry { get; set; }
        public string formatted_address { get; set; }
    }

    public class Geometry
    {
        public Location location { get; set; }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class NewGuard
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SecurityNo { get; set; }
        public string Initial { get; set; }
        public string Gender { get; set; }
        public string State { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public bool IsLB_KV_IR { get; set; }
        public bool IsMobileAppAccess { get; set; }
    }

    public class GuardComplianceAndLicenseDTO
    {
        public int Id { get; set; }
        public int GuardId { get; set; }
        public string Description { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }

        // Added for Graceful Migration
        public int? HrSettingsId { get; set; }

        public HrGroup? HrGroup { get; set; }
        public string HrGroupText { get; set; }

        //[ForeignKey("GuardId")]
        //public Guard Guard { get; set; }

        public string CurrentDateTime { get; set; }
        public int Reminder1 { get; set; }
        public int Reminder2 { get; set; }
        public string LicenseNo { get; set; }
        public bool DateType { get; set; }
        public bool IsDateFilterEnabledHidden { get; set; }
        public bool HRBanEdit { get; set; }
        public string IsLogin { get; set; }
        public int MasterDateType { get; set; }
        public string StatusColor { get; set; }

    }

}

