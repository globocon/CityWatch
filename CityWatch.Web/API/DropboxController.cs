using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class DropboxController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDropboxMonitorService _dropboxMonitorService;
        private readonly ILogger<DropboxController> _logger;

        public DropboxController(IWebHostEnvironment webHostEnvironment,
            IDropboxMonitorService dropboxMonitorService,
            ILogger<DropboxController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _dropboxMonitorService = dropboxMonitorService;
            _logger = logger;
        }

        [Route("[action]", Name = "Monitor")]
        [HttpGet]
        public async Task<bool> Monitor()
        {
            if (_webHostEnvironment.IsDevelopment())
                throw new NotSupportedException("Not supported in development environment");

            try
            {
                await _dropboxMonitorService.CreateFolders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                return false;
            }

            return true;
        }
    }
}
