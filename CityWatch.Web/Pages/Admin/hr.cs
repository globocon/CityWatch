using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using MailKit.Search;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using MailKit.Net.Smtp;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;
using static Dropbox.Api.Team.GroupSelector;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;

namespace CityWatch.Web.Pages.Admin
{
    public class GuardSettingsModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IGuardSettingsDataProvider _guardSettingsDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDropboxService _dropboxUploadService;
        private readonly Settings _settings;
        private readonly string _reportRootDir; 
        private readonly EmailOptions _EmailOptions;


        public GuardSettingsModel(IViewDataService viewDataService,
             IClientSiteWandDataProvider clientSiteWandDataProvider,
             IClientDataProvider clientDataProvider,
             IGuardDataProvider guardDataProvider,
             IConfigDataProvider configDataProvider,
             IGuardLogDataProvider guardLogDataProvider,
             IGuardSettingsDataProvider guardSettingsDataProvider,
             IWebHostEnvironment webHostEnvironment,
             IDropboxService dropboxUploadService,
             IOptions<Settings> settings,
             IOptions<EmailOptions> emailOptions


             )
        {
            _viewDataService = viewDataService;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
            _clientDataProvider = clientDataProvider;
            _guardDataProvider = guardDataProvider;
            _configDataProvider = configDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
            _guardSettingsDataProvider = guardSettingsDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _dropboxUploadService = dropboxUploadService;
            _settings = settings.Value;
            _reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath);
            _EmailOptions = emailOptions.Value;
        }

        public IViewDataService ViewDataService { get { return _viewDataService; } }
        public HrSettings HrSettings;

        public ActionResult OnGet()
        {
            if (!AuthUserHelper.IsAdminUserLoggedIn)
                return Redirect(Url.Page("/Account/Unauthorized"));

            return Page();
        }

        public JsonResult OnGetClientSiteWithLogSettings(string type, string searchTerm)
        {
            if (!string.IsNullOrEmpty(type) || !string.IsNullOrEmpty(searchTerm))
            {

                var clientSites = _viewDataService.GetUserClientSites(type, searchTerm);

                if (clientSites.Count == 0)
                {
                    var clientNewSites = _viewDataService.GetNewUserClientSites();

                    var clientSiteWithSettings = _clientSiteWandDataProvider.GetClientSiteSmartWands(searchTerm).ToList();
                    if (!string.IsNullOrEmpty(type))
                    {
                        return new JsonResult(clientNewSites.Select(z => new


                        {

                            z.Id,
                            ClientTypeName = z.ClientType.Name,
                            ClientSiteName = z.Name,
                            z.SiteEmail,
                            z.LandLine,
                            SiteUploadDailyLog = z.UploadGuardLog,
                            z.GuardLogEmailTo,
                            z.DataCollectionEnabled,
                            HasSettings = clientSiteWithSettings.Any(x => x.ClientSiteId == z.Id) || !string.IsNullOrEmpty(z.SiteEmail)
                        }).Where(x => (clientSiteWithSettings.Any(c => c.ClientSiteId == x.Id)) && (x.ClientTypeName == type)));
                    }
                    else
                    {
                        return new JsonResult(clientNewSites.Select(z => new


                        {

                            z.Id,
                            ClientTypeName = z.ClientType.Name,
                            ClientSiteName = z.Name,
                            z.SiteEmail,
                            z.LandLine,
                            SiteUploadDailyLog = z.UploadGuardLog,
                            z.GuardLogEmailTo,
                            z.DataCollectionEnabled,
                            HasSettings = clientSiteWithSettings.Any(x => x.ClientSiteId == z.Id) || !string.IsNullOrEmpty(z.SiteEmail)
                        }).Where(x => (clientSiteWithSettings.Any(c => c.ClientSiteId == x.Id))));
                    }
                }


                else
                {

                    var clientSiteWithSettings = _clientSiteWandDataProvider.GetClientSiteSmartWands().Select(z => z.ClientSiteId).ToList();

                    return new JsonResult(clientSites.Select(z => new
                    {
                        z.Id,
                        ClientTypeName = z.ClientType.Name,
                        ClientSiteName = z.Name,
                        z.SiteEmail,
                        z.LandLine,
                        z.DuressEmail,
                        z.DuressSms,
                        SiteUploadDailyLog = z.UploadGuardLog,
                        z.GuardLogEmailTo,
                        z.DataCollectionEnabled,
                        HasSettings = clientSiteWithSettings.Any(x => x == z.Id) || !string.IsNullOrEmpty(z.SiteEmail)
                    }));
                }

            }

            return new JsonResult(new { });
        }

        //public JsonResult OnGetPatrolCar(int clientSiteId)
        //{
        //    return new JsonResult(_clientSiteWandDataProvider.GetClientSitePatrolCars(clientSiteId).ToList());
        //}

        //public JsonResult OnPostPatrolCar(ClientSitePatrolCar record)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _clientSiteWandDataProvider.SaveClientSitePatrolCar(record);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    return new JsonResult(new { success, message });
        //}


        //public JsonResult OnPostDeletePatrolCar(int id)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _clientSiteWandDataProvider.DeleteClientSitePatrolCar(id);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    return new JsonResult(new { success, message });
        //}

        public JsonResult OnGetSmartWandSettings(int clientSiteId)
        {
            return new JsonResult(_clientSiteWandDataProvider.GetClientSiteSmartWands().Where(z => z.ClientSiteId == clientSiteId).ToList());
        }

        public JsonResult OnPostSmartWandSettings(ClientSiteSmartWand record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.SaveClientSiteSmartWand(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteSmartWandSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.DeleteClientSiteSmartWand(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        //public JsonResult OnGetClientSiteKeys(int clientSiteId)
        //{
        //    return new JsonResult(_guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).ToList());
        //}

        //public JsonResult OnPostClientSiteKey(ClientSiteKey clientSiteKey)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _guardSettingsDataProvider.SaveClientSiteKey(clientSiteKey);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //        if (ex.InnerException != null &&
        //            ex.InnerException is SqlException &&
        //            ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
        //        {
        //            message = "Key number already exists";
        //        }
        //    }

        //    return new JsonResult(new { success, message });
        //}

        //public JsonResult OnPostDeleteClientSiteKey(int id)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _guardSettingsDataProvider.DeleteClientSiteKey(id);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }

        //    return new JsonResult(new { success, message });
        //}

        //public void OnPostSaveSiteEmail(int siteId, string siteEmail, bool enableLogDump, string landLine, string guardEmailTo, string duressEmail, string duressSms)
        //{
        //    var clientSite = _clientDataProvider.GetClientSites(null).SingleOrDefault(z => z.Id == siteId);
        //    if (clientSite != null)
        //    {
        //        clientSite.SiteEmail = siteEmail;
        //        clientSite.UploadGuardLog = enableLogDump;
        //        clientSite.LandLine = landLine;
        //        clientSite.GuardLogEmailTo = guardEmailTo;
        //        clientSite.DuressEmail = duressEmail;
        //        clientSite.DuressSms = duressSms;
        //    }

        //    _clientDataProvider.SaveClientSite(clientSite);
        //}

        public JsonResult OnGetGuards()
        {
            return new JsonResult(new { data = _viewDataService.GetGuards() });
        }
        //To Get the Last Login Date 
        public IActionResult OnGetLastTimeLogin(int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetLastLoginUsingUserHistory(guardId));
        }

        public JsonResult OnPostGuards(Data.Models.Guard guard, string ClientSiteIds)
        {
            if (guard.GuardAccess != null)
            {
                foreach (var item in guard.GuardAccess)
                {

                    int val = Convert.ToInt32(item);
                    if (val == 1)
                    {
                        guard.IsLB_KV_IR = true;
                    }
                    else if (val == 2)
                    {
                        guard.IsSTATS = true
    ;
                    }
                    else if (val == 3)
                    {
                        guard.IsSTATSChartsAccess = true;
                    }
                    else if (val == 4)
                    {
                        guard.IsKPIAccess = true;
                    }
                    else if (val == 5)
                    {
                        guard.IsRCLiteAccess = true;
                    }
                    else if (val == 6)
                    {
                        guard.IsRCAccess = true;
                    }
                    else if (val == 7)
                    {
                        guard.IsRCHRAccess = true;
                    }
                    else if (val == 8)
                    {
                        guard.IsRCFusionAccess = true;
                    }
                    else if (val == 9)
                    {
                        guard.IsAdminPowerUser = true;
                    }
                    else if (val == 10)
                    {
                        guard.IsAdminSOPToolsAccess = true;
                    }
                    else if (val == 11)
                    {
                        guard.IsAdminAuditorAccess = true;
                    }
                    else if (val == 12)
                    {
                        guard.IsAdminInvestigatorAccess = true;
                    }
                    else if (val == 13)
                    {
                        guard.IsAdminThirdPartyAccess = true;
                    }
                    else if (val == 14)
                    {
                        guard.IsAdminGlobal = true;
                    }
                }
            }
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
            }


            var status = true;
            var message = "Success";
            var guardInitals = guard.Initial;
            var initalsUsed = string.Empty;
            var guardId = 0;

            try
            {
                guardId = _guardDataProvider.SaveGuard(guard, out initalsUsed);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A guard with same name or security number already exists";
                }
            }
            if (guardId > 0)
            {
                guard.Id = guardId;
                _guardDataProvider.SaveGuardLotes(guard);
            }
            return new JsonResult(new
            {
                status,
                message,
                initalsChangedMessage = initalsUsed != guardInitals ? $"Guard with initials {guardInitals} already exists. So initals changed to {initalsUsed}" : string.Empty,
                initalsUsed,
                guardId
            });
        }
        public JsonResult OnPostPersonalDetails(Data.Models.Guard guard)
        {
            var status = true;
            var message = "Success";
            var guardId = 0;

            try
            {
                 guardId = _guardDataProvider.SaveLanguageDetails(guard);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A guard with same name or security number already exists";
                }
            }
            return new JsonResult(new
            {
                status,
                message,
               
            });
        }
            public JsonResult OnPostExportGuardsToExcel(bool active, bool inactive, int[] guardIdsFilter)
        {
             return new JsonResult(new { data = _viewDataService.GetGuardsToExcel(active,inactive, guardIdsFilter) });
            //return new JsonResult("message");
        }
        public JsonResult OnGetClientStates()
        {
            return new JsonResult(_configDataProvider.GetStates());
        }

        //public JsonResult OnPostCustomFields(ClientSiteCustomField clientSiteCustomField)
        //{
        //    var status = true;
        //    var message = "Success";
        //    var id = -1;
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return new JsonResult(new { status = false, message = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
        //        }

        //        id = _guardLogDataProvider.SaveClientSiteCustomFields(clientSiteCustomField);

        //        var clientSiteLogBookId = _clientDataProvider.GetClientSiteLogBook(clientSiteCustomField.ClientSiteId, LogBookType.DailyGuardLog, DateTime.Today)?.Id;
        //        if (clientSiteLogBookId.HasValue)
        //        {
        //            var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBookId.Value).Where(x => x.CustomFieldId == id);
        //            if (!customFieldLogs.Any())
        //            {
        //                var customFieldLog = new CustomFieldLog
        //                {
        //                    CustomFieldId = id,
        //                    ClientSiteLogBookId = clientSiteLogBookId.Value
        //                };
        //                _guardLogDataProvider.SaveCustomFieldLog(customFieldLog);
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        status = false;
        //        message = "Error " + ex.Message;
        //    }

        //    return new JsonResult(new { status, message = new[] { message }, id });
        //}

        //public IActionResult OnGetClientSiteCustomFields(int clientSiteId)
        //{
        //    return new JsonResult(_guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId));
        //}

        //public IActionResult OnGetCustomFields()
        //{
        //    var customFields = _guardLogDataProvider.GetClientSiteCustomFields();
        //    var fieldNames = customFields.Select(z => z.Name).Distinct().OrderBy(z => z);
        //    var slots = customFields.Select(z => z.TimeSlot).Distinct().OrderBy(z => z);
        //    return new JsonResult(new { fieldNames, slots });
        //}

        //public IActionResult OnPostDeleteClientSiteCustomField(int id)
        //{
        //    var success = true;
        //    var message = "Success";
        //    try
        //    {
        //        _guardLogDataProvider.DeleteClientSiteCustomFields(id);
        //    }
        //    catch (Exception ex)
        //    {
        //        success = false;
        //        message = ex.Message;
        //    }
        //    return new JsonResult(new { success, message });
        //}

        //public IActionResult OnGetSitePocs(int clientSiteId)
        //{
        //    return new JsonResult(_guardSettingsDataProvider.GetClientSitePocs(clientSiteId));
        //}

        //public IActionResult OnPostSitePoc(ClientSitePoc record)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _guardSettingsDataProvider.SaveClientSitePoc(record);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;

        //        if (ex.InnerException != null &&
        //            ex.InnerException is SqlException &&
        //            ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
        //        {
        //            message = "A site POC with same name already exists. This may be deleted";
        //        }
        //    }
        //    return new JsonResult(new { success, message });
        //}

        //public IActionResult OnPostDeleteSitePoc(int id)
        //{
        //    var success = true;
        //    var message = "Success";
        //    try
        //    {
        //        _guardSettingsDataProvider.DeleteClientSitePoc(id);
        //    }
        //    catch (Exception ex)
        //    {
        //        success = false;
        //        message = ex.Message;
        //    }
        //    return new JsonResult(new { success, message });
        //}

        //public IActionResult OnGetSiteLocations(int clientSiteId)
        //{
        //    return new JsonResult(_guardSettingsDataProvider.GetClientSiteLocations(clientSiteId));
        //}

        //public IActionResult OnPostSiteLocation(ClientSiteLocation record)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _guardSettingsDataProvider.SaveClientSiteLocation(record);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;

        //        if (ex.InnerException != null &&
        //            ex.InnerException is SqlException &&
        //            ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
        //        {
        //            message = "A site location with same name already exists. This may be deleted";
        //        }
        //    }
        //    return new JsonResult(new { success, message });
        //}

        //public IActionResult OnPostDeleteSiteLocation(int id)
        //{
        //    var success = true;
        //    var message = "Success";
        //    try
        //    {
        //        _guardSettingsDataProvider.DeleteClientSiteLocation(id);
        //    }
        //    catch (Exception ex)
        //    {
        //        success = false;
        //        message = ex.Message;
        //    }
        //    return new JsonResult(new { success, message });
        //}

        public JsonResult OnGetKeyVehcileLogFields(KvlFieldType typeId)
        {
            return new JsonResult(_guardLogDataProvider.GetKeyVehicleLogFieldsByType(typeId));
        }

        public JsonResult OnPostSaveKeyVehicleLogField(KeyVehcileLogField record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                var existingField = _guardLogDataProvider.GetKeyVehicleLogFields(true).SingleOrDefault(z => z.TypeId == record.TypeId && z.Name.ToUpper() == record.Name.ToUpper());
                if (existingField != null)
                {
                    if (existingField.IsDeleted)
                    {
                        existingField.IsDeleted = false;
                        _guardLogDataProvider.SaveKeyVehicleLogField(existingField);
                    }
                    else
                    {
                        throw new InvalidOperationException("A field with same name exists");
                    }
                }
                else
                {
                    _guardLogDataProvider.SaveKeyVehicleLogField(record);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteKeyVehicleLogField(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardLogDataProvider.DeleteKeyVehicleLogField(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        //public JsonResult OnPostUpdateSiteDataCollection(int clientSiteId, bool disabled)
        //{
        //    var success = false;
        //    var message = string.Empty;
        //    try
        //    {
        //        _clientDataProvider.SetDataCollectionStatus(clientSiteId, !disabled);
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = ex.Message;
        //    }
        //    return new JsonResult(new { success, message });
        //}

        public JsonResult OnGetGuardLicense(int guardId)
        {
            return new JsonResult(_guardDataProvider.GetGuardLicenses(guardId));
        }
        public JsonResult OnGetGuardLicenseAndComplianceData(int guardId)
        {
            return new JsonResult(_guardDataProvider.GetGuardLicensesandcompliance(guardId));
        }

        public JsonResult OnPostSaveGuardLicense(GuardLicense guardLicense)
        {
            ModelState.Remove("guardLicense.Id");
            ModelState.Remove("guardLicense.LicenseTypeText");
            ModelState.Remove("guardLicense.LicenseType");
            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    status = false,
                    message = ModelState.Where(x => x.Value.Errors.Count > 0)
                                .Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage)))
                });
            }

            var status = true;
            var dbxUploaded = true;
            var message = "Success";

            try
            {

                dbxUploaded = UploadGuardLicenseToDropbox(guardLicense);
                if (guardLicense.LicenseType == null && guardLicense.Id != null && guardLicense.LicenseTypeName != null)
                {
                    //int intValueToCompare = -1;
                    int intValueToCompare = 0;
                    var licensetypeCount = _guardDataProvider.GetLicenseTypes().Where(x => x.Name == guardLicense.LicenseTypeName);
                    if (licensetypeCount.Count() > 0)
                    {
                        intValueToCompare = _guardDataProvider.GetLicenseTypes().Where(x => x.Name == guardLicense.LicenseTypeName).FirstOrDefault().Id;
                    }
                    else
                    {
                        intValueToCompare = -1;
                    }
                    guardLicense.LicenseType = (GuardLicenseType)intValueToCompare;
                    guardLicense.LicenseTypeName = guardLicense.LicenseTypeName;

                }
                else if (guardLicense.LicenseType == null)
                {
                    //int intValueToCompare = -1;
                    //guardLicense.LicenseType = (GuardLicenseType)intValueToCompare;
                    return new JsonResult(new
                    {
                        status = false,
                        message = "LicenseType Required"
                    });
                }

                _guardDataProvider.SaveGuardLicense(guardLicense);
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message;
                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "License number already exists";
                }
            }

            return new JsonResult(new { status, dbxUploaded, message });
        }

        public JsonResult OnPostDeleteGuardLicense(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardDataProvider.DeleteGuardLicense(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetGuardCompliances(int guardId)
        {
            return new JsonResult(_guardDataProvider.GetGuardCompliances(guardId));
        }

        public JsonResult OnGetHRDescription(int HRid, int GuardID)
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
                    };
                    combinedDataList.Add(combinedData);
                }

            }

            return new JsonResult(combinedDataList);
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

        public class CombinedData
        {
            public int HRGroupId { get; set; }
            public string Description { get; set; }
            public string UsedDescription { get; set; }
            public string ReferenceNo { get; set; }
        }
        //public JsonResult OnPostSaveGuardComplianceandlicanse(GuardComplianceAndLicense guardComplianceandlicense)
        //{
        //    ModelState.Remove("guardComplianceandlicense.Id");
        //    ModelState.Remove("guardComplianceandlicense.LicenseTypeText");
        //    ModelState.Remove("guardComplianceandlicense.LicenseType");

        //    if (!ModelState.IsValid)
        //    {
        //        return new JsonResult(new
        //        {
        //            status = false,
        //            message = ModelState.Where(x => x.Value.Errors.Count > 0)
        //                        .Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage)))
        //        });
        //    }

        //    var status = true;
        //    var dbxUploaded = true;
        //    var message = "Success";
        //    GuardCompliance guardCompliance = new GuardCompliance()
        //    {
        //       GuardId= guardComplianceandlicense.GuardId,
        //       ReferenceNo= guardComplianceandlicense.ReferenceNo,
        //       Description= guardComplianceandlicense.Description,
        //       ExpiryDate= guardComplianceandlicense.ExpiryDate,
        //       Reminder1= guardComplianceandlicense.Reminder1,
        //       Reminder2= guardComplianceandlicense.Reminder2,
        //       FileName= guardComplianceandlicense.FileName,
        //       HrGroup= guardComplianceandlicense.HrGroup
        //    };

        //    GuardLicense guardLicenseNew = new GuardLicense()
        //    {
        //        GuardId = guardComplianceandlicense.GuardId,
        //        LicenseNo = guardComplianceandlicense.LicenseNo,
        //        LicenseType = (GuardLicenseType?)guardComplianceandlicense.LicenseType,
        //        LicenseTypeName = guardComplianceandlicense.LicenseTypeName,
        //        ExpiryDate = guardComplianceandlicense.ExpiryDate,
        //        Reminder1 = guardComplianceandlicense.Reminder1,
        //        Reminder2 = guardComplianceandlicense.Reminder2,
        //        FileName = guardComplianceandlicense.FileName,

        //    };

        //    try
        //    {
        //        dbxUploaded = UploadGuardComplianceToDropbox(guardCompliance);
        //        _guardDataProvider.SaveGuardCompliance(guardCompliance);
        //        //To Save License data start
        //        dbxUploaded = UploadGuardLicenseToDropbox(guardLicenseNew);
        //        if (guardLicenseNew.LicenseType == null && guardLicenseNew.Id != null && guardLicenseNew.LicenseTypeName != null)
        //        {
        //            //int intValueToCompare = -1;
        //            int intValueToCompare = 0;
        //            var licensetypeCount = _guardDataProvider.GetLicenseTypes().Where(x => x.Name == guardLicenseNew.LicenseTypeName);
        //            if (licensetypeCount.Count() > 0)
        //            {
        //                intValueToCompare = _guardDataProvider.GetLicenseTypes().Where(x => x.Name == guardLicenseNew.LicenseTypeName).FirstOrDefault().Id;
        //            }
        //            else
        //            {
        //                intValueToCompare = -1;
        //            }
        //            guardLicenseNew.LicenseType = (GuardLicenseType)intValueToCompare;
        //            guardLicenseNew.LicenseTypeName = guardLicenseNew.LicenseTypeName;

        //        }
        //        else if (guardLicenseNew.LicenseType == null)
        //        {
        //            //int intValueToCompare = -1;
        //            //guardLicense.LicenseType = (GuardLicenseType)intValueToCompare;
        //            return new JsonResult(new
        //            {
        //                status = false,
        //                message = "LicenseType Required"
        //            });
        //        }

        //        _guardDataProvider.SaveGuardLicense(guardLicenseNew);

        //        //To Save License data stop
        //    }
        //    catch (Exception ex)
        //    {
        //        status = false;
        //        message = ex.Message;
        //    }
        //    return new JsonResult(new { status, dbxUploaded, message });
        //}

        public JsonResult OnPostSaveGuardComplianceandlicanse(GuardComplianceAndLicense guardComplianceandlicense)
        {
            guardComplianceandlicense.DateType = guardComplianceandlicense.IsDateFilterEnabledHidden;
            ModelState.Remove("guardComplianceandlicense.Id");
            ModelState.Remove("guardComplianceandlicense.CurrentDateTime");
            ModelState.Remove("guardComplianceandlicense.LicenseNo");
            ModelState.Remove("guardComplianceandlicense.DataType");
            ModelState.Remove("guardComplianceandlicense.IsDateFilterEnabledHidden");

            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    status = false,
                    message = ModelState.Where(x => x.Value.Errors.Count > 0)
                                .Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage)))
                });
            }
            if (!string.IsNullOrEmpty(guardComplianceandlicense.Description))
            {
                guardComplianceandlicense.Description = Regex.Replace(guardComplianceandlicense.Description, "[✔️❌]", "").Trim();
            }
            var status = true;
            var dbxUploaded = true;
            var message = "Success";
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
                    //var RefrenceNoList = _guardDataProvider.GetHRRefernceNo(Convert.ToInt16(guardComplianceandlicense.HrGroup));


                    string extension = Path.GetExtension(guardComplianceandlicense.FileName);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(guardComplianceandlicense.FileName);
                    guardComplianceandlicense.FileName = guardComplianceandlicense.FileName;
                }

                try
                {
                    dbxUploaded = UploadGuardComplianceandLicenseToDropbox(guardComplianceandlicense);
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
            return new JsonResult(new { status, dbxUploaded, message });
        }

        public JsonResult OnPostSaveGuardComplianceandlicanseNew(GuardComplianceAndLicense guardComplianceandlicense)
        {
            guardComplianceandlicense.DateType = guardComplianceandlicense.IsDateFilterEnabledHidden;
            ModelState.Remove("guardComplianceandlicense.Id");
            ModelState.Remove("guardComplianceandlicense.CurrentDateTime");
            ModelState.Remove("guardComplianceandlicense.LicenseNo");
            ModelState.Remove("guardComplianceandlicense.DataType");
            ModelState.Remove("guardComplianceandlicense.IsDateFilterEnabledHidden");
            ModelState.Remove("guardComplianceandlicense.IsDateFilterEnabledHidden");
            ModelState.Remove("guardComplianceandlicense.ExpiryDate");

            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    status = false,
                    message = ModelState.Where(x => x.Value.Errors.Count > 0)
                                .Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage)))
                });
            }
            if (!string.IsNullOrEmpty(guardComplianceandlicense.Description))
            {
                guardComplianceandlicense.Description = Regex.Replace(guardComplianceandlicense.Description, "[✔️❌]", "").Trim();
            }
            var status = true;
            var dbxUploaded = true;
            var message = "Success";
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
            return new JsonResult(new { status, dbxUploaded, message });
        }
        public JsonResult OnPostSaveGuardCompliance(GuardCompliance guardCompliance)
        {
            ModelState.Remove("guardCompliance.Id");
            if (!ModelState.IsValid)
            {
                return new JsonResult(new
                {
                    status = false,
                    message = ModelState.Where(x => x.Value.Errors.Count > 0)
                                .Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage)))
                });
            }

            var status = true;
            var dbxUploaded = true;
            var message = "Success";

            try
            {
                dbxUploaded = UploadGuardComplianceToDropbox(guardCompliance);
                _guardDataProvider.SaveGuardCompliance(guardCompliance);
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message;
            }
            return new JsonResult(new { status, dbxUploaded, message });
        }

        public JsonResult OnPostDeleteGuardCompliance(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardDataProvider.DeleteGuardCompliance(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostUploadGuardAttachment()
        {
            var success = true;
            var files = Request.Form.Files;

            var guardId = Request.Form["guardId"];
            var LicenseNo = Request.Form["LicenseNo"].ToString();
            var Description = Request.Form["Description"].ToString();
            var HRid = Request.Form["HRID"].ToString();
            var fileName = string.Empty;
            var CurrentDate = DateTime.Now.Ticks / 1000;
            var ExpiryDate = Request.Form["ExpiryDate"];
            var dateType = Request.Form["DateType"];
            int hrIdInt;
            if (!string.IsNullOrEmpty(HRid))
            {
                hrIdInt = Convert.ToInt32(HRid);

            }
            else
            {
                hrIdInt = 0;
            }
            var RefNo = "";
            var RefrenceNoList = _guardDataProvider.GetHRRefernceNo(hrIdInt, Description);
            if (RefrenceNoList != null)
            {
                RefNo = RefrenceNoList.ReferenceNo;
            }



            try
            {
                if (files.Count == 1 && RefrenceNoList != null)
                {
                    var file = files[0];
                    fileName = file.FileName;


                    string extension = ""; string newFileName = ""; var formattedDate = "";

                    extension = Path.GetExtension(fileName).ToLower();
                    if (string.IsNullOrEmpty(ExpiryDate))
                    {
                        newFileName = Description + extension;
                        fileName = RefNo + "_" + newFileName;
                        fileName = GetFilename(fileName);
                    }
                    else if (dateType == "true")
                    {
                        extension = Path.GetExtension(fileName).ToLower();
                        DateTime parsedDate = DateTime.Parse(ExpiryDate);
                        formattedDate = parsedDate.ToString("dd MMM yy").ToUpper();
                        newFileName = Description + "-" + "doi " + formattedDate + extension;
                        fileName = RefNo + "_" + newFileName;
                        fileName = GetFilename(fileName);
                    }
                    else
                    {
                        extension = Path.GetExtension(fileName).ToLower();
                        DateTime parsedDate = DateTime.Parse(ExpiryDate);
                        formattedDate = parsedDate.ToString("dd MMM yy").ToUpper();
                        newFileName = Description + "-" + "exp " + formattedDate + extension;
                        fileName = RefNo + "_" + newFileName;
                        fileName = GetFilename(fileName);
                    }



                    //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    //var fileNameUpload = fileNameWithoutExtension + "_" + CurrentDate + extension;




                    var guardUploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "Guards", "License", LicenseNo);

                    if (!Directory.Exists(guardUploadDir))
                        Directory.CreateDirectory(guardUploadDir);

                    string filePath = Path.Combine(guardUploadDir, fileName);
                    if (System.IO.File.Exists(filePath))
                    {

                        System.IO.File.Delete(filePath);

                    }
                    using var stream = System.IO.File.Create(Path.Combine(guardUploadDir, fileName));
                    file.CopyTo(stream);



                }

            }
            catch
            {
                success = false;
            }

            return new JsonResult(new { success, fileName, CurrentDate });
        }

        public string GetFilename(string filename)
        {
            // Use Regex to replace problematic characters with an underscore
            string newFilename = Regex.Replace(filename, @"[\/\\?%*:|""<>]", "_");
            return newFilename;
        }

        public IActionResult OnGetGuardLicenseAndCompliance(int guardId)
        {
            ViewData["Guard_Id"] = guardId;
            ViewData["Guard_License"] = _guardDataProvider.GetGuardLicenses(guardId);
            ViewData["Guard_Compliance"] = _guardDataProvider.GetGuardCompliances(guardId);

            return new PartialViewResult
            {
                ViewName = "_GuardAdditionalDetails",
                ViewData = new ViewDataDictionary(ViewData)
            };
        }

        public JsonResult OnPostDeleteGuardAttachment(int id, char type)
        {
            var status = true;

            try
            {
                if (type == 'c')
                {
                    var compliance = _guardDataProvider.GetGuardComplianceFile(id);
                    if (compliance != null)
                    {
                        compliance.FileName = string.Empty;
                        _guardDataProvider.SaveGuardComplianceandlicanse(compliance);
                    }

                }
            }
            catch (Exception)
            {
                status = false;
            }

            return new JsonResult(new { status });
        }


        public JsonResult OnPostGuardDetailsForRCLogin(string securityLicenseNo, string type)
        {
            var AccessPermission = false;
            int? LoggedInUserId = 0;
            string SuccessMessage = string.Empty;
            int? SuccessCode = 0;
            int? GuId = 0;
            AuthUserHelper.IsAdminPowerUser = false;
            AuthUserHelper.IsAdminGlobal = false;

            if (!string.IsNullOrEmpty(securityLicenseNo))
            {
                var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(securityLicenseNo);
                if (guard != null)
                {
                    if (guard.IsAdminPowerUser)
                    {
                        AuthUserHelper.IsAdminPowerUser = true;
                    }
                    else
                    {
                        AuthUserHelper.IsAdminPowerUser = false;
                    }
                    if (guard.IsAdminGlobal)
                    {
                        AuthUserHelper.IsAdminGlobal = true;
                    }
                    else
                    {
                        AuthUserHelper.IsAdminGlobal = false;
                    }
                    if (guard.IsAdminThirdPartyAccess)
                    {
                        AuthUserHelper.IsAdminThirdParty = true;
                    }
                    else
                    {
                        AuthUserHelper.IsAdminThirdParty = false;
                    }


                    //code to check the SecurityNo of Type Patrols&Alarm Statics
                    if (type == "Patrols")
                    {
                        if (guard.IsActive)
                        {
                            if (guard.IsSTATS || guard.IsSTATSChartsAccess)
                            {
                                AccessPermission = true;
                                GuId = guard.Id;
                                if (AuthUserHelper.LoggedInUserId != null)
                                {
                                    LoggedInUserId = AuthUserHelper.LoggedInUserId;

                                }
                                HttpContext.Session.SetString("GuardId", guard.Id.ToString());
                                SuccessCode = 1;
                            }
                            else
                            {
                                if (guard.IsAdminGlobal)
                                {
                                    AccessPermission = true;
                                    GuId = guard.Id;
                                    if (AuthUserHelper.LoggedInUserId != null)
                                    {
                                        LoggedInUserId = AuthUserHelper.LoggedInUserId;
                                    }
                                    SuccessCode = 1;
                                }
                                else
                                {
                                    SuccessMessage = "Not authorized to access this page";
                                }
                            }
                        }
                        else
                        {
                            SuccessMessage = "Guard is inactive";
                        }

                    }

                    if (type == "KPI")
                    {
                        if (guard.IsActive)
                        {
                            if (guard.IsKPIAccess)
                            {
                                AccessPermission = true;
                                GuId = guard.Id;
                                if (AuthUserHelper.LoggedInUserId != null)
                                {
                                    LoggedInUserId = AuthUserHelper.LoggedInUserId;

                                }

                                SuccessCode = 1;
                            }
                            else
                            {
                                SuccessMessage = "Not authorized to access this page";
                            }
                        }
                        else
                        {
                            SuccessMessage = "Guard is inactive";
                        }

                    }
                    if (type == "RC")
                    {
                        if (guard.IsActive)
                        {
                            if (guard.IsRCAccess || guard.IsRCFusionAccess || guard.IsRCHRAccess || guard.IsRCLiteAccess)
                            {
                                AccessPermission = true;
                                GuId = guard.Id;
                                if (AuthUserHelper.LoggedInUserId != null)
                                {
                                    LoggedInUserId = AuthUserHelper.LoggedInUserId;
                                }
                                SuccessCode = 1;
                               
                            }
                            else
                            {
                                if (guard.IsAdminGlobal || guard.IsAdminPowerUser || guard.IsAdminSOPToolsAccess || guard.IsAdminAuditorAccess || guard.IsAdminInvestigatorAccess)
                                {
                                    AccessPermission = true;
                                    GuId = guard.Id;
                                    if (AuthUserHelper.LoggedInUserId != null)
                                    {
                                        LoggedInUserId = AuthUserHelper.LoggedInUserId;
                                    }
                                    SuccessCode = 1;
                                }
                                else
                                {
                                    SuccessMessage = "Not authorized to access this page";
                                }
                            }
                        }
                        else
                        {
                            SuccessMessage = "Guard is inactive";
                        }
                    }


                    if (type == "IR")

                    {
                        /* Store the value of the Guard Id to seesion for create the Ir from the session-start */
                        HttpContext.Session.SetString("GuardId", guard.Id.ToString());
                        if (guard.Mobile == null || guard.Mobile == "+61 4")
                        {
                            SuccessMessage = "Mobile is null";
                        }
                        else
                        {
                            if (guard.IsActive)
                            {
                                if (guard.IsLB_KV_IR)
                                {
                                    AccessPermission = true;
                                    GuId = guard.Id;
                                    if (AuthUserHelper.LoggedInUserId != null)
                                    {
                                        LoggedInUserId = AuthUserHelper.LoggedInUserId;
                                    }
                                    SuccessCode = 1;
                                }
                                else
                                {
                                    SuccessMessage = "Not authorized to access this page";
                                }
                            }
                            else
                            {
                                SuccessMessage = "Guard is inactive";
                            }
                        }

                        //if (AuthUserHelper.LoggedInUserId != null)
                        //{
                        //    LoggedInUserId = AuthUserHelper.LoggedInUserId;
                        //}
                        //SuccessCode = 1;
                    }
                    //p1-203 Admin User Profile-start
                    if (type == "Settings")

                    {
                        /* Store the value of the Guard Id to seesion for create the Ir from the session-start */

                        if (guard.IsActive)
                        {
                            if (guard.IsAdminGlobal || guard.IsAdminPowerUser || guard.IsAdminSOPToolsAccess || guard.IsAdminAuditorAccess || guard.IsAdminInvestigatorAccess || guard.IsAdminThirdPartyAccess)
                            {
                                AccessPermission = true;
                                GuId = guard.Id;
                                if (AuthUserHelper.LoggedInUserId != null)
                                {
                                    LoggedInUserId = AuthUserHelper.LoggedInUserId;
                                }
                                SuccessCode = 1;
                            }
                            else
                            {
                                SuccessMessage = "Not authorized to access this page";
                            }
                        }
                        else
                        {
                            SuccessMessage = "Guard is inactive";
                        }


                        //if (AuthUserHelper.LoggedInUserId != null)
                        //{
                        //    LoggedInUserId = AuthUserHelper.LoggedInUserId;
                        //}
                        //SuccessCode = 1;
                    }
                    //p1-203 Admin User Profile-end
                    /* Store the value of the Guard Id to seesion for create the Ir from the session-end */

                }
                else
                {
                    SuccessMessage = "Invalid Security License No";

                }

            }
            return new JsonResult(new { AccessPermission, LoggedInUserId, GuId, SuccessCode, SuccessMessage });
        }

        private bool UploadGuardLicenseToDropbox(GuardLicense guardLicense)
        {
            guardLicense.Guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == guardLicense.GuardId);
            var existingGuardLicense = _guardDataProvider.GetGuardLicense(guardLicense.Id);
            if ((guardLicense.Id == 0 && string.IsNullOrEmpty(guardLicense.FileName)) ||
                (guardLicense.Id != 0 && existingGuardLicense.FileName == guardLicense.FileName))
                return true;

            var fileToUpload = Path.Combine(_reportRootDir, "Uploads", "Guards", guardLicense.GuardId.ToString(), guardLicense.FileName);
            var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardLicense.Guard)}/{guardLicense.LicenseNo}/{guardLicense.FileName}");
            return UpoadDocumentToDropbox(fileToUpload, dbxFilePath);
        }

        private bool UploadGuardComplianceToDropbox(GuardCompliance guardCompliance)
        {
            guardCompliance.Guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == guardCompliance.GuardId);
            var existingGuardCompliance = _guardDataProvider.GetGuardCompliance(guardCompliance.Id);
            if ((guardCompliance.Id == 0 && string.IsNullOrEmpty(guardCompliance.FileName)) ||
                (guardCompliance.Id != 0 && existingGuardCompliance.FileName == guardCompliance.FileName))
                return true;

            var fileToUpload = Path.Combine(_reportRootDir, "Uploads", "Guards", guardCompliance.GuardId.ToString(), guardCompliance.FileName);
            var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardCompliance.Guard)}/{guardCompliance.ReferenceNo}/{guardCompliance.FileName}");
            return UpoadDocumentToDropbox(fileToUpload, dbxFilePath);
        }
        private bool UploadGuardComplianceandLicenseToDropbox(GuardComplianceAndLicense guardComplianceandlicense)
        {
            guardComplianceandlicense.Guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == guardComplianceandlicense.GuardId);
            var existingGuardCompliance = _guardDataProvider.GetGuardComplianceFile(guardComplianceandlicense.Id);
            if ((guardComplianceandlicense.Id == 0 && string.IsNullOrEmpty(guardComplianceandlicense.FileName)) ||
                (guardComplianceandlicense.Id != 0 && existingGuardCompliance.FileName == guardComplianceandlicense.FileName))
                return true;


            var fileToUpload = Path.Combine(_reportRootDir, "Uploads", "Guards", "License", guardComplianceandlicense.LicenseNo, guardComplianceandlicense.FileName);
            var DropboxDir = _guardDataProvider.GetDrobox();
            var dbxFilePath = "";

            dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolderNew(guardComplianceandlicense.Guard, DropboxDir.DropboxDir)}/{guardComplianceandlicense.FileName}");

            //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardComplianceandlicense.Guard)}/{guardComplianceandlicense.FileName}");


            return UpoadDocumentToDropbox(fileToUpload, dbxFilePath);
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

            return UpoadDocumentToDropbox(fileToUpload, dbxFilePath);
        }
        private bool UpoadDocumentToDropbox(string fileToUpload, string dbxFilePath)
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
        //Dos and Donts-start
        public JsonResult OnGetDosandDontsFields(int typeId)
        {
            return new JsonResult(_guardLogDataProvider.GetDosandDontsFields(typeId));
        }

        public JsonResult OnPostSaveDosandDontsField(DosAndDontsField record)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                if (record.Id == -1)
                {
                    if (record.ReferenceNo != null)
                    {
                        record.ReferenceNo = record.ReferenceNo.Trim();
                        int refno;
                        if (int.TryParse(record.ReferenceNo, out refno) == false)
                        {
                            return new JsonResult(new { success = false, message = "Reference number should only contains numbers. !!!" });
                        }

                        var eventReferenceExists = _guardLogDataProvider.GetDosandDontsFields(record.TypeId).Where(x=>x.ReferenceNo==record.ReferenceNo);
                        if (eventReferenceExists.Count() > 0)
                        {
                            return new JsonResult(new { success = false, message = "Similar reference number already exists !!!" });
                        }
                    }
                    else
                    {
                        // Set Referenece number.
                        int LastOne = _guardLogDataProvider.GetDosandDontsFieldsCount(record.TypeId);
                        string newrefnumb = "";
                        bool refok = false;
                        do
                        {

                            LastOne++;
                            newrefnumb = LastOne.ToString("00");
                            var eventReferenceExists = _guardLogDataProvider.GetDosandDontsFields(record.TypeId).Where(x => x.ReferenceNo == record.ReferenceNo);
                            if (eventReferenceExists.Count() < 1)
                            {
                                refok = true;
                            }


                        } while (refok == false);
                        record.ReferenceNo = newrefnumb;
                    }
                   
                }
                else
                {
                    if (record.ReferenceNo != null)
                    {
                        record.ReferenceNo = record.ReferenceNo.Trim();
                        int refno;
                        if (int.TryParse(record.ReferenceNo, out refno) == false)
                        {
                            return new JsonResult(new { success = false, message = "Reference number should only contains numbers. !!!" });
                        }

                        var oldrefNo = _guardLogDataProvider.GetDosandDontsFields(record.TypeId).Where(x => x.Id == record.Id).FirstOrDefault().ReferenceNo;
                        if (!oldrefNo.Equals(record.ReferenceNo))
                        {
                            var eventReferenceExists = _guardLogDataProvider.GetDosandDontsFields(record.TypeId).Where(x => x.ReferenceNo == record.ReferenceNo);
                            if (eventReferenceExists.Count() > 0)
                            {
                                return new JsonResult(new { success = false, message = "Similar reference number already exists !!!" });
                            }
                        }
                    }

                }
                _guardLogDataProvider.SaveDosandDontsField(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteDosandDontsField(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardLogDataProvider.DeleteDosandDontsField(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //Dos and Donts-end
        public IActionResult OnGetGuardLicenseAndCompliancForGuardse(int guardId)
        {
            //ViewData["Guard_Id"] = guardId;
            //ViewData["Guard_License"] = _guardDataProvider.GetGuardLicenses(guardId);
            //ViewData["Guard_Compliance"] = _guardDataProvider.GetGuardCompliances(guardId);

            //return new PartialViewResult
            //{
            //    ViewName = "_GuardAdditionalDetails",
            //    ViewData = new ViewDataDictionary(ViewData)
            //};
            return new JsonResult(_guardDataProvider.GetGuards().Where(x => x.Id == guardId));
        }
        public IActionResult OnGetExpiredDocuments(int guardId)
        {
            var guardLicenses = _guardDataProvider.GetAllGuardLicenses().Where(z => z.ExpiryDate.HasValue && z.GuardId == guardId).ToList();
            var guardcompliances = _guardDataProvider.GetAllGuardCompliances().Where(z => z.ExpiryDate.HasValue && z.GuardId == guardId).ToList();

            var messages = new List<KeyValuePair<DateTime, string>>();
            messages.AddRange(GetLicenseMessages(guardLicenses));
            messages.AddRange(GetComplianceMessages(guardcompliances));
            return new JsonResult(messages);
        }
        private static IEnumerable<KeyValuePair<DateTime, string>> GetComplianceMessages(List<GuardCompliance> guardCompliances)
        {
            foreach (var compliance in guardCompliances)
            {
                if ((DateTime.Today.AddDays(compliance.Reminder1.GetValueOrDefault()) == compliance.ExpiryDate) ||
                    (DateTime.Today.AddDays(compliance.Reminder2.GetValueOrDefault()) == compliance.ExpiryDate))
                {
                    var message = $"<tr><td>Compliance</td><td>{compliance.ReferenceNo}</td><td>{compliance.Guard.Name}</td><td>{compliance.ExpiryDate?.ToString("dd-MMM-yyyy")}</td>";
                    yield return new KeyValuePair<DateTime, string>(compliance.ExpiryDate.Value, message);
                }
            }
        }

        private static IEnumerable<KeyValuePair<DateTime, string>> GetLicenseMessages(List<GuardLicense> guardLicenses)
        {
            foreach (var license in guardLicenses)
            {
                if ((DateTime.Today.AddDays(license.Reminder1.GetValueOrDefault()) == license.ExpiryDate) ||
                    (DateTime.Today.AddDays(license.Reminder2.GetValueOrDefault()) == license.ExpiryDate))
                {
                    var message = $"<tr><td>License</td><td>{license.LicenseNo}</td><td>{license.Guard.Name}</td><td>{license.ExpiryDate?.ToString("dd-MMM-yyyy")}</td>";
                    yield return new KeyValuePair<DateTime, string>(license.ExpiryDate.Value, message);
                }
            }
        }
        //for toggle areas - start 
        public void OnPostSaveToggleType(int siteId, int timeslottoggleTypeId, bool timeslotIsActive, int vwitoggleTypeId, bool vwiIsActive,
            int sendertoggleTypeId, bool senderIsActive, int reelstoggleTypeId, bool reelsIsActive)
        {

            _clientDataProvider.SaveClientSiteToggle(siteId, timeslottoggleTypeId, timeslotIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, vwitoggleTypeId, vwiIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, sendertoggleTypeId, senderIsActive);
            _clientDataProvider.SaveClientSiteToggle(siteId, reelstoggleTypeId, reelsIsActive);
        }
        public IActionResult OnGetClientSiteToggle(int siteId)
        {

            return new JsonResult(_guardDataProvider.GetClientSiteToggle().Where(x => x.ClientSiteId == siteId));
        }
        //for toggle areas - end
        // p1-191 hr files task 3-start
        public JsonResult OnPostSaveHRSettings(int Id, int hrGroupId, int refNoNumberId, int refNoAlphabetId, string description, int[] Selectedsites, string[] SelectedStates)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {
                //if (!ModelState.IsValid)
                //{
                //    return new JsonResult(new { status = false, message = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
                //}
                HrSettings hrSettingsnew = new HrSettings()
                {
                    Id = Id,
                    HRGroupId = hrGroupId,
                    ReferenceNoNumberId = refNoNumberId,
                    ReferenceNoAlphabetId = refNoAlphabetId,
                    Description = description

                };


                _guardLogDataProvider.SaveHRSettings(hrSettingsnew, Selectedsites, SelectedStates);


            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message = new[] { message }, id });
        }
        public JsonResult OnPostDeleteHRSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardLogDataProvider.DeleteHRSettings(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostUpdateLockSettings(int id)
        {
            var success = false;
            var message = string.Empty;
            //try
            //{
            //    _guardLogDataProvider.UpdateHRLockSettings(id);
            //    success = true;
            //}
            //catch (Exception ex)
            //{
            //    message = ex.Message;
            //}

            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetHRSettings()
        {
            var jresult = _configDataProvider.GetHRSettings()
            .Select(z => HrDoumentViewModel.FromDataModel(z))
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.referenceNo)
            .ThenBy(x => x.referenceNoAlphabetsName);

            return new JsonResult(jresult);

            //return new JsonResult(_configDataProvider.GetHRSettings());
        }
        public JsonResult OnPostSaveLicensesTypes(LicenseTypes record)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                _guardLogDataProvider.SaveLicensesTypes(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteLicensesTypes(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardLogDataProvider.DeleteLicensesTypes(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetLicensesTypes()
        {
            return new JsonResult(_configDataProvider.GetLicensesTypes());
        }
        public JsonResult OnGetLanguages()
        {
            return new JsonResult(_guardLogDataProvider.GetLanguages());
        }
        // p1-191 hr files task 3-end


        public JsonResult OnGetHrSettingById(int id)
        {

            return new JsonResult(_configDataProvider.GetHrSettingById(id));

        }


        public JsonResult OnPostGuardHrDocLoginConformation(int guardId, string key)
        {
            var AccessPermission = false;
            int? LoggedInUserId = 0;
            string SuccessMessage = string.Empty;
            int? SuccessCode = 0;
            int? GuId = 0;
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
            return new JsonResult(new { AccessPermission, LoggedInUserId, GuId, SuccessCode, SuccessMessage });
        }




        public JsonResult OnPostCheckIfPINSetForTheGuard(int guardId)
        {
            var AccessPermission = false;
            string SuccessMessage = string.Empty;
            var guard = _guardDataProvider.GetGuardDetailsUsingId(guardId);
            var firstGuard = guard.FirstOrDefault();
            if (firstGuard != null && firstGuard.Pin != null)
            {
                SuccessMessage = "Pin alerady Set ";
            }
            else
            {
                AccessPermission = true;
                SuccessMessage = "No PIN Set for you";

            }

            return new JsonResult(new { AccessPermission,SuccessMessage });
        }


        public JsonResult OnPostSaveNewPINSetForTheGuard(int guardId, string NewPIN)
        {
            var AccessPermission = false;
            string SuccessMessage = string.Empty;
            if (!string.IsNullOrEmpty(NewPIN))
            {
                var guard = _guardDataProvider.GetGuardDetailsUsingId(guardId);
                var firstGuard = guard.FirstOrDefault();
                if (firstGuard != null && firstGuard.Pin != null)
                {
                    SuccessMessage = "Pin alerady Set ";
                }
                else
                {
                    _guardDataProvider.SetGuardNewPIN(guardId, NewPIN);
                    AccessPermission = true;
                    SuccessMessage = "New PIN Set for you";

                }

            }
            else
            {
              
                    SuccessMessage = "Enter your New PIN";

                

            }

            return new JsonResult(new { AccessPermission, SuccessMessage });
        }

        public JsonResult OnGetHrsettingsUisngHrGroupId(int hrgroupId,string searchKeyNo)
        {
          
            return new JsonResult(_configDataProvider.GetHRSettingsUsingGroupId(hrgroupId, searchKeyNo));
           
        }


        public JsonResult OnGetKVCompanyDetails(string clientSiteIds, string searchKeyNo)
        {
            var arClientSiteIds = clientSiteIds?.Split(";").Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            return new JsonResult(_configDataProvider.GetCompanyDetailsUsingFilter(arClientSiteIds, searchKeyNo));

        }
        public JsonResult OnGetLanguageDetails(int guardId)
        {
            return new JsonResult(new { data = _guardLogDataProvider.GetLanguageDetails(guardId) });
        }

        
        public JsonResult OnPostResetGaurdHrPin(int guardId)
        {
            var message = string.Empty;
            var success = false;
            // Fetch guard details only once to avoid redundant queries
            var guard = _guardDataProvider.GetGuards().FirstOrDefault(z => z.Id == guardId);

            if (guard != null && !string.IsNullOrEmpty(guard.Email) && !string.IsNullOrEmpty(guard.Pin))
            {
                // Generate email body and send email
                var emailBody = GetPasswordResetEmail(guard.Name, guard.Pin);
                SendEmailNew(emailBody, guard.Email);

                // Update message
                message = $"PIN sent to the email ID: {guard.Email}";
                success = true;
            }
            else
            {
                // Handle missing data
                message = "Invalid guard details or missing email/PIN.";
                success = false;
            }

            return new JsonResult(new { message, success });
        }




        private void SendEmailNew(string mailBodyHtml, string ToAddress)
        {
            var fromAddress = _EmailOptions.FromAddress.Split('|');
            var Emails = _clientDataProvider.GetGlobalComplianceAlertEmail().ToList();
            var emailAddresses = string.Join(",", Emails.Select(email => email.Email));



            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromAddress[1], fromAddress[0]));
            if (emailAddresses != null && emailAddresses != "")
            {
                var toAddressNew = emailAddresses.Split(',');
                foreach (var address in GetToEmailAddressList(toAddressNew))
                    message.To.Add(address);
            }
            if (ToAddress != null && ToAddress != "")
            {
                var toAddressNew = ToAddress.Split(',');
                foreach (var address in GetToEmailAddressList(toAddressNew))

                    if (!message.To.Contains(address))
                    {
                        message.To.Add(address);
                    }

            }

            message.Subject = "HR Document PIN Reset";
            message.Bcc.Add(new MailboxAddress("globoconsoftware", "globoconsoftware@gmail.com"));
            var builder = new BodyBuilder()
            {
                HtmlBody = mailBodyHtml
            };
            message.Body = builder.ToMessageBody();
            using (var client = new SmtpClient())
            {
                client.Connect(_EmailOptions.SmtpServer, _EmailOptions.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                if (!string.IsNullOrEmpty(_EmailOptions.SmtpUserName) &&
                    !string.IsNullOrEmpty(_EmailOptions.SmtpPassword))
                    client.Authenticate(_EmailOptions.SmtpUserName, _EmailOptions.SmtpPassword);
                client.Send(message);
                client.Disconnect(true);
            }

        }
        private List<MailboxAddress> GetToEmailAddressList(string[] toAddress)
        {
            var emailAddressList = new List<MailboxAddress>();
            foreach (var item in toAddress)
            {
                emailAddressList.Add(new MailboxAddress(string.Empty, item));
            }
            return emailAddressList;
        }



        public string GetPasswordResetEmail(string userName, string temporaryPassword)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body {");
            sb.AppendLine("    font-family: Arial, sans-serif;");
            sb.AppendLine("    line-height: 1.6;");
            sb.AppendLine("    color: #333;");
            sb.AppendLine("    background-color: #f9f9f9;");
            sb.AppendLine("    margin: 0;");
            sb.AppendLine("    padding: 0;");
            sb.AppendLine("}");
            sb.AppendLine(".email-container {");
            sb.AppendLine("    width: 100%;");
            sb.AppendLine("    max-width: 600px;");
            sb.AppendLine("    margin: 20px auto;");
            sb.AppendLine("    background-color: #ffffff;");
            sb.AppendLine("    border: 1px solid #ddd;");
            sb.AppendLine("    padding: 20px;");
            sb.AppendLine("    border-radius: 8px;");
            sb.AppendLine("    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);");
            sb.AppendLine("}");
            sb.AppendLine(".email-header {");
            sb.AppendLine("    text-align: center;");
            sb.AppendLine("    font-size: 18px;");
            sb.AppendLine("    font-weight: bold;");
            sb.AppendLine("    margin-bottom: 20px;");
            sb.AppendLine("}");
            sb.AppendLine(".temporary-password {");
            sb.AppendLine("    font-weight: bold;");
            sb.AppendLine("    background-color: #f2f2f2;");
            sb.AppendLine("    padding: 5px 10px;");
            sb.AppendLine("    border-radius: 5px;");
            sb.AppendLine("    display: inline-block;");
            sb.AppendLine("    margin-left: 5px;"); // Slight spacing after the label
            sb.AppendLine("}");
            sb.AppendLine(".footer {");
            sb.AppendLine("    margin-top: 20px;");
            sb.AppendLine("    font-size: 12px;");
            sb.AppendLine("    color: #666;");
            sb.AppendLine("    text-align: center;");
            sb.AppendLine("}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"email-container\">");
            sb.AppendLine("    <div class=\"email-header\">");
            sb.AppendLine("        HR PIN Reset Request");
            sb.AppendLine("    </div>");
            sb.AppendLine($"    <p>Hi {userName},</p>");
            sb.AppendLine($"    <p>Here is your HR PIN: <span class=\"temporary-password\">{temporaryPassword}</span></p>");
            sb.AppendLine("    <div class=\"footer\">");
            sb.AppendLine("        <p>If you have any questions, please contact our support team.</p>");
            sb.AppendLine($"        <p>&copy; {DateTime.Today.Year} C4i System. All rights reserved.</p>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();

        }
        //p5-Issue-1-start
        public JsonResult OnGetGuardTrainingAndAssessmentTab(int guardId)
        {
            return new JsonResult(_guardDataProvider.GetGuardTrainingAndAssessment(guardId));
        }
        //p5-Issue1-end
        //p5-Issue-20-Instructor-start
        public JsonResult OnGetTrainingInstructorNameandPositionFields()
        {
            return new JsonResult(_guardLogDataProvider.GetTrainingInstructorNameandPositionFields());
        }
        public JsonResult OnPostSaveTrainingInstructorNameandPositionFields(TrainingInstructor record)
        {
            var success = false;
            var message = string.Empty;
            try
            {


                _guardLogDataProvider.SaveTrainingInstructorNameandPositionFields(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteTrainingInstructorNameandPositionFields(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardLogDataProvider.DeleteTrainingInstructorNameandPositionFields(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //p5-Issue-20-Instructor-end
    }









}