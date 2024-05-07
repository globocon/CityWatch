using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Models;
using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.ComponentModel.Design;
using Microsoft.Data.SqlClient;
using System.Net.NetworkInformation;

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

        [BindProperty]
        public KpiRequest ReportRequest { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IImportJobDataProvider ImportJobDataProvider { get { return _importJobDataProvider; } }
        public int GuardId { get; set; }
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
             IGuardDataProvider guardDataProvider)
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
        }

        public IActionResult OnGet()
        {
            ReportRequest = new KpiRequest();
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            var claimsIdentity = User.Identity as ClaimsIdentity;
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {   /* admin login only*/
                ReportRequest = new KpiRequest();
                HttpContext.Session.SetInt32("GuardId", 0);
                return Page();
            }
            else if (GuardId != 0)
            {
                /* When guard Login*/
                HttpContext.Session.SetInt32("GuardId", GuardId);
                return Page();
            }
            else
            {
                /*unauthorized login*/
                HttpContext.Session.SetInt32("GuardId", 0);
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

        public JsonResult OnGetClientSiteWithSettings(string type, string searchTerm)
        {
            if (!string.IsNullOrEmpty(type) || !string.IsNullOrEmpty(searchTerm))
            {
                var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == type);
                if (clientType != null)
                {
                    var clientSites = _clientDataProvider.GetUserClientSites(type, searchTerm);
                    GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    if (GuardId != 0)
                        clientSites = _clientDataProvider.GetClientSitesUsingGuardId(GuardId);
                    var clientSiteWithSettings = _clientDataProvider.GetClientSiteKpiSettings().Select(z => z.ClientSiteId).ToList();

                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        SiteUploadDailyLog = z.UploadGuardLog,
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id),
                        z.SiteEmail,
                        z.LandLine,
                        z.GuardLogEmailTo,
                        z.DataCollectionEnabled
                    }));

                }
                else
                {
                    var clientSites = _clientDataProvider.GetUserClientSites(type, searchTerm);
                    GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
                    if (GuardId != 0)
                        clientSites = _clientDataProvider.GetClientSitesUsingGuardId(GuardId);
                    var clientSiteWithSettings = _clientDataProvider.GetClientSiteKpiSettings().Select(z => z.ClientSiteId).ToList();

                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id),
                       z.SiteEmail
                    }));
                }
            }
            return new JsonResult(new { });
        }

        //code added to search client name start
        public PartialViewResult OnGetClientSiteKpiSettings(int siteId)
        {
            var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
            clientSiteKpiSetting ??= new ClientSiteKpiSetting() { ClientSiteId = siteId };
            return Partial("_ClientSiteKpiSetting", clientSiteKpiSetting);
        }

        public JsonResult OnPostClientSiteKpiSettings(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = true;
            try
            {
                // Default TuneDowngradeBuffer = 1
                if (clientSiteKpiSetting.TuneDowngradeBuffer.GetValueOrDefault() == 0)
                    clientSiteKpiSetting.TuneDowngradeBuffer = 1;

                _clientDataProvider.SaveClientSiteKpiSetting(clientSiteKpiSetting);
            }
            catch
            {
                success = false;
            }

            return new JsonResult(success);
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
                                success = _clientDataProvider.SaveClientSiteManningKpiSetting(clientSiteKpiSetting);
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
            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    fileName = file.FileName;
                    var scheduleId = Convert.ToInt32(Request.Form["scheduleId"]);

                    var summaryImageDir = Path.Combine(_webHostEnvironment.WebRootPath, "RCImage");
                    if (!Directory.Exists(summaryImageDir))
                        Directory.CreateDirectory(summaryImageDir);

                    using (var stream = System.IO.File.Create(Path.Combine(summaryImageDir, fileName)))
                    {
                        file.CopyTo(stream);
                    }
                    //_kpiSchedulesDataProvider.SaveKpiSendScheduleSummaryImage(scheduleId, fileName);
                }
            }
            catch
            {
                success = false;
            }

            return new JsonResult(new { success, fileName, LastUpdated = DateTime.Now });
        }

        public JsonResult OnGetKpiSendSchedules(int type, string searchTerm)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedules()
                    .Select(z => KpiSendScheduleViewModel.FromDataModel(z))
                    .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                    .OrderBy(x => x.ProjectName)
                    .ThenBy(x => x.ClientTypes));

            }
            else
            {

                return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedulesUisngGuardId(GuardId)
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
                Notes = string.Empty
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
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "RCImage", imageName);
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

        public IActionResult OnGetDownloadPdf(int scheduleId, int reportYear, int reportMonth, bool ignoreRecipients)
        {
            var schedule = _kpiSchedulesDataProvider.GetSendScheduleById(scheduleId);
            if (schedule == null)
                throw new ArgumentException("Schedule not found");
            // Generate the PDF file
            byte[] pdfBytes = _sendScheduleService.ProcessDownload(schedule, new DateTime(reportYear, reportMonth, 1), ignoreRecipients, false);
            Response.Headers["Content-Disposition"] = "inline; filename=" + "CityWatch_Schedule_" + reportYear + "_" + reportMonth + "_Doc";
            // Return the PDF file as a download
            return File(pdfBytes, "application/pdf", "CityWatch_Schedule_" + reportYear + "_" + reportMonth + "_Doc.pdf");
        }
        //Menu chage -start
        public JsonResult OnGetSmartWandSettings(int clientSiteId)
        {
            return new JsonResult(_clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSiteId).ToList());
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

        public void OnPostSaveSiteEmail(int siteId, string siteEmail, bool enableLogDump, string landLine, string guardEmailTo, string duressEmail, string duressSms)
        {
            var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(z => z.Id == siteId);
            if (clientSite != null)
            {
                clientSite.SiteEmail = siteEmail;
                clientSite.UploadGuardLog = enableLogDump;
                clientSite.LandLine = landLine;
                clientSite.GuardLogEmailTo = guardEmailTo;
                clientSite.DuressEmail = duressEmail;
                clientSite.DuressSms = duressSms;
            }

            _clientDataProvider.SaveClientSite(clientSite);
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
            return new JsonResult(_guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).ToList());
        }
        public JsonResult OnPostClientSiteKey(ClientSiteKey clientSiteKey)
        {
            var success = false;
            var message = string.Empty;
            try
            {
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
            int sendertoggleTypeId, bool senderIsActive, int reelstoggleTypeId, bool reelsIsActive,int trailerRegoTypeId,bool isISOVINAcive)
        {

            _clientDataProvider.SaveClientSiteToggle(siteId, timeslottoggleTypeId, timeslotIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, vwitoggleTypeId, vwiIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, sendertoggleTypeId, senderIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, reelstoggleTypeId, reelsIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, trailerRegoTypeId, isISOVINAcive);
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
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));
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

    }

}
