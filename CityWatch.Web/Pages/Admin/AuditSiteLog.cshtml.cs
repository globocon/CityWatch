using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Pages.Admin
{
    public class AuditSiteLogModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardLogZipGenerator _guardLogZipGenerator;
        private readonly IAuditLogViewDataService _auditLogViewDataService;
        private readonly IClientSiteViewDataService _clientViewDataService;
        private readonly ITimesheetReportGenerator _TimesheetReportGenerator;

        public AuditSiteLogModel(IViewDataService viewDataService,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardLogZipGenerator guardLogZipGenerator,
            IAuditLogViewDataService auditLogViewDataService,
            IClientSiteViewDataService clientViewDataService,
            ITimesheetReportGenerator TimesheetReportGenerator)
        {
            _viewDataService = viewDataService;
            _guardLogDataProvider = guardLogDataProvider;
            _guardLogZipGenerator = guardLogZipGenerator;
            _auditLogViewDataService = auditLogViewDataService;
            _clientViewDataService = clientViewDataService;
            _TimesheetReportGenerator = TimesheetReportGenerator;
        }

        public KeyVehicleLogAuditLogRequest KeyVehicleLogAuditLogRequest { get; set; }
        public string loggedInUserId { get; set; }
        public int GuardId { get; set; }
        public GuardViewModel Guard { get; set; }

        public ActionResult OnGet()
        {
            string securityLicenseNonew = Request.Query["Sl"];
            string guid = Request.Query["guid"];
            string luid = Request.Query["lud"];
            GuardId = Convert.ToInt32(guid);
            loggedInUserId = luid;
            if (GuardId != 0)
            {
                Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);

            }
            /* Normal admin,PowerAdmin and AdminGlobal can access this page */
            if (!AuthUserHelper.IsAdminUserLoggedIn && !AuthUserHelper.IsAdminGlobal && !AuthUserHelper.IsAdminPowerUser && !AuthUserHelper.IsAdminThirdParty && !Guard.IsAdminInvestigatorAccess && !Guard.IsAdminAuditorAccess)
                return Redirect(Url.Page("/Account/Unauthorized"));

            return Page();
        }

        public IActionResult OnGetKeyVehicleLogProfile(int id)
        {
            var keyVehicleLogProfile = _guardLogDataProvider.GetKeyVehicleLogProfileWithPersonalDetails(id);
            keyVehicleLogProfile ??= new KeyVehicleLogVisitorPersonalDetail() { Id = id, KeyVehicleLogProfile = new KeyVehicleLogProfile() };
            ViewData["KeyVehicleLog_AuditHistory"] = _viewDataService.GetKeyVehicleLogAuditHistory(keyVehicleLogProfile.ProfileId).ToList();

            return new PartialViewResult
            {
                ViewName = "_KeyVehicleLogProfilePopup",
                ViewData = new ViewDataDictionary<KeyVehicleLogVisitorPersonalDetail>(ViewData, keyVehicleLogProfile)
            };
        }

        public JsonResult OnGetDailyGuardSiteLogs(int pageNo, int limit, int clientSiteId,
                                                    DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            var start = (pageNo - 1) * limit;
            var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs);
            var records = dailyGuardLogs.Skip(start).Take(limit).ToList();
            return new JsonResult(new { records, total = dailyGuardLogs.Count });
        }

        public IActionResult OnPostKeyVehicleSiteLogs(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest)
        {
            //return new JsonResult(_auditLogViewDataService.GetKeyVehicleLogs(keyVehicleLogAuditLogRequest));
           
            var keyVehicleAuditLogRequest = _auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequest);
            //duress entries per week-start
             var today = keyVehicleLogAuditLogRequest.LogFromDate;
            var todate= keyVehicleLogAuditLogRequest.LogToDate;
            var todaynew = keyVehicleLogAuditLogRequest.LogFromDate;
            var todatenew = keyVehicleLogAuditLogRequest.LogToDate;
            var kvtruckentriesForWeekNew = new List<KeyVehicleLogAuditLogRequest>();
            int kvtruckentriesForWeekNewCountnew = 0;
            TimeSpan ts = keyVehicleLogAuditLogRequest.LogToDate.Subtract(today);
            int dateDiff = ts.Days;
            int totalWeeks = (int)dateDiff / 7;
            KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequestnew = new KeyVehicleLogAuditLogRequest();
            keyVehicleLogAuditLogRequestnew = keyVehicleLogAuditLogRequest;
            for (int i = 1; i <= totalWeeks; i++)
            {
                keyVehicleLogAuditLogRequest.LogToDate = todatenew;

                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
                if (thisWeekStart < today)
                {
                    thisWeekStart = today;
                }

                if (thisWeekEnd > keyVehicleLogAuditLogRequest.LogToDate)
                {
                    thisWeekEnd = keyVehicleLogAuditLogRequest.LogToDate;
                }
                keyVehicleLogAuditLogRequestnew.LogFromDate = thisWeekStart;
                keyVehicleLogAuditLogRequestnew.LogToDate = thisWeekEnd;
                var kvtruckentriesTypeforWeek = _auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequestnew);
                string newdaterange = thisWeekStart.ToString("dd-MM-yyy") + " to " + thisWeekEnd.ToString("dd-MM-yyy");
                KeyVehicleLogAuditLogRequest obj = new KeyVehicleLogAuditLogRequest();
                obj.DateRange = newdaterange;
                obj.RecordCount = kvtruckentriesTypeforWeek.Count();
                kvtruckentriesForWeekNew.Add(obj);
                kvtruckentriesForWeekNewCountnew = kvtruckentriesForWeekNewCountnew + obj.RecordCount;
                today = thisWeekEnd.AddDays(1);

            }
            var kvtruckentriesForWeekNewCount = kvtruckentriesForWeekNewCountnew;
            //duress entries per week-end
            //duress entries per month-start
            keyVehicleLogAuditLogRequest.LogFromDate= todaynew;
            keyVehicleLogAuditLogRequest.LogToDate= todatenew;
            today = todaynew;
            todate = todatenew;
            var kvtruckentriesForMonthNew = new List<KeyVehicleLogAuditLogRequest>();
            int kvtruckentriesForMonthNewCountnew = 0;

            //int months = (int)(ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Month);
            int months = (keyVehicleLogAuditLogRequest.LogToDate.Year * 12 + keyVehicleLogAuditLogRequest.LogToDate.Month) - (keyVehicleLogAuditLogRequest.LogFromDate.Year * 12 + keyVehicleLogAuditLogRequest.LogFromDate.Month) + 1;
            for (int i = 1; i <= months; i++)
            {

                var thisMonthStart = new DateTime(today.Year, today.Month, 1);
                var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
                keyVehicleLogAuditLogRequestnew.LogFromDate = thisMonthStart;
                keyVehicleLogAuditLogRequestnew.LogToDate = thisMonthEnd;
                var kvtruckentriesTypeforMonth = _auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequestnew);
                string newdaterange = thisMonthStart.ToString("MMM");
                KeyVehicleLogAuditLogRequest obj = new KeyVehicleLogAuditLogRequest();
                obj.DateRange = newdaterange;
                obj.RecordCount = kvtruckentriesTypeforMonth.Count();
                kvtruckentriesForMonthNew.Add(obj);
                kvtruckentriesForMonthNewCountnew = kvtruckentriesForMonthNewCountnew + obj.RecordCount;
                today = thisMonthEnd.AddDays(1);

            }
            var kvtruckentriesForMonthNewCount = kvtruckentriesForMonthNewCountnew;
            //duress entries per month-end

            //duress entries per year-start
            keyVehicleLogAuditLogRequest.LogFromDate = todaynew;
            keyVehicleLogAuditLogRequest.LogToDate = todatenew;
            today = todaynew;
            todate = todatenew;

            var kvtruckentriesForYearNew = new List<KeyVehicleLogAuditLogRequest>();
            int kvtruckentriesForYearNewCountnew = 0;

            int years = (int)(keyVehicleLogAuditLogRequest.LogToDate.Year - keyVehicleLogAuditLogRequest.LogFromDate.Year) +
        (((keyVehicleLogAuditLogRequest.LogToDate.Month > keyVehicleLogAuditLogRequest.LogFromDate.Month) ||
        ((keyVehicleLogAuditLogRequest.LogToDate.Month == keyVehicleLogAuditLogRequest.LogFromDate.Month) && (keyVehicleLogAuditLogRequest.LogToDate.Day >= keyVehicleLogAuditLogRequest.LogFromDate.Day))) ? 1 : 0);

            for (int i = 1; i <= years; i++)
            {

                var thisYearStart = new DateTime(today.Year, 1, 1);
                var thisYearEnd = new DateTime(today.Year, 12, 1);
                keyVehicleLogAuditLogRequestnew.LogFromDate = thisYearStart;
                keyVehicleLogAuditLogRequestnew.LogToDate = thisYearEnd;
                var kvtruckentriesTypeforYear = _auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequestnew);
                string newdaterange = thisYearStart.Year.ToString();
                KeyVehicleLogAuditLogRequest obj = new KeyVehicleLogAuditLogRequest();
                obj.DateRange = newdaterange;
                obj.RecordCount = kvtruckentriesTypeforYear.Count();
                kvtruckentriesForYearNew.Add(obj);
                kvtruckentriesForYearNewCountnew = kvtruckentriesForYearNewCountnew + obj.RecordCount;
                today = new DateTime(today.Year + 1, 1, 1);

            }
            var kvtruckentriesForYearNewCount = kvtruckentriesForYearNewCountnew;


            //duress entries per year-end
            return new JsonResult(new { keyVehicleAuditLogRequest, chartData = new { kvtruckentriesForWeekNew, kvtruckentriesForMonthNew, kvtruckentriesForYearNew }, kvtruckentriesForWeekNewCount, kvtruckentriesForMonthNewCount, kvtruckentriesForYearNewCount });
        }

        /*
         *  TODO: Remove this unused handler
            public JsonResult OnGetGuardLogBookId(int clientSiteId, LogBookType logBookType, DateTime eventDate)
            {
                var logBookId = _clientDataProvider.GetClientSiteLogBook(clientSiteId, logBookType, eventDate)?.Id;
                return new JsonResult(new { success = true, logBookId });
            }
        */

        public JsonResult OnPostDownloadDailyGuardLogZip(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                zipFileName = _guardLogZipGenerator.GenerateZipFile(new int[] { clientSiteId }, logFromDate, logToDate, LogBookType.DailyGuardLog).Result;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;

                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
            }

            return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
        }

        public JsonResult OnPostDownloadKeyVehicleLogZip(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                zipFileName = _guardLogZipGenerator.GenerateZipFile(keyVehicleLogAuditLogRequest);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;

                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
            }

            return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
        }

        //public JsonResult OnGetKeyVehicleLogProfiles(string truckRego)
        //{
        //    return new JsonResult(_viewDataService.GetKeyVehicleLogProfilesByRego(truckRego));
        //}

        //to check with bdm-start
        public JsonResult OnGetKeyVehicleLogProfiles(string truckRego, string poi)
        {
            return new JsonResult(_viewDataService.GetKeyVehicleLogProfilesByRego(truckRego, poi));
        }
        //to check with bdm-end
        public JsonResult OnPostUpdateKeyVehicleLogProfile(KeyVehicleLogVisitorPersonalDetail keyVehicleLogVisitorPersonalDetail)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(keyVehicleLogVisitorPersonalDetail, new ValidationContext(keyVehicleLogVisitorPersonalDetail), results, true))
                return new JsonResult(new { success = false, errors = results.Select(z => z.ErrorMessage) });

            if (keyVehicleLogVisitorPersonalDetail.Id == 0 &&
                _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(keyVehicleLogVisitorPersonalDetail.KeyVehicleLogProfile.VehicleRego)
                                        .Any(z => z.Equals(keyVehicleLogVisitorPersonalDetail)))
            {
                return new JsonResult(new { success = false, errors = new List<string>() { "Another entry with same attributes exists" } });
            }
            if (keyVehicleLogVisitorPersonalDetail.Id == 0)
            {
                keyVehicleLogVisitorPersonalDetail.IsBDM = true;
            }
            var status = true;
            var message = "success";
            try
            {
                _guardLogDataProvider.SaveKeyVehicleLogProfileWithPersonalDetail(keyVehicleLogVisitorPersonalDetail);
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message;
            }
            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostDeleteKeyVehicleLogProfile(int id)
        {
            var status = true;
            var message = "success";
            try
            {
                _guardLogDataProvider.DeleteKeyVehicleLogPersonalDetails(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message;
            }
            return new JsonResult(new { status, message });
        }

        public JsonResult OnGetVehicleRegos(string q)
        {
            return new JsonResult(_viewDataService.VehicleRegos.Where(z => string.IsNullOrEmpty(q) || z.Value.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }
        public JsonResult OnGetPOIBDMSupplier(string q)
        {
            return new JsonResult(_viewDataService.POIBDMSupplier.Where(z => string.IsNullOrEmpty(q) || z.Value.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        public JsonResult OnGetClientSites(string types)
        {
            return new JsonResult(_clientViewDataService.GetUserClientSitesWithId(types).OrderBy(z => z.Text));
        }

        public JsonResult OnGetClientSiteLocationsAndPocs(string clientSiteIds)
        {
            var siteLocations = new List<SelectListItem>();
            var sitePocs = new List<SelectListItem>();
            var arClientSiteIds = clientSiteIds.Split(";").Select(z => int.Parse(z)).ToArray();

            siteLocations = _clientViewDataService.GetClientSiteLocationsNew(arClientSiteIds);
            sitePocs = _clientViewDataService.GetClientSitePocsNew(arClientSiteIds);

            return new JsonResult(new { siteLocations, sitePocs });
        }

        public JsonResult OnGetClientSiteKeys(string clientSiteIds, string searchKeyNo)
        {
            var arClientSiteIds = clientSiteIds?.Split(";").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            return new JsonResult(_clientViewDataService.GetClientSiteKeys(arClientSiteIds, searchKeyNo));
        }

        public JsonResult OnGetGuardData(int id)

        {
            // return new JsonResult(_viewDataService.GetGuards().SingleOrDefault(z => z.Id == id));
            return new JsonResult(_guardLogDataProvider.GetGuardsWtihProviderNumber(id));
        }
        //to get audit log-start
        //public JsonResult OnGetAuditHistory(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest)
        //{
        //    return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistory(keyVehicleLogAuditLogRequest.VehicleRego).Where(x => keyVehicleLogAuditLogRequest.ClientSiteIds.Contains(x.GuardLogin.ClientSiteId)).ToList());
        //}
        public JsonResult OnGetAuditHistory(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest)
        {
            //(string.IsNullOrEmpty(kvlRequest.VehicleRego) || string.Equals(z.VehicleRego, kvlRequest.VehicleRego, StringComparison.OrdinalIgnoreCase)) &&


            //return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistory().Where(x => keyVehicleLogAuditLogRequest.ClientSiteIds.Contains(x.GuardLogin.ClientSiteId)
            //&& ((string.IsNullOrEmpty(keyVehicleLogAuditLogRequest.VehicleRego) || string.Equals(x.KeyVehicleLog.VehicleRego, keyVehicleLogAuditLogRequest.VehicleRego, StringComparison.OrdinalIgnoreCase))
            //&& (string.IsNullOrEmpty(keyVehicleLogAuditLogRequest.KeyNo) || string.Equals(x.KeyVehicleLog.KeyNo, keyVehicleLogAuditLogRequest.KeyNo, StringComparison.OrdinalIgnoreCase))
            // && (string.IsNullOrEmpty(keyVehicleLogAuditLogRequest.PersonName) || string.Equals(x.KeyVehicleLog.PersonName, keyVehicleLogAuditLogRequest.PersonName, StringComparison.OrdinalIgnoreCase)))
            //).ToList());
            if (keyVehicleLogAuditLogRequest.VehicleRego != null)
            {


                return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistory(keyVehicleLogAuditLogRequest.VehicleRego).Where(x => keyVehicleLogAuditLogRequest.ClientSiteIds.Contains(x.GuardLogin.ClientSiteId)).ToList());
            }
            if (keyVehicleLogAuditLogRequest.PersonName != null)
            {


                return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistoryWithPersonName(keyVehicleLogAuditLogRequest.PersonName).Where(x => keyVehicleLogAuditLogRequest.ClientSiteIds.Contains(x.GuardLogin.ClientSiteId)).ToList());
            }
            if (keyVehicleLogAuditLogRequest.KeyNo != null)
            {


                return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistoryWithKeyNo(keyVehicleLogAuditLogRequest.KeyNo).Where(x => keyVehicleLogAuditLogRequest.ClientSiteIds.Contains(x.GuardLogin.ClientSiteId)).ToList());
            }
            return new JsonResult(_viewDataService.GetKeyVehicleLogAuditHistory(keyVehicleLogAuditLogRequest.VehicleRego).Where(x => keyVehicleLogAuditLogRequest.ClientSiteIds.Contains(x.GuardLogin.ClientSiteId)).ToList());
        }
        //to get audit log-end


        //fusion Start
        public JsonResult OnGetDailyGuardFusionSiteLogs(int pageNo, int limit, int clientSiteId,
                                                    DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            var start = (pageNo - 1) * limit;
            var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs);
            var records = dailyGuardLogs.Skip(start).Take(limit).ToList();
            return new JsonResult(new { records, total = dailyGuardLogs.Count });
        }


        public JsonResult OnPostDownloadDailyFusionGuardLogZip(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                zipFileName = _guardLogZipGenerator.GenerateFusionZipFile(new int[] { clientSiteId }, logFromDate, logToDate, LogBookType.DailyGuardLog).Result;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;

                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
            }

            return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
        }
        public JsonResult OnPostDownloadDailyTimesheetLogZip(string clientSiteId, string frequency)
        {
            List<int> clientSiteIds = clientSiteId.Split(',').Select(int.Parse).ToList();
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;
            var fileName = string.Empty;
            var statusCode = 0;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
           
                DateTime today = DateTime.Today;

                if (frequency == "ThisWeek")
                {

                    // Assuming the week starts on Monday and ends on Sunday
                    int daysToSubtract = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
                    startDate = today.AddDays(-daysToSubtract);

                    endDate = startDate.AddDays(6);
                }
                else if (frequency == "Last2weeks")
                {
                    // Calculate the end of last week (Sunday)
                    int daysToSubtract = (int)today.DayOfWeek - (int)DayOfWeek.Sunday + 7;
                    endDate = today.AddDays(-daysToSubtract);

                    // Start date is 13 days before the end date (2 weeks)
                    startDate = endDate.AddDays(-13);
                }
                else if (frequency == "Last4weeks")
                {
                    // Calculate the end of last week (Sunday)
                    int daysToSubtract = (int)today.DayOfWeek + 1; // daysToSubtract for the previous Sunday
                    endDate = today.AddDays(-daysToSubtract);

                    // Start date is 27 days before the end date (for four weeks)
                    startDate = endDate.AddDays(-27);
                }
                else if (frequency == "Month")
                {
                    // Calculate the start date as the first day of the last month
                    startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);

                    // Calculate the end date as the last day of the last month
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                else if (frequency == "Today")
                {
                    startDate = today;
                    endDate = today;
                }
                string StartDate = startDate.ToString();
                string EndDate = endDate.ToString();

                try
            {
                zipFileName = _TimesheetReportGenerator.GenerateTimesheetZipFileFrequency(clientSiteIds.ToArray(), StartDate, EndDate).Result;
              //fileName = _TimesheetReportGenerator.GeneratePdfTimesheetReport(startdate, endDate, guradid);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;

                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
            }

            return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
        }
        public async Task<JsonResult> OnPostDownloadTimesheetBulk(string clientSiteId,string startdate, string endDate)
        {
            List<int> clientSiteIds = clientSiteId.Split(',').Select(int.Parse).ToList();
            var fileName = string.Empty;
            var statusCode = 0;
            int id = 1;
            var zipFileName = string.Empty;
            var success = true;
            var message = string.Empty;
            try
            {
                zipFileName = _TimesheetReportGenerator.GenerateTimesheetZipFile(clientSiteIds.ToArray(), startdate, endDate).Result;

            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;

                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
            }

            if (string.IsNullOrEmpty(zipFileName))
                return new JsonResult(new { fileName, message = "Failed to generate pdf", statusCode = -1 });




            return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
        }
        public JsonResult OnPostGenerateDownloadFilesLog(DateTime logFromDate, DateTime logToDate)
        {
            var r = _viewDataService.GetFileDownloadAuditLogs(logFromDate, logToDate);
            return new JsonResult(r);
        }


    }
}