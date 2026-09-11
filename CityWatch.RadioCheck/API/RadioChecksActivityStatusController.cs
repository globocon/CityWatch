using CityWatch.RadioCheck.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace CityWatch.RadioCheck.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RadioChecksActivityStatusController : ControllerBase
    {
        private readonly IRadioChecksActivityStatusService _radioChecksActivityStatusService;
       
        public RadioChecksActivityStatusController(IRadioChecksActivityStatusService radioChecksActivityStatusService)
        {
            _radioChecksActivityStatusService = radioChecksActivityStatusService;
           
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
                return false;
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
                return false;
            }

            return true;
        }
    }
}
