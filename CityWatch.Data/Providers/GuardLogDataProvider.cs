using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Services;
using Dropbox.Api.Files;
using Dropbox.Api.Users;
using iText.Commons.Actions.Contexts;
using iText.Kernel.Crypto.Securityhandler;
using iText.Layout.Element;
using iText.StyledXmlParser.Css.Resolve.Shorthand.Impl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static Dropbox.Api.Files.WriteMode;
using static Dropbox.Api.Team.GroupSelector;
using static Dropbox.Api.TeamLog.EventCategory;
using static Dropbox.Api.TeamLog.TimeUnit;

namespace CityWatch.Data.Providers
{
    public interface IGuardLogDataProvider
    {
        List<GuardLog> GetGuardLogs(int logBookId, DateTime logDate);
        List<GuardLog> GetGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);
        GuardLog GetLatestGuardLog(int clientSiteId, int guardId);
        void SaveGuardLog(GuardLog guardLog);
        void DeleteGuardLog(int id);
        //logBookId delete for radio checklist-start
        void DeleteClientSiteRadioCheckActivityStatusForLogBookEntry(int id);
        void DeleteClientSiteRadioCheckActivityStatusForKeyVehicleEntry(int id);
        void SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(int GuardId, int ClientSiteId);

        //logBookId delete for radio checklist-end
        List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRego(string vehicleRego);
        List<KeyVehicleLog> GetKeyVehicleLogs(int logBookId);
        List<KeyVehicleLog> GetKeyVehicleLogs(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate);
        List<KeyVehicleLog> GetKeyVehicleLogsWithPOI(int[] clientSiteIds, int[] personOfInterestIds, DateTime logFromDate, DateTime logToDate);
        KeyVehicleLog GetKeyVehicleLogById(int id);
        KeyVehcileLogField GetIndividualType(int PersonType);
        List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids);
        List<KeyVehicleLog> GetPOIAlert(string companyname, string individualname, int individualtype);
        void SaveDocketSerialNo(int id, string serialNo);
        void SaveKeyVehicleLog(KeyVehicleLog keyVehicleLog);
        void DeleteKeyVehicleLog(int id);
        void KeyVehicleLogQuickExit(int id, DateTime? ExitTimeLocal);
        List<PatrolCarLog> GetPatrolCarLogs(int logBookId);
        List<PatrolCarLog> GetPatrolCarLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate);
        void SavePatrolCarLog(PatrolCarLog patrolCarLog);
        void SavePatrolCarLogs(IEnumerable<PatrolCarLog> patrolCarLogs);
        List<ClientSiteCustomField> GetClientSiteCustomFields();
        List<ClientSiteCustomField> GetCustomFieldsByClientSiteId(int clientSiteId);
        int SaveClientSiteCustomFields(ClientSiteCustomField clientSiteCustomField);
        void DeleteClientSiteCustomFields(int id);
        List<CustomFieldLog> GetCustomFieldLogs(int logBookId);
        List<CustomFieldLog> GetCustomFieldLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate);
        void SaveCustomFieldLogs(List<CustomFieldLog> customFieldLogs);
        void SaveCustomFieldLog(CustomFieldLog customFieldLog);
        List<string> GetVehicleRegos(string regoStart = null);
        List<string> GetVehicleRegosForKVL(string regoStart = null);
        List<string> GetClientSiteSearch(string clientSiteNew = null);
        List<string> GetCompanyNames(string companyNameStart);
        List<string> GetSenderNames(string senderNameStart);
        KeyVehicleLogProfile GetKeyVehicleLogVisitorProfile(string truckRego);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsWithIndividualType(int individualtype);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego, string personName);
        KeyVehicleLogVisitorPersonalDetail GetKeyVehicleLogProfileWithPersonalDetails(int id);
        int SaveKeyVehicleLogProfileWithPersonalDetail(KeyVehicleLogVisitorPersonalDetail keyVehicleLogProfile);
        int SaveKeyVehicleLogVisitorPersonalDetail(KeyVehicleLogVisitorPersonalDetail keyVehicleLogVisitorPersonalDetail);
        void SaveKeyVehicleLogProfileNotes(string truckRego, string notes);
        void DeleteKeyVehicleLogPersonalDetails(int id);
        List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false);
        List<KeyVehcileLogField> GetKeyVehicleLogFieldsByType(KvlFieldType type);
        void SaveKeyVehicleLogField(KeyVehcileLogField field);
        void DeleteKeyVehicleLogField(int id);
        List<KeyVehicleLogAuditHistory> GetAuditHistory(int id);
        void SaveKeyVehicleLogAuditHistory(KeyVehicleLogAuditHistory keyVehicleLogAuditHistory);
        void SaveClientSiteDuress(int clientSiteId, int guardId, string gpsCoordinates, string enabledAddress, GuardLog tmzdata, int linkedDuressParentSiteId, int isLinkedDuressParentSite);
        ClientSiteDuress GetClientSiteDuress(int clientSiteId);
        List<CompanyDetails> GetCompanyDetails();
        //logBookId entry for radio checklist-start
        void SaveRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        List<ClientSiteRadioChecksActivityStatus> GetClientSiteRadioChecksActivityDetails();
        void DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivityStatus ClientSiteRadioChecksActivityStatus);
        List<RadioCheckListGuardData> GetActiveGuardDetails();
        List<RadioCheckListInActiveGuardData> GetInActiveGuardDetails();
        public Guard GetGuards(int guardId);
        //logBookId entry for radio checklist-end
        public KeyVehicleLog GetCompanyDetailsVehLog(string companyName);

        //for getting logBook details of the  guard-start
        List<RadioCheckListGuardLoginData> GetActiveGuardlogBookDetails(int clientSiteId, int guardId);
        //for getting logBook details of the  guard-end

        //for getting logBook history of the  guard-start
        List<GuardLog> GetActiveGuardlogBookHistory(int clientSiteId, int guardId);
        //for getting logBook history of the  guard-end

        //for getting incident report history of the  guard-start
        List<IncidentReport> GetActiveGuardIncidentReportHistory(int clientSiteId, int guardId);
        //for getting incident report history of the  guard-end

        //for getting Key Vehicle history of the  guard-start
        List<KeyVehicleLog> GetActiveGuardKeyVehicleHistory(int clientSiteId, int guardId);
        //for getting Key Vehicle history of the  guard-end

        //for getting smartwand history of the  guard-start
        List<SmartWandScanGuardHistory> GetActiveGuardSwHistory(int clientSiteId, int guardId);
        //for getting smartwand history of the  guard-end


        //for getting list of guards not available-start
        List<RadioCheckListNotAvailableGuardData> GetNotAvailableGuardDetails();
        //for getting list of guards not available-end
        //for getting key vehicle log details of the  guard-start

        List<RadioCheckListGuardKeyVehicleData> GetActiveGuardKeyVehicleLogDetails(int clientSiteId, int guardId);
        //for getting  key vehicle log details of the  guard-end

        //for getting incident report details of the  guard-start

        List<RadioCheckListGuardIncidentReportData> GetActiveGuardIncidentReportDetails(int clientSiteId, int guardId);
        //for getting  incident report details of the  guard-end
        void SaveRadioCheckDuress(string UserID);
        public bool IsRadiocheckDuressEnabled(int UserID);
        public int UserIDDuress(int UserID);

        //rc status save Start
        void SaveClientSiteRadioCheck(ClientSiteRadioCheck clientSiteRadioCheck);
        //rc status save end

        int GetClientSiteLogBookId(int clientsiteId, LogBookType type, DateTime date);
        int GetGuardLoginId(int clientsitelogbookId, int guardId, DateTime date);
        List<GuardLog> GetGuardLogsId(int logBookId, DateTime logDate, int guardLoginId, IrEntryType type, string notes);
        void UpdateRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        List<GuardLogin> GetGuardLogins(int guardLoginId);

        /* new Change by dileep for p4 task 17 start*/
        void UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        void GetGuardManningDetails(DayOfWeek CurrentDay);
        void RemoveTheeRadioChecksActivityWithNotifcationtypeOne(int ClientSiteId);
        public void RemoveClientSiteRadioChecksGreaterthanTwoHours();
        public void SaveClientSiteRadioCheckStatusFromlogBook(ClientSiteRadioCheck clientSiteRadioCheck);
        public bool getIfAnyActivityInbufferTime(int GuardId, int ClientSiteId);
        /* new Change by dileep for p4 task 17 end*/


        //p4#48 AudioNotification - Binoy - 12-01-2024
        public void UpdateDuressAlarmPlayedStatus();


        //listing clientsites for radio check
        List<ClientSite> GetClientSites(int? Id);
        List<ClientSiteSmartWand> GetClientSiteSmartWands(int? clientSiteId);
        int GetGuardLoginId(int guardId, DateTime date);
        List<GuardLogin> GetGuardLoginsByClientSiteId(int? clientsiteId, DateTime date);

        // for global push message- start
        List<ClientType> GetUserClientTypesHavingAccess(int? userId);
        List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm);
        List<ClientSite> GetUserClientSitesHavingAccessRadio(int? typeId, int? userId, string searchTerm);
        List<State> GetStates();
        List<ClientSite> GetClientSitesForState(string State);
        int GetClientSiteLogBookIdGloablmessage(int clientsiteId, LogBookType type, DateTime date);
        List<ClientSite> GetAllClientSites();
        List<SelectListItem> GetUserClientSitesWithId(string types);
        // for global push message- end


        //for saving status for active guards-start
        void SaveClientSiteRadioCheckNew(ClientSiteRadioCheck clientSiteRadioCheck, GuardLog tmzdata, int controlroomGuardLoginId);
        //for saving status for active guards-end

        void EditRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity);
        List<RadioCheckListGuardLoginData> GetClientSiteRadiocheckStatus(int clientSiteId, int guardId);

        void RemoveGuardLoginFromdifferentSites();

        List<KeyVehicleLog> GetKeyVehicleLogs(string truckno);


        void InsertPreviousLogBook(KeyVehicleLog keyVehicleLog);

        List<GuardLog> GetGuardLogswithKvLogData(int logBookId, DateTime logDate);

        void LogBookEntryForRcControlRoomMessages(int loginGuardId, int selectedGuardId, string subject, string notifications,
                                                    IrEntryType entryType, int type, int clientSiteId, GuardLog tmzdata);
        void LogBookEntryFromRcControlRoomMessages(int loginGuardId, int selectedGuardId, string subject, string notifications,
                                                    IrEntryType entryType, int type, int clientSiteId, GuardLog tmzdata);
        //do's and donts-start
        void SaveDosandDontsField(DosAndDontsField dosanddontsField);
        void DeleteDosandDontsField(int id);
        List<DosAndDontsField> GetDosandDontsFields(int type);
        void SaveActionList(ActionListNotification ActionList);
        RCActionList GetActionlist(int Cliensiteid);
        string GetUserClientSites(string searchTerm);
        int GetUserClientSitesRCList(string searchTerm);
        //do's and donts-end

        void DeleteClientSiteRadioCheckActivityStatusForKV(int id);

        /* Save push messages*/
        int SavePushMessage(RadioCheckPushMessages radioCheckPushMessages);


        void UpdateIsAcknowledged(int rcPushMessageId);

        void CopyPreviousDaysPushMessageToLogBook(List<RadioCheckPushMessages> previousDayPushmessageList, int logBookId, int guardLoginId, GuardLog tmzdata);

        List<KeyVehicleLogProfile> GetKeyVehicleLogVisitorProfile();
        List<KeyVehicleLog> GetKeyVehicleLogsByID(int Id);


        // Project 4 , Task 48, Audio notification, Added By Binoy
        void UpdateNotificationSoundPlayedStatusForGuardLogs(int logBookId, bool isControlRoomLogBook);

        List<int> GetGuardLogsNotAcknowledgedForNotificationSound();

        void CopyPreviousDaysDuressToLogBook(List<RadioCheckPushMessages> previousDayDuressList, int logBookId, int guardLoginId, GuardLog tmzdata);


        // p6#73 timezone bug - Added by binoy 24-01-2024
        int GetClientSiteLogBookIdByLogBookMaxID(int clientsiteId, LogBookType type, out DateTime logbookDate);

        List<RadioCheckListSWReadData> GetActiveGuardSWDetails(int clientSiteId, int guardId);
        List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsWithPersonName(string personName);
        List<KeyVehicleLog> GetKeyVehicleLogsWithKeyNo(string KeyNo);
        List<KeyVehicleLogAuditHistory> GetAuditHistoryWithKeyVehicleLogId(int id);
        int GetClientTypeCount(int? typeId);

        List<KeyVehicleLogVisitorPersonalDetail> GetPOIListFromVisitorPersonalDetails();
        RadioCheckLogbookSiteDetails GetRadiocheckLogbookDetails();

        List<GuardLogin> GetLastLoginNew(int GuradId);

        //p1-191 hr files task 3-start
        void SaveHRSettings(HrSettings hrSettings, int[] selectSites, string[] selectedStates);
        void DeleteHRSettings(int id);
        void SaveLicensesTypes(LicenseTypes licenseTypes);
        void DeleteLicensesTypes(int id);
        //p1-191 hr files task 3-end

        //P4-79 MENU CORRECTIONS START
        List<GuardLogin> GetGuardLogs(int clientSiteId);
        //P4-79 MENU CORRECTIONS END

        List<string> GetTrailerRegosForKVL(string regoStart = null);

        List<TrailerDeatilsViewModel> GetKeyVehicleLogProfileDetails(string pattern);
        public KeyVehicleLogProfile GetKeyVehicleLogVisitorProfileUsingTrailerRigo(string TrailerRigo1, string TrailerRigo2,
            string TrailerRigo3, string TrailerRigo4, int? TrailerRigo1Id, int? TrailerRigo2Id, int? TrailerRigo3Id, int? TrailerRigo4Id);
        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsUsingTrailerRego(
          string trailerRego1, string trailerRego2, string trailerRego3, string trailerRego4, int? trailerRego1Id, int? trailerRego2Id,
          int? trailerRego3Id, int? trailerRego4Id
          );

        public void SaveKeyVehicleLogProfileNotesByTrailerRiog(string Trailer1Rego, string Trailer2Rego, string Trailer3Rego,
            string Trailer4Rego, int? Trailer1PlateId, int? Trailer2PlateId, int? Trailer3PlateId, int? Trailer4PlateId, string notes);
        public int SaveKeyVehicleLogProfileWithPersonalDetailForTrailer(KeyVehicleLogVisitorPersonalDetail kvlVisitorPersonalDetail);


        public List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRegoForTrailer(string trailer1Rego, string trailer2Rego, string trailer3Rego, string trailer4Rego);


        ClientSitePoc GetEmailPOC(int id);
        ClientSitePoc GetClientSitePOCName(int id);
        int GetClientTypeByClientSiteId(int ClientSiteId);
        public void SaveClientSiteRadioCheckStatusFromlogBookNewUpdate(ClientSiteRadioCheck clientSiteRadioCheck);
        public Guard GetGuardsWtihProviderNumber(int guardId);

        public List<RCLinkedDuressClientSites> checkIfASiteisLinkedDuress(int siteId);

        public List<RCLinkedDuressClientSites> getallClientSitesLinkedDuress(int siteId);

        bool IsRClogbookStampRequired(string StampName);


        public List<ClientSiteRadioChecksActivityStatus_History> GetGuardFusionLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs);

        List<FileDownloadAuditLogs> GetFileDownloadAuditLogsData(DateTime logFromDate, DateTime logToDate);

        void CreateDownloadFileAuditLogEntry(FileDownloadAuditLogs fdal);
        void SaveGuardLogDocumentImages(GuardLogsDocumentImages guardLogDocumentImages);


        List<GuardLogsDocumentImages> GetGuardLogDocumentImaes(int LogId);

        void GetGuardManningDetailsForPublicHolidays();

        List<GuardLogsDocumentImages> GetGuardLogDocumentImaesById(int Id);
        void DeleteGuardLogDocumentImaes(int id);

        public void SaveLprWebhookResponse(LprWebhookResponse lprWebhookResponse);
    }

    public class GuardLogDataProvider : IGuardLogDataProvider
    {
        private readonly CityWatchDbContext _context;
        private readonly ILogbookDataService _logbookDataService;

        public GuardLogDataProvider(CityWatchDbContext context,
            ILogbookDataService logbookDataService)
        {
            _context = context;
            _logbookDataService = logbookDataService;
        }

        public List<GuardLog> GetGuardLogs(int logBookId, DateTime logDate)
        {
            return _context.GuardLogs
                .Where(z => z.ClientSiteLogBookId == logBookId && z.EventDateTime >= logDate && z.EventDateTime < logDate.AddDays(1))
                .Include(z => z.ClientSiteLogBook)
                .Include(z => z.GuardLogin.Guard)
                .OrderBy(z => z.Id)
                .ThenBy(z => z.EventDateTime)
                .ToList();



        }


        public List<GuardLog> GetGuardLogswithKvLogData(int logBookId, DateTime logDate)
        {
            var result = new List<GuardLog>();
            if (logBookId != 0)
            {
                var clientSiteId = _context.ClientSiteLogBooks.Where(x => x.Id == logBookId).FirstOrDefault().ClientSiteId;
                if (clientSiteId != null)
                {
                    //var clientSiteLogBook = _context.ClientSiteLogBooks.Where(x => x.ClientSiteId == clientSiteId && x.Date == DateTime.Now.Date).Select(x => x.Id).ToList();
                    var clientSiteLogBook = _context.ClientSiteLogBooks.Where(x => x.ClientSiteId == clientSiteId && x.Date == logDate.Date).Select(x => x.Id).ToList();
                    if (clientSiteLogBook.Count != 0)
                    {
                        //result = _context.GuardLogs
                        //   .Where(z => clientSiteLogBook.Contains(z.ClientSiteLogBookId) && (z.EventDateTime >= logDate && z.EventDateTime < logDate.AddDays(1)))
                        //   .Include(z => z.ClientSiteLogBook)
                        //   .Include(z => z.GuardLogin.Guard)
                        //   .OrderBy(z => z.Id)
                        //   .ThenBy(z => z.EventDateTime)
                        //   .ToList();

                        // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- modified by Binoy - 02-02-2024
                        result = _context.GuardLogs
                          .Where(z => clientSiteLogBook.Contains(z.ClientSiteLogBookId))
                          .Include(z => z.ClientSiteLogBook)
                          .Include(z => z.GuardLogin.Guard)
                          .OrderBy(z => z.Id)
                          .ThenBy(z => z.EventDateTime)
                          .ToList();

                    }
                }
                else
                {
                    return result;
                }

            }
            else
            {
                //result = _context.GuardLogs
                //  .Where(z => z.ClientSiteLogBookId == logBookId && (z.EventDateTime >= logDate && z.EventDateTime < logDate.AddDays(1)))
                //  .Include(z => z.ClientSiteLogBook)
                //  .Include(z => z.GuardLogin.Guard)
                //  .OrderBy(z => z.Id)
                //  .ThenBy(z => z.EventDateTime)
                //  .ToList();
            }

            return result;
        }


        // Project 4 , Task 48, Audio notification, By Binoy -- Start
        public void UpdateNotificationSoundPlayedStatusForGuardLogs(int logBookId, bool isControlRoomLogBook)
        {
            if (isControlRoomLogBook)
            {
                var ControlRoomLog = _context.RadioCheckPushMessages.Where(x => x.Id == logBookId).SingleOrDefault();
                if (ControlRoomLog != null)
                {
                    ControlRoomLog.PlayNotificationSound = false;
                    _context.SaveChanges();
                }
                return;
            }
            else
            {
                var GuardLogRecord = _context.GuardLogs.Where(x => x.Id == logBookId).SingleOrDefault();
                if (GuardLogRecord != null)
                {
                    GuardLogRecord.PlayNotificationSound = false;
                    _context.SaveChanges();

                }
                return;
            }
        }

        public List<int> GetGuardLogsNotAcknowledgedForNotificationSound()
        {
            //List<int?> returnId = null;
            var TonotifySoundList = _context.RadioCheckPushMessages.Where(x => x.IsAcknowledged == 1 && x.PlayNotificationSound == true).Select(x => x.Id).ToList();
            //var returnId = TonotifySoundList.Select(x => x.Id).ToList();
            var returnId = TonotifySoundList;
            //foreach (var t in TonotifySoundList)
            //{
            //    returnId.Add(t.Id);
            //}            
            return returnId;
        }
        // Project 4 , Task 48, Audio notification, By Binoy -- End


        public List<GuardLog> GetGuardLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {



            //return _context.GuardLogs
            //    .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Type == LogBookType.DailyGuardLog
            //            && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate &&
            //            (!excludeSystemLogs || (excludeSystemLogs && (!z.IsSystemEntry || z.IrEntryType.HasValue))))
            //    .Include(z => z.GuardLogin.Guard)
            //    .OrderBy(z => z.EventDateTimeLocal.HasValue? z.EventDateTimeLocal : z.EventDateTime) // p6#73 timezone bug - Modified by binoy 29-01-2024
            //    .ThenBy(z => z.Id)
            //    //.OrderBy(z => z.Id)
            //    //.ThenBy(z => z.EventDateTime)
            //    .ToList();

            var data = _context.GuardLogs
               .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Type == LogBookType.DailyGuardLog
                       && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate &&
                       (!excludeSystemLogs || (excludeSystemLogs && (!z.IsSystemEntry || z.IrEntryType.HasValue))))
               .Include(z => z.GuardLogin.Guard)
               .ToList();

            var returnData = data.OrderBy(z => z.EventDateTimeLocal.HasValue ? z.EventDateTimeLocal : z.EventDateTime)
                .ThenBy(z => z.Id)
                .ToList();

            return returnData;
        }

        public GuardLog GetLatestGuardLog(int clientSiteId, int guardId)
        {
            var latestGuardLogin = _context.GuardLogins
                                    .Where(z => z.ClientSiteId == clientSiteId && z.GuardId == guardId)
                                    .OrderByDescending(x => x.Id)
                                    .FirstOrDefault();

            if (latestGuardLogin != null)
            {
                return _context.GuardLogs.Where(z => z.GuardLoginId == latestGuardLogin.Id)
                                            .OrderBy(z => z.Id)
                                            .ThenBy(z => z.EventDateTime)
                                            .LastOrDefault();
            }

            return null;
        }

        public ClientSiteDuress GetClientSiteDuress(int clientSiteId)
        {
            return _context.ClientSiteDuress
                .Where(z => z.ClientSiteId == clientSiteId)
                .OrderBy(z => z.Id)
                .LastOrDefault();
        }

        public void SaveClientSiteDuress(int clientSiteId, int guardId, string gpsCoordinates, string enabledAddress, GuardLog tmzdata, int linkedDuressParentSiteId, int isLinkedDuressParentSite)
        {
            var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmzdata.EventDateTimeUtcOffsetMinute);  // p6#73 timezone bug - Added by binoy 24-01-2024
            _context.ClientSiteDuress.Add(new ClientSiteDuress()
            {
                ClientSiteId = clientSiteId,
                IsEnabled = true,
                EnabledBy = guardId,
                EnabledDate = localDateTime, //DateTime.Today,
                GpsCoordinates = gpsCoordinates,
                EnabledAddress = enabledAddress,
                PlayDuressAlarm = true,
                EnabledDateTimeLocal = tmzdata.EventDateTimeLocal,
                EnabledDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                EnabledDateTimeZone = tmzdata.EventDateTimeZone,
                EnabledDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                EnabledDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                IsLinkedDuressParentSite = isLinkedDuressParentSite,
                LinkedDuressParentSiteId = linkedDuressParentSiteId


            });
            _context.SaveChanges();


        }
        //To Save ActionList
        public void SaveActionList(ActionListNotification ActionList)
        {
            var ActionListToUpdate = _context.ActionListNotification.SingleOrDefault(x => x.ClientSiteID == ActionList.ClientSiteID);
            if (ActionListToUpdate == null)
            {
                _context.ActionListNotification.Add(new ActionListNotification()
                {
                    ClientSiteID = ActionList.ClientSiteID,
                    AlarmKeypadCode = ActionList.AlarmKeypadCode,
                    Physicalkey = ActionList.Physicalkey,
                    CombinationLook = ActionList.CombinationLook,
                    Action1 = ActionList.Action1,
                    Action2 = ActionList.Action2,
                    Action3 = ActionList.Action3,
                    Action4 = ActionList.Action4,
                    Action5 = ActionList.Action5,
                    CommentsForControlRoomOperator = ActionList.CommentsForControlRoomOperator,
                    Message = ActionList.Message
                });
            }
            else
            {
                ActionListToUpdate.AlarmKeypadCode = ActionList.AlarmKeypadCode;
                ActionListToUpdate.Physicalkey = ActionList.Physicalkey;
                ActionListToUpdate.CombinationLook = ActionList.CombinationLook;
                ActionListToUpdate.Action1 = ActionList.Action1;
                ActionListToUpdate.Action2 = ActionList.Action2;
                ActionListToUpdate.Action3 = ActionList.Action3;
                ActionListToUpdate.Action4 = ActionList.Action4;
                ActionListToUpdate.Action5 = ActionList.Action5;
                ActionListToUpdate.CommentsForControlRoomOperator = ActionList.CommentsForControlRoomOperator;
                ActionListToUpdate.Message = ActionList.Message;
            }

            _context.SaveChanges();


        }

        public void SaveGuardLog(GuardLog guardLog)
        {
            if (guardLog.Id == 0)
            {
                _context.GuardLogs.Add(new GuardLog()
                {
                    ClientSiteLogBookId = guardLog.ClientSiteLogBookId,
                    EventDateTime = guardLog.EventDateTime,
                    Notes = guardLog.Notes,
                    GuardLoginId = guardLog.GuardLoginId,
                    IsSystemEntry = guardLog.IsSystemEntry,
                    IrEntryType = guardLog.IrEntryType,
                    RcPushMessageId = guardLog.RcPushMessageId,
                    EventDateTimeLocal = guardLog.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = guardLog.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = guardLog.EventDateTimeZone,
                    EventDateTimeZoneShort = guardLog.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = guardLog.EventDateTimeUtcOffsetMinute, // Task p6#73_TimeZone issue -- added by Binoy - End

                    PlayNotificationSound = guardLog.PlayNotificationSound,
                    GpsCoordinates = guardLog.GpsCoordinates,
                    IsIRReportTypeEntry = guardLog.IsIRReportTypeEntry,
                    RcLogbookStamp = guardLog.RcLogbookStamp,
                    EventType = guardLog.EventType

                });
            }
            else
            {
                var guardLogToUpdate = _context.GuardLogs.SingleOrDefault(x => x.Id == guardLog.Id);
                if (guardLogToUpdate == null)
                    throw new InvalidOperationException();

                guardLogToUpdate.Notes = guardLog.Notes;
            }
            _context.SaveChanges();
        }

        public void DeleteGuardLog(int id)
        {
            var guardLogToDelete = _context.GuardLogs.SingleOrDefault(x => x.Id == id);
            if (guardLogToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(guardLogToDelete);
            _context.SaveChanges();
        }

        public List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRego(string vehicleRego)
        {
            var results = _context.KeyVehicleLogs.Where(x => x.VehicleRego == vehicleRego && !x.ExitTime.HasValue && x.EntryTime >= DateTime.Today);

            results.Include(x => x.ClientSiteLogBook)
                .ThenInclude(x => x.ClientSite)
                .Load();


            return results.ToList();
        }

        public List<KeyVehicleLog> GetOpenKeyVehicleLogsByVehicleRegoForTrailer(string trailer1Rego, string trailer2Rego, string trailer3Rego, string trailer4Rego)
        {
            var results = _context.KeyVehicleLogs.Where(x =>
            ((x.Trailer1Rego == trailer1Rego && !string.IsNullOrEmpty(trailer1Rego)) || (x.Trailer2Rego == trailer1Rego && !string.IsNullOrEmpty(trailer1Rego)) || (x.Trailer3Rego == trailer1Rego && !string.IsNullOrEmpty(trailer1Rego)) || (x.Trailer4Rego == trailer1Rego && !string.IsNullOrEmpty(trailer1Rego)) ||
                    (x.Trailer1Rego == trailer2Rego && !string.IsNullOrEmpty(trailer2Rego)) || (x.Trailer2Rego == trailer2Rego && !string.IsNullOrEmpty(trailer2Rego)) || (x.Trailer3Rego == trailer2Rego && !string.IsNullOrEmpty(trailer2Rego)) || (x.Trailer4Rego == trailer2Rego && !string.IsNullOrEmpty(trailer2Rego)) ||
                    (x.Trailer1Rego == trailer3Rego && !string.IsNullOrEmpty(trailer3Rego)) || (x.Trailer2Rego == trailer3Rego && !string.IsNullOrEmpty(trailer3Rego)) || (x.Trailer3Rego == trailer3Rego && !string.IsNullOrEmpty(trailer3Rego)) || (x.Trailer4Rego == trailer3Rego && !string.IsNullOrEmpty(trailer3Rego)) ||
                    (x.Trailer1Rego == trailer4Rego && !string.IsNullOrEmpty(trailer4Rego)) || (x.Trailer2Rego == trailer4Rego && !string.IsNullOrEmpty(trailer4Rego)) || (x.Trailer3Rego == trailer4Rego && !string.IsNullOrEmpty(trailer4Rego)) || (x.Trailer4Rego == trailer4Rego && !string.IsNullOrEmpty(trailer4Rego)))
            && !x.ExitTime.HasValue && x.EntryTime >= DateTime.Today);

            results.Include(x => x.ClientSiteLogBook)
                .ThenInclude(x => x.ClientSite)
                .Load();

            return results.ToList();
        }



        public List<KeyVehicleLog> GetKeyVehicleLogs(int logBookId)
        {
            var results = _context.KeyVehicleLogs.Where(z => z.ClientSiteLogBookId == logBookId);

            results.Include(x => x.ClientSiteLogBook)
                .Include(x => x.GuardLogin)
                .Include(x => x.ClientSiteLocation)
                .Include(x => x.ClientSitePoc)
                .Load();

            return results.OrderBy(z => z.EntryTime).ToList();
        }

        public List<KeyVehicleLog> GetKeyVehicleLogs(int[] clientSiteIds, DateTime logFromDate, DateTime logToDate)
        {
            var results = _context.KeyVehicleLogs
               .Where(z => clientSiteIds.Contains(z.ClientSiteLogBook.ClientSiteId) && z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog
                            && z.EntryTime >= logFromDate && z.EntryTime < logToDate.AddDays(1))
               .Include(z => z.GuardLogin.Guard)
               .Include(x => x.ClientSiteLocation)
               .Include(x => x.ClientSitePoc);

            results.Include(x => x.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .Load();

            return results.OrderBy(z => z.EntryTime).ToList();
        }
        public List<KeyVehicleLog> GetKeyVehicleLogsWithPOI(int[] clientSiteIds, int[] personOfInterestIds, DateTime logFromDate, DateTime logToDate)

        {
            var results = _context.KeyVehicleLogs
               .Where(z => clientSiteIds.Contains(z.ClientSiteLogBook.ClientSiteId) && z.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog
                            && z.EntryTime >= logFromDate && z.EntryTime < logToDate.AddDays(1))
               .Include(z => z.GuardLogin.Guard)
               .Include(x => x.ClientSiteLocation)
               .Include(x => x.ClientSitePoc);

            results.Include(x => x.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .Load();

            return results.OrderBy(z => z.EntryTime).ToList();
        }
        public KeyVehicleLog GetKeyVehicleLogById(int id)
        {
            return _context.KeyVehicleLogs
                .Include(z => z.GuardLogin.Guard)
                .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)
                .Include(z => z.ClientSitePoc)
                .Include(z => z.ClientSiteLocation)
                .SingleOrDefault(z => z.Id == id);
        }
        public ClientSitePoc GetClientSitePOCName(int id)
        {
            return _context.ClientSitePocs.Where(x => x.Id == id).SingleOrDefault();
        }
        public ClientSitePoc GetEmailPOC(int id)
        {
            return _context.ClientSitePocs
                .Where(x => x.Id == id).SingleOrDefault();
        }
        public KeyVehcileLogField GetIndividualType(int PersonType)
        {
            return _context.KeyVehcileLogFields.SingleOrDefault(z => z.Id == PersonType);
        }
        public List<KeyVehicleLog> GetKeyVehicleLogByIds(int[] ids)
        {
            return _context.KeyVehicleLogs.Where(z => ids.Contains(z.Id) && z.ClientSiteLogBook.ClientSite.IsActive == true)
                .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)
                .ToList();
        }
        public List<KeyVehicleLog> GetPOIAlert(string companyname, string individualname, int individualtype)
        {
            //return _context.KeyVehicleLogs.Where(z =>  z.CompanyName==companyname && z.PersonName==individualname && z.PersonType==individualtype && z.IsPOIAlert==true)
            // .Include(z => z.ClientSiteLogBook)
            //    .ThenInclude(z => z.ClientSite)
            //    .ToList();
            return _context.KeyVehicleLogs.Where(z => z.CompanyName == companyname && z.PersonName == individualname && z.PersonType == individualtype && z.PersonOfInterest != 0)
            .Include(z => z.ClientSiteLogBook)
               .ThenInclude(z => z.ClientSite)
               .ToList();

        }

        public void SaveKeyVehicleLog(KeyVehicleLog keyVehicleLog)
        {
            try
            {

                if (keyVehicleLog.Id == 0)
                {

                    _context.KeyVehicleLogs.Add(keyVehicleLog);
                    _context.SaveChanges();

                    /* update already existing CRM Company details for the keyVehicleLog for fix the issue(the company details are taking frm keyVehicleLog RC and other modules  ) */
                    if (keyVehicleLog.Website != string.Empty || keyVehicleLog.Email != string.Empty
                        || keyVehicleLog.CompanyABN != string.Empty || keyVehicleLog.CompanyLandline != string.Empty)
                    {
                        var CRMdetails = GetKeyVehicleLogs(keyVehicleLog.VehicleRego.Trim());
                        if (CRMdetails.Count != 0)
                        {
                            foreach (var kvp in CRMdetails)
                            {

                                kvp.Website = keyVehicleLog.Website;
                                kvp.Email = keyVehicleLog.Email;
                                kvp.CompanyABN = keyVehicleLog.CompanyABN;
                                kvp.CompanyLandline = keyVehicleLog.CompanyLandline;
                                _context.SaveChanges();
                            }

                        }


                    }




                }
                else
                {
                    var keyVehicleLogToUpdate = _context.KeyVehicleLogs.SingleOrDefault(x => x.Id == keyVehicleLog.Id);

                    keyVehicleLogToUpdate.InitialCallTime = keyVehicleLog.InitialCallTime;
                    keyVehicleLogToUpdate.EntryTime = keyVehicleLog.EntryTime;
                    keyVehicleLogToUpdate.SentInTime = keyVehicleLog.SentInTime;
                    keyVehicleLogToUpdate.ExitTime = keyVehicleLog.ExitTime;
                    keyVehicleLogToUpdate.TimeSlotNo = keyVehicleLog.TimeSlotNo;
                    keyVehicleLogToUpdate.PersonType = keyVehicleLog.PersonType;
                    keyVehicleLogToUpdate.VehicleRego = keyVehicleLog.VehicleRego;
                    keyVehicleLogToUpdate.CompanyName = keyVehicleLog.CompanyName;
                    keyVehicleLogToUpdate.Trailer1Rego = keyVehicleLog.Trailer1Rego;
                    keyVehicleLogToUpdate.Trailer2Rego = keyVehicleLog.Trailer2Rego;
                    keyVehicleLogToUpdate.Trailer3Rego = keyVehicleLog.Trailer3Rego;
                    keyVehicleLogToUpdate.Trailer4Rego = keyVehicleLog.Trailer4Rego;
                    keyVehicleLogToUpdate.PlateId = keyVehicleLog.PlateId;
                    keyVehicleLogToUpdate.TruckConfig = keyVehicleLog.TruckConfig;
                    keyVehicleLogToUpdate.KeyNo = keyVehicleLog.KeyNo;
                    keyVehicleLogToUpdate.PersonName = keyVehicleLog.PersonName;
                    keyVehicleLogToUpdate.MobileNumber = keyVehicleLog.MobileNumber;
                    keyVehicleLogToUpdate.TrailerType = keyVehicleLog.TrailerType;
                    keyVehicleLogToUpdate.InWeight = keyVehicleLog.InWeight;
                    keyVehicleLogToUpdate.OutWeight = keyVehicleLog.OutWeight;
                    keyVehicleLogToUpdate.TareWeight = keyVehicleLog.TareWeight;
                    keyVehicleLogToUpdate.MaxWeight = keyVehicleLog.MaxWeight;
                    keyVehicleLogToUpdate.Notes = keyVehicleLog.Notes;
                    keyVehicleLogToUpdate.Product = keyVehicleLog.Product;
                    keyVehicleLogToUpdate.EntryReason = keyVehicleLog.EntryReason;
                    keyVehicleLogToUpdate.ClientSitePocId = keyVehicleLog.ClientSitePocId;
                    keyVehicleLogToUpdate.ClientSiteLocationId = keyVehicleLog.ClientSiteLocationId;
                    keyVehicleLogToUpdate.MoistureDeduction = keyVehicleLog.MoistureDeduction;
                    keyVehicleLogToUpdate.RubbishDeduction = keyVehicleLog.RubbishDeduction;
                    keyVehicleLogToUpdate.DeductionPercentage = keyVehicleLog.DeductionPercentage;
                    keyVehicleLogToUpdate.IsTimeSlotNo = keyVehicleLog.IsTimeSlotNo;
                    keyVehicleLogToUpdate.Reels = keyVehicleLog.Reels;
                    keyVehicleLogToUpdate.CustomerRef = keyVehicleLog.CustomerRef;
                    keyVehicleLogToUpdate.Vwi = keyVehicleLog.Vwi;
                    keyVehicleLogToUpdate.Sender = keyVehicleLog.Sender;
                    keyVehicleLogToUpdate.IsSender = keyVehicleLog.IsSender;
                    keyVehicleLogToUpdate.PersonOfInterest = keyVehicleLog.PersonOfInterest;
                    keyVehicleLogToUpdate.IsBDM = keyVehicleLog.IsBDM;
                    if (keyVehicleLog.CRMId != null)
                    {
                        keyVehicleLogToUpdate.CRMId = keyVehicleLog.CRMId;
                        keyVehicleLogToUpdate.IndividualTitle = keyVehicleLog.IndividualTitle;
                        keyVehicleLogToUpdate.Gender = keyVehicleLog.Gender;
                        keyVehicleLogToUpdate.CompanyABN = keyVehicleLog.CompanyABN;
                        keyVehicleLogToUpdate.CompanyLandline = keyVehicleLog.CompanyLandline;
                        keyVehicleLogToUpdate.Email = keyVehicleLog.Email;
                        keyVehicleLogToUpdate.Website = keyVehicleLog.Website;
                        keyVehicleLogToUpdate.BDMList = keyVehicleLog.BDMList;

                    }

                    keyVehicleLogToUpdate.IsDocketNo = keyVehicleLog.IsDocketNo;
                    keyVehicleLogToUpdate.LoaderName = keyVehicleLog.LoaderName;
                    keyVehicleLogToUpdate.DispatchName = keyVehicleLog.DispatchName;

                    keyVehicleLogToUpdate.IsReels = keyVehicleLog.IsReels;
                    keyVehicleLogToUpdate.IsVWI = keyVehicleLog.IsVWI;
                    keyVehicleLogToUpdate.IsISOVIN = keyVehicleLog.IsISOVIN;

                    keyVehicleLogToUpdate.Trailer1PlateId = keyVehicleLog.Trailer1PlateId;
                    keyVehicleLogToUpdate.Trailer2PlateId = keyVehicleLog.Trailer2PlateId;
                    keyVehicleLogToUpdate.Trailer3PlateId = keyVehicleLog.Trailer3PlateId;
                    keyVehicleLogToUpdate.Trailer4PlateId = keyVehicleLog.Trailer4PlateId;

                    keyVehicleLogToUpdate.ClientSitePocIdsVehicleLog = keyVehicleLog.ClientSitePocIdsVehicleLog;

                    keyVehicleLogToUpdate.EmailCompany = keyVehicleLog.EmailCompany;
                    keyVehicleLogToUpdate.Emailindividual = keyVehicleLog.Emailindividual;



                    _context.SaveChanges();
                }

            }
            catch (Exception ex)
            {


            }

        }



        public void InsertPreviousLogBook(KeyVehicleLog keyVehicleLog)
        {

            try
            {
                /* this condition added for prevent duplicate kV p7 103 issue 30112023 dileep
                  the insert with entity framework shows some key reference issue ,so query using
                 */
                var checkifAlreadyExist = _context.KeyVehicleLogs.Where(x => x.InitialCallTime == keyVehicleLog.InitialCallTime
            && x.EntryTime == keyVehicleLog.EntryTime && x.SentInTime == keyVehicleLog.SentInTime && x.VehicleRego == keyVehicleLog.VehicleRego).ToList();
                if (checkifAlreadyExist.Count == 0)
                {
                    _context.Database.ExecuteSqlRaw(
                    " INSERT INTO VehicleKeyLogs (ClientSiteLogBookId, GuardLoginId, EntryTime, SentInTime, ExitTime, VehicleRego, Trailer1Rego, " +
                    " Trailer2Rego, Trailer3Rego, Plate, KeyNo, CompanyName, PersonName, PersonType, MobileNumber, PurposeOfEntry, InWeight, OutWeight, " +
                    " TareWeight, Notes, TimeSlotNo, TruckConfig, TrailerType, MaxWeight, Trailer4Rego, EntryReason, ClientSitePocId, ClientSiteLocationId," +
                    " KeyDescription, InitialCallTime, ReportReference, PlateId, MoistureDeduction, RubbishDeduction, DeductionPercentage, CopiedFromId," +
                    " IsTimeSlotNo, Reels, CustomerRef, Wvi, IsSender, Sender, DocketSerialNo, POIImage, PersonOfInterest, IsBDM, IndividualTitle, Gender, " +
                    " CompanyABN, CompanyLandline, Email, Website, CRMId, BDMList) VALUES (@ClientSiteLogBookId, @GuardLoginId, @EntryTime, @SentInTime, @ExitTime, @VehicleRego, @Trailer1Rego, @Trailer2Rego, @Trailer3Rego, @Plate," +
                    " @KeyNo, @CompanyName, @PersonName, @PersonType, @MobileNumber, @PurposeOfEntry, @InWeight, @OutWeight,@TareWeight, @Notes, @TimeSlotNo, @TruckConfig, @TrailerType, @MaxWeight, @Trailer4Rego, @EntryReason, @ClientSitePocId," +
                    " @ClientSiteLocationId, @KeyDescription, @InitialCallTime, @ReportReference, @PlateId, @MoistureDeduction, @RubbishDeduction, @DeductionPercentage, @CopiedFromId, @IsTimeSlotNo, @Reels, @CustomerRef, @Wvi, @IsSender, @Sender," +
                    " @DocketSerialNo, @POIImage, @PersonOfInterest, @IsBDM, @IndividualTitle, @Gender, @CompanyABN, @CompanyLandline, @Email, @Website, @CRMId, @BDMList)",
                     new SqlParameter("@ClientSiteLogBookId", keyVehicleLog.ClientSiteLogBookId == null ? DBNull.Value : keyVehicleLog.ClientSiteLogBookId),
                     new SqlParameter("@GuardLoginId", keyVehicleLog.GuardLoginId == null ? DBNull.Value : keyVehicleLog.GuardLoginId),
                     new SqlParameter("@EntryTime", keyVehicleLog.EntryTime == null ? DBNull.Value : keyVehicleLog.EntryTime),
                     new SqlParameter("@SentInTime", keyVehicleLog.SentInTime == null ? DBNull.Value : keyVehicleLog.SentInTime),
                     new SqlParameter("@ExitTime", keyVehicleLog.ExitTime == null ? DBNull.Value : keyVehicleLog.ExitTime),
                     new SqlParameter("@VehicleRego", keyVehicleLog.VehicleRego == null ? DBNull.Value : keyVehicleLog.VehicleRego),
                     new SqlParameter("@Trailer1Rego", keyVehicleLog.Trailer1Rego == null ? DBNull.Value : keyVehicleLog.Trailer1Rego),
                     new SqlParameter("@Trailer2Rego", keyVehicleLog.Trailer2Rego == null ? DBNull.Value : keyVehicleLog.Trailer2Rego),
                     new SqlParameter("@Trailer3Rego", keyVehicleLog.Trailer3Rego == null ? DBNull.Value : keyVehicleLog.Trailer3Rego),
                     new SqlParameter("@Plate", DBNull.Value),
                     new SqlParameter("@KeyNo", keyVehicleLog.KeyNo == null ? DBNull.Value : keyVehicleLog.KeyNo),
                     new SqlParameter("@CompanyName", keyVehicleLog.CompanyName == null ? DBNull.Value : keyVehicleLog.CompanyName),
                     new SqlParameter("@PersonName", keyVehicleLog.PersonName == null ? DBNull.Value : keyVehicleLog.PersonName),
                     new SqlParameter("@PersonType", keyVehicleLog.PersonType == null ? DBNull.Value : keyVehicleLog.PersonType),
                     new SqlParameter("@MobileNumber", keyVehicleLog.MobileNumber == null ? DBNull.Value : keyVehicleLog.MobileNumber),
                     new SqlParameter("@PurposeOfEntry", DBNull.Value),
                     new SqlParameter("@InWeight", keyVehicleLog.InWeight == null ? DBNull.Value : keyVehicleLog.InWeight),
                     new SqlParameter("@OutWeight", keyVehicleLog.OutWeight == null ? DBNull.Value : keyVehicleLog.OutWeight),
                     new SqlParameter("@TareWeight", keyVehicleLog.TareWeight == null ? DBNull.Value : keyVehicleLog.TareWeight),
                     new SqlParameter("@Notes", keyVehicleLog.Notes == null ? DBNull.Value : keyVehicleLog.Notes),
                     new SqlParameter("@TimeSlotNo", keyVehicleLog.TimeSlotNo == null ? DBNull.Value : keyVehicleLog.TimeSlotNo),
                     new SqlParameter("@TruckConfig", keyVehicleLog.TruckConfig == null ? DBNull.Value : keyVehicleLog.TruckConfig),
                     new SqlParameter("@TrailerType", keyVehicleLog.TrailerType == null ? DBNull.Value : keyVehicleLog.TrailerType),
                     new SqlParameter("@MaxWeight", keyVehicleLog.MaxWeight == null ? DBNull.Value : keyVehicleLog.MaxWeight),
                     new SqlParameter("@Trailer4Rego", keyVehicleLog.Trailer4Rego == null ? DBNull.Value : keyVehicleLog.Trailer4Rego),
                     new SqlParameter("@EntryReason", keyVehicleLog.EntryReason == null ? DBNull.Value : keyVehicleLog.EntryReason),
                     new SqlParameter("@ClientSitePocId", keyVehicleLog.ClientSitePocId == null ? DBNull.Value : keyVehicleLog.ClientSitePocId),
                     new SqlParameter("@ClientSiteLocationId", keyVehicleLog.ClientSiteLocationId == null ? DBNull.Value : keyVehicleLog.ClientSiteLocationId),
                     new SqlParameter("@KeyDescription", DBNull.Value),
                     new SqlParameter("@InitialCallTime", keyVehicleLog.InitialCallTime == null ? DBNull.Value : keyVehicleLog.InitialCallTime),
                     new SqlParameter("@ReportReference", keyVehicleLog.ReportReference == null ? DBNull.Value : keyVehicleLog.ReportReference),
                     new SqlParameter("@PlateId", keyVehicleLog.PlateId == null ? DBNull.Value : keyVehicleLog.PlateId),
                     new SqlParameter("@MoistureDeduction", keyVehicleLog.MoistureDeduction == null ? DBNull.Value : keyVehicleLog.MoistureDeduction),
                     new SqlParameter("@RubbishDeduction", keyVehicleLog.RubbishDeduction == null ? DBNull.Value : keyVehicleLog.RubbishDeduction),
                     new SqlParameter("@DeductionPercentage", keyVehicleLog.DeductionPercentage == null ? DBNull.Value : keyVehicleLog.DeductionPercentage),
                     new SqlParameter("@CopiedFromId", keyVehicleLog.CopiedFromId == null ? DBNull.Value : keyVehicleLog.CopiedFromId),
                     new SqlParameter("@IsTimeSlotNo", keyVehicleLog.IsTimeSlotNo == null ? DBNull.Value : keyVehicleLog.IsTimeSlotNo),
                     new SqlParameter("@Reels", keyVehicleLog.Reels == null ? DBNull.Value : keyVehicleLog.Reels),
                     new SqlParameter("@CustomerRef", keyVehicleLog.CustomerRef == null ? DBNull.Value : keyVehicleLog.CustomerRef),
                     new SqlParameter("@Wvi", keyVehicleLog.Vwi == null ? DBNull.Value : keyVehicleLog.Vwi),
                     new SqlParameter("@IsSender", keyVehicleLog.IsSender == null ? DBNull.Value : keyVehicleLog.IsSender),
                     new SqlParameter("@Sender", keyVehicleLog.Sender == null ? DBNull.Value : keyVehicleLog.Sender),
                     new SqlParameter("@DocketSerialNo", keyVehicleLog.DocketSerialNo == null ? DBNull.Value : keyVehicleLog.DocketSerialNo),
                     new SqlParameter("@POIImage", keyVehicleLog.POIImage == null ? DBNull.Value : keyVehicleLog.POIImage),
                     new SqlParameter("@PersonOfInterest", keyVehicleLog.PersonOfInterest == null ? DBNull.Value : keyVehicleLog.PersonOfInterest),
                     new SqlParameter("@IsBDM", keyVehicleLog.IsBDM == null ? DBNull.Value : keyVehicleLog.IsBDM),
                     new SqlParameter("@IndividualTitle", keyVehicleLog.IndividualTitle == null ? DBNull.Value : keyVehicleLog.IndividualTitle),
                     new SqlParameter("@Gender", keyVehicleLog.Gender == null ? DBNull.Value : keyVehicleLog.Gender),
                     new SqlParameter("@CompanyABN", keyVehicleLog.CompanyABN == null ? DBNull.Value : keyVehicleLog.CompanyABN),
                     new SqlParameter("@CompanyLandline", keyVehicleLog.CompanyLandline == null ? DBNull.Value : keyVehicleLog.CompanyLandline),
                     new SqlParameter("@Email", keyVehicleLog.Email == null ? DBNull.Value : keyVehicleLog.Email),
                     new SqlParameter("@Website", keyVehicleLog.Website == null ? DBNull.Value : keyVehicleLog.Website),
                     new SqlParameter("@CRMId", keyVehicleLog.CRMId == null ? DBNull.Value : keyVehicleLog.CRMId),
                     new SqlParameter("@BDMList", keyVehicleLog.BDMList == null ? DBNull.Value : keyVehicleLog.BDMList)
                     );


                }


            }
            catch (Exception ex)
            {

            }

        }


        public void SaveDocketSerialNo(int id, string serialNo)
        {
            var keyVehicleLog = _context.KeyVehicleLogs.SingleOrDefault(i => i.Id == id);
            if (keyVehicleLog != null)
            {
                keyVehicleLog.DocketSerialNo = serialNo;
                _context.SaveChanges();
            }
        }

        public void DeleteKeyVehicleLog(int id)
        {
            var keyVehicleLogToDelete = _context.KeyVehicleLogs.SingleOrDefault(i => i.Id == id);
            if (keyVehicleLogToDelete != null)
            {
                _context.Remove(keyVehicleLogToDelete);
                _context.SaveChanges();
            }
        }

        public void KeyVehicleLogQuickExit(int id, DateTime? ExitTimeLocal)
        {
            var keyVehicleLog = _context.KeyVehicleLogs.SingleOrDefault(x => x.Id == id);
            if (keyVehicleLog != null)
            {
                keyVehicleLog.ExitTime = ExitTimeLocal;
                _context.SaveChanges();
            }
        }

        public List<PatrolCarLog> GetPatrolCarLogs(int logBookId)
        {
            var result = _context.PatrolCarLogs
                .Where(z => z.ClientSiteLogBookId == logBookId)
                .Include(x => x.ClientSiteLogBook)
                .Include(x => x.ClientSitePatrolCar)
                .ToList();

            return result;
        }

        public List<PatrolCarLog> GetPatrolCarLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            var result = _context.PatrolCarLogs
                .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate)
                .Include(x => x.ClientSiteLogBook)
                .Include(x => x.ClientSitePatrolCar)
                .ToList();

            return result;
        }

        public void SavePatrolCarLog(PatrolCarLog patrolCarLog)
        {
            if (patrolCarLog.Id == 0)
            {
                _context.PatrolCarLogs.Add(patrolCarLog);
            }
            else
            {
                var patrolCarDetailsToUpdate = _context.PatrolCarLogs.SingleOrDefault(x => x.Id == patrolCarLog.Id);
                patrolCarDetailsToUpdate.Mileage = patrolCarLog.Mileage;
            }
            _context.SaveChanges();
        }

        public void SavePatrolCarLogs(IEnumerable<PatrolCarLog> patrolCarLogs)
        {
            var patrolCarLogsToInsert = patrolCarLogs.Where(z => z.Id == 0);
            if (patrolCarLogsToInsert.Any())
            {
                _context.PatrolCarLogs.AddRange(patrolCarLogsToInsert);
                _context.SaveChanges();
            }
        }

        public List<ClientSiteCustomField> GetClientSiteCustomFields()
        {
            return _context.ClientSiteCustomFields
                .Where(x => x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .ToList();
        }

        public List<ClientSiteCustomField> GetCustomFieldsByClientSiteId(int clientSiteId)
        {
            return _context.ClientSiteCustomFields.Where(z => z.ClientSiteId == clientSiteId).ToList();
        }

        public List<GuardLogin> GetLastLoginNew(int GuradId)
        {
            try
            {
                var guardLogins = _context.GuardLogins.Where(x => x.GuardId == GuradId);

                if (!guardLogins.Any())
                {
                    // No records found for the provided GuradId, return an empty list
                    return new List<GuardLogin>();
                }

                var lastLoginDate = guardLogins
                    .Select(x => x.LoginDate)
                    .Max(); // Find the maximum LoginDate

                var GuraLoginId = _context.GuardLogins
                    .Where(x => x.GuardId == GuradId && x.LoginDate.Date == lastLoginDate.Date)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefault();

                if (GuraLoginId == null)
                {
                    // GuraLoginId is null, which means no corresponding record found, return an empty list
                    return new List<GuardLogin>();
                }

                var GuardLogGuraId = _context.GuardLogs.Where(x => x.GuardLoginId == GuraLoginId.Id);

                var LastEventLoginDate = GuardLogGuraId.Select(x => x.EventDateTime.Date)
            .OrderByDescending(EventDateTime => EventDateTime)
            .Take(5)
            .ToList();

                var result = _context.GuardLogins
     .Where(x => x.GuardId == GuradId)
     .Include(x=>x.ClientSite)
     .OrderByDescending(x => x.LoginDate)
     .Take(5)
     .ToList();

                return result;
            }
            catch (Exception ex)
            {
                // Handle the exception here (log it, return a specific error response, etc.)
                // For now, we'll just return an empty list
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<GuardLogin>();
            }
        }
        public int SaveClientSiteCustomFields(ClientSiteCustomField clientSiteCustomField)
        {
            if (clientSiteCustomField.Id == 0)
            {
                _context.ClientSiteCustomFields.Add(clientSiteCustomField);
            }
            else
            {
                var clientSiteCustomFieldToUpdate = _context.ClientSiteCustomFields.SingleOrDefault(x => x.Id == clientSiteCustomField.Id);
                if (clientSiteCustomFieldToUpdate != null)
                {
                    clientSiteCustomFieldToUpdate.Name = clientSiteCustomField.Name;
                    clientSiteCustomFieldToUpdate.TimeSlot = clientSiteCustomField.TimeSlot;
                }
            }
            _context.SaveChanges();
            return clientSiteCustomField.Id;
        }

        public void DeleteClientSiteCustomFields(int id)
        {
            var clientSiteCustomFieldToDelete = _context.ClientSiteCustomFields.SingleOrDefault(i => i.Id == id);
            if (clientSiteCustomFieldToDelete != null)
            {
                _context.Remove(clientSiteCustomFieldToDelete);
                _context.SaveChanges();
            }
        }

        public List<CustomFieldLog> GetCustomFieldLogs(int logBookId)
        {
            return _context.CustomFieldLogs
                .Include(z => z.ClientSiteCustomField)
                .Include(z => z.ClientSiteLogBook)
                .Where(x => x.ClientSiteLogBookId == logBookId)
                .ToList();
        }

        public List<CustomFieldLog> GetCustomFieldLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            return _context.CustomFieldLogs
                .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate)
                .Include(x => x.ClientSiteLogBook)
                .Include(x => x.ClientSiteCustomField)
                .ToList();
        }

        public void SaveCustomFieldLogs(List<CustomFieldLog> customFieldLogs)
        {
            foreach (var customFieldLog in customFieldLogs)
            {
                SaveCustomFieldLog(customFieldLog);
            }
        }

        public void SaveCustomFieldLog(CustomFieldLog customFieldLog)
        {
            if (customFieldLog.Id == 0)
            {
                _context.CustomFieldLogs.Add(customFieldLog);
            }
            else
            {
                var customFieldLogToUpdate = _context.CustomFieldLogs.SingleOrDefault(x => x.Id == customFieldLog.Id);
                if (customFieldLogToUpdate != null)
                {
                    customFieldLogToUpdate.DayValue = customFieldLog.DayValue;
                }
            }
            _context.SaveChanges();
        }

        public List<string> GetVehicleRegos(string regoStart = null)
        {
            return _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.VehicleRego) &&
                                z.VehicleRego.Substring(0, regoStart.Length).ToLower() == regoStart.ToLower()))
                .Select(z => z.VehicleRego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
        }
        public List<string> GetVehicleRegosForKVL(string regoStart = null)
        {
            return _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.VehicleRego) &&
                                z.VehicleRego.Contains(regoStart)))
                .Select(z => z.VehicleRego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
        }
        public List<string> GetClientSiteSearch(string clientSiteNew = null)
        {
            return _context.ClientSites
                .Where(z => string.IsNullOrEmpty(clientSiteNew) ||
                            (!string.IsNullOrEmpty(z.Name) &&
                                z.Name.Contains(clientSiteNew)))
                .Select(z => z.Name)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
        }
        ////trailer changes New change for Add rigo without plate number 21032024 dileep start*//
        public List<string> GetTrailerRegosForKVL(string regoStart = null)
        {
            var newList = new List<string>();
            var trailerRego = _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.VehicleRego) &&
                                z.VehicleRego.Contains(regoStart)))
                .Select(z => z.VehicleRego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();


            var trailer1Rego = _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.Trailer1Rego) &&
                                z.Trailer1Rego.Contains(regoStart)))
                .Select(z => z.Trailer1Rego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
            var trailer2Rego = _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.Trailer2Rego) &&
                                z.Trailer2Rego.Contains(regoStart)))
                .Select(z => z.Trailer2Rego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
            var trailer3Rego = _context.KeyVehicleLogVisitorProfiles
                .Where(z => string.IsNullOrEmpty(regoStart) ||
                            (!string.IsNullOrEmpty(z.Trailer3Rego) &&
                                z.Trailer3Rego.Contains(regoStart)))
                .Select(z => z.Trailer3Rego)
                .Distinct()
                .OrderBy(z => z)
                .ToList();
            var trailer4Rego = _context.KeyVehicleLogVisitorProfiles
               .Where(z => string.IsNullOrEmpty(regoStart) ||
                           (!string.IsNullOrEmpty(z.Trailer4Rego) &&
                               z.Trailer4Rego.Contains(regoStart)))
               .Select(z => z.Trailer4Rego)
               .Distinct()
               .OrderBy(z => z)
               .ToList();

            newList.AddRange(trailerRego);
            newList.AddRange(trailer1Rego);
            newList.AddRange(trailer2Rego);
            newList.AddRange(trailer3Rego);
            newList.AddRange(trailer3Rego);
            return newList.Distinct().OrderBy(s => s.FirstOrDefault()).ToList();
        }
        ////taliler changes New change for Add rigo without plate number 21032024 dileep end*//
        public List<string> GetCompanyNames(string companyNameStart)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Where(x => !string.IsNullOrEmpty(x.CompanyName) && x.CompanyName.Substring(0, companyNameStart.Length).ToLower() == companyNameStart.ToLower())
                .Select(x => x.CompanyName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public List<string> GetSenderNames(string senderNameStart)
        {
            return _context.KeyVehicleLogs
                .Where(x => !string.IsNullOrEmpty(x.Sender) && x.Sender.Substring(0, senderNameStart.Length).ToLower() == senderNameStart.ToLower())
                .Select(x => x.Sender)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public KeyVehicleLogVisitorPersonalDetail GetKeyVehicleLogProfileWithPersonalDetails(int id)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .SingleOrDefault(z => z.Id == id);
        }

        public KeyVehicleLogProfile GetKeyVehicleLogVisitorProfile(string truckRego)
        {
            return _context.KeyVehicleLogVisitorProfiles
                            .SingleOrDefault(z => z.VehicleRego == truckRego);
        }
        public KeyVehicleLogProfile GetKeyVehicleLogVisitorProfileUsingTrailerRigo(string TrailerRigo1, string TrailerRigo2,
            string TrailerRigo3, string TrailerRigo4, int? TrailerRigo1Id, int? TrailerRigo2Id, int? TrailerRigo3Id, int? TrailerRigo4Id)
        {
            return _context.KeyVehicleLogVisitorProfiles
            .SingleOrDefault(z => z.Trailer1Rego == TrailerRigo1
            && z.Trailer2Rego == TrailerRigo2
            && z.Trailer3Rego == TrailerRigo3
            && z.Trailer4Rego == TrailerRigo4
            && z.Trailer1PlateId == TrailerRigo1Id
            && z.Trailer2PlateId == TrailerRigo2Id
            && z.Trailer3PlateId == TrailerRigo3Id
            && z.Trailer4PlateId == TrailerRigo4Id
            );
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .Where(z => string.IsNullOrEmpty(truckRego) || string.Equals(z.KeyVehicleLogProfile.VehicleRego, truckRego))
                .ToList();
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsUsingTrailerRego(
            string trailerRego1, string trailerRego2, string trailerRego3, string trailerRego4, int? trailerRego1Id, int? trailerRego2Id,
            int? trailerRego3Id, int? trailerRego4Id
            )
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .Where(z => z.KeyVehicleLogProfile.Trailer1Rego == trailerRego1
                  && (z.KeyVehicleLogProfile.Trailer2Rego == trailerRego2)
                  && (z.KeyVehicleLogProfile.Trailer3Rego == trailerRego3)
                  && (z.KeyVehicleLogProfile.Trailer4Rego == trailerRego4)
                  && z.KeyVehicleLogProfile.Trailer1PlateId == trailerRego1Id
                  && z.KeyVehicleLogProfile.Trailer2PlateId == trailerRego2Id
                  && z.KeyVehicleLogProfile.Trailer3PlateId == trailerRego3Id
                  && z.KeyVehicleLogProfile.Trailer4PlateId == trailerRego4Id
                )
                .ToList();
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetails(string truckRego, string personName)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Where(z => string.Equals(z.KeyVehicleLogProfile.VehicleRego, truckRego) && string.Equals(z.PersonName, personName))
                .ToList();
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetPOIListFromVisitorPersonalDetails()
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                 .Include(z => z.KeyVehicleLogProfile)
                .Where(z => z.PersonOfInterest != null).ToList();
        }

        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsWithIndividualType(int individualtype)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .Where(z => z.PersonType == individualtype)
                .ToList();
        }
        public int SaveKeyVehicleLogProfileWithPersonalDetail(KeyVehicleLogVisitorPersonalDetail kvlVisitorPersonalDetail)
        {
            kvlVisitorPersonalDetail.ProfileId = SaveKeyVehicleLogProfile(kvlVisitorPersonalDetail.KeyVehicleLogProfile);
            SaveKeyVehicleLogVisitorPersonalDetail(kvlVisitorPersonalDetail);
            return kvlVisitorPersonalDetail.ProfileId;
        }

        public int SaveKeyVehicleLogProfileWithPersonalDetailForTrailer(KeyVehicleLogVisitorPersonalDetail kvlVisitorPersonalDetail)
        {
            kvlVisitorPersonalDetail.ProfileId = SaveKeyVehicleLogProfileForTrailer(kvlVisitorPersonalDetail.KeyVehicleLogProfile);
            SaveKeyVehicleLogVisitorPersonalDetail(kvlVisitorPersonalDetail);
            return kvlVisitorPersonalDetail.ProfileId;
        }


        public int SaveKeyVehicleLogVisitorPersonalDetail(KeyVehicleLogVisitorPersonalDetail keyVehicleLogVisitorPersonalDetail)
        {
            var kvlPersonalDetailsToDb = _context.KeyVehicleLogVisitorPersonalDetails
                                            .SingleOrDefault(z => z.Id == keyVehicleLogVisitorPersonalDetail.Id) ??
                                            new KeyVehicleLogVisitorPersonalDetail();

            kvlPersonalDetailsToDb.ProfileId = keyVehicleLogVisitorPersonalDetail.ProfileId;
            kvlPersonalDetailsToDb.CompanyName = keyVehicleLogVisitorPersonalDetail.CompanyName;
            kvlPersonalDetailsToDb.PersonName = keyVehicleLogVisitorPersonalDetail.PersonName;
            kvlPersonalDetailsToDb.PersonType = keyVehicleLogVisitorPersonalDetail.PersonType;
            kvlPersonalDetailsToDb.PersonOfInterest = keyVehicleLogVisitorPersonalDetail.PersonOfInterest;
            if (keyVehicleLogVisitorPersonalDetail.PersonOfInterest != null || keyVehicleLogVisitorPersonalDetail.POIId != null)
            {
                string imagepath = "~/images/ziren.png";
                kvlPersonalDetailsToDb.POIImage = keyVehicleLogVisitorPersonalDetail.POIImage;
            }
            kvlPersonalDetailsToDb.IsBDM = keyVehicleLogVisitorPersonalDetail.IsBDM;
            if (keyVehicleLogVisitorPersonalDetail.CRMId != null)
            {
                kvlPersonalDetailsToDb.CRMId = keyVehicleLogVisitorPersonalDetail.CRMId;
                kvlPersonalDetailsToDb.IndividualTitle = keyVehicleLogVisitorPersonalDetail.IndividualTitle;
                kvlPersonalDetailsToDb.Gender = keyVehicleLogVisitorPersonalDetail.Gender;
                kvlPersonalDetailsToDb.CompanyABN = keyVehicleLogVisitorPersonalDetail.CompanyABN;
                kvlPersonalDetailsToDb.CompanyLandline = keyVehicleLogVisitorPersonalDetail.CompanyLandline;
                kvlPersonalDetailsToDb.Email = keyVehicleLogVisitorPersonalDetail.Email;
                kvlPersonalDetailsToDb.Website = keyVehicleLogVisitorPersonalDetail.Website;
                kvlPersonalDetailsToDb.BDMList = keyVehicleLogVisitorPersonalDetail.BDMList;


            }
            kvlPersonalDetailsToDb.POIId = keyVehicleLogVisitorPersonalDetail.POIId;
            if (kvlPersonalDetailsToDb.Id == 0)
            {
                _context.KeyVehicleLogVisitorPersonalDetails.Add(kvlPersonalDetailsToDb);
            }

            _context.SaveChanges();

            return kvlPersonalDetailsToDb.Id;
        }

        public void SaveKeyVehicleLogProfileNotes(string truckRego, string notes)
        {
            var profileDetailsInDb = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(z => z.VehicleRego == truckRego);
            if (profileDetailsInDb != null && !string.Equals(profileDetailsInDb.Notes, notes))
            {
                profileDetailsInDb.Notes = notes;
                _context.SaveChanges();
            }
        }

        public void SaveKeyVehicleLogProfileNotesByTrailerRiog(string Trailer1Rego, string Trailer2Rego, string Trailer3Rego,
            string Trailer4Rego, int? Trailer1PlateId, int? Trailer2PlateId, int? Trailer3PlateId, int? Trailer4PlateId, string notes)
        {
            var profileDetailsInDb = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(z => z.Trailer1Rego == Trailer1Rego
            && z.Trailer2Rego == Trailer2Rego && z.Trailer3Rego == Trailer3Rego && z.Trailer4Rego == Trailer4Rego
            && z.Trailer1PlateId == Trailer1PlateId && z.Trailer2PlateId == Trailer2PlateId && z.Trailer3PlateId == Trailer3PlateId
            && z.Trailer4PlateId == Trailer4PlateId
            );
            if (profileDetailsInDb != null && !string.Equals(profileDetailsInDb.Notes, notes))
            {
                profileDetailsInDb.Notes = notes;
                _context.SaveChanges();
            }
        }

        public void DeleteKeyVehicleLogPersonalDetails(int id)
        {
            var kvlPersonalDetailsToDelete = _context.KeyVehicleLogVisitorPersonalDetails.SingleOrDefault(x => x.Id == id);
            if (kvlPersonalDetailsToDelete != null)
            {
                _context.KeyVehicleLogVisitorPersonalDetails.Remove(kvlPersonalDetailsToDelete);

                var personalDetailsCount = _context.KeyVehicleLogVisitorPersonalDetails.Count(x => x.ProfileId == kvlPersonalDetailsToDelete.ProfileId);
                if (personalDetailsCount == 1)
                {
                    var kvlProfileToDelete = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(x => x.Id == kvlPersonalDetailsToDelete.ProfileId);
                    if (kvlProfileToDelete != null)
                    {
                        _context.KeyVehicleLogVisitorProfiles.Remove(kvlProfileToDelete);
                    }
                }

                _context.SaveChanges();
            }
        }

        public List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false)
        {
            return _context.KeyVehcileLogFields
                .Where(x => includeDeleted || !x.IsDeleted)
                .OrderBy(x => x.TypeId)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public List<TrailerDeatilsViewModel> GetKeyVehicleLogProfileDetails(string pattern)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@pattern";
            param1.SqlDbType = SqlDbType.VarChar;
            param1.SqlValue = pattern;
            return _context.TrailerDeatilsViewModel.FromSqlRaw($"EXEC sp_GetTrailerDetailsUsingSearchQuery @pattern", param1).ToList();
        }

        public List<KeyVehcileLogField> GetKeyVehicleLogFieldsByType(KvlFieldType type)
        {
            return GetKeyVehicleLogFields()
                .Where(x => x.TypeId == type)
                .OrderBy(x => x.Name)
                .ToList();
        }

        public void SaveKeyVehicleLogField(KeyVehcileLogField keyVehcileLogField)
        {
            if (keyVehcileLogField.Id == -1)
            {
                keyVehcileLogField.Id = 0;
                _context.KeyVehcileLogFields.Add(keyVehcileLogField);
            }
            else
            {
                var kvlFieldToUpdate = _context.KeyVehcileLogFields.SingleOrDefault(x => x.Id == keyVehcileLogField.Id);
                if (kvlFieldToUpdate != null)
                {
                    kvlFieldToUpdate.Name = keyVehcileLogField.Name;
                    kvlFieldToUpdate.TypeId = keyVehcileLogField.TypeId;
                    kvlFieldToUpdate.IsDeleted = keyVehcileLogField.IsDeleted;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteKeyVehicleLogField(int id)
        {
            var kvlFieldToDelete = _context.KeyVehcileLogFields.SingleOrDefault(x => x.Id == id);
            if (kvlFieldToDelete != null)
                kvlFieldToDelete.IsDeleted = true;
            _context.SaveChanges();
        }

        public List<KeyVehicleLogAuditHistory> GetAuditHistory(int id)
        {
            return _context.KeyVehicleLogAuditHistory
                .Where(z => z.ProfileId == id)
                .Include(z => z.GuardLogin)
                .ThenInclude(z => z.Guard)
                .ToList();
        }

        public void SaveKeyVehicleLogAuditHistory(KeyVehicleLogAuditHistory keyVehicleLogAuditHistory)
        {
            if (keyVehicleLogAuditHistory != null)
            {
                _context.KeyVehicleLogAuditHistory.Add(keyVehicleLogAuditHistory);
                _context.SaveChanges();
            }
        }

        private int SaveKeyVehicleLogProfile(KeyVehicleLogProfile keyVehicleLogProfile)
        {
            var kvlProfileToDb = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(z => z.VehicleRego == keyVehicleLogProfile.VehicleRego) ?? new KeyVehicleLogProfile();
            kvlProfileToDb.VehicleRego = keyVehicleLogProfile.VehicleRego;
            kvlProfileToDb.Trailer1Rego = keyVehicleLogProfile.Trailer1Rego;
            kvlProfileToDb.Trailer2Rego = keyVehicleLogProfile.Trailer2Rego;
            kvlProfileToDb.Trailer3Rego = keyVehicleLogProfile.Trailer3Rego;
            kvlProfileToDb.Trailer4Rego = keyVehicleLogProfile.Trailer4Rego;
            kvlProfileToDb.TruckConfig = keyVehicleLogProfile.TruckConfig;
            kvlProfileToDb.TrailerType = keyVehicleLogProfile.TrailerType;
            kvlProfileToDb.MaxWeight = keyVehicleLogProfile.MaxWeight;
            kvlProfileToDb.MobileNumber = keyVehicleLogProfile.MobileNumber;
            kvlProfileToDb.Product = keyVehicleLogProfile.Product;
            kvlProfileToDb.EntryReason = keyVehicleLogProfile.EntryReason;
            kvlProfileToDb.CreatedLogId = keyVehicleLogProfile.CreatedLogId;
            kvlProfileToDb.PlateId = keyVehicleLogProfile.PlateId;
            kvlProfileToDb.Sender = keyVehicleLogProfile.Sender;
            kvlProfileToDb.IsSender = keyVehicleLogProfile.IsSender;
            kvlProfileToDb.Notes = keyVehicleLogProfile.Notes;
            kvlProfileToDb.Trailer1PlateId = keyVehicleLogProfile.Trailer1PlateId;
            kvlProfileToDb.Trailer2PlateId = keyVehicleLogProfile.Trailer2PlateId;
            kvlProfileToDb.Trailer3PlateId = keyVehicleLogProfile.Trailer3PlateId;
            kvlProfileToDb.Trailer4PlateId = keyVehicleLogProfile.Trailer3PlateId;

            if (kvlProfileToDb.Id == 0)
            {
                _context.KeyVehicleLogVisitorProfiles.Add(kvlProfileToDb);
            }

            _context.SaveChanges();

            return kvlProfileToDb.Id;
        }

        private int SaveKeyVehicleLogProfileForTrailer(KeyVehicleLogProfile keyVehicleLogProfile)
        {
            var kvlProfileToDb = _context.KeyVehicleLogVisitorProfiles.SingleOrDefault(z => (z.Trailer1Rego == keyVehicleLogProfile.Trailer1Rego)
            && (z.Trailer2Rego == keyVehicleLogProfile.Trailer2Rego)
            && (z.Trailer3Rego == keyVehicleLogProfile.Trailer3Rego)
            && (z.Trailer4Rego == keyVehicleLogProfile.Trailer4Rego)
            && (z.Trailer1PlateId == keyVehicleLogProfile.Trailer1PlateId)
            && (z.Trailer2PlateId == keyVehicleLogProfile.Trailer2PlateId)
            && (z.Trailer3PlateId == keyVehicleLogProfile.Trailer3PlateId)
            && (z.Trailer4PlateId == keyVehicleLogProfile.Trailer4PlateId)
            ) ?? new KeyVehicleLogProfile();
            kvlProfileToDb.VehicleRego = keyVehicleLogProfile.VehicleRego;
            kvlProfileToDb.Trailer1Rego = keyVehicleLogProfile.Trailer1Rego;
            kvlProfileToDb.Trailer2Rego = keyVehicleLogProfile.Trailer2Rego;
            kvlProfileToDb.Trailer3Rego = keyVehicleLogProfile.Trailer3Rego;
            kvlProfileToDb.Trailer4Rego = keyVehicleLogProfile.Trailer4Rego;
            kvlProfileToDb.TruckConfig = keyVehicleLogProfile.TruckConfig;
            kvlProfileToDb.TrailerType = keyVehicleLogProfile.TrailerType;
            kvlProfileToDb.MaxWeight = keyVehicleLogProfile.MaxWeight;
            kvlProfileToDb.MobileNumber = keyVehicleLogProfile.MobileNumber;
            kvlProfileToDb.Product = keyVehicleLogProfile.Product;
            kvlProfileToDb.EntryReason = keyVehicleLogProfile.EntryReason;
            kvlProfileToDb.CreatedLogId = keyVehicleLogProfile.CreatedLogId;
            kvlProfileToDb.PlateId = keyVehicleLogProfile.PlateId;
            kvlProfileToDb.Sender = keyVehicleLogProfile.Sender;
            kvlProfileToDb.IsSender = keyVehicleLogProfile.IsSender;
            kvlProfileToDb.Notes = keyVehicleLogProfile.Notes;
            kvlProfileToDb.Trailer1PlateId = keyVehicleLogProfile.Trailer1PlateId;
            kvlProfileToDb.Trailer2PlateId = keyVehicleLogProfile.Trailer2PlateId;
            kvlProfileToDb.Trailer3PlateId = keyVehicleLogProfile.Trailer3PlateId;
            kvlProfileToDb.Trailer4PlateId = keyVehicleLogProfile.Trailer4PlateId;

            if (kvlProfileToDb.Id == 0)
            {
                _context.KeyVehicleLogVisitorProfiles.Add(kvlProfileToDb);
            }

            _context.SaveChanges();

            return kvlProfileToDb.Id;
        }
        public List<CompanyDetails> GetCompanyDetails()
        {
            return _context.CompanyDetails.ToList();
        }

        //To Update keyvehiclelog
        public void EditRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {
                if (clientSiteActivity.Id == 0)
                {

                    _context.ClientSiteRadioChecksActivityStatus.Add(new ClientSiteRadioChecksActivityStatus()
                    {
                        ClientSiteId = clientSiteActivity.ClientSiteId,
                        GuardId = clientSiteActivity.GuardId,
                        LastIRCreatedTime = clientSiteActivity.LastIRCreatedTime,
                        LastKVCreatedTime = clientSiteActivity.LastKVCreatedTime,
                        LastLBCreatedTime = clientSiteActivity.LastLBCreatedTime,
                        GuardLoginTime = clientSiteActivity.GuardLoginTime,
                        GuardLogoutTime = clientSiteActivity.GuardLogoutTime,
                        IRId = clientSiteActivity.IRId,
                        KVId = clientSiteActivity.KVId,
                        LBId = clientSiteActivity.LBId,
                        ActivityType = clientSiteActivity.ActivityType,
                        OnDuty = clientSiteActivity.OnDuty,
                        OffDuty = clientSiteActivity.OffDuty,
                        ActivityDescription =  clientSiteActivity.ActivityDescription!=string.Empty? clientSiteActivity.ActivityDescription: "Edited"
                    });

                }


                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public void SaveRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {
                if (clientSiteActivity.Id == 0)
                {

                    _context.ClientSiteRadioChecksActivityStatus.Add(new ClientSiteRadioChecksActivityStatus()
                    {
                        ClientSiteId = clientSiteActivity.ClientSiteId,
                        GuardId = clientSiteActivity.GuardId,
                        LastIRCreatedTime = clientSiteActivity.LastIRCreatedTime,
                        LastKVCreatedTime = clientSiteActivity.LastKVCreatedTime,
                        LastLBCreatedTime = clientSiteActivity.LastLBCreatedTime,
                        GuardLoginTime = clientSiteActivity.GuardLoginTime,
                        GuardLogoutTime = clientSiteActivity.GuardLogoutTime,
                        IRId = clientSiteActivity.IRId,
                        KVId = clientSiteActivity.KVId,
                        LBId = clientSiteActivity.LBId,
                        ActivityType = clientSiteActivity.ActivityType,
                        ActivityDescription= clientSiteActivity.ActivityDescription,
                        OnDuty = clientSiteActivity.OnDuty,
                        OffDuty = clientSiteActivity.OffDuty,
                        GuardLoginTimeLocal = clientSiteActivity.GuardLoginTimeLocal,
                        GuardLoginTimeLocalWithOffset = clientSiteActivity.GuardLoginTimeLocalWithOffset,
                        GuardLoginTimeZone = clientSiteActivity.GuardLoginTimeZone,
                        GuardLoginTimeZoneShort = clientSiteActivity.GuardLoginTimeZoneShort,
                        GuardLoginTimeUtcOffsetMinute = clientSiteActivity.GuardLoginTimeUtcOffsetMinute
                    });

                }
                else
                {

                    var clientSiteActivityToUpdate = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == clientSiteActivity.Id);
                    if (clientSiteActivityToUpdate == null)
                        throw new InvalidOperationException();

                    clientSiteActivityToUpdate.ClientSiteId = clientSiteActivity.ClientSiteId;
                    clientSiteActivityToUpdate.GuardId = clientSiteActivity.GuardId;
                    clientSiteActivityToUpdate.LastIRCreatedTime = clientSiteActivity.LastIRCreatedTime;
                    clientSiteActivityToUpdate.LastKVCreatedTime = clientSiteActivity.LastKVCreatedTime;
                    clientSiteActivityToUpdate.LastLBCreatedTime = clientSiteActivity.LastLBCreatedTime;
                    clientSiteActivityToUpdate.GuardLoginTime = clientSiteActivity.GuardLoginTime;
                    clientSiteActivityToUpdate.GuardLogoutTime = clientSiteActivity.GuardLogoutTime;
                    clientSiteActivityToUpdate.IRId = clientSiteActivity.IRId;
                    clientSiteActivityToUpdate.KVId = clientSiteActivity.KVId;
                    clientSiteActivityToUpdate.LBId = clientSiteActivity.LBId;
                    clientSiteActivityToUpdate.ActivityType = clientSiteActivity.ActivityType;
                    clientSiteActivityToUpdate.ActivityDescription = clientSiteActivity.ActivityDescription;
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public List<ClientSiteRadioChecksActivityStatus> GetClientSiteRadioChecksActivityDetails()
        {
            return _context.ClientSiteRadioChecksActivityStatus.ToList();
        }
        public List<RadioCheckListGuardData> GetActiveGuardDetails()
        {

            var allvalues = _context.RadioCheckListGuardData.FromSqlRaw($"EXEC sp_GetActiveGuardDetailsForRC").ToList();
            List<ClientSiteSmartWand> allphoneNumbers = _context.ClientSiteSmartWands.ToList();
            foreach (var item in allvalues)
            {
                var phoneNumbers = allphoneNumbers
               .Where(x => x.ClientSiteId == item.ClientSiteId)
               .Select(x => x.PhoneNumber)
               .ToList();
                var phoneNumbersString = string.Join("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp", phoneNumbers);
                if (phoneNumbers.Count != 0)
                {
                    item.hasmartwand = 1;

                }

                item.SiteName = item.SiteName + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class=\"fa fa-mobile\" aria-hidden=\"true\"></i> " + string.Join(",&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp", _context.ClientSiteSmartWands.Where(x => x.ClientSiteId == item.ClientSiteId).Select(x => x.PhoneNumber).ToList()) + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp<span class=\"icon-satellite-3 satellite-3-fontsize\" aria-hidden=\"true\" id=\"btnUpArrow\"></span> ";
                item.Address = " <a id=\"btnActiveGuardsMap\" href=\"https://www.google.com/maps?q=" + item.GPS + "\"target=\"_blank\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> </a>" + item.Address + " <input type=\"hidden\" class=\"form-control\" value=\"" + item.GPS + "\" id=\"txtGPSActiveguards\" />";
            }
            return allvalues;
        }

        public List<RadioCheckListInActiveGuardData> GetInActiveGuardDetails()
        {

            var allvalues = _context.RadioCheckListInActiveGuardData.FromSqlRaw($"EXEC sp_GetInActiveGuardDetailsForRC").ToList();
            List<ClientSiteSmartWand> allphoneNumbers = _context.ClientSiteSmartWands.ToList();
            foreach (var item in allvalues)
            {
                var phoneNumbers = allphoneNumbers
                .Where(x => x.ClientSiteId == item.ClientSiteId)
                .Select(x => x.PhoneNumber)
                .ToList();
                var phoneNumbersString = string.Join("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp", phoneNumbers);

                item.SiteName = item.SiteName + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class=\"fa fa-mobile\" aria-hidden=\"true\"></i> " + phoneNumbersString + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp<span class=\"icon-satellite-3 satellite-3-fontsize\" aria-hidden=\"true\" id=\"btnUpArrow\"></span>";



                item.Address = " <a id=\"btnActiveGuardsMap\" href=\"https://www.google.com/maps?q=" + item.GPS + "\"target=\"_blank\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> </a>" + item.Address + " <input type=\"hidden\" class=\"form-control\" value=\"" + item.GPS + "\" id=\"txtGPSActiveguards\" />";
            }
            return allvalues;
        }


        //logBookId delete for radio checklist-start
        public void DeleteClientSiteRadioCheckActivityStatusForLogBookEntry(int id)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.LBId == id);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }


            _context.SaveChanges();


            var clientSiteRadioCheckActivityStatusToDelete_History = _context.ClientSiteRadioChecksActivityStatus_History.Where(x => x.LBId == id);
            if (clientSiteRadioCheckActivityStatusToDelete_History == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete_History)
            {
                _context.Remove(item);
            }


            _context.SaveChanges();
        }
        public void SignOffClientSiteRadioCheckActivityStatusForLogBookEntry(int GuardId, int ClientSiteId)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardId == GuardId && x.ClientSiteId == ClientSiteId);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }



            _context.SaveChanges();

        }
        /* Find all the Activity of the user */
        public bool getIfAnyActivityInbufferTime(int GuardId, int ClientSiteId)
        {
            bool status = false;
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardId == GuardId && x.ClientSiteId == ClientSiteId && x.GuardLoginTime == null).ToList();

            if (clientSiteRadioCheckActivityStatusToDelete.Count > 0)
            {
                foreach (var activity in clientSiteRadioCheckActivityStatusToDelete)
                {
                    if (activity.LastIRCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastIRCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                    if (activity.LastKVCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastKVCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                    if (activity.LastLBCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastLBCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                    if (activity.LastSWCreatedTime != null)
                    {
                        if ((DateTime.Now - activity.LastSWCreatedTime).Value.TotalMinutes < 90)
                        {
                            status = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                return status = false;
            }

            return status;

        }
        //logBookId delete for radio checklist-end

        public void DeleteClientSiteRadioChecksActivity(ClientSiteRadioChecksActivityStatus ClientSiteRadioChecksActivityStatus)
        {
            var ClientSiteRadioChecksActivity = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == ClientSiteRadioChecksActivityStatus.Id);
            if (ClientSiteRadioChecksActivity != null)
            {
                /*var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == ClientSiteRadioChecksActivity.GuardId && x.ClientSiteId == ClientSiteRadioChecksActivity.ClientSiteId);
                /* remove the Pervious Status*/
                /*if (clientSiteRcStatus != null)
                  /*  _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);*/

                _context.ClientSiteRadioChecksActivityStatus.Remove(ClientSiteRadioChecksActivity);

            }
            _context.SaveChanges();

        }

        //for getting logbookdetails of the guard-start
        public List<RadioCheckListGuardLoginData> GetActiveGuardlogBookDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardLoginData.FromSqlRaw($"EXEC sp_GetActiveGuardLogBookDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting logbookdetails of the guard-end

        //for getting logbook history of the guard-start
        public List<GuardLog> GetActiveGuardlogBookHistory(int clientSiteId, int guardId)
        {
            List<GuardLog> gl = new List<GuardLog>();
            if (clientSiteId == 0 || guardId == 0)
            {
                return gl;
            }
            var logins = _context.GuardLogins.Where(x => x.GuardId == guardId) // && x.ClientSiteId == clientSiteId
                .Include(y => y.ClientSiteLogBook).Where(t => t.ClientSiteLogBook.Type == LogBookType.DailyGuardLog)
                .OrderByDescending(d => d.LoginDate)
                .Take(1).FirstOrDefault();
            if (logins == null)
            {
                return gl;
            }

            var guardhistory = _context.GuardLogs.Where(x => x.GuardLoginId == logins.Id && x.ClientSiteLogBookId == logins.ClientSiteLogBookId)
                .OrderByDescending(x => x.EventDateTime)
                .Take(1).ToList();

            return guardhistory;
        }
        //for getting logbook history of the guard-end

        //for getting Incident Report history of the guard-start
        public List<IncidentReport> GetActiveGuardIncidentReportHistory(int clientSiteId, int guardId)
        {
            List<IncidentReport> irl = new List<IncidentReport>();
            if (clientSiteId == 0 || guardId == 0)
            {
                return irl;
            }

            var irh = _context.IncidentReports.Where(x => x.GuardId == guardId) // && x.ClientSiteId == clientSiteId
                .OrderByDescending(x => x.CreatedOn)
                .Take(1).ToList();
            return irh;
        }
        //for getting Incident Report history of the guard-end

        //for getting Key Vehicle history of the guard-start
        public List<KeyVehicleLog> GetActiveGuardKeyVehicleHistory(int clientSiteId, int guardId)
        {
            List<KeyVehicleLog> gl = new List<KeyVehicleLog>();
            if (clientSiteId == 0 || guardId == 0)
            {
                return gl;
            }
            var logins = _context.GuardLogins.Where(x => x.GuardId == guardId) // && x.ClientSiteId == clientSiteId
                .Include(y => y.ClientSiteLogBook).Where(t => t.ClientSiteLogBook.Type == LogBookType.VehicleAndKeyLog)
                .OrderByDescending(d => d.LoginDate)
                .Take(1).FirstOrDefault();
            if (logins == null)
            {
                return gl;
            }
            try
            {
                var guardhistory = _context.KeyVehicleLogs.Where(x => x.GuardLoginId == logins.Id && x.ClientSiteLogBookId == logins.ClientSiteLogBookId
                && x.EntryCreatedDateTimeLocal != null)
                .OrderByDescending(x => x.EntryCreatedDateTimeLocal)
                .Take(1).ToList();

                if (guardhistory.Count > 0)
                {
                    gl = guardhistory;
                    gl.ForEach(x =>
                    {
                        x.IndividualTitle = "KV Log";
                        x.RubbishDeduction = true;
                    });
                }
                else
                {
                    var guardloghistory = _context.GuardLogs.Where(x => x.GuardLoginId == logins.Id && x.ClientSiteLogBookId == logins.ClientSiteLogBookId)
                        .OrderByDescending(x => x.EventDateTime)
                        .Take(1).ToList();
                    if (guardloghistory.Count > 0)
                    {
                        KeyVehicleLog glh = new KeyVehicleLog();
                        glh.Id = guardloghistory.First().Id;
                        glh.IndividualTitle = guardloghistory.First().Notes;
                        glh.RubbishDeduction = false;
                        glh.EntryCreatedDateTimeLocal = guardloghistory.First().EventDateTimeLocal;
                        glh.EntryCreatedDateTimeZoneShort = guardloghistory.First().EventDateTimeZoneShort;
                        gl.Add(glh);
                    }

                }

            }
            catch (Exception)
            {
                // throw;
            }


            return gl;
        }
        //for getting Key Vehicle history of the guard-end


        //for getting SmartWand history of the guard-start
        public List<SmartWandScanGuardHistory> GetActiveGuardSwHistory(int clientSiteId, int guardId)
        {
            List<SmartWandScanGuardHistory> swl = new List<SmartWandScanGuardHistory>();
            if (clientSiteId == 0 || guardId == 0)
            {
                return swl;
            }

            var swh = _context.SmartWandScanGuardHistory.Where(x => x.GuardId == guardId) // && x.ClientSiteId == clientSiteId
                .OrderByDescending(x => x.InspectionStartDatetimeLocal)
                .Take(1).ToList();
            return swh;
        }
        //for getting SmartWand history of the guard-end

        //for getting the details of guards not available-start
        public List<RadioCheckListNotAvailableGuardData> GetNotAvailableGuardDetails()
        {

            var allvalues = _context.RadioCheckListNotAvailableGuardData.FromSqlRaw($"EXEC sp_GetNotAvailableGuardDetailsForRC").ToList();
            foreach (var item in allvalues)
            {
                var phoneNumbers = _context.ClientSiteSmartWands
               .Where(x => x.ClientSiteId == item.ClientSiteId)
               .Select(x => x.PhoneNumber)
               .ToList();
                var phoneNumbersString = string.Join("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp", phoneNumbers);

                item.SiteName = item.SiteName + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp <i class=\"fa fa-mobile\" aria-hidden=\"true\"></i> " + string.Join(",&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp", _context.ClientSiteSmartWands.Where(x => x.ClientSiteId == item.ClientSiteId).Select(x => x.PhoneNumber).ToList()) + " <i class=\"fa fa-caret-down\" aria-hidden=\"true\" id=\"btnUpArrow\"></i> ";
                item.Address = " <a id=\"btnActiveGuardsMap\" href=\"https://www.google.com/maps?q=" + item.GPS + "\"target=\"_blank\"><i class=\"fa fa-map-marker\" aria-hidden=\"true\"></i> </a>" + item.Address + " <input type=\"hidden\" class=\"form-control\" value=\"" + item.GPS + "\" id=\"txtGPSActiveguards\" />";
            }
            return allvalues;
        }
        //for getting the details of guards not available-end//for getting key vehicle log details of the  guard-start

        public List<RadioCheckListGuardKeyVehicleData> GetActiveGuardKeyVehicleLogDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardKeyVehicleData.FromSqlRaw($"EXEC sp_GetActiveGuardKeyVehicleDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting  key vehicle log details of the  guard-end

        //for getting incident report details of the  guard-start

        public List<RadioCheckListGuardIncidentReportData> GetActiveGuardIncidentReportDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardIncidentReportData.FromSqlRaw($"EXEC sp_GetActiveGuardIncidentReportsDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting  incident report details of the  guard-end

        //for getting SW details of the  guard-start

        public List<RadioCheckListSWReadData> GetActiveGuardSWDetails(int clientSiteId, int guardId)
        {
            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListSWReadData.FromSqlRaw($"EXEC sp_GetActiveGuardSWDetailsForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }
        //for getting  SW details of the  guard-end
        public Guard GetGuards(int guardId)
        {

            return _context.Guards.Where(x => x.Id == guardId).FirstOrDefault();
        }

        public KeyVehicleLog GetCompanyDetailsVehLog(string companyName)
        {
            if (companyName == null)
            {
                // Handle the case where companyName is null
                return null;
            }

            return _context.KeyVehicleLogs.FirstOrDefault(x => x.CompanyName == companyName);

        }
        public void DeleteClientSiteRadioCheckActivityStatusForKeyVehicleEntry(int id)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.KVId == id);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }

            _context.SaveChanges();


            var clientSiteRadioCheckActivityStatusToDelete_History = _context.ClientSiteRadioChecksActivityStatus_History.Where(x => x.KVId == id);
            if (clientSiteRadioCheckActivityStatusToDelete_History == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete_History)
            {
                _context.Remove(item);
            }


            _context.SaveChanges();
        }
        public int GetClientSiteLogBookId(int clientsiteId, LogBookType type, DateTime date)
        {
            return _context.ClientSiteLogBooks
                 .SingleOrDefault(z => z.ClientSiteId == clientsiteId && z.Type == type && z.Date == date).Id;
        }

        // p6#73 timezone bug - Added by binoy 24-01-2024
        public int GetClientSiteLogBookIdByLogBookMaxID(int clientsiteId, LogBookType type, out DateTime logbookDate)
        {
            int lbid = _context.ClientSiteLogBooks.Where(z => z.ClientSiteId == clientsiteId && z.Type == type).Select(x => x.Id).Max();
            logbookDate = _context.ClientSiteLogBooks.Where(z => z.Id == lbid && z.Type == type).Select(x => x.Date).FirstOrDefault();
            return lbid;
        }

        public void SaveClientSiteRadioCheck(ClientSiteRadioCheck clientSiteRadioCheck)
        {

            try
            {

                var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
                /* remove the Pervious Status*/
                if (clientSiteRcStatus != null)
                {
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);


                    if (clientSiteRadioCheck.Status == "Off Duty (RC automatic logoff)")
                    {
                        /* Check if Manning type notfication */
                        var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                        if (checkIfTypeOneManning.Count == 0)
                        {

                            // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- Start -- added by Binoy - 02-02-2024
                            // To Log the entry to the last logbook id of the client.
                            var logbookdate = DateTime.Today;
                            var logbooktype = LogBookType.DailyGuardLog;
                            var logBookId = GetClientSiteLogBookIdByLogBookMaxID(clientSiteRadioCheck.ClientSiteId, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                            var logbook = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookId);

                            var tznm = TimeZoneHelper.GetCurrentTimeZoneShortName();
                            var tzshrtnm = TimeZoneHelper.GetCurrentTimeZoneShortName();
                            var tzoffmin = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute();
                            var tztm = TimeZoneHelper.GetCurrentTimeZoneCurrentTime();
                            var tztmwithoffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset();

                            // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- End -- added by Binoy - 02-02-2024


                            //   var logbook = _context.ClientSiteLogBooks
                            //.SingleOrDefault(z => z.ClientSiteId == clientSiteRadioCheck.ClientSiteId && z.Type == LogBookType.DailyGuardLog && z.Date == DateTime.Today);

                            //   int logBookId;
                            //   if (logbook == null)
                            //   {
                            //       var newLogBook = new ClientSiteLogBook()
                            //       {
                            //           ClientSiteId = clientSiteRadioCheck.ClientSiteId,
                            //           Type = LogBookType.DailyGuardLog,
                            //           Date = DateTime.Today
                            //       };

                            //       if (newLogBook.Id == 0)
                            //       {
                            //           _context.ClientSiteLogBooks.Add(newLogBook);
                            //       }
                            //       else
                            //       {
                            //           var logBookToUpdate = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == newLogBook.Id);
                            //           if (logBookToUpdate != null)
                            //           {
                            //               // nothing to update
                            //           }
                            //       }
                            //       _context.SaveChanges();
                            //       logBookId = newLogBook.Id;

                            //   }
                            //   else
                            //   {
                            //       logBookId = logbook.Id;
                            //   } 



                            // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- added by Binoy - 04-02-2024
                            // z.OnDuty.Date == DateTime.Today changed to z.OnDuty.Date == logbookdate.Date

                            //  var guardLoginId = _context.GuardLogins
                            // .SingleOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);

                            var guardLoginId = _context.GuardLogins
                            .SingleOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == logbookdate.Date);
                            if (guardLoginId != null)
                            {
                                var guardLog = new GuardLog()
                                {
                                    ClientSiteLogBookId = logBookId,
                                    GuardLoginId = guardLoginId.Id,
                                    EventDateTime = DateTime.Now,
                                    Notes = "Off Duty (RC automatic logoff)",
                                    IsSystemEntry = true,
                                    EventDateTimeLocal = tztm, // Task p6#73_TimeZone issue -- added by Binoy - Start
                                    EventDateTimeLocalWithOffset = tztmwithoffset,
                                    EventDateTimeZone = tznm,
                                    EventDateTimeZoneShort = tzshrtnm,
                                    EventDateTimeUtcOffsetMinute = tzoffmin // Task p6#73_TimeZone issue -- added by Binoy - End

                                };
                                SaveGuardLog(guardLog);
                                var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                if (guardLoginToUpdate != null)
                                {
                                    guardLoginToUpdate.OffDuty = DateTime.Now;
                                    _context.SaveChanges();
                                }

                            }
                            else
                            {
                                var latestRecord = _context.GuardLogins
                                .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                .OrderByDescending(r => r.Id)
                                 .FirstOrDefault();
                                if (latestRecord != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = latestRecord.Id,
                                        EventDateTime = DateTime.Now,
                                        Notes = "Off Duty (RC automatic logoff)",
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tztm, // Task p6#73_TimeZone issue -- added by Binoy - Start
                                        EventDateTimeLocalWithOffset = tztmwithoffset,
                                        EventDateTimeZone = tznm,
                                        EventDateTimeZoneShort = tzshrtnm,
                                        EventDateTimeUtcOffsetMinute = tzoffmin // Task p6#73_TimeZone issue -- added by Binoy - End

                                    };
                                    SaveGuardLog(guardLog);
                                    var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                    if (guardLoginToUpdate != null)
                                    {
                                        guardLoginToUpdate.OffDuty = DateTime.Now;
                                        _context.SaveChanges();
                                    }

                                }

                            }


                            //var signOffEntry = new GuardLog()
                            //{
                            //    ClientSiteLogBookId = clientSiteLogBookId,
                            //    GuardLoginId = guardLoginId,
                            //    EventDateTime = DateTime.Now,
                            //    Notes = "Guard Off Duty (Logbook Signout)",
                            //    IsSystemEntry = true
                            //};
                            //_guardLogDataProvider.SaveGuardLog(signOffEntry);
                            //_guardDataProvider.UpdateGuardOffDuty(guardLoginId, DateTime.Now);


                            var ClientSiteRadioChecksActivityDetails = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null);
                            foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                            {
                                ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                                UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);

                                var newstatu = new ClientSiteRadioCheck()
                                {
                                    ClientSiteId = ClientSiteRadioChecksActivity.ClientSiteId,
                                    GuardId = ClientSiteRadioChecksActivity.GuardId,
                                    Status = "Off Duty (RC automatic logoff)",
                                    CheckedAt = DateTime.Now,
                                    Active = true,
                                    RadioCheckStatusId = 1,
                                };
                                _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                                _context.ClientSiteRadioChecks.Add(newstatu);
                                _context.SaveChanges();
                                /* Update Radio check status logOff*/

                            }

                        }
                        else
                        {
                            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                            _context.SaveChanges();

                            /* Remove the Notification Row */
                            var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                            _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                            _context.SaveChanges();
                        }

                    }
                    else
                    {
                        _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                        _context.SaveChanges();


                    }
                }
            }
            catch (Exception ex)
            {


            }
        }





        public void SaveClientSiteRadioCheckStatusFromlogBook(ClientSiteRadioCheck clientSiteRadioCheck)
        {
            var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
            /* remove the Pervious Status*/
            if (clientSiteRcStatus != null)
            {
                var ClientSiteRadioChecksActivityDetails = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null);
                foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                {
                    ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                    UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);

                    var newstatu = new ClientSiteRadioCheck()
                    {
                        ClientSiteId = ClientSiteRadioChecksActivity.ClientSiteId,
                        GuardId = ClientSiteRadioChecksActivity.GuardId,
                        Status = "Off Duty",
                        RadioCheckStatusId = 1,
                        CheckedAt = DateTime.Now,
                        Active = true
                    };
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                    _context.ClientSiteRadioChecks.Add(newstatu);
                    _context.SaveChanges();
                    /* Update Radio check status logOff*/


                }
            }
        }


        public void SaveClientSiteRadioCheckStatusFromlogBookNewUpdate(ClientSiteRadioCheck clientSiteRadioCheck)
        {
            var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == clientSiteRadioCheck.GuardId &&
            x.ClientSiteId == clientSiteRadioCheck.ClientSiteId).ToList();
            /* remove the Pervious Status*/
            if (clientSiteRcStatus.Count != 0)
            {
                if (clientSiteRcStatus != null)
                {
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                    _context.SaveChanges();
                    _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                    _context.SaveChanges();
                    /* Update Radio check status logOff*/
                }
            }
            else
            {

                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                _context.SaveChanges();

            }
        }
        public int GetGuardLoginId(int clientsitelogbookId, int guardId, DateTime date)
        {
            return _context.GuardLogins
                 .SingleOrDefault(z => z.ClientSiteLogBookId == clientsitelogbookId && z.GuardId == guardId && z.OnDuty.Date == date.Date).Id;
        }
        public List<GuardLog> GetGuardLogsId(int logBookId, DateTime logDate, int guardLoginId, IrEntryType type, string notes)
        {
            return _context.GuardLogs
               .Where(z => z.ClientSiteLogBookId == logBookId && z.EventDateTime >= logDate && z.EventDateTime < logDate.AddDays(1)
               && z.GuardLoginId == guardLoginId && z.IrEntryType == type && z.Notes == notes).ToList();


        }
        public void UpdateRadioChecklistEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {


                var clientSiteActivityToUpdate = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == clientSiteActivity.Id);
                if (clientSiteActivityToUpdate == null)
                    throw new InvalidOperationException();
                clientSiteActivityToUpdate.NotificationCreatedTime = clientSiteActivity.NotificationCreatedTime;




                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public void UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivityStatus clientSiteActivity)
        {
            try
            {


                var clientSiteActivityToUpdate = _context.ClientSiteRadioChecksActivityStatus.SingleOrDefault(x => x.Id == clientSiteActivity.Id);
                if (clientSiteActivityToUpdate == null)
                    throw new InvalidOperationException();
                clientSiteActivityToUpdate.GuardLogoutTime = clientSiteActivity.GuardLogoutTime;
                _context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }
        public List<GuardLogin> GetGuardLogins(int guardLoginId)
        {
            return _context.GuardLogins.Where(z => z.Id == guardLoginId).ToList();
        }

        /* New Change by dileep for P4 task 17 Start */
        public void GetGuardManningDetails(DayOfWeek currentDay)
        {
            try
            {
                /*remove all the manning notification Start for showing today's manning*/

                var notificationDetailsAll = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardLoginTime != null && x.NotificationType == 1);
                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(notificationDetailsAll);
                _context.SaveChanges();

                /* remove all the manning notification end */

                /* get the manning details corresponding to the currentDay*/
                /* type 2 for avoid petrol car*/
                /*IsPHO check if its a public holyday */
                /*ScheduleisActive activate for particular  Site*/
                var clientSiteManningKpiSettings = _context.ClientSiteManningKpiSettings.Include(x => x.ClientSiteKpiSetting).
                    Where(x => x.WeekDay == currentDay && x.Type == "2" && x.IsPHO != 1 && x.ClientSiteKpiSetting.ScheduleisActive == true).ToList();
                foreach (var manning in clientSiteManningKpiSettings)
                {
                    if (manning.EmpHoursStart != null && manning.EmpHoursEnd != null)
                    {
                        /* Check the number of logins */
                        var numberOfLogin = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == null).Count() == 0;
                        if (numberOfLogin)
                        {    /* No login found */
                            /* find the emp Hours  Start time -5 (ie show notification 5 min before the guard login in the site) */
                            var dateTime = DateTime.ParseExact(manning.EmpHoursStart, "H:mm", null, System.Globalization.DateTimeStyles.None).AddMinutes(-5);
                            var dateendTime = DateTime.ParseExact(manning.EmpHoursEnd, "H:mm", null, System.Globalization.DateTimeStyles.None).AddMinutes(1);
                            if (DateTime.Now >= dateTime && DateTime.Now <= dateendTime)
                            {
                                /* Check if anylogbook entery exits in that timing */
                                var checkSiteLogBook = _context.ClientSiteLogBooks.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.Date == DateTime.Now.Date).ToList();
                                bool iflogbookentryexist = false;
                                foreach (var log in checkSiteLogBook)
                                {
                                    var checklogbookEntryInSpecificTiming = _context.GuardLogs.Where(x => x.ClientSiteLogBookId == log.Id && x.EventType != (int)GuardLogEventType.NoGuardLogin && (x.EventDateTime >= dateTime && x.EventDateTime <= dateendTime)).ToList();
                                    if (checklogbookEntryInSpecificTiming.Count != 0)
                                    {
                                        iflogbookentryexist = true;
                                    }
                                }

                                if (!iflogbookentryexist)
                                {
                                    var radioChecklist = _context.ClientSiteRadioChecksActivityStatus.Where(z => z.GuardId == 4 && z.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && z.GuardLoginTime != null && z.NotificationType == 1)
                                      .ToList();
                                    if (radioChecklist.Count == 0)
                                    {
                                        /* Check if any off duty status checked for this row */
                                        var rcOffDutyStatus = _context.ClientSiteRadioChecks.Where(z => z.GuardId == 4 && z.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && z.CheckedAt.Date == DateTime.Today.Date && z.Status == "Off Duty")
                                      .ToList();
                                        if (rcOffDutyStatus.Count == 0)
                                        {
                                            if (!CheckIfAnyEntryexistInRadioCheckStatus(manning.ClientSiteKpiSetting.ClientSiteId))
                                            {
                                                var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                                                {
                                                    ClientSiteId = manning.ClientSiteKpiSetting.ClientSiteId,
                                                    GuardId = 4,/* temp Guard(bruno) Id because forgin key  is set*/
                                                    GuardLoginTime = DateTime.ParseExact(manning.EmpHoursStart, "H:mm", null, System.Globalization.DateTimeStyles.None),/* Expected Time for Login
                                                /* New Field Added for NotificationType only for manning notification*/
                                                    NotificationType = 1,
                                                    /* added for show the crm CrmSupplier deatils in the 'no guard on duty' */
                                                    CRMSupplier = manning.CrmSupplier
                                                };
                                                _context.ClientSiteRadioChecksActivityStatus.Add(clientsiteRadioCheck);
                                                _context.SaveChanges();

                                                CreateLogBookStampForNoGuard(manning.ClientSiteKpiSetting.ClientSiteId, dateTime, dateendTime);

                                            }

                                        }
                                    }
                                }
                            }

                        }
                        else
                        {
                            /* if login  found  remove the notification*/
                            var notificationCountIsZero = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).Count() == 0;
                            if (!notificationCountIsZero)
                            {
                                /* Remove notification because login found */
                                var notificationDetails = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1);
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(notificationDetails);
                                _context.SaveChanges();
                            }
                        }

                    }
                }


            }
            catch (Exception ex)
            {

            }
        }

        public void CreateLogBookStampForNoGuard(int ClientSiteID,DateTime dateTime, DateTime dateendTime)
        {
            /* Check if NoGuardLogin event type exists in the logbook for the date if not create entry */
            // Check if Logbook id exists for the date create new logbookid
            var logbookdate = DateTime.Today;
            var logbooktype = LogBookType.DailyGuardLog;
            //var logBookId = GetClientSiteLogBookIdByLogBookMaxID(ClientSiteID, logbooktype, out logbookdate);
            var logBookId =  _logbookDataService.GetNewOrExistingClientSiteLogBookId(ClientSiteID, logbooktype);
            var ClientSiteName = GetClientSites(ClientSiteID).FirstOrDefault().Name; 
            var checklogbookEntry = _context.GuardLogs.Where(x => x.ClientSiteLogBookId == logBookId && x.EventType == (int)GuardLogEventType.NoGuardLogin).ToList();
            var subject = "No Guard on Duty";
            if (checklogbookEntry.Count < 1 )
            {
                var guardLog = new GuardLog()
                {
                    ClientSiteLogBookId = logBookId,
                    EventDateTime = DateTime.Now,
                    Notes = subject,
                    EventType = (int)GuardLogEventType.NoGuardLogin,
                    IsSystemEntry = true,
                    EventDateTimeLocal = TimeZoneHelper.GetCurrentTimeZoneCurrentTime(),
                    EventDateTimeLocalWithOffset = TimeZoneHelper.GetCurrentTimeZoneCurrentTimeWithOffset(),
                    EventDateTimeZone = TimeZoneHelper.GetCurrentTimeZone(),
                    EventDateTimeZoneShort = TimeZoneHelper.GetCurrentTimeZoneShortName(),
                    EventDateTimeUtcOffsetMinute = TimeZoneHelper.GetCurrentTimeZoneOffsetMinute(),
                    PlayNotificationSound = false
                };
                SaveGuardLog(guardLog);

                LogBookEntryFromRcControlRoomMessages(0, 0, subject, ClientSiteName, IrEntryType.Alarm, 1, 0, guardLog);
            }            
        }



        /* in some time the no guard shows when guard is active in the two hour list
         * this function will check if any Activity Status in the radio status list 
         */
        public bool CheckIfAnyEntryexistInRadioCheckStatus(int ClientSiteId)
        {
            var numberofactiveRowExistintheRadioStatus =_context.ClientSiteRadioChecksActivityStatus.Where(x =>  x.ClientSiteId == ClientSiteId && x.GuardLoginTime == null).ToList();
            if(numberofactiveRowExistintheRadioStatus.Count!=0)
            {
                return true;

            }
            else
            {
                return false;
            }

        }


        public void GetGuardManningDetailsForPublicHolidays()
        {
            try
            {
                //Check today is a public Holiday

                var IftodayIsAPublicHolday = _context.BroadcastBannerCalendarEvents.Where(x => x.IsPublicHoliday == true && x.StartDate.Date == DateTime.Today.Date).ToList();


                if (IftodayIsAPublicHolday.Count != 0 && IftodayIsAPublicHolday != null)
                {
                    /* get the manning details for public holdday*/
                    /* type 2 for avoid petrol car*/
                    /*IsPHO check if its a public holyday */
                    /*ScheduleisActive activate for particular  Site*/
                    var clientSiteManningKpiSettings = _context.ClientSiteManningKpiSettings.Include(x => x.ClientSiteKpiSetting).
                        Where(x => x.Type == "2" && x.IsPHO == 1 && x.EmpHoursStart !=null && x.EmpHoursEnd!=null && x.ClientSiteKpiSetting.ScheduleisActive == true).ToList();
                    foreach (var manning in clientSiteManningKpiSettings)
                    {
                        if (manning.EmpHoursStart != null && manning.EmpHoursEnd != null)
                        {
                            /* Check the number of logins in Rc status */
                            var numberOfLogin = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == null).Count() == 0;
                            if (numberOfLogin)
                            {
                                /* No login found */
                                /* find the emp Hours  Start time -5 (ie show notification 5 min before the guard login in the site) */
                                var dateTime = DateTime.ParseExact(manning.EmpHoursStart, "H:mm", null, System.Globalization.DateTimeStyles.None).AddMinutes(-5);
                                var dateendTime = DateTime.ParseExact(manning.EmpHoursEnd, "H:mm", null, System.Globalization.DateTimeStyles.None).AddMinutes(1);
                                if (DateTime.Now >= dateTime && DateTime.Now <= dateendTime)
                                {
                                    /* Check if anylogbook entery exits in that timing */
                                    var checkSiteLogBook = _context.ClientSiteLogBooks.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.Date == DateTime.Now.Date).ToList();
                                    bool iflogbookentryexist = false;
                                    foreach (var log in checkSiteLogBook)
                                    {
                                        var checklogbookEntryInSpecificTiming = _context.GuardLogs.Where(x => x.ClientSiteLogBookId == log.Id && x.EventType != (int)GuardLogEventType.NoGuardLogin && (x.EventDateTime >= dateTime && x.EventDateTime <= dateendTime)).ToList();
                                        if (checklogbookEntryInSpecificTiming.Count != 0)
                                        {
                                            iflogbookentryexist = true;
                                        }
                                    }

                                    if (!iflogbookentryexist)
                                    {
                                        var radioChecklist = _context.ClientSiteRadioChecksActivityStatus.Where(z => z.GuardId == 4 && z.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && z.GuardLoginTime != null && z.NotificationType == 1)
                                          .ToList();
                                        if (radioChecklist.Count == 0)
                                        {
                                            /* Check if any off duty status checked for this row */
                                            var rcOffDutyStatus = _context.ClientSiteRadioChecks.Where(z => z.GuardId == 4 && z.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && z.CheckedAt.Date == DateTime.Today.Date && z.Status == "Off Duty")
                                          .ToList();
                                            if (rcOffDutyStatus.Count == 0)
                                            {
                                                if (!CheckIfAnyEntryexistInRadioCheckStatus(manning.ClientSiteKpiSetting.ClientSiteId))
                                                {
                                                    var clientsiteRadioCheck = new ClientSiteRadioChecksActivityStatus()
                                                    {
                                                        ClientSiteId = manning.ClientSiteKpiSetting.ClientSiteId,
                                                        GuardId = 4,/* temp Guard(bruno) Id because forgin key  is set*/
                                                        GuardLoginTime = DateTime.ParseExact(manning.EmpHoursStart, "H:mm", null, System.Globalization.DateTimeStyles.None),/* Expected Time for Login
                                                /* New Field Added for NotificationType only for manning notification*/
                                                        NotificationType = 1,
                                                        /* added for show the crm CrmSupplier deatils in the 'no guard on duty' */
                                                        CRMSupplier = manning.CrmSupplier
                                                    };
                                                    _context.ClientSiteRadioChecksActivityStatus.Add(clientsiteRadioCheck);
                                                    _context.SaveChanges();

                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            else
                            {
                                /* if login  found  remove the notification*/
                                var notificationCountIsZero = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).Count() == 0;
                                if (!notificationCountIsZero)
                                {
                                    /* Remove notification because login found */
                                    var notificationDetails = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == manning.ClientSiteKpiSetting.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1);
                                    _context.ClientSiteRadioChecksActivityStatus.RemoveRange(notificationDetails);
                                    _context.SaveChanges();
                                }
                            }

                        }

                    }
                }




            }
            catch (Exception ex)
            {

            }
        }



        public void RemoveGuardLoginFromdifferentSites()
        {

            /* this function is used to remove the guard login in diffrent sites
             only latest login details needed
             */
            /* find the gurads login in diffrent sites */
            var duplicates = _context.ClientSiteRadioChecksActivityStatus.
            Where(x => x.GuardLoginTime != null && x.NotificationType == null).
            GroupBy(p => new { p.GuardId })
            .Where(group => group.Count() > 1)
            .Select(g => new
            {
                GuardId = g.Key.GuardId,
                Count = g.Count()
            }).ToList();

            if (duplicates.Count != 0)
            {
                foreach (var li in duplicates)
                {
                    /* find the latest login not to remove */
                    var latestItemsNottoRemove = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardLoginTime != null && x.NotificationType == null && x.GuardId == li.GuardId).OrderByDescending(x => x.GuardLoginTime).FirstOrDefault();
                    if (latestItemsNottoRemove != null)
                    {
                        /* list to remove */
                        var listtoremove = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardLoginTime != null && x.NotificationType == null && x.GuardId == li.GuardId && x.ClientSiteId != latestItemsNottoRemove.ClientSiteId).ToList();

                        if (listtoremove.Count > 0)

                            foreach (var removeItems in listtoremove)
                            {
                                var activitesToRemove = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.GuardId == removeItems.GuardId && x.ClientSiteId == removeItems.ClientSiteId).FirstOrDefault();
                                _context.ClientSiteRadioChecksActivityStatus.Remove(activitesToRemove);
                                _context.SaveChanges();

                            }

                    }

                }
            }


        }
        public void RemoveTheeRadioChecksActivityWithNotifcationtypeOne(int ClientSiteId)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == ClientSiteId && x.NotificationType == 1).ToList();
            if (clientSiteRadioCheckActivityStatusToDelete.Count != 0)
            {
                _context.RemoveRange(clientSiteRadioCheckActivityStatusToDelete);
                _context.SaveChanges();
            }

        }

        public void RemoveClientSiteRadioChecksGreaterthanTwoHours()
        {
            var clientSiteRadioChecksToDelete = _context.ClientSiteRadioChecks.Where(x => 1 == 1).ToList();
            if (clientSiteRadioChecksToDelete == null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                foreach (var item in clientSiteRadioChecksToDelete)
                {


                    var isActive = (DateTime.Now - item.CheckedAt).TotalHours < 3;
                    if (!isActive)
                    {
                        /* check any active row exist */
                        var checkIfExistAnyActiveRow = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.ClientSiteId == item.ClientSiteId && x.GuardId == item.GuardId &&
                         (x.LastIRCreatedTime != null || x.LastKVCreatedTime != null || x.LastLBCreatedTime != null || x.LastSWCreatedTime != null)).ToList();

                        if (checkIfExistAnyActiveRow.Count == 0)
                        {
                            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecks.Where(x => x.Id == item.Id);
                            if (clientSiteRadioCheckActivityStatusToDelete == null)
                                throw new InvalidOperationException();
                            else

                            {
                                _context.RemoveRange(clientSiteRadioCheckActivityStatusToDelete);
                                _context.SaveChanges();
                            }

                        }


                    }
                }
            }
        }

        /* New Change by dileep for P4 task 17 end */




        //code added to save Duress radio check start
        public void SaveRadioCheckDuress(string UserID)
        {
            _context.RadioCheckDuress.Add(new RadioCheckDuress()
            {
                UserID = Convert.ToInt32(UserID),
                IsActive = true,
                CurrentDateTime = DateTime.Today
            });
            _context.SaveChanges();
        }
        public bool IsRadiocheckDuressEnabled(int UserID)
        {
            return _context.RadioCheckDuress
        .Where(z => z.UserID == UserID)
        .OrderByDescending(z => z.Id)
        .Select(z => z.IsActive)
        .LastOrDefault();
        }
        public int UserIDDuress(int UserID)
        {
            return _context.RadioCheckDuress
        .Where(z => z.UserID == UserID)
        .OrderByDescending(z => z.Id)
        .Select(z => z.UserID)
        .LastOrDefault();
        }


        //listing clientsites for radio check
        public List<ClientSite> GetClientSites(int? Id)
        {
            return _context.ClientSites
                .Where(x => !Id.HasValue || (Id.HasValue && x.Id == Id.Value)).ToList();

        }
        public List<ClientSiteSmartWand> GetClientSiteSmartWands(int? clientSiteId)
        {
            return _context.ClientSiteSmartWands
                .Where(x => (!clientSiteId.HasValue || (clientSiteId.HasValue && x.ClientSiteId == clientSiteId.Value)) && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .ToList();
        }
        public int GetGuardLoginId(int guardId, DateTime date)
        {
            return _context.GuardLogins
                 .Where(z => z.GuardId == guardId && z.OnDuty.Date == date.Date).Max(x => x.Id);
        }
        public List<GuardLogin> GetGuardLoginsByClientSiteId(int? clientsiteId, DateTime date)
        {
            var guarlogins = _context.GuardLogins.Where(z => (!clientsiteId.HasValue || z.ClientSiteId == clientsiteId) && z.OnDuty.Date == date.Date).ToList();

            foreach (var item in guarlogins)
            {
                item.Guard = GetGuards(item.GuardId);
            }
            return guarlogins;
        }
        //for active guards-start

        public void SaveClientSiteRadioCheckNew(ClientSiteRadioCheck clientSiteRadioCheck, GuardLog tmzdata, int controlroomGuardLoginId)
        {

            try
            {

                var clientSiteRcStatus = _context.ClientSiteRadioChecks.Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
                /* remove the Pervious Status*/
                if (clientSiteRcStatus != null)
                {
                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                    var colorId = _context.RadioCheckStatus.Where(x => x.Id == clientSiteRadioCheck.RadioCheckStatusId).FirstOrDefault().RadioCheckStatusColorId;

                    // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- Start -- added by Binoy - 02-02-2024
                    // To Log the entry to the last logbook id of the client.
                    var logbookdate = DateTime.Today;
                    var logbooktype = LogBookType.DailyGuardLog;
                    var logBookId = GetClientSiteLogBookIdByLogBookMaxID(clientSiteRadioCheck.ClientSiteId, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                    var logbook = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookId);
                    // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- End -- added by Binoy - 02-02-2024

                    if (colorId != null)
                    {
                        var color = _context.RadioCheckStatusColor.Where(x => x.Id == colorId).FirstOrDefault().Name;
                        // if (clientSiteRadioCheck.Status == "Off Duty") -commenting temporarily
                        //if (color == "Red 1")
                        if (colorId == 1)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Guard[" + guardInitials + "] did not logoff and Control Room had to correct",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);

                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }

                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            // Notes = "Guard[" + guardInitials + "] did not logoff and Control Room had to correct",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Normal,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }
                                        }

                                    }

                                }





                                var ClientSiteRadioChecksActivityDetails = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null);
                                foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                                {
                                    ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                                    UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);


                                    _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);


                                    /* Update Radio check status logOff*/

                                }

                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();
                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        //else if (color == "Red 2")
                        else if (colorId == 2)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        //GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Control Room tried to contact Guard[" + guardInitials + "] and no answer.",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);
                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }

                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            //GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            //  Notes = "Control Room tried to contact Guard[" + guardInitials + "] and no answer.",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }
                                        }

                                    }

                                }






                                _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                                _context.SaveChanges();

                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();
                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        //else if (color == "Red 3")
                        else if (colorId == 3)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        //GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);
                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }
                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            //  GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            // Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }

                                        }

                                    }

                                }
                                _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                                _context.SaveChanges();

                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();
                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        else if (colorId == 4)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);
                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }
                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            // Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }
                                        }

                                    }

                                }
                                _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                                _context.SaveChanges();

                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        else if (colorId == 5)
                        {

                            var DuressEnabledUpdate = _context.ClientSiteDuress.Where(z => z.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
                            //DuressEnabledUpdate.IsEnabled = false;
                            _context.ClientSiteDuress.RemoveRange(DuressEnabledUpdate);
                            /* remove Duressbutton Status from RadioCheckPushMessages*/
                            UpdateDuressButtonAcknowledged(clientSiteRadioCheck.ClientSiteId);


                            var guardLoginId = _context.GuardLogins
                            .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                            if (guardLoginId != null)
                            {
                                var guardLog = new GuardLog()
                                {
                                    ClientSiteLogBookId = logBookId,
                                    GuardLoginId = guardLoginId.Id,
                                    EventDateTime = DateTime.Now,
                                    Notes = "Duress Alarm De-Activated by Control Room",
                                    IrEntryType = IrEntryType.Notification,
                                    IsSystemEntry = true,
                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                    PlayNotificationSound = true


                                };
                                SaveGuardLog(guardLog);
                                if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                {
                                    var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                    if (guardLoginToUpdate != null)
                                    {
                                        guardLoginToUpdate.OffDuty = DateTime.Now;
                                        _context.SaveChanges();
                                    }
                                }



                            }
                            else
                            {
                                var latestRecord = _context.GuardLogins
                                .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                .OrderByDescending(r => r.Id)
                                 .FirstOrDefault();
                                if (latestRecord != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = latestRecord.Id,
                                        EventDateTime = DateTime.Now,
                                        Notes = "Duress Alarm De-Activated by Control Room",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);


                                }

                            }


                            /* linked duress De-Activated Start */
                            var ifSiteisLinkedDuressSite = checkIfASiteisLinkedDuress(clientSiteRadioCheck.ClientSiteId);
                            if (ifSiteisLinkedDuressSite.Count != 0)
                            {   /*get all linked duress sites */
                                var allLinkedSites = getallClientSitesLinkedDuress(clientSiteRadioCheck.ClientSiteId);
                                if (allLinkedSites.Count != 0)
                                {

                                    foreach (var linkedSite in allLinkedSites)
                                    {
                                        /* avoid Repete entery for duress enabled site */
                                        if (linkedSite.ClientSiteId != clientSiteRadioCheck.ClientSiteId)
                                        {


                                            LogBookEntryFromRcControlRoomMessages(0, clientSiteRadioCheck.GuardId, null, clientSiteRadioCheck.Status, IrEntryType.Notification, 2, clientSiteRadioCheck.ClientSiteId, tmzdata);

                                            var DuressEnabledUpdateLinked = _context.ClientSiteDuress.Where(z => z.ClientSiteId == linkedSite.ClientSiteId && z.LinkedDuressParentSiteId == clientSiteRadioCheck.ClientSiteId && z.IsLinkedDuressParentSite == 0);
                                            //DuressEnabledUpdate.IsEnabled = false;
                                            _context.ClientSiteDuress.RemoveRange(DuressEnabledUpdateLinked);
                                            /* remove Duressbutton Status from RadioCheckPushMessages*/
                                            UpdateDuressButtonAcknowledged(linkedSite.ClientSiteId);

                                            var logBookIdLinked = GetClientSiteLogBookIdGloablmessage(linkedSite.ClientSiteId, LogBookType.DailyGuardLog, logbookdate);

                                            // var logBookIdLinked = GetClientSiteLogBookIdByLogBookMaxID(linkedSite.ClientSiteId, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                                            var logbookLinked = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookIdLinked);
                                            var guardLoginIdLinked = _context.GuardLogins
                            .FirstOrDefault(z => z.ClientSiteLogBookId == logBookIdLinked && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                            if (guardLoginIdLinked != null)
                                            {
                                                var guardLog = new GuardLog()
                                                {
                                                    ClientSiteLogBookId = logBookIdLinked,
                                                    GuardLoginId = guardLoginIdLinked.Id,
                                                    EventDateTime = DateTime.Now,
                                                    Notes = "Duress Alarm De-Activated by Control Room",
                                                    IrEntryType = IrEntryType.Notification,
                                                    IsSystemEntry = true,
                                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                                    PlayNotificationSound = true


                                                };
                                                SaveGuardLog(guardLog);



                                            }
                                            else
                                            {

                                                var guardLog = new GuardLog()
                                                {
                                                    ClientSiteLogBookId = logBookIdLinked,
                                                    GuardLoginId = null,
                                                    EventDateTime = DateTime.Now,
                                                    Notes = "Duress Alarm De-Activated by Control Room",
                                                    IrEntryType = IrEntryType.Notification,
                                                    IsSystemEntry = true,
                                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                                    PlayNotificationSound = true

                                                };
                                                SaveGuardLog(guardLog);




                                            }

                                        }
                                    }

                                }

                            }

                            /* linked duress De-Activated end*/



                            _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                            _context.SaveChanges();

                            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                            _context.SaveChanges();

                        }
                        else if (colorId == 6)
                        {
                            _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                            _context.SaveChanges();
                        }
                        else
                        {
                            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                            _context.SaveChanges();


                        }

                    }
                    else
                    {

                        _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                        _context.SaveChanges();
                    }
                }

                else
                {



                    var colorId = _context.RadioCheckStatus.Where(x => x.Id == clientSiteRadioCheck.RadioCheckStatusId).FirstOrDefault().RadioCheckStatusColorId;

                    // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- Start -- added by Binoy - 02-02-2024
                    // To Log the entry to the last logbook id of the client.
                    var logbookdate = DateTime.Today;
                    var logbooktype = LogBookType.DailyGuardLog;
                    var logBookId = GetClientSiteLogBookIdByLogBookMaxID(clientSiteRadioCheck.ClientSiteId, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                    var logbook = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookId);
                    // Task p6#73_TimeZone_Midnight_Perth_CreateEntryAfterMidnight issue -- End -- added by Binoy - 02-02-2024

                    if (colorId != null)
                    {
                        var color = _context.RadioCheckStatusColor.Where(x => x.Id == colorId).FirstOrDefault().Name;
                        // if (clientSiteRadioCheck.Status == "Off Duty") -commenting temporarily
                        //if (color == "Red 1")
                        if (colorId == 1)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Guard[" + guardInitials + "] did not logoff and Control Room had to correct",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);

                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }

                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            // Notes = "Guard[" + guardInitials + "] did not logoff and Control Room had to correct",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Normal,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }
                                        }

                                    }

                                }



                                var ClientSiteRadioChecksActivityDetails = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null);
                                foreach (var ClientSiteRadioChecksActivity in ClientSiteRadioChecksActivityDetails)
                                {
                                    ClientSiteRadioChecksActivity.GuardLogoutTime = DateTime.Now;
                                    UpdateRadioChecklistLogOffEntry(ClientSiteRadioChecksActivity);




                                    /* Update Radio check status logOff*/

                                }


                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        //else if (color == "Red 2")
                        else if (colorId == 2)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        //GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Control Room tried to contact Guard[" + guardInitials + "] and no answer.",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);
                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }

                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            //GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            //  Notes = "Control Room tried to contact Guard[" + guardInitials + "] and no answer.",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }
                                        }

                                    }

                                }







                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        //else if (color == "Red 3")
                        else if (colorId == 3)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        //GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);
                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }
                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            //  GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            // Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }

                                        }

                                    }

                                }





                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();
                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        else if (colorId == 4)
                        {
                            /* Check if Manning type notfication */
                            var checkIfTypeOneManning = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();

                            if (checkIfTypeOneManning.Count == 0)
                            {


                                var guardLoginId = _context.GuardLogins
                              .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                var guardInitials = _context.Guards.Where(x => x.Id == clientSiteRadioCheck.GuardId).FirstOrDefault().Initial;
                                if (guardLoginId != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = guardLoginId.Id,
                                        EventDateTime = DateTime.Now,
                                        //Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                        Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                        // Notes = "Guard Off Duty (Logbook Signout)",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);
                                    if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                    {
                                        var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                        if (guardLoginToUpdate != null)
                                        {
                                            guardLoginToUpdate.OffDuty = DateTime.Now;
                                            _context.SaveChanges();
                                        }
                                    }

                                }
                                else
                                {
                                    var latestRecord = _context.GuardLogins
                                    .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                    .OrderByDescending(r => r.Id)
                                     .FirstOrDefault();
                                    if (latestRecord != null)
                                    {
                                        var guardLog = new GuardLog()
                                        {
                                            ClientSiteLogBookId = logBookId,
                                            GuardLoginId = latestRecord.Id,
                                            EventDateTime = DateTime.Now,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            // Notes = "Control Room tried to contact Guard[" + guardInitials + "] and they are on their way but running late.",
                                            Notes = "Control Room Alert:" + clientSiteRadioCheck.Status,
                                            // Notes = "Guard Off Duty (Logbook Signout)",
                                            IrEntryType = IrEntryType.Notification,
                                            IsSystemEntry = true,
                                            EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                            EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                            EventDateTimeZone = tmzdata.EventDateTimeZone,
                                            EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                            EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                            PlayNotificationSound = true

                                        };
                                        SaveGuardLog(guardLog);
                                        if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                        {
                                            var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == latestRecord.Id);
                                            if (guardLoginToUpdate != null)
                                            {
                                                guardLoginToUpdate.OffDuty = DateTime.Now;
                                                _context.SaveChanges();
                                            }
                                        }

                                    }

                                }






                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                            }
                            else
                            {
                                _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                                _context.SaveChanges();

                                /* Remove the Notification Row */
                                var removeList = GetClientSiteRadioChecksActivityDetails().Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId && x.GuardLoginTime != null && x.NotificationType == 1).ToList();
                                _context.ClientSiteRadioChecksActivityStatus.RemoveRange(removeList);
                                _context.SaveChanges();
                            }

                        }
                        else if (colorId == 5)
                        {

                            var DuressEnabledUpdate = _context.ClientSiteDuress.Where(z => z.ClientSiteId == clientSiteRadioCheck.ClientSiteId);
                            //DuressEnabledUpdate.IsEnabled = false;
                            _context.ClientSiteDuress.RemoveRange(DuressEnabledUpdate);
                            /* remove Duressbutton Status from RadioCheckPushMessages*/
                            UpdateDuressButtonAcknowledged(clientSiteRadioCheck.ClientSiteId);


                            var guardLoginId = _context.GuardLogins
                            .FirstOrDefault(z => z.ClientSiteLogBookId == logBookId && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                            if (guardLoginId != null)
                            {
                                var guardLog = new GuardLog()
                                {
                                    ClientSiteLogBookId = logBookId,
                                    GuardLoginId = guardLoginId.Id,
                                    EventDateTime = DateTime.Now,
                                    Notes = "Duress Alarm De-Activated by Control Room",
                                    IrEntryType = IrEntryType.Notification,
                                    IsSystemEntry = true,
                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                    PlayNotificationSound = true


                                };
                                SaveGuardLog(guardLog);
                                if (clientSiteRadioCheck.Status.Contains("Off Duty"))
                                {
                                    var guardLoginToUpdate = _context.GuardLogins.SingleOrDefault(x => x.Id == guardLoginId.Id);
                                    if (guardLoginToUpdate != null)
                                    {
                                        guardLoginToUpdate.OffDuty = DateTime.Now;
                                        _context.SaveChanges();
                                    }
                                }

                            }
                            else
                            {
                                var latestRecord = _context.GuardLogins
                                .Where(x => x.GuardId == clientSiteRadioCheck.GuardId && x.ClientSiteId == clientSiteRadioCheck.ClientSiteId)
                                .OrderByDescending(r => r.Id)
                                 .FirstOrDefault();
                                if (latestRecord != null)
                                {
                                    var guardLog = new GuardLog()
                                    {
                                        ClientSiteLogBookId = logBookId,
                                        GuardLoginId = latestRecord.Id,
                                        EventDateTime = DateTime.Now,
                                        Notes = "Duress Alarm De-Activated by Control Room",
                                        IrEntryType = IrEntryType.Notification,
                                        IsSystemEntry = true,
                                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                        PlayNotificationSound = true

                                    };
                                    SaveGuardLog(guardLog);


                                }

                            }

                            /* linked duress De-Activated Start */
                            var ifSiteisLinkedDuressSite = checkIfASiteisLinkedDuress(clientSiteRadioCheck.ClientSiteId);
                            if (ifSiteisLinkedDuressSite.Count != 0)
                            {   /*get all linked duress sites */
                                var allLinkedSites = getallClientSitesLinkedDuress(clientSiteRadioCheck.ClientSiteId);
                                if (allLinkedSites.Count != 0)
                                {

                                    foreach (var linkedSite in allLinkedSites)
                                    {
                                        /* avoid Repete entery for duress enabled site */
                                        if (linkedSite.ClientSiteId != clientSiteRadioCheck.ClientSiteId)
                                        {

                                            LogBookEntryFromRcControlRoomMessages(0, clientSiteRadioCheck.GuardId, null, clientSiteRadioCheck.Status, IrEntryType.Notification, 2, linkedSite.ClientSiteId, tmzdata);

                                            var DuressEnabledUpdateLinked = _context.ClientSiteDuress.Where(z => z.ClientSiteId == linkedSite.ClientSiteId && z.LinkedDuressParentSiteId == clientSiteRadioCheck.ClientSiteId && z.IsLinkedDuressParentSite == 0);
                                            //DuressEnabledUpdate.IsEnabled = false;
                                            _context.ClientSiteDuress.RemoveRange(DuressEnabledUpdateLinked);
                                            /* remove Duressbutton Status from RadioCheckPushMessages*/
                                            UpdateDuressButtonAcknowledged(linkedSite.ClientSiteId);
                                            var logBookIdLinked = GetClientSiteLogBookIdGloablmessage(linkedSite.ClientSiteId, LogBookType.DailyGuardLog, logbookdate);
                                            //var logBookIdLinked = GetClientSiteLogBookIdByLogBookMaxID(linkedSite.ClientSiteId, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                                            var logbookLinked = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookIdLinked);
                                            var guardLoginIdLinked = _context.GuardLogins
                            .FirstOrDefault(z => z.ClientSiteLogBookId == logBookIdLinked && z.GuardId == clientSiteRadioCheck.GuardId && z.OnDuty.Date == DateTime.Today);
                                            if (guardLoginId != null)
                                            {
                                                var guardLog = new GuardLog()
                                                {
                                                    ClientSiteLogBookId = logBookIdLinked,
                                                    GuardLoginId = guardLoginIdLinked.Id,
                                                    EventDateTime = DateTime.Now,
                                                    Notes = "Duress Alarm De-Activated by Control Room",
                                                    IrEntryType = IrEntryType.Notification,
                                                    IsSystemEntry = true,
                                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                                    PlayNotificationSound = true


                                                };
                                                SaveGuardLog(guardLog);



                                            }
                                            else
                                            {

                                                var guardLog = new GuardLog()
                                                {
                                                    ClientSiteLogBookId = logBookIdLinked,
                                                    EventDateTime = DateTime.Now,
                                                    Notes = "Duress Alarm De-Activated by Control Room",
                                                    IrEntryType = IrEntryType.Notification,
                                                    IsSystemEntry = true,
                                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                                                    PlayNotificationSound = true

                                                };
                                                SaveGuardLog(guardLog);




                                            }

                                        }
                                    }

                                }

                            }

                            /* linked duress De-Activated end*/



                            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                            _context.SaveChanges();

                        }
                        else if (colorId == 6)
                        {
                            _context.ClientSiteRadioChecks.RemoveRange(clientSiteRcStatus);
                            _context.SaveChanges();
                        }
                        else
                        {
                            _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                            _context.SaveChanges();


                        }

                    }
                    else
                    {

                        _context.ClientSiteRadioChecks.Add(clientSiteRadioCheck);
                        _context.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {


            }
        }
        //for active guards-end


        public List<RadioCheckListGuardLoginData> GetClientSiteRadiocheckStatus(int clientSiteId, int guardId)
        {





            var param1 = new SqlParameter();
            param1.ParameterName = "@ClientSiteId";
            param1.SqlDbType = SqlDbType.Int;
            param1.SqlValue = clientSiteId;

            var param2 = new SqlParameter();
            param2.ParameterName = "@GuardId";
            param2.SqlDbType = SqlDbType.Int;
            param2.SqlValue = guardId;


            var allvalues = _context.RadioCheckListGuardLoginData.FromSqlRaw($"EXEC sp_GetActiveGuardRadioCheckStatusForRC @ClientSiteId,@GuardId", param1, param2).ToList();

            return allvalues;
        }

        //for global push message-start
        public int GetClientSiteLogBookIdGloablmessage(int clientsiteId, LogBookType type, DateTime date)
        {
            var logBook = _context?.ClientSiteLogBooks
            .FirstOrDefault(z => z.ClientSiteId == clientsiteId && z.Type == type && z.Date == date);

            if (logBook != null && logBook.Id != null)
            {
                return logBook.Id;
            }
            else
            {
                // p6#73 timezone bug - Modified by binoy on 24-01-2024 Date = DateTime.Today changed to Date = date
                var newLogBook = new ClientSiteLogBook()
                {
                    ClientSiteId = clientsiteId,
                    Type = LogBookType.DailyGuardLog,
                    Date = date
                };

                if (newLogBook.Id == 0)
                {
                    _context.ClientSiteLogBooks.Add(newLogBook);
                }
                _context.SaveChanges();
                // Handle the case where no matching log book is found or logBook.Id is null
                return newLogBook.Id; ; // Return null or another suitable default value
            }
        }
        //To get the count of ClientType start
        public int GetClientTypeCount(int? typeId)
        {
            var result = GetClientSite(typeId);
            return result;
        }
        public int GetClientSite(int? typeId)
        {
            return _context.ClientSites.Where(x => x.TypeId == typeId).Select(x => x.Id).Count();
        }
        //To get the count of ClientType stop
        //code added for client site dropdown starts
        public List<ClientType> GetUserClientTypesHavingAccess(int? userId)
        {
            var clientTypes = GetClientTypes().Where(x=>x.IsActive==true).ToList();
            if (userId == null)
                return clientTypes;

            var allUserAccess = GetUserClientSiteAccess(userId);
            var clientTypeIds = allUserAccess.Select(x => x.ClientSite.TypeId).Distinct().ToList();
            return clientTypes.Where(x => clientTypeIds.Contains(x.Id)).ToList();
        }
        public List<ClientType> GetClientTypes()
        {
            return _context.ClientTypes.OrderBy(x => x.Name).ToList();
        }
        public List<UserClientSiteAccess> GetUserClientSiteAccess(int? userId)
        {
            return _context.UserClientSiteAccess
                .Where(x => (!userId.HasValue || userId.HasValue && x.UserId == userId) && x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.User)
                .ToList();
        }
        public List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm)
        {
            var results = new List<ClientSite>();
            var clientSites = GetClientSites(typeId);
            if (userId == null)
                results = clientSites;
            else
            {
                var allUserAccess = GetUserClientSiteAccess(userId);
                var clientSiteIds = allUserAccess.Select(x => x.ClientSite.Id).Distinct().ToList();
                results = clientSites.Where(x => clientSiteIds.Contains(x.Id)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
                results = results.Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(x.Address) && x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))).ToList();

            return results;
        }
        public List<ClientSite> GetUserClientSitesHavingAccessRadio(int? typeId, int? userId, string searchTerm)
        {
            var results = new List<ClientSite>();
            var clientSites = GetClientSitesRadio(typeId);
            if (userId == null)
                results = clientSites;
            else
            {
                var allUserAccess = GetUserClientSiteAccess(userId);
                var clientSiteIds = allUserAccess.Select(x => x.ClientSite.Id).Distinct().ToList();
                results = clientSites.Where(x => clientSiteIds.Contains(x.Id)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
                results = results.Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(x.Address) && x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))).ToList();


            return results;
        }
        public List<ClientSite> GetClientSitesRadio(int? typeId)
        {
            return _context.ClientSites
                .Where(x => (!typeId.HasValue || (typeId.HasValue && x.TypeId == typeId.Value)) && x.IsActive == true)
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();
        }
        public List<State> GetStates()
        {
            return new List<State>()
            {
                new State() { Name = "ACT" },
                new State() { Name = "NSW" },
                new State() { Name = "NT" },
                new State() { Name = "QLD" },
                new State() { Name = "SA" },
                new State() { Name = "TAS" },
                new State() { Name = "VIC" },
                new State() { Name = "WA" }
            }
            .OrderBy(x => x.Name)
            .ToList();
        }
        public List<ClientSite> GetClientSitesForState(string State)
        {
            return _context.ClientSites
                .Where(site => site.State == State)
                        .ToList();

        }
        public List<ClientSite> GetAllClientSites()
        {
            return _context.ClientSites.ToList();

        }
        public List<SelectListItem> GetUserClientSitesWithId(string types)
        {
            if (string.IsNullOrEmpty(types))
                return Enumerable.Empty<SelectListItem>().ToList();

            return GetAllClientSites()
                .Where(z => types.Split(';').Contains(z.ClientType.Name))
                .Select(z => new SelectListItem(z.Name, z.Id.ToString()))
                .ToList();
        }


        public List<KeyVehicleLog> GetKeyVehicleLogs(string truckno)
        {
            var results = _context.KeyVehicleLogs.Where(z => z.VehicleRego == truckno);


            return results.ToList();
        }

        public void LogBookEntryForRcControlRoomMessages(int loginGuardId, int selectedGuardId, string subject, string notifications,
                                                         IrEntryType entryType, int type, int clientSiteId, GuardLog tmzdata)
        {

            var guardInitials = string.Empty;
            var alreadyExistingSite = _context.RadioCheckLogbookSiteDetails.ToList();
            var clientSiteForLogbook = _context.ClientSites.Where(x => x.Id == alreadyExistingSite.FirstOrDefault().ClientSiteId)
                .Include(x => x.ClientType).OrderBy(x => x.ClientType.Name).ThenBy(x => x.Name).ToList();
            if (selectedGuardId != 0)
            {

                guardInitials = _context.Guards.Where(x => x.Id == selectedGuardId).FirstOrDefault().Name + " [" + _context.Guards.Where(x => x.Id == selectedGuardId).FirstOrDefault().Initial + "]";

            }
            /* Rc Status update*/
            if (type == 2)
            {
                if (clientSiteForLogbook.Count() > 0)
                {

                    var clientsitename = GetClientSites(clientSiteId).FirstOrDefault().Name;
                    notifications = "Control Room Alert for " + guardInitials + " - " + clientsitename + ": " + notifications;
                }


            }

            if (clientSiteForLogbook.Count != 0)
            {
                // p6#73 timezone bug - Modified by binoy 24-01-2024 changed DateTime.Today to localDateTime.Date
                var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmzdata.EventDateTimeUtcOffsetMinute);
                var logBookId = GetClientSiteLogBookIdGloablmessage(clientSiteForLogbook.FirstOrDefault().Id, LogBookType.DailyGuardLog, localDateTime.Date);

                if (loginGuardId != 0)
                {
                    var guardLoginId = GetGuardLoginId(Convert.ToInt32(loginGuardId), localDateTime.Date); // DateTime.Today
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = string.IsNullOrEmpty(subject) ? notifications : subject + " : " + notifications,
                        IrEntryType = entryType,
                        IsSystemEntry = true,
                        EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                        PlayNotificationSound = true // Task p6#73_TimeZone issue -- added by Binoy - End

                    };
                    SaveGuardLog(guardLog);
                }
                else
                {
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        EventDateTime = DateTime.Now,
                        Notes = string.IsNullOrEmpty(subject) ? notifications : subject + " : " + notifications,
                        IrEntryType = entryType,
                        IsSystemEntry = true,
                        EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                        PlayNotificationSound = true // Task p6#73_TimeZone issue -- added by Binoy - End
                    };
                    if (guardLog.ClientSiteLogBookId != 0)
                    {
                        SaveGuardLog(guardLog);
                    }

                }

            }

        }

        public void LogBookEntryFromRcControlRoomMessages(int loginGuardId, int selectedGuardId, string subject, string notifications,
                                                         IrEntryType entryType, int type, int clientSiteId, GuardLog tmzdata)
        {

            var guardInitials = string.Empty;
            var alreadyExistingSite = _context.RadioCheckLogbookSiteDetails.ToList();
            var clientSiteForLogbook = _context.ClientSites.Where(x => x.Id == alreadyExistingSite.FirstOrDefault().ClientSiteId)
                .Include(x => x.ClientType).OrderBy(x => x.ClientType.Name).ThenBy(x => x.Name).ToList();
            if (selectedGuardId != 0)
            {

                guardInitials = _context.Guards.Where(x => x.Id == selectedGuardId).FirstOrDefault().Name + " [" + _context.Guards.Where(x => x.Id == selectedGuardId).FirstOrDefault().Initial + "]";

            }
            /* Rc Status update*/
            if (type == 2)
            {
                if (clientSiteForLogbook.Count() > 0)
                {

                    var clientsitename = GetClientSites(clientSiteId).FirstOrDefault().Name;
                    notifications = "Control Room Alert for " + guardInitials + " - " + clientsitename + ": " + notifications;
                }


            }

            if (clientSiteForLogbook.Count != 0)
            {
                var localDateTime = DateTime.Today;
                var entryTime = DateTime.Now;
                // p6#73 timezone bug - Modified by binoy 29-01-2024 changed DateTime.Today to localDateTime.Date
                // var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmzdata.EventDateTimeUtcOffsetMinute);
                var logBookId = GetClientSiteLogBookIdGloablmessage(clientSiteForLogbook.FirstOrDefault().Id, LogBookType.DailyGuardLog, localDateTime.Date);
                //var logbookdate = DateTime.Today;
                //var logbooktype = LogBookType.DailyGuardLog;      
                //var logBookId = GetClientSiteLogBookIdByLogBookMaxID(clientSiteForLogbook.FirstOrDefault().Id, logbooktype, out logbookdate); // Get Last Logbookid and logbook Date by latest logbookid  of the client site
                //var entryTime = DateTimeHelper.GetLogbookEndTimeFromDate(logbookdate);

                if (loginGuardId != 0)
                {
                    var guardLoginId = GetGuardLoginId(Convert.ToInt32(loginGuardId), localDateTime.Date); // DateTime.Today
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = string.IsNullOrEmpty(subject) ? notifications : subject + " : " + notifications,
                        IrEntryType = entryType,
                        IsSystemEntry = true,
                        EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                        PlayNotificationSound = true // Task p6#73_TimeZone issue -- added by Binoy - End

                    };
                    SaveGuardLog(guardLog);
                }
                else
                {
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        EventDateTime = DateTime.Now,
                        Notes = string.IsNullOrEmpty(subject) ? notifications : subject + " : " + notifications,
                        IrEntryType = entryType,
                        IsSystemEntry = true,
                        EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                        PlayNotificationSound = true // Task p6#73_TimeZone issue -- added by Binoy - End
                    };
                    if (guardLog.ClientSiteLogBookId != 0)
                    {
                        SaveGuardLog(guardLog);
                    }

                }

            }

        }

        //do's and donts-start
        public List<DosAndDontsField> GetDosandDontsFields(int type)
        {
            return _context.DosAndDontsField
                .Where(x => x.TypeId == type)
                .OrderBy(x => x.Id)
                .ToList();
        }
        public void SaveDosandDontsField(DosAndDontsField dosanddontsField)
        {
            if (dosanddontsField.Id == -1)
            {
                dosanddontsField.Id = 0;
                _context.DosAndDontsField.Add(dosanddontsField);
            }
            else
            {
                var dosanddontsFieldUpdate = _context.DosAndDontsField.SingleOrDefault(x => x.Id == dosanddontsField.Id);
                if (dosanddontsFieldUpdate != null)
                {
                    dosanddontsFieldUpdate.Name = dosanddontsField.Name;
                    dosanddontsFieldUpdate.TypeId = dosanddontsField.TypeId;
                }
            }
            _context.SaveChanges();
        }
        public void DeleteDosandDontsField(int id)
        {
            var DosAndDontsFieldToDelete = _context.DosAndDontsField.SingleOrDefault(x => x.Id == id);
            if (DosAndDontsFieldToDelete == null)
                throw new InvalidOperationException();

            _context.Remove(DosAndDontsFieldToDelete);
            _context.SaveChanges();
        }
        //code to get ActionList start
        public RCActionList GetActionlist(int Cliensiteid)
        {
            var ActionList = _context?.RCActionList
            .FirstOrDefault(z => z.ClientSiteID == Cliensiteid);
            return ActionList;
        }
        public string GetUserClientSites(string searchTerm)
        {
            var clientSites = _context?.ClientSites
     .Where(z => string.IsNullOrEmpty(searchTerm) || z.Name.ToLower().Contains(searchTerm.ToLower()))
     .FirstOrDefault();

            if (clientSites != null)
            {
                return clientSites.Address;
            }
            else
            {
                // Handle the case when no matching record is found
                return "No matching record found";
            }
        }
        public int GetUserClientSitesRCList(string searchTerm)
        {
            var clientSites = _context?.ClientSites
     .Where(z => string.IsNullOrEmpty(searchTerm) || z.Name.ToLower().Contains(searchTerm.ToLower()))
     .FirstOrDefault();

            if (clientSites != null)
            {
                return clientSites.Id;
            }
            else
            {
                // Handle the case when no matching record is found
                return 0;
            }
        }
        //code to get ActionList stop

        //To Delete RadiocheckStatusKV
        public void DeleteClientSiteRadioCheckActivityStatusForKV(int id)
        {
            var clientSiteRadioCheckActivityStatusToDelete = _context.ClientSiteRadioChecksActivityStatus.Where(x => x.Id == id);
            if (clientSiteRadioCheckActivityStatusToDelete == null)
                throw new InvalidOperationException();
            foreach (var item in clientSiteRadioCheckActivityStatusToDelete)
            {
                _context.Remove(item);
            }


            _context.SaveChanges();
        }

        //do's and donts-end

        public int SavePushMessage(RadioCheckPushMessages radioCheckPushMessages)
        {
            _context.RadioCheckPushMessages.Add(radioCheckPushMessages);
            _context.SaveChanges();
            return radioCheckPushMessages.Id;
        }

        public void UpdateIsAcknowledged(int rcPushMessageId)
        {
            var radioCheckPushMessages = _context.RadioCheckPushMessages.SingleOrDefault(x => x.Id == rcPushMessageId);
            if (radioCheckPushMessages == null)
                throw new InvalidOperationException();
            radioCheckPushMessages.IsAcknowledged = 1;
            radioCheckPushMessages.PlayNotificationSound = true; // Project 4 , Task 48, Audio notification, Added By Binoy
            _context.SaveChanges();

        }

        public void UpdateDuressButtonAcknowledged(int ClientSiteId)
        {
            var duressButtonList = _context.RadioCheckPushMessages.Where(x => x.ClientSiteId == ClientSiteId && x.IsDuress == 1 && x.IsAcknowledged == 0).ToList();
            if (duressButtonList == null)
                throw new InvalidOperationException();
            foreach (var row in duressButtonList)
            {
                row.IsAcknowledged = 1;
                _context.SaveChanges();

            }

        }
        public void CopyPreviousDaysPushMessageToLogBook(List<RadioCheckPushMessages> previousDayPushmessageList, int logBookId, int guardLoginId, GuardLog tmzdata)
        {
            foreach (var pushMessage in previousDayPushmessageList)
            {
                if (pushMessage.IsAcknowledged == 0)
                {
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = pushMessage.Notes,
                        IrEntryType = IrEntryType.Alarm,
                        RcPushMessageId = pushMessage.Id,
                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                        PlayNotificationSound = false
                    };
                    SaveGuardLog(guardLog);

                }

            }

        }

        public void CopyPreviousDaysDuressToLogBook(List<RadioCheckPushMessages> previousDayDuressList, int logBookId, int guardLoginId, GuardLog tmzdata)
        {
            foreach (var pushMessage in previousDayDuressList)
            {
                if (pushMessage.IsAcknowledged == 0)
                {
                    var guardLog = new GuardLog()
                    {
                        ClientSiteLogBookId = logBookId,
                        IsSystemEntry = true,
                        GuardLoginId = guardLoginId,
                        EventDateTime = DateTime.Now,
                        Notes = pushMessage.Notes,
                        IrEntryType = IrEntryType.Alarm,
                        RcPushMessageId = pushMessage.Id,
                        EventDateTimeLocal = tmzdata.EventDateTimeLocal,
                        EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                        EventDateTimeZone = tmzdata.EventDateTimeZone,
                        EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                        EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute,
                        PlayNotificationSound = false
                    };
                    SaveGuardLog(guardLog);

                }

            }

        }
        public List<KeyVehicleLogProfile> GetKeyVehicleLogVisitorProfile()
        {
            return _context.KeyVehicleLogVisitorProfiles.ToList();
        }
        public List<KeyVehicleLog> GetKeyVehicleLogsByID(int Id)
        {
            var results = _context.KeyVehicleLogs.Where(z => z.Id == Id);


            return results.ToList();
        }

        public void UpdateDuressAlarmPlayedStatus()
        {
            var alarmplayed = _context.ClientSiteDuress.Where(x => x.IsEnabled == true && x.PlayDuressAlarm == true);

            foreach (var a in alarmplayed)
            {
                a.PlayDuressAlarm = false;
            }
            _context.SaveChanges();

        }
        public List<KeyVehicleLogVisitorPersonalDetail> GetKeyVehicleLogVisitorPersonalDetailsWithPersonName(string personName)
        {
            return _context.KeyVehicleLogVisitorPersonalDetails
                .Include(z => z.KeyVehicleLogProfile)
                .Where(z => string.IsNullOrEmpty(personName) || string.Equals(z.PersonName, personName))
                .ToList();
        }
        public List<KeyVehicleLog> GetKeyVehicleLogsWithKeyNo(string KeyNo)
        {
            var results = _context.KeyVehicleLogs.Where(z => z.KeyNo.Contains(KeyNo));

            //results.Include(x => x.ClientSiteLogBook)
            //    .Include(x => x.GuardLogin)
            //    .Include(x => x.ClientSiteLocation)
            //    .Include(x => x.ClientSitePoc)
            //    .Load();

            return results.OrderByDescending(z => z.Id).ToList();
        }
        public List<KeyVehicleLogAuditHistory> GetAuditHistoryWithKeyVehicleLogId(int id)
        {
            return _context.KeyVehicleLogAuditHistory
                .Where(z => z.KeyVehicleLogId == id)
                .Include(z => z.GuardLogin)
                .ThenInclude(z => z.Guard)
                .ToList();
        }
        //To get the Details of RadiocheckLogbookDetails start
        public RadioCheckLogbookSiteDetails GetRadiocheckLogbookDetails()
        {
            return _context.RadioCheckLogbookSiteDetails.SingleOrDefault();
        }

        //To get the Details of RadiocheckLogbookDetails stop

        public int GetClientTypeByClientSiteId(int ClientSiteId)
        {
            var typeid = _context.ClientSites.Where(x => x.Id == ClientSiteId).FirstOrDefault().TypeId;
            return typeid;
        }

        //p1-191 hr files task 3-start
        public void SaveHRSettings(HrSettings hrSettings, int[] selctedSites, string[] selectedStates)
        {



            if (hrSettings.Id == 0)
            {
                _context.HrSettings.Add(hrSettings);
                _context.SaveChanges();
                int newId = hrSettings.Id;
                if (newId != 0)
                {
                    // Sites 

                    foreach (var siteId in selctedSites)
                    {
                        HrSettingsClientSites HrSettingsClientSites = new HrSettingsClientSites()
                        {

                            ClientSiteId = siteId,
                            HrSettingsId = newId

                        };


                        _context.HrSettingsClientSites.Add(HrSettingsClientSites);
                        _context.SaveChanges();

                    }


                    // State
                    if (selectedStates.Count() != 0)
                    {
                        foreach (var state in selectedStates)
                        {
                            HrSettingsClientStates HrSettingsStates = new HrSettingsClientStates()
                            {


                                HrSettingsId = newId,
                                State = state

                            };


                            _context.HrSettingsClientStates.Add(HrSettingsStates);
                            _context.SaveChanges();

                        }

                    }


                }
            }
            else
            {
                var hrSettingsToUpdate = _context.HrSettings.SingleOrDefault(x => x.Id == hrSettings.Id);
                if (hrSettingsToUpdate != null)
                {
                    hrSettingsToUpdate.HRGroupId = hrSettings.HRGroupId;
                    hrSettingsToUpdate.ReferenceNoAlphabetId = hrSettings.ReferenceNoAlphabetId;
                    hrSettingsToUpdate.ReferenceNoNumberId = hrSettings.ReferenceNoNumberId;
                    hrSettingsToUpdate.Description = hrSettings.Description;
                    _context.SaveChanges();
                }

                var hrremoveSites = _context.HrSettingsClientSites.Where(x => x.HrSettingsId == hrSettings.Id).ToList();
                if (hrremoveSites != null)
                {
                    _context.HrSettingsClientSites.RemoveRange(hrremoveSites);
                    _context.SaveChanges();

                }
                foreach (var siteId in selctedSites)
                {
                    HrSettingsClientSites HrSettingsClientSites = new HrSettingsClientSites()
                    {

                        ClientSiteId = siteId,
                        HrSettingsId = hrSettings.Id

                    };

                    _context.HrSettingsClientSites.Add(HrSettingsClientSites);
                    _context.SaveChanges();

                }





                var hrremoveStates = _context.HrSettingsClientStates.Where(x => x.HrSettingsId == hrSettings.Id).ToList();
                if (hrremoveStates != null)
                {
                    _context.HrSettingsClientStates.RemoveRange(hrremoveStates);
                    _context.SaveChanges();

                }
                foreach (var State in selectedStates)
                {
                    HrSettingsClientStates HrSettingsStates = new HrSettingsClientStates()
                    {

                        State = State,
                        HrSettingsId = hrSettings.Id

                    };

                    _context.HrSettingsClientStates.Add(HrSettingsStates);
                    _context.SaveChanges();

                }





            }

        }
        public void DeleteHRSettings(int id)
        {
            var deleteHrSettings = _context.HrSettings.SingleOrDefault(x => x.Id == id);
            if (deleteHrSettings != null)
                _context.HrSettings.Remove(deleteHrSettings);

            _context.SaveChanges();
        }
        public void SaveLicensesTypes(LicenseTypes licenseTypes)
        {
            if (licenseTypes.Id == -1)
            {
                licenseTypes.Id = 0;
                _context.LicenseTypes.Add(licenseTypes);
            }
            else
            {
                var licenseTypesToUpdate = _context.LicenseTypes.SingleOrDefault(x => x.Id == licenseTypes.Id);
                if (licenseTypesToUpdate != null)
                {
                    licenseTypesToUpdate.Name = licenseTypes.Name;
                    licenseTypesToUpdate.IsDeleted = false;

                }
            }
            _context.SaveChanges();
        }
        public void DeleteLicensesTypes(int id)
        {
            var licenseTypeToDelete = _context.LicenseTypes.SingleOrDefault(x => x.Id == id);
            if (licenseTypeToDelete != null)
                licenseTypeToDelete.IsDeleted = true;
            _context.SaveChanges();
        }
        //p1-191 hr files task 3-end
        //P4-79 MENU CORRECTIONS START
        public List<GuardLogin> GetGuardLogs(int clientSiteId)
        {



            //return _context.GuardLogs
            //    .Where(z => z.ClientSiteLogBook.ClientSiteId == clientSiteId && z.ClientSiteLogBook.Type == LogBookType.DailyGuardLog
            //            && z.ClientSiteLogBook.Date >= logFromDate && z.ClientSiteLogBook.Date <= logToDate &&
            //            (!excludeSystemLogs || (excludeSystemLogs && (!z.IsSystemEntry || z.IrEntryType.HasValue))))
            //    .Include(z => z.GuardLogin.Guard)
            //    .OrderBy(z => z.EventDateTimeLocal.HasValue? z.EventDateTimeLocal : z.EventDateTime) // p6#73 timezone bug - Modified by binoy 29-01-2024
            //    .ThenBy(z => z.Id)
            //    //.OrderBy(z => z.Id)
            //    //.ThenBy(z => z.EventDateTime)
            //    .ToList();

            var data = _context.GuardLogins
               .Where(z => z.ClientSiteId == clientSiteId)
               .Include(z => z.Guard)
               .ToList();



            return data;
        }
        //P4-79 MENU CORRECTIONS END
        public Guard GetGuardsWtihProviderNumber(int guardId)
        {

            var guards = _context.Guards.Where(x => x.Id == guardId).FirstOrDefault();
            if (guards != null)
            {
                if (guards.Provider != null)
                {
                    var results = _context.KeyVehicleLogs.Where(x => x.CompanyName == guards.Provider).FirstOrDefault();
                    guards.ProviderNo = results != null ? results.CompanyLandline : string.Empty;
                }
                else
                {
                    guards.ProviderNo = string.Empty;

                }


            }



            return guards;
        }

        public List<RCLinkedDuressClientSites> checkIfASiteisLinkedDuress(int siteId)
        {

            var ifexist = _context.RCLinkedDuressClientSites
               .Where(z => z.ClientSiteId == siteId)
               .ToList();
            return ifexist;

        }

        public List<RCLinkedDuressClientSites> getallClientSitesLinkedDuress(int siteId)
        {
            var linkedSitesList = new List<RCLinkedDuressClientSites>();
            var ifexist = _context.RCLinkedDuressClientSites
               .Where(z => z.ClientSiteId == siteId)
               .ToList();
            if (ifexist.Count > 0)
            {
                var rclinkedId = ifexist.FirstOrDefault().RCLinkedId;
                var alllinkedSites = _context.RCLinkedDuressClientSites.Where(x => x.RCLinkedId == rclinkedId).ToList();
                linkedSitesList = alllinkedSites;
            }
            return linkedSitesList;
        }


        public bool IsRClogbookStampRequired(string StampedByName)
        {
            bool Req = false;
            if (!string.IsNullOrEmpty(StampedByName))
            {
                var RecExists = _context.IncidentReportFields.Where(x => x.TypeId == ReportFieldType.NotifiedBy && x.Name.Equals(StampedByName)).FirstOrDefault();
                if (RecExists.StampRcLogbook == true)
                    Req = true;
            }
            return Req;
        }

        public List<FileDownloadAuditLogs> GetFileDownloadAuditLogsData(DateTime logFromDate, DateTime logToDate)
        {
            var r = _context.FileDownloadAuditLogs.Where(x => x.EventDateTime.Date >= logFromDate.Date && x.EventDateTime.Date <= logToDate.Date)
                .Include(u => u.User)
                .Include(g => g.Guard)
                .ToList();
            return r;
        }

        public void CreateDownloadFileAuditLogEntry(FileDownloadAuditLogs fdal)
        {
            if (fdal != null)
            {
                _context.Add(fdal);
                _context.SaveChanges();
            }
        }


        public List<ClientSiteRadioChecksActivityStatus_History> GetGuardFusionLogs(int clientSiteId, DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            //var data = _context.ClientSiteRadioChecksActivityStatus_History
            //.Where(z => z.ClientSiteId == clientSiteId )
            //.ToList();

            var data = _context.ClientSiteRadioChecksActivityStatus_History
               .Where(z => z.ClientSiteId == clientSiteId && z.EventDateTime.Date >= logFromDate && z.EventDateTime.Date <= logToDate)
               .ToList();

            var returnData = data.OrderBy(z => z.EventDateTime)
                .ToList();

            return returnData;
        }
        //p6-102 Add Photo -start
        public void SaveGuardLogDocumentImages(GuardLogsDocumentImages guardLogDocumentImages)
        {
            if (guardLogDocumentImages.Id == 0)
            {
                _context.GuardLogsDocumentImages.Add(new GuardLogsDocumentImages()
                {
                    ImagePath = guardLogDocumentImages.ImagePath,
                    IsRearfile = guardLogDocumentImages.IsRearfile,
                    IsTwentyfivePercentfile = guardLogDocumentImages.IsTwentyfivePercentfile,
                    GuardLogId = guardLogDocumentImages.GuardLogId

                });
            }
            //else
            //{
            //    var guardLogToUpdate = _context.GuardLogsDocumentImages.SingleOrDefault(x => x.Id == guardLogDocumentImages.Id);
            //    if (guardLogToUpdate == null)
            //        throw new InvalidOperationException();

            //    guardLogToUpdate.Notes = guardLogDocumentImages.Notes;
            //}
            _context.SaveChanges();
        }
        public List<GuardLogsDocumentImages> GetGuardLogDocumentImaes(int LogId)
        {
            var result = new List<GuardLogsDocumentImages>();
           result = _context.GuardLogsDocumentImages
                          .Where(z => z.GuardLogId==LogId)
                          .OrderBy(z => z.ImagePath)
                          .ToList();

                    
            return result;
        }


        //p6-102 Add Photo -end
        public List<GuardLogsDocumentImages> GetGuardLogDocumentImaesById(int Id)
        {
            var result = new List<GuardLogsDocumentImages>();
            result = _context.GuardLogsDocumentImages
                           .Where(z => z.Id == Id)
                         
                           .ToList();


            return result;
        }
        public void DeleteGuardLogDocumentImaes(int id)
        {
            var guardLogDocumentImaes = _context.GuardLogsDocumentImages.SingleOrDefault(i => i.Id == id);
            if (guardLogDocumentImaes != null)
            {
                _context.Remove(guardLogDocumentImaes);
                _context.SaveChanges();
            }
        }


        public void SaveLprWebhookResponse(LprWebhookResponse lprWebhookResponse)
        {
            if (lprWebhookResponse.Id == 0)
            {
                //Only one Licence no curresponding to One carmeraId
                var deletePreviousLprResponceForTheCameraId = _context.LprWebhookResponse.SingleOrDefault(x => x.camera_id == lprWebhookResponse.camera_id);


                if(deletePreviousLprResponceForTheCameraId!=null)
                {
                    _context.LprWebhookResponse.Remove(deletePreviousLprResponceForTheCameraId);
                    _context.SaveChanges();
                }


                _context.LprWebhookResponse.Add(lprWebhookResponse);
                _context.SaveChanges();
            }
           
           
        }
    }

}