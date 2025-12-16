using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Pages.Incident;
using CityWatch.Web.Services;
using ConvertApiDotNet;
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
using System.Threading.Tasks;
using System.Web;
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;
using static Dropbox.Api.TeamLog.SpaceCapsType;


namespace CityWatch.Web.API
{


    [Route("api/[controller]")]
    [ApiController]
    public class GuardSecurityNumberController : ControllerBase
    {
        //public IncidentRequest Report { get; set; }
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
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        private readonly IIrDataProvider _irDataProvider;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IUserDataProvider _userDataProvider;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly IUserAuthenticationService _userAuthentication;
        const string LAST_USED_IR_SEQ_NO_CONFIG_NAME = "LastUsedIrSn";


        public GuardSecurityNumberController(IGuardDataProvider guardDataProvider, IViewDataService viewDataService,
            ILogbookDataService logbookDataService, IGuardLogDataProvider guardLogDataProvider,
            IClientDataProvider clientDataProvider, ISiteEventLogDataProvider siteEventLogDataProvider,
            IWebHostEnvironment webHostEnvironment, ISmsSenderProvider smsSenderProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration, IConfigDataProvider configDataProvider, IIrDataProvider irDataProvider,
            ILogger<RegisterModel> logger, IUserDataProvider userDataProvider, IIncidentReportGenerator incidentReportGenerator,
            IAppConfigurationProvider appConfigurationProvider, IUserAuthenticationService userAuthentication,
            IMobileAppDataServices mobileAppDataServices)
        {
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
        }

        [HttpGet("GetGuardDetails/{securityNumber}")]
        public IActionResult GetGuardDetails(string securityNumber)
        {
            if (string.IsNullOrWhiteSpace(securityNumber))
                return BadRequest(new { message = "Security number is required." });

            var guard = _guardDataProvider.GetGuards()
                .SingleOrDefault(z => string.Compare(z.SecurityNo, securityNumber, StringComparison.OrdinalIgnoreCase) == 0);

            if (guard == null)
            {
                //return NotFound(new
                //{
                //    message = "User not found. Please check if input is correct. If you are a new, Please click Register.",
                //    isActive = false
                //});
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
                                              HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }

                        // Set HR2Status
                        var HR2List = statusLookup["HR 2 (Client)"];
                        if (HR2List.Any())
                        {
                            HR2 = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                              HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                              "Green";
                        }

                        // Set HR3Status
                        var HR3List = statusLookup["HR 3 (Special)"];
                        if (HR3List.Any())
                        {
                            HR3 = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
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
                GuardLockStatusBasedOnRedDoc = guardLockStatusBasedOnRedDoc
            });
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
                var firstItem = selectedList.FirstOrDefault(x => x.ExpiryDate != null);

