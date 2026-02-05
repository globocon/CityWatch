using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Services
{
    public interface IMobileAppDataServices
    {
        //public Task<(bool IsSuccess, bool TagFound, string message, string TagInfoLabel)> CreateSmartWandNFCHitLogRecord(int siteId, string TagUid, int GuardId,
        //    int UserId, bool IsOfflineRecord, Guid uniqueRecordID, DateTime HitUtcDateTime, int? SmartWandId = null);

        public Task<(bool IsSuccess, bool TagFound, string message, string TagInfoLabel, int ScanFromLinkedSiteId)> CreateSmartWandScannerHitLogRecord(int siteId, string TagUid, int GuardId,
          int UserId, bool IsOfflineRecord, Guid uniqueRecordID, DateTime HitUtcDateTime, ScanningType scanningType, int? SmartWandId = null);

        public (bool IsSuccess, string msg, int guardLoginId) PostMobileLogActivity(PostActivityRequest request, string IPAddress);
        //public (bool IsSuccess, string msg, int guardLoginId) PostMobileLogActivity(int guardId, int clientsiteId, int userId, string activityString, string gps,
        //    string IPAddress, DateTime LogDateTime, bool systemEntry = true, int scanningType = 0, string tagUID = "NA");

        public int GetGuardLoginId(int logBookId, int guardId, int clientsiteId, int userId, string IPAddress);
    }
    public class MobileAppDataServices: IMobileAppDataServices
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientDataProvider _clientDataProvider;
        private readonly IHubContext<MobileAppSignalRHub> _hubContext;

        public MobileAppDataServices(IViewDataService viewDataService, IClientSiteWandDataProvider clientSiteWandDataProvider, 
            IClientDataProvider clientDataProvider, IGuardDataProvider guardDataProvider,
            ILogbookDataService logbookDataService, IGuardLogDataProvider guardLogDataProvider, IHubContext<MobileAppSignalRHub> hubContext)
        {
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _logbookDataService = logbookDataService;
            _guardLogDataProvider = guardLogDataProvider;
            _hubContext = hubContext;
        }

        public async Task<(bool IsSuccess, bool TagFound, string message, string TagInfoLabel,int ScanFromLinkedSiteId)> CreateSmartWandScannerHitLogRecord(int siteId, string TagUid, int GuardId,
           int UserId, bool IsOfflineRecord, Guid uniqueRecordID, DateTime HitUtcDateTime,ScanningType scanningType ,int? SmartWandId = null)
        {
            bool IsSuccess = false;
            string message = "An error occurred.";
            bool TagFound = false;
            string TagInfoLabel = string.Empty;
            int ScanFromLinkedSiteId = siteId;
            ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLog = new ClientSiteSmartWandTagsHitLog();
            try
            {
                var _smartWandTagsTypes = _clientSiteWandDataProvider.GetSmartWandTagsType();
                var _lastTagScannedRecord = _clientSiteWandDataProvider.GetLastScannedTagDateTime(siteId, TagUid);

                //Check if scanned tag recently with in a minute from the same site
                if (!IsOfflineRecord)
                {
                    if (_lastTagScannedRecord != null && _lastTagScannedRecord.LoggedInClientSiteId == siteId && (DateTime.UtcNow - _lastTagScannedRecord.HitUtcDateTime).TotalSeconds < 60)
                    {
                        if (scanningType == ScanningType.NFC) { message = "Tag already scanned !!!"; }
                        else if (scanningType == ScanningType.BLUETOOTH) { message = "iBeacon already scanned !!!"; }

                        return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId);
                    }
                }
                var _ClientSiteTourMode = _clientDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();
                ScannerTagDetails TagInfoDetails = new ScannerTagDetails();
                if (scanningType == ScanningType.NFC) { TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(TagUid, "nfc"); }
                else if (scanningType == ScanningType.BLUETOOTH) { TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(TagUid, "bluetooth"); }

                int? tagtypeid = 0;
                if (scanningType == ScanningType.NFC) { tagtypeid = _smartWandTagsTypes.Where(x => x.value.ToLower() == "nfc").FirstOrDefault()?.Id ?? null; }
                else if (scanningType == ScanningType.BLUETOOTH) { tagtypeid = _smartWandTagsTypes.Where(x => x.value.ToLower() == "bluetooth").FirstOrDefault()?.Id ?? null; }

                _clientSiteSmartWandTagsHitLog.LoggedInClientSiteId = siteId;
                _clientSiteSmartWandTagsHitLog.LoggedInGuardId = GuardId;
                _clientSiteSmartWandTagsHitLog.LoggedInUserId = UserId;
                _clientSiteSmartWandTagsHitLog.TagUId = TagUid;
                _clientSiteSmartWandTagsHitLog.HitUtcDateTime = HitUtcDateTime;
                _clientSiteSmartWandTagsHitLog.TagsTypeId = tagtypeid;
                _clientSiteSmartWandTagsHitLog.SmartWandId = SmartWandId ?? 0;
                _clientSiteSmartWandTagsHitLog.IsOfflineRecord = IsOfflineRecord;

                if (IsOfflineRecord)
                {
                    _clientSiteSmartWandTagsHitLog.UniqueRecordId = uniqueRecordID;
                    _clientSiteSmartWandTagsHitLog.OfflineRecordSyncUtcDateTime = DateTime.UtcNow;
                }

                if (TagInfoDetails == null)
                {

                    if (scanningType == ScanningType.NFC)
                    {  // if tag not found show uid in log book entry
                        IsSuccess = true;
                        message = "Tag Not Found";
                        TagInfoLabel = $"{TagUid} [NFC]";
                        _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
                    }
                    else if (scanningType == ScanningType.BLUETOOTH)
                    {  // if ibeacon not found dont show in log book entry
                        message = "iBeacon Not Found";
                        return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId);
                    }
                }
                else
                {
                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagInfoDetails.LabelDescription;
                    _clientSiteSmartWandTagsHitLog.TagLinkedClientSiteId = TagInfoDetails.ClientSiteId;
                    if (TagInfoDetails.ClientSiteId != siteId)
                    {
                        if (TagInfoDetails.ClientSiteId == 0)
                        {
                            if (scanningType == ScanningType.NFC)
                            {
                                IsSuccess = true;
                                message = "Tag Not Found";
                                TagInfoLabel = $"{TagUid} [NFC]";
                                _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
                            }
                            else if (scanningType == ScanningType.BLUETOOTH)
                            {  // if ibeacon
                                message = "iBeacon Not Found";
                                return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId);
                            }
                        }
                        else
                        {
                            if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode == PatrolTouringMode.STND)
                            {
                                bool isTagSiteLinked = false;
                                List<RCLinkedDuressClientSites> _rcLinkedClientSites = new();

                                var getallRCLinkedDuressMaster = _guardLogDataProvider.getallRCLinkedDuressMaster();
                                _rcLinkedClientSites = _guardLogDataProvider.getallClientSitesLinkedDuress(siteId);
                                var _check = getallRCLinkedDuressMaster.Where(x => x.Id == _rcLinkedClientSites.FirstOrDefault().RCLinkedId).FirstOrDefault();
                                if (_check != null)
                                {
                                    if (!_check.IsSW)
                                    {
                                        //allow only if smartwand is enabled in linked sites
                                        _rcLinkedClientSites = new List<RCLinkedDuressClientSites>();
                                    }
                                }
                                
                                if (_rcLinkedClientSites != null && _rcLinkedClientSites.Count > 0)
                                {
                                    if (_rcLinkedClientSites.Any(x => x.ClientSiteId == TagInfoDetails.ClientSiteId))
                                    {
                                        isTagSiteLinked = true;
                                        ScanFromLinkedSiteId = TagInfoDetails.ClientSiteId;

                                        if (scanningType == ScanningType.NFC)
                                        {
                                            IsSuccess = true;
                                            TagFound = true;
                                            message = "Tag Found";
                                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";
                                        }
                                        else if (scanningType == ScanningType.BLUETOOTH)
                                        {  // if ibeacon
                                            IsSuccess = true;
                                            TagFound = true;
                                            message = "iBeacon Found";
                                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} [BLE]";
                                        }

                                    }
                                    else
                                    {
                                        if (scanningType == ScanningType.NFC)
                                            message = "Tag does not belong to logged in site. Please check.";
                                        else if (scanningType == ScanningType.BLUETOOTH)
                                            message = "iBeacon does not belong to logged in site. Please check.";
                                    }
                                }
                                else
                                {
                                    if (scanningType == ScanningType.NFC)
                                        message = "Tag does not belong to logged in site. Please check.";
                                    else if (scanningType == ScanningType.BLUETOOTH)
                                        message = "iBeacon does not belong to logged in site. Please check.";
                                }

                                _clientSiteSmartWandTagsHitLog.IsScanFromLinkedSite = isTagSiteLinked;
                            }
                            else
                            {
                                if (scanningType == ScanningType.NFC)
                                {
                                    IsSuccess = true;
                                    TagFound = true;
                                    message = "Tag Found";
                                    TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";
                                }
                                else if (scanningType == ScanningType.BLUETOOTH)
                                {  // if ibeacon
                                    IsSuccess = true;
                                    TagFound = true;
                                    message = "iBeacon Found";
                                    TagInfoLabel = $"{TagInfoDetails.LabelDescription} [BLE]";
                                }
                            }
                        }
                    }
                    else
                    {
                        if (scanningType == ScanningType.NFC)
                        {
                            IsSuccess = true;
                            TagFound = true;
                            message = "Tag Found";
                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";
                        }
                        else if (scanningType == ScanningType.BLUETOOTH)
                        {  // if ibeacon
                            IsSuccess = true;
                            TagFound = true;
                            message = "iBeacon Found";
                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} [BLE]";
                        }
                    }
                }
                try
                {
                    // Log the tag details
                    _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLog);
                    if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
                    {
                        // If tour mode enabled then log the tour activity  
                        if (siteId != TagInfoDetails.ClientSiteId && TagInfoDetails != null && TagInfoDetails?.ClientSiteId > 0)
                        {
                            ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLogCorrespondingSite = _clientSiteSmartWandTagsHitLog;
                            _clientSiteSmartWandTagsHitLogCorrespondingSite.Id = 0;
                            _clientSiteSmartWandTagsHitLogCorrespondingSite.LoggedInClientSiteId = TagInfoDetails.ClientSiteId;
                            _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLogCorrespondingSite);
                        }
                    }
                }
                catch (Exception exp)
                {

                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId);
        }

        //public async Task<(bool IsSuccess, bool TagFound, string message, string TagInfoLabel)> CreateSmartWandNFCHitLogRecord(int siteId, string TagUid, int GuardId,
        //   int UserId, bool IsOfflineRecord, Guid uniqueRecordID, DateTime HitUtcDateTime, int? SmartWandId = null)
        //{
        //    bool IsSuccess = false;
        //    string message = "An error occurred.";
        //    bool TagFound = false;
        //    string TagInfoLabel = string.Empty;
        //    ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLog = new ClientSiteSmartWandTagsHitLog();
        //    try
        //    {
        //        var _smartWandTagsTypes = _clientSiteWandDataProvider.GetSmartWandTagsType();
        //        var _lastTagScannedRecord = _clientSiteWandDataProvider.GetLastScannedTagDateTime(siteId, TagUid);

        //        //Check if scanned tag recently with in a minute from the same site
        //        if (!IsOfflineRecord)
        //        {
        //            if (_lastTagScannedRecord != null && _lastTagScannedRecord.LoggedInClientSiteId == siteId && (DateTime.UtcNow - _lastTagScannedRecord.HitUtcDateTime).TotalSeconds < 60)
        //            {
        //                message = "Tag already scanned !!!";
        //                return (IsSuccess, TagFound, message, TagInfoLabel);
        //            }
        //        }
        //        var _ClientSiteTourMode = _clientDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();
        //        var TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(TagUid, "nfc");


        //        _clientSiteSmartWandTagsHitLog.LoggedInClientSiteId = siteId;
        //        _clientSiteSmartWandTagsHitLog.LoggedInGuardId = GuardId;
        //        _clientSiteSmartWandTagsHitLog.LoggedInUserId = UserId;
        //        _clientSiteSmartWandTagsHitLog.TagUId = TagUid;
        //        _clientSiteSmartWandTagsHitLog.HitUtcDateTime = HitUtcDateTime;
        //        _clientSiteSmartWandTagsHitLog.TagsTypeId = _smartWandTagsTypes.Where(x => x.value.ToLower().Equals("nfc")).FirstOrDefault()?.Id ?? null;
        //        _clientSiteSmartWandTagsHitLog.SmartWandId = SmartWandId ?? 0;
        //        _clientSiteSmartWandTagsHitLog.IsOfflineRecord = IsOfflineRecord;

        //        if (IsOfflineRecord)
        //        {
        //            _clientSiteSmartWandTagsHitLog.UniqueRecordId = uniqueRecordID;
        //            _clientSiteSmartWandTagsHitLog.OfflineRecordSyncUtcDateTime = DateTime.UtcNow;
        //        }

        //        if (TagInfoDetails == null)
        //        {
        //            // if tag not found show uid in log book entry
        //            IsSuccess = true;
        //            message = "Tag Not Found";
        //            TagInfoLabel = $"{TagUid} [NFC]";
        //            _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;


        //        }
        //        else
        //        {
        //            _clientSiteSmartWandTagsHitLog.LabelDescription = TagInfoDetails.LabelDescription;
        //            _clientSiteSmartWandTagsHitLog.TagLinkedClientSiteId = TagInfoDetails.ClientSiteId;
        //            if (TagInfoDetails.ClientSiteId != siteId)
        //            {
        //                if (TagInfoDetails.ClientSiteId == 0)
        //                {
        //                    IsSuccess = true;
        //                    message = "Tag Not Found";
        //                    TagInfoLabel = $"{TagUid} [NFC]";
        //                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
        //                }
        //                else
        //                {
        //                    if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode == PatrolTouringMode.STND)
        //                    {
        //                        message = "Tag does not belong to logged in site. Please check.";
        //                    }
        //                    else
        //                    {
        //                        IsSuccess = true;
        //                        TagFound = true;
        //                        message = "Tag Found";
        //                        TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                IsSuccess = true;
        //                TagFound = true;
        //                message = "Tag Found";
        //                TagInfoLabel = $"{TagInfoDetails.LabelDescription} [NFC]";
        //            }
        //        }
        //        try
        //        {
        //            // Log the tag details
        //            _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLog);
        //            if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
        //            {
        //                // If tour mode enabled then log the tour activity  
        //                if (siteId != TagInfoDetails.ClientSiteId && TagInfoDetails != null && TagInfoDetails?.ClientSiteId > 0)
        //                {
        //                    ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLogCorrespondingSite = _clientSiteSmartWandTagsHitLog;
        //                    _clientSiteSmartWandTagsHitLogCorrespondingSite.Id = 0;
        //                    _clientSiteSmartWandTagsHitLogCorrespondingSite.LoggedInClientSiteId = TagInfoDetails.ClientSiteId;
        //                    _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLogCorrespondingSite);
        //                }
        //            }
        //        }
        //        catch (Exception exp)
        //        {

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    return (IsSuccess, TagFound, message, TagInfoLabel);
        //}


        public (bool IsSuccess, string msg, int guardLoginId) PostMobileLogActivity(PostActivityRequest request, string IPAddress)
        {
            bool IsSuccess = false;
            int guardLoginId = 0;
            string msg = "";

            if (request.guardId <= 0 || request.clientsiteId <= 0)
            {
                msg = "Invalid guard ID or client site ID.";
                return (IsSuccess, msg, guardLoginId);
            }


            var logBookType = LogBookType.DailyGuardLog;
            var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(request.clientsiteId, logBookType, request.EventDateTimeLocal.Value.Date);

            if (logBookId <= 0)
            {
                msg = "Failed to retrieve logbook ID.";
                return (IsSuccess, msg, guardLoginId);
            }


            // Get Guard Login ID
            guardLoginId = GetGuardLoginId(logBookId, request.guardId, request.clientsiteId, request.userId, IPAddress);

            if (guardLoginId <= 0)
            {
                msg = "Guard login failed.";
                return (IsSuccess, msg, guardLoginId);
            }

            // Default GPS coordinates (should be replaced with actual values if available)
            var gpsCoordinates = request.gps;

            var _scanningType = (ScanningType)request.scanningType;

            // Create a log entry
            var signInEntry = new GuardLog
            {
                ClientSiteLogBookId = logBookId,
                GuardLoginId = guardLoginId,
                EventDateTime = DateTime.Now,
                Notes = request.activityString,
                IsSystemEntry = request.systemEntry,
                EventDateTimeLocal = request.EventDateTimeLocal ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                EventDateTimeLocalWithOffset = request.EventDateTimeLocalWithOffset ?? TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                EventDateTimeZone = request.EventDateTimeZone ?? TimeZoneHelper.GetCurrentTimeZone(),
                EventDateTimeZoneShort = request.EventDateTimeZoneShort ?? TimeZoneHelper.GetCurrentTimeZoneShortName(),
                EventDateTimeUtcOffsetMinute = request.EventDateTimeUtcOffsetMinute ?? TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                GpsCoordinates = gpsCoordinates,
                WAND_TAG_ENTRY_TYPE = _scanningType,
                IsOfflineRecord = request.IsOfflineRecord,
                OfflineRecordSyncDateTime = request.OfflineRecordSyncDateTime
            };

            _guardLogDataProvider.SaveGuardLog(signInEntry);

            //Check if tour mode is enabled for the site then log into corresponding tag attached site also
            var _ClientSiteTourMode = _clientDataProvider.GetClientSiteDetailsWithId(request.clientsiteId).FirstOrDefault();
            if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND && !string.Equals(request.tagUID, "NA"))
            {
                var TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(request.tagUID, "nfc");
                if (TagInfoDetails != null && TagInfoDetails?.ClientSiteId > 0)
                {
                    var _CorrespondingSitelogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(TagInfoDetails.ClientSiteId, logBookType);
                    guardLoginId = GetGuardLoginId(_CorrespondingSitelogBookId, request.guardId, TagInfoDetails.ClientSiteId, request.userId, IPAddress);


                    // If tour mode enabled then log the tour activity
                    GuardLog _CorrespondingSiteLogEntry = signInEntry;
                    _CorrespondingSiteLogEntry.Id = 0;
                    _CorrespondingSiteLogEntry.ClientSiteLogBookId = _CorrespondingSitelogBookId;
                    _CorrespondingSiteLogEntry.GuardLoginId = guardLoginId;
                    if (request.clientsiteId != TagInfoDetails.ClientSiteId)
                    {
                        _guardLogDataProvider.SaveGuardLog(_CorrespondingSiteLogEntry);
                    }
                }

            }

            // Notify all SignalR clients in this ClientSiteId group to refresh the tag scan status
            if (_scanningType != ScanningType.Normal)
            {
                _hubContext.Clients.Group(request.clientsiteId.ToString()).SendAsync("RefreshTagScanStatus");
            }

            IsSuccess = true;
            msg = "Guard successfully logged in.";
            return (IsSuccess, msg, guardLoginId);
        }


        //public (bool IsSuccess, string msg, int guardLoginId) PostMobileLogActivity(int guardId, int clientsiteId, int userId, string activityString, string gps, 
        //    string IPAddress,DateTime LogDateTime, bool systemEntry = true, int scanningType = 0, string tagUID = "NA")
        //{
        //    bool IsSuccess = false;
        //    int guardLoginId = 0;
        //    string msg = "";

        //    if (guardId <= 0 || clientsiteId <= 0)
        //    {
        //        msg = "Invalid guard ID or client site ID.";
        //        return(IsSuccess, msg, guardLoginId);
        //    }
                

        //    var logBookType = LogBookType.DailyGuardLog;
        //    var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientsiteId, logBookType, LogDateTime.Date);

        //    if (logBookId <= 0)
        //    {
        //        msg = "Failed to retrieve logbook ID.";
        //        return (IsSuccess, msg, guardLoginId);
        //    }
                

        //    // Get Guard Login ID
        //    guardLoginId = GetGuardLoginId(logBookId, guardId, clientsiteId, userId, IPAddress);

        //    if (guardLoginId <= 0)
        //    {
        //        msg = "Guard login failed.";
        //        return (IsSuccess, msg, guardLoginId);
        //    }
            
        //    // Default GPS coordinates (should be replaced with actual values if available)
        //    var gpsCoordinates = gps;

        //    var _scanningType = (ScanningType)scanningType;

        //    // Create a log entry
        //    var signInEntry = new GuardLog
        //    {
        //        ClientSiteLogBookId = logBookId,
        //        GuardLoginId = guardLoginId,
        //        EventDateTime = LogDateTime,
        //        /*your message */
        //        Notes = activityString,
        //        IsSystemEntry = systemEntry,
        //        EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
        //        EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
        //        EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
        //        EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
        //        EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
        //        GpsCoordinates = gpsCoordinates,
        //        WAND_TAG_ENTRY_TYPE = _scanningType
        //    };

        //    _guardLogDataProvider.SaveGuardLog(signInEntry);

        //    //Check if tour mode is enabled for the site then log into corresponding tag attached site also
        //    var _ClientSiteTourMode = _clientDataProvider.GetClientSiteDetailsWithId(clientsiteId).FirstOrDefault();
        //    if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND && !string.Equals(tagUID, "NA"))
        //    {
        //        var TagInfoDetails = _viewDataService.GetSmartWandTagDetailOfTag(tagUID, "nfc");
        //        if (TagInfoDetails != null && TagInfoDetails?.ClientSiteId > 0)
        //        {
        //            var _CorrespondingSitelogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(TagInfoDetails.ClientSiteId, logBookType);
        //            guardLoginId = GetGuardLoginId(_CorrespondingSitelogBookId, guardId, TagInfoDetails.ClientSiteId, userId, IPAddress);


        //            // If tour mode enabled then log the tour activity
        //            GuardLog _CorrespondingSiteLogEntry = signInEntry;
        //            _CorrespondingSiteLogEntry.Id = 0;
        //            _CorrespondingSiteLogEntry.ClientSiteLogBookId = _CorrespondingSitelogBookId;
        //            _CorrespondingSiteLogEntry.GuardLoginId = guardLoginId;
        //            if (clientsiteId != TagInfoDetails.ClientSiteId)
        //            {
        //                _guardLogDataProvider.SaveGuardLog(_CorrespondingSiteLogEntry);
        //            }
        //        }

        //    }

        //    // Notify all SignalR clients in this ClientSiteId group to refresh the tag scan status
        //    if (_scanningType != ScanningType.Normal)
        //    {
        //        _hubContext.Clients.Group(clientsiteId.ToString()).SendAsync("RefreshTagScanStatus");
        //    }

        //    IsSuccess = true;
        //    msg = "Guard successfully logged in.";
        //    return (IsSuccess, msg, guardLoginId);
        //}


        public int GetGuardLoginId(int logBookId, int guardId, int clientsiteId, int userId, string IPAddress)
        {
            // Get all guard logins associated with the logBookId
            var guardLoginList = _guardDataProvider.GetGuardLoginsByLogBookId(logBookId).ToList();

            // Check if a guard login exists for the current day
            var existingGuardLogin = guardLoginList.FirstOrDefault(x => x.GuardId == guardId && x.OnDuty.Date == DateTime.Now.Date);

            if (existingGuardLogin != null)
            {
                return existingGuardLogin.Id; // Return existing login ID
            }

            // Create a new GuardLogin entry
            var newGuardLogin = new GuardLogin
            {
                LoginDate = DateTime.Now,
                GuardId = guardId,
                ClientSiteId = clientsiteId,
                ClientSiteLogBookId = logBookId,
                PositionId = null,
                SmartWandId = null,
                OnDuty = DateTime.Now,
                OffDuty = DateTime.Now.AddHours(1),
                UserId = userId,
                IPAddress = IPAddress
            };

            // Save and return new login ID
            return _guardDataProvider.SaveGuardLogin(newGuardLogin);
        }

    }
}
