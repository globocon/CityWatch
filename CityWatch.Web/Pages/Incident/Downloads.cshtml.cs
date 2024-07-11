using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Pages.Guard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;

namespace CityWatch.Web.Pages.Incident
{
    public class DownloadsModel : PageModel
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly ILogger<DownloadsModel> _logger;

        public DownloadsModel(IGuardDataProvider guardDataProvider,
           IGuardLogDataProvider guardLogDataProvider,
             ILogger<DownloadsModel> logger)
        {
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public JsonResult OnPostCheckAndCreateDownloadAuditLog(string guardLicNo, string downloadCatg,string downloadFileName, GuardLog tmdata)
        {
            var Issuccess = false;
            var exMessage = "";
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userid = AuthUserHelper.GetLoggedInUserId;
                    if(userid != null)
                    {
                        var IPAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                        if (!string.IsNullOrEmpty(guardLicNo))
                        {
                            var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(guardLicNo);
                            if (guard != null)
                            {
                                if (guard.IsActive)
                                {
                                    FileDownloadAuditLogs fdal = new FileDownloadAuditLogs()
                                    {
                                        UserID = (int)userid,
                                        GuardID = guard.Id,
                                        IPAddress = IPAddress,
                                        DwnlCatagory = downloadCatg,
                                        DwnlFileName = downloadFileName,
                                        EventDateTimeLocal = tmdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmdata.EventDateTimeUtcOffsetMinute
                                    };

                                    _guardLogDataProvider.CreateDownloadFileAuditLogEntry(fdal);
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
                exMessage=$"Error: {ex.Message}.";
            }

            return new JsonResult(new { success = Issuccess, message = exMessage });
        }
    }
}