                if (firstItem != null)
                {
                    var expiryDate = firstItem.ExpiryDate.Value; // Assuming ExpiryDate is not null here

                    // Compare expiry date with today's date
                    if (expiryDate < today)
                    {
                        return "Red";
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
                var clientTypes = _viewDataService.GetUserClientTypesWithId(userId);

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
                    GpsCoordinates = gpsCoordinates
                };

                _guardLogDataProvider.SaveGuardLog(signInEntry);

                var clientsiteDetails = _clientDataProvider.GetClientSiteDetailsWithId(request.clientsiteId).FirstOrDefault();

                return Ok(new { message = "Guard successfully logged in.", guardLoginId, TourMode = (int)clientsiteDetails.PatrolTourMode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }

        }

        //[HttpGet("EnterGuardLogin")]
        //public IActionResult EnterGuardLogin(int guardId, int clientsiteId, int userId, string gps)
        //{
        //    try
        //    {

        //        if (guardId <= 0 || clientsiteId <= 0)
        //            return BadRequest(new { message = "Invalid guard ID or client site ID." });

        //        var logBookType = LogBookType.DailyGuardLog;
        //        var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType);

        //        if (logBookId <= 0)
        //            return BadRequest(new { message = "Failed to retrieve logbook ID." });

        //        // Get Guard Login ID
        //        var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        //        var guardLoginId = _mobileAppDataServices.GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);

        //        if (guardLoginId <= 0)
        //            return BadRequest(new { message = "Guard login failed." });

        //        // Default GPS coordinates (should be replaced with actual values if available)
        //        var gpsCoordinates = gps;

        //        // Create a log entry
        //        var signInEntry = new GuardLog
        //        {
        //            ClientSiteLogBookId = logBookId,
        //            GuardLoginId = guardLoginId,
        //            EventDateTime = DateTime.Now,
        //            Notes = "Logbook Logged In (Mob App)",
        //            IsSystemEntry = true,
        //            EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
        //            EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
        //            EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
        //            EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
        //            EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
        //            GpsCoordinates = gpsCoordinates
        //        };

        //        _guardLogDataProvider.SaveGuardLog(signInEntry);

        //        var clientsiteDetails = _clientDataProvider.GetClientSiteDetailsWithId(clientsiteId).FirstOrDefault();

        //        return Ok(new { message = "Guard successfully logged in.", guardLoginId, TourMode = (int)clientsiteDetails.PatrolTourMode });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        //    }

        //}


        [HttpGet("GetActivities")]
        public IActionResult GetActivities([FromQuery] int type, [FromQuery] int? siteid = 0)
        {
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
                var activity = _viewDataService.GetDressAppFieldsAudio(type);

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


            void AddIfValid(string email)
            {
                if (!string.IsNullOrWhiteSpace(email) &&
                    MailboxAddress.TryParse(email.Trim(), out var mailbox))
                {
                    emailAddressList.Add(mailbox);
                }
            }

            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.Position, Report?.Officer?.Position));
            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.NotifiedBy, Report?.Officer?.NotifiedBy));
            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.CallSign, Report?.Officer?.CallSign));
            AddIfValid(GetFieldEmailAddress(fields, ReportFieldType.ClientArea, Report?.DateLocation?.ClientArea));

            return emailAddressList;
        }

        private static string GetFieldEmailAddress(List<IncidentReportField> fields, ReportFieldType type, string fieldValue)
        {
            return fields.SingleOrDefault(x => x.TypeId == type && x.Name == fieldValue)?.EmailTo;
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
            var result = _guardLogDataProvider.GetFeedbackTemplates();
            return Ok(result);
        }





        [HttpPost("ProcessIrSubmit")]
        public IActionResult ProcessIrSubmit([FromQuery] string gps, [FromQuery] int UserId, [FromQuery] int IRguardId, [FromQuery] int IRclientSiteId, [FromBody] IncidentRequest Report)
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
                Position = string.Empty,
                Email = GuardDetails.Email,
                LicenseNumber = GuardDetails.SecurityNo,
                LicenseState = GuardDetails.State,
                CallSign = string.Empty,
                Billing = string.Empty,
                GuardMonth = Report.Officer.GuardMonth,
                NotifiedBy = Report.Officer.NotifiedBy
            };
            /* specific for mobile app Android*/
            Report.WebVersion = false;
            Report.Android = true;
            Report.iOS = false;


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

            var clientSitePosition = _clientDataProvider.GetClientSitePosition(Report.Officer.Position);


            //live map settings 
            if (Report?.DateLocation?.ShowIncidentLocationAddress == true &&
  !string.IsNullOrWhiteSpace(Report.DateLocation.ClientAddress))
            {
                var result = GetCoordinatesFromAddress(Report.DateLocation.ClientAddress);
                Report.DateLocation.ClientSiteLiveGps = result.Latitude + "," + result.Longitude;
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
                IsPatrol = Report?.IsPositionPatrolCar ?? false,
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

                CreatedOnDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                CreatedOnDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                CreatedOnDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                CreatedOnDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                CreatedOnDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                HASH = hashCode,
                ClientSitePositionId = clientSitePosition?.ClientsiteId,
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
                    CreateControlRoomLogEntry(report);//To Save in the control room
                    if (report.ClientSitePositionId.HasValue)
                    {
                        CreatePositionGuardLogEntry(report);
                    }


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

            return Ok(new
            {
                Success = processResult.Count == 0,
                FileName = fileName,
                Domin = domain?.Domain ?? string.Empty,
                Errors = processResult.Select(p => new { Code = p.Key, Message = p.Value.ErrorMessage })
            });
        }


        private void CreatePositionGuardLogEntry(IncidentReport report)
        {
            // p6#73 timezone bug - Added by binoy 24-01-2024
            var logBookId = GetLogBookId(report.ClientSitePositionId.Value, (int)report.CreatedOnDateTimeUtcOffsetMinute);
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
                IsIRReportTypeEntry = true
            };
            _guardLogDataProvider.SaveGuardLog(guardLog);
        }
        private void CreateControlRoomLogEntry(IncidentReport report)
        {
            var RadioCheckDetails = _guardLogDataProvider.GetRadiocheckLogbookDetails();
            // p6#73 timezone bug - Added by binoy 24-01-2024
            var logBookId = GetLogBookId(RadioCheckDetails.ClientSiteId, (int)report.CreatedOnDateTimeUtcOffsetMinute);
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
                        RcLogbookStamp = StampRcLogbook
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
                                RcLogbookStamp = StampRcLogbook
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

            /* Mail Id added Bcc globoconsoftware for checking Ir Mail not getting Issue Start(date 13,09,2023) */
            //message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "addileepsebastian@gmail.com"));
            // message.Bcc.Add(new MailboxAddress("globoconsoftware", "jishakallani@gmail.com"));
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
            var site = _clientDataProvider
                .GetClientSites(null)
                .FirstOrDefault(x => x.Name == name);

            if (site == null)
                return NotFound();

            var dto = new ClientSiteDto
            {
                Id = site.Id,
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

            return Ok(dto);
        }





        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile([FromQuery] string reportReference, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file provided." });

            if (string.IsNullOrEmpty(reportReference))
                return BadRequest(new { success = false, message = "Missing report reference." });

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

                return Ok(new
                {
                    success = true,
                    fileName = uploadFileName
                });
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


            var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            var clientArea = _configDataProvider.GetReportFieldsByType(ReportFieldType.ClientArea);
            foreach (var item in clientArea)
            {
                if (!String.IsNullOrEmpty(item.ClientSiteIds))
                {
                    foreach (var clientsiteid in item.ClientSiteIdsNew)
                    {
                        if (clientsiteid.Equals(clientSiteId))
                        {
                            items.Add(new SelectListItem(item.Name, item.Name));
                        }
                    }
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
            }


            return Ok(items);
        }

        [HttpGet("GetNotifiedByList")]
        public IActionResult GetNotifiedByList()
        {
            var notifiedBy = _configDataProvider.GetReportFieldsByType(ReportFieldType.NotifiedBy);

            // Extract just the names
            var result = notifiedBy
                .Select(item => item.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            return Ok(result);
        }


        [HttpPost("UploadMultiple")]
        public async Task<IActionResult> UploadMultiple(
    [FromForm] List<IFormFile> files,
    [FromForm] List<string> types,   // <-- multiple types aligned with files
    [FromForm] int guardId,
    [FromForm] int clientsiteId,
    [FromForm] int userId,
    [FromForm] string gps
    )
        {
            bool success = false;
            string message = "Uploaded successfully";
            var uploadedFiles = new List<string>();

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


                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    /*your message */
                    Notes = "Mob app image upload",
                    IsSystemEntry = false,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gpsCoordinates
                };

                int GuardLogId = _guardLogDataProvider.SaveGuardLogandReturnId(signInEntry);

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

                    using (var stream = System.IO.File.Create(fullPath))
                        await file.CopyToAsync(stream);

                    var finalFileName = uploadFileName;

                    // HEIC conversion
                    if (ext == ".heic")
                    {
                        var newPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                        await ConvertHeicToJpgAsync(fullPath, folderPath);
                        System.IO.File.Delete(fullPath);
                        finalFileName = Path.GetFileName(newPath);
                    }

                    var publicPath = $"https://cws-ir.com/DglUploads/{GuardLogId}/{folderName}/{finalFileName}";

                    var logImage = new GuardLogsDocumentImages
                    {
                        GuardLogId = GuardLogId,
                        ImagePath = publicPath,
                        IsRearfile = type?.ToLower() == "rear",
                        IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                    };

                    _guardLogDataProvider.SaveGuardLogDocumentImages(logImage);

                    uploadedFiles.Add(publicPath);
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

                    using (var stream = System.IO.File.Create(fullPath))
                        await file.CopyToAsync(stream);

                    var finalFileName = uploadFileName;

                    // HEIC conversion
                    if (ext == ".heic")
                    {
                        var newPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ".jpg");
                        await ConvertHeicToJpgAsync(fullPath, folderPath);
                        System.IO.File.Delete(fullPath);
                        finalFileName = Path.GetFileName(newPath);
                    }

                    var publicPath = $"https://cws-ir.com/DglUploads/{GuardLogId}/{folderName}/{finalFileName}";

                    var logImage = new GuardLogsDocumentImages
                    {
                        GuardLogId = GuardLogId,
                        ImagePath = publicPath,
                        IsRearfile = type?.ToLower() == "rear",
                        IsTwentyfivePercentfile = type?.ToLower() == "twentyfive"
                    };

                    _guardLogDataProvider.SaveGuardLogDocumentImages(logImage);

                    uploadedFiles.Add(publicPath);
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
        public async Task<IActionResult> UploadMultipleVideos(
      [FromForm] List<IFormFile> files,
      [FromForm] int guardId,
      [FromForm] int clientsiteId,
      [FromForm] int userId,
      [FromForm] string gps
  )
        {
            bool success = false;
            string message = "Uploaded successfully";
            var uploadedFiles = new List<string>();

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

                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Mob app video upload",
                    IsSystemEntry = false,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gps
                };

                int GuardLogId = _guardLogDataProvider.SaveGuardLogandReturnId(signInEntry);

                string[] allowedExtensions = { ".mp4", ".mov", ".avi", ".mkv" };

                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(ext) || !file.ContentType.StartsWith("video"))
                        throw new Exception($"Unsupported video type: {file.FileName}");

                    string folderPath = Path.Combine(_WebHostEnvironment.WebRootPath, "DglUploads", GuardLogId.ToString(), "Videos");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var dateTick = DateTime.Now.Ticks.ToString().Substring(10);
                    var uploadFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + dateTick + ext;
                    var fullPath = Path.Combine(folderPath, uploadFileName);

                    using (var stream = System.IO.File.Create(fullPath))
                        await file.CopyToAsync(stream);

                    var publicPath = $"https://cws-ir.com/DglUploads/{GuardLogId}/Videos/{uploadFileName}";

                    // Save record in the same table
                    var logFile = new GuardLogsDocumentImages
                    {
                        GuardLogId = GuardLogId,
                        ImagePath = publicPath,
                        IsVideo = true,
                        IsRearfile = false,
                        IsTwentyfivePercentfile = false
                    };

                    _guardLogDataProvider.SaveGuardLogDocumentImages(logFile);

                    uploadedFiles.Add(publicPath);
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



        [HttpGet("GetTagStatus")]
        public ActionResult<IEnumerable<SiteTagStatus>> GetTagStatus(int clientId)
        {
            try
            {
                var result = _guardLogDataProvider.GetSiteTagStatus(clientId);
                if (result == null || result.Count == 0)
                    return NotFound($"No tag status found for clientId {clientId}");

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

                    return  StatusCode(500, new
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

}
