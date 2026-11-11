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

        public Task<(bool IsSuccess, bool TagFound, string message, string TagInfoLabel, int ScanFromLinkedSiteId, int RowIdInServer, int TagSiteId, string TagSiteName)> CreateSmartWandScannerHitLogRecord(int siteId, string TagUid, int GuardId,
          int UserId, bool IsOfflineRecord, Guid uniqueRecordID, DateTime HitUtcDateTime, ScanningType scanningType, string gpsCordinates, int? SmartWandId = null);

        public (bool IsSuccess, string msg, int guardLoginId) PostMobileLogActivity(PostActivityRequest request, string IPAddress);
        //public (bool IsSuccess, string msg, int guardLoginId) PostMobileLogActivity(int guardId, int clientsiteId, int userId, string activityString, string gps,
        //    string IPAddress, DateTime LogDateTime, bool systemEntry = true, int scanningType = 0, string tagUID = "NA");

        public int GetGuardLoginId(int logBookId, int guardId, int clientsiteId, int userId, string IPAddress);
    }
    public class MobileAppDataServices : IMobileAppDataServices
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public readonly IClientDataProvider _clientDataProvider;
        private readonly IHubContext<MobileAppSignalRHub> _hubContext;
        private readonly IAlertEmailServices _alertEmailServices;

        /* Tracking feature pack: optional publisher, no-op unless a subscriber activated
           the bus. See CityWatch.Events.NullDomainEventPublisher. */
        private readonly CityWatch.Events.IDomainEventPublisher _events;

        /* P1#375: how long the same tag at the same site is refused after a hit. Raised from
           1 minute to 5. The mobile app holds the same 5 minutes in its own
           ScanDuplicateGuard, which is now the first line of defence - this check backs it up
           for handsets that have not been updated, and for anything else posting to the API. */
        private static readonly TimeSpan TagRescanWindow = TimeSpan.FromMinutes(5);

        /* P1#375 wand strike manipulation flag: how many hits of the same NFC tag, one after
           another by the same guard at the same site, raise the alert. */
        private const int WandStrikeManipulationHitCount = 3;

        /* How far back a run may reach. Keeps a run from spanning shifts or days - the last
           scan of one night and the first two of the next are not "three in a row". */
        private static readonly TimeSpan WandStrikeManipulationLookback = TimeSpan.FromHours(12);

        public MobileAppDataServices(IViewDataService viewDataService, IClientSiteWandDataProvider clientSiteWandDataProvider,
            IClientDataProvider clientDataProvider, IGuardDataProvider guardDataProvider,
            ILogbookDataService logbookDataService, IGuardLogDataProvider guardLogDataProvider, IHubContext<MobileAppSignalRHub> hubContext,
            IAlertEmailServices alertEmailServices,
            CityWatch.Events.IDomainEventPublisher events = null)
        {
            _alertEmailServices = alertEmailServices;
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _logbookDataService = logbookDataService;
            _guardLogDataProvider = guardLogDataProvider;
            _hubContext = hubContext;
            _events = events ?? CityWatch.Events.NullDomainEventPublisher.Instance;
        }

        public async Task<(bool IsSuccess, bool TagFound, string message, string TagInfoLabel, int ScanFromLinkedSiteId, int RowIdInServer, int TagSiteId, string TagSiteName)> CreateSmartWandScannerHitLogRecord(int siteId, string TagUid, int GuardId,
           int UserId, bool IsOfflineRecord, Guid uniqueRecordID, DateTime HitUtcDateTime, ScanningType scanningType, string gpsCordinates, int? SmartWandId = null)
        {
            bool IsSuccess = false;
            string message = "An error occurred.";
            bool TagFound = false;
            string TagInfoLabel = string.Empty;
            int ScanFromLinkedSiteId = siteId;
            string _tagEndDesc = "[NFC]";
            int RowIdInServer = 0;
            int TagSiteId = 0;
            string TagSiteName = string.Empty;

            ClientSiteSmartWandTagsHitLog _clientSiteSmartWandTagsHitLog = new ClientSiteSmartWandTagsHitLog();
            try
            {
                var _smartWandTagsTypes = _clientSiteWandDataProvider.GetSmartWandTagsType();
                var _lastTagScannedRecord = _clientSiteWandDataProvider.GetLastScannedTagDateTime(siteId, TagUid);

                /* Check if the tag was scanned recently (TagRescanWindow) from the same site.
                   Offline records are exempt on purpose: they are synced long after the fact,
                   so comparing their stored hit time against UtcNow would refuse every one of
                   them. The handset applies the window to offline scans before caching them. */
                if (!IsOfflineRecord)
                {
                    if (_lastTagScannedRecord != null && _lastTagScannedRecord.LoggedInClientSiteId == siteId && (DateTime.UtcNow - _lastTagScannedRecord.HitUtcDateTime) < TagRescanWindow)
                    {
                        if (scanningType == ScanningType.NFC) { message = "Tag already scanned !!!"; }
                        else if (scanningType == ScanningType.BLUETOOTH) { message = "iBeacon already scanned !!!"; }

                        return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId, RowIdInServer, TagSiteId, TagSiteName);
                    }
                }
                var _ClientSiteTourMode = _clientDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();

                if (scanningType == ScanningType.NFC)
                {
                    _tagEndDesc = "[NFC]";
                    if(_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
                    {
                        _tagEndDesc = $"[{_ClientSiteTourMode.PatrolTourMode.ToString()} NFC]";
                    }
                }
                else
                {
                    _tagEndDesc = "[BLE]";
                    if (_ClientSiteTourMode != null && _ClientSiteTourMode.PatrolTourMode != PatrolTouringMode.STND)
                    {
                        _tagEndDesc = $"[{_ClientSiteTourMode.PatrolTourMode.ToString()} BLE]";
                    }
                }

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
                _clientSiteSmartWandTagsHitLog.GPScoordinates = gpsCordinates;

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
                        TagInfoLabel = $"{TagUid} {_tagEndDesc}";
                        _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
                    }
                    else if (scanningType == ScanningType.BLUETOOTH)
                    {  // if ibeacon not found dont show in log book entry
                        message = "iBeacon Not Found";
                        return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId, RowIdInServer, TagSiteId, TagSiteName);
                    }
                }
                else
                {
                    _clientSiteSmartWandTagsHitLog.LabelDescription = TagInfoDetails.LabelDescription;
                    _clientSiteSmartWandTagsHitLog.TagLinkedClientSiteId = TagInfoDetails.ClientSiteId;
                    TagSiteId = TagInfoDetails.ClientSiteId;
                    TagSiteName = TagInfoDetails.ClientSiteName ?? string.Empty;
                    if (TagInfoDetails.ClientSiteId != siteId)
                    {
                        if (TagInfoDetails.ClientSiteId == 0)
                        {
                            if (scanningType == ScanningType.NFC)
                            {
                                IsSuccess = true;
                                message = "Tag Not Found";
                                TagInfoLabel = $"{TagUid} {_tagEndDesc}";
                                _clientSiteSmartWandTagsHitLog.LabelDescription = TagUid;
                            }
                            else if (scanningType == ScanningType.BLUETOOTH)
                            {  // if ibeacon
                                message = "iBeacon Not Found";
                                return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId, RowIdInServer, TagSiteId, TagSiteName);
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
                                var _check = getallRCLinkedDuressMaster.Where(x => x.Id == _rcLinkedClientSites?.FirstOrDefault()?.RCLinkedId)?.FirstOrDefault();
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
                                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} {_tagEndDesc}";
                                        }
                                        else if (scanningType == ScanningType.BLUETOOTH)
                                        {  // if ibeacon
                                            IsSuccess = true;
                                            TagFound = true;
                                            message = "iBeacon Found";
                                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} {_tagEndDesc}";
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
                                /* P4#153: tour-mode guard on another site's tag — the owning site
                                   leads the label because the app's toast truncates at 35 chars;
                                   a trailing site name would never reach the guard's screen. The
                                   same string becomes the logbook entry text (online via the app,
                                   offline via SyncOfflineSmartWandTagHitData). */
                                var _tagSitePrefix = string.IsNullOrWhiteSpace(TagInfoDetails.ClientSiteName) ? string.Empty : $"[{TagInfoDetails.ClientSiteName}] ";
                                if (scanningType == ScanningType.NFC)
                                {
                                    IsSuccess = true;
                                    TagFound = true;
                                    message = "Tag Found";
                                    TagInfoLabel = $"{_tagSitePrefix}{TagInfoDetails.LabelDescription} {_tagEndDesc}";
                                }
                                else if (scanningType == ScanningType.BLUETOOTH)
                                {  // if ibeacon
                                    IsSuccess = true;
                                    TagFound = true;
                                    message = "iBeacon Found";
                                    TagInfoLabel = $"{_tagSitePrefix}{TagInfoDetails.LabelDescription} {_tagEndDesc}";
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
                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} {_tagEndDesc}";
                        }
                        else if (scanningType == ScanningType.BLUETOOTH)
                        {  // if ibeacon
                            IsSuccess = true;
                            TagFound = true;
                            message = "iBeacon Found";
                            TagInfoLabel = $"{TagInfoDetails.LabelDescription} {_tagEndDesc}";
                        }
                    }
                }
                try
                {
                    /*
                     * Isolated Feature: FQ milestone message (fires when the per-site "X PD" target is met)
                     * -----------------------------------------------------------------------------------
                     * Logic Summary:
                     * 1. This block only decides ELIGIBILITY and reads the site's per-day patrol target.
                     * 2. The site must NOT be a control room logbook, and it must have a "Per Day" (PD) target > 0.
                     * 3. The trigger threshold is the site's configured "X PD" (NoOfPatrols) - which is UNIQUE per site
                     *    (may be 3, 4, 5, ...), NOT a fixed number. The actual FQ count is read AFTER the scan is saved.
                     */
                    bool isEligibleForFqMessage = false;
                    int fqTargetPerDay = 0;
                    try
                    {
                        var controlSites = _clientDataProvider.GetClientSiteForRcLogBook();
                        if (controlSites != null && !controlSites.Any(x => x.Id == siteId))
                        {
                            // The site's own per-day target (X in "X PD"); 0 when not Per Day / not configured.
                            fqTargetPerDay = _guardLogDataProvider.GetClientSitePatrolTargetPerDay(siteId);
                            if (fqTargetPerDay > 0)
                            {
                                isEligibleForFqMessage = true;
                            }
                        }
                    }
                    catch { }

                    // Log the tag details
                    _clientSiteWandDataProvider.SaveSmartWandTagLog(_clientSiteSmartWandTagsHitLog);
                    RowIdInServer = _clientSiteSmartWandTagsHitLog.Id;

                    /* P1#375: flag the same NFC checkpoint being struck over and over. Runs
                       after the hit is committed and never fails the scan - a guard must not
                       be blocked by a mail server. NFC only: see CheckForWandStrikeManipulation. */
                    if (scanningType == ScanningType.NFC)
                    {
                        await CheckForWandStrikeManipulation(siteId, GuardId, tagtypeid,
                            _clientSiteSmartWandTagsHitLog, IsOfflineRecord);
                    }

                    /* Tracking feature pack: the scan is committed above; publish is fire-and-
                       forget and cannot fail this workflow (D17). LoggedInClientSiteId lets
                       tracking tell an in-car tag (belongs to the officer's own fleet site)
                       from a client-site checkpoint. */
                    var _trkTagSiteId = _clientSiteSmartWandTagsHitLog.TagLinkedClientSiteId.GetValueOrDefault();
                    if (_trkTagSiteId <= 0) _trkTagSiteId = ScanFromLinkedSiteId > 0 ? ScanFromLinkedSiteId : siteId;
                    _events.Publish(new CityWatch.Events.Events.NfcCheckpointScanned(
                        SmartWandId ?? 0, TagUid, _trkTagSiteId,
                        GuardId, UserId, gpsCordinates, HitUtcDateTime, (int)scanningType, IsOfflineRecord)
                    {
                        LoggedInClientSiteId = siteId,
                        LabelDescription = _clientSiteSmartWandTagsHitLog.LabelDescription,
                        TagSiteName = TagInfoDetails?.ClientSiteName
                    });
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

                    /*
                     * Isolated Feature: Check if FQ has reached the site's per-day target (guaranteed-once delivery)
                     * ----------------------------------------------------------------------------------------------
                     * Logic Summary:
                     * 1. After the wand scan is saved, we read the current FQ (completed rounds) once.
                     * 2. The milestone fires when FQ reaches the site's configured target (fqTargetPerDay, the "X" in
                     *    "X PD"). Using ">=" guarantees the message is still sent if the FQ minimum jumps past the target.
                     * 3. Idempotency is enforced by the DATABASE, not by catching the exact transition: before inserting
                     *    we re-read today's DailyGuardLog and skip if a milestone entry (identified by FqMilestoneMarker)
                     *    already exists. This makes "send exactly once per day" deterministic even with concurrent scans.
                     * 4. The message is inserted into GuardLog as a System Entry (IsSystemEntry = true, GuardLoginId = null).
                     * 5. IrEntryType.Notification renders the message with a yellow background in the UI,
                     *    identically to how Control Room Push Notifications are formatted.
                     */
                    try
                    {
                        if (isEligibleForFqMessage)
                        {
                            int afterRounds = _guardLogDataProvider.GetCompletedPatrolRounds(siteId);
                            if (afterRounds >= fqTargetPerDay)
                            {
                                // Distinctive substring present in the milestone message; used for the once-per-day dedup check.
                                const string FqMilestoneMarker = "based on the FQ counter";

                                var logBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(siteId, LogBookType.DailyGuardLog, DateTime.Now.Date);
                                if (logBookId > 0)
                                {
                                    // Only insert if today's logbook does not already contain the FQ milestone entry.
                                    var todaysLogs = _guardLogDataProvider.GetGuardLogs(logBookId, DateTime.Now.Date);
                                    bool alreadySent = todaysLogs != null && todaysLogs.Any(g => g.IsSystemEntry && g.Notes != null && g.Notes.Contains(FqMilestoneMarker));

                                    if (!alreadySent)
                                    {
                                        string milestoneMsg = "C4i System Supervisor:\n\"This team has worked well together to achieve the minimum daily patrol requirements based on the FQ counter. Well done, and thank you for ensuring SOPs are being followed and site risks are being mitigated through your security patrols and OH&S observation checks.\n\nPlease continue to patrol in line with SOPs and client expectations, as this achievement represents a minimum milestone—not a reason to reduce patrol activity. The quality of patrols is equally important, so please remember to slow down between points and actively check surrounding areas.\n\nKeep up the good work, and remember: counters reset at midnight.\"";

                                        var fqGuardLog = new GuardLog
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            EventDateTime = DateTime.Now,
                                            Notes = milestoneMsg,
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                                            EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                                            EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                                            EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                                            EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                                            PlayNotificationSound = true
                                        };
                                        var fqLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(fqGuardLog);

                                        // The FQ log is a system entry with no GuardLoginId, so the GuardLogs trigger
                                        // can't create its Fusion row. Insert directly into the Fusion history table so it
                                        // shows in Fusion as a system entry (no guard initials) and yellow, without
                                        // touching the live Radio Check dashboard. The logbook entry stays a system entry.
                                        if (fqLogId > 0)
                                        {
                                            fqGuardLog.Id = fqLogId;
                                            _guardLogDataProvider.AddFqMilestoneFusionActivity(siteId, fqGuardLog);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
                catch (Exception exp)
                {

                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return (IsSuccess, TagFound, message, TagInfoLabel, ScanFromLinkedSiteId, RowIdInServer, TagSiteId, TagSiteName);
        }

        /// <summary>
        /// P1#375: raise a flag when one guard hits the same NFC checkpoint
        /// <see cref="WandStrikeManipulationHitCount"/> times in a row at one site, with no
        /// other checkpoint in between - the pattern you get when a tag has been taken off its
        /// post and is being struck by hand instead of walked to.
        ///
        /// NFC only, by design. BLE scans continuously, so its hits would land between every
        /// pair of NFC hits and no run would ever be visible; a beacon merely in range also
        /// says nothing about where the guard walked.
        ///
        /// Fires once per run, at the hit that reaches the count: the run is read as exactly
        /// <see cref="WandStrikeManipulationHitCount"/> long when the hit before it is a
        /// different tag (or there is none in the lookback). A 4th and 5th strike are already
        /// covered by the mail the 3rd sent, so they stay quiet; scanning a different tag ends
        /// the run, and a fresh run of three alerts again.
        ///
        /// Never throws into the scan path. The strike is already committed when this runs, and
        /// a guard on patrol must not be held up - or turned away - by a mail server.
        /// </summary>
        private async Task CheckForWandStrikeManipulation(int siteId, int guardId, int? tagsTypeId,
            ClientSiteSmartWandTagsHitLog currentHit, bool isOfflineRecord)
        {
            try
            {
                if (_alertEmailServices == null || !tagsTypeId.HasValue || currentHit == null)
                    return;

                if (string.IsNullOrWhiteSpace(currentHit.TagUId))
                    return;

                /* One more than the trigger count: the extra row is what tells a run that has
                   just reached three from one that was already longer and has alerted. */
                var recentHits = _clientSiteWandDataProvider.GetRecentTagHitsForGuardAtSite(
                    siteId, guardId, tagsTypeId.Value,
                    WandStrikeManipulationHitCount + 1,
                    DateTime.UtcNow - WandStrikeManipulationLookback);

                if (recentHits == null || recentHits.Count < WandStrikeManipulationHitCount)
                    return;

                var run = recentHits.Take(WandStrikeManipulationHitCount).ToList();
                if (!run.All(x => string.Equals(x.TagUId, currentHit.TagUId, StringComparison.OrdinalIgnoreCase)))
                    return;

                // Already longer than the trigger count - the mail went out on the third strike.
                if (recentHits.Count > WandStrikeManipulationHitCount &&
                    string.Equals(recentHits[WandStrikeManipulationHitCount].TagUId, currentHit.TagUId, StringComparison.OrdinalIgnoreCase))
                    return;

                /* A site with a single NFC checkpoint hits the same tag on every patrol, so a
                   run of three is just the site working normally - flagging it would email the
                   IR desk forever and teach them to ignore the alert. Needs at least two tags
                   for "no other checkpoint in between" to mean anything. */
                if (_clientSiteWandDataProvider.GetRegisteredTagCountForSite(siteId, tagsTypeId.Value) < 2)
                    return;

                var site = _clientDataProvider.GetClientSiteDetailsWithId(siteId).FirstOrDefault();
                var guard = _guardDataProvider.GetGuardDetailsUsingId(guardId).FirstOrDefault();

                /* Sites span AU timezones, so the server's clock is not the site's. Derive the
                   site's local time the same way the Wand Strikes report does: the site's
                   configured UTC offset, falling back to +10:00 when it has none. */
                var siteUtcOffset = "+10:00";
                try
                {
                    var kpiSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
                    if (kpiSetting != null && !string.IsNullOrWhiteSpace(kpiSetting.UTC))
                        siteUtcOffset = kpiSetting.UTC;
                }
                catch { }

                if (!TimeSpan.TryParse(siteUtcOffset.Replace("+", ""), out var offset))
                    offset = TimeSpan.Zero;

                var alert = new WandStrikeManipulationAlert
                {
                    ClientSiteId = siteId,
                    ClientSiteName = site?.Name ?? $"Site {siteId}",
                    GuardId = guardId,
                    GuardName = guard?.Name ?? $"Guard {guardId}",
                    TagUId = currentHit.TagUId,
                    TagLabelDescription = currentHit.LabelDescription,
                    ConsecutiveHitCount = WandStrikeManipulationHitCount,
                    HitUtcDateTime = currentHit.HitUtcDateTime,
                    HitSiteLocalDateTime = currentHit.HitUtcDateTime + offset,
                    SiteUtcOffset = siteUtcOffset,
                    // Oldest first, so the email reads in the order the strikes happened.
                    RunSiteLocalDateTimes = run.OrderBy(x => x.HitUtcDateTime)
                                               .Select(x => x.HitUtcDateTime + offset)
                                               .ToArray(),
                    IsOfflineRecord = isOfflineRecord
                };

                await _alertEmailServices.SendWandStrikeManipulationAlertMail(alert);
            }
            catch (Exception ex)
            {
                // Swallowed on purpose: a failed flag must never cost the guard their scan.
                Console.WriteLine($"Wand strike manipulation check failed for site {siteId}, guard {guardId}: {ex.Message}");
            }
        }

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
                OfflineRecordSyncDateTime = request.OfflineRecordSyncDateTime,
                TagScanHitLogRefId = request.TagScanHitLogRefId,
                EventMobileUtcDateTime = request.EventMobileUtcDateTime,
                IsEntryByPCAR = request.IsEntryByPCAR,
                PositionId = request.PositionId,
                CallSignId = request.CallSignId,
                EntryPassedByPCARclientsiteId = request.IsEntryByPCAR ? request.clientsiteId : null,
            };

            var newPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(signInEntry);

            if(request.IsEntryByPCAR && _scanningType == ScanningType.Normal && request.clientsiteId != request.LogbookclientsiteId)
            {
                // Make entry in corresponding non PCAR site logbook
                GuardLog _NonPcarSiteLogEntry = signInEntry;

                _NonPcarSiteLogEntry.Id = 0;
                _NonPcarSiteLogEntry.EntryPassedByPCARclientsiteId = request.clientsiteId;
                _NonPcarSiteLogEntry.ClientSiteLogBookId = 0;
                _NonPcarSiteLogEntry.GuardLoginId = null;

                var _NonPcarSitelogBookId = _logbookDataService.GetNewOrExistingClientSiteLogBookId(request.LogbookclientsiteId.Value, logBookType);
                guardLoginId = GetGuardLoginId(_NonPcarSitelogBookId, request.guardId, request.LogbookclientsiteId.Value, request.userId, IPAddress);

                _NonPcarSiteLogEntry.ClientSiteLogBookId = _NonPcarSitelogBookId;
                _NonPcarSiteLogEntry.GuardLoginId = guardLoginId;

                if (_NonPcarSitelogBookId > 0 && guardLoginId > 0)
                {
                  var newNonPcarGuardLogId = _guardLogDataProvider.SaveGuardLogAndReturnId(_NonPcarSiteLogEntry);
                    // Link the GuardLog entries
                    var linkid = _guardLogDataProvider.LinkGuardLogIds(newNonPcarGuardLogId, newPcarGuardLogId);
                }
            }

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
                    _CorrespondingSiteLogEntry.TagScanHitLogRefId = null;
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
