using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace CityWatch.Web.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScannerController : ControllerBase
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;

        public ScannerController(IViewDataService viewDataService, IClientSiteWandDataProvider clientSiteWandDataProvider)
        {
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
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

        [HttpGet("GetNFCtagInfoData")]
        public IActionResult GetNFCtagInfoData(int siteId, string TagUid, int GuardId, int UserId)
        {
            bool IsSuccess = false;
            string message = "An error occurred.";
            bool TagFound = false;
            string TagInfoLabel = string.Empty;
            ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLog = new ClientSiteSmartWandTagsHitLog();
            try
            {
                var TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(TagUid, "nfc");

                _clientSiteSmartWandTagsHitLog.LoggedInClientSiteId = siteId;
                _clientSiteSmartWandTagsHitLog.LoggedInGuardId = GuardId;
                _clientSiteSmartWandTagsHitLog.LoggedInUserId = UserId;
                _clientSiteSmartWandTagsHitLog.TagUId = TagUid;
                _clientSiteSmartWandTagsHitLog.HitUtcDateTime = DateTime.UtcNow;

                if (TagInfoDetails == null)
                {
                    // if tag not found show uid in log book entry
                    IsSuccess = true;
                    message = "Tag Not Found";
                    TagInfoLabel = $"{TagUid} [NFC]";
                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
                    var _smartWandTagsTypes = _clientSiteWandDataProvider.GetSmartWandTagsType();
                    _clientSiteSmartWandTagsHitLog.TagsTypeId = _smartWandTagsTypes.Where(x => x.value.ToLower().Equals("nfc")).FirstOrDefault()?.Id ?? null;
                }
                else
                {
                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagInfoDetails.LabelDescription;
                    _clientSiteSmartWandTagsHitLog.TagsTypeId = TagInfoDetails.TagsTypeId;
                    _clientSiteSmartWandTagsHitLog.TagLinkedClientSiteId = TagInfoDetails.ClientSiteId;
                    if (TagInfoDetails.ClientSiteId != siteId)
                    {
                        message = "Tag does not belong to logged in site.";
                    }
                    else
                    {
                        IsSuccess = true;
                        TagFound = true;
                        message = "Tag Found";
                        TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";
                    }
                }
                try
                {   
                    // Log the tag details
                    _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLog);
                }
                catch (Exception exp)
                {

                }
            }
            catch (Exception ex)
            {
                //return StatusCode(500, new { message = "An error occurred", error = ex.Message });
                message = ex.Message;
            }

            return Ok(new { IsSuccess = IsSuccess, tagFound = TagFound, message = message, tagInfoLabel = TagInfoLabel });
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
