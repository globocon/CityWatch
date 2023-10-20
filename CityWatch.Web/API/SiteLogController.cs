using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;

namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class SiteLogController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISiteLogUploadService _siteLogUploadService;

        public SiteLogController(IWebHostEnvironment webHostEnvironment,
            ISiteLogUploadService siteLogUploadService)
        {
            _webHostEnvironment = webHostEnvironment;
            _siteLogUploadService = siteLogUploadService;
        }

        [Route("[action]", Name = "UploadLogs")]
        public JsonResult Upload()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Dropbox upload not supported in development environment");

            _siteLogUploadService.ProcessDailyGuardLogs();

            return new JsonResult(true);
        }
        [Route("[action]", Name = "UploadLogsSecondRun")]
        public JsonResult UploadSecondRun()
        {
            //if (_webHostEnvironment.IsDevelopment())
            //    throw new NotSupportedException("Dropbox upload not supported in development environment");

            _siteLogUploadService.ProcessDailyGuardLogsSecondRun();

            return new JsonResult(true);
        }
    }
}
