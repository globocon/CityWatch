using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;

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

        public GuardSettingsModel(IViewDataService viewDataService,
             IClientSiteWandDataProvider clientSiteWandDataProvider,
             IClientDataProvider clientDataProvider,
             IGuardDataProvider guardDataProvider,
             IConfigDataProvider configDataProvider,
             IGuardLogDataProvider guardLogDataProvider,
             IGuardSettingsDataProvider guardSettingsDataProvider,
             IWebHostEnvironment webHostEnvironment,
             IDropboxService dropboxUploadService,
             IOptions<Settings> settings)
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
        }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

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

        public JsonResult OnGetPatrolCar(int clientSiteId)
        {
            return new JsonResult(_clientSiteWandDataProvider.GetClientSitePatrolCars(clientSiteId).ToList());
        }

        public JsonResult OnPostPatrolCar(ClientSitePatrolCar record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.SaveClientSitePatrolCar(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }


        public JsonResult OnPostDeletePatrolCar(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientSiteWandDataProvider.DeleteClientSitePatrolCar(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

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

        public JsonResult OnGetClientSiteKeys(int clientSiteId)
        {
            return new JsonResult(_guardSettingsDataProvider.GetClientSiteKeys(clientSiteId).ToList());
        }

        public JsonResult OnPostClientSiteKey(ClientSiteKey clientSiteKey)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.SaveClientSiteKey(clientSiteKey);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "Key number already exists";
                }
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteClientSiteKey(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.DeleteClientSiteKey(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

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
                        guard.IsKPIAccess = true;
                    }
                    else if (val == 4)
                    {
                        guard.IsRCAccess = true;
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

            return new JsonResult(new
            {
                status,
                message,
                initalsChangedMessage = initalsUsed != guardInitals ? $"Guard with initials {guardInitals} already exists. So initals changed to {initalsUsed}" : string.Empty,
                initalsUsed,
                guardId
            });
        }

        public JsonResult OnGetClientStates()
        {
            return new JsonResult(_configDataProvider.GetStates());
        }

        public JsonResult OnPostCustomFields(ClientSiteCustomField clientSiteCustomField)
        {
            var status = true;
            var message = "Success";
            var id = -1;
            try
            {
                if (!ModelState.IsValid)
                {
                    return new JsonResult(new { status = false, message = ModelState.Where(x => x.Value.Errors.Count > 0).Select(x => string.Join(',', x.Value.Errors.Select(y => y.ErrorMessage))) });
                }

                id = _guardLogDataProvider.SaveClientSiteCustomFields(clientSiteCustomField);

                var clientSiteLogBookId = _clientDataProvider.GetClientSiteLogBook(clientSiteCustomField.ClientSiteId, LogBookType.DailyGuardLog, DateTime.Today)?.Id;
                if (clientSiteLogBookId.HasValue)
                {
                    var customFieldLogs = _guardLogDataProvider.GetCustomFieldLogs(clientSiteLogBookId.Value).Where(x => x.CustomFieldId == id);
                    if (!customFieldLogs.Any())
                    {
                        var customFieldLog = new CustomFieldLog
                        {
                            CustomFieldId = id,
                            ClientSiteLogBookId = clientSiteLogBookId.Value
                        };
                        _guardLogDataProvider.SaveCustomFieldLog(customFieldLog);
                    }
                }

            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message = new[] { message }, id });
        }

        public IActionResult OnGetClientSiteCustomFields(int clientSiteId)
        {
            return new JsonResult(_guardLogDataProvider.GetCustomFieldsByClientSiteId(clientSiteId));
        }

        public IActionResult OnGetCustomFields()
        {
            var customFields = _guardLogDataProvider.GetClientSiteCustomFields();
            var fieldNames = customFields.Select(z => z.Name).Distinct().OrderBy(z => z);
            var slots = customFields.Select(z => z.TimeSlot).Distinct().OrderBy(z => z);
            return new JsonResult(new { fieldNames, slots });
        }

        public IActionResult OnPostDeleteClientSiteCustomField(int id)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.DeleteClientSiteCustomFields(id);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnGetSitePocs(int clientSiteId)
        {
            return new JsonResult(_guardSettingsDataProvider.GetClientSitePocs(clientSiteId));
        }

        public IActionResult OnPostSitePoc(ClientSitePoc record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.SaveClientSitePoc(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A site POC with same name already exists. This may be deleted";
                }
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnPostDeleteSitePoc(int id)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardSettingsDataProvider.DeleteClientSitePoc(id);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnGetSiteLocations(int clientSiteId)
        {
            return new JsonResult(_guardSettingsDataProvider.GetClientSiteLocations(clientSiteId));
        }

        public IActionResult OnPostSiteLocation(ClientSiteLocation record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardSettingsDataProvider.SaveClientSiteLocation(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A site location with same name already exists. This may be deleted";
                }
            }
            return new JsonResult(new { success, message });
        }

        public IActionResult OnPostDeleteSiteLocation(int id)
        {
            var success = true;
            var message = "Success";
            try
            {
                _guardSettingsDataProvider.DeleteClientSiteLocation(id);
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

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

        public JsonResult OnPostUpdateSiteDataCollection(int clientSiteId, bool disabled)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _clientDataProvider.SetDataCollectionStatus(clientSiteId, !disabled);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetGuardLicense(int guardId)
        {
            return new JsonResult(_guardDataProvider.GetGuardLicenses(guardId));
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
                if (guardLicense.LicenseType==null && guardLicense.Id!=null && guardLicense.LicenseTypeName!=null)
                {
                    int intValueToCompare = -1;
                    guardLicense.LicenseType = (GuardLicenseType)intValueToCompare;
                    guardLicense.LicenseTypeName = guardLicense.LicenseTypeName;

                }
                else if(guardLicense.LicenseType == null)
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
            var fileName = string.Empty;

            try
            {
                if (files.Count == 1)
                {
                    var file = files[0];
                    fileName = file.FileName;
                    var guardUploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "Guards", guardId);
                    if (!Directory.Exists(guardUploadDir))
                        Directory.CreateDirectory(guardUploadDir);

                    using var stream = System.IO.File.Create(Path.Combine(guardUploadDir, fileName));
                    file.CopyTo(stream);
                }

            }
            catch
            {
                success = false;
            }
            return new JsonResult(new { success, fileName });
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
                if (type == 'l')
                {
                    var license = _guardDataProvider.GetGuardLicense(id);
                    license.FileName = string.Empty;
                    _guardDataProvider.SaveGuardLicense(license);
                }
                else if (type == 'c')
                {
                    var compliance = _guardDataProvider.GetGuardCompliance(id);
                    compliance.FileName = string.Empty;
                    _guardDataProvider.SaveGuardCompliance(compliance);
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
            if (!string.IsNullOrEmpty(securityLicenseNo))
            {
                var guard = _guardDataProvider.GetGuardDetailsbySecurityLicenseNo(securityLicenseNo);
                if (guard != null)
                {
                    //code to check the SecurityNo of Type Patrols&Alarm Statics
                    if (type == "Patrols")
                    {
                        if (guard.IsActive)
                        {
                            if (guard.IsSTATS)
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
                            if (guard.IsRCAccess)
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


                    if(type=="IR")

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

        private bool UpoadDocumentToDropbox(string fileToUpload, string dbxFilePath)
        {
            var dropboxSettings = new DropboxSettings(_settings.DropboxAppKey, _settings.DropboxAppSecret, _settings.DropboxAccessToken,
                                                        _settings.DropboxRefreshToken, _settings.DropboxUserEmail);

            bool uploaded = false;
            try
            {
                uploaded = Task.Run(() => _dropboxUploadService.Upload(dropboxSettings, fileToUpload, dbxFilePath)).Result;
                if (uploaded && System.IO.File.Exists(fileToUpload))
                    System.IO.File.Delete(fileToUpload);
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
            return new JsonResult(_guardDataProvider.GetGuards().Where(x=>x.Id== guardId));
        }
        public IActionResult OnGetExpiredDocuments(int guardId)
        {
            var guardLicenses = _guardDataProvider.GetAllGuardLicenses().Where(z => z.ExpiryDate.HasValue && z.GuardId==guardId).ToList();
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
    }
}