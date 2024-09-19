using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;

namespace CityWatch.Web.Pages.Admin
{
    public class RosterModel : PageModel
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly ILogger<RosterModel> _logger;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public RosterModel(ILogger<RosterModel> logger,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider)
        {
            _logger = logger;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
        }
        public void OnGet()
        {
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
                            var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo);
                            if (guard != null)
                            {
                                if (guard.IsActive)
                                {
                                    
                                    Issuccess = true;
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
    }
}
