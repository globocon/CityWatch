using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Models.DTO;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.API;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Microsoft.Office.Interop;
using Microsoft.Office.Interop.Access;
using NuGet.Packaging;
using Org.BouncyCastle.Asn1.Pkcs;
using SMSGlobal.api;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;
using static CityWatch.Data.Providers.AppConfigurationProvider;
using static CityWatch.Web.Pages.Admin.GuardSettingsModel;
using static CityWatch.Web.Services.ViewDataService;
using static iText.Kernel.Pdf.Colorspace.PdfSpecialCs;


namespace CityWatch.Web.Services
{
    public enum OfficerPositionFilter
    {
        All = 0,

        PatrolOnly = 1,

        NonPatrolOnly = 2,

        SecurityOnly = 3
    }

    public enum KvlStatusFilter
    {
        All = 0,

        Open = 1,

        Closed = 2,

        Pending = 3
    }

    public interface IViewDataService
    {
        List<SelectListItem> Genders { get; }
        List<SelectListItem> PSPFType { get; }
        List<SelectListItem> States { get; }
        List<SelectListItem> LicenseStates { get; }
        List<SelectListItem> ProviderList { get; }
        List<SelectListItem> NotifiedBy { get; }
        List<SelectListItem> CallSign { get; }
        List<SelectListItem> ClientArea { get; }
        List<SelectListItem> GuardMonth { get; }
        List<SelectListItem> VehicleRegos { get; }
        List<SelectListItem> POIBDMSupplier { get; }
        string GetFeedbackTemplateText(int id);
        //List<SelectListItem> GetFeedbackTemplatesByType(FeedbackType type);
        List<SelectListItem> GetFeedbackTemplatesByType(int type);
        List<SelectListItem> GetOfficerPositions(OfficerPositionFilter positionFilter = OfficerPositionFilter.All);
        List<SelectListItem> GetUserClientTypes(int? userId);
        List<SelectListItem> GetUserClientSites(int? userId, string type = "");
        List<SelectListItem> GetUserClientSites(string types = "");
        int GetUserClientSitesNew(int? userId, string type = "");
        List<object> GetAllUsersClientSiteAccess(string searchTerm);
        List<object> GetUserClientSiteAccess(int userId);
        List<object> GetAllCoreSettings(int companyId);

        List<ClientType> GetUserClientTypesHavingAccess(int? userId);
        List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm);
        Task<DataTable> PatrolDataToDataTable(List<DailyPatrolData> dailyPatrolData);

        // Daily Guard Logs & Key Vehicle Logs
        bool CheckWandIsInUse(int smartWandId, int? guardId);
        List<ClientSiteSmartWand> GetSmartWands(string siteName, int? guardId);
        List<ClientSiteSmartWand> GetClientSiteSmartWands(int clientSiteId);
        List<GuardViewModel> GetGuards();
        List<GuardViewExcelModel> GetGuardsToExcel(bool active, bool inactive, int[] guardIds);
        List<KeyVehicleLogViewModel> GetKeyVehicleLogs(int logBookId, KvlStatusFilter kvlStatusFilter);
        List<KeyVehicleLogViewModel> GetKeyVehicleLogsForIds(int logBookId);
        List<SelectListItem> GetKeyVehicleLogFieldsByType(KvlFieldType type, bool withoutSelect = false);
        List<KeyVehicleLogProfileViewModel> GetKeyVehicleLogProfilesByRego(string truckRego);
        List<KeyVehicleLogProfileViewModel> GetKeyVehicleLogProfilesByRego(string truckRego, string poi);
        List<KeyVehicleLogProfileViewModel> GetKeyVehicleLogProfilesByRegoNew(string truckRego, string ImagePath);
        IEnumerable<string> GetKeyVehicleLogAttachments(string uploadsDir, string reportReference);
        IEnumerable<ClientSiteKey> GetKeyVehicleLogKeys(KeyVehicleLog keyVehicleLog);
        IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistory(string vehicleRego);
        IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistory(int profileId);
        List<ClientSite> GetUserClientSites(string type, string searchTerm);
        List<ClientSite> GetNewUserClientSites();
        List<ClientSiteKey> GetClientSiteKeys(int clientSiteId, string searchKeyNo, string searchKeyDesc);
        int GetNewGuardLoginId(GuardLogin currentGuardLogin, DateTime? currentGuardLoginOffDutyActual, int newLogBookId);
        int GetNewClientSiteLogBookId(int clientSiteId, LogBookType logBookType);
        string GetClientSiteKeyDescription(int KeyId, int clientSiteId);
        void CopyOpenLogbookEntriesFromPreviousDay(int previousDayLogBookId, int logBookId, int guardLoginId);
        IEnumerable<string> GetCompanyAndSenderNames(string startsWith);
        IEnumerable<string> GetCompanyNames(string startsWith);
        bool IsClientSiteDuressEnabled(int clientSiteId);
        void EnableClientSiteDuress(int clientSiteId, int guardLoginId, int logBookId, int guardId, string gpsCoordinates, string enabledAddress, GuardLog tmzdata, string clientSiteName, string GuradName);
        int GetClientTypeCount(int? typeId);

        //For Access Type
        List<SelectListItem> GetAccessTypes(bool withoutSelect = false);
        string GetClientSiteKeyNo(int keyId, int clientSiteId);
        List<SelectListItem> GetUserClientTypesCount(int? userId);

        List<ClientSiteKey> GetClientSiteKeysbySearchDesc(int clientSiteId, string searchKeyDesc);
        List<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryNew(int profileId);
        IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryWithPersonName(string PersonName);
        IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryWithKeyNo(string KeyNo);
        string GetFeedbackTemplatesByTypeByColor(int type, int id);
        List<FeedbackTemplate> GetFeedbackTemplateListByType(int type);
        public IncidentReportPosition GetLoogbookdata(string IncidentName);

        List<TrailerDeatilsViewModel> GetKeyVehicleTrailerNew(string truckRego);


        List<SelectListItem> GetClientSitePocsVehicleLog(int[] clientSiteIds);


