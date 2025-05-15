using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Models;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Org.BouncyCastle.Asn1.Pkcs;
using SMSGlobal.api;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public List<ActivityModel> GetDressAppFields(int type);

        public List<Mp3File> GetDressAppFieldsAudio(int type);

        public ClientSiteMobileAppSettings GetCrowdSettingForSite(int siteId);
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

        public ViewDataService(IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardSettingsDataProvider guardSettingsDataProvider,
            ILogbookDataService logbookDataService)
        {
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _userDataProvider = userDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
            _logbookDataService = logbookDataService;
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
            return _guardDataProvider.GetGuardLoginsBySmartWandId(smartWandId)
                .Where(x => x.LoginDate >= DateTime.Today && x.LoginDate < DateTime.Today.AddDays(1)
                        && (!guardId.HasValue || x.GuardId != guardId.Value) && x.OffDuty > DateTime.Now)
                .Any();
        }

        public List<ClientSiteSmartWand> GetClientSiteSmartWands(int clientSiteId)
        {
            return _clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSiteId).ToList();
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
                    company.TimesheetsMail


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
                                      HR1List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                // Set HR2Status
                var HR2List = statusLookup["HR 2 (Client)"];
                if (HR2List.Any())
                {
                    guard.HR2Status = HR2List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
                                      HR2List.Any(x => x.ColourCodeStatus == "Yellow") ? "Yellow" :
                                      "Green";
                }

                // Set HR3Status
                var HR3List = statusLookup["HR 3 (Special)"];
                if (HR3List.Any())
                {
                    guard.HR3Status = HR3List.Any(x => x.ColourCodeStatus == "Red") ? "Red" :
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
                var firstItem = selectedList.FirstOrDefault(x => x.ExpiryDate != null);

                if (firstItem != null)
                {
                    var expiryDate = firstItem.ExpiryDate.Value; // Assuming ExpiryDate is not null here

                    // Compare expiry date with today's date
                    if (expiryDate < today)
                    {
                        return "Red";
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
                var row = dt.NewRow();
                row["Day"] = data.NameOfDay;
                row["Date"] = data.Date;
                //row["IR S/No"] = data.SerialNo;
                row["Control Room Job No."] = data.ControlRoomJobNo;
                row["Site"] = data.SiteName;
                row["Address"] = data.SiteAddress;
                row["Desp. Time"] = data.DespatchTime;
                row["Arrival"] = data.ArrivalTime;
                row["Depart."] = data.DepartureTime;
                row["CWS SNo."] = data.SerialNo;
                row["Total mins on Site"] = data.TotalMinsOnsite;
                row["Resp. Time"] = data.ResponseTime;
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

            var sortedRows = dt.AsEnumerable()
                      .OrderBy(row => DateTime.ParseExact(row.Field<string>("Date"), "dd MMM yyyy", null));

            // Create a new sorted DataTable
            DataTable sortedTable = sortedRows.Any() ? sortedRows.CopyToDataTable() : dt.Clone();

            return sortedTable;
            //return dt;
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

            foreach (var item in kvlFields)
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
            var profiles = _guardLogDataProvider.GetKeyVehicleLogVisitorPersonalDetails(truckRego);

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

            var logsToCopy = previousDayLogs.Where(z => !z.ExitTime.HasValue &&
                ((kvlFieldsToLookup.TryGetValue("Law Enforcement", out int idLawEnforce) && z.PersonType == idLawEnforce) ||
                    (kvlFieldsToLookup.TryGetValue("Emergency Services", out int idEms) && z.PersonType == idEms) ||
                    (kvlFieldsToLookup.TryGetValue("Emergency Situation", out int idEmSituation) && z.EntryReason == idEmSituation) ||
                    !string.IsNullOrEmpty(z.KeyNo)));



            if (logsToCopy.Any())
            {
                foreach (var logToCopy in logsToCopy)
                {
                    var test = logToCopy;
                    logToCopy.Id = 0;
                    logToCopy.InitialCallTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 00, 01, 0);
                    logToCopy.EntryTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 00, 01, 0);
                    logToCopy.SentInTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 00, 01, 0);
                    logToCopy.ClientSiteLogBookId = logBookId;
                    logToCopy.GuardLoginId = guardLoginId;
                    logToCopy.CopiedFromId = logToCopy.Id;

                    try
                    {
                        _guardLogDataProvider.InsertPreviousLogBook(logToCopy);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            // Task P7#129 Yellow wont roll over  - Binoy 29-07-2024 -- Start
            // To rollover previous days pending yellow entries to new logbook
            var pendinglogentries = previousDayLogs.Where(z => !z.ExitTime.HasValue && !z.EntryTime.HasValue && !z.SentInTime.HasValue && z.InitialCallTime.HasValue);
            if (pendinglogentries.Count() > 0)
            {
                foreach (var logToCopy in pendinglogentries)
                {
                    logToCopy.Id = 0;
                    logToCopy.InitialCallTime = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 00, 01, 0);
                    logToCopy.ClientSiteLogBookId = logBookId;
                    logToCopy.GuardLoginId = guardLoginId;
                    logToCopy.CopiedFromId = logToCopy.Id;

                    try
                    {
                        _guardLogDataProvider.InsertPreviousLogBook(logToCopy);
                    }
                    catch (Exception ex)
                    {

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
        //}
        //code added for Guard Access Dropdown stop
        //public IEnumerable<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistory()
        //{
        //    var kvlVisitorProfile = _guardLogDataProvider.GetKeyVehicleLogVisitorProfile();
        //    var history = new List<KeyVehicleLogAuditHistory>();
        //    foreach (var item in kvlVisitorProfile)
        //    {
        //        var hist = GetKeyVehicleLogAuditHistoryNew(item.Id);
        //        foreach(var item2 in hist)
        //        {
        //            item2.KeyVehicleLog = _guardLogDataProvider.GetKeyVehicleLogsByID(item2.KeyVehicleLogId).FirstOrDefault();

        //        }
        //        history.AddRange(hist); 
        //    }

        //    return history;
        //}
        //public List<KeyVehicleLogAuditHistory> GetKeyVehicleLogAuditHistoryNew(int profileId)
        //{
        //    return _guardLogDataProvider.GetAuditHistory(profileId)
        //        .OrderByDescending(z => z.Id)
        //        .ThenByDescending(z => z.AuditTime).ToList();
        //}
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

            // Join ClientSite with ClientSiteSmartWands using ClientSiteId
            var finalResults = results
                .Select(clientSite => new ClientSiteWithWands
                {
                    ClientSite = clientSite,
                    SmartWands = clientSiteSmartWands
                        .Where(smartWand => smartWand.ClientSiteId == clientSite.Id)
                        .ToList()
                })
                .ToList();

            return finalResults;
        }
        public class ClientSiteWithWands
        {
            public ClientSite ClientSite { get; set; }
            public List<ClientSiteSmartWand> SmartWands { get; set; }
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

        public List<ActivityModel> GetDressAppFields(int type)
        {
            var hrGroups = _guardLogDataProvider.GetDuressAppFields(type);

            // Convert the list of DuressAppField to DropdownItem
            return hrGroups.Select(x => new ActivityModel
            {
                Id = x.Id,
                Name = x.Name ,
                Label=x.Label
            }).ToList();
        }


        //public List<Mp3File> GetDressAppFieldsAudio(int type)
        //{
        //    var audio = _guardLogDataProvider.GetDuressAppFields(type);

        //    return audio.Select(x => new Mp3File
        //    {
        //        Label = x.Label,
        //        Url = "http://test.c4i-system.com/DuressAppAudio/Deva%20Deva.mp3",

        //    }).ToList();
        //}
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


        public ClientSiteMobileAppSettings GetCrowdSettingForSite(int siteId)
        {
            return _configDataProvider.GetCrowdSettingForSite(siteId);
        }



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
}
