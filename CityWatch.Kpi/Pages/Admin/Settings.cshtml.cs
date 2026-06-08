using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Helpers;
using CityWatch.Kpi.Models;
using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Security.Policy;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CityWatch.Kpi.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IImportDataService _importDataService;
        private readonly IImportJobDataProvider _importJobDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IKpiSchedulesDataProvider _kpiSchedulesDataProvider;
        private readonly ISendScheduleService _sendScheduleService;
        private readonly ILogger<SettingsModel> _logger;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly Settings _settings;
        private readonly IDropboxService _dropboxUploadService;
        private readonly IClientSiteViewDataService _clientViewDataService;


        [BindProperty]
        public KpiRequest ReportRequest { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IGuardLogDataProvider GuardLogDataProvider { get { return _guardLogDataProvider; } }

        public IImportJobDataProvider ImportJobDataProvider { get { return _importJobDataProvider; } }
        public IClientSiteViewDataService ClientViewDataService { get { return _clientViewDataService; } }
        public int GuardId { get; set; }
        public int userId { get; set; }
        public int ClientTypeId { get; set; }
        public int ClientSiteId { get; set; }
        public string ClientSiteName { get; set; }

        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService,
            IImportDataService importDataService,
            IImportJobDataProvider importJobDataProvider,
            IClientDataProvider clientDataProvider,
            IKpiSchedulesDataProvider kpiSchedulesDataProvider,
            ISendScheduleService sendScheduleService,
            ILogger<SettingsModel> logger,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
             IGuardLogDataProvider guardLogDataProvider,
             IGuardSettingsDataProvider guardSettingsDataProvider,
             IGuardDataProvider guardDataProvider,
             IConfigDataProvider configDataProvider,
             IOptions<Settings> settings,
             IDropboxService dropboxUploadService,
             IClientSiteViewDataService clientViewDataService
             )
        {
            _webHostEnvironment = webHostEnvironment;
            _viewDataService = viewDataService;
            _importDataService = importDataService;
            _importJobDataProvider = importJobDataProvider;
            _clientDataProvider = clientDataProvider;
            _kpiSchedulesDataProvider = kpiSchedulesDataProvider;
            _sendScheduleService = sendScheduleService;
            _logger = logger;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
            _guardDataProvider = guardDataProvider;
            _configDataProvider = configDataProvider;
            _settings = settings.Value;
            _dropboxUploadService = dropboxUploadService;
            _clientViewDataService = clientViewDataService;

        }

        public IActionResult OnGet()
        {
            ReportRequest = new KpiRequest();
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;

            // Fallback to query string if session is completely lost
            if (GuardId == 0 && Request.Query.ContainsKey("guardId"))
            {
                if (int.TryParse(Request.Query["guardId"], out int parsedGuardId))
                {
                    GuardId = parsedGuardId;
                    HttpContext.Session.SetInt32("GuardId", GuardId);
                }
            }

            if (userId == 0 && Request.Query.ContainsKey("userId"))
            {
                if (int.TryParse(Request.Query["userId"], out int parsedUserId))
                {
                    userId = parsedUserId;
                    HttpContext.Session.SetInt32("loginUserId", userId);
                }
            }

            ClientTypeId = HttpContext.Session.GetInt32("ClientTypeId") ?? 0;
            ClientSiteId = HttpContext.Session.GetInt32("ClientSiteId") ?? 0;
            var claimsIdentity = User.Identity as ClaimsIdentity;
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {   /* admin login only*/
                ReportRequest = new KpiRequest();
                HttpContext.Session.SetInt32("GuardId", 0);
                var roleClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                if (roleClaim == "Administrator")
                {
                    HttpContext.Session.SetInt32("loginUserId", 0);
                }
                else if (roleClaim == "User")
                {
                    var sidClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Sid)?.Value;
                    if (!string.IsNullOrEmpty(sidClaim))
                    {
                        int parsedSid = int.Parse(sidClaim);
                        HttpContext.Session.SetInt32("loginUserId", parsedSid);
                        userId = parsedSid;
                    }
                }

                if (ClientTypeId != 0)
                {
                    HttpContext.Session.SetInt32("ClientTypeId", ClientTypeId);
                }
                else
                {
                    HttpContext.Session.SetInt32("ClientTypeId", 0);
                }
                if (ClientSiteId != 0)
                {
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);
                    ClientSiteName = _viewDataService.ClientSitesUsingId(ClientSiteId);
                }
                else
                {
                    HttpContext.Session.SetInt32("ClientSiteId", 0);
                }
                return Page();
            }
            else if (GuardId != 0)
            {
                /* When guard Login*/
                HttpContext.Session.SetInt32("GuardId", GuardId);
                if (userId != 0)
                {
                    HttpContext.Session.SetInt32("loginUserId", userId);
                }
                if (ClientSiteId != 0)
                {
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);
                    ClientSiteName = _viewDataService.ClientSitesUsingId(ClientSiteId);
                }
                else
                {
                    HttpContext.Session.SetInt32("ClientSiteId", 0);
                }
                return Page();
            }
            else
            {
                /*unauthorized login*/
                if (ClientSiteId != 0)
                {
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);
                    ClientSiteName = _viewDataService.ClientSitesUsingId(ClientSiteId);
                    return Page();
                }
                
                // Only wipe session if completely unauthorized and redirecting to login
                HttpContext.Session.SetInt32("GuardId", 0);
                HttpContext.Session.SetInt32("loginUserId", 0);
                return Redirect(Url.Page("/Account/Login"));
            }
        }

        public IActionResult OnGetKpiDataImportJobs()
        {
            var importJobs = _importJobDataProvider.GetKpiDataImportJobs()
                .Select(x => new
                {
                    x.Id,
                    x.ClientSite.Name,
                    ReportDate = x.ReportDate.ToString("MMM yyyy"),
                    CreatedDate = x.CreatedDate.ToString("dd MMM yyyy HH:mm"),
                    CompletedDate = x.CompletedDate?.ToString("dd MMM yyyy HH:mm"),
                    x.Success
                })
                .ToList();
            return new JsonResult(importJobs);
        }

        public async Task OnPostAsync()
        {
            var serviceLog = new KpiDataImportJob()
            {
                ClientSiteId = ReportRequest.ClientSiteId,
                ReportDate = new DateTime(int.Parse(Request.Form["year"]), int.Parse(Request.Form["month"]), 1),
                CreatedDate = DateTime.Now,
            };
            var jobId = _importJobDataProvider.SaveKpiDataImportJob(serviceLog);
            await _importDataService.Run(jobId);
        }

        //code added to search client name start

        public JsonResult OnGetClientSiteWithSettings(string type, string searchTerm, int userId)
        {
            if (!string.IsNullOrEmpty(type) || !string.IsNullOrEmpty(searchTerm))
            {
                var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == type);
                if (clientType != null)
                {
                    var clientSites = _clientDataProvider.GetUserClientSites(type, searchTerm);
                    //GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    //if (GuardId != 0)
                    //    clientSites = _clientDataProvider.GetClientSitesUsingGuardId(GuardId);
                    //userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
                    if (userId != 0)
                        clientSites = _clientDataProvider.GetClientSitesUsingLoginUser(userId, searchTerm).Where(x => x.ClientType.Id == clientType.Id).ToList();
                    var clientSiteWithSettings = _clientDataProvider.GetClientSiteKpiSettings().Select(z => z.ClientSiteId).ToList();

                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        SiteUploadDailyLog = (z.UploadGuardLog || z.UploadKVLog || z.UploadSWLog || z.UploadFusionLog),
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id),
                        z.SiteEmail,
                        z.Address,
                        z.LandLine,
                        z.GuardLogEmailTo,
                        z.DataCollectionEnabled
                    }));

                }
                else
                {
                    var clientSites = _clientDataProvider.GetUserClientSites(type, searchTerm);
                    //GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    //if (GuardId != 0)
                    //    clientSites = _clientDataProvider.GetClientSitesUsingGuardId(GuardId);
                    //userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
                    if (userId != 0)
                        clientSites = _clientDataProvider.GetClientSitesUsingLoginUser(userId, searchTerm);
                    var clientSiteWithSettings = _clientDataProvider.GetClientSiteKpiSettings().Select(z => z.ClientSiteId).ToList();

                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        SiteUploadDailyLog = (z.UploadGuardLog || z.UploadKVLog || z.UploadSWLog || z.UploadFusionLog),
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id),
                        z.SiteEmail,
                        z.Address,
                        z.LandLine,
                        z.GuardLogEmailTo,
                        z.DataCollectionEnabled
                    }));
                }
            }
            return new JsonResult(new { });
        }

        //code added to search client name start
        public PartialViewResult OnGetClientSiteKpiSettings(int siteId)
        {
            var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
            var _clientSiteMobileAppSettings = _configDataProvider.GetCrowdSettingForSite(siteId);
            if (_clientSiteMobileAppSettings == null)
            {
                _clientSiteMobileAppSettings = new ClientSiteMobileAppSettings() { ClientSiteId = siteId };
            }
            if (clientSiteKpiSetting == null)
            {
                var clientSiteForTheId = _guardLogDataProvider.GetClientSites(siteId).FirstOrDefault();
                clientSiteKpiSetting ??= new ClientSiteKpiSetting() { ClientSiteId = siteId, ClientSite = clientSiteForTheId, clientSiteMobileAppSettings = _clientSiteMobileAppSettings };
            }
            else
            {
                clientSiteKpiSetting.clientSiteMobileAppSettings = _clientSiteMobileAppSettings;
            }

            // Standardize RCActionList loading and Base64 conversion
            var rcAction = _guardLogDataProvider.GetActionlist(siteId);
            if (rcAction != null)
            {
                if (!string.IsNullOrEmpty(rcAction.Imagepath))
                {
                    rcAction.Imagepath = rcAction.Imagepath + ":-:" + ConvertFileToBase64(rcAction.Imagepath);
                }
                clientSiteKpiSetting.RCActionList = new List<RCActionList> { rcAction };
            }

            return Partial("_ClientSiteKpiSetting", clientSiteKpiSetting);
        }

        public JsonResult OnPostClientSiteKpiSettings(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var clientSiteId = 0;

            var success = true;
            try
            {
                clientSiteId = clientSiteKpiSetting.ClientSiteId;

                // Default TuneDowngradeBuffer = 1
                if (clientSiteKpiSetting.TuneDowngradeBuffer.GetValueOrDefault() == 0)
                {
                    clientSiteKpiSetting.TuneDowngradeBuffer = 1;
                }
                clientSiteKpiSetting.ScheduleisActive = true;
                clientSiteKpiSetting.clientSiteMobileAppSettings = null;
                _clientDataProvider.SaveClientSiteKpiSetting(clientSiteKpiSetting);
            }
            catch
            {
                success = false;
            }

            //return new JsonResult(success);
            return new JsonResult(new { success, clientSiteId });
        }

        public JsonResult OnPostClientSiteManningKpiSettings(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = 0;
            var clientSiteId = 0;
            var erorrMessage = string.Empty;
            try
            {


                if (clientSiteKpiSetting != null)
                {
                    if (clientSiteKpiSetting.Id != 0)
                    {


                        clientSiteId = clientSiteKpiSetting.ClientSiteId;
                        var positionIdGuard = clientSiteKpiSetting.ClientSiteManningGuardKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        var positionIdPatrolCar = clientSiteKpiSetting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();

                        var InvalidTimes = _clientDataProvider.ValidDateTime(clientSiteKpiSetting);

                        if (InvalidTimes.Trim() == string.Empty)
                        {

                            if (positionIdGuard != null || positionIdPatrolCar != null)
                            {
                                var rulenumberOne = _clientDataProvider.CheckRulesOneinKpiManningInput(clientSiteKpiSetting);

                                if (rulenumberOne.Trim() == string.Empty)
                                {
                                    var rulenumberTwo = _clientDataProvider.CheckRulesTwoinKpiManningInput(clientSiteKpiSetting);
                                    if (rulenumberTwo.Trim() == string.Empty)
                                    {
                                        success = _clientDataProvider.SaveClientSiteManningKpiSetting(clientSiteKpiSetting);
                                        /* If change in the status update start */
                                        _clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id, clientSiteKpiSetting.KPITelematicsFieldID);
                                        /* If change in the status update end */
                                    }
                                    else
                                    {
                                        erorrMessage = rulenumberTwo;
                                        success = 7;

                                    }

                                }
                                else
                                {
                                    erorrMessage = rulenumberOne;
                                    success = 6;

                                }



                            }
                            else
                            {
                                success = 3;
                            }

                        }
                        else
                        {
                            erorrMessage = InvalidTimes;
                            success = 5;

                        }
                    }
                    else
                    {
                        success = 2;
                    }

                }
                else
                {
                    success = 4;

                }

            }
            catch
            {
                success = 4;
            }

            return new JsonResult(new { success, clientSiteId, erorrMessage });
        }




        public JsonResult OnPostClientSiteManningKpiSettingsWithoutValue(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = 0;
            var clientSiteId = 0;
            var erorrMessage = string.Empty;
            try
            {


                if (clientSiteKpiSetting != null)
                {
                    if (clientSiteKpiSetting.Id != 0)
                    {
                        clientSiteId = clientSiteKpiSetting.ClientSiteId;
                        success = _clientDataProvider.SaveClientSiteManningKpiSettingOnlyNoHours(clientSiteKpiSetting);
                        /* If change in the status update start */
                        _clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id, clientSiteKpiSetting.KPITelematicsFieldID);
                        /* If change in the status update end */
                    }
                    else
                    {
                        success = 2;
                    }

                }
                else
                {
                    success = 4;

                }

            }
            catch
            {
                success = 4;
            }

            return new JsonResult(new { success, clientSiteId, erorrMessage });
        }




        public JsonResult OnPostClientSiteManningKpiSettingsADHOC(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = 0;
            var clientSiteId = 0;
            var erorrMessage = string.Empty;
            try
            {


                if (clientSiteKpiSetting != null)
                {
                    if (clientSiteKpiSetting.Id != 0)
                    {


                        clientSiteId = clientSiteKpiSetting.ClientSiteId;
                        var positionIdGuard = clientSiteKpiSetting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.PositionId != 0).FirstOrDefault();
                        var positionIdPatrolCar = clientSiteKpiSetting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.PositionId != 0).FirstOrDefault();

                        var InvalidTimes = _clientDataProvider.ValidDateTimeADHOC(clientSiteKpiSetting);

                        if (InvalidTimes.Trim() == string.Empty)
                        {

                            if (positionIdGuard != null || positionIdPatrolCar != null)
                            {
                                var rulenumberOne = _clientDataProvider.CheckRulesOneinKpiManningInputADHOC(clientSiteKpiSetting);

                                if (rulenumberOne.Trim() == string.Empty)
                                {
                                    var rulenumberTwo = _clientDataProvider.CheckRulesTwoinKpiManningInputADHOC(clientSiteKpiSetting);
                                    if (rulenumberTwo.Trim() == string.Empty)
                                    {
                                        success = _clientDataProvider.SaveClientSiteManningKpiSettingADHOC(clientSiteKpiSetting);
                                        /* If change in the status update start */
                                        //_clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id);
                                        /* If change in the status update end */
                                    }
                                    else
                                    {
                                        erorrMessage = rulenumberTwo;
                                        success = 7;

                                    }

                                }
                                else
                                {
                                    erorrMessage = rulenumberOne;
                                    success = 6;

                                }



                            }
                            else
                            {
                                success = 3;
                            }

                        }
                        else
                        {
                            erorrMessage = InvalidTimes;
                            success = 5;

                        }
                    }
                    else
                    {
                        success = 2;
                    }

                }
                else
                {
                    success = 4;

                }

            }
            catch
            {
                success = 4;
            }

            return new JsonResult(new { success, clientSiteId, erorrMessage });
        }

        public JsonResult OnPostUploadSiteImage()
        {
            var success = true;
            var files = Request.Form.Files;
            var clientSiteId = Request.Form["ClientSiteId"];
            var previousFileName = Path.GetFileName(Request.Form["PreviousFileName"]);
            var fileName = $"Client_{clientSiteId}_{DateTime.Now.Ticks}.jpg";
            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    var siteImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "SiteImage");
                    if (!Directory.Exists(siteImageDir))
                        Directory.CreateDirectory(siteImageDir);

                    if (!string.IsNullOrEmpty(previousFileName) && System.IO.File.Exists(Path.Combine(siteImageDir, previousFileName)))
                        System.IO.File.Delete(Path.Combine(siteImageDir, previousFileName));

                    using (var stream = System.IO.File.Create(Path.Combine(siteImageDir, fileName)))
                    {
                        file.CopyTo(stream);
                    }
                }
            }
            catch
            {
                success = false;
            }

            return new JsonResult(new { success, fileName = $"/SiteImage/{fileName}" });
        }


        public IActionResult OnGetOfficerPositions(OfficerPositionFilterManning filter)
        {
            return new JsonResult(ViewDataService.GetOfficerPositions((OfficerPositionFilterManning)filter));
        }

        public JsonResult OnGetKpiSendScheduleSummaryImage(int scheduleId)
        {
            return new JsonResult(_kpiSchedulesDataProvider.GetScheduleSummaryImage(scheduleId));
        }

        public JsonResult OnPostUploadSummaryImage()
        {
            var success = true;
            var files = Request.Form.Files;
            var fileName = string.Empty;
            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    fileName = file.FileName;
                    var scheduleId = Convert.ToInt32(Request.Form["scheduleId"]);

                    var summaryImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "SummaryImage");
                    if (!Directory.Exists(summaryImageDir))
                        Directory.CreateDirectory(summaryImageDir);

                    using (var stream = System.IO.File.Create(Path.Combine(summaryImageDir, fileName)))
                    {
                        file.CopyTo(stream);
                    }
                    _kpiSchedulesDataProvider.SaveKpiSendScheduleSummaryImage(scheduleId, fileName);
                }
            }
            catch
            {
                success = false;
            }

            return new JsonResult(new { success, fileName, LastUpdated = DateTime.Now });
        }

        public JsonResult OnPostUploadRCImage()
        {
            var success = true;
            var files = Request.Form.Files;
            var fileName = string.Empty;
            var Imagepath = string.Empty;
            var dtm = DateTime.Now;
            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    fileName = file.FileName;
                    var Id = Convert.ToInt32(Request.Form["id"]); // Changed from scheduleId to id to match RCActionList ID

                    // Always use local RCImage folder
                    string summaryImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "RCImage");

                    if (!Directory.Exists(summaryImageDir))
                        Directory.CreateDirectory(summaryImageDir);

                    using (var stream = System.IO.File.Create(Path.Combine(summaryImageDir, fileName)))
                    {
                        file.CopyTo(stream);
                    }

                    // Update the database record
                    _clientDataProvider.SaveUpdateRCListFile(Id, fileName, dtm);
                    Imagepath = fileName + ":-:" + ConvertFileToBase64(fileName);
                }
            }
            catch
            {
                success = false;
            }

            return new JsonResult(new { success, fileName, LastUpdated = dtm, Imagepath = Imagepath });
        }

        public JsonResult OnGetKpiSendSchedules(int type, string searchTerm)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
            if (userId == 0 && Request.Query.ContainsKey("uId"))
            {
                if (int.TryParse(Request.Query["uId"], out int parsedUId))
                {
                    userId = parsedUId;
                }
            }

            if (GuardId != 0)
            {
                // Guard accessing via specific portal link - check sites they've logged into
                return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedulesUisngGuardId(GuardId)
                   .Select(z => KpiSendScheduleViewModel.FromDataModel(z))
                   .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                   .OrderBy(x => x.ProjectName)
                   .ThenBy(x => x.ClientTypes));
            }
            else if (userId != 0)
            {
                // Normal User logged in (like MARTHA) - restrict by their explicit UserClientSiteAccess assignments
                return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedulesUsingUserId(userId)
                   .Select(z => KpiSendScheduleViewModel.FromDataModel(z))
                   .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                   .OrderBy(x => x.ProjectName)
                   .ThenBy(x => x.ClientTypes));
            }
            else
            {
                // Super Admin / Main Admin accessing via CityWatch login portal (claimsIdentity.IsAuthenticated)
                // Their session explicitly sets GuardId = 0 and loginUserId = 0, so they can see all schedules
                return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedules()
                    .Select(z => KpiSendScheduleViewModel.FromDataModel(z))
                    .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                    .OrderBy(x => x.ProjectName)
                    .ThenBy(x => x.ClientTypes));
            }
        }

        public JsonResult OnGetKpiSendSchedule(int id)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetSendScheduleById(id));
            }
            else
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetSendScheduleByIdandGuardId(id, GuardId));
            }
        }

        public JsonResult OnPostSaveKpiSendSchedule(KpiSendScheduleViewModel kpiSendScheduleViewModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(kpiSendScheduleViewModel, new ValidationContext(kpiSendScheduleViewModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            var success = true;
            var message = "Saved successfully";
            try
            {
                var kpiSendSchedule = KpiSendScheduleViewModel.ToDataModel(kpiSendScheduleViewModel);
                if (kpiSendSchedule.Id == 0)
                    kpiSendSchedule.NextRunOn = KpiSendScheduleRunOnCalculator.GetNextRunOn(kpiSendSchedule);
                else
                    kpiSendSchedule.NextRunOn = KpiSendScheduleRunOnCalculator.GetNextRunOnUpdate(kpiSendSchedule);
                _kpiSchedulesDataProvider.SaveSendSchedule(kpiSendSchedule, true);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteKpiSendSchedule(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _kpiSchedulesDataProvider.DeleteSendSchedule(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }



        // Custom Wand Start

        public JsonResult OnGetKpiCustomWandSendSchedules(int type, string searchTerm)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
            if (userId == 0 && Request.Query.ContainsKey("uId"))
            {
                if (int.TryParse(Request.Query["uId"], out int parsedUId))
                {
                    userId = parsedUId;
                }
            }

            if (GuardId != 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetAllCustomWandSchedulesUisngGuardId(GuardId)
                   .Select(z => KpiCustomWandScheduleViewModel.FromDataModel(z))
                   .Where(z => (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                   .OrderBy(x => x.ProjectName)
                   .ThenBy(x => x.ClientTypes));
            }
            else if (userId != 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetAllCustomWandSchedulesUsingUserId(userId)
                   .Select(z => KpiCustomWandScheduleViewModel.FromDataModel(z))
                   .Where(z => (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                   .OrderBy(x => x.ProjectName)
                   .ThenBy(x => x.ClientTypes));
            }
            else
            {
                // Super Admin / Main Admin fallback
                return new JsonResult(_kpiSchedulesDataProvider.GetAllCustomWandSchedules()
                    .Select(z => KpiCustomWandScheduleViewModel.FromDataModel(z))
                    .Where(z => (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                    .OrderBy(x => x.ProjectName)
                    .ThenBy(x => x.ClientTypes));
            }
        }

        public JsonResult OnGetKpiCustomWandSchedule(int id)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetCustomWandScheduleById(id));
            }
            else
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetCustomWandScheduleByIdandGuardId(id, GuardId));
            }
        }

        public JsonResult OnPostSaveKpiCustomWandSchedule(KpiCustomWandScheduleViewModel kpiCustomWandScheduleViewModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(kpiCustomWandScheduleViewModel, new ValidationContext(kpiCustomWandScheduleViewModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            var success = true;
            var message = "Saved successfully";
            try
            {
                var kpiCustomWandSchedule = KpiCustomWandScheduleViewModel.ToDataModel(kpiCustomWandScheduleViewModel);
                if (kpiCustomWandSchedule.Id == 0)
                    kpiCustomWandSchedule.NextRunOn = KpiCustomWandScheduleRunOnCalculator.GetNextRunOn(kpiCustomWandSchedule);
                else
                    kpiCustomWandSchedule.NextRunOn = KpiCustomWandScheduleRunOnCalculator.GetNextRunOnUpdate(kpiCustomWandSchedule);
                _kpiSchedulesDataProvider.SaveCustomWandSchedule(kpiCustomWandSchedule, true);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteKpiCustomWandSchedule(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _kpiSchedulesDataProvider.DeleteCustomWandSchedule(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public async Task<IActionResult> OnGetDownloadExcelCustomWands(int scheduleId, DateTime reportDate, bool ignoreRecipients)
        {
            var schedule = _kpiSchedulesDataProvider.GetCustomWandScheduleById(scheduleId);

            if (schedule == null)
                throw new ArgumentException("Schedule not found");

            var (task, fileNames) = await _sendScheduleService.ProcessCustomWandSchedule(schedule, reportDate, ignoreRecipients, false, false, false, false);

            if (fileNames == null || !fileNames.Any())
                return NotFound("No files generated.");

            // Single file -> return excel directly
            if (fileNames.Count == 1)
            {
                var filePath = fileNames.First();

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found.");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                var downloadFileName = Path.GetFileName(filePath);

                // Delete file after reading
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch
                {
                    // Ignore delete failure
                }

                Response.Headers["Content-Disposition"] = $"attachment; filename={downloadFileName}";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadFileName);
            }

            // Multiple files -> return zip
            using var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var filePath in fileNames)
                {
                    if (!System.IO.File.Exists(filePath))
                        continue;

                    var entry = archive.CreateEntry(Path.GetFileName(filePath));

                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                    await fileStream.CopyToAsync(entryStream);
                }
            }

            memoryStream.Position = 0;

            // Delete files after zip creation
            foreach (var filePath in fileNames)
            {
                try
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch
                {
                    // Ignore delete failure
                }
            }

            var zipFileName = $"CustomWands_{DateTime.Now:yyyyMMddHHmmss}.zip";
            Response.Headers["Content-Disposition"] = $"attachment; filename={zipFileName}";
            return File(memoryStream.ToArray(), "application/zip", zipFileName);
        }

        public async Task<JsonResult> OnPostRunScheduleCustomWand(int scheduleId, DateTime reportDate, bool ignoreRecipients)
        {
            var success = false;
            string message;
            try
            {
                var schedule = _kpiSchedulesDataProvider.GetCustomWandScheduleById(scheduleId);
                if (schedule == null)
                    throw new ArgumentException("Schedule not found");

                var (task, fileNames) = await _sendScheduleService.ProcessCustomWandSchedule(schedule, reportDate, ignoreRecipients, false, false, true, true);

                message = task;
                success = !(message.Contains("Error") || message.Contains("Exception"));
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            if (!success)
            {
                _logger.LogError(message);
            }

            return new JsonResult(new { success });
        }


        // Custom Wand End



        //code added For RcAction List start
        public JsonResult OnPostClientSiteRCActionList(RCActionList rcActionList)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {

                if (rcActionList.SettingsId != 0)
                {

                    id = _clientDataProvider.SaveRCList(rcActionList);

                }
                else
                {
                    status = false;
                    message = "Please add site settings first";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message, id });
        }

        //code added For RcAction List stop
        public JsonResult OnGetClientSiteKpiNote(int clientSiteId, int month, int year)
        {
            var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            var clientSiteKpiNote = kpiSetting.Notes.SingleOrDefault(z => z.ForMonth == new DateTime(year, month, 1));
            clientSiteKpiNote ??= new ClientSiteKpiNote()
            {
                ForMonth = new DateTime(year, month, 1),
                SettingsId = kpiSetting.Id,
                Notes = string.Empty,
                HRRecords = string.Empty
            };
            return new JsonResult(clientSiteKpiNote);
        }

        public JsonResult OnPostClientSiteKpiNote(ClientSiteKpiNote clientSiteKpiNote)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {
                id = _clientDataProvider.SaveClientSiteKpiNote(clientSiteKpiNote);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message, id });
        }

        public JsonResult OnPostDeleteClientSiteKpiNote(int noteId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var note = _clientDataProvider.GetClientSiteKpiNote(noteId);
                if (note != null)
                {
                    note.Notes = string.Empty;
                    _clientDataProvider.SaveClientSiteKpiNote(note);
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        //to delete RC
        public JsonResult OnPostDeleteRC(int RCId)
        {
            var status = true;
            var message = "Success";
            try
            {

                _clientDataProvider.RemoveRCList(RCId);
                //var RCList = _clientDataProvider.GetClientSiteKpiRC(RCId);
                //if (RCList != null)
                //{
                //    RCList.SiteAlarmKeypadCode = string.Empty;
                //    RCList.Action1 = string.Empty;
                //    RCList.Sitephysicalkey = string.Empty;
                //    RCList.Action2 = string.Empty;
                //    RCList.SiteCombinationLook = string.Empty;
                //    RCList.Action3 = string.Empty;
                //    RCList.ControlRoomOperator = string.Empty;
                //    RCList.Action4 = string.Empty;
                //    RCList.Action5 = string.Empty;
                //    _clientDataProvider.SaveRCList(RCList);
                //}
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostDeleteWorker(string settingsId)
        {
            var status = true;
            var message = "Success";
            var clientSiteId = 0;
            try
            {
                if (settingsId != string.Empty)
                {
                    var split = settingsId.Split('_');

                    if (split.Length > 0)
                    {
                        var settId = int.Parse(split[0]);
                        var orderId = int.Parse(split[1]);
                        clientSiteId = int.Parse(split[2]);
                        _clientDataProvider.RemoveWorker(settId, orderId);

                    }

                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message, clientSiteId });
        }

        public JsonResult OnPostDeleteWorkerADHOC(string settingsId)
        {
            var status = true;
            var message = "Success";
            var clientSiteId = 0;
            try
            {
                if (settingsId != string.Empty)
                {
                    var split = settingsId.Split('_');

                    if (split.Length > 0)
                    {
                        var settId = int.Parse(split[0]);
                        var orderId = int.Parse(split[1]);
                        clientSiteId = int.Parse(split[2]);
                        _clientDataProvider.RemoveWorkerADHOC(settId, orderId);

                    }

                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message, clientSiteId });
        }

        public JsonResult OnPostRunSchedule(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var success = false;
            string message;
            try
            {
                var schedule = _kpiSchedulesDataProvider.GetSendScheduleById(scheduleId);
                if (schedule == null)
                    throw new ArgumentException("Schedule not found");

                var task = _sendScheduleService.ProcessSchedule(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);

                message = task.Result;
                success = !(message.Contains("Error") || message.Contains("Exception"));
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            if (!success)
            {
                _logger.LogError(message);
            }

            return new JsonResult(new { success });
        }

        public JsonResult OnGetKpiSendScheduleSummaryNote(int scheduleId, int month, int year)
        {
            var kpiSendScheduleNotes = _kpiSchedulesDataProvider.GetSendScheduleById(scheduleId);
            var kpiScheduleNote = kpiSendScheduleNotes.KpiSendScheduleSummaryNotes.SingleOrDefault(z => z.ForMonth == new DateTime(year, month, 1));
            kpiScheduleNote ??= new KpiSendScheduleSummaryNote()
            {
                ForMonth = new DateTime(year, month, 1),
                ScheduleId = kpiSendScheduleNotes.Id,
                Notes = string.Empty
            };
            return new JsonResult(kpiScheduleNote);
        }

        public JsonResult OnPostKpiSendScheduleSummaryNote(KpiSendScheduleSummaryNote kpiSendScheduleSummaryNote)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {
                id = _kpiSchedulesDataProvider.SaveKpiSendScheduleSummaryNote(kpiSendScheduleSummaryNote);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message, id });
        }

        public JsonResult OnPostDeleteKpiSendScheduleSummaryNote(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                var note = _kpiSchedulesDataProvider.GetKpiSendScheduleSummaryNote(id);
                if (note != null)
                {
                    note.Notes = string.Empty;
                    _kpiSchedulesDataProvider.SaveKpiSendScheduleSummaryNote(note);
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostDeleteSummaryImage(int scheduleId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var summaryImage = _kpiSchedulesDataProvider.GetScheduleSummaryImage(scheduleId);
                if (summaryImage != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "SummaryImage", summaryImage.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);

                    _kpiSchedulesDataProvider.DeleteSummaryImage(scheduleId);
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostDeleteRCImage(string imageName)
        {
            var status = true;
            var message = "Success";
            try
            {

                if (!string.IsNullOrEmpty(imageName))
                {
                    // Always use local RCImage folder
                    string summaryImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "RCImage");

                    var fileToDelete = Path.Combine(summaryImageDir, imageName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);

                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public string ConvertFileToBase64(string imageName)
        {
            var rtnstring = string.Empty;

            if (!string.IsNullOrEmpty(imageName))
            {
                // Always use local RCImage folder
                string summaryImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "RCImage");

                var fileToConvert = Path.Combine(summaryImageDir, imageName);
                if (System.IO.File.Exists(fileToConvert))
                {
                    byte[] AsBytes = System.IO.File.ReadAllBytes(fileToConvert);
                    rtnstring = "data:application/octet-stream;base64," + Convert.ToBase64String(AsBytes);
                }
            }

            return rtnstring;
        }

        public IActionResult OnGetDownloadPdf(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var schedule = _kpiSchedulesDataProvider.GetSendScheduleById(scheduleId);
            if (schedule == null)
                throw new ArgumentException("Schedule not found");
            // Generate the PDF file
            DateTime date = new DateTime(reportYear, reportMonth, 1);
            string filename = $"{reportYear}{reportMonth.ToString("00")} - {FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Monthly Report - {date.ToString("MMM").ToUpper()} {reportYear}";
            byte[] pdfBytes = _sendScheduleService.ProcessDownload(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);
            Response.Headers["Content-Disposition"] = $"inline; filename={filename}";
            // Return the PDF file as a download
            return File(pdfBytes, "application/pdf", filename + ".pdf");
        }

        //Menu chage -start
        public JsonResult OnGetSmartWandSettings(int clientSiteId)
        {
            var records = _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSiteId).OrderBy(z => z.SmartWandId).ToList();
            var pcar = _clientSiteWandDataProvider.GetPatrolCars();
            foreach (var r in records)
            {
                r.PatrolCarId = r.PatrolCarId ?? -1;
                r.PatrolCarName = pcar?.FirstOrDefault(x => x.Id == r.PatrolCarId)?.Name ?? "";
            }
            return new JsonResult(_clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSiteId).OrderBy(z => z.SmartWandId).ToList());
        }

        public JsonResult OnPostDeleteSmartWandSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.DeleteClientSiteSmartWand(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public void OnPostSaveSiteEmail(int siteId, string siteEmail, string landLine,
             string duressEmail, string duressSms, bool IsDosDontList)
        {
            var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(z => z.Id == siteId);
            if (clientSite != null)
            {
                clientSite.SiteEmail = siteEmail;
                //clientSite.UploadGuardLog = enableLBLogDump;
                //clientSite.UploadKVLog = enableKVLogDump;
                //clientSite.UploadSWLog = enableSWLogDump;
                //clientSite.UploadFusionLog = uploadFusionLog;
                clientSite.LandLine = landLine;
                //clientSite.GuardLogEmailTo = guardEmailTo;
                clientSite.DuressEmail = duressEmail;
                clientSite.DuressSms = duressSms;
                clientSite.IsDosDontList = IsDosDontList;
            }

            _clientDataProvider.SaveClientSite(clientSite);
        }
        public JsonResult OnGetSmartWandPhoneNumber(string PhoneNumber, int id)
        {
            var ssd = _clientSiteWandDataProvider.GetClientSiteSmartWandsNo(PhoneNumber, id);
            return new JsonResult(_clientSiteWandDataProvider.GetClientSiteSmartWandsNo(PhoneNumber, id));
        }

        public JsonResult OnPostSmartWandSettings(ClientSiteSmartWand record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.SaveClientSiteSmartWand(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        //Menu change -end

        /*patrolcar settings-start*/
        public JsonResult OnGetPatrolCar(int clientSiteId)
        {
            return new JsonResult(_clientSiteWandDataProvider.GetClientSitePatrolCars(clientSiteId).ToList());
        }

        public JsonResult OnPostPatrolCar(ClientSitePatrolCar record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.SaveClientSitePatrolCar(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }


        public JsonResult OnPostDeletePatrolCar(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.DeleteClientSitePatrolCar(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        /*patrolcar settings-end*/
        //custom fields settings-start
        public IActionResult OnGetCustomFields()
        {
            var customFields = _guardLogDataProvider.GetClientSiteCustomFields();
            var fieldNames = customFields.Select(z => z.Name).Distinct().OrderBy(z => z);
            var slots = customFields.Select(z => z.TimeSlot).Distinct().OrderBy(z => z);
            return new JsonResult(new { fieldNames, slots });
        }
        public IActionResult OnGetClientSiteCustomFields(int clientSiteId)
        {
            return new JsonResult(_guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId));
        }
        public JsonResult OnPostCustomFields(ClientSiteCustomField clientSiteCustomField)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {
                if (!ModelState.IsValid)
                {
                    return new JsonResult(new { status = false, message = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
                }

                id = _guardLogDataProvider.SaveClientSiteCustomFields(clientSiteCustomField);

                var clientSiteLogBookId = _clientDataProvider.GetClientSiteLogBook(clientSiteCustomField.ClientSiteId, LogBookType.DailyGuardLog, DateTime.Today)?.Id;
                if (clientSiteLogBookId.HasValue)
                {
                    var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBookId.Value).Where(x => x.CustomFieldId == id);
                    if (!customFieldLogs.Any())
                    {
                        var customFieldLog = new CustomFieldLog
                        {
                            CustomFieldId = id,
                            ClientSiteLogBookId = clientSiteLogBookId.Value
                        };
                        _guardLogDataProvider.SaveCustomFieldLog(customFieldLog);
                    }
                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message = new[] { message }, id });
        }
        public IActionResult OnPostDeleteClientSiteCustomField(int id)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.DeleteClientSiteCustomFields(id);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //custom fields settings-end
        /*site poc and locations-start*/
        public IActionResult OnGetSitePocs(int clientSiteId)
        {
            return new JsonResult(_guardSettingsDataProvider.GetClientSitePocs(clientSiteId));
        }

        public IActionResult OnPostSitePoc(ClientSitePoc record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.SaveClientSitePoc(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A site POC with same name already exists. This may be deleted";
                }
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnPostDeleteSitePoc(int id)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardSettingsDataProvider.DeleteClientSitePoc(id);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnGetSiteLocations(int clientSiteId)
        {
            return new JsonResult(_guardSettingsDataProvider.GetClientSiteLocations(clientSiteId));
        }

        public IActionResult OnPostSiteLocation(ClientSiteLocation record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.SaveClientSiteLocation(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A site location with same name already exists. This may be deleted";
                }
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnPostDeleteSiteLocation(int id)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardSettingsDataProvider.DeleteClientSiteLocation(id);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        /*site poc and locations-end*/
        /*key settings-start*/
        public JsonResult OnGetClientSiteKeys(int clientSiteId)
        {
            //p2-140 key photos  -start
            var clientSiteKeys = _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).ToList();
            foreach (var item in clientSiteKeys)
            {
                if (item.ImagePath == null)
                {
                    item.ImagePathNew = string.Empty;
                }
                else
                {
                    item.ImagePathNew = Path.GetFileName(item.ImagePath);
                }
            }
            return new JsonResult(clientSiteKeys);
            //p2-140 key photos  -end
            //return new JsonResult(_guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).ToList());
        }
        public JsonResult OnPostClientSiteKey(ClientSiteKey clientSiteKey)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                if (clientSiteKey.ImagePath == null)
                {
                    clientSiteKey.ImagePath = string.Empty;
                }
                _guardSettingsDataProvider.SaveClientSiteKey(clientSiteKey);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "Key number already exists";
                }
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteClientSiteKey(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.DeleteClientSiteKey(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        /*key settings - end*/
        //for toggle areas - start 
        public void OnPostSaveToggleType(int siteId, int timeslottoggleTypeId, bool timeslotIsActive, int vwitoggleTypeId, bool vwiIsActive,
            int sendertoggleTypeId, bool senderIsActive, int reelstoggleTypeId, bool reelsIsActive, int trailerRegoTypeId, bool isISO, bool isVIN,
            bool isTrailerRego, bool isCarsStock)
        {

            bool trailerRegoIsActive = isVIN || isISO || isTrailerRego || isCarsStock;

            _clientDataProvider.SaveClientSiteToggle(siteId, timeslottoggleTypeId, timeslotIsActive,false,false,false,false);
            _clientDataProvider.SaveClientSiteToggle(siteId, vwitoggleTypeId, vwiIsActive, false, false, false, false);
            _clientDataProvider.SaveClientSiteToggle(siteId, sendertoggleTypeId, senderIsActive, false, false, false, false);
            _clientDataProvider.SaveClientSiteToggle(siteId, reelstoggleTypeId, reelsIsActive, false, false, false, false);
            _clientDataProvider.SaveClientSiteToggle(siteId, trailerRegoTypeId, trailerRegoIsActive, isVIN, isISO, isTrailerRego, isCarsStock);
        }
        public IActionResult OnGetClientSiteToggle(int siteId)
        {

            return new JsonResult(_guardDataProvider.GetClientSiteToggle().Where(x => x.ClientSiteId == siteId));
        }
        //for toggle areas - end
        //for ring fence-start
        public JsonResult OnPostUpdateSiteDataCollection(int clientSiteId, bool disabled)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientDataProvider.SetDataCollectionStatus(clientSiteId, !disabled);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //for ring fence-end
        public JsonResult OnGetClientSiteEmail(int clientSiteId)
        {

            return new JsonResult(_clientDataProvider.GetNewClientSites(clientSiteId));
        }


        public JsonResult OnGetKPIScheduleDeafultMailbox()
        {
            var Emails = _clientDataProvider.GetKPIScheduleDeafultMailbox();
            var emailAddresses = string.Join(",", Emails.Select(email => email.KPIMail));
            return new JsonResult(new { Emails = emailAddresses });
        }
        public JsonResult OnPostSaveDeafultMailBox(string Email)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeafultMailBox(Email);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = Email, message = message });
        }
        public JsonResult OnGetClientTypes(int? page, int? limit)
        {
            return new JsonResult(_viewDataService.GetUserClientTypesHavingAccess(AuthUserHelperRadio.LoggedInUserId));
        }

        /*Dropbox settings-start*/
        public JsonResult OnGetCustomDropboxSettings(int clientSiteId)
        {
            var r = _clientDataProvider.GetKpiSettingsCustomDropboxFolder(clientSiteId);
            return new JsonResult(r);
        }

        public async Task<JsonResult> OnPostCustomDropboxSettings(ClientSiteKpiSettingsCustomDropboxFolder record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientDataProvider.SaveKpiSettingsCustomDropboxFolder(record);
                success = true;

                await CheckAndCreateCustomDropBoxFolder(record);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        private async Task<bool> CheckAndCreateCustomDropBoxFolder(ClientSiteKpiSettingsCustomDropboxFolder record)
        {
            try
            {
                // Try creating folder
                var reportFromDate = DateTime.Now;
                var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSettings().Where(x => x.ClientSiteId == record.ClientSiteId).SingleOrDefault();
                var customDbxFolderPath = $"{clientSiteKpiSetting.DropboxImagesDir}/FLIR - Wand Recordings - IRs - Daily Logs/{reportFromDate.Date.Year}/{reportFromDate.Date:yyyyMM} - {reportFromDate.Date.ToString("MMMM").ToUpper()} DATA/";
                var customdropboxfolders = _clientDataProvider.GetKpiSettingsCustomDropboxFolder(clientSiteKpiSetting.ClientSiteId).ToList();
                if (customdropboxfolders.Count > 0)
                {
                    //This is active only create new folder27/11/2024 Start
                    if (clientSiteKpiSetting.DropboxScheduleisActive)
                    {
                        //This is active only create new folder27/11/2024 end
                        var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                _settings.DropboxRefreshToken, _settings.DropboxUserEmail);
                        foreach (var customdropboxfolder in customdropboxfolders)
                        {
                            var dbxfldr = $"{customDbxFolderPath}{customdropboxfolder.DropboxFolderName}";
                            try
                            {
                                await _dropboxUploadService.CreateFolder(dropboxSettings, dbxfldr);
                                _logger.LogInformation($"Custom dropbox folder {dbxfldr} created.");
                                return true;
                            }
                            catch (Exception exp)
                            {
                                _logger.LogError(exp.Message);
                                _logger.LogError(exp.InnerException.ToString());
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.InnerException.ToString());
            }
            return false;
        }

        public JsonResult OnPostDeleteCustomDropboxSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientDataProvider.DeleteKpiSettingsCustomDropboxFolder(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostSaveDropboxSettings(KpiDropboxSettings record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientDataProvider.SaveKpiDropboxSettings(record);
                success = true;
                message = "Site dropbox settings updated successfully.";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        /*Dropbox settings-end*/


        public JsonResult OnGetCriticalDocumentList(int clientSiteId)
        {

            int GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                //var ddd = _configDataProvider.GetCriticalDocs()
                //    .Select(z => CriticalDocumentViewModel.FromDataModel(z));
                return new JsonResult(_configDataProvider.GetCriticalDocsByClientSiteId(clientSiteId)
                    .Select(z => CriticalDocumentViewModel.FromDataModel(z)));


            }
            else
            {
                return new JsonResult(_configDataProvider.GetCriticalDocsByClientSiteId(clientSiteId)
                   .Select(z => CriticalDocumentViewModel.FromDataModel(z)));
                //return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedulesUisngGuardId(GuardId)
                //   .Select(z => KpiSendScheduleViewModel.FromDataModel(z))
                //   .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                //   .OrderBy(x => x.ProjectName)
                //   .ThenBy(x => x.ClientTypes));

            }
        }


        public JsonResult OnGetClientSitesNew(string typeId)
        {
            if (typeId != null)
            {
                string[] typeId2 = typeId.Split(';');
                int[] typeId3 = new int[typeId2.Length];
                int i = 0;
                foreach (var item in typeId2)
                {

                    typeId3[i] = Convert.ToInt32(item);
                    i++;


                }

                return new JsonResult(_guardLogDataProvider.GetAllClientSites().Where(x => typeId == null || typeId3.Contains(x.TypeId)).OrderBy(z => z.Name).ThenBy(z => z.TypeId));
            }
            return new JsonResult(_guardLogDataProvider.GetAllClientSites().Where(x => x.TypeId == 0).OrderBy(z => z.Name).ThenBy(z => z.TypeId));
        }

        public IActionResult OnGetDescriptionList(int HRGroupId)
        {
            return new JsonResult(_configDataProvider.GetDescList(HRGroupId));
        }

        public JsonResult OnPostSaveCriticalDocuments(CriticalDocumentViewModel CriticalDocModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(CriticalDocModel, new ValidationContext(CriticalDocModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            var success = true;
            var message = "Saved successfully";
            try
            {
                var CriticalDoc = CriticalDocumentViewModel.ToDataModel(CriticalDocModel);
                _configDataProvider.SaveCriticalDoc(CriticalDoc, true);

            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetCriticalDocList(int id)
        {
            int GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                var document = _configDataProvider.GetCriticalDocById(id);

                if (document == null)
                {
                    return new JsonResult(null);
                }

                var documentDto = new CriticalDocuments
                {
                    Id = document.Id,
                    ClientTypeId = document.ClientTypeId,
                    HRGroupID = document.HRGroupID,
                    GroupName = document.GroupName,
                    CriticalDocumentsClientSites = document.CriticalDocumentsClientSites.Select(cs => new CriticalDocumentsClientSites
                    {
                        Id = cs.Id,
                        ClientSiteId = cs.ClientSiteId,
                        ClientSite = new ClientSite
                        {
                            Id = cs.ClientSite.Id,
                            Name = cs.ClientSite.Name,
                            //ClientTypeId = cs.ClientSite.ClientTypeId,

                        }
                    }).ToList(),
                    CriticalDocumentDescriptions = document.CriticalDocumentDescriptions.Select(desc => new CriticalDocumentDescriptions
                    {
                        Id = desc.Id,
                        DescriptionID = desc.DescriptionID,
                        HRSettings = desc.HRSettings == null ? null : new HrSettings
                        {
                            Id = desc.HRSettings.Id,
                            Description = desc.HRSettings.Description,
                            ReferenceNoNumbers = desc.HRSettings.ReferenceNoNumbers == null ? null : new ReferenceNoNumbers
                            {
                                Id = desc.HRSettings.ReferenceNoNumbers.Id,
                                Name = desc.HRSettings.ReferenceNoNumbers.Name
                            },
                            ReferenceNoAlphabets = desc.HRSettings.ReferenceNoAlphabets == null ? null : new ReferenceNoAlphabets
                            {
                                Id = desc.HRSettings.ReferenceNoAlphabets.Id,
                                Name = desc.HRSettings.ReferenceNoAlphabets.Name
                            },
                            HRGroups = desc.HRSettings.HRGroups == null ? null : new HRGroups
                            {
                                Id = desc.HRSettings.HRGroups.Id,
                                Name = desc.HRSettings.HRGroups.Name,
                                IsDeleted = desc.HRSettings.HRGroups.IsDeleted

                            }
                        }
                    }).ToList()
                };

                return new JsonResult(documentDto);
            }
            else
            {
                return new JsonResult(_configDataProvider.GetCriticalDocByIdandGuardId(id, GuardId));
            }
        }

        public JsonResult OnPostDeleteCriticalDoc(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _configDataProvider.DeleteCriticalDoc(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }
        //p2-140 key photos  -start
        public JsonResult OnPostUploadKeyFileAttachmentAttachment()
        {
            var success = true;
            var files = Request.Form.Files;

            var keyNo = Request.Form["keyNo"];
            var clientSiteId = Request.Form["clientSiteId"];
            var fileName = string.Empty;
            string filePath = string.Empty;
            string url = Request.Form["url"].ToString();

            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    fileName = file.FileName;


                    string extension = ""; string newFileName = ""; var formattedDate = "";

                    extension = Path.GetExtension(fileName).ToLower();
                    if (!string.IsNullOrEmpty(keyNo))
                    {
                        newFileName = keyNo + extension;
                        fileName = newFileName;
                    }





                    //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    //var fileNameUpload = fileNameWithoutExtension + "_" + CurrentDate + extension;




                    var guardUploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "KeyImage", clientSiteId);

                    if (!Directory.Exists(guardUploadDir))
                        Directory.CreateDirectory(guardUploadDir);

                    filePath = Path.Combine(guardUploadDir, fileName);
                    if (System.IO.File.Exists(filePath))
                    {

                        System.IO.File.Delete(filePath);

                    }
                    using var stream = System.IO.File.Create(Path.Combine(guardUploadDir, fileName));
                    file.CopyTo(stream);



                }

            }
            catch
            {
                success = false;
            }
            var guardUploadDirnew = Path.Combine(url, "KeyImage", clientSiteId);
            var imagePath = Path.Combine(guardUploadDirnew, fileName);
            var imagePathNew = fileName;
            return new JsonResult(new { success, imagePath, imagePathNew });
        }

        public JsonResult OnPostDeleteKeyImageAttachment(int id, string name, int clientsiteid)

        {

            var success = false;
            var message = "Success";
            try
            {
                if (id != 0)
                {

                    var filePath = string.Empty;
                    filePath = Path.Combine(_webHostEnvironment.WebRootPath, "KeyImage", clientsiteid.ToString(), name);
                    if (System.IO.File.Exists(filePath))
                    {

                        System.IO.File.Delete(filePath);

                    }

                    _guardSettingsDataProvider.DeleteClientSiteKeyImage(id);
                }



            }
            catch (Exception ex)
            {
                success = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //p2-140 key photos  -start
        public JsonResult OnGetCrmSupplierData(string companyName)
        {
            return new JsonResult(_guardLogDataProvider.GetCompanyDetailsVehLog(companyName));
        }

        public JsonResult OnPostSaveKpiTimesheetSchedule(KpiTimeSheetScheduleViewModel kpiSendTimesheetViewModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(kpiSendTimesheetViewModel, new ValidationContext(kpiSendTimesheetViewModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            var success = true;
            var message = "Saved successfully";
            try
            {
                var kpiSendSchedule = KpiTimeSheetScheduleViewModel.ToDataModel(kpiSendTimesheetViewModel);
                if (kpiSendSchedule.Id == 0)
                    kpiSendSchedule.NextRunOn = KpiTimesheetScheduleRunOnCalculator.GetNextRunOn(kpiSendSchedule);
                else
                    kpiSendSchedule.NextRunOn = KpiTimesheetScheduleRunOnCalculator.GetNextRunOnUpdate(kpiSendSchedule);
                _kpiSchedulesDataProvider.SaveTimesheetSchedule(kpiSendSchedule, true);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }



        public JsonResult OnPostCreateProfile([FromForm] int routeId, [FromForm] string pcarroutename, [FromForm] int smartwandallocation)
        {
            if (string.IsNullOrWhiteSpace(pcarroutename))
                return new JsonResult(new { success = false, message = "Route name is required." });

            // NEW VALIDATION
            if (smartwandallocation == 0)
                return new JsonResult(new { success = false, message = "Please select at least one Smart Wand Allocation." });

            // Call your validated save method
            var result = _kpiSchedulesDataProvider.SavePcarrouteMaster(
                routeId == 0 ? (int?)null : routeId,
                pcarroutename,
                smartwandallocation
            );

            // Validation failed?
            if (!result.success)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = result.message
                });
            }

            // Success — return new or updated route
            return new JsonResult(new
            {
                success = true,
                message = result.message,
                routeId = result.route.Id
            });
        }



        public JsonResult OnPostDeletePCarProfile(int RouteId)
        {


            var result = _kpiSchedulesDataProvider.DeletePcarrouteProfile(RouteId);

            if (!result)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Failed to delete profile."
                });
            }

            return new JsonResult(new
            {
                success = true,
                message = "Profile deleted successfully.",
                routeId = RouteId
            });
        }


        public JsonResult OnPostSaveRouteDetails([FromBody] PcarRouteSaveViewModel model)
        {
            if (model.PcarRouteId == 0 || model.SiteSchedules == null || !model.SiteSchedules.Any())
                return new JsonResult(new { success = false, message = "Invalid Profile or Client Sites." });

            _kpiSchedulesDataProvider.SavePcarrouteDetails(model);
            return new JsonResult(new { success = true });
        }


        public JsonResult OnGetKpiTimesheetSchedules(int type, string searchTerm)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
            if (userId == 0 && Request.Query.ContainsKey("uId"))
            {
                if (int.TryParse(Request.Query["uId"], out int parsedUId))
                {
                    userId = parsedUId;
                }
            }

            if (GuardId != 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetAllTimesheetSchedulesUisngGuardId(GuardId)
                   .Select(z => KpiTimeSheetScheduleViewModel.FromDataModel(z))
                   .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                   .OrderBy(x => x.ProjectName)
                   .ThenBy(x => x.ClientTypes));
            }
            else if (userId != 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetAllTimesheetSchedulesUsingUserId(userId)
                   .Select(z => KpiTimeSheetScheduleViewModel.FromDataModel(z))
                   .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                   .OrderBy(x => x.ProjectName)
                   .ThenBy(x => x.ClientTypes));
            }
            else
            {
                // Super Admin / Main Admin fallback
                return new JsonResult(_kpiSchedulesDataProvider.GetAllTimesheetSchedules()
                    .Select(z => KpiTimeSheetScheduleViewModel.FromDataModel(z))
                    .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                    .OrderBy(x => x.ProjectName)
                    .ThenBy(x => x.ClientTypes));
            }
        }
        public JsonResult OnGetKpiTimesheetSchedule(int id)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetTimesheetScheduleById(id));
            }
            else
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetTimesheetScheduleByIdandGuardId(id, GuardId));
            }
        }
        public JsonResult OnPostDeleteKpiSendScheduleTimesheet(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _kpiSchedulesDataProvider.DeleteSendScheduleTimesheet(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public IActionResult OnGetDownloadPdfTimesheet(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var schedule = _kpiSchedulesDataProvider.GetTimesheetScheduleById(scheduleId);
            if (schedule == null)
                throw new ArgumentException("Schedule not found");
            // Generate the PDF file
            DateTime date = new DateTime(reportYear, reportMonth, 1);
            string filename = $"{reportYear}{reportMonth.ToString("00")} - {FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Monthly Report - {date.ToString("MMM").ToUpper()} {reportYear}";
            byte[] pdfBytes = _sendScheduleService.ProcessDownloadTimeSheet(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);
            Response.Headers["Content-Disposition"] = $"inline; filename={filename}";
            // Return the PDF file as a download
            return File(pdfBytes, "application/pdf", filename + ".pdf");
        }

        public JsonResult OnPostRunScheduleTimeSheet(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var success = false;
            string message;
            try
            {
                var schedule = _kpiSchedulesDataProvider.GetTimesheetScheduleById(scheduleId);
                if (schedule == null)
                    throw new ArgumentException("Schedule not found");

                var task = _sendScheduleService.ProcessTimeSheetSchedule(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);

                message = task.Result;
                success = !(message.Contains("Error") || message.Contains("Exception"));
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            if (!success)
            {
                _logger.LogError(message);
            }

            return new JsonResult(new { success });
        }

        //code for anpr start
        public JsonResult OnPostANPR(ANPR anpr)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                if (anpr.Apicalls == null)
                {
                    message = "API Calls Required";
                }
                else
                {
                    _guardSettingsDataProvider.SaveANPR(anpr);
                    success = true;
                }

            }

            catch (Exception ex)
            {
                message = ex.Message;
                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "Key number already exists";
                }

            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetANPR(int clientSiteId)
        {

            var clientSiteKeys = _guardSettingsDataProvider.GetANPR(clientSiteId).ToList();

            return new JsonResult(clientSiteKeys);

        }
        public JsonResult OnPostDeleteANPR(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.DeleteANPR(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        //code for anpr stop

        public JsonResult OnGetMobileNo(int Id)
        {
            return new JsonResult(_viewDataService.GetMobileNo(Id));
        }
        public JsonResult OnGetStaffDocsUsingType(int type, string query)
        {
            return new JsonResult(_configDataProvider.GetStaffDocumentsUsingType(type, query));
        }
        public JsonResult OnGetStaffDocsUsingTypeNew(int type, int ClientSiteId)
        {
            return new JsonResult(_configDataProvider.GetStaffDocumentsUsingTypeNew(type, Convert.ToInt32(ClientSiteId)));
        }
        public JsonResult OnPostUploadStaffDocUsingType()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            var fileName = string.Empty;
            int DocumentID = 0;
            string Filepath = string.Empty;

            if (files.Count == 1)
            {
                var file = files[0];
                fileName = file.FileName;
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.docx,.xlsx".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");

                        var staffDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs");
                        if (!Directory.Exists(staffDocsFolder))
                            Directory.CreateDirectory(staffDocsFolder);

                        using (var stream = System.IO.File.Create(Path.Combine(staffDocsFolder, file.FileName)))
                        {
                            file.CopyTo(stream);
                        }
                        var staffDocsUrl = $"{Request.Scheme}://{Request.Host}/StaffDocs/";
                        var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                        var type = Convert.ToInt32(Request.Form["type"]);
                        var ClienSiteID = Convert.ToInt32(Request.Form["ClientSiteID"]);
                        _configDataProvider.SaveStaffDocument(new StaffDocument()
                        {
                            Id = documentId,
                            FileName = file.FileName,
                            LastUpdated = DateTime.Now,
                            DocumentType = type,
                            ClientSite = ClienSiteID,
                            FilePath = staffDocsUrl
                        });

                        success = true;
                        var Details = _configDataProvider.GetStaffDocumentsID(ClienSiteID);
                        DocumentID = _configDataProvider.GetStaffDocumentsID(ClienSiteID).Id;
                        Filepath = _configDataProvider.GetStaffDocumentsID(ClienSiteID).FilePath;

                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }
            return new JsonResult(new { success, message, DocumentID, Filepath, fileName, LastUpdated = DateTime.Now });

        }

        public JsonResult OnPostDeleteStaffDoc(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                var document = _configDataProvider.GetStaffDocuments().SingleOrDefault(x => x.Id == id);
                if (document != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs", document.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);

                    _configDataProvider.DeleteStaffDocument(id);
                }


            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostDeRegisterDevice(int id)
        {
            var status = true;
            var message = "Device has been successfully de-registered.";
            try
            {
                var document = _clientSiteWandDataProvider.DeRegisterDeviceWithSmartWand(id);
                if (!document)
                {
                    status = false;
                    message = "An error occured while de-registering device. Please try after sometime.";
                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostSaveDuressApp(int duressAppId, string positionFilter, int selectedPosition, int siteDuressNumber, int clientSiteIdDuress, int? logProfileId)
        {
            var success = false;
            string message = "Failed to save data.";

            try
            {
                // Check if duressAppId is 0 (Add new) or not (Update existing)
                if (duressAppId == 0)
                {
                    // Add new DuressSetting
                    var setting = new DuressSetting
                    {
                        ClientSiteId = clientSiteIdDuress,
                        PositionFilter = positionFilter,
                        SelectedPosition = selectedPosition,
                        SiteDuressNumber = siteDuressNumber,
                        LogProfileId = logProfileId
                    };
                    success = _configDataProvider.AddDuressSetting(setting);
                    if (success)
                        message = "Duress settings saved successfully.";
                }
                else
                {
                    // Update existing DuressSetting
                    var existingSetting = _configDataProvider.GetDuressSettingById(duressAppId);
                    if (existingSetting != null)
                    {

                        if (existingSetting.SiteDuressNumber == siteDuressNumber)
                        {
                            existingSetting.PositionFilter = positionFilter;
                            existingSetting.SelectedPosition = selectedPosition;
                            existingSetting.SiteDuressNumber = siteDuressNumber;
                            existingSetting.LogProfileId = logProfileId;
                            success = _configDataProvider.UpdateDuressSetting(existingSetting);
                            if (success)
                                message = "Duress settings updated successfully.";
                            else
                                message = "Failed to update Duress settings.";

                        }
                        else
                        {
                            var setting = new DuressSetting
                            {
                                ClientSiteId = clientSiteIdDuress,
                                PositionFilter = positionFilter,
                                SelectedPosition = selectedPosition,
                                SiteDuressNumber = siteDuressNumber,
                                LogProfileId = logProfileId
                            };
                            success = _configDataProvider.AddDuressSetting(setting);
                            if (success)
                                message = "Duress settings saved successfully.";
                        }
                    }
                    else
                    {
                        var setting = new DuressSetting
                        {
                            ClientSiteId = clientSiteIdDuress,
                            PositionFilter = positionFilter,
                            SelectedPosition = selectedPosition,
                            SiteDuressNumber = siteDuressNumber,
                            LogProfileId = logProfileId
                        };
                        success = _configDataProvider.AddDuressSetting(setting);
                        if (success)
                            message = "Duress settings saved successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                _logger.LogError(message);
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetGetDuressApp(int clientSiteIdDuress, int siteDuressNumber)
        {
            try
            {
                // Fetch the duress setting from the data provider
                var clientSiteKpiNote = _configDataProvider.GetDuressSetting(clientSiteIdDuress, siteDuressNumber);

                if (clientSiteKpiNote == null)
                {
                    // Return a custom error message if no record is found
                    return new JsonResult(new { success = false, message = "No duress setting found for the specified site and duress number." });
                }

                // Return the successful result
                return new JsonResult(new { success = true, data = clientSiteKpiNote });
            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                _logger.LogError(ex, "An error occurred while fetching the duress setting.");

                // Return an error message
                return new JsonResult(new { success = false, message = "An error occurred while processing your request. Please try again later." });
            }
        }


        public JsonResult OnPostDeleteDuressApp(int duressAppId)
        {
            try
            {
                // Attempt to delete the duress setting
                bool isDeleted = _configDataProvider.DeleteDuressSettingById(duressAppId);

                if (!isDeleted)
                {
                    // Return a custom error message if no record is found
                    return new JsonResult(new { success = false, message = "No duress setting found with the specified ID." });
                }

                // Return the successful result
                return new JsonResult(new { success = true, message = "Duress setting deleted successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                _logger.LogError(ex, "An error occurred while deleting the duress setting.");

                // Return an error message
                return new JsonResult(new { success = false, message = "An error occurred while processing your request. Please try again later." });
            }
        }

        public JsonResult OnPostSaveClientSiteMobileAppCrowdSettings(ClientSiteMobileAppSettings csmas)
        {
            try
            {

                if (csmas.ClientSiteId != 0)
                {
                    // Check if settings already exists
                    var existingSetting = _configDataProvider.GetCrowdSettingForSite(csmas.ClientSiteId);
                    if (existingSetting != null)
                    {
                        //Update changes
                        existingSetting = _configDataProvider.UpdateCrowdSettingForSite(csmas);
                        return new JsonResult(new { success = true, message = "Crowd setting saved successfully.", clientSiteMobileAppSettings = existingSetting });
                    }
                    else
                    {
                        //New record
                        var newrecord = _configDataProvider.SaveCrowdSettingForSite(csmas);
                        return new JsonResult(new { success = true, message = "Crowd setting saved successfully.", clientSiteMobileAppSettings = newrecord });

                    }
                }
                else
                {
                    // Return a custom error message if no record is found
                    return new JsonResult(new { success = false, message = "No client site found with the specified ID." });
                }

            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                _logger.LogError(ex, "An error occurred while saving the crowd setting.");

                // Return an error message
                return new JsonResult(new { success = false, message = "An error occurred while processing your request. Please try again later." });
            }
        }

        //wand tags-start
        public JsonResult OnGetWandTagsSettings(int clientSiteId)
        {
            return new JsonResult(_clientSiteWandDataProvider.GetClientSiteSmartWandTags().Where(z => z.ClientSiteId == clientSiteId).OrderBy(x => x.LabelDescription).ToList());
        }
        public JsonResult OnGetTagType()
        {
            return new JsonResult(_configDataProvider.GetSmartWandTagsType());
        }
        public JsonResult OnPostSmartWandTagsSettings(ClientSiteSmartWandTags record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.SaveClientSiteSmartWandTags(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success = success, message = message });
        }
        public JsonResult OnPostDeleteSmartWandTagSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.DeleteClientSiteSmartWandTags(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostSaveClientSiteMobileAppPatrolTouringModeSettings(int clientSiteId, PatrolTouringMode PatrolTourMode)
        {
            try
            {

                if (clientSiteId != 0)
                {
                    _clientDataProvider.SaveClientSitePatrolTourSettings(clientSiteId, PatrolTourMode);
                    return new JsonResult(new { success = true, message = "Settings saved successfully." });
                }
                else
                {
                    // Return a custom error message if no record is found
                    return new JsonResult(new { success = false, message = "No client site found with the specified ID." });
                }

            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                _logger.LogError(ex, "An error occurred while saving the Patrol Touring Mode setting.");

                // Return an error message
                return new JsonResult(new { success = false, message = "An error occurred while processing your request. Please try again later." });
            }
        }
        //wand tags-end
        //kv-scheudle-start


        public JsonResult OnPostSavemobAppShowClientTypeandSiteSettings(int clientSiteId, bool mobAppShowClientTypeandSite)
        {
            try
            {
                if (clientSiteId != 0)
                {
                    _clientDataProvider.SavemobAppShowClientTypeandSiteSettings(clientSiteId, mobAppShowClientTypeandSite);
                    return new JsonResult(new { success = true, message = "Settings saved successfully." });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No client site found with the specified ID." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving MobAppShowClientTypeandSite settings.");

                return new JsonResult(new { success = false, message = "An error occurred while processing your request. Please try again later." });
            }
        }

        public JsonResult OnGetKpiKVSchedules()
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
            if (userId == 0 && Request.Query.ContainsKey("uId"))
            {
                if (int.TryParse(Request.Query["uId"], out int parsedUId))
                {
                    userId = parsedUId;
                }
            }

            if (GuardId != 0)
            {
                // Wait, KV doesn't have GuardId method in existing code? Let's just fallback or skip it? No, wait! The original code for KV didn't even check GuardId! It just returned GetAllKVSchedules.
                // We'll fallback to GetAllKVSchedules for GuardId != 0 as well to preserve existing behavior for guards, or let's use the new UserId check.
                return new JsonResult(_kpiSchedulesDataProvider.GetAllKVSchedules()
                    .OrderBy(x => x.ProjectName));
            }
            else if (userId != 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetAllKVSchedulesUsingUserId(userId)
                    .OrderBy(x => x.ProjectName));
            }
            else
            {
                // Super Admin fallback
                return new JsonResult(_kpiSchedulesDataProvider.GetAllKVSchedules()
                    .OrderBy(x => x.ProjectName));
            }
        }


        public JsonResult OnGetPCARProfiles()
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            userId = HttpContext.Session.GetInt32("loginUserId") ?? 0;
            if (userId == 0 && Request.Query.ContainsKey("uId"))
            {
                if (int.TryParse(Request.Query["uId"], out int parsedUId))
                {
                    userId = parsedUId;
                }
            }

            if (userId != 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetPCARProfilesUsingUserId(userId));
            }
            else
            {
                // Super Admin / Guard fallback (Original code didn't filter by GuardId for PCAR Routes)
                return new JsonResult(_kpiSchedulesDataProvider.GetPCARProfilesAll());
            }
        }

        public JsonResult OnGetPCARRouteDetails(int id)
        {

            return (_configDataProvider.GetPCARRouteDetails(id));

        }

        public JsonResult OnPostRemovePcarRouteSites([FromBody] RemoveRouteSitesModel model)
        {
            var result = _configDataProvider.RemovePCarDeatils(model.PcarRouteId, model.ClientSiteIds);

            return new JsonResult(new { success = result });
        }

        public class RemoveRouteSitesModel
        {
            public int PcarRouteId { get; set; }
            public List<int> ClientSiteIds { get; set; }
        }

        public JsonResult OnPostSaveKpiKVSchedule(KpiKVScheduleViewModel kpiSendKVViewModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(kpiSendKVViewModel, new ValidationContext(kpiSendKVViewModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            var success = true;
            var message = "Saved successfully";
            try
            {
                var kpiSendSchedule = KpiKVScheduleViewModel.ToDataModel(kpiSendKVViewModel);
                if (kpiSendSchedule.Id == 0)
                    kpiSendSchedule.NextRunOn = KpiKVScheduleRunOnCalculator.GetNextRunOn(kpiSendSchedule);
                else
                    kpiSendSchedule.NextRunOn = KpiKVScheduleRunOnCalculator.GetNextRunOnUpdate(kpiSendSchedule);
                _kpiSchedulesDataProvider.SaveKVSchedule(kpiSendSchedule, true);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetKVCompanyDetails(string clientSiteIds, string searchKeyNo)
        {
            var arClientSiteIds = clientSiteIds?.Split(";").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            return new JsonResult(_configDataProvider.GetCompanyDetailsUsingFilter(arClientSiteIds, ""));

        }
        public JsonResult OnGetClientSiteLocationsAndCompanyDetails(string clientSiteIds)
        {
            var siteLocations = new List<SelectListItem>();

            var arClientSiteIds = clientSiteIds.Split(";").Select(z => int.Parse(z)).ToArray();

            siteLocations = _clientViewDataService.GetClientSiteLocationsNew(arClientSiteIds);
            var companyDetails = _configDataProvider.GetCompanyDetailsUsingFilter(arClientSiteIds, "");
            var clientSiteKeys = _guardSettingsDataProvider.GetClientSiteKeysFilter(arClientSiteIds).ToList();
            return new JsonResult(new { siteLocations, companyDetails, clientSiteKeys });
        }
        public JsonResult OnGetKpiKVSchedule(int id)
        {

            return new JsonResult(_kpiSchedulesDataProvider.GetAllKVSchedules().Where(x => x.Id == id).FirstOrDefault());
        }
        public JsonResult OnPostDeleteKpiSendScheduleKV(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _kpiSchedulesDataProvider.DeleteSendScheduleKV(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }
        public JsonResult OnPostRunScheduleKV(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var success = false;
            string message;
            try
            {
                var schedule = _kpiSchedulesDataProvider.GetKVScheduleById(scheduleId);
                if (schedule == null)
                    throw new ArgumentException("Schedule not found");

                var task = _sendScheduleService.ProcessKVSchedule(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);

                message = task.Result;
                success = !(message.Contains("Error") || message.Contains("Exception"));
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            if (!success)
            {
                _logger.LogError(message);
            }

            return new JsonResult(new { success });
        }
        public IActionResult OnGetDownloadPdfKV(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var schedule = _kpiSchedulesDataProvider.GetKVScheduleById(scheduleId);
            if (schedule == null)
                throw new ArgumentException("Schedule not found");
            // Generate the PDF file
            DateTime date = new DateTime(reportYear, reportMonth, 1);
            string filename = $"{reportYear}{reportMonth.ToString("00")} - {FileNameHelper.GetSanitizedFileNamePart(schedule.ProjectName)} - Monthly Report - {date.ToString("MMM").ToUpper()} {reportYear}";
            byte[] pdfBytes = _sendScheduleService.ProcessDownloadKVSchedule(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);
            Response.Headers["Content-Disposition"] = $"inline; filename={filename}";
            // Return the PDF file as a download
            return File(pdfBytes, "application/pdf", filename + ".pdf");
        }


        //-kvscheudele-end
        public JsonResult OnGetPreviousMonthSummaryNote(int scheduleId, int month, int year)
        {


            var kpiSendScheduleNotes = _kpiSchedulesDataProvider.GetSendScheduleById(scheduleId);
            var kpiScheduleNote = kpiSendScheduleNotes.KpiSendScheduleSummaryNotes.SingleOrDefault(z => z.ForMonth == new DateTime(year, month, 1).AddMonths(-1));

            return new JsonResult(kpiScheduleNote.Notes);
        }
        public JsonResult OnGetPreviousMonthKPINotesAndHRRecords(int clientSiteId, int month, int year)
        {
            var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            var clientSiteKpiNote = kpiSetting.Notes.SingleOrDefault(z => z.ForMonth == new DateTime(year, month, 1).AddMonths(-1));
            clientSiteKpiNote ??= new ClientSiteKpiNote()
            {
                ForMonth = new DateTime(year, month, 1),
                SettingsId = kpiSetting.Id,
                Notes = string.Empty,
                HRRecords = string.Empty
            };
            return new JsonResult(clientSiteKpiNote);
        }
        public JsonResult OnGetEquipments()
        {
            return new JsonResult(_guardLogDataProvider.GetKPITelemarics(2).OrderBy(x => x.Name).ToList());
        }
        public JsonResult OnGetSiteEquipmentSettings(int clientSiteId)
        {
            return new JsonResult(_clientSiteWandDataProvider.GetClientSiteEquipments().Where(z => z.ClientSiteId == clientSiteId).OrderBy(x => x.KPITelematicsField.Name).ToList());
        }
        public JsonResult OnPostSiteEquipmentSettings(SiteEquipmentsDetails record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                if (record.Equipment != null)
                {
                    record.EquipmentId = _guardLogDataProvider.GetKPITelemarics(2).SingleOrDefault(x => x.Name == record.Equipment).Id;
                }
                if (record.Id < 0)
                {
                    record.Id = 0;
                }
                _clientSiteWandDataProvider.SaveClientSiteEquipments(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success = success, message = message });
        }
        public JsonResult OnPostDeleteSiteEquipmentSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.DeleteClientSiteEquipments(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        public void OnPostSaveSiteEmailBasedOnLogs(int siteId,
            bool enableLBLogDump, bool enableKVLogDump, bool enableSWLogDump, bool uploadFusionLog, string guardEmailTo,
            bool enableLBWeeklyLogDump, bool enableKVWeeklyLogDump, bool enableSWWeeklyLogDump, bool uploadFusionWeeklyLog, string guardEmailWeeklyLogTo,
            bool enableLBMonthlyLogDump, bool enableKVMonthlyLogDump, bool enableSWMonthlyLogDump, bool uploadFusionMonthlyLog, string guardEmailMonthlyLogTo)
        {
            var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(z => z.Id == siteId);
            if (clientSite != null)
            {

                clientSite.UploadGuardLog = enableLBLogDump;
                clientSite.UploadKVLog = enableKVLogDump;
                clientSite.UploadSWLog = enableSWLogDump;
                clientSite.UploadFusionLog = uploadFusionLog;
                clientSite.GuardLogEmailTo = guardEmailTo;

                clientSite.UploadGuardWeeklyLog = enableLBWeeklyLogDump;
                clientSite.UploadKVWeeklyLog = enableKVWeeklyLogDump;
                clientSite.UploadSWWeeklyLog = enableSWWeeklyLogDump;
                clientSite.UploadFusionWeeklyLog = uploadFusionWeeklyLog;
                clientSite.GuardLogEmailWeeklyLogTo = guardEmailWeeklyLogTo;

                clientSite.UploadGuardMonthlyLog = enableLBMonthlyLogDump;
                clientSite.UploadKVMonthlyLog = enableKVMonthlyLogDump;
                clientSite.UploadSWMonthlyLog = enableSWMonthlyLogDump;
                clientSite.UploadFusionMonthlyLog = uploadFusionMonthlyLog;
                clientSite.GuardLogEmailMonthlyLogTo = guardEmailMonthlyLogTo;
            }

            _clientDataProvider.SaveClientSite(clientSite);
        }
    }


}
