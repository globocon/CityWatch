using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using iText.Commons.Actions.Contexts;
using iText.Layout.Element;
using iText.StyledXmlParser.Jsoup.Safety;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Dropbox.Api.FileProperties.PropertyType;
using static Dropbox.Api.Files.ListRevisionsMode;
//using static Dropbox.Api.Files.ListRevisionsMode;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

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
        List<RadioCheckPushMessages> GetPushMessagesNotAcknowledged(int clientSiteId, DateTime date);

        List<RadioCheckPushMessages> GetDuressMessageNotAcknowledged(int clientSiteId, DateTime date);
        List<RadioCheckPushMessages> GetDuressMessageNotAcknowledgedForControlRoom(DateTime date);

        public string GetDefaultEmailAddress();


        void RemoveWorker(int settingsId, int OrderId);

        List<ClientSiteLogBook> GetClientSiteLogBookWithOutType(int clientSiteId, DateTime date);
        //for checking whther user has any access to the client site ie to be deleted-start
        List<UserClientSiteAccess> GetUserAccessWithClientSiteId(int Id);
        //for checking whther user has any access to the client site ie to be deleted-end
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
            return _context.ClientTypes.OrderBy(x => x.Name).ToList();
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
                _context.ClientTypes.Add(new ClientType() { Name = clientType.Name });
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

            _context.ClientTypes.Remove(clientTypeToDelete);
            _context.SaveChanges();
        }

        public List<ClientSite> GetClientSites(int? typeId)
        {
            return _context.ClientSites
                .Where(x => !typeId.HasValue || (typeId.HasValue && x.TypeId == typeId.Value))
                .Include(x => x.ClientType)
                .OrderBy(x => x.ClientType.Name)
                .ThenBy(x => x.Name)
                .ToList();
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

        public void SaveClientSite(ClientSite clientSite)
        {
            if (clientSite == null)
                throw new ArgumentNullException();

            var gpsHasChanged = false;

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
                    DataCollectionEnabled = true
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

            _context.ClientSites.Remove(clientSiteToDelete);
            _context.SaveChanges();
        }

        public List<ClientSiteKpiSetting> GetClientSiteKpiSettings()
        {
            return _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.Notes)
                .ToList();
        }

        public ClientSiteKpiSetting GetClientSiteKpiSetting(int clientSiteId)
        {
            var clientSiteKpiSetting = _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSiteDayKpiSettings)
                .Include(x => x.Notes)
                 .Include(x => x.RCActionList)
                .SingleOrDefault(x => x.ClientSiteId == clientSiteId);
            if (clientSiteKpiSetting != null)
            {



                clientSiteKpiSetting.ClientSiteManningGuardKpiSettings = _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == clientSiteKpiSetting.Id).OrderBy(x => x.OrderId).ThenBy(x => ((int)x.WeekDay + 6) % 7).ToList();

            }
            return clientSiteKpiSetting;
        }

        public List<ClientSiteKpiSetting> GetClientSiteKpiSetting(int[] clientSiteIds)
        {
            var clientSiteKpiSetting = _context.ClientSiteKpiSettings
                .Include(x => x.ClientSite)
                .Include(x => x.ClientSite.ClientType)
                .Include(x => x.ClientSiteDayKpiSettings)
                .Where(x => clientSiteIds.Contains(x.ClientSiteId))
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
            _context.ClientSiteKpiSettings.Attach(setting);
            _context.Entry(setting).State = entityState;
            _context.SaveChanges();

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

        public List<ClientSiteLogBook> GetClientSiteLogBooks()
        {
            return _context.ClientSiteLogBooks
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

        public List<ClientSiteLogBook> GetClientSiteLogBookWithOutType(int clientSiteId, DateTime date)
        {
            return _context.ClientSiteLogBooks
                 .Where(z => z.ClientSiteId == clientSiteId  && z.Date == date).ToList();
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
                    templateToUpdate.MessageBarColour = companyDetails.MessageBarColour;
                    templateToUpdate.HomePageMessageUploadedOn = DateTime.Now;
                    templateToUpdate.BannerLogoPath = companyDetails.BannerLogoPath;
                    templateToUpdate.BannerMessage = companyDetails.BannerMessage;
                    templateToUpdate.BannerMessageUploadedOn = DateTime.Now;
                    templateToUpdate.Hyperlink = companyDetails.Hyperlink;
                    templateToUpdate.EmailMessage = companyDetails.EmailMessage;
                    templateToUpdate.EmailMessageUploadedOn = DateTime.Now;
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
                    if (setting.ClientSiteManningGuardKpiSettings.Any() || setting.ClientSiteManningPatrolCarKpiSettings.Any())
                    {

                        if (setting.ClientSiteManningPatrolCarKpiSettings.Count > 0)
                        {
                            if (setting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.DefaultValue == false).Count() > 0)
                            {
                                var positionIdPatrolCar = setting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                                //set the values for SettingsId and PositionId                       
                                if (positionIdPatrolCar != null)
                                {
                                    int? maxId = _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == setting.Id).Any() ? _context.ClientSiteManningKpiSettings.Where(x => x.SettingsId == setting.Id).Max(item => item.OrderId) : 0;
                                    setting.ClientSiteManningPatrolCarKpiSettings.ForEach(x => { x.SettingsId = setting.Id; x.PositionId = positionIdPatrolCar.PositionId; x.OrderId = maxId + 1; });
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
                                            setting.ClientSiteManningGuardKpiSettings.Where(x => x.OrderId == poId.OrderId).ToList().ForEach(x => { x.SettingsId = setting.Id; x.PositionId = poId.PositionId; });

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
        public bool IsValidTimeFormat(string input)
        {
            TimeSpan dummyOutput;
            return TimeSpan.TryParse(input, out dummyOutput);
        }
        public List<ClientSite> GetNewClientSites()
        {
            return _context.ClientSites

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
                .Where(z => z.ClientSiteId == clientSiteId)
                .Include(x => x.ClientSite)
                .OrderBy(z => z.KeyNo)
                .ToList();
        }

        public List<ClientSiteKey> GetClientSiteKeys(int[] clientSiteIds)
        {
            return _context.ClientSiteKeys
                .Where(z => clientSiteIds.Contains(z.ClientSiteId))
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
                .Where(z => z.Id == logBookId && z.Type == type)
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
                    .Where(x => !userId.HasValue || userId.HasValue && x.UserId == userId)
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
            return _context.ReportTemplates.Select(x => x.DefaultEmail).FirstOrDefault();
        }
        //To Get the Default Email Address stop

        //for checking whther user has any access to the client site ie to be deleted-start
        public List<UserClientSiteAccess> GetUserAccessWithClientSiteId(int Id)
        {
            return _context.UserClientSiteAccess.Where(x => x.ClientSiteId == Id).ToList();
        }
        //for checking whther user has any access to the client site ie to be deleted-end
    }


}
