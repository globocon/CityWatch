using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Security.Claims;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrowdCountController : ControllerBase
    {
        private readonly IUserAuthenticationService _userAuthentication;
        private readonly IViewDataService _viewDataService;

        public CrowdCountController(IUserAuthenticationService userAuthentication, IViewDataService viewDataService)
        {
            _userAuthentication = userAuthentication;
            _viewDataService = viewDataService;
        }

        [HttpGet("GetCrowdCountControlSettings/{siteId}")]
        public IActionResult GetClientSitesCrowdCountControlSettings(int siteId)
        {
            try
            {
                var clientSiteMobileAppSettings = _viewDataService.GetCrowdSettingForSite(siteId);

                if (clientSiteMobileAppSettings == null)
                {
                    clientSiteMobileAppSettings = new ClientSiteMobileAppSettings() { ClientSiteId = siteId, IsCrowdCountEnabled = false };
                }                    

                return Ok(clientSiteMobileAppSettings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

    }
}
