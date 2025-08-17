using CityWatch.Data.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScannerController : ControllerBase
    {
        private readonly IViewDataService _viewDataService;

        public ScannerController(IViewDataService viewDataService)
        {
            _viewDataService = viewDataService;
        }

        [HttpGet("GetScannerControlSettings")]
        public IActionResult GetScannerControlSettings(int siteId)
        {
            try
            {
                var clientSiteScannerOnBoardingSettings = _viewDataService.GetSmartWandTagTypesForClientSite(siteId);                                
                return Ok(clientSiteScannerOnBoardingSettings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        //[HttpGet("GetCrowdCountControlDataAndSettings")]
        //public async Task<IActionResult> GetCrowdCountControlDataAndSettingsAsync(int siteId)
        //{
        //    try
        //    {
        //        var cdto = await _viewDataService.GetCrowdCountControlDataAndSettings(siteId);                                
        //        return Ok(cdto);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        //    }
        //}

        //[HttpGet("ResetCrowdCountControl")]
        //public async Task<IActionResult> ResetCrowdCountControl()
        //{
        //    try
        //    {
        //        await _viewDataService.ResetAllSiteCrowdCountControl();
        //        return Ok("Ok");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        //    }
        //}

        //[HttpPost("SaveGuardLocation")]
        //public async Task<IActionResult> SaveGuardLocation([FromBody] MobileCrowdControlGuard MCCG)
        //{
        //    await _viewDataService.SaveCrowdControlGuardLocation(MCCG);
        //    return Ok("Ok");
        //}

    }
}
