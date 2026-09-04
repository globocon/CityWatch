using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
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
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IMobileAppDataServices _mobileAppDataServices;
        private readonly IGuardLogDataProvider _guardLogDataProvider;

        public ScannerController(IViewDataService viewDataService, IClientSiteWandDataProvider clientSiteWandDataProvider,
            IClientDataProvider clientSitesDataProvider, IGuardDataProvider guardDataProvider,
            IMobileAppDataServices mobileAppDataServices, IGuardLogDataProvider guardLogDataProvider)
        {
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _clientSitesDataProvider = clientSitesDataProvider;
            _guardDataProvider = guardDataProvider;
            _mobileAppDataServices = mobileAppDataServices;
            _guardLogDataProvider = guardLogDataProvider;
        }

        [HttpGet("GetScannerControlSettings")]
        public IActionResult GetScannerControlSettings(int siteId)
        {
            try
            {
                List<string> clientSiteScannerOnBoardingSettings = new List<string>();
                //Check if tour mode is enabled for the site then allow nfc and bluetooth tag scanning
                var _ClientSiteTourMode = _clientSitesDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();
                if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
                {
                    clientSiteScannerOnBoardingSettings = _viewDataService.GetSmartWandTagTypes().Distinct().Select(x => x.value).ToList();
                }
                else
                {
                    clientSiteScannerOnBoardingSettings = _viewDataService.GetSmartWandTagTypesForClientSite(siteId);
                }


                return Ok(clientSiteScannerOnBoardingSettings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("GetScannerTagInfoData")]
        public async Task<IActionResult> GetScannerTagInfoData(int siteId, string TagUid, int GuardId, int UserId, int TagsTypeId, int? SmartWandId = null, string gpsCoordinates = null)
        {
            bool IsSuccess = false;
            string message = "An error occurred.";
            bool TagFound = false;
            string TagInfoLabel = string.Empty;
            int ScannedFromLinkedSite = siteId;
            int rowIdInServer = 0;
            int tagSiteId = 0;
            string tagSiteName = string.Empty;
            if (!string.IsNullOrEmpty(gpsCoordinates))
            {
                gpsCoordinates = Uri.UnescapeDataString(gpsCoordinates.Trim());
            }
            

            try
            {
                var (IsSuccessR, TagFoundR, messageR, TagInfoLabelR, ScanFromLinkedSiteId, RowIdInServerR, TagSiteIdR, TagSiteNameR) = await _mobileAppDataServices.CreateSmartWandScannerHitLogRecord(siteId, TagUid, GuardId, UserId, false,
                    Guid.NewGuid(), DateTime.UtcNow, (ScanningType)TagsTypeId, gpsCoordinates, SmartWandId);
                IsSuccess = IsSuccessR;
                message = messageR;
                TagFound = TagFoundR;
                TagInfoLabel = TagInfoLabelR;
                ScannedFromLinkedSite = ScanFromLinkedSiteId;
                rowIdInServer = RowIdInServerR;
                tagSiteId = TagSiteIdR;
                tagSiteName = TagSiteNameR;

            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return Ok(new { IsSuccess = IsSuccess, tagFound = TagFound, message = message, tagInfoLabel = TagInfoLabel, ScannedFromLinkedSite, RowIdInServer = rowIdInServer, TagSiteId = tagSiteId, TagSiteName = tagSiteName });
        }

        [HttpPost("SyncOfflineSmartWandTagHitData")]
        public async Task<IActionResult> SyncOfflineSmartWandTagHitData([FromBody] List<ClientSiteSmartWandTagsHitLogCacheOffline> offlineRecords)
        {
            var IPAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            if (offlineRecords != null && offlineRecords.Count > 0)
            {
                foreach (var offlineRecord in offlineRecords)
                {
                    try
                    {
                        //Save tag hit 
                        var (IsSuccessR, TagFoundR, messageR, TagInfoLabelR, ScanFromLinkedSiteId, RowIdInServerR, TagSiteIdR, TagSiteNameR) = await _mobileAppDataServices.CreateSmartWandScannerHitLogRecord(offlineRecord.LoggedInClientSiteId,
                            offlineRecord.TagUId, offlineRecord.LoggedInGuardId, offlineRecord.LoggedInUserId, true, offlineRecord.UniqueRecordId,
                            offlineRecord.HitUtcDateTime, (ScanningType)offlineRecord.TagsTypeId, offlineRecord.GPScoordinates, offlineRecord.SmartWandId);

                        int postToClientSiteId = ScanFromLinkedSiteId > 0 ? ScanFromLinkedSiteId : offlineRecord.LoggedInClientSiteId;

                        if (IsSuccessR)
                        {
                            PostActivityRequest request = new PostActivityRequest()
                            {
                                guardId = offlineRecord.LoggedInGuardId,
                                clientsiteId = postToClientSiteId,
                                userId = offlineRecord.LoggedInUserId,
                                activityString = TagInfoLabelR,
                                gps = offlineRecord.GPScoordinates,
                                systemEntry = true,
                                scanningType = offlineRecord.TagsTypeId,
                                tagUID = offlineRecord.TagUId,
                                EventDateTimeLocal = offlineRecord.EventDateTimeLocal,
                                EventDateTimeLocalWithOffset = offlineRecord.EventDateTimeLocalWithOffset,
                                EventDateTimeZone = offlineRecord.EventDateTimeZone,
                                EventDateTimeZoneShort = offlineRecord.EventDateTimeZoneShort,
                                EventDateTimeUtcOffsetMinute = offlineRecord.EventDateTimeUtcOffsetMinute,
                                IsOfflineRecord = true,
                                OfflineRecordSyncDateTime = DateTime.Now,
                                TagScanHitLogRefId = RowIdInServerR,
                                EventMobileUtcDateTime = offlineRecord.HitUtcDateTime
                            };

                            //Create Logbook entries                        
                            var (IsSuccessLR, msgLR, guardLoginIdLR) = _mobileAppDataServices.PostMobileLogActivity(request, IPAddress);
                            if (!IsSuccessLR)
                            {
                                // Save the record in DB to process later.
                                SaveSyncOfflineSmartWandTagHitDataError(offlineRecord, msgLR);
                            }

                            Thread.Sleep(500); //wait a while since signalR pushes the refresh signal for logbook refresh
                        }
                        else
                        {
                            // Save the record in DB to process later.
                            SaveSyncOfflineSmartWandTagHitDataError(offlineRecord, messageR);
                        }

                        offlineRecord.IsSynced = true;
                    }
                    catch (Exception ex)
                    {
                        SaveSyncOfflineSmartWandTagHitDataError(offlineRecord, ex.ToString());
                        offlineRecord.IsSynced = true;
                    }
                }
            }

            return Ok(offlineRecords);

        }


        [HttpGet("CheckIfGuardHasTagAddAccess")]
        public IActionResult CheckIfGuardHasTagAddAccess(int GuardId)
        {
            var hasAccess = false;
            try
            {
                var guardDetails = _guardDataProvider.GetGuardDetailsUsingId(GuardId).FirstOrDefault();
                if (guardDetails != null && guardDetails.IsMobileAppPlusTags)
                {
                    hasAccess = true;
                }
                return Ok(hasAccess);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPost("SaveNFCtagInfoData")]
        public IActionResult SaveNFCtagInfoData([FromBody] ClientSiteSmartWandTags csswt)
        {
            var IsSuccess = false;
            var message = string.Empty;
            var TagFound = false;
            try
            {
                _clientSiteWandDataProvider.SaveClientSiteSmartWandTags(csswt);
                IsSuccess = true;
                TagFound = true;
                message = "Tag saved successfully.";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return Ok(new { IsSuccess = IsSuccess, tagFound = TagFound, message = message, tagInfoLabel = csswt.LabelDescription });
        }


        [HttpPost("CheckAndRegisterDeviceWithSmartWand")]
        public IActionResult CheckAndRegisterDeviceWithSmartWand([FromBody] SmartWandDeviceRegister csswt)
        {
            try
            {
                var res = _viewDataService.CheckAndRegisterDeviceWithSmartWand(csswt);
                return Ok(res);
            }
            catch (Exception ex)
            {
                csswt.Message = ex.Message;
            }

            return Ok(csswt);
        }

        [HttpPost("CheckIfSmartWandIsDeRegisteredAsync")]
        public IActionResult CheckIfSmartWandIsDeRegisteredAsync([FromBody] string deviceid)
        {

            try
            {
                var res = _viewDataService.CheckIfSmartWandIsDeRegisteredAsync(deviceid);
                return Ok(res);
            }
            catch (Exception)
            {
                return Ok(false);
            }
        }

        [HttpPost("GetSmartWandByDeviceId")]
        public IActionResult GetSmartWandByDeviceId([FromBody] string deviceid)
        {
            try
            {
                var res = _viewDataService.GetSmartWandIdFromDeviceId(deviceid);
                return Ok(res);
            }
            catch (Exception)
            {
                return Ok(0);
            }
        }

        [HttpGet("GetClientSiteSmartWands")]
        public IActionResult GetClientSiteSmartWands(int siteId)
        {
            try
            {
                //GetClientSiteSmartWands
                var smartWandList = _viewDataService.GetClientSiteSmartWandListForMobile(siteId);

                if (smartWandList == null || !smartWandList.Any())
                    return NotFound(new { message = "No smart wands found." });

                return Ok(smartWandList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpGet("GetClientSiteAllSmartWandTags")]
        public IActionResult GetClientSiteAllSmartWandTags(int siteId)
        {
            try
            {
                //GetClientSiteSmartWandsTags
                int[] clientSiteIds;
                List<ClientSiteSmartWandTags> smartWandTagList = new();

                var clientsiteDetails = _clientSitesDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();
                var PatrolTourMode = clientsiteDetails != null ? clientsiteDetails.PatrolTourMode : PatrolTouringMode.STND;
                if (PatrolTourMode != PatrolTouringMode.STND)
                {
                    // If tour mode is INSP or PCAR then get the tags of all sites.
                    smartWandTagList = _clientSiteWandDataProvider.GetAllClientSitesSmartwandTags();
                }
                else
                {
                    var ls = _guardLogDataProvider.getallClientSitesLinkedDuress(siteId);

                    if (ls != null && ls.Count > 0)
                    {
                        // If there are linked sites then get the tags of linked sites also.
                        clientSiteIds = ls.Select(x => x.ClientSiteId).ToArray();
                    }
                    else
                    {
                        // If there are no linked sites then get the tags of current site.
                        clientSiteIds = new[] { siteId };
                    }

                    smartWandTagList = _viewDataService.GetClientSiteTagIds(clientSiteIds);
                }

                //List<ClientSiteSmartWandTagsLocal> nwswtL = new List<ClientSiteSmartWandTagsLocal>();
                //foreach (var item in smartWandTagList)
                //{
                //    nwswtL.Add(new ClientSiteSmartWandTagsLocal()
                //    {
                //        Id = item.Id,
                //        ClientSiteId = item.ClientSiteId,
                //        UId = item.UId,
                //        TagsTypeId = item.TagsTypeId,
                //        LabelDescription = item.LabelDescription,
                //        FqBypass = item.FqBypass,
                //        TagsType = item.TagsType
                //    });
                //}


                List<ClientSiteSmartWandTagsLocal> nwswtL = smartWandTagList
                    .Select(item => new ClientSiteSmartWandTagsLocal
                    {
                        Id = item.Id,
                        ClientSiteId = item.ClientSiteId,
                        UId = item.UId,
                        TagsTypeId = item.TagsTypeId,
                        LabelDescription = item.LabelDescription,
                        FqBypass = item.FqBypass,
                        TagsType = item.TagsType
                    })
                    .ToList();


                return Ok(nwswtL);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        private bool SaveSyncOfflineSmartWandTagHitDataError(ClientSiteSmartWandTagsHitLogCacheOffline _oR, string syncError)
        {
            bool IsSuccess = false;
            ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced _offlineRecordsNotSynced = new ClientSiteSmartWandTagsHitLogCacheOfflineNotSynced()
            {
                Id = _oR.Id,
                LoggedInClientSiteId = _oR.LoggedInClientSiteId,
                LoggedInUserId = _oR.LoggedInUserId,
                LoggedInGuardId = _oR.LoggedInGuardId,
                TagUId = _oR.TagUId,
                TagsTypeId = _oR.TagsTypeId,
                HitUtcDateTime = _oR.HitUtcDateTime,
                HitLocalDateTime = _oR.HitLocalDateTime,
                LastModifiedUtc = _oR.LastModifiedUtc,
                SmartWandId = _oR.SmartWandId,
                GPScoordinates = _oR.GPScoordinates,
                IsSynced = _oR.IsSynced,
                UniqueRecordId = _oR.UniqueRecordId,
                EventDateTimeLocal = _oR.EventDateTimeLocal,
                EventDateTimeLocalWithOffset = _oR.EventDateTimeLocalWithOffset,
                EventDateTimeZone = _oR.EventDateTimeZone,
                EventDateTimeZoneShort = _oR.EventDateTimeZoneShort,
                EventDateTimeUtcOffsetMinute = _oR.EventDateTimeUtcOffsetMinute,
                DeviceId = _oR.DeviceId,
                DeviceName = _oR.DeviceName,
                SyncTime = DateTime.Now,
                NotSyncError = syncError,
                IsScanFromLinkedSite = _oR.IsScanFromLinkedSite
            };

            try
            {
                IsSuccess = _clientSiteWandDataProvider.SaveOfflineSmartWandTagHitDataRecordError(_offlineRecordsNotSynced);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.ToString()}");
            }

            return IsSuccess;
        }

    }

    public class ClientSiteSmartWandTagsLocal
    {
        public int Id { get; set; }
        public int ClientSiteId { get; set; }
        public string UId { get; set; }
        public int TagsTypeId { get; set; }
        public string LabelDescription { get; set; }
        public bool FqBypass { get; set; }
        public string TagsType { get; set; }
    }
}