        //p2-192 client email search-start
        List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm, string searchTermtwo);
        //p2-192 client email search-end

        //p1-191 HR Files Task3-start
        List<SelectListItem> GetHRGroups(bool withoutSelect = false);
        List<SelectListItem> GetReferenceNoNumbers(bool withoutSelect = false);
        List<SelectListItem> GetReferenceNoAlphabets(bool withoutSelect = false);
        //p1-191 HR Files Task3-end
        List<SelectListItem> GetLicenseTypes(bool withoutSelect = false);
        //p1-202 site allocation-start
        List<SelectListItem> GetClientAreas(IncidentReportField ir);
        List<SelectListItem> GetClientSites(string type = "");
        List<HRGroups> GetHRGroups();

        public List<SelectListItem> ProviderListNewwithSmallLetter { get; }

        //p1-202 site allocation-end

        List<FileDownloadAuditLogs> GetFileDownloadAuditLogs(DateTime logFromDate, DateTime logToDate);
        IEnumerable<string> GetDailyGuardLogAttachments(string uploadsDir, string reportReference);
        List<SelectListItem> GetOfficerPositionsNew(OfficerPositionFilter positionFilter);
        ClientSiteKey GetClientSiteKeyDescriptionAndImage(int keyId, int clientSiteId);
        public ANPR GetANPR(int clientSiteId);

        List<object> GetHrSettingsClientSiteLockStatus(int hrSettingsId);
        List<SelectListItem> GetUserClientTypesCountWithTypeId(int? userId, int? clienttypeid);
        public List<SelectListItem> GetLanguageMaster(bool withoutSelect = true);
        List<SelectListItem> GetLanguages(bool withoutSelect = true);
        public List<ClientSiteWithWands> GetUserClientSitesExcel(int? typeId, int? userId);

        List<SelectListItem> GetCourseDuration(bool withoutSelect = true);
        List<SelectListItem> GetTestDuration(bool withoutSelect = true);
        List<SelectListItem> GetPassMark(bool withoutSelect = true);
        List<SelectListItem> GetTestAttempts(bool withoutSelect = true);
        List<SelectListItem> GetTrainingCertificateExpiryYears(bool withoutSelect = true);
        List<SelectListItem> GetTestQuestionNumbers(bool withoutSelect = true);
        List<SelectListItem> GetTestTQNumbers(bool withoutSelect = true);
        List<SelectListItem> GetPracticalLocation(bool withoutSelect = true);
        List<ClientType> GetUserClientTypesHavingAccessThird(int? userId);
        public UserClientSiteAccess GetUserClientSiteAccessNew(int userId);
        public List<DropdownItem> GetUserClientTypesWithId(int? userId);
        public List<DropdownItem> GetUserClientSitesUsingId(int? userId, int id);
        public List<ClientSite> GetUserClientSitesFromUserId(int? userId, int id);
        public List<ActivityModel> GetDressAppFields(int type, int? siteid = 0);

        public List<Mp3File> GetDressAppFieldsAudio(int type);

        public ClientSiteMobileAppSettings GetCrowdSettingForSite(int siteId);
        public Task ResetAllSiteCrowdCountControl();
        public Task SaveCrowdControlGuardLocation(MobileCrowdControlGuard MCCG);
        List<SelectListItem> GetUserClientSitesWithPatrolData(int? userId, string[] type);
        public Task<ClientSiteMobileCrowdControlDTO> GetCrowdCountControlDataAndSettings(int siteId);
        List<SubDomain> GetUserSubDomainsHavingAccess(int? userId);
        List<string> GetSmartWandTagTypesForClientSite(int clientSiteId);
        List<SmartWandTagsType> GetSmartWandTagTypes();
        ScannerTagDetails GetSmartWandTagDetailOfTag(string TagUid, string TagType);
        List<object> GetGuardRcClientSiteAccess(int guardId);
        List<ClientSiteSmartWandTags> GetClientSiteTagIds(int[] clientSiteIds);
        List<SelectListItem> GetClientSiteSmartWandIds(int[] clientSiteIds);
        List<SelectListItem> GetPatrolCarAssociatedSmartWands(int[] patrolCarIds);
        List<KeyVehicleLogDocketViewModel> GetKeyVehicleLogsWithDockets(DateTime LogFromDate, DateTime LogToDate, int[] ClientSiteIds);
        Task<DataTable> KVDocketToDataTable(List<KeyVehicleLogDocketViewModel> dailyPatrolData);

        public List<DropdownItemWithAddress> GetUserClientSitesWithAddressUsingId(int? userId, int id);
        public List<DropdownItem> GetClientSiteSmartWandListForMobile(int clientSiteId);
        public SmartWandDeviceRegister CheckAndRegisterDeviceWithSmartWand(SmartWandDeviceRegister DeviceToRegister);
        public bool CheckIfSmartWandIsDeRegisteredAsync(string DeviceIdToCheck);
        public int GetSmartWandIdFromDeviceId(string DeviceIdToCheck);
        public List<Dictionary<string, string>> GetCustomFieldLogs(int logBookId, int clientSiteId);
        public bool SaveCustomFieldLog(int logBookId, Dictionary<string, string> records);
        public List<PatrolCarLog> GetPatrolCarLogs(int logBookId, int clientSiteId);
        public bool SavePatrolCarLog(PatrolCarLog record);
        public Dictionary<string, string> GetCustomFieldConfig(int clientSiteId);
        public MobileAppUpgrade GetLatestMobileAppVersion(string platformType);
        public List<MobileAppUpgrade> GetAllMobileAppVersion();
        public void SaveMobileAppUpgrade(MobileAppUpgrade mobileAppUpgrade);
        public void DeleteMobileAppUpgrade(int id);
        public void UpdateDownloadCount(int id);
        public void RollBackToVersion(int recordId);
        public (bool AccessPermission, int? LoggedInUserId, int? GuId, int? SuccessCode, string SuccessMessage) ValidateGuardHrPin(int guardId, string key);
        public List<Guard> GetLicenseAndCompliancForGuards(int guardId);
        public List<GuardComplianceAndLicense> GetGuardLicenseAndComplianceData(int guardId);
        public List<CombinedData> GetHRDescription(int HRid, int GuardID);
        public Task<HrSettings> GetHRDescriptionBanDetailsAsync(int DescriptionID);
        public (bool status, bool dbxUploaded, IEnumerable<string> msg) SaveOrUpdateGuardComplianceandlicanseNew(GuardComplianceAndLicense guardComplianceandlicense);
        public bool UploadDocumentToDropbox(string fileToUpload, string dbxFilePath);
        public Task<bool> UploadHrDocumentFileToServer(IFormFile Docfile, string LicenseNo, string uploadFileName);
        public void DeleteGuardHrDocument(int hrDocId);

        //p3-42-Dockets-start
        public List<KVLogDocketsViewModel> GetKeyVehicleLogDocketHistory(PatrolRequest patrolRequest);
        List<KeyVehicleLogDocketViewModel> GetKeyVehicleLogDocketHistoryWithIR(PatrolRequest patrolRequest);
        //p3-42-Dockets-end


        public Task<ClientSiteMobileCrowdControl> GetCrowdControlCount(MobileCrowdControlGuard JoinGaurd);
        List<KeyVehicleLogViewModel> GetKeyVehicleLogsWithPax(int logBookId, KvlStatusFilter kvlStatusFilter);
        List<SelectListItem> GetClientSitePatrolCarIds(int[] clientSiteIds);
        public List<SelectListItem> GetAllPatrolCars();
        public List<ActivityModelDTO> GetPreDefinedActivitesFields();
    }


    public class ViewDataService : IViewDataService
    {
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IUserDataProvider _userDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;
        private readonly ILogbookDataService _logbookDataService;
        private readonly IAppConfigurationProvider _appConfigurationProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDropboxService _dropboxUploadService;
        private readonly Settings _settings;
        private readonly string _reportRootDir;
        private readonly IIrDataProvider _irDataProvider;

        public ViewDataService(IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardSettingsDataProvider guardSettingsDataProvider,
            ILogbookDataService logbookDataService,
            IAppConfigurationProvider appConfigurationProvider,
             IWebHostEnvironment webHostEnvironment,
             IDropboxService dropboxUploadService,
             IOptions<Settings> settings, IIrDataProvider irDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _userDataProvider = userDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
            _logbookDataService = logbookDataService;
            _appConfigurationProvider = appConfigurationProvider;
            _webHostEnvironment = webHostEnvironment;
            _dropboxUploadService = dropboxUploadService;
            _settings = settings.Value;
            _reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath);
            _irDataProvider = irDataProvider;
        }

        public List<SelectListItem> Genders
        {
            get
            {
                return new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true),
                    new SelectListItem("Male", "Male"),
                    new SelectListItem("Female", "Female"),
                    new SelectListItem("Non-Binary", "Non-Binary"),
                    new SelectListItem("Not Stated", "Not Stated"),
                    new SelectListItem("Other", "Other")
                };
            }
        }
        //code added for PSPF dropdown start
        public List<SelectListItem> PSPFType
        {
            get
            {
                var pspfTypes = _clientDataProvider.GetPSPF();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in pspfTypes)
                {
                    var selectListItem = new SelectListItem(item.Name, item.Name);
                    var selectListItem1 = item.Name;
                    var Default = item.IsDefault;
                    if (Default == true)
                    {
                        selectListItem.Selected = true;
                    }
                    items.Add(selectListItem);
                }

                return items;
            }
        }
        //code added for PSPF dropdown stop

        public List<SelectListItem> GetOfficerPositions(OfficerPositionFilter positionFilter = OfficerPositionFilter.All)
        {
            var items = new List<SelectListItem>()
            {
                new SelectListItem("Select", "", true),
            };
            var officerPositions = _configDataProvider.GetPositions();
            foreach (var officerPosition in officerPositions.Where(z => positionFilter == OfficerPositionFilter.All ||
                 positionFilter == OfficerPositionFilter.PatrolOnly && z.IsPatrolCar ||
                 positionFilter == OfficerPositionFilter.NonPatrolOnly && !z.IsPatrolCar ||
                 positionFilter == OfficerPositionFilter.SecurityOnly && z.Name.Contains("Security")))
            {
                items.Add(new SelectListItem(officerPosition.Name, officerPosition.Name));



            }

            return items;
        }
        //To get the logbook data in IncidentReport start
        public IncidentReportPosition GetLoogbookdata(string IncidentName)
        {
            return _configDataProvider.GetIsLogbookData(IncidentName);
        }
        //To get the logbook data in IncidentReport stop

        public List<ClientSiteSmartWand> GetSmartWands(string siteName, int? guardId)
        {
            var wandNames = _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(x => x.ClientSite.Name == siteName).ToList();
            foreach (var wandName in wandNames)
            {
                wandName.IsInUse = CheckWandIsInUse(wandName.Id, guardId);
            }

            return wandNames;
        }

        public bool CheckWandIsInUse(int smartWandId, int? guardId)
        {
            //return _guardDataProvider.GetGuardLoginsBySmartWandId(smartWandId)
            //    .Where(x => x.LoginDate >= DateTime.Today && x.LoginDate < DateTime.Today.AddDays(1)
            //            && (!guardId.HasValue || x.GuardId != guardId.Value) && x.OffDuty > DateTime.Now)
            //    .Any();
            var today = DateTime.Today;
            var lastGuardUsed = _guardDataProvider
                .GetLastGuardUsedSmartWandBySmartWandId(smartWandId)
                .Where(x => x.CreatedDate >= today && x.CreatedDate < today.AddDays(1))
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefault();
            if (lastGuardUsed == null)
                return false;

            var lastLoginToday = _guardDataProvider
                .GetGuardLoginsBySmartWandId(smartWandId)
                .Where(x => x.LoginDate >= today && x.LoginDate < today.AddDays(1) && x.Id == lastGuardUsed.GuardLoginId)
                .OrderByDescending(x => x.LoginDate)
                .FirstOrDefault();

            // NOT in use if nobody used it today
            if (lastLoginToday == null)
                return false;

            // NOT in use if last guard is off duty
            if (lastLoginToday.OffDuty <= DateTime.Now)
                return false;

            // NOT in use if same guard is reusing it
            if (guardId.HasValue && lastLoginToday.GuardId == guardId.Value)
                return false;

            // Otherwise → in use by another guard
            return true;

        }

        public List<ClientSiteSmartWand> GetClientSiteSmartWands(int clientSiteId)
        {
            return _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSiteId).ToList();
        }

        public List<string> GetSmartWandTagTypesForClientSite(int clientSiteId)
        {
            var smartWandTags = _clientSiteWandDataProvider.GetClientSiteSmartWandTags()
                 .Where(z => z.ClientSiteId == clientSiteId)
                 .Select(z => z.TagsType)
                 .Distinct()
                 .ToList();
            return smartWandTags;
        }

        public List<SmartWandTagsType> GetSmartWandTagTypes()
        {
            var smartWandTags = _clientSiteWandDataProvider.GetSmartWandTagsType()
                 .ToList();
            return smartWandTags;
        }

        public ScannerTagDetails GetSmartWandTagDetailOfTag(string TagUid, string TagType)
        {
            var smartWandTag = _clientSiteWandDataProvider.GetClientSiteSmartWandTags()
                 .FirstOrDefault(z => z.UId == TagUid && z.SmartWandTagsType.value.ToLower() == TagType.ToLower());

            ScannerTagDetails scannerTagDetails = new ScannerTagDetails();
            if (smartWandTag != null)
            {
                scannerTagDetails.Id = smartWandTag.Id;
                scannerTagDetails.ClientSiteId = smartWandTag.ClientSiteId;
                scannerTagDetails.ClientSiteName = smartWandTag.ClientSite.Name;
                scannerTagDetails.UId = smartWandTag.UId;
                scannerTagDetails.TagsTypeId = smartWandTag.TagsTypeId;
                scannerTagDetails.TagsType = smartWandTag.SmartWandTagsType.value;
                scannerTagDetails.LabelDescription = smartWandTag.LabelDescription;
            }
            else
            {
                return null;
            }

            return scannerTagDetails;
        }

        public List<SelectListItem> LicenseStates
        {
            get
            {
                var items = new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true),
                    new SelectListItem("N/A", "N/A")
                };
                var licenseStates = _configDataProvider.GetStates();
                foreach (var item in licenseStates)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
                return items;
            }
        }

        public List<SelectListItem> ProviderList
        {
            get
            {
                var items = new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true)
                };
                var KVID = _configDataProvider.GetKVLogField();
                var providerlist = _configDataProvider.GetProviderList(KVID.Id);
                foreach (var item in providerlist)
                {
                    if (item.CompanyName != null)
                    {
                        items.Add(new SelectListItem(item.CompanyName, item.CompanyName));
                    }

                }
                return items;
            }
        }

        public List<SelectListItem> ProviderListNewwithSmallLetter
        {
            get
            {
                var items = new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true)
                };
                var KVID = _configDataProvider.GetKVLogField();
                var providerlist = _configDataProvider.GetProviderList(KVID.Id);
                foreach (var item in providerlist)
                {
                    if (item.CompanyName != null)
                    {
                        items.Add(new SelectListItem(item.CompanyName, item.CompanyName.Trim().ToLower()));
                    }

                }
                return items;
            }
        }

        public List<SelectListItem> States
        {
            get
            {
                var clientStates = _configDataProvider.GetStates();
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                foreach (var item in clientStates)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
                return items;
            }
        }

        public List<SelectListItem> NotifiedBy
        {
            get
            {

                var items = new List<SelectListItem>();
                var notifiedBy = _configDataProvider.GetReportFieldsByType(ReportFieldType.NotifiedBy);
                foreach (var item in notifiedBy)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
                return items.ToList();

            }
        }

        public List<SelectListItem> CallSign
        {
            get
            {
                var items = new List<SelectListItem>();
                var callSign = _configDataProvider.GetReportFieldsByType(ReportFieldType.CallSign);
                foreach (var item in callSign)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
                return items.ToList();
            }
        }

        public List<SelectListItem> GuardMonth
        {
            get
            {
                return new List<SelectListItem>()
                {
                    new SelectListItem("Select", "", true),
                    new SelectListItem("< 3 Months", "< 3 Months"),
                    new SelectListItem("3-11 Months", "3-11 Months"),
                    new SelectListItem("1~2 years", "1~2 years"),
                    new SelectListItem("2~4 years", "2~4 years"),
                    new SelectListItem("5~10 years", "5~10 years"),
                    new SelectListItem("10+ years", "10+ years")
                };
            }
        }

        public List<SelectListItem> ClientArea
        {
            get
            {
                var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
                var clientArea = _configDataProvider.GetReportFieldsByType(ReportFieldType.ClientArea);
                foreach (var item in clientArea)
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
                return items.ToList();
            }
        }

        public List<SelectListItem> GetUserClientTypes(int? userId)
        {
            var clientTypes = GetUserClientTypesHavingAccess(userId);
            var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            foreach (var item in clientTypes)
            {
                items.Add(new SelectListItem(item.Name, item.Name));
            }

            return items;
        }
        //To get the count of ClientTypes start
        public List<SelectListItem> GetUserClientTypesCount(int? userId)
        {
            var clientTypes = GetUserClientTypesHavingAccess(userId);
            var sortedClientTypes = clientTypes.OrderByDescending(clientType => GetClientTypeCount(clientType.Id));
            sortedClientTypes = sortedClientTypes.OrderBy(clientType => clientType.Name);
            var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            foreach (var item in sortedClientTypes)
            {
                var countClientType = GetClientTypeCount(item.Id);
                items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name));
            }

            return items;
        }
        public List<SelectListItem> GetUserClientTypesCountWithTypeId(int? userId, int? clienttypeid)
        {
            var clientTypes = GetUserClientTypesHavingAccess(userId).Where(x => x.Id == clienttypeid);
            var sortedClientTypes = clientTypes.OrderByDescending(clientType => GetClientTypeCount(clientType.Id));
            sortedClientTypes = sortedClientTypes.OrderBy(clientType => clientType.Name);
            var items = new List<SelectListItem>() { new SelectListItem("Select", "") };
            foreach (var item in sortedClientTypes)
            {
                var countClientType = GetClientTypeCount(item.Id);
                items.Add(new SelectListItem($"{item.Name} ({countClientType})", item.Name, true));
            }

            return items;
        }
        //To get the count of ClientTypes stop

        public List<SelectListItem> GetUserClientSites(int? userId, string type = "")
        {
            var sites = new List<SelectListItem>();
            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == type);
            if (clientType != null)
            {
                var mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty).Where(x => x.ClientType.Name == type);
                foreach (var item in mapping)
                {
                    sites.Add(new SelectListItem(item.Name, item.Name));
                }

            }
            return sites;
        }
        public int GetUserClientSitesNew(int? userId, string type = "")
        {

            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == type);
            var mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty).Where(x => x.ClientType.Name == type).FirstOrDefault();

            return mapping.Id;
        }
        public List<SelectListItem> GetUserClientSites(string types = "")
        {
            var sites = new List<SelectListItem>();
            if (string.IsNullOrEmpty(types))
                return sites;

            var clientSites = _clientDataProvider.GetClientSites(null).Where(z => types.Contains(z.ClientType.Name));
            foreach (var item in clientSites)
            {
                sites.Add(new SelectListItem(item.Name, item.Name));
            }
            return sites;
        }

        //public List<SelectListItem> GetFeedbackTemplatesByType(FeedbackType type)
        //{
        //    var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == type.Id);
        //    var items = new List<SelectListItem>() { new SelectListItem("Select Template", "", true) };
        //    foreach (var item in feedbackTemplates)
        //    {
        //        items.Add(new SelectListItem(item.Name, item.Id.ToString()));
        //    }

        //    return items;
        //}
        public List<SelectListItem> GetFeedbackTemplatesByType(int type)
        {
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == type);
            var items = new List<SelectListItem>() { new SelectListItem("Select Template", "", true) };
            foreach (var item in feedbackTemplates)
            {
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }
        public List<FeedbackTemplate> GetFeedbackTemplateListByType(int type)
        {
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == type).ToList();
            return feedbackTemplates;
        }
        public string GetFeedbackTemplateText(int id)
        {
            return _configDataProvider.GetFeedbackTemplates().SingleOrDefault(x => x.Id == id)?.Text;
        }


        public List<object> GetAllUsersClientSiteAccess(string searchterm)
        {
            var results = new List<object>();
            var users = _userDataProvider.GetUsers();
            var allUserAccess = _userDataProvider.GetUserClientSiteAccess(null);
            foreach (var user in users)
            {
                var ThirdPartyID = _userDataProvider.GetUserClientSiteAccessThirdParty(user.Id);
                var currUserAccess = allUserAccess.Where(x => x.UserId == user.Id);
                results.Add(new
                {
                    user.Id,
                    user.UserName,
                    ClientTypeCsv = GetFormattedClientTypes(currUserAccess),
                    ClientSiteCsv = GetFormattedClientSites(currUserAccess),
                    ThirdParty = (ThirdPartyID != null && ThirdPartyID.ThirdPartyID != 0) ? ThirdPartyID.ThirdPartyID : null
                });
            }
            var filteredResults = results;

            if (!string.IsNullOrEmpty(searchterm))
            {
                filteredResults = results
                    .Where(x =>
                        ((dynamic)x).UserName.Contains(searchterm, StringComparison.OrdinalIgnoreCase) ||
                        ((dynamic)x).ClientTypeCsv.Contains(searchterm, StringComparison.OrdinalIgnoreCase) ||
                        ((dynamic)x).ClientSiteCsv.Contains(searchterm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return filteredResults;
        }
        public List<object> GetAllCoreSettings(int companyId)
        {
            var results = new List<object>();
            var coreSettings = _userDataProvider.GetCompanyDetails();
            var currUserAccess = coreSettings.Where(x => x.Id == companyId);
            foreach (var company in currUserAccess)
            {


                results.Add(new
                {
                    company.Id,
                    company.Name,
                    company.Domain,
                    company.LastUploaded,
                    company.FormattedLastUploaded,
                    company.PrimaryLogoPath,
                    company.PrimaryLogoUploadedOn,
                    company.FormattedPrimaryLogoUploaded,
                    company.HomePageMessage,
                    company.HomePageMessage2,
                    company.MessageBarColour,
                    company.HomePageMessageUploadedOn,
                    company.FormattedHomePageMessageUploaded,
                    company.BannerMessage,
                    company.Hyperlink,
                    company.BannerMessageUploadedOn,
                    company.FormattedBannerMessageUploaded,
                    company.EmailMessage,
                    company.EmailMessageUploadedOn,
                    company.FormattedEmailMessageUploaded,
                    company.BannerLogoPath,
                    //p1-225 Core Settings-start
                    company.HyperlinkLabel,
                    company.HyperlinkColour,
                    company.LogoHyperlink,
                    company.ApiProvider,
                    company.ApiSecretkey,
                    //p1-225 Core Settings-end
                    company.IRMail,
                    company.KPIMail,
                    company.FusionMail,
                    company.TimesheetsMail,
                    company.ApiProviderIR,
                    company.ApiSecretkeyIR,
                    company.ROMail


                });
            }
            return results;
        }
        public List<object> GetUserClientSiteAccess(int userId)
        {
            var results = new List<object>();
            var userAccess = _userDataProvider.GetUserClientSiteAccess(userId);
            var clientSitesUserAccess = userAccess.Select(x => x.ClientSiteId);
            var allClientSitesGrouped = _clientDataProvider.GetClientSites(null).GroupBy(x => x.ClientType.Name);

            foreach (var item in allClientSitesGrouped)
            {
                results.Add(new
                {
                    Name = item.Key,
                    ClientSites = item.Select(x => new
                    {
                        Id = x.Id,
                        x.Name,
                        Checked = clientSitesUserAccess.Contains(x.Id)
                    }).ToList()
                });
            }

            return results;
        }
        public UserClientSiteAccess GetUserClientSiteAccessNew(int userId)
        {
            return _userDataProvider.GetUserClientSiteAccessThirdParty(userId);
        }

        public List<object> GetHrSettingsClientSiteLockStatus(int hrSettingsId)
        {
            var results = new List<object>();
            var userAccess = _userDataProvider.GetHrSettingsLockedClientSites(hrSettingsId);
            var clientSitesUserAccess = userAccess.Select(x => x.ClientSiteId);
            var allClientSitesGrouped = _clientDataProvider.GetClientSites(null).GroupBy(x => x.ClientType.Name);

            foreach (var item in allClientSitesGrouped)
            {
                results.Add(new
                {
                    Name = item.Key,
                    ClientSites = item.Select(x => new
                    {
                        Id = x.Id,
                        x.Name,
                        Checked = clientSitesUserAccess.Contains(x.Id)
                    }).ToList()
                });
            }

            return results;
        }

        public List<ClientType> GetUserClientTypesHavingAccess(int? userId)
        {
            var clientTypes = _clientDataProvider.GetClientTypes();
            if (userId == null)
                return clientTypes;

            var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
            var clientTypeIds = allUserAccess.Select(x => x.ClientSite.TypeId).Distinct().ToList();
            return clientTypes.Where(x => clientTypeIds.Contains(x.Id)).ToList();
        }
        //To get the count of ClientType start
        public List<ClientType> GetUserClientTypesHavingAccessThird(int? userId)
        {
            var results = new List<ClientType>();

            var allClientSitesGrouped = _clientDataProvider.GetClientSites(null)
                .GroupBy(x => new { x.ClientType.Name, x.ClientType.Id });

            foreach (var item in allClientSitesGrouped)
            {
                results.Add(new ClientType
                {
                    Name = item.Key.Name,
                    Id = item.Key.Id,
                    IsSubDomainEnabled = false,  // Default to false

                });
            }

            return results;
        }
        public int GetClientTypeCount(int? typeId)
        {
            var result = _clientDataProvider.GetClientSite(typeId);
            return result;
        }
        //To get the count of ClientType stop

        public List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm)
        {
            var results = new List<ClientSite>();
            var clientSites = _clientDataProvider.GetClientSites(typeId);
            if (userId == null)
                results = clientSites;
            else
            {
                var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
                var clientSiteIds = allUserAccess.Select(x => x.ClientSite.Id).Distinct().ToList();
                results = clientSites.Where(x => clientSiteIds.Contains(x.Id)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
                results = results.Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(x.Address) && x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))).ToList();

            return results;
        }

        public List<GuardViewModel> GetGuards()
        {
            // Retrieve guards and guard logins in a single step
            var guards = _guardDataProvider.GetGuards().ToList();
            var guardIds = guards.Select(z => z.Id).ToArray();

            // Retrieve guard logins in one call
            var guardLogins = _guardDataProvider.GetGuardLogins(guardIds).ToList();
            var guardLotes = _guardDataProvider.GetGuardLotes(guardIds).ToList();
            var guardRcSiteAccess = _guardDataProvider.GetAllGuardRcClientSiteAccess();  //GuardRcSiteAccessCount
            // Create GuardViewModel list in one query
            var guardViewModels = guards.Select(guard =>
                new GuardViewModel(guard, guardLogins.Where(login => login.GuardId == guard.Id).ToList(), guardLotes.ToList())).ToList();

            // Retrieve all document statuses for guard IDs at once
            var documentStatusesByGuard = guardIds.ToDictionary(
                guardId => guardId,
                guardId => LEDStatusForLoginUser(guardId) // Assuming this returns a list
            );

            // Process the status checks
            foreach (var guard in guardViewModels)
            {
                var documentStatuses = documentStatusesByGuard[guard.Id];
                guard.GuardRcSiteAccessCount = guardRcSiteAccess.Count(x => x.GuardId == guard.Id);

                // Initialize default statuses to "Grey"
                guard.HR1Status = "Grey";
                guard.HR2Status = "Grey";
                guard.HR3Status = "Grey";
                guard.hr1Description = string.Empty;
                guard.hr2Description = string.Empty;
                guard.hr3Description = string.Empty;



                if (documentStatuses == null || documentStatuses.Count == 0)
                    continue;

                // Group document statuses by GroupName for faster lookups
                var statusLookup = documentStatuses.ToLookup(x => x.GroupName.Trim());

                // Set HR1Status
                var HR1List = statusLookup["HR 1 (C4i)"];
                if (HR1List.Any())
                {
                    guard.HR1Status = HR1List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR1List.Any(x => x.ColourCodeStatus == "Orange") ? "Orange" :
                                      HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                // Set HR2Status
                var HR2List = statusLookup["HR 2 (Client)"];
                if (HR2List.Any())
                {
                    guard.HR2Status = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR2List.Any(x => x.ColourCodeStatus == "Orange") ? "Orange" :
                                      HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                // Set HR3Status
                var HR3List = statusLookup["HR 3 (Special)"];
                if (HR3List.Any())
                {
                    guard.HR3Status = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR3List.Any(x => x.ColourCodeStatus == "Orange") ? "Orange" :
                                      HR3List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                foreach (var desc in documentStatuses)
                {
                    if (desc.GroupName == "HR 1 (C4i)")
                    {
                        guard.hr1Description = guard.hr1Description + desc.Description + " ";
                    }
                    else if (desc.GroupName == "HR 2 (Client)")
                    {
                        guard.hr2Description = guard.hr2Description + desc.Description + " ";
                    }
                    else if (desc.GroupName == "HR 3 (Special)")
                    {
                        guard.hr3Description = guard.hr3Description + desc.Description + " ";
                    }
                }


            }

            return guardViewModels;
        }
        public List<GuardViewExcelModel> GetGuardsToExcel(bool active, bool inactive, int[] guardIds)
        {
            var listGuardExcel = new List<GuardViewExcelModel>();
            if (guardIds != null && guardIds.Length > 0)
            {
                // Fetch guards based on the provided guardIds
                var guards = _guardDataProvider.GetGuards()
                                                .Where(x => guardIds.Contains(x.Id))
                                                .ToList(); // Materialize the query

                var quaterDeatils = _guardLogDataProvider.GetGuardWorkingHoursInQuater();

                // If there are no guards found, return an empty list
                if (!guards.Any())
                    return listGuardExcel;

                // Fetch guard logins for the found guards in a single call
                var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray())
                                                     .ToList(); // Materialize the query
                var GuardLanguages = _guardDataProvider.GetGuardLanguages(guards.Select(z => z.Id).ToArray())
                                                     .ToList();

                // Create the list of GuardViewExcelModel objects using a single Select
                listGuardExcel = guards.Select(z => new GuardViewExcelModel(z,
                                                    guardLogins.Where(y => y.GuardId == z.Id),
                                                    GuardLanguages.Where(y => y.GuardId == z.Id),
                                                    _guardDataProvider))
                                       .ToList();

                foreach (var item in listGuardExcel)
                {
                    var guardQuaterDeatils = quaterDeatils.Where(x => x.GuardId == item.Id).FirstOrDefault();
                    if (guardQuaterDeatils != null)
                    {
                        item.Q1HRS2023 = guardQuaterDeatils.Q1HRS2023;
                        item.Q2HRS2023 = guardQuaterDeatils.Q2HRS2023;
                        item.Q3HRS2023 = guardQuaterDeatils.Q3HRS2023;
                        item.Q4HRS2023 = guardQuaterDeatils.Q4HRS2023;

                        item.Q1HRS2024 = guardQuaterDeatils.Q1HRS2024;
                        item.Q2HRS2024 = guardQuaterDeatils.Q2HRS2024;
                        item.Q3HRS2024 = guardQuaterDeatils.Q3HRS2024;
                        item.Q4HRS2024 = guardQuaterDeatils.Q4HRS2024;

                        item.Q1HRS2025 = guardQuaterDeatils.Q1HRS2025;
                        item.Q2HRS2025 = guardQuaterDeatils.Q2HRS2025;
                        item.Q3HRS2025 = guardQuaterDeatils.Q3HRS2025;
                        item.Q4HRS2025 = guardQuaterDeatils.Q4HRS2025;

                        //item.Q1HRS2026 = guardQuaterDeatils.Q1HRS2026;
                        //item.Q2HRS2026 = guardQuaterDeatils.Q2HRS2026;
                        //item.Q3HRS2026 = guardQuaterDeatils.Q3HRS2026;
                        //item.Q4HRS2026 = guardQuaterDeatils.Q4HRS2026;
                    }
                    // Assuming GuardViewExcelModel has a string property called 'ColumnName'
                    if (!string.IsNullOrEmpty(item.ClientSites))
                    {
                        var test = Regex.Replace(item.ClientSites, @"<br\s*/?>", "", RegexOptions.IgnoreCase);
                        if (!string.IsNullOrEmpty(test))
                            item.ClientSites = test;
                    }
                }
            }

            return listGuardExcel;


        }

        private List<HRGroupStatusNew> LEDStatusForLoginUser(int GuardID)
        {
            // Retrieve guard document details in one call
            var guardDocumentDetails = _guardDataProvider.GetGuardLicensesandcompliance(GuardID);
            var hrGroupStatusesNew = new List<HRGroupStatusNew>();

            // Iterate through each document detail
            foreach (var item in guardDocumentDetails)
            {
                // Directly use the item without filtering again
                hrGroupStatusesNew.Add(new HRGroupStatusNew
                {
                    Status = 1,
                    GroupName = item.HrGroupText.Trim(), // Assuming HrGroupText replaces GroupName
                                                         // Generate the color code based on the current item
                    ColourCodeStatus = GuardledColourCodeGenerator(new List<GuardComplianceAndLicense> { item }),
                    Description = item.Description,
                });
            }

            return hrGroupStatusesNew;
        }

        private string GuardledColourCodeGenerator(List<GuardComplianceAndLicense> selectedList)
        {
            var today = DateTime.Now;
            var colourCode = "Green"; // Default to green

            if (selectedList.Count > 0)
            {
                // Check if any entry has DateType == true
                var hasDateTypeTrue = selectedList.Any(x => x.DateType == true);

                if (hasDateTypeTrue)
                {
                    return "Green"; // Return immediately if DateType == true exists
                }

                // Get the first non-null expiry date (if any)
                //var firstItem = selectedList.FirstOrDefault(x => x.ExpiryDate != null);
                var firstItem = selectedList
                    .Where(x => x.ExpiryDate != null)
                    .OrderBy(x => x.IsPending)   // false comes first, true comes next
                    .FirstOrDefault();

                if (firstItem != null)
                {
                    var expiryDate = firstItem.ExpiryDate.Value; // Assuming ExpiryDate is not null here
                    var daysAfterExpiry = (today.Date - expiryDate.Date).TotalDays;
                    // Compare expiry date with today's date
                    if (expiryDate < today)
                    {
                        // EXPLANATION: If the record is expired but marked as "Pending" (toggle ON), 
                        // it will show an ORANGE clock to indicate a grace period.
                        // After 99 days past the expiry date, this grace period expires and it forcefully turns RED.
                        if (firstItem.IsPending && daysAfterExpiry <= 99)
                        {
                            return "Orange";
                        }
                        else
                        {
                            return "Red";
                        }
                    }
                    else if ((expiryDate - today).Days < 45)
                    {
                        return "Yellow";
                    }
                }
            }

            return colourCode; // Default return is green
        }
        public async Task<DataTable> PatrolDataToDataTable(List<DailyPatrolData> dailyPatrolData)
        {

            var dt = new DataTable("IR Statistics");
            dt.Columns.Add("Day");
            dt.Columns.Add("Date", typeof(string)); // Use string to hold formatted date
            //  dt.Columns.Add("IR S/No");
            dt.Columns.Add("Control Room Job No.");
            dt.Columns.Add("Site");
            dt.Columns.Add("Address");
            dt.Columns.Add("Desp. Time");
            dt.Columns.Add("Arrival");
            dt.Columns.Add("Depart.");
            dt.Columns.Add("CWS SNo.");
            dt.Columns.Add("Total mins on Site");
            dt.Columns.Add("Resp. Time");
            dt.Columns.Add("Alarm");
            dt.Columns.Add("Patrol Att.");
            dt.Columns.Add("Colour Code");
            dt.Columns.Add("Action Taken");
            dt.Columns.Add("Notified By");
            dt.Columns.Add("Bill To:");
            dt.Columns.Add("File Name");
            dt.Columns.Add("PSPF");
            dt.Columns.Add("File Size(KB)");
            dt.Columns.Add("Hash String");
            foreach (var data in dailyPatrolData)
            {

                try
                {
                    var row = dt.NewRow();
                    row["Day"] = data.NameOfDay;
                    row["Date"] = data.Date;
                    //row["IR S/No"] = data.SerialNo;
                    row["Control Room Job No."] = data.ControlRoomJobNo;
                    row["Site"] = data.SiteName;
                    row["Address"] = data.SiteAddress;
                    row["Desp. Time"] = NormalizeTime(data.DespatchTime);
                    row["Arrival"] = NormalizeTime(data.ArrivalTime);
                    row["Depart."] = NormalizeTime(data.DepartureTime);
                    row["CWS SNo."] = data.SerialNo;
                    row["Total mins on Site"] = data.TotalMinsOnsite;
                    row["Resp. Time"] = NormalizeTime(data.ResponseTime);
                    row["Alarm"] = data.Alarm;
                    row["Patrol Att."] = data.PatrolAttented;
                    row["Colour Code"] = data.ColorCodeStr;
                    row["Action Taken"] = data.ActionTaken;
                    row["Notified By"] = data.NotifiedBy;
                    row["Bill To:"] = data.Billing;
                    row["File Name"] = data.fileNametodownload;
                    row["PSPF"] = data.pspfname;
                    row["File Size(Kb)"] = await data.GetBlobSizeAsync();
                    row["Hash String"] = data.hashvalue;
                    dt.Rows.Add(row);

                }
                catch (Exception ex)
                {

                }
            }

            var sortedRows = dt.AsEnumerable()
                      .OrderBy(row =>
                      {
                          var dateStr = row.Field<string>("Date");
                          return DateTime.TryParseExact(dateStr, "dd MMM yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate)
                              ? parsedDate
                              : DateTime.MinValue;
                      });

            // Create a new sorted DataTable
            DataTable sortedTable = sortedRows.Any() ? sortedRows.CopyToDataTable() : dt.Clone();

            return sortedTable;
            //return dt;
        }


        private string NormalizeTime(object input)
        {
            if (input == null) return string.Empty;

            string str = input.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(str))
                return string.Empty;

            if (TimeSpan.TryParse(str, out var ts))
                return ts.ToString(@"hh\:mm");

            // Optional: log invalid value
            Console.WriteLine($"Invalid time value: '{str}'");

            return string.Empty;
        }
        private string GetFormattedClientSites(IEnumerable<UserClientSiteAccess> userClientSiteAccess)
        {
            var clientSites = userClientSiteAccess.Select(x => x.ClientSite.Name).OrderBy(x => x);
            if (clientSites.Count() == 0)
                return "None";
            if (clientSites.Count() <= 3)
                return string.Join(", ", clientSites);

            return $"{string.Join(", ", clientSites.Take(3))} and {clientSites.Count() - 3} more sites";
        }

        private string GetFormattedClientTypes(IEnumerable<UserClientSiteAccess> userClientSiteAccess)
        {
            var clientTypes = userClientSiteAccess.GroupBy(x => x.ClientSite.ClientType.Name).OrderBy(x => x.Key);
            if (clientTypes.Count() == 0)
                return "None";
            if (clientTypes.Count() <= 3)
                return string.Join(", ", clientTypes.Select(x => x.Key));

            return $"{string.Join(", ", clientTypes.Select(x => x.Key).Take(3))} and {clientTypes.Count() - 3} more clients";
        }

        public List<KeyVehicleLogViewModel> GetKeyVehicleLogs(int logBookId, KvlStatusFilter kvlStatusFilter)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            return _guardLogDataProvider.GetKeyVehicleLogs(logBookId)
                .Select(z => new KeyVehicleLogViewModel(z, kvlFields))
                .Where(r => kvlStatusFilter == KvlStatusFilter.All || r.Status == kvlStatusFilter)
               .ToList();
        }

        public List<KeyVehicleLogViewModel> GetKeyVehicleLogsForIds(int logBookId)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            return _guardLogDataProvider.GetKeyVehicleLogs(logBookId)
                .Select(z => new KeyVehicleLogViewModel(z, kvlFields))
               .ToList();
        }

        public List<ClientSite> GetUserClientSites(string type, string searchTerm)
        {
            var clientSites = _clientDataProvider.GetClientSites(null)
                .Where(z => (string.IsNullOrEmpty(type) || z.ClientType.Name.Equals(type)) &&
                            (string.IsNullOrEmpty(searchTerm) || z.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return clientSites;
        }

        public List<SelectListItem> GetKeyVehicleLogFieldsByType(KvlFieldType type, bool withoutSelect = true)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFieldsByType(type);
            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));
            }

            foreach (var item in kvlFields.OrderBy(x => x.Name))
            {
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }

        public List<KeyVehicleLogProfileViewModel> GetKeyVehicleLogProfilesByRego(string truckRego)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var profiles = _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(truckRego);

            var createdLogIds = profiles.Select(z => z.KeyVehicleLogProfile.CreatedLogId).Where(z => z > 0).ToArray();
            var kvls = _guardLogDataProvider.GetKeyVehicleLogByIds(createdLogIds);
            foreach (var profile in profiles)
            {
                profile.KeyVehicleLogProfile.KeyVehicleLog = kvls.SingleOrDefault(z => z.Id == profile.KeyVehicleLogProfile.CreatedLogId);

                //for checking whether the entry is  either POI,BDM OR SUPPLIER-start
                if (profile.PersonOfInterest != null)
                {
                    profile.POIOrBDM = "POI";
                }
                else if (profile.IsBDM == true && profile.BDMList != null)
                {
                    profile.POIOrBDM = "BDM";
                }
                else if (profile.IsBDM == false)
                {
                    profile.POIOrBDM = "Supplier";
                }
                else
                {
                    profile.POIOrBDM = null;
                }
                //for checking whether the entry is  either POI,BDM OR SUPPLIER-end

            }



            return profiles.Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
        }

        //to check with bdm also-start
        public List<KeyVehicleLogProfileViewModel> GetKeyVehicleLogProfilesByRego(string truckRego, string poi)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var profiles = _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(truckRego);

            var createdLogIds = profiles.Select(z => z.KeyVehicleLogProfile.CreatedLogId).Where(z => z > 0).ToArray();
            var kvls = _guardLogDataProvider.GetKeyVehicleLogByIds(createdLogIds);
            foreach (var profile in profiles)
            {
                //if LOGID=0-START
                //if (profile.KeyVehicleLogProfile.CreatedLogId == 0)
                //{
                //    var list = _guardLogDataProvider.GetKeyVehicleLogs(profile.KeyVehicleLogProfile.VehicleRego);
                //    //if (list.Count != 0)
                //    //{
                //    //    profile.KeyVehicleLogProfile.CreatedLogId = list.Max(x => x.Id);
                //    //}
                //}
                //if LOGID=0-end
                profile.KeyVehicleLogProfile.KeyVehicleLog = kvls.SingleOrDefault(z => z.Id == profile.KeyVehicleLogProfile.CreatedLogId);

                //for checking whether the entry is  either POI,BDM OR SUPPLIER-start
                if (profile.PersonOfInterest != null)
                {
                    profile.POIOrBDM = "POI";
                }
                else if (profile.IsBDM == true && profile.BDMList != null)
                {
                    profile.POIOrBDM = "BDM";
                }
                else if (profile.IsBDM == false)
                {
                    profile.POIOrBDM = "Supplier";
                }
                else
                {
                    profile.POIOrBDM = null;
                }
                //for checking whether the entry is  either POI,BDM OR SUPPLIER-end

            }

            var kvlIds = kvls.Select(z => z.Id).ToArray();

            // return profiles.Where(z => (string.IsNullOrEmpty(poi) || string.Equals(z.POIOrBDM, poi)) || (z.KeyVehicleLogProfile.CreatedLogId == 0 || kvlIds.Contains(z.KeyVehicleLogProfile.CreatedLogId))).Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
            return profiles.Where(z => (string.IsNullOrEmpty(poi) || string.Equals(z.POIOrBDM, poi))).Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
        }
        //to check with bdm also-end
        public List<KeyVehicleLogProfileViewModel> GetKeyVehicleLogProfilesByRegoNew(string truckRego, string Image)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var profiles = _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(truckRego).Where(p => !string.IsNullOrWhiteSpace(p.CompanyName)
             && !string.IsNullOrWhiteSpace(p.PersonName)).GroupBy(p => new { p.CompanyName, p.PersonName })
    .Select(g => g.OrderByDescending(x => x.Id).First());

            var createdLogIds = profiles.Select(z => z.KeyVehicleLogProfile.CreatedLogId).Where(z => z > 0).ToArray();
            var kvls = _guardLogDataProvider.GetKeyVehicleLogByIds(createdLogIds);
            foreach (var profile in profiles)
            {
                profile.KeyVehicleLogProfile.KeyVehicleLog = kvls.SingleOrDefault(z => z.Id == profile.KeyVehicleLogProfile.CreatedLogId);
                if (profile.PersonOfInterest != null)
                {
                    profile.POIImageDisplay = "<img  src=" + profile.POIImage + " height=35px width=35px class=ml-2 />";
                    //  profile.POIImage = "Yes";
                }
                else
                {
                    profile.POIImageDisplay = null;
                }
            }

            return profiles.Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
        }


        public List<TrailerDeatilsViewModel> GetKeyVehicleTrailerNew(string truckRego)
        {
            return _guardLogDataProvider.GetKeyVehicleLogProfileDetails(truckRego);

        }



        public List<SelectListItem> VehicleRegos
        {
            get
            {
                var items = new List<SelectListItem>()
                {
                    new SelectListItem("All", string.Empty, true)
                };

                var vehicleRegos = _guardLogDataProvider.GetVehicleRegos();
                foreach (var item in vehicleRegos)
                {
                    items.Add(new SelectListItem(item, item));
                }
                return items;
            }

        }
        public List<SelectListItem> POIBDMSupplier
        {
            get
            {
                var items = new List<SelectListItem>()
                {
                    new SelectListItem("All", string.Empty, true)
                };


                items.Add(new SelectListItem("POI", "POI"));
                items.Add(new SelectListItem("CRM BDM", "BDM"));
                items.Add(new SelectListItem("CRM Supplier", "Supplier"));


                return items;
            }

        }

        public List<ClientSiteKey> GetClientSiteKeys(int clientSiteId, string searchKeyNo, string searchKeyDesc)
        {
            var clientSiteKeys = _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId)
                                    .Where(z => string.IsNullOrEmpty(searchKeyNo) || z.KeyNo.Contains(searchKeyNo, StringComparison.OrdinalIgnoreCase))
                                    .ToList();

            if (!string.IsNullOrEmpty(searchKeyDesc))
            {
                var searchTerms = searchKeyDesc.Split(new[] { ',', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    clientSiteKeys = clientSiteKeys.Where(z => z.Description.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            return clientSiteKeys;
        }

        public List<ClientSiteKey> GetClientSiteKeysbySearchDesc(int clientSiteId, string searchKeyDesc)
        {
            var clientSiteKeys = _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).ToList();

            if (!string.IsNullOrEmpty(searchKeyDesc))
            {
                var searchTerms = searchKeyDesc.Split(new[] { ',', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    clientSiteKeys = clientSiteKeys.Where(z => z.Description.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            return clientSiteKeys;
        }

        public int GetNewGuardLoginId(GuardLogin currentGuardLogin, DateTime? currentGuardLoginOffDutyActual, int newLogBookId)
        {
            var onDutyDate = DateTime.Today;
            var newGuardLogin = new GuardLogin()
            {
                LoginDate = DateTime.Now,
                GuardId = currentGuardLogin.GuardId,
                ClientSiteId = currentGuardLogin.ClientSiteId,
                PositionId = currentGuardLogin.PositionId,
                SmartWandId = currentGuardLogin.SmartWandId,
                OnDuty = new DateTime(onDutyDate.Year, onDutyDate.Month, onDutyDate.Day, 00, 01, 0),
                OffDuty = currentGuardLoginOffDutyActual,
                UserId = currentGuardLogin.UserId,
                ClientSiteLogBookId = newLogBookId
            };

            return _guardDataProvider.SaveGuardLogin(newGuardLogin);
        }

        public int GetNewClientSiteLogBookId(int clientSiteId, LogBookType logBookType)
        {
            return _logbookDataService.GetNewOrExistingClientSiteLogBookId(clientSiteId, logBookType);
        }

        public string GetClientSiteKeyDescription(int keyId, int clientSiteId)
        {
            return _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).SingleOrDefault(z => z.Id == keyId)?.Description;
        }
        public ClientSiteKey GetClientSiteKeyDescriptionAndImage(int keyId, int clientSiteId)
        {
            return _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).SingleOrDefault(z => z.Id == keyId);
        }
        public string GetClientSiteKeyNo(int keyId, int clientSiteId)
        {
            return _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).SingleOrDefault(z => z.Id == keyId)?.KeyNo;
        }
        public ANPR GetANPR(int clientSiteId)
        {
            return _guardSettingsDataProvider.GetANPRCheckbox(clientSiteId);
        }
        public void CopyOpenLogbookEntriesFromPreviousDay(int previousDayLogBookId, int logBookId, int guardLoginId)
        {
            var kvlFieldsToLookup = _guardLogDataProvider.GetKeyVehicleLogFields()
                .Where(z => z.Name == "Law Enforcement" || z.Name == "Emergency Services" || z.Name == "Emergency Situation")
                .ToDictionary(z => z.Name, z => z.Id);

            var previousDayLogs = _guardLogDataProvider.GetKeyVehicleLogs(previousDayLogBookId);

            // p7#136 -Update to midnight logic start
            var logsToCopy = previousDayLogs.Where(z => !z.ExitTime.HasValue && !z.HasLoadVariation &&
                (z.EntryTime.HasValue ||
                (kvlFieldsToLookup.TryGetValue("Law Enforcement", out int idLawEnforce) && z.PersonType == idLawEnforce) ||
                    (kvlFieldsToLookup.TryGetValue("Emergency Services", out int idEms) && z.PersonType == idEms) ||
                    (kvlFieldsToLookup.TryGetValue("Emergency Situation", out int idEmSituation) && z.EntryReason == idEmSituation) ||
                    !string.IsNullOrEmpty(z.KeyNo)));
            // p7#136 -Update to midnight logic end



            if (logsToCopy.Any())
            {
                foreach (var logToCopy in logsToCopy)
                {
                    // Create new Keyvechilelog using Json to avoid the reference issue. This done so the new fields automatically gets copied.
                    var newLog = JsonSerializer.Deserialize<KeyVehicleLog>(JsonSerializer.Serialize(logToCopy));
                    var newtime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 00, 01, 0);

                    newLog.Id = 0;
                    newLog.InitialCallTime = newtime;
                    newLog.EntryTime = newtime;
                    newLog.SentInTime = newtime;
                    newLog.ClientSiteLogBookId = logBookId;
                    newLog.GuardLoginId = guardLoginId;
                    newLog.CopiedFromId = logToCopy.Id;

                    //Make all ForeignKey null for new entry otherwise add will fail with Instance tracking issue. 
                    // #########  Any new ForeignKey added in KeyVehicleLog must be made null here to avoid Instance tracking issue  ########
                    newLog.ClientSiteLogBook = null;
                    newLog.GuardLogin = null;
                    newLog.ClientSiteLocation = null;
                    newLog.ClientSitePoc = null;

                    try
                    {
                        _guardLogDataProvider.InsertPreviousLogBook(newLog);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            // Task P7#129 Yellow wont roll over  - Binoy 29-07-2024 -- Start
            // To rollover previous days pending yellow entries to new logbook
            var pendinglogentries = previousDayLogs.Where(z => !z.ExitTime.HasValue && !z.EntryTime.HasValue && !z.SentInTime.HasValue && z.InitialCallTime.HasValue && !z.HasLoadVariation);
            if (pendinglogentries.Count() > 0)
            {
                foreach (var logToCopy in pendinglogentries)
                {
                    // Create new Keyvechilelog using Json to avoid the reference issue This done so the new fields automatically gets copied.
                    var newLog = JsonSerializer.Deserialize<KeyVehicleLog>(JsonSerializer.Serialize(logToCopy));
                    var newtime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 00, 01, 0);

                    newLog.Id = 0;
                    newLog.InitialCallTime = newtime;
                    newLog.ClientSiteLogBookId = logBookId;
                    newLog.GuardLoginId = guardLoginId;
                    newLog.CopiedFromId = logToCopy.Id;

                    //Make all ForeignKey null for new entry otherwise add will fail with Instance tracking issue
                    // #########  Any new ForeignKey added in KeyVehicleLog must be made null here to avoid Instance tracking issue  ########
                    newLog.ClientSiteLogBook = null;
                    newLog.GuardLogin = null;
                    newLog.ClientSiteLocation = null;
                    newLog.ClientSitePoc = null;

                    try
                    {
                        _guardLogDataProvider.InsertPreviousLogBook(newLog);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            // Task P7#129 Yellow wont roll over  - Binoy 29-07-2024 -- End

        }

        public IEnumerable<string> GetKeyVehicleLogAttachments(string uploadsDir, string reportReference)
        {
            if (!string.IsNullOrEmpty(reportReference))
            {
                var folderPath = Path.Combine(uploadsDir, reportReference);
                if (Directory.Exists(folderPath))
                {
                    var files = Directory.GetFiles(folderPath);
                    if (files.Any())
                    {
                        return files.Select(z => Path.GetFileName(z));
                    }
                }
            }
            return Enumerable.Empty<string>();
        }

        public IEnumerable<ClientSiteKey> GetKeyVehicleLogKeys(KeyVehicleLog keyVehicleLog)
        {
            if (!string.IsNullOrEmpty(keyVehicleLog.KeyNo))
            {
                var keys = keyVehicleLog.KeyNo.Split(';').Select(z => z.Trim());
                if (keys.Any())
                {
                    return _guardSettingsDataProvider
                        .GetClientSiteKeys(keyVehicleLog.ClientSiteLogBook.ClientSiteId)
                        .Where(z => keys.Contains(z.KeyNo))
                        .Select(z => z);
                }
            }
            return Enumerable.Empty<ClientSiteKey>();
        }
        public List<SelectListItem> GetClientSitePocsVehicleLog(int[] clientSiteIds)
        {
            var sitePocs = new List<SelectListItem>();

            sitePocs.AddRange(_guardSettingsDataProvider.GetClientSitePocs(clientSiteIds)
                .Select(z => new SelectListItem(z.Name, z.Id.ToString())));

            return sitePocs;
        }
        public IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistory(string vehicleRego)
        {
            var kvlVisitorProfile = _guardLogDataProvider.GetKeyVehicleLogVisitorProfile(vehicleRego);
            return GetKeyVehicleLogAuditHistory(kvlVisitorProfile.Id);
        }

        public IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistory(int profileId)
        {
            return _guardLogDataProvider.GetAuditHistory(profileId)
                .OrderByDescending(z => z.Id)
                .ThenByDescending(z => z.AuditTime);
        }

        public IEnumerable<string> GetCompanyAndSenderNames(string startsWith)
        {
            var companyNames = _guardLogDataProvider.GetCompanyNames(startsWith);
            var senderNames = _guardLogDataProvider.GetSenderNames(startsWith);

            return companyNames.Concat(senderNames).Distinct().OrderBy(x => x).ToList();
        }

        public IEnumerable<string> GetCompanyNames(string startsWith)
        {
            return _guardLogDataProvider.GetCompanyNames(startsWith);
        }

        public List<ClientSite> GetNewUserClientSites()
        {


            var clientSites = _clientDataProvider.GetNewClientSites();

            return clientSites;
        }

        public bool IsClientSiteDuressEnabled(int clientSiteId)
        {
            return _guardLogDataProvider.GetClientSiteDuress(clientSiteId)?.IsEnabled ?? false;
        }

        public void EnableClientSiteDuress(int clientSiteId, int guardLoginId, int logBookId, int guardId,
                                            string gpsCoordinates, string enabledAddress, GuardLog tmzdata, string clientSiteName, string GuradName)
        // GuardLog tmzdata parameter added by binoy for Task p6#73_TimeZone issue
        {
            if (!IsClientSiteDuressEnabled(clientSiteId))
            {


                /* Save the push message for reload to logbook on next day Start*/
                DateTime? logBook_Date = null;
                logBook_Date = _guardDataProvider.GetLogbookDateFromLogbook(logBookId); // p6#73 timezone bug - Added by binoy 24-01-2024
                var localDateTime = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmzdata.EventDateTimeUtcOffsetMinute);
                var radioCheckPushMessages = new RadioCheckPushMessages()
                {
                    ClientSiteId = clientSiteId,
                    LogBookId = logBookId,
                    Notes = "Duress Alarm Activated By " + GuradName + " From " + clientSiteName,
                    EntryType = (int)IrEntryType.Alarm,
                    Date = logBook_Date.Value,
                    IsAcknowledged = 0,
                    IsDuress = 1
                };
                var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);
                /* Save the push message for reload to logbook on next day end*/

                _guardLogDataProvider.LogBookEntryForRcControlRoomMessages(guardId, guardId, null, "Duress Alarm Activated By " + GuradName + " From " + clientSiteName, IrEntryType.Alarm, 1, 0, tmzdata); // GuardLog tmzdata parameter added by binoy for Task p6#73_TimeZone issue
                _guardLogDataProvider.SaveClientSiteDuress(clientSiteId, guardId, gpsCoordinates, enabledAddress, tmzdata, clientSiteId, 1);

                _guardLogDataProvider.SaveGuardLog(new GuardLog()
                {
                    Notes = "Duress Alarm Activated By " + GuradName + " From " + clientSiteName,
                    IsSystemEntry = true,
                    IrEntryType = Data.Enums.IrEntryType.Alarm,
                    EventDateTime = DateTime.Now,
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    RcPushMessageId = pushMessageId,
                    EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute, // Task p6#73_TimeZone issue -- added by Binoy - End
                    PlayNotificationSound = true,
                    GpsCoordinates = gpsCoordinates
                });

                /* enable linked site duress  start */
                /*Check if the site is a linked duress site*/
                var ifSiteisLinkedDuressSite = _guardLogDataProvider.checkIfASiteisLinkedDuress(clientSiteId);
                if (ifSiteisLinkedDuressSite.Count != 0)
                {   /*get all linked duress sites */
                    var allLinkedSites = _guardLogDataProvider.getallClientSitesLinkedDuress(clientSiteId);
                    if (allLinkedSites.Count != 0)
                    {

                        foreach (var linkedSite in allLinkedSites)
                        {
                            /* avoid Repete entery for duress enabled site */
                            if (linkedSite.ClientSiteId != clientSiteId)
                            {
                                var ClientsiteDetails = _clientDataProvider.GetClientSiteName(linkedSite.ClientSiteId);
                                var localDateTimeLinked = DateTimeHelper.GetCurrentLocalTimeFromUtcMinute((int)tmzdata.EventDateTimeUtcOffsetMinute);
                                var logBookIdLinked = _guardLogDataProvider.GetClientSiteLogBookIdGloablmessage(ClientsiteDetails.Id, LogBookType.DailyGuardLog, localDateTimeLinked.Date);
                                var radioCheckPushMessagesLinked = new RadioCheckPushMessages()
                                {
                                    ClientSiteId = linkedSite.ClientSiteId,
                                    LogBookId = logBookIdLinked,
                                    //Notes = "Duress Alarm[Linked] Activated By " + GuradName + " From " + ClientsiteDetails.Name,
                                    Notes = "Duress Alarm[Linked] Activated By " + GuradName + " From " + clientSiteName,

                                    EntryType = (int)IrEntryType.Alarm,
                                    Date = logBook_Date.Value,
                                    IsAcknowledged = 0,
                                    IsDuress = 1
                                };
                                var pushMessageIdSave = _guardLogDataProvider.SavePushMessage(radioCheckPushMessagesLinked);
                                _guardLogDataProvider.LogBookEntryForRcControlRoomMessages(guardId, guardId, null, "Duress Alarm[Linked] Activated By " + GuradName + " From " + ClientsiteDetails.Name, IrEntryType.Alarm, 1, 0, tmzdata); // GuardLog tmzdata parameter added by binoy for Task p6#73_TimeZone issue
                                _guardLogDataProvider.SaveClientSiteDuress(linkedSite.ClientSiteId, guardId, gpsCoordinates, enabledAddress, tmzdata, clientSiteId, 0);

                                _guardLogDataProvider.SaveGuardLog(new GuardLog()
                                {
                                    //Notes = "Duress Alarm[linked] Activated By " + GuradName + " From " + ClientsiteDetails.Name,
                                    Notes = "Duress Alarm[Linked] Activated By " + GuradName + " From " + clientSiteName,
                                    IsSystemEntry = true,
                                    IrEntryType = Data.Enums.IrEntryType.Alarm,
                                    EventDateTime = DateTime.Now,
                                    ClientSiteLogBookId = logBookIdLinked,
                                    GuardLoginId = guardLoginId,
                                    RcPushMessageId = pushMessageId,
                                    EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute, // Task p6#73_TimeZone issue -- added by Binoy - End
                                    PlayNotificationSound = true,
                                    GpsCoordinates = gpsCoordinates
                                });

                            }
                        }

                    }

                }
                /* enable linked site duress  end */

            }
        }


        //code added for Guard Access Dropdown start
        //public List<GuardAccess> GetAccessTypes()
        //{
        //    return _clientDataProvider.GetGuardAccess();
        public List<SelectListItem> GetAccessTypes(bool withoutSelect = true)
        {
            var Access = _clientDataProvider.GetGuardAccess();
            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));
            }

            foreach (var item in Access)
            {
                items.Add(new SelectListItem(item.AccessName, item.Id.ToString()));
            }

            return items;
        }
        public List<SelectListItem> GetAccessTypes1(bool withoutSelect = true)
        {
            var Access = _clientDataProvider.GetGuardAccess();
            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));
            }

            foreach (var item in Access)
            {
                items.Add(new SelectListItem(item.AccessName, item.Id.ToString()));
            }

            return items;
        }

        public IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryWithPersonName(string PersonName)
        {
            var kvlVisitorProfile = _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetailsWithPersonName(PersonName);
            var history = new List<KeyVehicleLogAuditHistory>();
            foreach (var item in kvlVisitorProfile)
            {
                var hist = GetKeyVehicleLogAuditHistoryNew(item.ProfileId);
                foreach (var item2 in hist)
                {
                    item2.KeyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogsByID(item2.KeyVehicleLogId).FirstOrDefault();

                }
                var newhist = hist.Where(x => x.KeyVehicleLog.PersonName == PersonName);
                history.AddRange(newhist);
            }

            return history;
        }
        public List<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryNew(int profileId)
        {
            return _guardLogDataProvider.GetAuditHistory(profileId)
                .OrderByDescending(z => z.Id)
                .ThenByDescending(z => z.AuditTime).ToList();
        }
        public IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryWithKeyNo(string KeyNo)
        {
            var kvlVisitorProfile = _guardLogDataProvider.GetKeyVehicleLogsWithKeyNo(KeyNo);
            var history = new List<KeyVehicleLogAuditHistory>();
            foreach (var item in kvlVisitorProfile)
            {
                var hist = GetKeyVehicleLogAuditHistoryWithKeyVehicleLogId(item.Id);

                foreach (var item2 in hist)
                {
                    if (item2.AuditMessage == "Initial entry")
                    {
                        item2.AuditMessage = "Key received";
                    }
                    if (item2.AuditMessage == "Exit entry")
                    {
                        item2.AuditMessage = "Key returned";
                    }
                }
                var newhist = hist;
                history.AddRange(newhist);
            }

            return history;
        }
        public List<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryWithKeyVehicleLogId(int keyVehicleId)
        {
            return _guardLogDataProvider.GetAuditHistoryWithKeyVehicleLogId(keyVehicleId)
                .OrderByDescending(z => z.Id)
                .ThenByDescending(z => z.AuditTime).ToList();
        }
        public string GetFeedbackTemplatesByTypeByColor(int type, int id)
        {

            var item = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == type && z.Id == id);
            string st1 = string.Empty;
            foreach (var it1 in item)
            {
                st1 = it1.Name;
            }

            return st1;
        }
        //p2-192 client email search-start
        public List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm, string searchTermtwo)
        {
            var results = new List<ClientSite>();
            var clientSites = _clientDataProvider.GetClientSites(typeId);
            if (userId == null)
                results = clientSites;
            else
            {
                var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
                var clientSiteIds = allUserAccess.Select(x => x.ClientSite.Id).Distinct().ToList();
                results = clientSites.Where(x => clientSiteIds.Contains(x.Id)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
                results = results.Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(x.Address) && x.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))).ToList();
            if (!string.IsNullOrEmpty(searchTermtwo))
                results = results.Where(x => !string.IsNullOrEmpty(x.Emails) && x.Emails.Contains(searchTermtwo, StringComparison.OrdinalIgnoreCase)).ToList();

            return results;
        }
        //p2-192 client email search-end
        public List<ClientSiteWithWands> GetUserClientSitesExcel(int? typeId, int? userId)
        {
            var results = new List<ClientSite>();
            var clientSites = _clientDataProvider.GetClientSites(typeId);

            // Fetch all KPI settings and details in bulk
            var siteIds = clientSites.Select(cs => cs.Id).ToList();
            var allClientSiteSettings = _clientDataProvider.GetClientSiteKpiSettings(siteIds).ToList();
            var kpiFieldIds = allClientSiteSettings
                .Where(s => s.KPITelematicsFieldID.HasValue)
                .Select(s => s.KPITelematicsFieldID.Value)
                .Distinct()
                .ToList();
            var allKpiFields = _clientDataProvider.GetKPITelematicsDetailsNew(kpiFieldIds).ToList();

            var kpiFieldLookup = allKpiFields.ToDictionary(k => k.Id, k => k.Name); // Assuming Id is the unique key

            foreach (var site in clientSites)
            {
                // Get the first matching KPI setting for the site
                var siteSetting = allClientSiteSettings.FirstOrDefault(s => s.ClientSiteId == site.Id);

                if (siteSetting != null &&
                    siteSetting.KPITelematicsFieldID.HasValue &&
                    kpiFieldLookup.TryGetValue(siteSetting.KPITelematicsFieldID.Value, out var accountManager))
                {
                    site.AccountManager = accountManager; // Assign the AccountManager
                }

                results.Add(site);
            }

            if (userId == null)
            {
                results = clientSites;
            }
            else
            {
                var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
                var clientSiteIds = allUserAccess.Select(x => x.ClientSite.Id).Distinct().ToList();
                results = clientSites.Where(x => clientSiteIds.Contains(x.Id)).ToList();
            }

            var clientSiteSmartWands = _clientDataProvider.GetClientSmartWand();
            //p2-171-equipment -start
            var siteEquipments = _clientSiteWandDataProvider.GetClientSiteEquipments(); // to get all the site equipments
            var groupedEquipments = siteEquipments
            .GroupBy(x => new    // to group by client site id and Equipment types
            {
                x.ClientSiteId,
                EquipmentType = x.KPITelematicsField.Name
            })
            .Select(g => new
            {
                g.Key.ClientSiteId,  // display client site id , equipment types and items under each equipment
                Equipment = new SiteEquipmentsViewModelcs
                {
                    EquipmentType = g.Key.EquipmentType,
                    Items = g.Select(i => new EquipmentItemDetails
                    {
                        Id = i.Id,
                        SerialNumber = i.SerialNo,
                        Brand = i.Brand
                    }).ToList()
                }
            })
            .ToList();

            //p2-171-equipment -end
            // Join ClientSite with ClientSiteSmartWands using ClientSiteId
            var finalResults = results
                .Select(clientSite => new ClientSiteWithWands
                {
                    ClientSite = clientSite,
                    SmartWands = clientSiteSmartWands
                        .Where(smartWand => smartWand.ClientSiteId == clientSite.Id)
                        .ToList(),
                    Equipments = groupedEquipments
                        .Where(e => e.ClientSiteId == clientSite.Id)
                        .Select(e => e.Equipment)
                        .ToList()
                })
                .ToList();

            return finalResults;
        }
        public class ClientSiteWithWands
        {
            public ClientSite ClientSite { get; set; }
            public List<ClientSiteSmartWand> SmartWands { get; set; }
            //p2-171-equipmets--start
            public List<SiteEquipmentsViewModelcs> Equipments { get; set; } // to get the quipments
            //p2-171-equipmets--end
        }
        //p1-191 HR Files Task 3-start

        public List<SelectListItem> GetHRGroups(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetHRGroups();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", "", true));
            //}

            foreach (var item in hrGroups)
            {
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }
        public List<SelectListItem> GetReferenceNoNumbers(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetReferenceNoNumbers();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", "", true));
            //}

            foreach (var item in hrGroups)
            {
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }
        public List<SelectListItem> GetReferenceNoAlphabets(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetReferenceNoAlphabets();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", "", true));
            // }

            foreach (var item in hrGroups)
            {
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }
        //p1-191 HR Files Task 3-end
        public List<SelectListItem> GetLicenseTypes(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetLicenseTypes().Where(x => x.IsDeleted == false);
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", "", true));
            //}

            foreach (var item in hrGroups)
            {
                items.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            return items;
        }
        //p1-202 site allocation-start
        public List<SelectListItem> GetClientAreas(IncidentReportField ir)
        {

            var items = new List<SelectListItem>() { new SelectListItem("Select", "", true) };
            var clientArea = _configDataProvider.GetReportFieldsByType(ReportFieldType.ClientArea);
            foreach (var item in clientArea)
            {
                if (!String.IsNullOrEmpty(item.ClientSiteIds))
                {
                    foreach (var clientsiteid in item.ClientSiteIdsNew)
                    {
                        if (clientsiteid.Equals(Convert.ToInt16(ir.ClientSiteIds)))
                        {
                            items.Add(new SelectListItem(item.Name, item.Name));
                        }
                    }
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Name));
                }
            }
            return items.ToList();

        }

        //p1-202 site allocation-end

        //p1-213 Critical Documents start
        public List<SelectListItem> GetClientSites(string type = "")
        {
            var sites = new List<SelectListItem>();
            var mapping = _clientDataProvider.GetClientSites(null).Where(x => x.ClientType.Name == type).OrderBy(clientType => clientType.Name);
            foreach (var item in mapping)
            {
                sites.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }
            return sites;
        }
        public List<HRGroups> GetHRGroups()
        {
            var HRGropList = _clientDataProvider.GetHRGroups();
            return HRGropList;
        }

        //p1-213 Critical Documents stop

        public List<FileDownloadAuditLogs> GetFileDownloadAuditLogs(DateTime logFromDate, DateTime logToDate)
        {
            return _guardLogDataProvider.GetFileDownloadAuditLogsData(logFromDate, logToDate);
        }
        public IEnumerable<string> GetDailyGuardLogAttachments(string uploadsDir, string reportReference)
        {
            if (!string.IsNullOrEmpty(reportReference))
            {
                var folderPath = Path.Combine(uploadsDir, reportReference);
                if (Directory.Exists(folderPath))
                {
                    var files = Directory.GetFiles(folderPath);
                    if (files.Any())
                    {
                        return files.Select(z => Path.GetFileName(z));
                    }
                }
            }
            return Enumerable.Empty<string>();
        }

        public List<SelectListItem> GetOfficerPositionsNew(OfficerPositionFilter positionFilter = OfficerPositionFilter.All)
        {
            var items = new List<SelectListItem>()
            {
                new SelectListItem("Select", "", true),
            };
            var officerPositions = _configDataProvider.GetPositions();
            foreach (var officerPosition in officerPositions.Where(z => positionFilter == OfficerPositionFilter.All ||
                 positionFilter == OfficerPositionFilter.PatrolOnly && z.IsPatrolCar ||
                 positionFilter == OfficerPositionFilter.NonPatrolOnly && !z.IsPatrolCar ||
                 positionFilter == OfficerPositionFilter.SecurityOnly && z.Name.Contains("Security")))
            {
                items.Add(new SelectListItem(officerPosition.Name, officerPosition.Id.ToString()));



            }

            return items;
        }


        public List<SelectListItem> GetLanguageMaster(bool withoutSelect = true)
        {
            var Access = _clientDataProvider.GetLanguages();
            var items = new List<SelectListItem>();
            if (!withoutSelect)
            {
                items.Add(new SelectListItem("Select", "", true));

            }
            foreach (var item in Access)
            {
                items.Add(new SelectListItem(item.Language, item.Id.ToString()));
            }

            return items;

        }



        public List<SelectListItem> GetLanguages(bool withoutSelect = true)
        {
            var Access = _clientDataProvider.GetLanguages();

            var items = new List<SelectListItem>();

            if (!withoutSelect)
            {

                items.Add(new SelectListItem("Select", "", false));



            }

            foreach (var item in Access)
            {
                items.Add(new SelectListItem(item.Language, item.Id.ToString()));
            }

            return items;
        }
        public List<SelectListItem> GetCourseDuration(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetCourseDuration();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", ""));
            //}

            foreach (var item in hrGroups)
            {
                if (item.Id == 3)
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString(), true));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }
        public List<SelectListItem> GetTestDuration(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetTestDuration();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", ""));
            //}

            foreach (var item in hrGroups)
            {
                if (item.Id == 3)
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString(), true));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }
        public List<SelectListItem> GetPassMark(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetPassMark();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", ""));
            //}

            foreach (var item in hrGroups)
            {
                if (item.Id == 3)
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString(), true));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }
        public List<SelectListItem> GetTestAttempts(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetTestAttempts();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", ""));
            //}


            foreach (var item in hrGroups)
            {
                if (item.Id == 1)
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString(), true));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }
        public List<SelectListItem> GetTrainingCertificateExpiryYears(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetTrainingCertificateExpiryYears();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", "", true));
            //}

            foreach (var item in hrGroups)
            {

                items.Add(new SelectListItem(item.Name, item.Id.ToString()));

            }

            return items;
        }
        public List<SelectListItem> GetTestQuestionNumbers(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetTestQuestionNumbers();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            items.Add(new SelectListItem("Select", ""));
            //}

            foreach (var item in hrGroups)
            {
                if (item.Id == 1)
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString(), true));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }

        public List<SelectListItem> GetTestTQNumbers(bool withoutSelect = true)
        {
            var hrGroups = _guardDataProvider.GetTestTQNumbers();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            // items.Add(new SelectListItem("Select", ""));
            //}

            foreach (var item in hrGroups)
            {
                if (item.Id == 1)
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString(), true));
                }
                else
                {
                    items.Add(new SelectListItem(item.Name, item.Id.ToString()));
                }
            }

            return items;
        }



        public List<SelectListItem> GetPracticalLocation(bool withoutSelect = true)
        {
            var hrGroups = _guardLogDataProvider.GetTrainingLocation();
            var items = new List<SelectListItem>();

            //if (!withoutSelect)
            //{
            //items.Add(new SelectListItem("Select", "", true));
            //}

            foreach (var item in hrGroups)
            {

                if (item.Id == 1)
                {
                    items.Add(new SelectListItem(item.Location, item.Id.ToString(), true));
                }
                else
                    items.Add(new SelectListItem(item.Location, item.Id.ToString()));

            }

            return items;
        }

        public List<ActivityModel> GetDressAppFields(int type, int? siteid = 0)
        {
            var hrGroups = _guardLogDataProvider.GetDuressAppFields(type, siteid);

            // Convert the list of DuressAppField to DropdownItem
            return hrGroups.Select(x => new ActivityModel
            {
                Id = x.Id,
                Name = x.Name,
                Label = x.Label
            }).ToList();
        }

        public List<ActivityModelDTO> GetPreDefinedActivitesFields()
        {
            var hrGroups = _guardLogDataProvider.GetActivityModels();
            return hrGroups;
        }

        public List<Mp3File> GetDressAppFieldsAudio(int type)
        {
            string baseUrl = "https://cws-ir.com/DuressAppAudio/";
            var audio = _guardLogDataProvider.GetDuressAppFields(type);

            if (type == 3)
            {
                baseUrl = "https://cws-ir.com/DuressAppMultimedia/"; // Your base URL

            }


            return audio.Select(x => new Mp3File
            {
                Label = x.Label,
                Url = $"{baseUrl}{Uri.EscapeDataString(x.Name)}" // Constructing dynamic URL

            }).ToList();
        }

        public List<DropdownItem> GetUserClientTypesWithId(int? userId)
        {
            var clientTypes = GetUserClientTypesHavingAccess(userId);

            // Ensure sorting is done in a single step
            var sortedClientTypes = clientTypes
                .OrderByDescending(clientType => GetClientTypeCount(clientType.Id))
                .ThenBy(clientType => clientType.Name)
                .ToList(); // Materialize the collection

            // Initialize with the default "Select" option
            var items = new List<DropdownItem>
    {
        new DropdownItem { Id = 0, Name = "Select" }
    };

            // Add sorted client types
            items.AddRange(sortedClientTypes.Select(item =>
                new DropdownItem
                {
                    Id = item.Id,
                    Name = $"{item.Name} ({GetClientTypeCount(item.Id)})"
                }
            ));

            return items;
        }


        public List<DropdownItem> GetUserClientSitesUsingId(int? userId, int id)
        {
            var sites = new List<DropdownItem>
    {
        new DropdownItem { Id = 0, Name = "Select" } // Default option
    };

            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Id == id);

            if (clientType != null)
            {
                var mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty);

                sites.AddRange(mapping.Select(item => new DropdownItem
                {
                    Id = item.Id,
                    Name = item.Name
                }));
            }

            return sites;
        }

        public List<ClientSite> GetUserClientSitesFromUserId(int? userId, int id)
        {
            List<ClientSite> mapping = new List<ClientSite>();

            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Id == id);

            if (clientType != null)
            {
                mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty);
            }

            return mapping;
        }

        public ClientSiteMobileAppSettings GetCrowdSettingForSite(int siteId)
        {
            return _configDataProvider.GetCrowdSettingForSite(siteId);
        }

        public async Task<ClientSiteMobileCrowdControlDTO> GetCrowdCountControlDataAndSettings(int siteId)
        {

            ClientSiteMobileCrowdControlDTO cdto = new ClientSiteMobileCrowdControlDTO();
            var Cs = _configDataProvider.GetCrowdSettingForSite(siteId);

            if (Cs == null) return cdto;

            if (Cs != null)
            {
                cdto.ClientSiteId = Cs.ClientSiteId;
                cdto.IsCrowdCountEnabled = Cs.IsCrowdCountEnabled;
                cdto.IsDoorEnabled = Cs.IsDoorEnabled;
                cdto.IsGateEnabled = Cs.IsGateEnabled;
                cdto.IsLevelFloorEnabled = Cs.IsLevelFloorEnabled;
                cdto.IsRoomEnabled = Cs.IsRoomEnabled;
                cdto.CounterQuantity = Cs.CounterQuantity;
                cdto.IsGateEnabled = Cs.IsGateEnabled;
                cdto.IsGateEnabled = Cs.IsGateEnabled;
                cdto.IsGateEnabled = Cs.IsGateEnabled;

            }

            var histLocData = await _configDataProvider.GetCrowdControlHistoryDataForSite(siteId);
            var locGuardData = await _configDataProvider.GetCrowdControlLocationDataForSite(siteId);
            var currLocData = await _configDataProvider.GetCrowdControlDataForSite(siteId);

            cdto.CurrentCount = currLocData?.Ccount ?? 0;
            cdto.TotalCount = currLocData?.Tcount ?? 0;

            cdto.TillDateCount = histLocData?.Sum(x => x.Tcount) ?? 0;
            cdto.TillDateCount += currLocData?.Tcount ?? 0;
            cdto.TillDate = (currLocData.CrowdControlDate.HasValue ? currLocData.CrowdControlDate.Value.ToString("dd MMM yyyy") : "");



            if (Cs.IsDoorEnabled)
            {
                for (int i = 1; i <= Cs.CounterQuantity; i++)
                {
                    var locNme = $"Door {i:00}";
                    var lccount = locGuardData?.Where(x => x.Location == locNme).Sum(x => x.Pcount) ?? 0;
                    cdto.CounterNameAndCount.Add(locNme, lccount);
                }
            }
            if (Cs.IsGateEnabled)
            {
                for (int i = 1; i <= Cs.CounterQuantity; i++)
                {
                    var locNme = $"Gate {i:00}";
                    var lccount = locGuardData?.Where(x => x.Location == locNme).Sum(x => x.Pcount) ?? 0;
                    cdto.CounterNameAndCount.Add(locNme, lccount);
                }
            }
            if (Cs.IsRoomEnabled)
            {
                for (int i = 1; i <= Cs.CounterQuantity; i++)
                {
                    var locNme = $"Room {i:00}";
                    var lccount = locGuardData?.Where(x => x.Location == locNme).Sum(x => x.Pcount) ?? 0;
                    cdto.CounterNameAndCount.Add(locNme, lccount);
                }
            }
            if (Cs.IsLevelFloorEnabled)
            {
                for (int i = 1; i <= Cs.CounterQuantity; i++)
                {
                    var locNme = $"Level(Floor) {i:00}";
                    var lccount = locGuardData?.Where(x => x.Location == locNme).Sum(x => x.Pcount) ?? 0;
                    cdto.CounterNameAndCount.Add(locNme, lccount);
                }
            }

            return cdto;
        }

        public async Task ResetAllSiteCrowdCountControl()
        {
            try
            {


                var al = new ClientSiteMobileCrowdControlAuditLog()
                {
                    ActionDescription = $"Scheduler started for all site count reset."
                };
                await WriteToMobileCrowdControlAuditLog(al);
                string _ArchivedMode = "Reset by auto scheduler";
                var _crowdControlSitesList = await _configDataProvider.GetAllCrowdControlSite();
                var _allCrowdControlData = await _configDataProvider.GetAllCurrentCrowdControlData();
                int _inactivityTimeFrameWindow = 4; // hours
                if (_crowdControlSitesList != null)
                {
                    al = new ClientSiteMobileCrowdControlAuditLog()
                    {
                        ActionDescription = $"Total crowd control sites found: {_crowdControlSitesList.Count}."
                    };
                    await WriteToMobileCrowdControlAuditLog(al);

                    var _siteTimeZone = await _configDataProvider.GetClientSitesTimeZones();
                    foreach (var site in _crowdControlSitesList)
                    {
                        string _ClientSiteName = _clientDataProvider.GetClientSiteName(site.ClientSiteId).Name;
                        string utcOffsetString = "10:00";
                        if (_siteTimeZone != null)
                        {
                            utcOffsetString = _siteTimeZone.FirstOrDefault(x => x.ClientSiteId == site.ClientSiteId)?.UTC ?? "10:00";
                        }

                        // Remove '+' sign if present; leave '-' intact
                        if (utcOffsetString.StartsWith("+"))
                            utcOffsetString = utcOffsetString.Substring(1);

                        // Parse offset and calculate local time
                        TimeSpan offset = TimeSpan.Parse(utcOffsetString.Replace("UTC", "").Trim());
                        DateTime utcNow = DateTime.UtcNow;
                        DateTime localNow = utcNow.Add(offset);

                        // Time window check: Only between 03:00 and 20:00
                        if (localNow.Hour >= 3 && localNow.Hour < 20)
                        {
                            var siteCrowdData = _allCrowdControlData.FirstOrDefault(x => x.ClientSiteId == site.ClientSiteId);

                            if (siteCrowdData != null && siteCrowdData.LastUpdateTime.HasValue)
                            {
                                DateTime lastUpdateLocal = siteCrowdData.LastUpdateTime.Value.Add(offset);
                                TimeSpan inactivity = localNow - lastUpdateLocal;

                                // Check for hours of inactivity and that we haven't already reset
                                if (inactivity.TotalHours >= _inactivityTimeFrameWindow)
                                {
                                    if (siteCrowdData.CrowdControlDate.Value.Date != utcNow.Date)
                                    {
                                        // Reset only if the date has changed
                                        string _ChangeReason = $"Date changed from {siteCrowdData.CrowdControlDate.Value.Date.ToString("dd-MM-yyyy")} to {utcNow.Date.ToString("dd-MM-yyyy")}";
                                        await ResetSiteCounter(siteCrowdData, utcNow, localNow, site.ClientSiteId, _ClientSiteName, utcOffsetString, _ArchivedMode, _ChangeReason);
                                    }
                                    else if (siteCrowdData.Tcount > 0 || siteCrowdData.Ccount > 0)
                                    {
                                        string _ChangeReason = $"Counts not 0. Tcount:{siteCrowdData.Tcount}, Ccount:{siteCrowdData.Ccount}.";
                                        await ResetSiteCounter(siteCrowdData, utcNow, localNow, site.ClientSiteId, _ClientSiteName, utcOffsetString, _ArchivedMode, _ChangeReason);
                                    }
                                }
                                else
                                {
                                    var msg = "";
                                    if (inactivity.TotalHours < _inactivityTimeFrameWindow)
                                    {
                                        msg = $"Skipping reset for site [{_ClientSiteName}] — due to inactivity hours less than required: {_inactivityTimeFrameWindow}hr, Current inactivity hours: {inactivity.TotalHours.Hours().Hours}hr.\nTcount:{siteCrowdData.Tcount}, Ccount:{siteCrowdData.Ccount}.";
                                    }
                                    else
                                    {
                                        msg = $"Skipping reset for site [{_ClientSiteName}] — due to: Counts are already zero, no reset needed. Tcount:{siteCrowdData.Tcount}, Ccount:{siteCrowdData.Ccount}.\nCurrent inactivity hours: {inactivity.TotalHours.Hours().Hours}hr, required: {_inactivityTimeFrameWindow}hr.";
                                    }


                                    //Console.WriteLine($"Skipping reset for site {site.ClientSiteId} — due to 12h inactivity at {localNow} (UTC {utcNow}).");
                                    al = new ClientSiteMobileCrowdControlAuditLog()
                                    {
                                        ClientSiteId = site.ClientSiteId,
                                        ActionTimeUTC = utcNow,
                                        ActionTimeLocal = localNow,
                                        TimeUTC = utcOffsetString,
                                        ActionDescription = msg
                                    };
                                    await WriteToMobileCrowdControlAuditLog(al);
                                }
                            }
                            else
                            {
                                if (siteCrowdData == null)
                                {
                                    al = new ClientSiteMobileCrowdControlAuditLog()
                                    {
                                        ClientSiteId = site.ClientSiteId,
                                        ActionTimeUTC = utcNow,
                                        ActionTimeLocal = localNow,
                                        TimeUTC = utcOffsetString,
                                        ActionDescription = $"Skipping reset for site [{_ClientSiteName}] due to no crowd control data found."
                                    };
                                    await WriteToMobileCrowdControlAuditLog(al);
                                }
                                else if (!siteCrowdData.LastUpdateTime.HasValue)
                                {
                                    al = new ClientSiteMobileCrowdControlAuditLog()
                                    {
                                        ClientSiteId = site.ClientSiteId,
                                        ActionTimeUTC = utcNow,
                                        ActionTimeLocal = localNow,
                                        TimeUTC = utcOffsetString,
                                        ActionDescription = $"Skipping reset for site [{_ClientSiteName}] due to no Last Update Time found in crowd control data."
                                    };
                                    await WriteToMobileCrowdControlAuditLog(al);
                                }
                            }
                        }
                        else
                        {
                            // Console.WriteLine($"Skipping reset for site {site.ClientSiteId} — outside reset window: {localNow.Hour}:00");
                            al = new ClientSiteMobileCrowdControlAuditLog()
                            {
                                ClientSiteId = site.ClientSiteId,
                                ActionTimeUTC = utcNow,
                                ActionTimeLocal = localNow,
                                TimeUTC = utcOffsetString,
                                ActionDescription = $"Skipping reset for site [{_ClientSiteName}] — outside reset window [between 03:00 am - 20:00 pm], local time is: {localNow.ToString("dd-MM-yyyy HH:mm tt")}"
                            };
                            await WriteToMobileCrowdControlAuditLog(al);
                        }
                    }
                }
                else
                {
                    al = new ClientSiteMobileCrowdControlAuditLog()
                    {
                        ActionDescription = $"No Mobile Crowd Control sites found. Exiting Scheduler."
                    };
                    await WriteToMobileCrowdControlAuditLog(al);
                }

            }
            catch (Exception ex)
            {
                ClientSiteMobileCrowdControlAuditLog al = new ClientSiteMobileCrowdControlAuditLog()
                {
                    ActionDescription = $"An error has occured in server:\nError Details:- InnerException:{ex.InnerException}\nStackTrace:{ex.StackTrace}\nMessage:{ex.Message}"
                };
                await WriteToMobileCrowdControlAuditLog(al);
                throw;
            }
        }

        private async Task ResetSiteCounter(ClientSiteMobileCrowdControl siteCrowdData, DateTime utcNow, DateTime localNow,
            int ClientSiteId, string _ClientSiteName, string utcOffsetString, string _ArchivedMode, string _ChangeReason)
        {
            // Move to history
            var history = new ClientSiteMobileCrowdControlHistory
            {
                Id = siteCrowdData.Id,
                ClientSiteId = siteCrowdData.ClientSiteId,
                Tcount = siteCrowdData.Tcount,
                Ccount = siteCrowdData.Ccount,
                CrowdControlDate = siteCrowdData.CrowdControlDate,
                LastUpdateTime = siteCrowdData.LastUpdateTime,
                ArchivedOn = utcNow,
                ArchivedMode = _ArchivedMode
            };

            await _configDataProvider.SaveCrowdControlHistory(history);

            // Reset current data
            siteCrowdData.Tcount = 0;
            siteCrowdData.Ccount = 0;
            siteCrowdData.CrowdControlDate = utcNow.Date;
            siteCrowdData.LastUpdateTime = utcNow;

            await _configDataProvider.ResetSiteAndGuardCrowdControlData(siteCrowdData, utcNow, _ArchivedMode);

            //Console.WriteLine($"Site {site.ClientSiteId} reset due to _hr inactivity at {localNow} (UTC {utcNow}).");
            var al = new ClientSiteMobileCrowdControlAuditLog()
            {
                ClientSiteId = ClientSiteId,
                ActionTimeUTC = utcNow,
                ActionTimeLocal = localNow,
                TimeUTC = utcOffsetString,
                ActionDescription = $"Site [{_ClientSiteName}] count has been reset due to {_ChangeReason}"
            };
            await WriteToMobileCrowdControlAuditLog(al);
        }

        private async Task WriteToMobileCrowdControlAuditLog(ClientSiteMobileCrowdControlAuditLog al)
        {
            await _clientDataProvider.SaveMobileCrowdControlAuditLog(al);
        }


        public async Task SaveCrowdControlGuardLocation(MobileCrowdControlGuard MCCG)
        {
            await _clientDataProvider.SaveCrowdControlGuardLocation(MCCG);
        }


        public List<SelectListItem> GetUserClientSitesWithPatrolData(int? userId, string[] type)
        {
            var sites = new List<SelectListItem>();
            var clientTypes = _clientDataProvider.GetClientTypes().Where(z => type.Contains(z.Name));
            if (clientTypes != null)
            {
                foreach (var clientType in clientTypes)
                {
                    var mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty);
                    foreach (var item in mapping)
                    {
                        sites.Add(new SelectListItem(item.Name, item.Name));
                    }
                }

            }
            return sites;
        }
        public List<SubDomain> GetUserSubDomainsHavingAccess(int? userId)
        {
            var subdomain = _clientDataProvider.GetSubDomains();
            var clientTypes = _clientDataProvider.GetClientTypes();
            if (userId == null)
            {
                var clientTypeIdsnew = clientTypes.Select(x => x.Id).Distinct().ToList();

                return subdomain.Where(x => clientTypeIdsnew.Contains(x.TypeId)).ToList();
            }


            var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
            var clientTypeIds = allUserAccess.Select(x => x.ClientSite.TypeId).Distinct().ToList();
            return subdomain.Where(x => clientTypeIds.Contains(x.TypeId)).ToList();
        }

        public List<object> GetGuardRcClientSiteAccess(int guardId)
        {
            var results = new List<object>();
            var guardAccess = _guardDataProvider.GetGuardRcClientSiteAccess(guardId);
            var clientSitesGuardAccess = guardAccess.Select(x => x.ClientSiteId);
            var allClientSitesGrouped = _clientDataProvider.GetClientSites(null).GroupBy(x => x.ClientType.Name);

            foreach (var item in allClientSitesGrouped)
            {
                results.Add(new
                {
                    Name = item.Key,
                    ClientSites = item.Select(x => new
                    {
                        Id = x.Id,
                        x.Name,
                        Checked = clientSitesGuardAccess.Contains(x.Id)
                    }).ToList()
                });
            }

            return results;
        }


        public List<ClientSiteSmartWandTags> GetClientSiteTagIds(int[] clientSiteIds)
        {
            // Get tags from logs history for the selected client sites
            //var tagsFromLogs = _clientSiteWandDataProvider.GetClientSiteWandTagsForClientSitesFromLogs(clientSiteIds);

            // Get tags from tags table for the selected client sites
            var tagsFromTagMaster = _clientSiteWandDataProvider.GetClientSiteWandTagsForClientSites(clientSiteIds);

            //// Get distinct tag ids and names
            //var uniqueUids = tagsFromLogs
            //.Concat(tagsFromTagMaster)
            //.Where(x => !string.IsNullOrWhiteSpace(x.UId))
            //.DistinctBy(x => x.UId)
            //.OrderBy(x => x.UId)
            //.ToList();

            //return uniqueUids;
            return tagsFromTagMaster;
        }
        public List<SelectListItem> GetClientSiteSmartWandIds(int[] clientSiteIds)
        {
            var siteSmartWands = new List<SelectListItem>();
            siteSmartWands.AddRange(_clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => clientSiteIds.Contains(z.ClientSiteId)).Select(z => new SelectListItem($"{z.SmartWandId} - [ {z.PhoneNumber} ]", z.SmartWandId)));
            return siteSmartWands;
        }

        public List<SelectListItem> GetPatrolCarAssociatedSmartWands(int[] patrolCarIds)
        {
            var siteSmartWands = new List<SelectListItem>();
            siteSmartWands.AddRange(_clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.PatrolCarId.HasValue && patrolCarIds.Contains(z.PatrolCarId.Value)).Select(z => new SelectListItem($"{z.SmartWandId} - [ {z.PhoneNumber} ]", z.SmartWandId)));
            return siteSmartWands;
        }

        public List<KeyVehicleLogDocketViewModel> GetKeyVehicleLogsWithDockets(DateTime LogFromDate, DateTime LogToDate, int[] ClientSiteIds)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();

            return _guardLogDataProvider.GetKeyVehicleLogsWithDockets(ClientSiteIds, LogFromDate, LogToDate)

                .Select(z => new KeyVehicleLogDocketViewModel(z, kvlFields))
                .ToList();
        }
        public async Task<DataTable> KVDocketToDataTable(List<KeyVehicleLogDocketViewModel> dailyPatrolData)
        {

            var dt = new DataTable("KV Dockets");
            //dt.Columns.Add("Day");
            //dt.Columns.Add("Date", typeof(string)); // Use string to hold formatted date
            ////  dt.Columns.Add("IR S/No");
            //dt.Columns.Add("Control Room Job No.");
            dt.Columns.Add("Site");
            //dt.Columns.Add("Address");
            //dt.Columns.Add("Desp. Time");
            //dt.Columns.Add("Arrival");
            //dt.Columns.Add("Depart.");
            //dt.Columns.Add("CWS SNo.");
            //dt.Columns.Add("Total mins on Site");
            //dt.Columns.Add("Resp. Time");
            //dt.Columns.Add("Alarm");
            //dt.Columns.Add("Patrol Att.");
            //dt.Columns.Add("Colour Code");
            //dt.Columns.Add("Action Taken");
            //dt.Columns.Add("Notified By");
            //dt.Columns.Add("Bill To:");
            //dt.Columns.Add("File Name");
            //dt.Columns.Add("PSPF");
            //dt.Columns.Add("File Size(KB)");
            //dt.Columns.Add("Hash String");


            foreach (var data in dailyPatrolData)
            {

                try
                {
                    var row = dt.NewRow();
                    //row["Day"] = data.NameOfDay;
                    //row["Date"] = data.Date;
                    ////row["IR S/No"] = data.SerialNo;
                    //row["Control Room Job No."] = data.ControlRoomJobNo;
                    row["Site"] = data.Detail.KeyVehicleLog.ClientSiteLogBook.ClientSite.Name;
                    //row["Address"] = data.SiteAddress;
                    //row["Desp. Time"] = NormalizeTime(data.DespatchTime);
                    //row["Arrival"] = NormalizeTime(data.ArrivalTime);
                    //row["Depart."] = NormalizeTime(data.DepartureTime);
                    //row["CWS SNo."] = data.SerialNo;
                    //row["Total mins on Site"] = data.TotalMinsOnsite;
                    //row["Resp. Time"] = NormalizeTime(data.ResponseTime);
                    //row["Alarm"] = data.Alarm;
                    //row["Patrol Att."] = data.PatrolAttented;
                    //row["Colour Code"] = data.ColorCodeStr;
                    //row["Action Taken"] = data.ActionTaken;
                    //row["Notified By"] = data.NotifiedBy;
                    //row["Bill To:"] = data.Billing;
                    //row["File Name"] = data.fileNametodownload;
                    //row["PSPF"] = data.pspfname;
                    //row["File Size(Kb)"] = await data.GetBlobSizeAsync();
                    //row["Hash String"] = data.hashvalue;
                    dt.Rows.Add(row);

                }
                catch (Exception ex)
                {

                }
            }

            var sortedRows = dt;
            //.AsEnumerable()
            //      .OrderBy(row =>
            //      {
            //          var dateStr = row.Field<string>("Date");
            //          return DateTime.TryParseExact(dateStr, "dd MMM yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate)
            //              ? parsedDate
            //              : DateTime.MinValue;
            //      });

            // Create a new sorted DataTable
            DataTable sortedTable = dt.Clone();
            //DataTable sortedTable = sortedRows.Any() ? sortedRows.CopyToDataTable() : dt.Clone();

            return sortedTable;
            //return dt;
        }


        public List<DropdownItemWithAddress> GetUserClientSitesWithAddressUsingId(int? userId, int id)
        {
            var sites = new List<DropdownItemWithAddress>
    {
        new DropdownItemWithAddress { Id = 0, Name = "Select", Address = string.Empty } // Default option
    };

            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Id == id);

            if (clientType != null)
            {
                var mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty);

                sites.AddRange(mapping.Select(item => new DropdownItemWithAddress
                {
                    Id = item.Id,
                    Name = item.Name,
                    Address = item.Address // assumes mapping object has Address property
                }));
            }

            return sites;

        }


        public List<DropdownItem> GetClientSiteSmartWandListForMobile(int clientSiteId)
        {
            var smartWandList = GetClientSiteSmartWands(clientSiteId).OrderBy(x => x.SmartWandId);

            // Initialize with the default "Select" option
            var items = new List<DropdownItem>
                {
                    new DropdownItem { Id = 0, Name = "Select" }
                };

            items.AddRange(smartWandList.Select(item =>
                new DropdownItem
                {
                    Id = item.Id,
                    Name = item.SmartWandId
                }
            ));

            return items;
        }

        public SmartWandDeviceRegister CheckAndRegisterDeviceWithSmartWand(SmartWandDeviceRegister DeviceToRegister)
        {
            // Get Details of SmartWand from ClientSiteSmartWand table

            var smartWand = _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(x => x.Id == DeviceToRegister.SmartWandId).FirstOrDefault();
            if (smartWand != null)
            {
                // Check if the SmartWandId already registered with any device
                if (smartWand.DeviceId != null && smartWand.DeviceId != "")
                {
                    // Check if registered with same device
                    if (smartWand.DeviceId == DeviceToRegister.DeviceId)
                    {
                        DeviceToRegister.IsSuccess = false;
                        DeviceToRegister.Message = "Device already registered with this device.";
                        return DeviceToRegister;
                    }
                    else
                    {
                        DeviceToRegister.IsSuccess = false;
                        DeviceToRegister.Message = "Device already registered with a different device.";
                        return DeviceToRegister;
                    }
                }
                else
                {
                    // Not registered, proceed to register
                    smartWand.DeviceId = DeviceToRegister.DeviceId;
                    smartWand.DeviceName = DeviceToRegister.DeviceName;
                    smartWand.DeviceType = DeviceToRegister.DeviceType;
                    try
                    {
                        var result = _clientSiteWandDataProvider.UpdateClientSiteSmartWand(smartWand);
                        if (result)
                        {
                            DeviceToRegister.IsSuccess = true;
                            DeviceToRegister.Message = "Device registered successfully.";
                        }
                        else
                        {
                            DeviceToRegister.IsSuccess = false;
                            DeviceToRegister.Message = "Failed to register device.";
                        }
                        return DeviceToRegister;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            else
            {
                throw new Exception("Smart Wand not found");
            }

        }

        public bool CheckIfSmartWandIsDeRegisteredAsync(string DeviceIdToCheck)
        {
            // Get Details of SmartWand from ClientSiteSmartWand table

            if (string.IsNullOrWhiteSpace(DeviceIdToCheck))
                return false; // invalid deviceId

            var allSmartWands = _clientSiteWandDataProvider.GetClientSiteSmartWands();

            if (allSmartWands == null)
                return true; // no data available means treat as deregistered

            var smartWand = allSmartWands.FirstOrDefault(x => x.DeviceId != null && x.DeviceId.Trim() == DeviceIdToCheck.Trim());

            return smartWand == null; // true = deregistered
        }

        public int GetSmartWandIdFromDeviceId(string DeviceIdToCheck)
        {
            // Get Details of SmartWand from ClientSiteSmartWand table

            if (string.IsNullOrWhiteSpace(DeviceIdToCheck))
                return 0; // invalid deviceId

            var allSmartWands = _clientSiteWandDataProvider.GetClientSiteSmartWands();

            if (allSmartWands == null)
                return 0; // no data available means treat as deregistered

            var smartWand = allSmartWands.FirstOrDefault(x => x.DeviceId != null && x.DeviceId.Trim() == DeviceIdToCheck.Trim());

            return smartWand.Id; // true = registered
        }

        public Dictionary<string, string> GetCustomFieldConfig(int clientSiteId)
        {

            var columns = new Dictionary<string, string>()
            {
                { "timeSlot", "Time Slot"}
            };
            var clientSiteCustomFields = _guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId);
            var fields = clientSiteCustomFields.Select(z => z.Name).Distinct();
            foreach (var field in fields)
            {
                columns.Add(field, field);
            }
            return columns;
        }

        public List<Dictionary<string, string>> GetCustomFieldLogs(int logBookId, int clientSiteId)
        {
            var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(logBookId);
            if (!customFieldLogs.Any())
            {
                var clientSiteCustomFields = _guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId)
                                                .Select(z => new CustomFieldLog()
                                                {
                                                    ClientSiteLogBookId = logBookId,
                                                    CustomFieldId = z.Id
                                                }).ToList();
                _guardLogDataProvider.SaveCustomFieldLogs(clientSiteCustomFields);
                customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(logBookId);
            }

            var timeSlotGroups = customFieldLogs.GroupBy(z => z.ClientSiteCustomField.TimeSlot);
            var rows = new List<Dictionary<string, string>>();
            foreach (var group in timeSlotGroups)
            {
                var columns = new Dictionary<string, string>();
                if (!columns.ContainsKey(group.Key))
                {
                    columns.Add("timeSlot", group.Key);
                }

                foreach (var field in group.ToList())
                {
                    columns.Add(field.ClientSiteCustomField.Name, field.DayValue);
                }
                rows.Add(columns);
            }
            return rows;
        }

        public bool SaveCustomFieldLog(int logBookId, Dictionary<string, string> records)
        {
            var timeSlot = records["timeSlot"];
            var success = true;
            try
            {
                var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(logBookId);
                foreach (var record in records.Where(z => z.Key != "timeSlot"))
                {
                    if (record.Value != null)
                    {
                        var customFieldLog = customFieldLogs.SingleOrDefault(x => x.ClientSiteCustomField.Name.Equals(record.Key) &&
                                                                x.ClientSiteCustomField.TimeSlot.Equals(timeSlot));
                        if (customFieldLog != null)
                        {
                            customFieldLog.DayValue = record.Value;
                            _guardLogDataProvider.SaveCustomFieldLog(customFieldLog);
                        }
                    }
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        public List<PatrolCarLog> GetPatrolCarLogs(int logBookId, int clientSiteId)
        {
            var patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(logBookId);
            if (!patrolCarLogs.Any())
            {
                var clientSitePatrolCars = _clientSiteWandDataProvider.GetClientSitePatrolCars(clientSiteId).Select(z => new PatrolCarLog()
                {
                    ClientSiteLogBookId = logBookId,
                    PatrolCarId = z.Id,
                });
                _guardLogDataProvider.SavePatrolCarLogs(clientSitePatrolCars);
                patrolCarLogs = _guardLogDataProvider.GetPatrolCarLogs(logBookId);
            }
            return patrolCarLogs;
        }

        public bool SavePatrolCarLog(PatrolCarLog record)
        {
            _guardLogDataProvider.SavePatrolCarLog(record);
            return true;
        }

        public MobileAppUpgrade GetLatestMobileAppVersion(string platformType)
        {
            return _appConfigurationProvider.GetLatestMobileAppVersion(platformType);
        }

        public List<MobileAppUpgrade> GetAllMobileAppVersion()
        {
            return _appConfigurationProvider.GetAllMobileAppVersion();
        }
        public void SaveMobileAppUpgrade(MobileAppUpgrade mobileAppUpgrade)
        {
            _appConfigurationProvider.SaveMobileAppUpgrade(mobileAppUpgrade);
        }
        public void DeleteMobileAppUpgrade(int id)
        {
            //Get app version by id
            var mv = _appConfigurationProvider.GetMobileAppVersionById(id);
            var versionPath = $"{mv.AppVersionMajor}.{mv.AppVersionMinor}.{mv.AppVersionPatch}";
            var filename = mv.FileName;
            var platform = mv.AppType;
            _appConfigurationProvider.DeleteMobileAppUpgrade(id);
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Downloads", "MobileApp", platform, versionPath, filename);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                //Log exception but do not throw as the main operation is already done
                Console.WriteLine($"Error deleting mobile app file from server: {ex.Message}");
            }
        }

        public void UpdateDownloadCount(int id)
        {
            _appConfigurationProvider.UpdateDownloadCount(id);
        }

        public void RollBackToVersion(int recordId)
        {
            _appConfigurationProvider.RollBackToVersion(recordId);
        }


        public (bool AccessPermission, int? LoggedInUserId, int? GuId, int? SuccessCode, string SuccessMessage) ValidateGuardHrPin(int guardId, string key)
        {
            bool AccessPermission = false;
            int? LoggedInUserId = 0;
            int? GuId = 0;
            string SuccessMessage = string.Empty;
            int? SuccessCode = 0;
            AuthUserHelper.IsAdminPowerUser = false;
            AuthUserHelper.IsAdminGlobal = false;

            if (!string.IsNullOrEmpty(key))
            {
                var guard = _guardDataProvider.GetGuardDetailsUsingId(guardId);
                if (guard == null)
                {
                    SuccessMessage = "Invalid PIN";
                }
                else
                {
                    var firstGuard = guard.FirstOrDefault();
                    if (firstGuard != null && firstGuard.Pin != null)
                    {
                        if (guard.FirstOrDefault().Pin.Trim() == key.Trim())
                        {
                            AccessPermission = true;
                        }
                        else
                        {
                            SuccessMessage = "Invalid PIN";
                        }
                    }
                    else
                    {
                        SuccessMessage = "No PIN Set for you";
                    }
                }
            }

            return (AccessPermission, LoggedInUserId, GuId, SuccessCode, SuccessMessage);
        }

        public List<Guard> GetLicenseAndCompliancForGuards(int guardId)
        {
            var result = _guardDataProvider.GetGuards().Where(x => x.Id == guardId).ToList();
            return result;
        }

        public List<GuardComplianceAndLicense> GetGuardLicenseAndComplianceData(int guardId)
        {
            var GuardDetails = _guardDataProvider.GetGuardLicensesandcompliance(guardId);
            return GuardDetails;
        }

        public List<CombinedData> GetHRDescription(int HRid, int GuardID)
        {
            var DescVal = _guardDataProvider.GetHRDesc(HRid);
            var combinedDataList = new List<CombinedData>();
            foreach (var item in DescVal)
            {
                var GropuNamee = RemoveBrackets(item.GroupName);
                if (Enum.TryParse<HrGroup>(GropuNamee, out var hrGroup))
                {
                    // var NewDesc = item.ReferenceNo+ item.Description;
                    var NewDesc = item.Description;
                    var UsedDesc = _guardDataProvider.GetDescriptionList(hrGroup, NewDesc, GuardID);
                    var combinedData = new CombinedData
                    {
                        HRGroupId = HRid,
                        Description = item.Description,
                        UsedDescription = UsedDesc?.Description,
                        ReferenceNo = item.ReferenceNo,
                        ID = item.Id,
                        DateType = (int)item.DateType
                    };
                    combinedDataList.Add(combinedData);
                }

            }

            return combinedDataList;

        }

        public async Task<HrSettings> GetHRDescriptionBanDetailsAsync(int DescriptionID)
        {
            var DescVal = await _guardDataProvider.GetHRDescEditBanAsync(DescriptionID);
            return DescVal;
        }

        public (bool status, bool dbxUploaded, IEnumerable<string> msg) SaveOrUpdateGuardComplianceandlicanseNew(GuardComplianceAndLicense guardComplianceandlicense)
        {
            var status = true;
            var dbxUploaded = true;
            var message = "Success";

            if (!string.IsNullOrEmpty(guardComplianceandlicense.Description))
            {
                guardComplianceandlicense.Description = Regex.Replace(guardComplianceandlicense.Description, "[✔️❌]", "").Trim();
            }

            //Check Description Used or not start
            var UsedDesc = new GuardComplianceAndLicense();
            var GropuNamee = RemoveBrackets(guardComplianceandlicense.HrGroupText);
            GropuNamee = GropuNamee.Replace(" ", "");
            if (Enum.TryParse<HrGroup>(GropuNamee, out var hrGroup1))
            {
                UsedDesc = _guardDataProvider.GetDescriptionUsed(hrGroup1, guardComplianceandlicense.Description, guardComplianceandlicense.GuardId);
            }
            if (UsedDesc != null && guardComplianceandlicense.Id == 0)
            {
                status = false;
                message = "The type of document you are trying to upload already exists. If it is a newer version, please EDIT the existing document instead, change the expiry date,and then add the latest document";
            }
            else
            {
                //Check Description Used or not stop
                if (guardComplianceandlicense.Id == 0)
                {
                    string extension = Path.GetExtension(guardComplianceandlicense.FileName);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(guardComplianceandlicense.FileName);
                    guardComplianceandlicense.FileName = guardComplianceandlicense.FileName;
                }

                try
                {
                    dbxUploaded = UploadGuardComplianceandLicenseToDropboxNew(guardComplianceandlicense);
                    guardComplianceandlicense.CurrentDateTime = DateTime.Now.ToString();
                    guardComplianceandlicense.Reminder1 = 45;
                    guardComplianceandlicense.Reminder2 = 7;
                    _guardDataProvider.SaveGuardComplianceandlicanse(guardComplianceandlicense);
                }
                catch (Exception ex)
                {
                    status = false;
                    message = ex.Message;
                }
            }

            return (status, dbxUploaded, new List<string> { message });
        }

        public void DeleteGuardHrDocument(int hrDocId)
        {
            _guardDataProvider.DeleteGuardLicense(hrDocId);
        }

        private string RemoveBrackets(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string pattern = @"\[.*?\]|\{.*?\}|\(.*?\)";
            return Regex.Replace(input, pattern, string.Empty);
        }

        private bool UploadGuardComplianceandLicenseToDropboxNew(GuardComplianceAndLicense guardComplianceandlicense)
        {
            guardComplianceandlicense.Guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == guardComplianceandlicense.GuardId);
            var existingGuardCompliance = _guardDataProvider.GetGuardComplianceFile(guardComplianceandlicense.Id);
            if ((guardComplianceandlicense.Id == 0 && string.IsNullOrEmpty(guardComplianceandlicense.FileName)) ||
                (guardComplianceandlicense.Id != 0 && existingGuardCompliance.FileName == guardComplianceandlicense.FileName))
                return true;


            var fileToUpload = Path.Combine(_reportRootDir, "Uploads", "Guards", "License", guardComplianceandlicense.LicenseNo, guardComplianceandlicense.FileName);
            var DropboxDir = _guardDataProvider.GetDrobox();
            //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardComplianceandlicense.Guard)}/{guardComplianceandlicense.FileName}");
            var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolderNew(guardComplianceandlicense.Guard, DropboxDir.DropboxDir)}/{guardComplianceandlicense.FileName}");

            return UploadDocumentToDropbox(fileToUpload, dbxFilePath);
        }
        public bool UploadDocumentToDropbox(string fileToUpload, string dbxFilePath)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                        _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            bool uploaded = false;
            try
            {
                uploaded = Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
                //if (uploaded && System.IO.File.Exists(fileToUpload))
                //    System.IO.File.Delete(fileToUpload);
            }
            catch
            {
            }

            return uploaded;
        }

        public async Task<bool> UploadHrDocumentFileToServer(IFormFile Docfile, string LicenseNo, string uploadFileName)
        {
            var PathToUpload = Path.Combine(_reportRootDir, "Uploads", "Guards", "License", LicenseNo);
            var fileToUpload = Path.Combine(PathToUpload, uploadFileName);
            try
            {
                if (!Directory.Exists(PathToUpload))
                {
                    Directory.CreateDirectory(PathToUpload);
                }
                using (var stream = new FileStream(fileToUpload, FileMode.Create))
                {
                    await Docfile.CopyToAsync(stream);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public List<KVLogDocketsViewModel> GetKeyVehicleLogDocketHistory(PatrolRequest patrolRequest)
        {
            var kvlFields = _guardLogDataProvider
                .GetKeyVehicleLogFields()
                .ToDictionary(x => x.Id, x => x.Name);

            string Lookup(int? id) => id is int value && kvlFields.TryGetValue(value, out var name) ? name : null;

            IEnumerable<KeyVehicleLogDocketHistory> query;
            if (!string.IsNullOrWhiteSpace(patrolRequest.SerialNo))
            {
                // Serial number search: match on serial alone, ignoring date range and sites
                query = _irDataProvider.GetKeyVehicleLogsWithDocketNumber(patrolRequest.SerialNo);
            }
            else
            {
                int[] clientSiteIds = _clientDataProvider
                    .GetClientSiteDetailsWithName(patrolRequest.ClientSites)
                    .Select(x => x.Id)
                    .ToArray();

                query = _irDataProvider
                    .GetKeyVehicleLogsWithDocketsWithoutDate()
                    .Where(x =>
                        x.KeyVehicleLog.EntryTime >= patrolRequest.FromDate &&
                        x.KeyVehicleLog.EntryTime < patrolRequest.ToDate.AddDays(1) &&
                        clientSiteIds.Contains(x.KeyVehicleLog.ClientSiteLogBook.ClientSiteId));
            }
            var res = query
                .ToList()
                .Select(r => new KVLogDocketsViewModel
                {
                    Id = r.Id,
                    KvLogId = r.KeyVehicleLogId,
                    FileNametodownload = r.FileName,
                    DateOfLog = r.KeyVehicleLog.ClientSiteLogBook.Date.ToString("yyyy-MMM-dd").ToUpper(),
                    DocketSerialNo = r.DocketSerialNo,
                    VehicleRego = r.KeyVehicleLog.VehicleRego,
                    Plate = Lookup(r.KeyVehicleLog.PlateId),
                    TruckConfigText = Lookup(r.KeyVehicleLog.TruckConfig),
                    DocketReason = r.DocketReason,
                    PurposeOfEntry = Lookup(r.KeyVehicleLog.EntryReason),
                    IntialCall = r.KeyVehicleLog.InitialCallTime?.ToString("HH:mm"),
                    EntryTime = r.KeyVehicleLog.EntryTime?.ToString("HH:mm"),
                    SentInTime = r.KeyVehicleLog.SentInTime?.ToString("HH:mm"),
                    ExitTime = r.KeyVehicleLog.ExitTime?.ToString("HH:mm")
                })
                .ToList();

            return res;
        }

        public List<KeyVehicleLogDocketViewModel> GetKeyVehicleLogDocketHistoryWithIR(PatrolRequest patrolRequest)
        {
            IEnumerable<IncidentReport> incidentReports;
            IEnumerable<KeyVehicleLogDocketHistory> docketHistories;
            IEnumerable<IncidentReportsPlatesLoaded> incidentReportsPlatesLoaded;

            // 1️⃣ Get Incident Reports
            if (patrolRequest.SerialNo == null)
            {
                incidentReports = _irDataProvider
                    .GetIncidentReportsForDockets(patrolRequest.FromDate, patrolRequest.ToDate)
                    .Where(z =>
                        (patrolRequest.ClientTypes == null ||
                            (z.ClientSiteId.HasValue &&
                             patrolRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name))) &&
                        (patrolRequest.ClientSites == null ||
                            (z.ClientSiteId.HasValue &&
                             patrolRequest.ClientSites.Contains(z.ClientSite.Name))) &&
                        (patrolRequest.Position == null || z.Position == patrolRequest.Position) &&
                        (patrolRequest.ColourCode == 0 || z.ColourCode == patrolRequest.ColourCode)
                    );
            }
            else
            {
                // Keep date filter consistent
                incidentReports = _irDataProvider
                    .GetIncidentReportsForDockets(patrolRequest.FromDate, patrolRequest.ToDate)
                    .Where(z => z.SerialNo == patrolRequest.SerialNo);
            }

            // 2️⃣ Extract IncidentReportIds once
            var incidentReportIds = incidentReports
                .Select(z => z.Id)
                .ToHashSet();

            if (!incidentReportIds.Any())
                return new List<KeyVehicleLogDocketViewModel>();

            // 3️⃣ Get plates linked to incident reports
            incidentReportsPlatesLoaded = _irDataProvider
                .GetIncidentReportsPlates()
                .Where(x => incidentReportIds.Contains(x.IncidentReportId))
                .ToList();

            if (!incidentReportsPlatesLoaded.Any())
                return new List<KeyVehicleLogDocketViewModel>();

            // 4️⃣ Prepare lookup sets (performance fix)
            var plateIds = incidentReportsPlatesLoaded
                .Select(z => z.PlateId)
                .ToHashSet();

            var truckNos = incidentReportsPlatesLoaded
                .Select(z => z.TruckNo)
                .ToHashSet();

            // 5️⃣ Get docket histories
            docketHistories = _irDataProvider
                .GetKeyVehicleLogsWithDocketsWithoutDate()
                .Where(x =>
                    plateIds.Contains(x.KeyVehicleLog.PlateId) &&
                    truckNos.Contains(x.KeyVehicleLog.VehicleRego))
                .ToList();

            // 6️⃣ Build ViewModels
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();

            return docketHistories
                .Select(z => new KeyVehicleLogDocketViewModel(z, kvlFields))
                .ToList();
        }

        public async Task<ClientSiteMobileCrowdControl> GetCrowdControlCount(MobileCrowdControlGuard JoinGaurd)
        {
            var currentCount = await _clientDataProvider.GetCrowdControlCount(JoinGaurd);
            return currentCount;

        }
        //p7-137--pax-start
        public List<KeyVehicleLogViewModel> GetKeyVehicleLogsWithPax(int logBookId, KvlStatusFilter kvlStatusFilter)
        {
            var kvlFields = _guardLogDataProvider.GetKeyVehicleLogFields();
            var kvlPax = _guardLogDataProvider.GetKeyVehicleLogPaxs();
            return _guardLogDataProvider.GetKeyVehicleLogs(logBookId)
                .Select(z => new KeyVehicleLogViewModel(z, kvlFields, kvlPax))
                .Where(r => kvlStatusFilter == KvlStatusFilter.All || r.Status == kvlStatusFilter)
               .ToList();
        }

        //p7-137--pax-end

        public List<object> GetAllUsersClientSiteAccessForOnboardingUsers(string searchterm)
        {
            var results = new List<object>();
            var users = _userDataProvider.GetUsers();
            var allUserAccess = _userDataProvider.GetUserClientSiteAccess(null);
            foreach (var user in users)
            {
                var ThirdPartyID = _userDataProvider.GetUserClientSiteAccessThirdParty(user.Id);
                var currUserAccess = allUserAccess.Where(x => x.UserId == user.Id);
                results.Add(new
                {
                    user.Id,
                    user.UserName,
                    ClientTypeCsv = GetFormattedClientTypes(currUserAccess),
                    ClientSiteCsv = GetFormattedClientSites(currUserAccess),
                    ThirdParty = (ThirdPartyID != null && ThirdPartyID.ThirdPartyID != 0) ? ThirdPartyID.ThirdPartyID : null
                });
            }
            var filteredResults = results;

            if (!string.IsNullOrEmpty(searchterm))
            {
                filteredResults = results
                    .Where(x =>
                        ((dynamic)x).UserName.Contains(searchterm, StringComparison.OrdinalIgnoreCase) ||
                        ((dynamic)x).ClientTypeCsv.Contains(searchterm, StringComparison.OrdinalIgnoreCase) ||
                        ((dynamic)x).ClientSiteCsv.Contains(searchterm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return filteredResults;
        }

        public List<SelectListItem> GetClientSitePatrolCarIds(int[] clientSiteIds)
        {
            var sitePatrolCars = new List<SelectListItem>();
            sitePatrolCars.AddRange(_clientSiteWandDataProvider.GetPatrolCarsForSite(clientSiteIds).Select(z => new SelectListItem(z.Name, z.Id.ToString())));
            return sitePatrolCars;
        }

        public List<SelectListItem> GetAllPatrolCars()
        {
            var sitePatrolCars = new List<SelectListItem>();
            sitePatrolCars.AddRange(_clientSiteWandDataProvider.GetPatrolCars().OrderBy(x => x.Name).Select(z => new SelectListItem(z.Name, z.Id.ToString())));
            return sitePatrolCars;

        }
    }

    public class DropdownItemWithAddress
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class HRGroupStatusNew
    {
        public int Status { get; set; }
        public string GroupName { get; set; }
        public string ColourCodeStatus { get; set; }
        public string Description { get; set; }
    }

    public class DropdownItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ActivityModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Label { get; set; }
    }

    public class Mp3File
    {
        public string Label { get; set; }
        public string Url { get; set; }
        public Command PlayCommand { get; set; }
    }

    public class CombinedData
    {
        public int HRGroupId { get; set; }
        public string Description { get; set; }
        public string UsedDescription { get; set; }
        public string ReferenceNo { get; set; }
        public int ID { get; set; }
        public int DateType { get; set; }
    }
}
