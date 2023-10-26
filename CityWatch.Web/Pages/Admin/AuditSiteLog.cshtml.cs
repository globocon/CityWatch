using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CityWatch.Web.Pages.Admin
{
    public class AuditSiteLogModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardLogZipGenerator _guardLogZipGenerator;
        private readonly IAuditLogViewDataService _auditLogViewDataService;
        private readonly IClientSiteViewDataService _clientViewDataService;

        public AuditSiteLogModel(IViewDataService viewDataService,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardLogZipGenerator guardLogZipGenerator,
            IAuditLogViewDataService auditLogViewDataService,
            IClientSiteViewDataService clientViewDataService)
        {
            _viewDataService = viewDataService;
            _guardLogDataProvider = guardLogDataProvider;
            _guardLogZipGenerator = guardLogZipGenerator;
            _auditLogViewDataService = auditLogViewDataService;
            _clientViewDataService = clientViewDataService;
        }

        public KeyVehicleLogAuditLogRequest KeyVehicleLogAuditLogRequest { get; set; }

        public ActionResult OnGet()
        {
            if (!AuthUserHelper.IsAdminUserLoggedIn)
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

        public JsonResult OnPostKeyVehicleSiteLogs(KeyVehicleLogAuditLogRequest keyVehicleLogAuditLogRequest)
        {
            //return new JsonResult(_auditLogViewDataService.GetKeyVehicleLogs(keyVehicleLogAuditLogRequest));
            return new JsonResult(_auditLogViewDataService.GetKeyVehicleLogsWithPOI(keyVehicleLogAuditLogRequest));
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
            return new JsonResult(_viewDataService.GetGuards().SingleOrDefault(z => z.Id == id));
        }
    }
}