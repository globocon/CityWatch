using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Models;
using DocumentFormat.OpenXml.Office.CustomUI;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Azure;
using Org.BouncyCastle.Asn1.Pkcs;
using SMSGlobal.api;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

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
        List<object> GetAllUsersClientSiteAccess();
        List<object> GetUserClientSiteAccess(int userId);
        List<object> GetAllCoreSettings(int companyId);

        List<ClientType> GetUserClientTypesHavingAccess(int? userId);
        List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm);
        DataTable PatrolDataToDataTable(List<DailyPatrolData> dailyPatrolData);

        // Daily Guard Logs & Key Vehicle Logs
        bool CheckWandIsInUse(int smartWandId, int? guardId);
        List<ClientSiteSmartWand> GetSmartWands(string siteName, int? guardId);
        List<ClientSiteSmartWand> GetClientSiteSmartWands(int clientSiteId);
        List<GuardViewModel> GetGuards();
        List<KeyVehicleLogViewModel> GetKeyVehicleLogs(int logBookId, KvlStatusFilter kvlStatusFilter);
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

        public IncidentReportPosition GetLoogbookdata(string IncidentName);

        //p2-192 client email search-start
        List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm, string searchTermtwo);
        //p2-192 client email search-end
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

        public ViewDataService(IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IClientSiteWandDataProvider clientSiteWandDataProvider,
            IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider,
            IGuardSettingsDataProvider guardSettingsDataProvider)
        {
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _userDataProvider = userDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
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
        //To get the count of ClientTypes stop

        public List<SelectListItem> GetUserClientSites(int? userId, string type = "")
        {
            var sites = new List<SelectListItem>();
            var clientType = _clientDataProvider.GetClientTypes().SingleOrDefault(z => z.Name == type);
            var mapping = GetUserClientSitesHavingAccess(clientType.Id, userId, string.Empty).Where(x => x.ClientType.Name == type);
            foreach (var item in mapping)
            {
                sites.Add(new SelectListItem(item.Name, item.Name));
            }
            return sites;
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
        public string GetFeedbackTemplateText(int id)
        {
            return _configDataProvider.GetFeedbackTemplates().SingleOrDefault(x => x.Id == id)?.Text;
        }

        public List<object> GetAllUsersClientSiteAccess()
        {
            var results = new List<object>();
            var users = _userDataProvider.GetUsers();
            var allUserAccess = _userDataProvider.GetUserClientSiteAccess(null);
            foreach (var user in users)
            {
                var currUserAccess = allUserAccess.Where(x => x.UserId == user.Id);
                results.Add(new
                {
                    user.Id,
                    user.UserName,
                    ClientTypeCsv = GetFormattedClientTypes(currUserAccess),
                    ClientSiteCsv = GetFormattedClientSites(currUserAccess)
                });
            }
            return results;
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
        public int GetClientTypeCount(int? typeId)
        {
            var result= _clientDataProvider.GetClientSite(typeId);
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
            var guards = _guardDataProvider.GetGuards();
            var guardLogins = _guardDataProvider.GetGuardLogins(guards.Select(z => z.Id).ToArray());
            return guards.Select(z => new GuardViewModel(z, guardLogins.Where(y => y.GuardId == z.Id))).ToList();
        }

        public DataTable PatrolDataToDataTable(List<DailyPatrolData> dailyPatrolData)
        {
            var dt = new DataTable("Patrol Report");
            dt.Columns.Add("Day");
            dt.Columns.Add("Date");
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
            dt.Columns.Add("Action Taken");
            dt.Columns.Add("Notified By");
            dt.Columns.Add("Bill To:");

            foreach (var data in dailyPatrolData)
            {
                var row = dt.NewRow();
                row["Day"] = data.NameOfDay;
                row["Date"] = data.Date;
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
                row["Action Taken"] = data.ActionTaken;
                row["Notified By"] = data.NotifiedBy;
                row["Bill To:"] = data.Billing;
                dt.Rows.Add(row);
            }

            return dt;
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
           return profiles.Where(z => (string.IsNullOrEmpty(poi) || string.Equals(z.POIOrBDM, poi)) ).Select(z => new KeyVehicleLogProfileViewModel(z, kvlFields)).ToList();
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
                items.Add(new SelectListItem("BDM", "BDM"));
                items.Add(new SelectListItem("Supplier", "Supplier"));


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
            int newLogBookId;
            var clientSiteLogBook = _clientDataProvider.GetClientSiteLogBook(clientSiteId, logBookType, DateTime.Today);
            if (clientSiteLogBook != null)
            {
                newLogBookId = clientSiteLogBook.Id;
            }
            else
            {
                var newClientSiteLogBook = new ClientSiteLogBook()
                {
                    ClientSiteId = clientSiteId,
                    Type = logBookType,
                    Date = DateTime.Today,
                    DbxUploaded = false
                };
                newLogBookId = _clientDataProvider.SaveClientSiteLogBook(newClientSiteLogBook);
            }

            return newLogBookId;
        }

        public string GetClientSiteKeyDescription(int keyId, int clientSiteId)
        {
            return _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).SingleOrDefault(z => z.Id == keyId)?.Description;
        }

        public string GetClientSiteKeyNo(int keyId, int clientSiteId)
        {
            return _guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).SingleOrDefault(z => z.Id == keyId)?.KeyNo;
        }

        public void CopyOpenLogbookEntriesFromPreviousDay(int previousDayLogBookId, int logBookId, int guardLoginId)
        {
            var kvlFieldsToLookup = _guardLogDataProvider.GetKeyVehicleLogFields()
                .Where(z => z.Name == "Law Enforcement" || z.Name == "Emergency Services" || z.Name == "Emergency Situation")
                .ToDictionary(z => z.Name, z => z.Id);

            var logsToCopy = _guardLogDataProvider.GetKeyVehicleLogs(previousDayLogBookId).Where(z => !z.ExitTime.HasValue &&
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
                    catch(Exception ex)
                    {

                    }
                }

               
            }
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
                                            string gpsCoordinates, string enabledAddress, GuardLog tmzdata,string clientSiteName,string GuradName)
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
                    Notes = "Duress Alarm Activated By "+ GuradName+" From "+ clientSiteName,
                    EntryType = (int)IrEntryType.Alarm,
                    Date = logBook_Date.Value,
                    IsAcknowledged = 0,
                    IsDuress=1
                };
                var pushMessageId = _guardLogDataProvider.SavePushMessage(radioCheckPushMessages);
                /* Save the push message for reload to logbook on next day end*/

                _guardLogDataProvider.LogBookEntryForRcControlRoomMessages(guardId, guardId, null, "Duress Alarm Activated By " + GuradName + " From " + clientSiteName, IrEntryType.Alarm, 1,0, tmzdata); // GuardLog tmzdata parameter added by binoy for Task p6#73_TimeZone issue
                _guardLogDataProvider.SaveClientSiteDuress(clientSiteId, guardId, gpsCoordinates, enabledAddress, tmzdata);

                _guardLogDataProvider.SaveGuardLog(new GuardLog()
                {
                    Notes = "Duress Alarm Activated By " + GuradName + " From " + clientSiteName,
                    IsSystemEntry = true,
                    IrEntryType = Data.Enums.IrEntryType.Alarm,
                    EventDateTime = DateTime.Now,
                    ClientSiteLogBookId = logBookId,
                    GuardLoginId = guardLoginId,
                    RcPushMessageId= pushMessageId,
                    EventDateTimeLocal = tmzdata.EventDateTimeLocal, // Task p6#73_TimeZone issue -- added by Binoy - Start
                    EventDateTimeLocalWithOffset = tmzdata.EventDateTimeLocalWithOffset,
                    EventDateTimeZone = tmzdata.EventDateTimeZone,
                    EventDateTimeZoneShort = tmzdata.EventDateTimeZoneShort,
                    EventDateTimeUtcOffsetMinute = tmzdata.EventDateTimeUtcOffsetMinute, // Task p6#73_TimeZone issue -- added by Binoy - End
                    PlayNotificationSound = true,
                    GpsCoordinates= gpsCoordinates
                });

             

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

               foreach(var item2 in hist)
                {
                    if(item2.AuditMessage=="Initial entry")
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
        public string GetFeedbackTemplatesByTypeByColor(int type,int id)
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
        public List<ClientSite> GetUserClientSitesHavingAccess(int? typeId, int? userId, string searchTerm,string searchTermtwo)
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
    }
}
