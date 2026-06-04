using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using CityWatch.Web.Services;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CityWatch.Web.Pages.Admin
{
    public class RosterModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly ILogger<RosterModel> _logger;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        public string ClientNameTitle { get; set; }

        public RosterModel(ILogger<RosterModel> logger,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider, IConfigDataProvider configDataProvider,IViewDataService viewDataService)
        {
            _logger = logger;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _configDataProvider = configDataProvider;
            _viewDataService = viewDataService;
        }
        public IViewDataService ViewDataService { get { return _viewDataService; } }
        public void OnGet()
        {
            var host = HttpContext.Request.Host.Host;
            var hostParts = host.Split('.');

            // Extract the client name
            string clientName = hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www"
                                ? hostParts[1]
                                : hostParts[0];
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

                        ClientNameTitle = _configDataProvider.GetSubDomainDetails(clientName).Domain;
                    }
                    else
                    {

                        ClientNameTitle = "Citywatch Security";
                    }
                }
                else
                {

                    ClientNameTitle = "Citywatch Security";
                }            }
            }
        public JsonResult OnGetGuardID(string LicenseNo)
        {
            var ddd = _guardDataProvider.GetGuardID(LicenseNo);
            return new JsonResult(_guardDataProvider.GetGuardID(LicenseNo));
        }
        public JsonResult OnPostCheckAndCreateDownloadAuditLog1(string guardLicNo)
        {
            var Issuccess = false;
            var exMessage = "";
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userid = AuthUserHelper.GetLoggedInUserId;
                    if (userid != null)
                    {
                        var IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                        if (!string.IsNullOrEmpty(guardLicNo))
                        {
                            var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo.Trim());
                            if (guard != null)
                            {
                                if (guard.IsActive)
                                {
                                    
                                    Issuccess = true;
                                    if(guard.IsAdminRosterAccess == false)
                                    {

                                        exMessage = "Need Pin";
                                    }
                                }
                                else
                                {
                                    exMessage = "Your security profile in inactive. Please contact your administrator!.";
                                }
                            }
                            else
                            {
                                exMessage = "Error: Guard details not found.";
                            }
                        }
                        else
                        {
                            exMessage = "Error: Invalid licence no.";
                        }
                    }
                    else
                    {
                        exMessage = "Error: User not authenticated.";
                    }
                }
                else
                {
                    exMessage = "Error: User not authenticated.";
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                exMessage = $"Error: {ex.Message}.";
            }

            return new JsonResult(new { success = Issuccess, message = exMessage });
        }
        public JsonResult OnPostCheckAndCreateDownloadAuditLogSite(int siteId)
        {
            var Issuccess = false;
            var exMessage = "";
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userid = AuthUserHelper.GetLoggedInUserId;
                    if (userid != null)
                    {
                        if (siteId > 0)
                        {
                            Issuccess = true;
                        }
                        else
                        {
                            exMessage = "Error: Invalid site selected.";
                        }
                    }
                    else
                    {
                        exMessage = "Error: User not authenticated.";
                    }
                }
                else
                {
                    exMessage = "Error: User not authenticated.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                exMessage = $"Error: {ex.Message}.";
            }

            return new JsonResult(new { success = Issuccess, message = exMessage });
        }

        public JsonResult OnPostVerifyBookingAccess(string guardLicNo, string pin)
        {
            var Issuccess = false;
            var exMessage = "";
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    bool isSystemAdmin = AuthUserHelper.IsAdminUserLoggedIn || AuthUserHelper.IsAdminGlobal || AuthUserHelper.IsAdminPowerUser;
                    if (isSystemAdmin && guardLicNo == "ADMIN" && pin == "ADMIN")
                    {
                        Issuccess = true;
                        HttpContext.Session.SetString("BookingAccessRole", "GSS");
                    }
                    else if (!string.IsNullOrEmpty(guardLicNo) && !string.IsNullOrEmpty(pin))
                    {
                        var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo.Trim());
                        if (guard != null)
                        {
                            if (guard.IsActive)
                            {
                                if (guard.IsAdminRosterAccess || guard.IsAdminRosterBaseAccess || guard.IsAdminRosterGSAccess)
                                {
                                    if (guard.Pin == pin.Trim())
                                    {
                                        Issuccess = true;
                                        string role = "ROEditor";
                                        if (guard.IsAdminRosterAccess) role = "GSS";
                                        else if (guard.IsAdminRosterGSAccess) role = "GS";
                                        else if (guard.IsAdminRosterBaseAccess) role = "Base";
                                        
                                        HttpContext.Session.SetString("BookingAccessRole", role);
                                    }
                                    else
                                    {
                                        exMessage = "Error: Invalid HR PIN.";
                                    }
                                }
                                else
                                {
                                    exMessage = "Error: You do not have Admin Roster access.";
                                }
                            }
                            else
                            {
                                exMessage = "Error: Your security profile is inactive.";
                            }
                        }
                        else
                        {
                            exMessage = "Error: Guard details not found.";
                        }
                    }
                    else
                    {
                        exMessage = "Error: Invalid license no or PIN.";
                    }
                }
                else
                {
                    exMessage = "Error: User not authenticated.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                exMessage = $"Error: {ex.Message}.";
            }

            return new JsonResult(new { success = Issuccess, message = exMessage });
        }
        public JsonResult OnGetClientTypesList()
        {
            try
            {
                var clientTypes = _viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId)
                                  .Select(s => new { value = s.Id.ToString(), text = s.Name })
                                  .ToList();
                return new JsonResult(clientTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return new JsonResult(new List<object>());
        }

        public JsonResult OnGetClientSitesWithIds(string type)
        {
            try
            {
                var clientTypes = _viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId);
                var clientTypeObj = clientTypes.FirstOrDefault(x => x.Name == type || x.Name.Trim() == type.Trim());

                if (clientTypeObj != null)
                {
                    var sites = _viewDataService.GetUserClientSitesHavingAccess(clientTypeObj.Id, AuthUserHelper.LoggedInUserId, string.Empty)
                                .Where(x => x.ClientType.Name == type || x.ClientType.Name.Trim() == type.Trim())
                                .Select(s => new { text = s.Name, value = s.Id.ToString() })
                                .ToList();
                    return new JsonResult(sites);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return new JsonResult(new List<object>());
        }
        public JsonResult OnPostVerifyGuardRosterAuth(string licenseNo, string pin)
        {
            var isSuccess = false;
            var message = "";
            int? guardId = null;

            CityWatch.Data.Models.Guard guard = null;
            try
            {
                if (string.IsNullOrEmpty(licenseNo) || string.IsNullOrEmpty(pin))
                {
                    message = "Security License No and HR PIN are required.";
                }
                else
                {
                    guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(licenseNo.Trim());
                    if (guard != null)
                    {
                        if (guard.IsActive)
                        {
                            if (guard.Pin == pin.Trim())
                            {
                                isSuccess = true;
                                guardId = guard.Id;
                            }
                            else
                            {
                                message = "Invalid HR PIN.";
                            }
                        }
                        else
                        {
                            message = "Your security profile is inactive. Please contact your administrator.";
                        }
                    }
                    else
                    {
                        message = "Guard details not found for the provided License No.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                message = $"Error: {ex.Message}";
            }

            bool isSystemAdmin = AuthUserHelper.IsAdminUserLoggedIn || AuthUserHelper.IsAdminGlobal || AuthUserHelper.IsAdminPowerUser;
            bool isRosterAdmin = isSuccess && guard != null && (guard.IsAdminRosterAccess || guard.IsAdminRosterBaseAccess || guard.IsAdminRosterGSAccess);
            
            return new JsonResult(new { 
                success = isSuccess || isSystemAdmin, 
                message = message, 
                guardId = guardId, 
                isAdminRoster = isRosterAdmin || isSystemAdmin,
                isROEditor = isSystemAdmin || isRosterAdmin || (isSuccess && guard != null && guard.IsROEditorAccess)
            });
        }
    }
}
