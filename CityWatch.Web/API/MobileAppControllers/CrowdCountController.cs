using CityWatch.Data.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrowdCountController : ControllerBase
    {
        private readonly IViewDataService _viewDataService;

        public CrowdCountController(IViewDataService viewDataService)
        {
            _viewDataService = viewDataService;
        }

        [HttpGet("GetCrowdCountControlSettings")]
        public IActionResult GetCrowdCountControlSettings(int siteId)
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

        [HttpGet("GetCrowdCountControlDataAndSettings")]
        public async Task<IActionResult> GetCrowdCountControlDataAndSettingsAsync(int siteId)
        {
            try
            {
                var cdto = await _viewDataService.GetCrowdCountControlDataAndSettings(siteId);                                
                return Ok(cdto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("ResetCrowdCountControl")]
        public async Task<IActionResult> ResetCrowdCountControl()
        {
            try
            {
                await _viewDataService.ResetAllSiteCrowdCountControl();
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("SaveGuardLocation")]
        public async Task<IActionResult> SaveGuardLocation([FromBody] MobileCrowdControlGuard MCCG)
        {
            await _viewDataService.SaveCrowdControlGuardLocation(MCCG);
            return Ok("Ok");
        }


        //// For Testing Purpose Only not used in Production
        //[HttpGet("GetCurrentCrowdControlData")]
        //public async Task<IActionResult> GetCurrentCrowdControlData(MobileCrowdControlGuard JoinGaurd)
        //{
        //    try
        //    {
        //        var currentCount = await _viewDataService.GetCrowdControlCount(JoinGaurd);
        //        return Ok(currentCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        //    }
        //}


    }
}
