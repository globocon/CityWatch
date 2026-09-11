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
    }
}
