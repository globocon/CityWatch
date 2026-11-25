using System;
using System.Linq;
using CityWatch.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CityWatch.Web.Pages.Admin
{
    /// <summary>
    /// RoosterHub > "PCAR ROUTE": a read-only view of the PCAR routes (managed in Booking > PCAR), for guards.
    /// Access: an active guard, by Security License No + HR PIN, with "PCAR" or "ADMIN - ROSTER" ticked in
    /// Admin/Settings > C4i HR > Access (GuardAccess 17 = IsPCARAccess, 19 = IsAdminRosterBaseAccess).
    /// Everyone else is denied, including the other roster levels (G$ / G$S) and system admins without such a guard.
    /// The viewer only reads: route names, and one route's smart wand and site schedule.
    /// </summary>
    public partial class RosterModel
    {
        /// <summary>Session key set when a guard passes the PCAR ROUTE check; the read handlers require it.</summary>
        private const string PcarRouteViewerSessionKey = "PcarRouteViewerGuardId";

        private bool HasPcarRouteViewerAccess => HttpContext.Session.GetInt32(PcarRouteViewerSessionKey).HasValue;

        private static bool IsAllowedPcarRouteViewer(CityWatch.Data.Models.Guard guard) =>
            guard.IsPCARAccess || guard.IsAdminRosterBaseAccess;

        public JsonResult OnPostVerifyPcarRouteAccess(string licenseNo, string pin)
        {
            HttpContext.Session.Remove(PcarRouteViewerSessionKey);
            try
            {
                if (string.IsNullOrWhiteSpace(licenseNo) || string.IsNullOrWhiteSpace(pin))
                    return PcarRouteAccessResult(false, "Security License No and HR PIN are required.");

                var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(licenseNo.Trim());
                if (guard == null)
                    return PcarRouteAccessResult(false, "Guard details not found for the provided License No.");
                if (!guard.IsActive)
                    return PcarRouteAccessResult(false, "Your security profile is inactive. Please contact your administrator.");
                if (guard.Pin != pin.Trim())
                    return PcarRouteAccessResult(false, "Invalid HR PIN.");
                if (!IsAllowedPcarRouteViewer(guard))
                    return PcarRouteAccessResult(false, "Access denied. PCAR or ADMIN - ROSTER access is required to view PCAR routes.");

                HttpContext.Session.SetInt32(PcarRouteViewerSessionKey, guard.Id);
                return PcarRouteAccessResult(true, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PCAR ROUTE access check failed");
                return PcarRouteAccessResult(false, "Error: " + ex.Message);
            }
        }

        private static JsonResult PcarRouteAccessResult(bool success, string message) =>
            new JsonResult(new { success, message });

        /// <summary>PCAR Route Name dropdown: every route, with its smart wand as shown in Booking.</summary>
        public IActionResult OnGetPcarRouteViewerRoutes()
        {
            if (!HasPcarRouteViewerAccess)
                return new UnauthorizedResult();

            var routes = _kpiSchedulesDataProvider.GetPCARProfilesAll()
                .OrderBy(r => r.Pcarroutename)
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Pcarroutename,
                    smartWand = string.IsNullOrEmpty(r.SmartWandId) ? "" : (r.SmartWandId + " " + r.PhoneNumber).Trim()
                })
                .ToList();
            return new JsonResult(routes);
        }

        /// <summary>One route's ordered sites with Start / End / Visits per day, for the read-only grid.</summary>
        public IActionResult OnGetPcarRouteViewerDetails(int id)
        {
            if (!HasPcarRouteViewerAccess)
                return new UnauthorizedResult();

            return _configDataProvider.GetPCARRouteDetails(id);
        }
    }
}
