using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Services;
//using iText.Kernel.Geom;
using iText.Layout;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;


namespace CityWatch.Web.API
{


    [Route("api/[controller]")]
    [ApiController]
    public class GuardSecurityNumberController : ControllerBase
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IViewDataService _viewDataService;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientDataProvider _clientDataProvider;
        private readonly ISiteEventLogDataProvider _SiteEventLogDataProvider;
        private readonly EmailOptions _emailOptions;
        private readonly IWebHostEnvironment _WebHostEnvironment;
        private readonly ISmsSenderProvider _smsSenderProvider;
        private readonly IConfiguration _configuration;
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        public GuardSecurityNumberController(IGuardDataProvider guardDataProvider, IViewDataService viewDataService, ILogbookDataService logbookDataService, IGuardLogDataProvider guardLogDataProvider, IClientDataProvider clientDataProvider, ISiteEventLogDataProvider siteEventLogDataProvider, IWebHostEnvironment webHostEnvironment, ISmsSenderProvider smsSenderProvider, IOptions<EmailOptions> emailOptions, IConfiguration configuration)
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
            _configuration= configuration;
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
                return NotFound(new
                {
                    message = "A guard with given security license number not found. If you are a new guard, tick 'New Guard?' to register and login.",
                    isActive = false
                });
            }

            if (!guard.IsActive)
            {
                return Unauthorized(new
                {
                    message = "A guard with given security license number is disabled. Please contact admin to activate.",
                    isActive = false
                });
            }

            return Ok(new
            {
                GuardId = guard.Id,
                Name = guard.Name,
                SecurityNo = guard.SecurityNo,
                isActive = true
            });
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

        [HttpGet("EnterGuardLogin")]
        public IActionResult EnterGuardLogin(int guardId, int clientsiteId, int userId)
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
                var guardLoginId = GetGuardLoginId(logBookId, guardId, clientsiteId, userId);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Default GPS coordinates (should be replaced with actual values if available)
                var gpsCoordinates = string.Empty;

                // Create a log entry
                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    Notes = "Logbook Logged In (Mob App)",
                    IsSystemEntry = true,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gpsCoordinates
                };

                _guardLogDataProvider.SaveGuardLog(signInEntry);

                return Ok(new { message = "Guard successfully logged in.", guardLoginId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }

        }



        [HttpGet("GetActivities")]
        public IActionResult GetActivities([FromQuery] int type)
        {
            try
            {
                var activity = _viewDataService.GetDressAppFields(type);

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



        private int GetGuardLoginId(int logBookId, int guardId, int clientsiteId, int userId)
        {
            // Get all guard logins associated with the logBookId
            var guardLoginList = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).ToList();

            // Check if a guard login exists for the current day
            var existingGuardLogin = guardLoginList.FirstOrDefault(x => x.GuardId == guardId && x.OnDuty.Date == DateTime.Now.Date);

            if (existingGuardLogin != null)
            {
                return existingGuardLogin.Id; // Return existing login ID
            }

            // Create a new GuardLogin entry
            var newGuardLogin = new GuardLogin
            {
                LoginDate = DateTime.Now,
                GuardId = guardId,
                ClientSiteId = clientsiteId,
                ClientSiteLogBookId = logBookId,
                PositionId = null,
                SmartWandId = null,
                OnDuty = DateTime.Now,
                OffDuty = DateTime.Now.AddHours(1),
                UserId = userId,
                IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            };




            // Save and return new login ID
            return _guardDataProvider.SaveGuardLogin(newGuardLogin);
        }



        [HttpGet("PostActivity")]
        public IActionResult PostActivity(int guardId, int clientsiteId, int userId, string activityString,string gps)
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
                var guardLoginId = GetGuardLoginId(logBookId, guardId, clientsiteId, userId);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Default GPS coordinates (should be replaced with actual values if available)
               var gpsCoordinates = gps;

                // Create a log entry
                var signInEntry = new GuardLog
                {
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    EventDateTime = DateTime.Now,
                    /*your message */
                    Notes = activityString,
                    IsSystemEntry = true,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    GpsCoordinates = gpsCoordinates
                };

                _guardLogDataProvider.SaveGuardLog(signInEntry);

                return Ok(new { message = "Guard successfully logged in.", guardLoginId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }

        }




        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                string fileExtension = Path.GetExtension(file.FileName);
                string newFileName = $"{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(_uploadFolder, newFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{newFileName}";

                return Ok(new { message = "File uploaded successfully!", fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("SaveClientSiteDuress")]
        public async Task<IActionResult> SaveClientSiteDuress(int guardId, int clientsiteId, int userId,string gps)
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
                var guardLoginId = GetGuardLoginId(logBookId, guardId, clientsiteId, userId);

                if (guardLoginId <= 0)
                    return BadRequest(new { message = "Guard login failed." });

                // Validate request parameters
                if (clientsiteId <= 0 || guardId <= 0 || guardLoginId <= 0 || logBookId <= 0 )
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

                        enabledAddress= address;
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

        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();
            foreach (var item in toAddress)
            {
                emailAddressList.Add(new MailboxAddress(string.Empty, item));
            }


            return emailAddressList;
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

        [HttpGet("GetSiteLog")]
        public IActionResult GetSiteLog(int clientsiteId)
        {
            try
            {
                // Fetch site name (optional usage)
                var site = _clientDataProvider.GetClientSiteName(clientsiteId);

                // Get today's logbook
                var logbook = _clientDataProvider.GetClientSiteLogBook(clientsiteId, LogBookType.DailyGuardLog, DateTime.Today);
                if (logbook == null)
                {
                    return NotFound(new { message = "No logbook found for today." });
                }

                // Get guard logs
                var guardLogs = _guardLogDataProvider.GetGuardLogswithKvLogData(logbook.Id, DateTime.Today)
                    .OrderByDescending(z => z.Id)
                    .ThenByDescending(z => z.EventDateTime)
                    .ToList();

                var result = new List<GuardLogDto>();

                foreach (var guardlog in guardLogs)
                {
                    var imageUrls = new List<string>();
                    var notes = guardlog.Notes ?? "";

                    // Process images
                    var images = _guardLogDataProvider.GetGuardLogDocumentImaes(guardlog.Id);
                    foreach (var img in images)
                    {
                        if (img.IsTwentyfivePercentfile == true && !string.IsNullOrEmpty(img.ImagePath))
                            imageUrls.Add(img.ImagePath);

                        if (img.IsRearfile == true && !string.IsNullOrEmpty(img.ImagePath))
                        {
                            var filename = Path.GetFileName(img.ImagePath);
                            notes += $"</br>See attached file <a href=\"{img.ImagePath}\" target=\"_blank\">{filename}</a>";
                        }
                    }

                    // Create response DTO
                    var localTime = TimeZoneInfo.ConvertTimeFromUtc(guardlog.EventDateTime, TimeZoneInfo.Local);
                    var offset = TimeZoneInfo.Local.GetUtcOffset(guardlog.EventDateTime);

                    var offsetSign = offset.TotalMinutes >= 0 ? "+" : "-";
                    var formattedOffset = $"{offsetSign}{offset:hh\\:mm}";
                    var timeZoneShort = $"GMT{formattedOffset}";

                    var formattedDisplayTime = localTime.ToString("HH:mm") + " Hrs " + timeZoneShort;

                    var dto = new GuardLogDto
                    {
                        Id = guardlog.Id,
                        EventDateTime = guardlog.EventDateTime,
                        EventDateTimeLocal = formattedDisplayTime, 
                        Notes = notes,
                        ImageUrls = imageUrls,
                        GuardInitials = guardlog.GuardLogin?.Guard?.Initial ?? "N/A"
                    };



                    result.Add(dto);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching the site log.",
                    error = ex.Message
                });
            }
        }





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

}
