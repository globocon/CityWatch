using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RadioChecksActivityStatusController : ControllerBase
    {
        private readonly IRadioChecksActivityStatusService _radioChecksActivityStatusService;
        private readonly ILogger<GuardsController> _logger;
        public RadioChecksActivityStatusController(IRadioChecksActivityStatusService radioChecksActivityStatusService, ILogger<GuardsController> logger)
        {
            _radioChecksActivityStatusService = radioChecksActivityStatusService;
            _logger = logger;
        }

        [Route("[action]", Name = "RadioChecksActivityStatus")]
        [HttpGet]
        public bool RadioChecksActivityStatus()
        {
           
            try
            {
                _radioChecksActivityStatusService.Process();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            return true;
        }

        [Route("[action]", Name = "RadioChecksManningHours")]
        [HttpGet]
        public bool RadioChecksManningHours()
        {

            try
            {
                _radioChecksActivityStatusService.Process2();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }

            return true;
        }
    }
}
