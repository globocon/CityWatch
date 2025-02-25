using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Dropbox.Api.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


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
        private readonly string _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        public GuardSecurityNumberController(IGuardDataProvider guardDataProvider, IViewDataService viewDataService, ILogbookDataService logbookDataService, IGuardLogDataProvider guardLogDataProvider)
        {
            _guardDataProvider = guardDataProvider;
            _viewDataService = viewDataService;
            _logbookDataService = logbookDataService;
            _guardLogDataProvider = guardLogDataProvider;
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
        public IActionResult PostActivity(int guardId, int clientsiteId, int userId,string activityString)
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


    }




}
