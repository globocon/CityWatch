using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
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
using System.IO;
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
        public readonly IConfigDataProvider _configDataProvider;
        public readonly ISmartWandReportZipGenarator _smartWandReportGenarator;
        public readonly IWandStrikeReportDataService _wandStrikeReportDataService;
        public string ClientNameTitle { get; set; }
        public AuditSiteLogModel(IViewDataService viewDataService,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardLogZipGenerator guardLogZipGenerator,
            IAuditLogViewDataService auditLogViewDataService,
            IClientSiteViewDataService clientViewDataService,
            ITimesheetReportGenerator TimesheetReportGenerator, 
            IConfigDataProvider configDataProvider,
            ISmartWandReportZipGenarator smartWandReportGenarator,
            IWandStrikeReportDataService wandStrikeReportDataService)
        {
            _viewDataService = viewDataService;
            _guardLogDataProvider = guardLogDataProvider;
            _guardLogZipGenerator = guardLogZipGenerator;
            _auditLogViewDataService = auditLogViewDataService;
            _clientViewDataService = clientViewDataService;
            _TimesheetReportGenerator = TimesheetReportGenerator;
            _configDataProvider = configDataProvider;
            _smartWandReportGenarator = smartWandReportGenarator;
            _wandStrikeReportDataService = wandStrikeReportDataService;
        }

        public KeyVehicleLogAuditLogRequest KeyVehicleLogAuditLogRequest { get; set; }
        public WandStrikeAuditLogRequest WandStrikeAuditLogRequest { get; set; }
        public string loggedInUserId { get; set; }
        public int GuardId { get; set; }
        public GuardViewModel Guard { get; set; }
        public int ClientTypeId { get; set; }
        public ActionResult OnGet()
        {
            string securityLicenseNonew = Request.Query["Sl"];
            string guid = Request.Query["guid"];
            string luid = Request.Query["lud"];
            GuardId = Convert.ToInt32(guid);
            loggedInUserId = luid;
            var host = HttpContext.Request.Host.Host;
            var clientName = string.Empty;
            var clientLogo = string.Empty;
            var url = string.Empty;

            // Split the host by dots to separate subdomains and domain name
            var hostParts = host.Split('.');

            // If the first part is "www", take the second part as the client name
            if (hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www")
            {
                clientName = hostParts[1];
            }
            else
            {
                clientName = hostParts[0];
            }
            if (!string.IsNullOrEmpty(clientName))
            {
                if (
                    clientName.Trim().ToLower() != "www" &&
                    clientName.Trim().ToLower() != "cws-ir" &&
                    clientName.Trim().ToLower() != "test"
                &&
                clientName.Trim().ToLower() != "localhost"
                )
                {
                    int domain = _configDataProvider.GetSubDomainDetails(clientName).TypeId;
                    if (domain != 0)
                    {
                        ClientTypeId = domain;
                        ClientNameTitle = _configDataProvider.GetSubDomainDetails(clientName).Domain;
                    }
                    else
                    {
                        ClientTypeId = 0;
                        ClientNameTitle = "Citywatch Security";
                    }
                }
            }
            if (GuardId != 0)
            {
                Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);

            }
            /* Normal admin,PowerAdmin and AdminGlobal can access this page */

            if (!AuthUserHelper.IsAdminUserLoggedIn && !AuthUserHelper.IsAdminGlobal && !AuthUserHelper.IsAdminInvestigator && !AuthUserHelper.IsAdminAuditor)
            {

                return Redirect(Url.Page("/Account/Unauthorized"));

            }
            else
            {

                return Page();
            }
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
                                                    DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs, string keywordDownSelect)
        {
            var start = (pageNo - 1) * limit;
            var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs).
                Where(x => string.IsNullOrEmpty(keywordDownSelect) ||
                (!string.IsNullOrEmpty(x.Notes) && x.Notes.Contains(keywordDownSelect)) ||
                (!string.IsNullOrEmpty(x.GuardInitials) && x.GuardInitials.Contains(keywordDownSelect)));
            if (limit == 0)
            {
                limit = dailyGuardLogs.Count();
            }
            var records = dailyGuardLogs.Skip(start).Take(limit).ToList();
            int total = dailyGuardLogs.Count();
            return new JsonResult(new { records, total = dailyGuardLogs.Count() });
        }

        public IActionResult OnPostKeyVehicleSiteLogs(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest)
        {
            // Single query for the whole range; the week/month/year charts are bucketed from
            // it in memory. This used to re-run the full logs query once per week, month and
            // year in the range (30+ heavy queries), which made the chart tab take minutes.
            var kvLogs = _auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequest);

            // Serialize only the fields the report tables actually bind (same JSON paths the
            // DataTables columns use) — returning the full entity graph per row made the
            // response enormous and slow to build, transfer and parse.
            var keyVehicleAuditLogRequest = kvLogs.Select(v => new
            {
                v.GroupText,
                v.Plate,
                v.TruckConfigText,
                v.TrailerTypeText,
                v.PersonTypeText,
                v.ClientSitePocName,
                v.ClientSiteLocationName,
                v.PurposeOfEntry,
                Detail = new
                {
                    v.Detail.EntryTime,
                    v.Detail.ExitTime,
                    v.Detail.TimeSlotNo,
                    v.Detail.VehicleRego,
                    v.Detail.Trailer1Rego,
                    v.Detail.Trailer2Rego,
                    v.Detail.Trailer3Rego,
                    v.Detail.Trailer4Rego,
                    v.Detail.KeyNo,
                    v.Detail.CompanyName,
                    v.Detail.PersonName,
                    v.Detail.MobileNumber,
                    v.Detail.InWeight,
                    v.Detail.OutWeight,
                    v.Detail.TareWeight,
                    v.Detail.Notes,
                    ClientSiteLogBook = new
                    {
                        ClientSite = new { Name = v.Detail.ClientSiteLogBook?.ClientSite?.Name }
                    }
                }
            }).ToList();

            var fromDate = keyVehicleLogAuditLogRequest.LogFromDate.Date;
            var toDate = keyVehicleLogAuditLogRequest.LogToDate.Date;
            if (toDate < fromDate)
                toDate = fromDate;

            var entryDates = keyVehicleAuditLogRequest
                .Where(v => v.Detail != null && v.Detail.EntryTime.HasValue)
                .Select(v => v.Detail.EntryTime.Value.Date)
                .Where(d => d >= fromDate && d <= toDate)
                .ToList();

            // truck entries per week (calendar weeks ending Saturday, clipped to the range)
            var kvtruckentriesForWeekNew = new List<KeyVehicleLogAuditLogRequest>();
            int kvtruckentriesForWeekNewCount = 0;
            var weekStart = fromDate;
            while (weekStart <= toDate)
            {
                var weekEnd = weekStart.AddDays(6 - (int)weekStart.DayOfWeek);
                if (weekEnd > toDate)
                    weekEnd = toDate;
                var start = weekStart;
                var end = weekEnd;
                var count = entryDates.Count(d => d >= start && d <= end);
                kvtruckentriesForWeekNew.Add(new KeyVehicleLogAuditLogRequest
                {
                    DateRange = start.ToString("dd-MM-yyyy") + " to " + end.ToString("dd-MM-yyyy"),
                    RecordCount = count
                });
                kvtruckentriesForWeekNewCount += count;
                weekStart = weekEnd.AddDays(1);
            }

            // truck entries per month
            var kvtruckentriesForMonthNew = new List<KeyVehicleLogAuditLogRequest>();
            int kvtruckentriesForMonthNewCount = 0;
            for (var month = new DateTime(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1))
            {
                var start = month;
                var end = month.AddMonths(1).AddDays(-1);
                var count = entryDates.Count(d => d >= start && d <= end);
                kvtruckentriesForMonthNew.Add(new KeyVehicleLogAuditLogRequest
                {
                    DateRange = month.ToString("MMM yyyy"),
                    RecordCount = count
                });
                kvtruckentriesForMonthNewCount += count;
            }

            // entries per year — independent of the selected from/to dates: the yearly
            // chart must show the full calendar year(s), not just the filtered range
            // (previously it reused entryDates, so "2026" always equalled the range total).
            // Same site/downselect filters apply; only the date window is widened.
            var savedLogFromDate = keyVehicleLogAuditLogRequest.LogFromDate;
            var savedLogToDate = keyVehicleLogAuditLogRequest.LogToDate;
            keyVehicleLogAuditLogRequest.LogFromDate = new DateTime(fromDate.Year, 1, 1);
            keyVehicleLogAuditLogRequest.LogToDate = new DateTime(toDate.Year, 12, 31);
            var yearEntryDates = _auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequest)
                .Where(v => v.Detail != null && v.Detail.EntryTime.HasValue)
                .Select(v => v.Detail.EntryTime.Value.Date)
                .ToList();
            keyVehicleLogAuditLogRequest.LogFromDate = savedLogFromDate;
            keyVehicleLogAuditLogRequest.LogToDate = savedLogToDate;

            var kvtruckentriesForYearNew = new List<KeyVehicleLogAuditLogRequest>();
            int kvtruckentriesForYearNewCount = 0;
            for (var year = new DateTime(fromDate.Year, 1, 1); year <= toDate; year = year.AddYears(1))
            {
                var count = yearEntryDates.Count(d => d.Year == year.Year);
                kvtruckentriesForYearNew.Add(new KeyVehicleLogAuditLogRequest
                {
                    DateRange = year.Year.ToString(),
                    RecordCount = count
                });
                kvtruckentriesForYearNewCount += count;
            }

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

        public JsonResult OnPostDownloadDailyGuardLogZip(int clientSiteId, DateTime logFromDate, DateTime logToDate, string keywordDownSelect)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                zipFileName = _guardLogZipGenerator.GenerateZipFile(new int[] { clientSiteId }, logFromDate, logToDate, keywordDownSelect, LogBookType.DailyGuardLog).Result;
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
        //public JsonResult OnGetDailyGuardFusionSiteLogs(int pageNo, int limit, int clientSiteId,
        //                                            DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        //{
        //    var start = (pageNo - 1) * limit;
        //    var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs);
        //    var records = dailyGuardLogs.Skip(start).Take(limit).ToList();
        //    return new JsonResult(new { records, total = dailyGuardLogs.Count });
        //}

        public JsonResult OnGetDailyGuardFusionSiteLogs(int pageNo, int limit, string clientSiteIds,
                                                   DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs, string keywordDownSelect)
        {
            if (string.IsNullOrWhiteSpace(clientSiteIds))
            {
                // Handle the case where clientSiteIds is null or empty
                return new JsonResult(new { records = new List<object>(), total = 0 });
            }

            var arClientSiteIds = clientSiteIds
                .Split(";")
                .Where(z => !string.IsNullOrWhiteSpace(z)) // Ensure no empty segments are processed
                .Select(z => int.Parse(z))
                .ToArray();

            var start = (pageNo - 1) * limit;
            //var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardFusionLogs(arClientSiteIds, logFromDate, logToDate, excludeSystemLogs).Where(x => string.IsNullOrEmpty(keywordDownSelect) || (!string.IsNullOrEmpty(x.Notes) && x.Notes.Contains(keywordDownSelect)) ||
            //(!string.IsNullOrEmpty(x.GuardName) && x.GuardName.Contains(keywordDownSelect))); ;

            var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardFusionLogs(arClientSiteIds, logFromDate, logToDate, excludeSystemLogs)
                                    .Where(x =>
                                        // filter by keyword if provided
                                        (string.IsNullOrEmpty(keywordDownSelect) ||
                                            (!string.IsNullOrEmpty(x.Notes) && x.Notes.Contains(keywordDownSelect)) ||
                                            (!string.IsNullOrEmpty(x.GuardName) && x.GuardName.Contains(keywordDownSelect)))
                                        &&
                                        // Exclude only when ActivityType is "LB" and Notes contain [NFC] or [BLE]
                                        !(x.ActivityType == "LB" &&
                                          (!string.IsNullOrEmpty(x.Notes) &&
                                           (x.Notes.Contains("[NFC]") || x.Notes.Contains("[BLE]"))))
                                    ).ToList();


            foreach (var guardlog in dailyGuardLogs)
            {
                if (guardlog.LBId != null)
                {
                    var guardlogImages = _guardLogDataProvider.GetGuardLogDocumentImaes((int)guardlog.LBId);



                    foreach (var guardLogImage in guardlogImages)
                    {
                        if (guardLogImage.IsRearfile == true)
                        {
                            guardlog.Notes = guardlog.Notes + "</br>See attached file <a href =\"" + guardLogImage.ImagePath + "\" target=\"_blank\">" + Path.GetFileName(guardLogImage.ImagePath) + "</a>";
                        }
                        if (guardLogImage.IsTwentyfivePercentfile == true)
                        {

                            guardlog.Notes = guardlog.Notes + " </br> <a href =\"" + guardLogImage.ImagePath + " \" target=\"_blank\"><img src =\"" + guardLogImage.ImagePath + "\"height=\"200px\" width=\"200px\" class=\"mt-2\"/></a>";


                        }
                    }
                }
            }
            if (limit == 0)
            {
                limit = dailyGuardLogs.Count();
            }
            var records = dailyGuardLogs.Skip(start).Take(limit).ToList();


            return new JsonResult(new { records, total = dailyGuardLogs.Count() });
        }


        public JsonResult OnPostDownloadDailyFusionGuardLogZip(string clientSiteId, DateTime logFromDate, DateTime logToDate, string keywordDownSelect)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(clientSiteId))
                {
                    success = false;
                    message = "error";
                }
                else
                {
                    var arClientSiteIds = clientSiteId
                       .Split(";")
                       .Where(z => !string.IsNullOrWhiteSpace(z)) // Ensure no empty segments are processed
                       .Select(z => int.Parse(z))
                       .ToArray();
                    zipFileName = _guardLogZipGenerator.GenerateFusionZipFile(arClientSiteIds, logFromDate, logToDate, LogBookType.DailyGuardLog, keywordDownSelect).Result;
                }
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
        public async Task<JsonResult> OnPostDownloadTimesheetBulk(string clientSiteId, string startdate, string endDate)
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

        #region "Wand Strikes"
        public JsonResult OnGetClientSiteWandAndTags(string clientSiteIds)
        {
            var tagIds = new List<SelectListItem>();
            var tagTypeIds = new List<SelectListItem>();
            var tagLabels = new List<SelectListItem>();
            var smartWandIds = new List<SelectListItem>();
            var patrolCarIds = new List<SelectListItem>();
            var arClientSiteIds = clientSiteIds.Split(";").Select(z => int.Parse(z)).ToArray();

            var tags = _viewDataService.GetClientSiteTagIds(arClientSiteIds);

            tagIds = tags.DistinctBy(x => x.UId)
                        .Select(x => new SelectListItem
                        {
                            Text = x.UId,
                            Value = x.UId
                        })
                        .ToList();

            tagTypeIds = tags.DistinctBy(x => x.SmartWandTagsType.Id)
                            .Select(x => new SelectListItem
                            {
                                Value = x.SmartWandTagsType.Id.ToString(),
                                Text = x.SmartWandTagsType.value
                            })
                            .OrderBy(x => x.Text)
                            .ToList();

            tagLabels = tags.DistinctBy(x => x.LabelDescription)
                            .Select(x => new SelectListItem
                            {
                                Value = x.LabelDescription,
                                Text = x.LabelDescription
                            })
                            .OrderBy(x => x.Text)
                            .ToList();

            smartWandIds = _viewDataService.GetClientSiteSmartWandIds(arClientSiteIds);
            patrolCarIds = _viewDataService.GetAllPatrolCars();

            return new JsonResult(new { tagIds, tagTypeIds, tagLabels, smartWandIds, patrolCarIds });
        }

        public JsonResult OnPostPatrolCarAssociatedSmartWands(WandStrikeAuditLogRequest wandStrikeAuditLogRequest)
        {
            var smartWandIds = new List<SelectListItem>();
            //var arPatrolCarIds = patrolCarIds.Split(";").Select(z => int.Parse(z)).ToArray();
            var arPatrolCarIds = wandStrikeAuditLogRequest.PatrolCarIds;
            smartWandIds = _viewDataService.GetPatrolCarAssociatedSmartWands(arPatrolCarIds);
            return new JsonResult(new { smartWandIds });
        }

        public IActionResult OnPostWandStrikeAuditSiteLogs(WandStrikeAuditLogRequest wandStrikeAuditLogRequest)
        {
            // if(!string.IsNullOrEmpty(wandStrikeAuditLogRequest.TagLabel)) { wandStrikeAuditLogRequest.TagLabel = Uri.UnescapeDataString(wandStrikeAuditLogRequest.TagLabel); }            

            if(!wandStrikeAuditLogRequest.IncludeAllTagsInStrike)
            {
                var wandStrikeAuditLogViewModel = _wandStrikeReportDataService.GetWandStrikeAuditLogIncludingSmartWandStrike(wandStrikeAuditLogRequest).OrderBy(x => x.DateTimeSort).ToList();
                return new JsonResult(new { wandStrikeAuditLogViewModel });
            }
            else
            {
                var wandStrikeAuditLogViewModel = _wandStrikeReportDataService.GetWandStrikeAuditLogIncludingSmartWandStrikeAndAllTags(wandStrikeAuditLogRequest).OrderBy(x => x.DateTimeSort).ToList();
                return new JsonResult(new { wandStrikeAuditLogViewModel });                
            }
            
        }

        public JsonResult OnPostDownloadWandStrikeLogZip(WandStrikeAuditLogRequest wandStrikeAuditLogRequest)
        {
            //var success = true;
            //var message = string.Empty;
            //var zipFileName = string.Empty;

            //try
            //{
            //    if (wandStrikeAuditLogRequest == null)
            //    {
            //        success = false;
            //        message = "error";
            //    }
            //    else
            //    {
            //        var arClientSiteIds = clientSiteId
            //   .Split(";")
            //   .Where(z => !string.IsNullOrWhiteSpace(z)) // Ensure no empty segments are processed
            //   .Select(z => int.Parse(z))
            //   .ToArray();
            //        zipFileName = _guardLogZipGenerator.GenerateFusionZipFile(arClientSiteIds, logFromDate, logToDate, LogBookType.DailyGuardLog, keywordDownSelect).Result;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    success = false;
            //    message = ex.Message;

            //    if (ex.InnerException != null)
            //        message = ex.InnerException.Message;
            //}

            //return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
            return new JsonResult(new { success = false, message = "Download not implemented." })
            {
                StatusCode = 501
            };
        }
        #endregion "Wand Strikes"
        public IActionResult OnGetClientSiteSWTagsDetails(int clientSiteId, string startdate, string endDate)
        {




            return new JsonResult(_guardLogDataProvider.GetTagStatusPendingForSpecificClientSite(clientSiteId, Convert.ToDateTime(startdate), Convert.ToDateTime(endDate).Date));
        }
        public JsonResult OnPostDownloadFQLogZip(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                zipFileName = _smartWandReportGenarator.GenerateZipFile(new int[] { clientSiteId }, logFromDate, logToDate).Result;
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


    }
}