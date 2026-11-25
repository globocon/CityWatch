using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using CityWatch.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CityWatch.Web.Pages.roster
{
    /// <summary>
    /// Booking > PCAR tab: patrol car (PCAR) route profiles, each a smart wand plus an ordered list of
    /// client sites with per-day visit windows. Moved here from Kpi Admin/Settings > "PCAR Routes"
    /// (P9 Issue 44 Step 1A); the data layer (PcarRoute / PcarRouteDetails) is unchanged.
    /// </summary>
    public partial class BookingModel
    {
        public List<SelectListItem> PcarSmartWandOptions { get; set; } = new();
        public List<SelectListItem> PcarClientTypeOptions { get; set; } = new();

        private void PopulatePcarDropdowns()
        {
            // Smart wand text is "<SmartWandId> <phone>", with any "[R<n>]" marker stripped from the phone (as in Kpi).
            PcarSmartWandOptions = new List<SelectListItem> { new SelectListItem("Select", "", true) };
            PcarSmartWandOptions.AddRange(_configDataProvider.GetSmartWandList().Select(sw => new SelectListItem(
                sw.SmartWandId + " " + Regex.Replace(sw.PhoneNumber ?? "", @"\[R\d+\]", "").Trim(),
                sw.Id.ToString())));

            PcarClientTypeOptions = _viewDataService.GetUserClientTypesCount(PcarScopeUserId());
        }

        /// <summary>
        /// The user whose site access limits what the PCAR tab lists: a "User" login sees only its own sites,
        /// an Administrator or a Booking access granted by licence / PIN sees all (as the Kpi tab did for
        /// Super Admin / guard access). Read without AuthUserHelper, which throws for an anonymous request.
        /// </summary>
        private int? PcarScopeUserId()
        {
            if (User?.Identity?.IsAuthenticated == true && User.IsInRole("User")
                && int.TryParse(User.FindFirst(ClaimTypes.Sid)?.Value, out var userId))
                return userId;
            return null;
        }

        /// <summary>ROEditor (Booking opened from a site logbook) does not get the PCAR tab, so nor its handlers.</summary>
        private bool IsPcarAccessDenied => HttpContext.Session.GetString("BookingAccessRole") == "ROEditor";

        private static JsonResult PcarAccessDeniedResult() =>
            new JsonResult(new { success = false, message = "You do not have access to PCAR routes." });

        public JsonResult OnGetPcarProfiles()
        {
            if (IsPcarAccessDenied)
                return new JsonResult(new List<object>());

            var userId = PcarScopeUserId();
            return new JsonResult(userId.HasValue
                ? _kpiSchedulesDataProvider.GetPCARProfilesUsingUserId(userId.Value)
                : _kpiSchedulesDataProvider.GetPCARProfilesAll());
        }

        public JsonResult OnGetPcarRouteDetails(int id)
        {
            if (IsPcarAccessDenied)
                return new JsonResult(null);

            return _configDataProvider.GetPCARRouteDetails(id);
        }

        /// <summary>Client Site dropdown of the PCAR modal, for the chosen Client Type (value = site id).</summary>
        public JsonResult OnGetPcarClientSites(string type)
        {
            if (IsPcarAccessDenied || string.IsNullOrWhiteSpace(type))
                return new JsonResult(new List<SelectListItem>());

            var clientType = _clientDataProvider.GetClientTypes().FirstOrDefault(z => z.Name == type);
            if (clientType == null)
                return new JsonResult(new List<SelectListItem>());

            var sites = _viewDataService.GetUserClientSitesHavingAccess(clientType.Id, PcarScopeUserId(), string.Empty)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToList();
            return new JsonResult(sites);
        }

        public JsonResult OnPostPcarCreateProfile([FromForm] int routeId, [FromForm] string pcarroutename, [FromForm] int smartwandallocation)
        {
            if (IsPcarAccessDenied)
                return PcarAccessDeniedResult();

            if (string.IsNullOrWhiteSpace(pcarroutename))
                return new JsonResult(new { success = false, message = "Route name is required." });

            if (smartwandallocation == 0)
                return new JsonResult(new { success = false, message = "Please select at least one Smart Wand Allocation." });

            var result = _kpiSchedulesDataProvider.SavePcarrouteMaster(
                routeId == 0 ? (int?)null : routeId,
                pcarroutename,
                smartwandallocation);

            if (!result.success)
                return new JsonResult(new { success = false, message = result.message });

            return new JsonResult(new { success = true, message = result.message, routeId = result.route.Id });
        }

        public JsonResult OnPostPcarDeleteProfile(int routeId)
        {
            if (IsPcarAccessDenied)
                return PcarAccessDeniedResult();

            if (!_kpiSchedulesDataProvider.DeletePcarrouteProfile(routeId))
                return new JsonResult(new { success = false, message = "Failed to delete profile." });

            return new JsonResult(new { success = true, message = "Profile deleted successfully.", routeId });
        }

        public JsonResult OnPostPcarSaveRouteDetails([FromBody] PcarRouteSaveViewModel model)
        {
            if (IsPcarAccessDenied)
                return PcarAccessDeniedResult();

            if (model == null || model.PcarRouteId == 0 || model.SiteSchedules == null || !model.SiteSchedules.Any())
                return new JsonResult(new { success = false, message = "Invalid Profile or Client Sites." });

            _kpiSchedulesDataProvider.SavePcarrouteDetails(model);
            return new JsonResult(new { success = true });
        }
    }
}
