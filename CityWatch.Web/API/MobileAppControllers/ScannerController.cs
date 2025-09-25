using CityWatch.Data.Enums;
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
        private readonly IClientDataProvider _clientSitesDataProvider;
        public ScannerController(IViewDataService viewDataService, IClientSiteWandDataProvider clientSiteWandDataProvider, IClientDataProvider clientSitesDataProvider)
        {
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _clientSitesDataProvider = clientSitesDataProvider;
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
                var _smartWandTagsTypes = _clientSiteWandDataProvider.GetSmartWandTagsType();
                var _lastTagScannedRecord = _clientSiteWandDataProvider.GetLastScannedTagDateTime(siteId,TagUid);

                //Check if scanned tag recently with in a minute from the same site
                if (_lastTagScannedRecord != null && _lastTagScannedRecord.LoggedInClientSiteId == siteId && (DateTime.UtcNow - _lastTagScannedRecord.HitUtcDateTime).TotalMinutes < 1)
                {
                    message = "Tag already scanned !!!";
                    return Ok(new { IsSuccess = IsSuccess, tagFound = TagFound, message = message, tagInfoLabel = TagInfoLabel });
                }

                var _ClientSiteTourMode = _clientSitesDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();
                var TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(TagUid, "nfc");


                _clientSiteSmartWandTagsHitLog.LoggedInClientSiteId = siteId;
                _clientSiteSmartWandTagsHitLog.LoggedInGuardId = GuardId;
                _clientSiteSmartWandTagsHitLog.LoggedInUserId = UserId;
                _clientSiteSmartWandTagsHitLog.TagUId = TagUid;
                _clientSiteSmartWandTagsHitLog.HitUtcDateTime = DateTime.UtcNow;
                _clientSiteSmartWandTagsHitLog.TagsTypeId = _smartWandTagsTypes.Where(x => x.value.ToLower().Equals("nfc")).FirstOrDefault()?.Id ?? null;

                if (TagInfoDetails == null)
                {
                    // if tag not found show uid in log book entry
                    IsSuccess = true;
                    message = "Tag Not Found";
                    TagInfoLabel = $"{TagUid} [NFC]";
                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;


                }
                else
                {
                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagInfoDetails.LabelDescription;
                    _clientSiteSmartWandTagsHitLog.TagLinkedClientSiteId = TagInfoDetails.ClientSiteId;
                    if (TagInfoDetails.ClientSiteId != siteId)
                    {
                        if (TagInfoDetails.ClientSiteId == 0)
                        {
                            IsSuccess = true;
                            message = "Tag Not Found";
                            TagInfoLabel = $"{TagUid} [NFC]";
                            _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
                        }
                        else
                        {
                            if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode == PatrolTouringMode.STND)
                            {
                                message = "Tag does not belong to logged in site. Please check.";
                            }
                            else {
                                IsSuccess = true;
                                TagFound = true;
                                message = "Tag Found";
                                TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";                                
                            }                                
                        }
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
                    if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
                    {
                        // If tour mode enabled then log the tour activity
                        ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLogCorrespondingSite = _clientSiteSmartWandTagsHitLog;
                        _clientSiteSmartWandTagsHitLogCorrespondingSite.Id = 0;
                        _clientSiteSmartWandTagsHitLogCorrespondingSite.LoggedInClientSiteId = TagInfoDetails.ClientSiteId;
                        _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLogCorrespondingSite);
                    }
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
