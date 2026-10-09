using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;

namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteLogNewController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISiteLogUploadService _siteLogUploadService;

        public SiteLogNewController(IWebHostEnvironment webHostEnvironment,
            ISiteLogUploadService siteLogUploadService)
        {
            _webHostEnvironment = webHostEnvironment;
            _siteLogUploadService = siteLogUploadService;
        }

        [Route("[action]", Name = "UploadLogsNew")]
        public JsonResult Upload()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            _siteLogUploadService.ProcessDailyGuardLogsNew();

            return new JsonResult(true);
        }
        [Route("[action]", Name = "UploadLogsSecondRunNew")]
        public JsonResult UploadSecondRun()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            _siteLogUploadService.ProcessDailyGuardLogsSecondRunNew();

            return new JsonResult(true);
        }

        /// <summary>
        /// The weekly log dump, for the Windows scheduler to call at 2am on Mondays - the time the
        /// site settings screen promises. It always covers the last week that fully ended, so a run
        /// that fires late, or is triggered by hand, still sends a whole Monday-to-Sunday week.
        /// </summary>
        /// <remarks>
        /// Anonymous, like the daily actions above and for the same reason: the caller is an
        /// external scheduler, not a signed-in user.
        /// </remarks>
        [Route("[action]", Name = "UploadWeeklyLogsNew")]
        public JsonResult UploadWeekly()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            _siteLogUploadService.ProcessWeeklyGuardLogs();

            return new JsonResult(true);
        }

        /// <summary>
        /// The monthly log dump, for the Windows scheduler to call at 6am on the 1st - the time the
        /// site settings screen promises. Covers the previous calendar month, and like the weekly
        /// action it records what it has produced, so calling it twice does not send twice.
        /// </summary>
        [Route("[action]", Name = "UploadMonthlyLogsNew")]
        public JsonResult UploadMonthly()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            _siteLogUploadService.ProcessMonthlyGuardLogs();

            return new JsonResult(true);
        }
    }
}
