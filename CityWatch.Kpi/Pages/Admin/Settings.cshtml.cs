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
            ILogger<SettingsModel> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _viewDataService = viewDataService;
            _importDataService = importDataService;
            _importJobDataProvider = importJobDataProvider;
            _clientDataProvider = clientDataProvider;
            _kpiSchedulesDataProvider = kpiSchedulesDataProvider;
            _sendScheduleService = sendScheduleService;
            _logger = logger;
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
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id)
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
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id)
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

                       var InvalidTimes=_clientDataProvider.ValidDateTime(clientSiteKpiSetting);

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

                if(!string.IsNullOrEmpty(imageName))
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

    }
}