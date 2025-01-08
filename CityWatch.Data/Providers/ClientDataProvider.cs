using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using iText.Commons.Actions.Contexts;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.StyledXmlParser.Jsoup.Safety;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.Esf;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Utilities;
using SMSGlobal.Response;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using static Dropbox.Api.FileProperties.PropertyType;
using static Dropbox.Api.Files.ListRevisionsMode;
using static Dropbox.Api.Files.SearchMatchType;
using static Dropbox.Api.TeamLog.PaperDownloadFormat;
//using static Dropbox.Api.Files.ListRevisionsMode;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CityWatch.Data.Providers
{
    public enum OfficerPositionFilterForManning
    {
        All = 0,

        PatrolOnly = 1,

        NonPatrolOnly = 2,

        SecurityOnly = 3
    }
    public interface IClientDataProvider
    {
        List<ClientSite> GetUserClientSites(string type, string searchTerm);
        List<ClientType> GetClientTypes();
        List<IncidentReportPSPF> GetPSPF();
        void SaveClientType(ClientType clientType);
        void DeleteClientType(int id);
        List<ClientSite> GetClientSites(int? typeId);
        List<ClientSite> GetNewClientSites();
        void SaveClientSite(ClientSite clientSite);
        void SaveCompanyDetails(CompanyDetails companyDetails);
         void SaveCompanyMailDetails(CompanyDetails companyDetails);
        void SavePlateLoaded(IncidentReportsPlatesLoaded report);
        void DeletePlateLoaded(IncidentReportsPlatesLoaded report);
        void DeleteFullPlateLoaded(IncidentReportsPlatesLoaded report, int Count);

        void DeleteClientSite(int id);
        List<ClientSiteKpiSetting> GetClientSiteKpiSettings();
        ClientSiteKpiSetting GetClientSiteKpiSetting(int clientSiteId);
        List<ClientSiteKpiSetting> GetClientSiteKpiSetting(int[] clientSiteIds);
        void SaveClientSiteKpiSetting(ClientSiteKpiSetting setting);
        int SaveClientSiteManningKpiSetting(ClientSiteKpiSetting setting);
        ClientSiteKpiNote GetClientSiteKpiNote(int id);
        RCActionList GetClientSiteKpiRC(int id);
        int SaveClientSiteKpiNote(ClientSiteKpiNote note);
        int SaveRCList(RCActionList RC);
        List<ClientSiteLogBook> GetClientSiteLogBooks();
        List<ClientSiteLogBook> GetClientSiteLogBooks(int? logBookId, LogBookType type);
        List<ClientSiteLogBook> GetClientSiteLogBooks(int clientSiteId, LogBookType type, DateTime fromDate, DateTime toDate);
        ClientSiteLogBook GetClientSiteLogBook(int clientSiteId, LogBookType type, DateTime date);
        int SaveClientSiteLogBook(ClientSiteLogBook logBook);
        void MarkClientSiteLogBookAsUploaded(int logBookId, string fileName);
        void SetDataCollectionStatus(int clientSiteId, bool enabled);

        string ValidDateTime(ClientSiteKpiSetting setting);

        List<IncidentReportsPlatesLoaded> GetIncidentDetailsKvlReport(int logId);
        List<KeyVehicleLog> GetKeyVehiclogWithPlateIdAndTruckNoByLogId(int[] plateid, string[] truckNo, int logId);
        List<KeyVehicleLog> GetKeyVehiclogWithPlateIdAndTruckNoByLogIdIndividual(int plateid, string truckNo, int logId);
        List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false);
        int GetMaxIncidentReportId(int LogId);
        public List<ClientSiteKey> GetClientSiteKeys(int clientSiteId);
        public List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds);
        List<ClientSitePoc> GetClientSitePocs();
        List<ClientSiteLocation> GetClientSiteLocations();
        List<ClientSite> GetClientSitesUsingGuardId(int? GuardId);
        List<ClientSiteLinksPageType> GetSiteLinksPageTypes();
        List<ClientSiteLinksDetails> GetSiteLinksPageDetails(int type);
        int SaveClientSiteLinksPageType(ClientSiteLinksPageType ClientSiteLinksPageTypeRecord);
        int SaveFeedbackType(FeedbackType FeedbackNewTypeRecord);

        int SaveSiteLinkDetails(ClientSiteLinksDetails ClientSiteLinksDetailsRecord);
        void DeleteSiteLinkDetails(int id);
        int GetIncidentReportsPlatesCount(int PlateId, string TruckNo, int? userId);

        List<ClientSiteLinksDetails> GetSiteLinkDetailsUsingTypeAndState(int type);
        string GetSiteLinksTypeUsingId(int typeId);
        int DeleteClientSiteLinksPageType(int typeId);
        int DeleteFeedBackType(int typeId);
        List<KeyVehcileLogField> GetKeyVehicleLogFieldsByTruckId(int TruckConfig);
        List<GuardLogin> GetGuardLogin(int GuardLoginId, int logBookId);
        List<GuardLog> GetGuardLogs(int GuardLoginId, int logBookId);

        List<GuardAccess> GetGuardAccess();
        ClientSite GetClientSitesUsingName(string name);

        List<ClientSite> GetClientSiteDetails(int[] clientSiteIds);
        List<ClientSiteRadioChecksActivityStatus> GetClientSiteRadioChecksActivityStatus(int GuardId, int ClientSiteId);
        //to add functions for settings in radio check-start
        void SaveRadioCheckStatus(RadioCheckStatus radioCheckStatus);
        void DeleteRadioCheckStatus(int id);
        //to get functions for settings in radio check-end

        int GetIncidentReportsPlatesCountWithOutPlateId(int? userId);

        List<IncidentReportsPlatesLoaded> GetIncidentReportsPlatesLoadedWithGuardId(int? userId, int? guardId);

        int RemoveIncidentReportsPlatesLoadedWithGuardId(List<IncidentReportsPlatesLoaded> platesLoaded);

        int RemoveIncidentReportsPlatesLoadedWithUserId(int? userId);
        //to add functions for Calendar events -start
        void SaveCalendarEvents(BroadcastBannerCalendarEvents calendarEvents);
        void DeleteCalendarEvents(int id);
        //to add functions for Calendar events -end
        //to add functions for Live events -start
        void SaveLiveEvents(BroadcastBannerLiveEvents liveEvents);
        void DeleteLiveEvents(int id);
        //to add functions for Live events -end
        //to get incident reports-start-jisha
        public List<IncidentReport> GetIncidentReports(DateTime date, int clientSiteId);
        //to get incident reports-end-jisha

        List<SelectListItem> GetUserClientTypesHavingAccess(int? userId);
        void SaveClientSiteForRcLogBook(int clientSiteId);
        List<ClientSite> GetClientSiteForRcLogBook();
        void RemoveRCList(int rcListId);
        void SaveUpdateRCListFile(int Id, string fileName, DateTime dtm);
        List<RadioCheckPushMessages> GetPushMessagesNotAcknowledged(int clientSiteId, DateTime date);

        List<RadioCheckPushMessages> GetDuressMessageNotAcknowledged(int clientSiteId, DateTime date);
        List<RadioCheckPushMessages> GetDuressMessageNotAcknowledgedForControlRoom(DateTime date);
        ClientSite GetClientSiteName(int clientSiteId);
        Guard GetGuradName(int guardId);
        List<GlobalDuressEmail> GetGlobalDuressEmail();
        public string GetDefaultEmailAddress();

        void RemoveWorker(int settingsId, int OrderId);
        void RemoveWorkerADHOC(int settingsId, int OrderId);

        List<ClientSiteLogBook> GetClientSiteLogBookWithOutType(int clientSiteId, DateTime date);
        //for checking whther user has any access to the client site ie to be deleted-start
        List<UserClientSiteAccess> GetUserAccessWithClientSiteId(int Id);
        int GetClientSite(int? typeId);
        //for checking whther user has any access to the client site ie to be deleted-end

        //SWChannels - start
        void SaveSWChannel(SWChannels liveEvents);
        void DeleteSWChannel(int id);


        //SWChannels - end
        //GeneralFeeds - start
        void SaveGeneralFeeds(GeneralFeeds generalFeeds);
        void DeleteGeneralFeeds(int id);
        void DuressGloablEmail(string Email);
        List<GlobalDuressEmail> GetDuressEmails();
        //GeneralFeeds - end

        void SaveSmsChannel(SmsChannel smsChannel);
        void DeleteSmsChannel(int id);
        List<ClientSite> GetNewClientSites(int siteId);

        List<GlobalDuressSms> GetDuressSms();
        string SaveDuressGloablSMS(GlobalDuressSms SmsNumber, out bool status);
        string DeleteDuressGloablSMSNumber(int SmsNumberId, out bool status);

        //for toggle areas - start 
        void SaveClientSiteToggle(int siteId, int toggleTypeId, bool IsActive);
        //for toggle areas - end
        IncidentReportPosition GetClientSitePosition(string Name);
        List<GlobalComplianceAlertEmail> GetGlobalComplianceAlertEmail();
        public void GlobalComplianceAlertEmail(string Email);
        //p1-191 hr files task 8-start
        List<KeyVehicleLog> GetKeyVehiclogWithProviders(string[] providers);
        //p1-191 hr files task 8-end
        public void DeafultMailBox(string Email);
        List<CompanyDetails> GetKPIScheduleDeafultMailbox();
        List<ClientSite> GetClientSitesWithTypeId(int[] typeId);
        public List<RCLinkedDuressMaster> GetAllRCLinkedDuress();
        public RCLinkedDuressMaster GetRCLinkedDuressById(int duressId);
        public void DeleteRCLinkedDuress(int id);

        public void SaveRCLinkedDuress(RCLinkedDuressMaster linkedDuress, bool updateClientSites = false);

        public bool CheckAlreadyExistTheGroupName(RCLinkedDuressMaster linkedDuress, bool updateClientSites = false);
        List<HRGroups> GetHRGroups();
        List<UserClientSiteAccess> GetUserClientSiteAccess(int? userId);

        List<ClientSiteKpiSettingsCustomDropboxFolder> GetKpiSettingsCustomDropboxFolder(int clientSiteId);
        void SaveKpiSettingsCustomDropboxFolder(ClientSiteKpiSettingsCustomDropboxFolder record);
        void DeleteKpiSettingsCustomDropboxFolder(int id);
        void SaveKpiDropboxSettings(KpiDropboxSettings record);

        void DroboxDir(string DroboxDir);
        GlobalComplianceAlertEmail GetEmail();
        public DropboxDirectory GetDropboxDir();

        public List<ClientSiteRadioChecksActivityStatus_History> GetClientSiteFunsionLogBooks(int clientSiteId, LogBookType type, DateTime fromDate, DateTime toDate);
        public string GetKeyVehiclogWithProviders(string providerName);

        public string GetGuardlogName(int GuardID, DateTime enddate);
        public string GetGuardlogSite(int GuardID, DateTime enddate);
        public List<GuardLogin> GetLoginDetailsGuard(int GuardID, DateTime startdate, DateTime enddate);
        public void TimesheetSave(string weekname, string time, string mailid, string dropbox);
        public TimeSheet GetTimesheetDetails();

        public List<ClientSite> GetClientSitesUsingLoginUser(int? userId, string searchTerm);
        public string CheckRulesOneinKpiManningInput(ClientSiteKpiSetting settings);
        public string CheckRulesTwoinKpiManningInput(ClientSiteKpiSetting settings);


        public string GetGuardLicenseNo(int GuardID, DateTime enddate);


        public string GetContractedManningDetailsForSpecificSite(string siteName);



        List<ClientSite> GetClientSiteDetailsWithId(int clientSiteIds);

        public StaffDocument GetStaffDocById(int documentId);

        public void SaveSiteLogUploadHistory(SiteLogUploadHistory siteLogUploadHistory);

        public void UpdateClientSiteStatus(int clientSiteId, DateTime? updateDatetime, int status, int kpiSettingsid, int? KPITelematicsFieldID);
        public List<GuardLogin> GetClientSiteGuradLoginDetails(int clientSiteId);
        public List<GuardLogin> GetGuardDetailsAll(int[] clientSiteIds, string startdate, string endDate);
        public GuardLogin GetGuardDetailsAllTimesheet(int clientSiteIds, string startdate, string endDate);
        public GuardLogin GetGuardDetailsAllTimesheet1(int clientSiteIds, DateTime startdate, DateTime endDate);

        public List<GuardLogin> GetGuardDetailsAllTimesheetList(int[] clientSiteIds, DateTime startdate, DateTime endDate);
        //p1-287 A to E-start
         List<LanguageMaster> GetLanguages();
        //p1-287 A to E-end
        public List<ClientSiteSmartWand> GetClientSmartWand();
        public IncidentReport GetLastIncidentReportsByGuardId( int guardId);
        public KPITelematicsField GetKPITelematicsDetails(int? Id);

        public string ValidDateTimeADHOC(ClientSiteKpiSetting setting);

        public string CheckRulesOneinKpiManningInputADHOC(ClientSiteKpiSetting settings);
        public string CheckRulesTwoinKpiManningInputADHOC(ClientSiteKpiSetting settings);
        public int SaveClientSiteManningKpiSettingADHOC(ClientSiteKpiSetting setting);

    }

    public class ClientDataProvider : IClientDataProvider
    {
        private readonly CityWatchDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public ClientDataProvider(IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            CityWatchDbContext context)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }
        //code added to get Guard Access
        public List<GuardAccess> GetGuardAccess()
        {
            return _context.GuardAccess.OrderBy(x => x.Id).ToList();
        }
        //code added to search client name
        public List<ClientSite> GetUserClientSites(string type, string searchTerm)
        {
            var clientSites = GetClientSites(null)
                .Where(z => (string.IsNullOrEmpty(type) || z.ClientType.Name.Equals(type)) &&
                            (string.IsNullOrEmpty(searchTerm) || z.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return clientSites;
        }
        public List<ClientType> GetClientTypes()
        {
            return _context.ClientTypes.Where(x => x.IsActive == true).OrderBy(x => x.Name).ToList();
        }

        //code added to PSPF Dropdown start
        public List<IncidentReportPSPF> GetPSPF()
        {
            return _context.IncidentReportPSPF.OrderBy(z => z.ReferenceNo).ToList();
        }
        //code added to PSPF Dropdown stop
        public void SaveClientType(ClientType clientType)
        {
            if (clientType == null)
                throw new ArgumentNullException();

            if (clientType.Id == -1)
            {
                _context.ClientTypes.Add(new ClientType() { Name = clientType.Name, IsActive = true });
            }
            else
            {
                var clientTypeToUpdate = _context.ClientTypes.SingleOrDefault(x => x.Id == clientType.Id);
                if (clientTypeToUpdate == null)
                    throw new InvalidOperationException();

                clientTypeToUpdate.Name = clientType.Name;
            }
            _context.SaveChanges();
        }

        public void DeleteClientType(int id)
        {
            if (id == -1)
                return;

            var clientTypeToDelete = _context.ClientTypes.SingleOrDefault(x => x.Id == id);
            if (clientTypeToDelete == null)
                throw new InvalidOperationException();

            clientTypeToDelete.IsActive = false;

            //_context.ClientTypes.Remove(clientTypeToDelete);
            _context.SaveChanges();
        }

        public List<ClientSite> GetClientSites(int? typeId)
        {


            return _context.ClientSites
                .Where(x => (!typeId.HasValue || (typeId.HasValue && x.TypeId == typeId.Value)) && x.IsActive == true)
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();
        }
        public List<ClientSiteSmartWand> GetClientSmartWand()
        {

            return _context.ClientSiteSmartWands
        .ToList();
        }

        public IncidentReportPosition GetClientSitePosition(string Name)
        {
            return _context.IncidentReportPositions.Where(x => x.Name == Name).FirstOrDefault();
        }
        public ClientSite GetClientSitesUsingName(string name)
        {
            return _context.ClientSites
                .Where(x => x.Name.Trim() == name.Trim())
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .FirstOrDefault();
        }
        public List<ClientSite> GetClientSitesUsingGuardId(int? GuardId)
        {
            return _context.GuardLogins
           .Where(z => z.GuardId == GuardId)
           .Select(z => z.ClientSite)
           .Distinct()
           .ToList();

        }


        public List<ClientSite> GetClientSitesUsingLoginUser(int? userId, string searchTerm)
        {
            var userAccessSitelist = _context.UserClientSiteAccess
            .Where(z => z.UserId == userId)
           .Select(z => z.ClientSite)
           .Distinct()
           .OrderBy(x => x.Name)
           .ToList();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                userAccessSitelist = userAccessSitelist.Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return userAccessSitelist;

        }

        public void SaveClientSite(ClientSite clientSite)
        {
            if (clientSite == null)
                throw new ArgumentNullException();

            var gpsHasChanged = false;
            /*update the status and kpi settings value start*/
            var kpiSettings = _context.ClientSiteKpiSettings.FirstOrDefault(x => x.ClientSiteId == clientSite.Id);

            if (kpiSettings != null)
            {
                UpdateClientSiteStatus(clientSite.Id, clientSite.StatusDate, clientSite.Status, kpiSettings.Id, kpiSettings.KPITelematicsFieldID);
            }
            /*update the status and kpi settings value end */
            if (clientSite.Id == -1)
            {
                _context.ClientSites.Add(new ClientSite()
                {
                    Name = clientSite.Name,
                    TypeId = clientSite.TypeId,
                    Emails = clientSite.Emails,
                    Address = clientSite.Address,
                    State = clientSite.State,
                    Billing = clientSite.Billing,
                    Gps = clientSite.Gps,
                    Status = clientSite.Status,
                    StatusDate = clientSite.StatusDate,
                    SiteEmail = clientSite.SiteEmail,
                    DuressEmail = clientSite.DuressEmail,
                    DuressSms = clientSite.DuressSms,
                    LandLine = "+61 (3)",
                    DataCollectionEnabled = true,
                    IsActive = true,
                    IsDosDontList= clientSite.IsDosDontList
                });

                gpsHasChanged = !string.IsNullOrEmpty(clientSite.Gps);
            }
            else
            {
                var clientSiteToUpdate = _context.ClientSites.SingleOrDefault(x => x.Id == clientSite.Id);
                if (clientSiteToUpdate == null)
                    throw new InvalidOperationException();

                gpsHasChanged = clientSiteToUpdate.Gps != clientSite.Gps;
                clientSiteToUpdate.Name = clientSite.Name;
                clientSiteToUpdate.Emails = clientSite.Emails;
                clientSiteToUpdate.Address = clientSite.Address;
                clientSiteToUpdate.State = clientSite.State;
                clientSiteToUpdate.Billing = clientSite.Billing;
                clientSiteToUpdate.Gps = clientSite.Gps;
                clientSiteToUpdate.Status = clientSite.Status;
                clientSiteToUpdate.StatusDate = clientSite.StatusDate;
                clientSiteToUpdate.SiteEmail = clientSite.SiteEmail;
                clientSiteToUpdate.DuressSms = clientSite.DuressSms;
                clientSiteToUpdate.DuressEmail = clientSite.DuressEmail;
                clientSiteToUpdate.IsDosDontList = clientSite.IsDosDontList;
            }
            _context.SaveChanges();

            if (gpsHasChanged && !string.IsNullOrEmpty(clientSite.Gps))
                CreateGpsImage(clientSite);




        }

        public void DeleteClientSite(int id)
        {
            if (id == -1)
                return;

            var clientSiteToDelete = _context.ClientSites.SingleOrDefault(x => x.Id == id);
            if (clientSiteToDelete == null)
                throw new InvalidOperationException();


            //_context.ClientSites.Remove(clientSiteToDelete);
            clientSiteToDelete.IsActive = false;
            _context.SaveChanges();
        }

        public List<ClientSiteKpiSetting> GetClientSiteKpiSettings()
        {
            return _context.ClientSiteKpiSettings
                .Where(x => x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.Notes)
                .ToList();
        }


        public ClientSiteKpiSetting GetClientSiteKpiSetting(int clientSiteId)
        {
            var clientSiteKpiSetting = _context.ClientSiteKpiSettings
                .Where(x => x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSiteDayKpiSettings)
                .Include(x => x.Notes)
                 .Include(x => x.RCActionList)
                .SingleOrDefault(x => x.ClientSiteId == clientSiteId);
            if (clientSiteKpiSetting != null)
            {



                clientSiteKpiSetting.ClientSiteManningGuardKpiSettings = _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == clientSiteKpiSetting.Id).OrderBy(x => x.OrderId).ThenBy(x => ((int)x.WeekDay + 6) % 7).ToList();
                clientSiteKpiSetting.ClientSiteManningGuardKpiSettingsADHOC = _context.ClientSiteManningKpiSettingsADHOC.Where(x => x.SettingsId == clientSiteKpiSetting.Id).OrderBy(x => x.OrderId).ThenBy(x => ((int)x.WeekDay + 6) % 7).ToList();

            }
            return clientSiteKpiSetting;
        }
        public List<GuardLogin> GetLoginDetailsGuard(int GuardID, DateTime startdate, DateTime enddate)
        {
            return _context.GuardLogins
        .Where(gl => gl.GuardId == GuardID && gl.LoginDate.Date >= startdate.Date && gl.LoginDate.Date <= enddate.Date)
        .Include(x => x.ClientSite)
        .ToList();


        }
        public string GetGuardlogName(int GuardID, DateTime enddate)
        {
            var TimesheetName = "";
            var guardLogin = _context.GuardLogins
                .Include(x => x.Guard)
     .FirstOrDefault(x => x.GuardId == GuardID);

            if (guardLogin != null && guardLogin.Guard != null)

            {
                return guardLogin.Guard.Name;
            }
            else
            {
                return "";
            }
            //if (guardLogin!=null)
            //{
            //    if (guardLogin.SmartWandId != null)
            //    {
            //        var Name = _context.ClientSiteSmartWands.Where(x => x.Id == guardLogin.SmartWandId).FirstOrDefault();
            //        TimesheetName = Name.SmartWandId;
            //    }
            //    else
            //    {
            //        var Name = _context.IncidentReportPositions.Where(x => x.Id == guardLogin.PositionId).FirstOrDefault();
            //        TimesheetName = Name.Name;
            //    }
            //}

            //return TimesheetName;


        }
        public string GetGuardLicenseNo(int GuardID, DateTime enddate)
        {

            var guardLogin = _context.GuardLogins
                .Include(x => x.Guard)
     .FirstOrDefault(x => x.GuardId == GuardID);

            if (guardLogin != null && guardLogin.Guard != null)
            {
                return guardLogin.Guard.SecurityNo;
            }
            else
            {
                return "";
            }
        }
        public string GetGuardlogSite(int GuardID, DateTime enddate)
        {
            var SiteName = "";
            var guardLogin = _context.GuardLogins
     .FirstOrDefault(x => x.GuardId == GuardID && x.LoginDate.Date == enddate.Date);
            if (guardLogin != null)
            {
                var SiteName1 = _context.ClientSites.Where(x => x.Id == guardLogin.ClientSiteId).FirstOrDefault();
                SiteName = SiteName1.Name;
            }
            return SiteName;
        }
        public List<ClientSiteKpiSetting> GetClientSiteKpiSetting(int[] clientSiteIds)
        {
            var clientSiteKpiSetting = _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSiteDayKpiSettings)
                .Where(x => clientSiteIds.Contains(x.ClientSiteId) && x.ClientSite.IsActive == true)

                .ToList();

            return clientSiteKpiSetting;
        }

        public List<ClientSite> GetClientSiteDetails(int[] clientSiteIds)
        {
            var clientSiteDetails = _context.ClientSites
                .Where(x => clientSiteIds.Contains(x.Id))
                .ToList();
            return clientSiteDetails;
        }

        public void SaveClientSiteKpiSetting(ClientSiteKpiSetting setting)
        {
            var entityState = !_context.ClientSiteKpiSettings.Any(x => x.ClientSiteId == setting.ClientSiteId) ? EntityState.Added : EntityState.Modified;

            if (entityState != EntityState.Modified)
            {
                setting.TimezoneString = "AUS Eastern Standard Time";
                setting.UTC = "+10:00";
                _context.ClientSiteKpiSettings.Attach(setting);
                _context.Entry(setting).State = entityState;
                _context.SaveChanges();
            }

            if (entityState == EntityState.Modified)
            {
                _context.ClientSiteDayKpiSettings.UpdateRange(setting.ClientSiteDayKpiSettings);
                _context.SaveChanges();
            }
        }

        public ClientSiteKpiNote GetClientSiteKpiNote(int id)
        {
            return _context.ClientSiteKpiNotes.SingleOrDefault(z => z.Id == id);
        }

        public int SaveClientSiteKpiNote(ClientSiteKpiNote note)
        {
            if (note.Id == 0)
            {
                _context.ClientSiteKpiNotes.Add(note);
            }
            else
            {
                var noteToUpdate = _context.ClientSiteKpiNotes.SingleOrDefault(z => z.Id == note.Id);
                if (noteToUpdate != null)
                {
                    noteToUpdate.Notes = note.Notes;
                    noteToUpdate.HRRecords = note.HRRecords;
                }
            }
            _context.SaveChanges();
            return note.Id;
        }
        public int SaveClientSiteKpiRC(RCActionList RCList)
        {
            if (RCList.Id == 0)
            {
                _context.RCActionList.Add(RCList);
            }
            else
            {
                var noteToUpdate = _context.RCActionList.SingleOrDefault(z => z.Id == RCList.Id);
                if (noteToUpdate != null)
                {
                    noteToUpdate.SiteAlarmKeypadCode = RCList.SiteAlarmKeypadCode;
                    noteToUpdate.Action1 = RCList.Action1;
                    noteToUpdate.Sitephysicalkey = RCList.Sitephysicalkey;
                    noteToUpdate.Action2 = RCList.Action2;
                    noteToUpdate.SiteCombinationLook = RCList.SiteCombinationLook;
                    noteToUpdate.Action3 = RCList.Action3;
                    noteToUpdate.ControlRoomOperator = RCList.ControlRoomOperator;
                    noteToUpdate.Action4 = RCList.Action4;
                    noteToUpdate.Action5 = RCList.Action5;
                }
            }
            _context.SaveChanges();
            return RCList.Id;
        }
        public RCActionList GetClientSiteKpiRC(int id)
        {
            return _context.RCActionList.SingleOrDefault(z => z.Id == id);
        }


        public int SaveRCList(RCActionList RC)
        {
            if (RC.Id == 0)
            {
                _context.RCActionList.Add(RC);
            }
            else
            {


                var RCToUpdate = _context.RCActionList.SingleOrDefault(z => z.Id == RC.Id);
                if (RCToUpdate != null)
                {
                    RCToUpdate.SiteAlarmKeypadCode = RC.SiteAlarmKeypadCode;
                    RCToUpdate.Action1 = RC.Action1;
                    RCToUpdate.Sitephysicalkey = RC.Sitephysicalkey;
                    RCToUpdate.Action2 = RC.Action2;
                    RCToUpdate.SiteCombinationLook = RC.SiteCombinationLook;
                    RCToUpdate.Action3 = RC.Action3;
                    RCToUpdate.ControlRoomOperator = RC.ControlRoomOperator;
                    RCToUpdate.Action4 = RC.Action4;
                    RCToUpdate.Action5 = RC.Action5;
                    RCToUpdate.Imagepath = RC.Imagepath;
                    RCToUpdate.DateandTimeUpdated = RC.DateandTimeUpdated;
                    RCToUpdate.ClientSiteID = RC.ClientSiteID;
                    RCToUpdate.IsRCBypass = RC.IsRCBypass;
                }
            }
            _context.SaveChanges();
            return RC.Id;
        }

        public void RemoveRCList(int rcListId)
        {
            var RCToUpdate = _context.RCActionList.SingleOrDefault(z => z.Id == rcListId);
            if (RCToUpdate != null)
            {
                _context.Remove(RCToUpdate);
                _context.SaveChanges();
            }
        }

        public void SaveUpdateRCListFile(int Id, string fileName, DateTime dtm)
        {
            var RCToUpdate = _context.RCActionList.SingleOrDefault(z => z.Id == Id);
            if (RCToUpdate != null)
            {
                string sdtm = dtm.Year.ToString() + "-" + dtm.Month.ToString("00") + "-" + dtm.Day.ToString("00") + " " + dtm.Hour.ToString("00") + ":" + dtm.Minute.ToString("00") + ":" + dtm.Second.ToString("00");
                RCToUpdate.Imagepath = fileName;
                RCToUpdate.DateandTimeUpdated = sdtm;
                _context.SaveChanges();
            }

        }


        public void RemoveWorker(int settingsId, int OrderId)
        {

            var ManningWorker = _context.ClientSiteManningKpiSettings.Where(z => z.SettingsId == settingsId && z.OrderId == OrderId).ToList();
            if (ManningWorker != null)
            {

                if (ManningWorker.Count > 0)
                {
                    _context.RemoveRange(ManningWorker);
                    _context.SaveChanges();

                }
            }



        }


        public void RemoveWorkerADHOC(int settingsId, int OrderId)
        {

            var ManningWorker = _context.ClientSiteManningKpiSettingsADHOC.Where(z => z.SettingsId == settingsId && z.OrderId == OrderId).ToList();
            if (ManningWorker != null)
            {

                if (ManningWorker.Count > 0)
                {
                    _context.RemoveRange(ManningWorker);
                    _context.SaveChanges();

                }
            }



        }

        public List<ClientSiteLogBook> GetClientSiteLogBooks()
        {
            return _context.ClientSiteLogBooks
                .Where(x => x.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
        }

        public List<ClientSiteLogBook> GetClientSiteLogBooks(int clientSiteId, LogBookType type, DateTime fromDate, DateTime toDate)
        {
            return _context.ClientSiteLogBooks
                .Where(z => z.ClientSiteId == clientSiteId && z.Type == type && z.Date >= fromDate && z.Date <= toDate)
                .ToList();
        }

        public ClientSiteLogBook GetClientSiteLogBook(int clientSiteId, LogBookType type, DateTime date)
        {
            return _context.ClientSiteLogBooks
                 .SingleOrDefault(z => z.ClientSiteId == clientSiteId && z.Type == type && z.Date == date);
        }
        public ClientSite GetClientSiteName(int clientSiteId)
        {
            return _context.ClientSites
                      .SingleOrDefault(z => z.Id == clientSiteId);

        }
        public Guard GetGuradName(int guardId)
        {
            return _context.Guards
                     .SingleOrDefault(z => z.Id == guardId);

        }
        public List<GlobalDuressEmail> GetGlobalDuressEmail()
        {
            return _context.GlobalDuressEmail.ToList();
        }
        public List<GlobalComplianceAlertEmail> GetGlobalComplianceAlertEmail()
        {
            return _context.GlobalComplianceAlertEmail.ToList();
        }
        //public List<KPIScheduleDeafultMailbox> GetKPIScheduleDeafultMailbox()
        //{
        //    return _context.KPIScheduleDeafultMailbox.ToList();
        //}
        public List<CompanyDetails> GetKPIScheduleDeafultMailbox()
        {
            return _context.CompanyDetails.ToList();
        }
        public List<ClientSiteLogBook> GetClientSiteLogBookWithOutType(int clientSiteId, DateTime date)
        {
            return _context.ClientSiteLogBooks
                 .Where(z => z.ClientSiteId == clientSiteId && z.Date == date).ToList();
        }
        public int SaveClientSiteLogBook(ClientSiteLogBook logBook)
        {
            if (logBook.Id == 0)
            {
                _context.ClientSiteLogBooks.Add(logBook);
            }
            else
            {
                var logBookToUpdate = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBook.Id);
                if (logBookToUpdate != null)
                {
                    // nothing to update
                }
            }
            _context.SaveChanges();

            return logBook.Id;
        }

        public void MarkClientSiteLogBookAsUploaded(int logBookId, string fileName)
        {
            var logBookToUpdate = _context.ClientSiteLogBooks.SingleOrDefault(z => z.Id == logBookId);
            if (logBookToUpdate != null)
            {
                logBookToUpdate.DbxUploaded = true;
                logBookToUpdate.FileName = fileName;
                _context.SaveChanges();
            }
        }

        public void SetDataCollectionStatus(int clientSiteId, bool enabled)
        {
            var clientSite = _context.ClientSites.SingleOrDefault(z => z.Id == clientSiteId);
            if (clientSite != null)
            {
                clientSite.DataCollectionEnabled = enabled;
                _context.SaveChanges();
            }
        }

        private void CreateGpsImage(ClientSite clientSite)
        {
            string gpsImageDir = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "GpsImage");
            var mapSettings = _configuration.GetSection("GoogleMap").Get(typeof(GoogleMapSettings)) as GoogleMapSettings;
            try
            {
                GoogleMapHelper.DownloadGpsImage(gpsImageDir, clientSite, mapSettings);
            }
            catch
            {

            }
        }


        public void SaveCompanyDetails(CompanyDetails companyDetails)
        {
            if (companyDetails.Id == 0)
            {
                companyDetails.LastUploaded = DateTime.Now;
                companyDetails.PrimaryLogoUploadedOn = DateTime.Now;
                companyDetails.HomePageMessageUploadedOn = DateTime.Now;
                companyDetails.BannerMessageUploadedOn = DateTime.Now;
                companyDetails.EmailMessageUploadedOn = DateTime.Now;

                _context.CompanyDetails.Add(companyDetails);
            }
            else
            {
                var templateToUpdate = _context.CompanyDetails.SingleOrDefault(x => x.Id == companyDetails.Id);
                if (templateToUpdate != null)
                {
                    templateToUpdate.Name = companyDetails.Name;
                    templateToUpdate.Domain = companyDetails.Domain;
                    templateToUpdate.LastUploaded = DateTime.Now;
                    templateToUpdate.PrimaryLogoUploadedOn = DateTime.Now;
                    templateToUpdate.PrimaryLogoPath = companyDetails.PrimaryLogoPath;
                    templateToUpdate.HomePageMessage = companyDetails.HomePageMessage;
                    templateToUpdate.HomePageMessage2 = companyDetails.HomePageMessage2;
                    templateToUpdate.MessageBarColour = companyDetails.MessageBarColour;
                    templateToUpdate.HomePageMessageUploadedOn = DateTime.Now;
                    templateToUpdate.BannerLogoPath = companyDetails.BannerLogoPath;
                    templateToUpdate.BannerMessage = companyDetails.BannerMessage;
                    templateToUpdate.BannerMessageUploadedOn = DateTime.Now;
                    templateToUpdate.Hyperlink = companyDetails.Hyperlink;
                    templateToUpdate.EmailMessage = companyDetails.EmailMessage;
                    templateToUpdate.EmailMessageUploadedOn = DateTime.Now;
                    //p1-225 Core Settings-start
                    templateToUpdate.HyperlinkColour = companyDetails.HyperlinkColour;
                    templateToUpdate.HyperlinkLabel = companyDetails.HyperlinkLabel;
                    templateToUpdate.LogoHyperlink = companyDetails.LogoHyperlink;
                    templateToUpdate.ApiProvider = companyDetails.ApiProvider;
                    templateToUpdate.ApiSecretkey = companyDetails.ApiSecretkey;
                    //p1-225 Core Settings-end
                }

            }
            _context.SaveChanges();
        }

        public void SaveCompanyMailDetails(CompanyDetails companyDetails)
        {
            if (companyDetails.Id != 0)
            {
                var templateToUpdate = _context.CompanyDetails.SingleOrDefault(x => x.Id == companyDetails.Id);
                if (templateToUpdate != null)
                {
                    templateToUpdate.IRMail = companyDetails.IRMail;
                    templateToUpdate.KPIMail = companyDetails.KPIMail;
                    templateToUpdate.FusionMail = companyDetails.FusionMail;
                    templateToUpdate.TimesheetsMail = companyDetails.TimesheetsMail;

                }
            }
           
            _context.SaveChanges();
        }

        /// <summary>
        /// For save and update ClientSite Manning details
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        public int SaveClientSiteManningKpiSetting(ClientSiteKpiSetting setting)
        {
            var success = 0;
            try
            {

                if (setting != null)
                {

                    //update the value of the ScheduleisActive Start

                    try
                    {
                        _context.ClientSiteKpiSettings
                        .Where(u => u.Id == setting.Id)
                         .ExecuteUpdate(b => b.SetProperty(u => u.ScheduleisActive, setting.ScheduleisActive)
                         );
                        _context.ClientSiteKpiSettings
                      .Where(u => u.Id == setting.Id)
                       .ExecuteUpdate(b => b.SetProperty(u => u.KPITelematicsFieldID, setting.KPITelematicsFieldID)
                       );
                        _context.ClientSiteKpiSettings
                      .Where(u => u.Id == setting.Id)
                       .ExecuteUpdate(b => b.SetProperty(u => u.TimezoneString, setting.TimezoneString)
                       );

                        TimeZoneInfo westernAustraliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(setting.TimezoneString);
                        string offset = westernAustraliaTimeZone.BaseUtcOffset.ToString(@"hh\:mm");
                        string sign = westernAustraliaTimeZone.BaseUtcOffset.Hours >= 0 ? "+" : "-";
                        string utcOffset = $"{sign}{offset}";
                        _context.ClientSiteKpiSettings
                     .Where(u => u.Id == setting.Id)
                      .ExecuteUpdate(b => b.SetProperty(u => u.UTC, utcOffset)
                      );

                    }
                    catch
                    {


                    }
                    //update the value of the ScheduleisActive end

                    if (setting.ClientSiteManningGuardKpiSettings.Any() || setting.ClientSiteManningPatrolCarKpiSettings.Any())
                    {

                        if (setting.ClientSiteManningPatrolCarKpiSettings.Count > 0)
                        {
                            if (setting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.DefaultValue == false).Count() > 0)
                            {
                                var positionIdPatrolCar = setting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                                var CrmSupplier = setting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.CrmSupplier != null).FirstOrDefault();
                                //set the values for SettingsId and PositionId                       
                                if (positionIdPatrolCar != null)
                                {
                                    int? maxId = _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == setting.Id).Any() ? _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == setting.Id).Max(item => item.OrderId) : 0;

                                    setting.ClientSiteManningPatrolCarKpiSettings.ForEach(x => { x.SettingsId = setting.Id; x.PositionId = positionIdPatrolCar.PositionId; x.OrderId = maxId + 1; });
                                    if (CrmSupplier != null)
                                    {
                                        setting.ClientSiteManningPatrolCarKpiSettings.ForEach(x => { x.CrmSupplier = CrmSupplier.CrmSupplier; });

                                    }
                                    if (setting.ClientSiteManningPatrolCarKpiSettings.Any())
                                    {
                                        _context.ClientSiteManningKpiSettings.AddRange(setting.ClientSiteManningPatrolCarKpiSettings);
                                        _context.SaveChanges();
                                        success = 1;
                                    }

                                }


                            }

                        }

                        if (setting.ClientSiteManningGuardKpiSettings.Count > 0)
                        {
                            if (setting.ClientSiteManningGuardKpiSettings.Where(x => x.DefaultValue == false).Count() > 0)
                            {
                                var positionIdGuard = setting.ClientSiteManningGuardKpiSettings.Where(x => x.PositionId != 0).ToList();

                                if (positionIdGuard != null)
                                {
                                    foreach (var poId in positionIdGuard)
                                    {
                                        if (poId != null)
                                        {
                                            var CrmSupplier = setting.ClientSiteManningGuardKpiSettings.Where(x => x.CrmSupplier != null && x.OrderId == poId.OrderId).FirstOrDefault();
                                            if (CrmSupplier != null)
                                            {
                                                setting.ClientSiteManningGuardKpiSettings.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.CrmSupplier = CrmSupplier.CrmSupplier; });

                                            }

                                            setting.ClientSiteManningGuardKpiSettings.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.SettingsId = setting.Id; x.PositionId = poId.PositionId; });
                                        }

                                    }

                                    if (setting.ClientSiteManningGuardKpiSettings.Any() && setting.ClientSiteManningGuardKpiSettings != null)
                                    {
                                        _context.ClientSiteManningKpiSettings.UpdateRange(setting.ClientSiteManningGuardKpiSettings);
                                        _context.SaveChanges();
                                        success = 1;
                                    }
                                }
                                else
                                {

                                    return 3;
                                }

                            }


                        }
                    }


                   
                }

            }
            catch
            {
                return success;
            }
            return success;
        }



        public int SaveClientSiteManningKpiSettingADHOC(ClientSiteKpiSetting setting)
        {
            var success = 0;
            try
            {

                if (setting != null)
                {

                    //update the value of the ScheduleisActive Start

                    try
                    {
                        _context.ClientSiteKpiSettings
                        .Where(u => u.Id == setting.Id)
                         .ExecuteUpdate(b => b.SetProperty(u => u.ScheduleisActiveADHOC, setting.ScheduleisActiveADHOC)
                         );

                     //   _context.ClientSiteKpiSettings
                     // .Where(u => u.Id == setting.Id)
                     //  .ExecuteUpdate(b => b.SetProperty(u => u.TimezoneString, setting.TimezoneString)
                     //  );

                     //   TimeZoneInfo westernAustraliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(setting.TimezoneString);
                     //   string offset = westernAustraliaTimeZone.BaseUtcOffset.ToString(@"hh\:mm");
                     //   string sign = westernAustraliaTimeZone.BaseUtcOffset.Hours >= 0 ? "+" : "-";
                     //   string utcOffset = $"{sign}{offset}";
                     //   _context.ClientSiteKpiSettings
                     //.Where(u => u.Id == setting.Id)
                     // .ExecuteUpdate(b => b.SetProperty(u => u.UTC, utcOffset)
                     // );

                    }
                    catch
                    {


                    }
                    //update the value of the ScheduleisActive end

                    //update the values of ADHOC 01012025

                    if (setting.ClientSiteManningGuardKpiSettingsADHOC.Any() || setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Any())
                    {

                        if (setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Count > 0)
                        {
                            if (setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.DefaultValue == false).Count() > 0)
                            {
                                var positionIdPatrolCar = setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.PositionId != 0).FirstOrDefault();
                                var CrmSupplier = setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.CrmSupplier != null).FirstOrDefault();
                                var WeekAdhocToBeValid = setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.WeekAdhocToBeValid != null).FirstOrDefault();
                                var IsExtraShiftEnabled = setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Where(x => x.IsExtraShiftEnabled != false).FirstOrDefault();

                                //set the values for SettingsId and PositionId                       
                                if (positionIdPatrolCar != null)
                                {
                                    int? maxId = _context.ClientSiteManningKpiSettingsADHOC.Where(x => x.SettingsId == setting.Id).Any() ? _context.ClientSiteManningKpiSettingsADHOC.Where(x => x.SettingsId == setting.Id).Max(item => item.OrderId) : 0;

                                    setting.ClientSiteManningPatrolCarKpiSettingsADHOC.ForEach(x => { x.SettingsId = setting.Id; x.PositionId = positionIdPatrolCar.PositionId; x.OrderId = maxId + 1; });
                                    if (CrmSupplier != null)
                                    {
                                        setting.ClientSiteManningPatrolCarKpiSettingsADHOC.ForEach(x => { x.CrmSupplier = CrmSupplier.CrmSupplier; });

                                    }
                                    if (WeekAdhocToBeValid != null)
                                    {
                                        setting.ClientSiteManningPatrolCarKpiSettingsADHOC.ForEach(x => { x.WeekAdhocToBeValid = WeekAdhocToBeValid.WeekAdhocToBeValid; });

                                    }

                                    if (IsExtraShiftEnabled != null)
                                    {
                                        setting.ClientSiteManningPatrolCarKpiSettingsADHOC.ForEach(x => { x.IsExtraShiftEnabled = IsExtraShiftEnabled.IsExtraShiftEnabled; });

                                    }
                                    if (setting.ClientSiteManningPatrolCarKpiSettingsADHOC.Any())
                                    {
                                        _context.ClientSiteManningKpiSettingsADHOC.AddRange(setting.ClientSiteManningPatrolCarKpiSettingsADHOC);
                                        _context.SaveChanges();
                                        success = 1;
                                    }

                                }


                            }

                        }

                        if (setting.ClientSiteManningGuardKpiSettingsADHOC.Count > 0)
                        {
                            if (setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.DefaultValue == false).Count() > 0)
                            {
                                var positionIdGuard = setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.PositionId != 0).ToList();

                                if (positionIdGuard != null)
                                {
                                    foreach (var poId in positionIdGuard)
                                    {
                                        if (poId != null)
                                        {
                                            var CrmSupplier = setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.CrmSupplier != null && x.OrderId == poId.OrderId).FirstOrDefault();
                                            var WeekAdhocToBeValid = setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.WeekAdhocToBeValid != null && x.OrderId == poId.OrderId).FirstOrDefault();
                                            var IsExtraShiftEnabled = setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.IsExtraShiftEnabled != false && x.OrderId == poId.OrderId).FirstOrDefault();

                                            if (CrmSupplier != null)
                                            {
                                                setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.CrmSupplier = CrmSupplier.CrmSupplier; });

                                            }
                                            if (WeekAdhocToBeValid != null)
                                            {
                                                setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.WeekAdhocToBeValid = WeekAdhocToBeValid.WeekAdhocToBeValid; });

                                            }

                                            if (IsExtraShiftEnabled != null)
                                            {
                                                setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.IsExtraShiftEnabled = IsExtraShiftEnabled.IsExtraShiftEnabled; });

                                            }

                                            setting.ClientSiteManningGuardKpiSettingsADHOC.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.SettingsId = setting.Id; x.PositionId = poId.PositionId; });
                                        }

                                    }

                                    if (setting.ClientSiteManningGuardKpiSettingsADHOC.Any() && setting.ClientSiteManningGuardKpiSettingsADHOC != null)
                                    {
                                        _context.ClientSiteManningKpiSettingsADHOC.UpdateRange(setting.ClientSiteManningGuardKpiSettingsADHOC);
                                        _context.SaveChanges();
                                        success = 1;
                                    }
                                }
                                else
                                {

                                    return 3;
                                }

                            }


                        }
                    }



                }

            }
            catch
            {
                return success;
            }
            return success;
        }

        public string ValidDateTime(ClientSiteKpiSetting setting)
        {
            var InvalidInputs = string.Empty;
            if (setting != null)
            {
                foreach (var listItem in setting.ClientSiteManningGuardKpiSettings)
                {
                    if (listItem.EmpHoursStart != null)
                    {
                        if (!IsValidTimeFormat(listItem.EmpHoursStart))
                        {
                            InvalidInputs = InvalidInputs + listItem.EmpHoursStart + ",";

                        }

                    }
                    if (listItem.EmpHoursEnd != null)
                    {
                        if (!IsValidTimeFormat(listItem.EmpHoursEnd))
                        {
                            InvalidInputs = InvalidInputs + listItem.EmpHoursEnd + ",";

                        }

                    }



                }

                foreach (var listItem2 in setting.ClientSiteManningPatrolCarKpiSettings)
                {
                    if (listItem2.EmpHoursStart != null)
                    {
                        if (!IsValidTimeFormat(listItem2.EmpHoursStart))
                        {
                            InvalidInputs = InvalidInputs + listItem2.EmpHoursStart + ",";

                        }

                    }
                    if (listItem2.EmpHoursEnd != null)
                    {
                        if (!IsValidTimeFormat(listItem2.EmpHoursStart))
                        {
                            InvalidInputs = InvalidInputs + listItem2.EmpHoursStart + ",";

                        }

                        if (!IsValidTimeFormat(listItem2.EmpHoursEnd))
                        {
                            InvalidInputs = InvalidInputs + listItem2.EmpHoursEnd + ",";

                        }

                    }

                }

            }
            return InvalidInputs.TrimEnd(',');
        }



        public string ValidDateTimeADHOC(ClientSiteKpiSetting setting)
        {
            var InvalidInputs = string.Empty;
            if (setting != null)
            {
                foreach (var listItem in setting.ClientSiteManningGuardKpiSettingsADHOC)
                {
                    if (listItem.EmpHoursStart != null)
                    {
                        if (!IsValidTimeFormat(listItem.EmpHoursStart))
                        {
                            InvalidInputs = InvalidInputs + listItem.EmpHoursStart + ",";

                        }

                    }
                    if (listItem.EmpHoursEnd != null)
                    {
                        if (!IsValidTimeFormat(listItem.EmpHoursEnd))
                        {
                            InvalidInputs = InvalidInputs + listItem.EmpHoursEnd + ",";

                        }

                    }



                }

                foreach (var listItem2 in setting.ClientSiteManningPatrolCarKpiSettingsADHOC)
                {
                    if (listItem2.EmpHoursStart != null)
                    {
                        if (!IsValidTimeFormat(listItem2.EmpHoursStart))
                        {
                            InvalidInputs = InvalidInputs + listItem2.EmpHoursStart + ",";

                        }

                    }
                    if (listItem2.EmpHoursEnd != null)
                    {
                        if (!IsValidTimeFormat(listItem2.EmpHoursStart))
                        {
                            InvalidInputs = InvalidInputs + listItem2.EmpHoursStart + ",";

                        }

                        if (!IsValidTimeFormat(listItem2.EmpHoursEnd))
                        {
                            InvalidInputs = InvalidInputs + listItem2.EmpHoursEnd + ",";

                        }

                    }

                }

            }
            return InvalidInputs.TrimEnd(',');
        }

        public bool IsValidTimeFormat(string input)
        {
            TimeSpan inputTimeSpan;
            if (!TimeSpan.TryParseExact(input, "hh\\:mm", null, out inputTimeSpan))
            {
                return false;
            }
            else
            {
                return TimeSpan.TryParse(input, out inputTimeSpan);
            }


        }

        /*
        ISSUE#131      
        Clocks must be in a range of 00:01 ~ 23:59; there is no 00:00 allowed
        Currently 00:00 is allowed, as is 00:0, etc. minimum value is BLANK or 00:01*/
        public string CheckRulesOneinKpiManningInput(ClientSiteKpiSetting settings)
        {
            var invalidInputs = string.Empty;
            if (settings != null)
            {
                foreach (var listItem in settings.ClientSiteManningGuardKpiSettings)
                {
                    if (!IsValidTime(listItem.EmpHoursStart))
                    {
                        invalidInputs += listItem.EmpHoursStart + ",";
                    }

                    if (!IsValidTime(listItem.EmpHoursEnd))
                    {
                        invalidInputs += listItem.EmpHoursEnd + ",";
                    }
                }

                foreach (var listItem2 in settings.ClientSiteManningPatrolCarKpiSettings)
                {
                    if (!IsValidTime(listItem2.EmpHoursStart))
                    {
                        invalidInputs += listItem2.EmpHoursStart + ",";
                    }

                    if (!IsValidTime(listItem2.EmpHoursEnd))
                    {
                        invalidInputs += listItem2.EmpHoursEnd + ",";
                    }
                }
            }
            return invalidInputs.TrimEnd(',');
        }


        public string CheckRulesOneinKpiManningInputADHOC(ClientSiteKpiSetting settings)
        {
            var invalidInputs = string.Empty;
            if (settings != null)
            {
                foreach (var listItem in settings.ClientSiteManningGuardKpiSettingsADHOC)
                {
                    if (!IsValidTime(listItem.EmpHoursStart))
                    {
                        invalidInputs += listItem.EmpHoursStart + ",";
                    }

                    if (!IsValidTime(listItem.EmpHoursEnd))
                    {
                        invalidInputs += listItem.EmpHoursEnd + ",";
                    }
                }

                foreach (var listItem2 in settings.ClientSiteManningPatrolCarKpiSettingsADHOC)
                {
                    if (!IsValidTime(listItem2.EmpHoursStart))
                    {
                        invalidInputs += listItem2.EmpHoursStart + ",";
                    }

                    if (!IsValidTime(listItem2.EmpHoursEnd))
                    {
                        invalidInputs += listItem2.EmpHoursEnd + ",";
                    }
                }
            }
            return invalidInputs.TrimEnd(',');
        }

        static bool IsValidTime(string time)
        {
            if (string.IsNullOrEmpty(time)) return true; // Assuming null or empty is valid

            TimeSpan inputTimeSpan;
            if (!TimeSpan.TryParse(time, out inputTimeSpan))
            {
                return false;
            }

            TimeSpan startTime = new TimeSpan(0, 1, 0);  // 00:01
            TimeSpan endTime = new TimeSpan(23, 59, 0);  // 23:59

            return inputTimeSpan >= startTime && inputTimeSpan <= endTime;
        }

        /*Rule2
         * Workers must have a value; it can not be blank once there is a clock set Currently
        Blank is allowed even when clocks blank above it or have inputs Blank must only be allowed when clocks blank*/
        public string CheckRulesTwoinKpiManningInput(ClientSiteKpiSetting settings)
        {
            var invalidInputs = string.Empty;
            if (settings != null)
            {
                foreach (var listItem in settings.ClientSiteManningGuardKpiSettings)
                {
                    //if (!IsWorkerValid(listItem.EmpHoursStart, listItem.EmpHoursEnd, listItem.NoOfPatrols))
                    //{
                    //    invalidInputs += $"Invalid worker entry for times {listItem.EmpHoursStart} - {listItem.EmpHoursEnd},";
                    //}

                    if (!AreAllValuesPresent(listItem.EmpHoursStart, listItem.EmpHoursEnd, listItem.NoOfPatrols))
                    {
                        string notnullVlaue = string.Empty;
                        string nullVlaue = string.Empty;
                        invalidInputs += "Incomplete entry for ";
                        if (string.IsNullOrEmpty(listItem.EmpHoursStart))
                        {
                            nullVlaue += " Start : __:__ ";
                        }
                        else
                        {
                            notnullVlaue += " Start :" + listItem.EmpHoursStart;

                        }
                        if (string.IsNullOrEmpty(listItem.EmpHoursEnd))
                        {
                            nullVlaue += " End : __:__ ";
                        }
                        else
                        {
                            notnullVlaue += " End :" + listItem.EmpHoursEnd;
                        }
                        if (listItem.NoOfPatrols == 0 || listItem.NoOfPatrols == null)
                        {
                            nullVlaue += " Worker : _ ";
                        }
                        else
                        {
                            notnullVlaue += " Worker : " + listItem.NoOfPatrols.ToString();
                        }


                        invalidInputs += notnullVlaue + nullVlaue + ", ";
                    }
                }
                foreach (var listItem2 in settings.ClientSiteManningPatrolCarKpiSettings)
                {
                    if (!IsWorkerValid(listItem2.EmpHoursStart, listItem2.EmpHoursEnd, listItem2.NoOfPatrols))
                    {
                        invalidInputs += $"Invalid worker entry for times {listItem2.EmpHoursStart} - {listItem2.EmpHoursEnd},";
                    }
                    if (!AreAllValuesPresent(listItem2.EmpHoursStart, listItem2.EmpHoursEnd, listItem2.NoOfPatrols))
                    {
                        invalidInputs += $"Incomplete entry for times' {listItem2.EmpHoursStart}' - ' {listItem2.EmpHoursEnd} with worker '{listItem2.NoOfPatrols}', ";
                    }
                }




            }
            return invalidInputs.TrimEnd(',');
        }



        public string CheckRulesTwoinKpiManningInputADHOC(ClientSiteKpiSetting settings)
        {
            var invalidInputs = string.Empty;
            if (settings != null)
            {
                foreach (var listItem in settings.ClientSiteManningGuardKpiSettingsADHOC)
                {
                    //if (!IsWorkerValid(listItem.EmpHoursStart, listItem.EmpHoursEnd, listItem.NoOfPatrols))
                    //{
                    //    invalidInputs += $"Invalid worker entry for times {listItem.EmpHoursStart} - {listItem.EmpHoursEnd},";
                    //}

                    if (!AreAllValuesPresent(listItem.EmpHoursStart, listItem.EmpHoursEnd, listItem.NoOfPatrols))
                    {
                        string notnullVlaue = string.Empty;
                        string nullVlaue = string.Empty;
                        invalidInputs += "Incomplete entry for ";
                        if (string.IsNullOrEmpty(listItem.EmpHoursStart))
                        {
                            nullVlaue += " Start : __:__ ";
                        }
                        else
                        {
                            notnullVlaue += " Start :" + listItem.EmpHoursStart;

                        }
                        if (string.IsNullOrEmpty(listItem.EmpHoursEnd))
                        {
                            nullVlaue += " End : __:__ ";
                        }
                        else
                        {
                            notnullVlaue += " End :" + listItem.EmpHoursEnd;
                        }
                        if (listItem.NoOfPatrols == 0 || listItem.NoOfPatrols == null)
                        {
                            nullVlaue += " Worker : _ ";
                        }
                        else
                        {
                            notnullVlaue += " Worker : " + listItem.NoOfPatrols.ToString();
                        }


                        invalidInputs += notnullVlaue + nullVlaue + ", ";
                    }
                }
                foreach (var listItem2 in settings.ClientSiteManningPatrolCarKpiSettingsADHOC)
                {
                    if (!IsWorkerValid(listItem2.EmpHoursStart, listItem2.EmpHoursEnd, listItem2.NoOfPatrols))
                    {
                        invalidInputs += $"Invalid worker entry for times {listItem2.EmpHoursStart} - {listItem2.EmpHoursEnd},";
                    }
                    if (!AreAllValuesPresent(listItem2.EmpHoursStart, listItem2.EmpHoursEnd, listItem2.NoOfPatrols))
                    {
                        invalidInputs += $"Incomplete entry for times' {listItem2.EmpHoursStart}' - ' {listItem2.EmpHoursEnd} with worker '{listItem2.NoOfPatrols}', ";
                    }
                }




            }
            return invalidInputs.TrimEnd(',');
        }

        static bool IsWorkerValid(string startTime, string endTime, int? worker)
        {
            bool timesNotNull = !string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime);
            bool workerNotNull = worker != null;

            if (timesNotNull && !workerNotNull)
            {
                return false;
            }

            if (!timesNotNull && workerNotNull)
            {
                return false;
            }



            return true;
        }

        static bool AreAllValuesPresent(string startTime, string endTime, int? worker)
        {


            bool timesNotNull = !string.IsNullOrEmpty(startTime) || !string.IsNullOrEmpty(endTime);
            if (!string.IsNullOrEmpty(startTime) || !string.IsNullOrEmpty(endTime))
            {
                timesNotNull = true;
            }
            else
            {
                timesNotNull = false;
            }

            bool workerNotNull = worker != null;

            if (timesNotNull && workerNotNull)
                return true;
            else if (!timesNotNull && !workerNotNull)
                return true;
            else
                return false;

            // Check if either all values are present or all are absent
            //return (timesNotNull || workerNotNull) || (!timesNotNull && !workerNotNull);
        }
        public List<ClientSite> GetNewClientSites()
        {
            return _context.ClientSites
                .Where(x => x.IsActive == true)
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();

        }

        public void SavePlateLoaded(IncidentReportsPlatesLoaded report)
        {
            _context.IncidentReportsPlatesLoaded.Add(report);
            _context.SaveChanges();
        }
        public void DeletePlateLoaded(IncidentReportsPlatesLoaded report)
        {






            var platesLoadedToDeleteId = _context.IncidentReportsPlatesLoaded.Where(x => x.LogId == report.LogId && x.PlateId == report.PlateId && x.TruckNo == report.TruckNo && x.IncidentReportId == 0).Max(z => z.Id);
            var platesLoadedToDelete = _context.IncidentReportsPlatesLoaded.SingleOrDefault(x => x.Id == platesLoadedToDeleteId.Value);
            if (platesLoadedToDelete == null)
                throw new InvalidOperationException();

            _context.IncidentReportsPlatesLoaded.Remove(platesLoadedToDelete);
            _context.SaveChanges();
        }

        public void DeleteFullPlateLoaded(IncidentReportsPlatesLoaded report, int Count)
        {

            var platesLoadedToDeleteId = _context.IncidentReportsPlatesLoaded.Where(x => x.LogId == report.LogId && x.IncidentReportId == 0);
            for (int i = 1; i < Count; i++)
            {

                var platesLoadedToDelete = _context.IncidentReportsPlatesLoaded.Where(x => platesLoadedToDeleteId.Select(z => z.Id).Contains(x.Id));
                if (platesLoadedToDelete == null)
                    throw new InvalidOperationException();
                var IdFrom = platesLoadedToDelete.Max(y => y.Id);
                var IdTo = platesLoadedToDelete.SingleOrDefault(x => x.Id == IdFrom);
                _context.IncidentReportsPlatesLoaded.Remove(IdTo);
                _context.SaveChanges();
            }

        }
        public List<IncidentReportsPlatesLoaded> GetIncidentDetailsKvlReport(int logId)
        {
            return _context.IncidentReportsPlatesLoaded
                .Where(z => z.LogId == logId && z.IncidentReportId == 0)
                .ToList();
        }

        public List<KeyVehicleLog> GetKeyVehiclogWithPlateIdAndTruckNoByLogId(int[] plateid, string[] truckNo, int logId)
        {

            return _context.KeyVehicleLogs.Where(z => truckNo.Contains(z.VehicleRego) && plateid.Contains(z.PlateId) && DateTime.Compare(z.ClientSiteLogBook.Date.Date, DateTime.Now.Date) == 0)
                  .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)

                .ToList();


        }
        public List<KeyVehicleLog> GetKeyVehiclogWithPlateIdAndTruckNoByLogIdIndividual(int plateid, string truckNo, int logId)
        {

            return _context.KeyVehicleLogs.Where(z => z.VehicleRego == truckNo && z.PlateId == plateid && DateTime.Compare(z.ClientSiteLogBook.Date.Date, DateTime.Now.Date) == 0)
                  .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)

                .ToList();

        }
        public List<KeyVehcileLogField> GetKeyVehicleLogFields(bool includeDeleted = false)
        {
            return _context.KeyVehcileLogFields
                .Where(x => includeDeleted || !x.IsDeleted)
                .OrderBy(x => x.TypeId)
                .ThenBy(x => x.Name)
                .ToList();
        }
        public int GetMaxIncidentReportId(int LogId)

        {
            var incidentReportid = _context.IncidentReports
                 .Where(z => z.LogId == LogId).Max(x => x.Id);
            return Convert.ToInt32(incidentReportid);


        }
        public List<ClientSiteKey> GetClientSiteKeys(int clientSiteId)
        {
            return _context.ClientSiteKeys
                .Where(z => z.ClientSiteId == clientSiteId && z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderBy(z => z.KeyNo)
                .ToList();
        }

        public List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds)
        {
            return _context.ClientSiteKeys
                .Where(z => clientSiteIds.Contains(z.ClientSiteId) && z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .OrderBy(z => z.KeyNo)
                .ToList();
        }
        public List<ClientSitePoc> GetClientSitePocs()
        {
            return _context.ClientSitePocs
                .Where(z => !z.IsDeleted)
                .OrderBy(z => z.Name)
                .ToList();
        }
        public List<ClientSiteLocation> GetClientSiteLocations()
        {
            return _context.ClientSiteLocations
                .Where(z => !z.IsDeleted)
                .OrderBy(z => z.Name)
                .ToList();
        }

        public List<ClientSiteLogBook> GetClientSiteLogBooks(int? logBookId, LogBookType type)
        {
            return _context.ClientSiteLogBooks
                .Where(z => z.Id == logBookId && z.Type == type && z.ClientSite.IsActive == true)
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .ToList();
        }



        /* Save Client Site Links Page Type*/

        public List<ClientSiteLinksPageType> GetSiteLinksPageTypes()
        {
            return _context.ClientSiteLinksPageType.OrderBy(x => x.PageTypeName).ToList();
        }
        public List<ClientSiteLinksDetails> GetSiteLinksPageDetails(int type)
        {
            return _context.ClientSiteLinksDetails.Where(x => x.ClientSiteLinksTypeId == type).OrderBy(x => x.Title).ToList();

        }
        public int SaveClientSiteLinksPageType(ClientSiteLinksPageType ClientSiteLinksPageTypeRecord)
        {
            int saveStatus = -1;
            if (ClientSiteLinksPageTypeRecord != null)
            {

                if (ClientSiteLinksPageTypeRecord.Id == 0)
                {
                    var ClientSiteLinksPageTypeToUpdate = _context.ClientSiteLinksPageType.SingleOrDefault(x => x.PageTypeName == ClientSiteLinksPageTypeRecord.PageTypeName);

                    if (ClientSiteLinksPageTypeToUpdate == null)
                    {
                        _context.ClientSiteLinksPageType.Add(new ClientSiteLinksPageType() { PageTypeName = ClientSiteLinksPageTypeRecord.PageTypeName });

                        saveStatus = 1;

                    }
                    else
                    {
                        saveStatus = -1;
                    }

                }
                else
                {
                    var ClientSiteLinksPageTypeToUpdate = _context.ClientSiteLinksPageType.SingleOrDefault(x => x.Id == ClientSiteLinksPageTypeRecord.Id);
                    if (ClientSiteLinksPageTypeToUpdate != null)
                    {

                        ClientSiteLinksPageTypeToUpdate.PageTypeName = ClientSiteLinksPageTypeRecord.PageTypeName;
                        saveStatus = 1;
                    }


                }


                _context.SaveChanges();
                if (saveStatus != -1)
                {
                    var lastInsertedId = _context.ClientSiteLinksPageType.SingleOrDefault(x => x.PageTypeName == ClientSiteLinksPageTypeRecord.PageTypeName);
                    saveStatus = lastInsertedId.Id;

                }
            }

            return saveStatus;
        }
        //to save  the feedback type-start
        public int SaveFeedbackType(FeedbackType FeedbackNewTypeRecord)
        {
            int saveStatus = -1;
            if (FeedbackNewTypeRecord != null)
            {

                if (FeedbackNewTypeRecord.Id == 0)
                {
                    var FeedbackNewTypeToUpdate = _context.FeedbackType.SingleOrDefault(x => x.Name == FeedbackNewTypeRecord.Name);

                    if (FeedbackNewTypeToUpdate == null)
                    {
                        _context.FeedbackType.Add(new FeedbackType() { Name = FeedbackNewTypeRecord.Name });

                        saveStatus = 1;

                    }
                    else
                    {
                        saveStatus = -1;
                    }

                }
                else
                {
                    var FeedbackNewTypeToUpdate = _context.FeedbackType.SingleOrDefault(x => x.Id == FeedbackNewTypeRecord.Id);
                    if (FeedbackNewTypeToUpdate != null)
                    {

                        FeedbackNewTypeToUpdate.Name = FeedbackNewTypeRecord.Name;
                        saveStatus = 1;
                    }


                }


                _context.SaveChanges();
                if (saveStatus != -1)
                {
                    var lastInsertedId = _context.FeedbackType.SingleOrDefault(x => x.Name == FeedbackNewTypeRecord.Name);
                    saveStatus = lastInsertedId.Id;

                }
            }

            return saveStatus;
        }
        //to save  the feedback type-end
        public int SaveSiteLinkDetails(ClientSiteLinksDetails ClientSiteLinksDetailsRecord)
        {
            int saveStatus = 0;
            if (ClientSiteLinksDetailsRecord != null)
            {

                if (ClientSiteLinksDetailsRecord.Id == -1)
                {
                    /* for checking already exist this title  */
                    var checkIfAlreadyExist = _context.ClientSiteLinksDetails.FirstOrDefault(x => x.Title == ClientSiteLinksDetailsRecord.Title && x.ClientSiteLinksTypeId == ClientSiteLinksDetailsRecord.typeId);
                    if (checkIfAlreadyExist == null)
                    {
                        _context.ClientSiteLinksDetails.Add(new ClientSiteLinksDetails()
                        {
                            Title = ClientSiteLinksDetailsRecord.Title,
                            Hyperlink = ClientSiteLinksDetailsRecord.Hyperlink,
                            State = ClientSiteLinksDetailsRecord.State,
                            ClientSiteLinksTypeId = ClientSiteLinksDetailsRecord.typeId
                        });
                        saveStatus = 1;
                    }
                    else
                    {
                        saveStatus = 2;
                    }

                }
                else
                {
                    var reportFieldToUpdate = _context.ClientSiteLinksDetails.SingleOrDefault(x => x.Id == ClientSiteLinksDetailsRecord.Id);

                    if (reportFieldToUpdate != null)
                    {
                        /* for checking already exist this title in state */
                        var checkIfAlreadyExist = _context.ClientSiteLinksDetails.FirstOrDefault(x => x.Title == ClientSiteLinksDetailsRecord.Title && x.ClientSiteLinksTypeId == ClientSiteLinksDetailsRecord.typeId && x.Id != ClientSiteLinksDetailsRecord.Id);
                        if (checkIfAlreadyExist == null)
                        {
                            reportFieldToUpdate.Title = ClientSiteLinksDetailsRecord.Title;
                            reportFieldToUpdate.Hyperlink = ClientSiteLinksDetailsRecord.Hyperlink;
                            reportFieldToUpdate.State = ClientSiteLinksDetailsRecord.State;
                            reportFieldToUpdate.ClientSiteLinksTypeId = ClientSiteLinksDetailsRecord.ClientSiteLinksTypeId;
                            saveStatus = 1;
                        }
                        else
                        {

                            saveStatus = 3;
                        }
                    }
                }
                _context.SaveChanges();

            }

            return saveStatus;
        }


        public void DeleteSiteLinkDetails(int id)
        {
            if (id == -1)
                return;

            var siteLinkDetailsToDelete = _context.ClientSiteLinksDetails.SingleOrDefault(x => x.Id == id);
            if (siteLinkDetailsToDelete == null)
                throw new InvalidOperationException();

            _context.ClientSiteLinksDetails.Remove(siteLinkDetailsToDelete);
            _context.SaveChanges();
        }

        public List<ClientSiteLinksDetails> GetSiteLinkDetailsUsingTypeAndState(int type)
        {
            return _context.ClientSiteLinksDetails.Where(x => x.ClientSiteLinksTypeId == type).OrderBy(x => x.Title).ToList();
        }

        public string GetSiteLinksTypeUsingId(int typeId)
        {

            return _context.ClientSiteLinksPageType.SingleOrDefault(x => x.Id == typeId).PageTypeName;
        }

        public List<IncidentReportsPlatesLoaded> GetPlateLoadedEntry(int? userId)
        {
            return _context.IncidentReportsPlatesLoaded
               .Where(z => z.LogId == userId && z.IncidentReportId == 0)
               .ToList();
        }
        public int GetIncidentReportsPlatesCount(int PlateId, string TruckNo, int? userId)
        {
            return _context.IncidentReportsPlatesLoaded
               .Where(z => z.LogId == userId && z.IncidentReportId == 0 && z.PlateId == PlateId && z.TruckNo == TruckNo)
               .Count();
        }

        public int GetIncidentReportsPlatesCountWithOutPlateId(int? userId)
        {
            return _context.IncidentReportsPlatesLoaded
               .Where(z => z.LogId == userId && z.IncidentReportId == 0)
               .Count();
        }

        public List<IncidentReportsPlatesLoaded> GetIncidentReportsPlatesLoadedWithGuardId(int? userId, int? guardId)
        {
            return _context.IncidentReportsPlatesLoaded
               .Where(z => z.LogId == userId && z.IncidentReportId == 0 && z.GuardId == guardId).ToList();

        }

        public int RemoveIncidentReportsPlatesLoadedWithGuardId(List<IncidentReportsPlatesLoaded> platesLoaded)
        {
            try
            {
                _context.IncidentReportsPlatesLoaded.RemoveRange(platesLoaded);
                _context.SaveChanges();
                return 1;
            }
            catch
            {

                return 0;
            }

        }
        public int RemoveIncidentReportsPlatesLoadedWithUserId(int? userId)
        {
            try
            {
                var IncidentReportsPlatesLoaded = _context.IncidentReportsPlatesLoaded
                .Where(z => z.LogId == userId && z.IncidentReportId == 0).ToList();
                if (IncidentReportsPlatesLoaded.Count != 0)
                {
                    _context.IncidentReportsPlatesLoaded.RemoveRange(IncidentReportsPlatesLoaded);
                    _context.SaveChanges();
                }

                return 1;
            }


            catch
            {

                return 0;
            }

        }


        public int DeleteClientSiteLinksPageType(int typeId)
        {
            if (typeId == -1)
                return 0;

            var siteLinkTypeToDelete = _context.ClientSiteLinksPageType.SingleOrDefault(x => x.Id == typeId);
            if (siteLinkTypeToDelete == null)
            {

                return 0;
            }
            else
            {
                var deatilsList = _context.ClientSiteLinksDetails.Where(x => x.ClientSiteLinksTypeId == typeId).ToList();
                if (deatilsList.Count != 0)
                {
                    foreach (var det in deatilsList)
                    {
                        _context.ClientSiteLinksDetails.Remove(det);
                        _context.SaveChanges();
                    }


                }

                _context.ClientSiteLinksPageType.Remove(siteLinkTypeToDelete);
                _context.SaveChanges();
                return 1;
            }
        }
        //to delete the feedback type-start
        public int DeleteFeedBackType(int typeId)
        {
            if (typeId == -1)
                return 0;

            var feedBackTypeToDelete = _context.FeedbackType.SingleOrDefault(x => x.Id == typeId);
            if (feedBackTypeToDelete == null)
            {

                return 0;
            }
            else
            {


                _context.FeedbackType.Remove(feedBackTypeToDelete);
                _context.SaveChanges();
                return 1;
            }
        }
        //to delete the feedback type-end
        public List<KeyVehcileLogField> GetKeyVehicleLogFieldsByTruckId(int TruckConfig)
        {
            return GetKeyVehicleLogFields()
                .Where(x => x.Id == TruckConfig)
                .OrderBy(x => x.Name)
                .ToList();
        }

        //logBookId entry for radio checklist-start

        public List<GuardLogin> GetGuardLogin(int GuardLoginId, int logBookId)
        {
            return _context.GuardLogins
                .Where(z => z.Id == GuardLoginId && z.ClientSiteLogBookId == logBookId)

                .ToList();
        }
        public List<GuardLog> GetGuardLogs(int GuardLoginId, int logBookId)
        {
            return _context.GuardLogs
                .Where(z => z.GuardLoginId == GuardLoginId && z.ClientSiteLogBookId == logBookId)

                .ToList();
        }

        public List<ClientSiteRadioChecksActivityStatus> GetClientSiteRadioChecksActivityStatus(int GuardId, int ClientSiteId)
        {
            return _context.ClientSiteRadioChecksActivityStatus
                .Where(z => z.GuardId == GuardId && z.ClientSiteId == ClientSiteId && z.GuardLoginTime != null)

                .ToList();
        }
        //logBookId entry for radio checklist-end
        //to add functions for settings in radio check-start
        public void SaveRadioCheckStatus(RadioCheckStatus radioCheckStatus)
        {
            if (radioCheckStatus == null)
                throw new ArgumentNullException();

            if (radioCheckStatus.Id == -1)
            {
                var radionew = new RadioCheckStatus()
                {
                    Name = radioCheckStatus.Name,
                    ReferenceNo = radioCheckStatus.ReferenceNo,
                    RadioCheckStatusColorId = radioCheckStatus.RadioCheckStatusColorId,
                    RadioCheckStatusColorName = radioCheckStatus.RadioCheckStatusColorName,
                };
                _context.RadioCheckStatus.Add(radionew);
            }
            else
            {
                var radioCheckStatusUpdate = _context.RadioCheckStatus.SingleOrDefault(x => x.Id == radioCheckStatus.Id);
                if (radioCheckStatusUpdate == null)
                    throw new InvalidOperationException();

                radioCheckStatusUpdate.Name = radioCheckStatus.Name;
                radioCheckStatusUpdate.ReferenceNo = radioCheckStatus.ReferenceNo;
                radioCheckStatusUpdate.RadioCheckStatusColorId = radioCheckStatus.RadioCheckStatusColorId;
                radioCheckStatusUpdate.RadioCheckStatusColorName = radioCheckStatus.RadioCheckStatusColorName;
            }
            _context.SaveChanges();
        }

        public void DeleteRadioCheckStatus(int id)
        {
            if (id == -1)
                return;

            var radioCheckStatusToDelete = _context.RadioCheckStatus.SingleOrDefault(x => x.Id == id);
            if (radioCheckStatusToDelete == null)
                throw new InvalidOperationException();

            _context.RadioCheckStatus.Remove(radioCheckStatusToDelete);
            _context.SaveChanges();
        }
        //to get functions for settings in radio check-end
        //to add functions for Live events -start
        public void SaveLiveEvents(BroadcastBannerLiveEvents liveEvents)
        {
            if (liveEvents == null)
                throw new ArgumentNullException();

            if (liveEvents.id == -1)
            {
                var calendarEventsnew = new BroadcastBannerLiveEvents()
                {
                    TextMessage = liveEvents.TextMessage,
                    ExpiryDate = liveEvents.ExpiryDate,
                    Weblink = liveEvents.Weblink,
                };
                _context.BroadcastBannerLiveEvents.Add(calendarEventsnew);
            }
            else
            {
                var calendarEventsUpdate = _context.BroadcastBannerLiveEvents.SingleOrDefault(x => x.id == liveEvents.id);
                if (calendarEventsUpdate == null)
                    throw new InvalidOperationException();

                calendarEventsUpdate.TextMessage = liveEvents.TextMessage;
                calendarEventsUpdate.ExpiryDate = liveEvents.ExpiryDate;
                calendarEventsUpdate.Weblink = liveEvents.Weblink;
            }
            _context.SaveChanges();
        }

        public void DeleteLiveEvents(int id)
        {
            if (id == -1)
                return;

            var liveEventsToDelete = _context.BroadcastBannerLiveEvents.SingleOrDefault(x => x.id == id);
            if (liveEventsToDelete == null)
                throw new InvalidOperationException();

            _context.BroadcastBannerLiveEvents.Remove(liveEventsToDelete);
            _context.SaveChanges();
        }
        //to add functions for Calendar events -end
        //to add functions for Calendar events -start

        //To save the Duress Email start
        public void DuressGloablEmail(string Email)
        {

            if (!string.IsNullOrEmpty(Email))
            {

                string[] emailIds = Email.Split(',');
                var EmailUpdate = _context.GlobalDuressEmail.Where(x => x.Id != 0).ToList();
                if (EmailUpdate != null)
                {
                    if (EmailUpdate.Count != 0)
                    {
                        _context.GlobalDuressEmail.RemoveRange(EmailUpdate);
                        _context.SaveChanges();
                    }
                }
                /*Remove all the rows */

                foreach (string part in emailIds)
                {
                    var Emailnew = new GlobalDuressEmail()
                    {
                        Email = part.Trim()
                    };
                    _context.GlobalDuressEmail.Add(Emailnew);
                }

                _context.SaveChanges();
            }




        }

        public void GlobalComplianceAlertEmail(string Email)
        {
            if (!string.IsNullOrEmpty(Email))
            {

                string[] emailIds = Email.Split(',');
                var EmailUpdate = _context.GlobalComplianceAlertEmail.Where(x => x.Id != 0).ToList();
                if (EmailUpdate != null)
                {
                    if (EmailUpdate.Count != 0)
                    {
                        _context.GlobalComplianceAlertEmail.RemoveRange(EmailUpdate);
                        _context.SaveChanges();
                    }
                }
                /*Remove all the rows */

                foreach (string part in emailIds)
                {
                    var Emailnew = new GlobalComplianceAlertEmail()
                    {
                        Email = part
                    };
                    _context.GlobalComplianceAlertEmail.Add(Emailnew);
                }

                _context.SaveChanges();
            }

        }

        public void DroboxDir(string DroboxDir)
        {
            if (!string.IsNullOrEmpty(DroboxDir))
            {


                var DropboxDirUpdate = _context.DropboxDirectory.Where(x => x.Id != 0).ToList();
                if (DropboxDirUpdate != null)
                {
                    if (DropboxDirUpdate.Count != 0)
                    {
                        _context.DropboxDirectory.RemoveRange(DropboxDirUpdate);
                        _context.SaveChanges();
                    }
                }
                var Dropboxnew = new DropboxDirectory()
                {
                    DropboxDir = DroboxDir
                };
                _context.DropboxDirectory.Add(Dropboxnew);


                _context.SaveChanges();
            }

        }
        public void TimesheetSave(string weekname, string time, string mailid, string dropbox)
        {
            if (!string.IsNullOrEmpty(weekname))
            {


                var TimesheetUpdate = _context.TimeSheet.Where(x => x.Id != 0).ToList();
                if (TimesheetUpdate != null)
                {
                    if (TimesheetUpdate.Count != 0)
                    {
                        _context.TimeSheet.RemoveRange(TimesheetUpdate);
                        _context.SaveChanges();
                    }
                }
                var TimeSheetnew = new TimeSheet()
                {
                    weekName = weekname,
                    Frequency = time,
                    Email = mailid,
                    Dropbox = dropbox,
                };
                _context.TimeSheet.Add(TimeSheetnew);


                _context.SaveChanges();
            }

        }
        public void DeafultMailBox(string Email)
        {
            if (!string.IsNullOrEmpty(Email))
            {

                string[] emailIds = Email.Split(',');
                var EmailUpdate = _context.CompanyDetails.Where(x => x.Id != 0).FirstOrDefault();
                if (EmailUpdate != null)
                {

                    EmailUpdate.KPIMail = Email;
                        _context.SaveChanges();
                    
                }
                /*Remove all the rows */

                foreach (string part in emailIds)
                {
                    var Emailnew = new CompanyDetails()
                    {
                        KPIMail = part
                    };

                    EmailUpdate.KPIMail = Emailnew.KPIMail;
                }
                _context.SaveChanges();

            }

        }

        public List<GlobalDuressEmail> GetDuressEmails()
        {
            return _context.GlobalDuressEmail.ToList();
        }
        //To save the Duress Email stop
        //To save the Global Duress SMS start
        public string SaveDuressGloablSMS(GlobalDuressSms SmsNumber, out bool status)
        {
            status = false;
            string msg = "Error in saving number.";
            if (SmsNumber.Id == 0)
            {
                var SmsToUpdate = _context.GlobalDuressSms.SingleOrDefault(x => x.SmsNumber == SmsNumber.SmsNumber && x.CompanyId == SmsNumber.CompanyId);
                if (SmsToUpdate == null)
                {
                    _context.GlobalDuressSms.Add(SmsNumber);
                    _context.SaveChanges();
                    status = true;
                    msg = "Sms number saved successfully.";
                }
                else
                {
                    msg = "Sms number already exists.";
                }
            }
            return msg;
        }
        public string DeleteDuressGloablSMSNumber(int SmsNumberId, out bool status)
        {
            status = false;
            string msg = "Error in deleting number.";
            if (SmsNumberId > 0)
            {
                var SmsToDelete = _context.GlobalDuressSms.SingleOrDefault(x => x.Id == SmsNumberId);
                if (SmsToDelete != null)
                {
                    _context.GlobalDuressSms.Remove(SmsToDelete);
                    _context.SaveChanges();
                    status = true;
                    msg = "Sms number deleted successfully.";
                }
                else
                {
                    msg = "Error sms number does not exists.";
                }
            }
            else
            {
                msg = "Error invalid sms number.";
            }
            return msg;
        }
        public List<GlobalDuressSms> GetDuressSms()
        {
            return _context.GlobalDuressSms.ToList();
        }
        //To save the Global Duress SMS stop
        public void SaveCalendarEvents(BroadcastBannerCalendarEvents calendarEvents)
        {
            if (calendarEvents == null)
                throw new ArgumentNullException();

            if (calendarEvents.id == -1)
            {
                var calendarEventsnew = new BroadcastBannerCalendarEvents()
                {
                    TextMessage = calendarEvents.TextMessage,
                    ExpiryDate = calendarEvents.ExpiryDate,
                    StartDate = calendarEvents.StartDate,
                    ReferenceNo = calendarEvents.ReferenceNo,
                    RepeatYearly = calendarEvents.RepeatYearly,
                    IsPublicHoliday = calendarEvents.IsPublicHoliday,
                };
                _context.BroadcastBannerCalendarEvents.Add(calendarEventsnew);
            }
            else
            {
                var calendarEventsUpdate = _context.BroadcastBannerCalendarEvents.SingleOrDefault(x => x.id == calendarEvents.id);
                if (calendarEventsUpdate == null)
                    throw new InvalidOperationException();

                calendarEventsUpdate.TextMessage = calendarEvents.TextMessage;
                calendarEventsUpdate.ExpiryDate = calendarEvents.ExpiryDate;
                calendarEventsUpdate.StartDate = calendarEvents.StartDate;
                calendarEventsUpdate.ReferenceNo = calendarEvents.ReferenceNo;
                calendarEventsUpdate.RepeatYearly = calendarEvents.RepeatYearly;
                calendarEventsUpdate.IsPublicHoliday = calendarEvents.IsPublicHoliday;
            }
            _context.SaveChanges();
        }

        public void DeleteCalendarEvents(int id)
        {
            if (id == -1)
                return;

            var calendarEventsToDelete = _context.BroadcastBannerCalendarEvents.SingleOrDefault(x => x.id == id);
            if (calendarEventsToDelete == null)
                throw new InvalidOperationException();

            _context.BroadcastBannerCalendarEvents.Remove(calendarEventsToDelete);
            _context.SaveChanges();
        }
        //to add functions for Calendar events -end


        public List<SelectListItem> GetUserClientTypesHavingAccess(int? userId)
        {
            var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            var clientTypes = _context.ClientTypes.OrderBy(x => x.Name).ToList();
            if (userId == null)
            {


                foreach (var item in clientTypes)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
            }
            else
            {
                var allUserAccess = _context.UserClientSiteAccess
                    .Where(x => (!userId.HasValue || userId.HasValue && x.UserId == userId) && x.ClientSite.IsActive == true)
                    .Include(x => x.ClientSite)
                    .Include(x => x.ClientSite.ClientType)
                    .Include(x => x.User)
                    .ToList();
                var clientTypeIds = allUserAccess.Select(x => x.ClientSite.TypeId).Distinct().ToList();
                var temp = clientTypes.Where(x => clientTypeIds.Contains(x.Id)).ToList();
                foreach (var item in temp)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }

            }

            return items;
        }


        public void SaveClientSiteForRcLogBook(int clientSiteId)
        {
            if (clientSiteId == null)
                throw new ArgumentNullException();

            if (clientSiteId != 0)
            {
                /* remove the old site id and save new */
                var alreadyExistingSite = _context.RadioCheckLogbookSiteDetails.ToList();
                _context.RemoveRange(alreadyExistingSite);
                _context.SaveChanges();
                _context.RadioCheckLogbookSiteDetails.Add(new RadioCheckLogbookSiteDetails() { ClientSiteId = clientSiteId, CrDate = DateTime.Today });
                _context.SaveChanges();
            }


        }

        public List<ClientSite> GetClientSiteForRcLogBook()
        {
            var Clisite = new List<ClientSite>();
            var alreadyExistingSite = _context.RadioCheckLogbookSiteDetails.ToList();
            if (alreadyExistingSite.Count != 0)
            {
                Clisite = _context.ClientSites
                .Where(x => x.Id == alreadyExistingSite.FirstOrDefault().ClientSiteId)
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();

            }
            return Clisite;



        }


        //to get incident reports-start-jisha
        public List<IncidentReport> GetIncidentReports(DateTime date, int clientSiteId)
        {
            return _context.IncidentReports
                .Where(x => x.ClientSiteId.GetValueOrDefault() == clientSiteId &&
                            x.CreatedOn.Date == date.Date)
                .ToList();
        }
        //to get incident reports-end-jisha

        /* Get Previous day pushmessages Start*/
        public List<RadioCheckPushMessages> GetPushMessagesNotAcknowledged(int clientSiteId, DateTime date)
        {
            return _context.RadioCheckPushMessages.Where
                 (z => z.ClientSiteId == clientSiteId && z.EntryType == 2 && z.IsAcknowledged == 0 && z.IsDuress == 0).ToList();
        }

        public List<RadioCheckPushMessages> GetDuressMessageNotAcknowledged(int clientSiteId, DateTime date)
        {
            return _context.RadioCheckPushMessages.Where
                 (z => z.ClientSiteId == clientSiteId && z.EntryType == 2 && z.IsAcknowledged == 0 && z.IsDuress == 1).ToList();
        }
        public List<RadioCheckPushMessages> GetDuressMessageNotAcknowledgedForControlRoom(DateTime date)
        {
            return _context.RadioCheckPushMessages.Where
                 (z => z.EntryType == 2 && z.IsAcknowledged == 0 && z.IsDuress == 1).ToList();
        }
        /* Get Previous day pushmessages end*/
        //To Get the Default Email Address start
        public string GetDefaultEmailAddress()
        {
            return _context.CompanyDetails.Select(x => x.IRMail).FirstOrDefault();
            //return _context.ReportTemplates.Select(x => x.DefaultEmail).FirstOrDefault();
        }
        //To Get the Default Email Address stop

        //for checking whther user has any access to the client site ie to be deleted-start
        public List<UserClientSiteAccess> GetUserAccessWithClientSiteId(int Id)
        {
            return _context.UserClientSiteAccess.Where(x => x.ClientSiteId == Id).ToList();
        }
        //for checking whther user has any access to the client site ie to be deleted-end
        //SWChannels - start
        public void SaveSWChannel(SWChannels swChannels)
        {
            if (swChannels == null)
                throw new ArgumentNullException();

            if (swChannels.Id == -1)
            {
                var SWChannelsnew = new SWChannels()
                {
                    SWChannel = swChannels.SWChannel,
                    keys = swChannels.keys,
                };
                _context.SWChannel.Add(SWChannelsnew);
            }
            else
            {
                var SWChannelsnew = _context.SWChannel.SingleOrDefault(x => x.Id == swChannels.Id);
                if (SWChannelsnew == null)
                    throw new InvalidOperationException();

                SWChannelsnew.SWChannel = swChannels.SWChannel;
                SWChannelsnew.keys = swChannels.keys;
            }
            _context.SaveChanges();
        }
        public void DeleteSWChannel(int id)
        {
            if (id == -1)
                return;

            var swChannelsToDelete = _context.SWChannel.SingleOrDefault(x => x.Id == id);
            if (swChannelsToDelete == null)
                throw new InvalidOperationException();

            _context.SWChannel.Remove(swChannelsToDelete);
            _context.SaveChanges();
        }
        //SWChannels - end
        //GeneralFeeds - start
        public void SaveGeneralFeeds(GeneralFeeds generalFeeds)
        {
            if (generalFeeds == null)
                throw new ArgumentNullException();

            if (generalFeeds.Id == -1)
            {
                var GeneralFeedsnew = new GeneralFeeds()
                {
                    Brand = generalFeeds.Brand,
                    APIStrings = generalFeeds.APIStrings,
                    keys = generalFeeds.keys,
                };
                _context.GeneralFeeds.Add(GeneralFeedsnew);
            }
            else
            {
                var GeneralFeedsnew = _context.GeneralFeeds.SingleOrDefault(x => x.Id == generalFeeds.Id);
                if (GeneralFeedsnew == null)
                    throw new InvalidOperationException();

                GeneralFeedsnew.Brand = generalFeeds.Brand;
                GeneralFeedsnew.APIStrings = generalFeeds.APIStrings;
                GeneralFeedsnew.keys = generalFeeds.keys;
            }
            _context.SaveChanges();
        }
        public void DeleteGeneralFeeds(int id)
        {
            if (id == -1)
                return;

            var generalFeedsToDelete = _context.GeneralFeeds.SingleOrDefault(x => x.Id == id);
            if (generalFeedsToDelete == null)
                throw new InvalidOperationException();

            _context.GeneralFeeds.Remove(generalFeedsToDelete);
            _context.SaveChanges();
        }


        #region Sms Channel
        public void SaveSmsChannel(SmsChannel smsChannel)
        {
            if (smsChannel == null)
                throw new ArgumentNullException();

            if (smsChannel.Id == -1)
            {
                var SmsChannelnew = new SmsChannel()
                {
                    CompanyId = smsChannel.CompanyId,
                    SmsProvider = smsChannel.SmsProvider,
                    ApiKey = smsChannel.ApiKey,
                    ApiSecret = smsChannel.ApiSecret,
                    SmsSender = smsChannel.SmsSender,
                };
                _context.SmsChannel.Add(SmsChannelnew);
            }
            else
            {
                var SmsChannelnew = _context.SmsChannel.SingleOrDefault(x => x.Id == smsChannel.Id);
                if (SmsChannelnew == null)
                    throw new InvalidOperationException();

                SmsChannelnew.CompanyId = smsChannel.CompanyId;
                SmsChannelnew.SmsProvider = smsChannel.SmsProvider;
                SmsChannelnew.ApiKey = smsChannel.ApiKey;
                SmsChannelnew.ApiSecret = smsChannel.ApiSecret;
                SmsChannelnew.SmsSender = smsChannel.SmsSender;
            }
            _context.SaveChanges();
        }
        public void DeleteSmsChannel(int id)
        {
            if (id == -1)
                return;

            var smsChannelToDelete = _context.SmsChannel.SingleOrDefault(x => x.Id == id);
            if (smsChannelToDelete == null)
                throw new InvalidOperationException();

            _context.SmsChannel.Remove(smsChannelToDelete);
            _context.SaveChanges();
        }
        #endregion


        //for checking whther user has any access to the client site ie to be deleted-end


        //To get the clientsites according to the clientType start
        public int GetClientSite(int? typeId)
        {
            return _context.UserClientSiteAccess
    .Where(x => x.ClientSite.ClientType.Id == typeId && x.ClientSite.IsActive == true)
    .Select(x => x.ClientSiteId)
    .Distinct()
    .Count();
            //return _context.ClientSites.Where(x => x.TypeId == typeId).Select(x => x.Id).Count();
        }
        //To get the clientsites according to the clientType stop



        //GeneralFeeds - end
        //for toggle areas - start 
        public void SaveClientSiteToggle(int siteId, int toggleTypeId, bool IsActive)
        {
            var clientSitetoggle = _context.ClientSiteToggle.Where(x => x.ClientSiteId == siteId && x.ToggleTypeId == toggleTypeId).FirstOrDefault();
            //if (clientSitetoggle == null)
            //    throw new ArgumentNullException();



            if (clientSitetoggle == null)
            {
                _context.ClientSiteToggle.Add(new ClientSiteToggle()
                {
                    ToggleTypeId = toggleTypeId,
                    ClientSiteId = siteId,

                    IsActive = IsActive
                });


            }
            else
            {
                var clientSiteToggleToUpdate = _context.ClientSiteToggle.SingleOrDefault(x => x.Id == clientSitetoggle.Id);
                if (clientSiteToggleToUpdate == null)
                    throw new InvalidOperationException();



                clientSiteToggleToUpdate.ToggleTypeId = toggleTypeId;
                clientSiteToggleToUpdate.ClientSiteId = siteId;
                clientSiteToggleToUpdate.IsActive = IsActive;

            }
            _context.SaveChanges();


        }

        //for toggle areas - end
        public List<ClientSite> GetNewClientSites(int siteId)
        {
            return _context.ClientSites
                .Where(x => x.IsActive == true && x.Id == siteId)
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();

        }
        //p1-191 hr files task 8-start
        public List<KeyVehicleLog> GetKeyVehiclogWithProviders(string[] providers)
        {

            return _context.KeyVehicleLogs.Where(z => providers.Contains(z.CompanyName) && !string.IsNullOrEmpty(z.Email) && z.PersonType == 195)
                  .Include(z => z.ClientSiteLogBook)
                .ThenInclude(z => z.ClientSite)

                .ToList();


        }

        public string GetKeyVehiclogWithProviders(string providerName)
        {

            return _context.KeyVehicleLogs.Where(z => z.CompanyName == providerName && z.PersonType == 195).FirstOrDefault().Email;



        }
        //p1-191 hr files task 8-end
        public List<ClientSite> GetClientSitesWithTypeId(int[] typeId)
        {
            return _context.ClientSites
                .Where(x => (typeId.Contains(x.TypeId)) && x.IsActive == true)
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public List<RCLinkedDuressMaster> GetAllRCLinkedDuress()
        {
            return _context.RCLinkedDuressMaster
                .Include(z => z.RCLinkedDuressClientSites)
                .ThenInclude(y => y.ClientSite)
                .ThenInclude(y => y.ClientType)
                .ToList();
        }

        public RCLinkedDuressMaster GetRCLinkedDuressById(int duressId)
        {

            return _context.RCLinkedDuressMaster
              .Include(t => t.RCLinkedDuressClientSites)
              .ThenInclude(y => y.ClientSite)
              .ThenInclude(y => y.ClientType)
              .SingleOrDefault(x => x.Id == duressId);
        }


        public StaffDocument GetStaffDocById(int documentId)
        {
            var staffDoc = _context.StaffDocuments
              .SingleOrDefault(x => x.Id == documentId);
            var clientSite = _context.ClientSites
                  .Where(x => x.Id == staffDoc.ClientSite).Include(x => x.ClientType).FirstOrDefault();
            if (clientSite != null)
            {
                staffDoc.ClientSiteName = clientSite?.Name ?? "Unknown";
                // Assign the site name or default to "Unknown"
                if (clientSite.ClientType != null)
                    staffDoc.ClientTypeName = clientSite.ClientType.Name;
                var sites = new List<SelectListItem>();
                var mapping = GetClientSites(null).Where(x => x.ClientType.Name == clientSite.ClientType.Name).OrderBy(clientType => clientType.Name);
                foreach (var item in mapping)
                {
                    sites.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }

                staffDoc.ClientSites = sites;

            }
            return staffDoc;
        }

        public void DeleteRCLinkedDuress(int id)
        {
            var recordToDelete = _context.RCLinkedDuressMaster.SingleOrDefault(x => x.Id == id);
            if (recordToDelete == null)
                throw new InvalidOperationException();

            _context.RCLinkedDuressMaster.Remove(recordToDelete);
            _context.SaveChanges();
        }

        public void SaveRCLinkedDuress(RCLinkedDuressMaster linkedDuress, bool updateClientSites = false)
        {
            var schedule = _context.RCLinkedDuressMaster.Include(z => z.RCLinkedDuressClientSites).SingleOrDefault(z => z.Id == linkedDuress.Id);
            if (schedule == null)
                _context.Add(linkedDuress);
            else
            {
                if (updateClientSites)
                {
                    _context.RCLinkedDuressClientSites.RemoveRange(schedule.RCLinkedDuressClientSites);
                    _context.SaveChanges();
                }


                schedule.GroupName = linkedDuress.GroupName.Trim();
                if (updateClientSites)
                    schedule.RCLinkedDuressClientSites = linkedDuress.RCLinkedDuressClientSites;
            }
            _context.SaveChanges();
        }

        public bool CheckAlreadyExistTheGroupName(RCLinkedDuressMaster linkedDuress, bool updateClientSites = false)
        {
            var status = true;
            if (updateClientSites)
            {
                var sameGroupName = _context.RCLinkedDuressMaster.Where(x => x.GroupName.Trim() == linkedDuress.GroupName.Trim()
                && x.Id != linkedDuress.Id).ToList();
                if (sameGroupName.Count != 0)
                    status = false;
            }
            return status;

        }
        public List<HRGroups> GetHRGroups()
        {
            return _context.HRGroups.ToList();
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
        public List<ClientSiteKpiSettingsCustomDropboxFolder> GetKpiSettingsCustomDropboxFolder(int clientSiteId)
        {
            return _context.ClientSiteKpiSettingsCustomDropboxFolder.Where(x => x.ClientSiteId == clientSiteId).ToList();
        }

        public void SaveKpiSettingsCustomDropboxFolder(ClientSiteKpiSettingsCustomDropboxFolder record)
        {
            if (record.Id == -1)
            {
                record.Id = 0;
                _context.ClientSiteKpiSettingsCustomDropboxFolder.Add(record);
            }
            else
            {
                var recordToUpdate = _context.ClientSiteKpiSettingsCustomDropboxFolder.SingleOrDefault(x => x.Id == record.Id);
                if (recordToUpdate != null)
                {
                    recordToUpdate.DropboxFolderName = record.DropboxFolderName;
                }
            }
            _context.SaveChanges();
        }

        public void DeleteKpiSettingsCustomDropboxFolder(int id)
        {
            var recordToDelete = _context.ClientSiteKpiSettingsCustomDropboxFolder.SingleOrDefault(x => x.Id == id);
            if (recordToDelete != null)
            {
                _context.ClientSiteKpiSettingsCustomDropboxFolder.Remove(recordToDelete);
                _context.SaveChanges();
            }
        }


        public void SaveKpiDropboxSettings(KpiDropboxSettings record)
        {
            if (record.Id > 0) //if (record.Id == -1)
            {
                //var recordToUpdate = _context.ClientSiteKpiSettings.SingleOrDefault(x => x.ClientSiteId == record.ClientSiteId);
                var recordToUpdate = _context.ClientSiteKpiSettings.SingleOrDefault(x => x.Id == record.Id);
                if (recordToUpdate != null)
                {
                    recordToUpdate.DropboxImagesDir = record.DropboxImagesDir;
                    recordToUpdate.IsThermalCameraSite = record.IsThermalCameraSite;
                    recordToUpdate.IsWeekendOnlySite = record.IsWeekendOnlySite;
                    recordToUpdate.KpiTelematicsAndStatistics = record.KpiTelematicsAndStatistics;
                    recordToUpdate.SmartWandPatrolReports = record.SmartWandPatrolReports;
                    recordToUpdate.MonthlyClientReport = record.MonthlyClientReport;
                    recordToUpdate.DropboxScheduleisActive= record.DropboxScheduleisActive;
                    _context.SaveChanges();
                }
                else
                {
                    throw new InvalidOperationException("Record not found for updation !!!.");
                }
            }
            else
            {
                throw new InvalidOperationException("Only update is allowed !!!.");
            }

        }
        public GlobalComplianceAlertEmail GetEmail()
        {
            return _context.GlobalComplianceAlertEmail.FirstOrDefault();
        }
        public DropboxDirectory GetDropboxDir()
        {
            return _context.DropboxDirectory.FirstOrDefault();
        }


        public List<ClientSiteRadioChecksActivityStatus_History> GetClientSiteFunsionLogBooks(int clientSiteId, LogBookType type, DateTime fromDate, DateTime toDate)
        {
            return _context.ClientSiteRadioChecksActivityStatus_History
                .Where(z => z.ClientSiteId == clientSiteId && z.EventDateTime.Date >= fromDate && z.EventDateTime.Date <= toDate)
                .ToList();
        }
        public List<GuardLogin> GetClientSiteGuradLoginDetails(int clientSiteId)
        {
            return _context.GuardLogins
                .Where(z => z.ClientSiteId == clientSiteId)
                .ToList();
        }
        public List<GuardLogin> GetGuardDetailsAll(int[] clientSiteIds, string startdate, string endDate)
        {
            DateTime startDateParsed;
            DateTime endDateParsed;

            // Use DateTime.TryParse to handle invalid date formats
            if (!DateTime.TryParse(startdate, out startDateParsed))
            {
                throw new ArgumentException("Invalid start date format");
            }

            if (!DateTime.TryParse(endDate, out endDateParsed))
            {
                throw new ArgumentException("Invalid end date format");
            }
            var clientSiteDetails = _context.GuardLogins
       .Include(x => x.ClientSite)
       .Where(x => clientSiteIds.Contains(x.ClientSiteId) &&
                   (startDateParsed == endDateParsed
                        ? x.LoginDate.Date == startDateParsed.Date // If dates are equal, check exact date
                        : x.LoginDate.Date >= startDateParsed.Date && x.LoginDate.Date <= endDateParsed.Date))
       .AsEnumerable() // Switch to client-side evaluation
       .DistinctBy(x => x.GuardId) // Now this works on the client side
       .ToList();

            return clientSiteDetails;
        }

        public GuardLogin GetGuardDetailsAllTimesheet(int clientSiteIds, string startdate, string endDate)
        {
            DateTime startDateParsed;
            DateTime endDateParsed;

            // Use DateTime.TryParse to handle invalid date formats
            if (!DateTime.TryParse(startdate, out startDateParsed))
            {
                throw new ArgumentException("Invalid start date format");
            }
            string format = "MM/dd/yyyy";
            if (!DateTime.TryParseExact(endDate, format, null, System.Globalization.DateTimeStyles.None, out endDateParsed))
            {

                throw new ArgumentException("Invalid end date format");
            }
            var clientSiteDetails = _context.GuardLogins
    .Include(x => x.ClientSite)
    .Where(x => x.ClientSiteId == clientSiteIds && // Direct comparison instead of Contains
                (startDateParsed == endDateParsed
                     ? x.LoginDate.Date == startDateParsed.Date // If dates are equal, check exact date
                     : x.LoginDate.Date >= startDateParsed.Date && x.LoginDate.Date <= endDateParsed.Date))
    .AsEnumerable() // Switch to client-side evaluation
    .DistinctBy(x => x.GuardId) // Ensures distinct guards on the client side
    .FirstOrDefault();

            return clientSiteDetails;
        }
        public GuardLogin GetGuardDetailsAllTimesheet1(int clientSiteIds, DateTime startdate, DateTime endDate)
        {

            var clientSiteDetails = _context.GuardLogins
    .Include(x => x.ClientSite)
    .Where(x => x.ClientSiteId == clientSiteIds && // Direct comparison instead of Contains
                (startdate == endDate
                     ? x.LoginDate.Date == startdate.Date // If dates are equal, check exact date
                     : x.LoginDate.Date >= startdate.Date && x.LoginDate.Date <= endDate.Date))
    .AsEnumerable() // Switch to client-side evaluation
    .DistinctBy(x => x.GuardId) // Ensures distinct guards on the client side
    .FirstOrDefault();

            return clientSiteDetails;
        }
        public List<GuardLogin> GetGuardDetailsAllTimesheetList(int[] clientSiteIds, DateTime startdate, DateTime endDate)
        {
            var clientSiteDetails = _context.GuardLogins
                .Include(x => x.ClientSite)
                .Where(x => clientSiteIds.Contains(x.ClientSiteId) && // Use Contains for array comparison
                            (startdate == endDate
                                 ? x.LoginDate.Date == startdate.Date // If dates are equal, check exact date
                                 : x.LoginDate.Date >= startdate.Date && x.LoginDate.Date <= endDate.Date))
                .AsEnumerable() // Switch to client-side evaluation
                .DistinctBy(x => x.GuardId) // Ensures distinct guards on the client side
                .ToList();

            return clientSiteDetails;
        }
        public TimeSheet GetTimesheetDetails()
        {
            return _context.TimeSheet.FirstOrDefault();
        }

        public List<ClientSite> GetClientSiteDetailsWithId(int clientSiteIds)
        {
            var clientSiteDetails = _context.ClientSites
                .Where(x => x.Id == clientSiteIds)
                .ToList();
            return clientSiteDetails;
        }

        public string GetContractedManningDetailsForSpecificSite(string siteName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(siteName))
                {
                    return string.Empty;
                }

                var clientSite = _context.ClientSites.FirstOrDefault(x => x.Name == siteName);
                if (clientSite == null)
                {
                    // Site not found, return a default or throw an exception
                    return string.Empty;
                }

                var clientSiteKpiSettings = _context.ClientSiteKpiSettings
                    .FirstOrDefault(x => x.ClientSiteId == clientSite.Id);
                if (clientSiteKpiSettings == null)
                {
                    // KPI settings not found, return a default or throw an exception
                    return string.Empty;
                }

                var distinctPositions = _context.ClientSiteManningKpiSettings
                    .Where(x => x.SettingsId == clientSiteKpiSettings.Id && x.Type == "2")
                    .Select(x => x.PositionId)
                    .Distinct()
                    .FirstOrDefault();
                var postionName = _context.IncidentReportPositions.Where(x => x.Id == distinctPositions).FirstOrDefault();

                if (postionName != null)
                {
                    return postionName.Name ?? string.Empty;

                }
                else
                {
                    return string.Empty;
                }

            }
            catch (Exception ex)
            {
                return string.Empty;
            }

        }


        public void SaveSiteLogUploadHistory(SiteLogUploadHistory siteLogUploadHistory)
        {
            if (siteLogUploadHistory.Id == 0)
            {

                siteLogUploadHistory.Date = DateTime.Now;
                _context.SiteLogUploadHistory.Add(siteLogUploadHistory);
                _context.SaveChanges();
            }

        }
        /* update Status for a site */
        public void UpdateClientSiteStatus(int clientSiteId, DateTime? updateDatetime, int status, int kpiSettingsid,int? KPITelematicsFieldID)
        {
            // Fetch the client site to update
            var updateClientSite = _context.ClientSites.SingleOrDefault(x => x.Id == clientSiteId);

            if (updateClientSite != null)
            {
                bool isUpdated = false;

                // Check if status or status date has changed
                if (updateClientSite.Status != status)
                {
                    updateClientSite.Status = status;
                    isUpdated = true;
                }

                if (updateClientSite.StatusDate != updateDatetime)
                {
                    updateClientSite.StatusDate = updateDatetime;
                    isUpdated = true;
                }

                // If either the status or date was updated, proceed to save changes
                if (isUpdated)
                {
                    _context.SaveChanges(); // Save changes for the ClientSite

                    // If the status is 'Expired' (assuming status '2' is Expired)
                    if (status == 2)
                    {
                        var kpiSettingsToUpdate = _context.ClientSiteKpiSettings.SingleOrDefault(x => x.Id == kpiSettingsid);

                        if (kpiSettingsToUpdate != null)
                        {
                            kpiSettingsToUpdate.ScheduleisActive = false;
                            kpiSettingsToUpdate.DropboxScheduleisActive = false;
                            kpiSettingsToUpdate.KPITelematicsFieldID = KPITelematicsFieldID;
                            _context.SaveChanges(); // Save changes for KPI Settings
                        }

                      
                        var clientSite = _context.ClientSites.SingleOrDefault(z => z.Id == clientSiteId);
                        if (clientSite != null)
                        {
                            clientSite.UploadGuardLog = false;
                            _context.SaveChanges();
                        }
                    }
                    else
                    {
                        var kpiSettingsToUpdate = _context.ClientSiteKpiSettings.SingleOrDefault(x => x.Id == kpiSettingsid);

                        if (kpiSettingsToUpdate != null)
                        {
                            kpiSettingsToUpdate.ScheduleisActive = true;
                            kpiSettingsToUpdate.DropboxScheduleisActive = true;
                            kpiSettingsToUpdate.KPITelematicsFieldID = KPITelematicsFieldID;
                            _context.SaveChanges(); // Save changes for KPI Settings
                        }

                        var clientSite = _context.ClientSites.SingleOrDefault(z => z.Id == clientSiteId);
                        if (clientSite != null)
                        {
                            clientSite.UploadGuardLog = true;
                            _context.SaveChanges();
                        }

                    }
                }
            }
        }
        //p1-287 A to E-start
        public List<LanguageMaster> GetLanguages()
        {
            return _context.LanguageMaster.Where(x=>x.IsDeleted==false).OrderBy(x => x.Language).ToList();
        }
        //p1-287 A to E-end
        public IncidentReport GetLastIncidentReportsByGuardId(int guardId)
        {
            return _context.IncidentReports
                .Where(x => x.GuardId == guardId ).OrderByDescending(z => z.CreatedOn)
                .FirstOrDefault();
        }
        public KPITelematicsField GetKPITelematicsDetails(int? Id)
        {
            if (Id == null)
            {
                // Handle null case explicitly, e.g., return null or throw an exception
                return null;
            }

            return _context.KPITelematicsField.FirstOrDefault(x => x.Id == Id);
        }
    }



}
