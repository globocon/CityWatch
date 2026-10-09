using CityWatch.Web.Services;
using Dropbox.Api.SeenState;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppUpgradeController : ControllerBase
    {
        private readonly IViewDataService _viewDataService;
        public AppUpgradeController(IViewDataService viewDataService)
        {
            _viewDataService = viewDataService;
        }

        [HttpGet("GetLatestAppVersion")]
        public IActionResult GetLatestAppVersion(string platformType)
        {
            try
            {
                var latestVersion = _viewDataService.GetLatestMobileAppVersion(platformType);
                return Ok(latestVersion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("DownloadMobileApp")]
        public IActionResult DownloadMobileApp(string platform)
        {
            var latestVersion = _viewDataService.GetLatestMobileAppVersion(platform);
            var versionPath = $"{latestVersion.AppVersionMajor}.{latestVersion.AppVersionMinor}.{latestVersion.AppVersionPatch}";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Downloads", "MobileApp", platform, versionPath, latestVersion.FileName);
            var contentType = "application/vnd.android.package-archive";
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Update download count            
            _viewDataService.UpdateDownloadCount(latestVersion.Id);

            return File(fileBytes, contentType, latestVersion.FileName); // <== forces download
        }

    }  
}
