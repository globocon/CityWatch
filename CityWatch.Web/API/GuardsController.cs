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
    public class GuardsController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IGuardReminderService _guardDocumentEmailService;
        private readonly ILogger<GuardsController> _logger;

        public GuardsController(IGuardReminderService guardDocumentEmailService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<GuardsController> logger)
        {
            _guardDocumentEmailService = guardDocumentEmailService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [Route("[action]", Name = "Reminder")]
        [HttpGet]
        public bool Reminder()
        {
            //if (_webHostEnvironment.IsDevelopment())
            //    throw new NotSupportedException("Not supported in development environment");
            try
            {
                _guardDocumentEmailService.Process();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            return true;
        }
    }
}
