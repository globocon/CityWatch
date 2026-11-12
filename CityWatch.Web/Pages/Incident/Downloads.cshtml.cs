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
        private readonly IConfigDataProvider _configDataProvider;
        public string ClientNameTitle { get; set; }

        /* p7-141 used by the page to render the category tabs (Training / Multimedia) */
        public IConfigDataProvider ConfigDataProvider { get { return _configDataProvider; } }
        public DownloadsModel(IGuardDataProvider guardDataProvider,
           IGuardLogDataProvider guardLogDataProvider,
             ILogger<DownloadsModel> logger, IConfigDataProvider configDataProvider)
        {
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _logger = logger;
            _configDataProvider = configDataProvider;
        }

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
                }
            }
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
