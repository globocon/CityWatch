using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using Dropbox.Api.Files;
using MailKit.Search;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Dropbox.Api.FileRequests.GracePeriod;
using static Dropbox.Api.TeamLog.ActorLogInfo;
using static Dropbox.Api.TeamLog.EventCategory;
using static Dropbox.Api.TeamLog.SpaceCapsType;

using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Office.Core;

using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Data.Enums;

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Org.BouncyCastle.Crypto.Macs;
using System.Net.Http.Headers;
using MailKit.Net.Smtp;
using System.Text.RegularExpressions;
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;
using static Dropbox.Api.Team.GroupSelector;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using static Dropbox.Api.Sharing.MemberSelector;



namespace CityWatch.Web.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IUserDataProvider _userDataProvider;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly ITimesheetReportGenerator _TimesheetReportGenerator;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IDropboxService _dropboxUploadService;
        private readonly Helpers.Settings _settings;
        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IViewDataService viewDataService,
            IGuardLogDataProvider guardLogDataProvider,
             ITimesheetReportGenerator TimesheetReportGenerator,IGuardDataProvider guardDataProvider, IOptions<Helpers.Settings> settings,
             IDropboxService dropboxUploadService)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _userDataProvider = userDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _viewDataService = viewDataService;
            _TimesheetReportGenerator = TimesheetReportGenerator;
            _guardDataProvider = guardDataProvider;
            _settings = settings.Value;
            _dropboxUploadService = dropboxUploadService;
        }
        public string IsAdminminOrPoweruser = string.Empty;
        public HrSettings HrSettings;
        public IncidentReportField IncidentReportField;
        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }

        public IUserDataProvider UserDataProvider { get { return _userDataProvider; } }

        public IClientDataProvider ClientDataProvider { get { return _clientDataProvider; } }

        [BindProperty]
        public FeedbackTemplate FeedbackTemplate { get; set; }
        [BindProperty]
        public FeedbackType FeedbackNewType { get; set; }
        [BindProperty]
        public CompanyDetails CompanyDetails { get; set; }

        [BindProperty]
        public ReportTemplate ReportTemplate { get; set; }

        public string SecurityLicenseNo { get; set; }
        public string loggedInUserId { get; set; }
        public int GuardId { get; set; }
        public GuardViewModel Guard { get; set; }
        
         public int ClientTypeId { get; set; }

        public IActionResult OnGet()
        {
            string securityLicenseNonew = Request.Query["Sl"];
            string guid = Request.Query["guid"];
            string luid = Request.Query["lud"];
            GuardId = Convert.ToInt32(guid);
            var host = HttpContext.Request.Host.Host;
            var clientName = string.Empty;
            var clientLogo = string.Empty;
            var url = string.Empty;

            // Split the host by dots to separate subdomains and domain name
            var hostParts = host.Split('.');

            // If the first part is "www", take the second part as the client name
            if (hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www")
            {
                clientName = hostParts[1];
            }
            else
            {
                clientName = hostParts[0];
            }
            if (!string.IsNullOrEmpty(clientName))
            {
                if (
                    clientName.Trim().ToLower() != "www" &&
                    clientName.Trim().ToLower() != "cws-ir" &&
                    clientName.Trim().ToLower() != "test"
                    &&
                    clientName.Trim().ToLower() != "localhost"
                )
                {
                    int domain = _configDataProvider.GetSubDomainDetails(clientName).TypeId;
                    if (domain != 0)
                    {
                        ClientTypeId = domain;
                    }
                    else
                    {
                        ClientTypeId = 0;
                    }
                }
            }
                if (GuardId != 0)
            {
                Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);

            }
            if (!AuthUserHelper.IsAdminUserLoggedIn && !AuthUserHelper.IsAdminGlobal && !AuthUserHelper.IsAdminPowerUser  && !Guard.IsAdminSOPToolsAccess && !Guard.IsAdminAuditorAccess && !Guard.IsAdminInvestigatorAccess && !Guard.IsAdminThirdPartyAccess)
            {
                return Redirect(Url.Page("/Account/Unauthorized"));
            }
            else
            {

                ReportTemplate = _configDataProvider.GetReportTemplate();
                SecurityLicenseNo = securityLicenseNonew;
                
                loggedInUserId = luid;
                
                return Page();

            }
        }

        public JsonResult OnGetClientTypes(int? page, int? limit)
        {
            // return new JsonResult(_viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId));
            //p1-259 counter-start
            var clienttypes = _viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId);
            foreach (var item in clienttypes)
            {
                item.ClientSiteCount = _viewDataService.GetClientTypeCount(item.Id);
                var result = _userDataProvider.GetDomainDeatils(item.Id);
                if (result != null)
                {
                    item.IsSubDomainEnabled = result.Enabled;
                }
            }
            return new JsonResult(clienttypes);
            //p1-259 counter-stop
        }

        public JsonResult OnGetClientSites(int? page, int? limit, int? typeId, string searchTerm, string searchTermtwo)
        {
            return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, AuthUserHelper.LoggedInUserId, searchTerm, searchTermtwo));
        }
        public JsonResult OnGetClientSitesExcel(int? page, int? limit, int? typeId, string searchTerm, string searchTermtwo)
        {
            return new JsonResult(_viewDataService.GetUserClientSitesExcel(typeId, AuthUserHelper.LoggedInUserId));
        }
        public JsonResult OnGetClientStates()
        {
            return new JsonResult(_configDataProvider.GetStates());
        }

        public JsonResult OnPostClientTypes(ClientType record)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.SaveClientType(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostClientSites(ClientSite record)
        {
            var status = true;
            var message = "Success";
            try
            {
                if (record.Id == -1)
                {
                    var clientsites = _viewDataService.GetUserClientSitesHavingAccess(null, null, record.Name);
                    if (clientsites.Count() > 0)
                    {
                        status = false;
                        message = "Error: " + "A profile with same client site name already exists";
                        return new JsonResult(new { status = status, message = message });
                    }
                }
                if (string.IsNullOrEmpty(record.Address))
                {
                    record.Gps = string.Empty;
                }
                _clientDataProvider.SaveClientSite(record);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostDeleteClientType(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                var clientsites = _viewDataService.GetUserClientSitesHavingAccess(id, AuthUserHelper.LoggedInUserId, null);
                if (clientsites.Count == 0)
                {
                    _clientDataProvider.DeleteClientType(id);
                }
                else
                {
                    status = false;
                    message = "Error " + " Some Client Sites are Active under this Client Typ,e so delete the Client Sites first\"";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostDeleteClientSite(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                //var useraccess = _clientDataProvider.GetUserAccessWithClientSiteId(id);
                //if (useraccess.Count == 0)
                //{
                _clientDataProvider.DeleteClientSite(id);
                //}
                //else
                //{
                //    status = false;

                //    message = "Error " + "Please unallocate the users who have access to the  site";
                //}
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostUpdateUserStatus(int id, bool deleted)
        {
            var status = true;
            var message = "Success";
            try
            {
                _userDataProvider.UpdateUserStatus(id, deleted);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }

        public JsonResult OnPostShowPassword(Data.Models.User user)
        {
            var value = string.Empty;
            try
            {
                var currUser = _userDataProvider.GetUsers().SingleOrDefault(x => x.Id == user.Id);
                if (currUser != null)
                    value = PasswordHelper.DecryptPassword(currUser.Password);
            }
            catch
            {
            }

            return new JsonResult(value);
        }

        public IActionResult OnGetFeedbackTemplate(int templateId)
        {
            var template = _configDataProvider.GetFeedbackTemplates().SingleOrDefault(x => x.Id == templateId);
            return new JsonResult(template);
        }

        public IActionResult OnGetFeedbackTemplateList()
        {
            return new JsonResult(_configDataProvider.GetFeedbackTemplates());
        }

        public JsonResult OnPostFeedbackTemplate()
        {
            var success = false;
            var message = "Updated successfully";
            if (FeedbackTemplate != null)
            {
                try
                {
                    if (FeedbackTemplate.Id == 0)
                    {
                        if (string.IsNullOrEmpty(FeedbackTemplate.Name) || string.IsNullOrEmpty(FeedbackTemplate.Text))
                            throw new ArgumentNullException("Required fields are missing");

                        if (_configDataProvider.GetFeedbackTemplates().Any(x => x.Name.Equals(FeedbackTemplate.Name)))
                            throw new ArgumentException($"Template name {FeedbackTemplate.Name} already exists!");
                    }

                    _configDataProvider.SaveFeedbackTemplate(FeedbackTemplate);
                    success = true;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }

            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteFeedbackTemplate()
        {
            var success = false;
            var message = "Deleted successfully";
            if (FeedbackTemplate != null)
            {
                try
                {
                    _configDataProvider.DeleteFeedbackTemplate(FeedbackTemplate.Id);
                    success = true;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }
            return new JsonResult(new { success, message });
        }
        //to delete existing feedback type -end
        public JsonResult OnPostIrTemplateUpload()
        {
            var success = false;
            var message = "Uploaded successfully";
            var dateTimeUpdated = DateTime.Now;
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (Path.GetExtension(file.FileName) != ".pdf")
                            throw new ArgumentException("Unsupported file type");

                        var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Template");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir, "IR_Form_Template.pdf")))
                        {
                            file.CopyTo(stream);
                        }
                        _configDataProvider.SaveReportTemplate(dateTimeUpdated);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            return new JsonResult(new { success, message, dateTimeUpdated = dateTimeUpdated.ToString("dd MMM yyyy @ HH:mm") });
        }

        public JsonResult OnPostIrTemplateUploadThirdParty()
        {
            var success = false;
            var message = "Uploaded successfully";
            var dateTimeUpdated = DateTime.Now;
            var files = Request.Form.Files;
            var domainName = Request.Form.ContainsKey("domain") ? Request.Form["domain"].ToString() : "Unknown";
            var domainId = Request.Form.ContainsKey("domainId") ? Request.Form["domainId"].ToString() : "Unknown";
            if (files.Count == 1 && domainId!=string.Empty)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (Path.GetExtension(file.FileName) != ".pdf")
                            throw new ArgumentException("Unsupported file type");

                        var fileName = domainId == "0" ? "IR_Form_Template.pdf" : "IR_Form_Template_"+domainName.Trim()+".pdf";
                        var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Template");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir, fileName)))
                        {
                            file.CopyTo(stream);
                        }
                         
                        _configDataProvider.SaveDefaultEmailThirdPartyDomains(string.Empty,int.Parse(domainId), fileName);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            // Fetch updated template details safely
            var templateDetails = _userDataProvider.GetThirdPartyDomainOrTemplateDetails()?
                                  .FirstOrDefault(x => x.DomainId == int.Parse(domainId));

            // If no record found, return default values
            return new JsonResult(new
            {
                success,
                message,
                dateTimeUpdated = templateDetails?.LastUpdated != null
                                  ? templateDetails.LastUpdated.ToString("dd MMM yyyy @ HH:mm")
                                  : "",
                defaultEmail = templateDetails?.DefaultEmail ?? "",
                filename = templateDetails?.FileName ?? ""
            });
        }
        //To get the default Email Path start



        public JsonResult OnPostDefaultEmailUpdate(string defaultMailEdit)
        {

            var success = false;
            var message = "Updated successfully";

            try
            {
                _configDataProvider.SaveDefaultEmail(defaultMailEdit);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }



            return new JsonResult(new { success, message });
        }
        //To get the default Email Path stop


        public JsonResult OnPostDefaultEmailUpdateThirdPartyDomains(int domainId, string domain, string DefaultEmail)
        {
            var success = false;
            var message = "Updated successfully";

            try
            {
                // Save the email update
                _configDataProvider.SaveDefaultEmailThirdPartyDomains(DefaultEmail, domainId, string.Empty);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            // Fetch updated template details safely
            var templateDetails = _userDataProvider.GetThirdPartyDomainOrTemplateDetails()?
                                  .FirstOrDefault(x => x.DomainId == domainId);

            // If no record found, return default values
            return new JsonResult(new
            {
                success,
                message,
                dateTimeUpdated = templateDetails?.LastUpdated != null
                                  ? templateDetails.LastUpdated.ToString("dd MMM yyyy @ HH:mm")
                                  : "",
                defaultEmail = templateDetails?.DefaultEmail ?? "",
                filename = templateDetails?.FileName ?? ""
            });
        }

        public JsonResult OnGetStaffDocs()
        {
            return new JsonResult(_configDataProvider.GetStaffDocuments());
        }
        public JsonResult OnGetStaffDocsUsingType(int type, string query)
        {
            return new JsonResult(_configDataProvider.GetStaffDocumentsUsingType(type, query));
        }
        
        public JsonResult OnPostUploadStaffDoc()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.docx,.xlsx".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");

                        var staffDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs");
                        if (!Directory.Exists(staffDocsFolder))
                            Directory.CreateDirectory(staffDocsFolder);

                        using (var stream = System.IO.File.Create(Path.Combine(staffDocsFolder, file.FileName)))
                        {
                            file.CopyTo(stream);
                        }

                        var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                        _configDataProvider.SaveStaffDocument(new StaffDocument()
                        {
                            Id = documentId,
                            FileName = file.FileName,
                            LastUpdated = DateTime.Now
                        });

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostUploadStaffDocUsingType()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.docx,.xlsx".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");

                        var staffDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs");
                        if (!Directory.Exists(staffDocsFolder))
                            Directory.CreateDirectory(staffDocsFolder);

                        using (var stream = System.IO.File.Create(Path.Combine(staffDocsFolder, file.FileName)))
                        {
                            file.CopyTo(stream);
                        }

                        var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                        var type = Convert.ToInt32(Request.Form["type"]);
                        _configDataProvider.SaveStaffDocument(new StaffDocument()
                        {
                            Id = documentId,
                            FileName = file.FileName,
                            LastUpdated = DateTime.Now,
                            DocumentType = type

                        });

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteStaffDoc(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                var document = _configDataProvider.GetStaffDocuments().SingleOrDefault(x => x.Id == id);
                if (document != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs", document.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);

                    _configDataProvider.DeleteStaffDocument(id);
                }


            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnGetUsers(string searchTerm)
        {
            var users = _userDataProvider.GetUsers()
             .Where(x => string.IsNullOrEmpty(searchTerm) || x.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
             .Select(x => new { x.Id, x.UserName, x.IsDeleted, x.LastLoginDate, x.LastLoginIPAdress, x.FormattedLastLoginDate });
            return new JsonResult(users);
        }


        public JsonResult OnGetUserLoginHistory(int userId)
        {
            var users = _userDataProvider.GetUserLoginHistory(userId);
            return new JsonResult(users);
        }

        public JsonResult OnPostUser(Data.Models.User record)
        {
            var status = true;
            var message = "Success";
            try
            {
                if (record != null)
                {
                    record.Password = PasswordHelper.EncryptPassword(record.Password);
                    _userDataProvider.SaveUser(record);
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;

                if (ex.InnerException != null &&
                    ex.InnerException is SqlException &&
                    ex.InnerException.Message.StartsWith("Violation of UNIQUE KEY constraint"))
                {
                    message = "A user with this username already exists";
                }
            }

            return new JsonResult(new { status = status, message = message });
        }


        public JsonResult OnPostLinksPageType(ClientSiteLinksPageType ClientSiteLinksPageTyperecord)
        {
            var status = 0;
            var message = "Success";
            try
            {
                if (ClientSiteLinksPageTyperecord != null)
                {

                    status = _clientDataProvider.SaveClientSiteLinksPageType(ClientSiteLinksPageTyperecord);
                    if (status == -1)
                    {

                        message = "Same button name already exist";


                    }
                }
            }
            catch (Exception ex)
            {
                status = 0;
                message = "Error " + ex.Message;


            }

            return new JsonResult(new { status = status, message = message });
        }
        //to add new feedback type -start
        public JsonResult OnPostFeedBackType(FeedbackType FeedbackNewTyperecord)
        {
            var status = 0;
            var message = "Success";
            try
            {
                if (FeedbackNewTyperecord != null)
                {

                    status = _clientDataProvider.SaveFeedbackType(FeedbackNewTyperecord);
                    if (status == -1)
                    {

                        message = "Same Category name already exist";


                    }
                }
            }
            catch (Exception ex)
            {
                status = 0;
                message = "Error " + ex.Message;


            }

            return new JsonResult(new { status = status, message = message });
        }
        //to add new feedback type -end
        public JsonResult OnPostDeletePageType(int TypeId)
        {
            var status = 0;
            var message = "Success";
            try
            {
                if (TypeId != 0)
                {

                    status = _clientDataProvider.DeleteClientSiteLinksPageType(TypeId);

                }
            }
            catch (Exception ex)
            {
                status = 0;
                message = "Error " + ex.Message;


            }

            return new JsonResult(new { status = status, message = message });
        }
        //to delete existing feedback type -start
        public JsonResult OnPostDeleteFeedBackType(int TypeId)
        {
            var status = 0;
            var message = "Success";
            try
            {
                if (TypeId != 0)
                {

                    status = _clientDataProvider.DeleteFeedBackType(TypeId);

                }
            }
            catch (Exception ex)
            {
                status = 0;
                message = "Error " + ex.Message;


            }

            return new JsonResult(new { status = status, message = message });
        }
        //to delete existing feedback type -end
        public IActionResult OnGetLinksPageTypeList()
        {
            return new JsonResult(_clientDataProvider.GetSiteLinksPageTypes());
        }
        //to get existing feedback type -start
        public IActionResult OnGetFeedBackTypeList()
        {
            return new JsonResult(_configDataProvider.GetFeedbackTypes());
        }
        //to get existing feedback type -end
        public JsonResult OnGetLinksPageDetails(int typeId)
        {
            var fields = _clientDataProvider.GetSiteLinksPageDetails(typeId);
            return new JsonResult(fields);
        }

        public JsonResult OnPostLinksPageDetails(ClientSiteLinksDetails reportfield)
        {
            var status = true;
            var message = "Success";
            var success = 1;
            try
            {

                if (reportfield.typeId != 0 && reportfield.ClientSiteLinksTypeId == 0)
                {
                    reportfield.ClientSiteLinksTypeId = reportfield.typeId;
                }
                else if (reportfield.typeId == 0 && reportfield.ClientSiteLinksTypeId != 0)
                {
                    reportfield.typeId = reportfield.ClientSiteLinksTypeId;

                }

                if (reportfield.ClientSiteLinksTypeId != 0)
                    success = _clientDataProvider.SaveSiteLinkDetails(reportfield);
                if (success != 1)
                {
                    if (success == 2)
                        message = "The title you have entered is already exists for this button. Please Use different Title or button.";
                    else if (success == 3)
                        message = "The title you have entered is already exists for this button. Please Use different Title or button.";
                    status = false;
                }


            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }


        public JsonResult OnPostDeleteLinksPageDetails(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteSiteLinkDetails(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetLinkDetailsUisngTypeandState(int type)
        {
            return new JsonResult(_clientDataProvider.GetSiteLinkDetailsUsingTypeAndState(type));
        }

        public JsonResult OnGetUserClientAccess(string searchTerm)
        {
            return new JsonResult(_viewDataService.GetAllUsersClientSiteAccess(searchTerm));
        }

        public JsonResult OnGetClientAccessByUserId(int userId)
        {
            return new JsonResult(_viewDataService.GetUserClientSiteAccess(userId));
        }
        public JsonResult OnGetClientAccessThirdParty(int userId)
        {
            var sss = _viewDataService.GetUserClientSiteAccessNew(userId);
            return new JsonResult(_viewDataService.GetUserClientSiteAccessNew(userId));
        }
        public JsonResult OnGetHrSettingsLockedClientSites(int hrSttingsId)
        {
            return new JsonResult(_viewDataService.GetHrSettingsClientSiteLockStatus(hrSttingsId));
        }

        public JsonResult OnPostClientAccessByUserId(int userId, int[] selectedSites,int ClientTypeId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var clientSiteAccess = selectedSites.Select(x => new UserClientSiteAccess()
                {
                    ClientSiteId = x,
                    UserId = userId,
                    ThirdPartyID= ClientTypeId
                }).ToList();
                _userDataProvider.SaveUserClientSiteAccess(userId, clientSiteAccess, ClientTypeId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostHrSettingsLockedClientSites(int hrSttingsId, int[] selectedSites,int enableStatus)
        {
            var status = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.UpdateHRLockSettings(hrSttingsId,Convert.ToBoolean(enableStatus));
                var clientSiteAccess = selectedSites.Select(x => new HrSettingsLockedClientSites()
                {
                    ClientSiteId = x,
                    HrSettingsId = hrSttingsId
                }).ToList();
                _userDataProvider.SaveHrSettingsLockedClientSites(hrSttingsId, clientSiteAccess);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostHrSettingsBanEdit(int hrSttingsId, int enableStatus)
        {
            var status = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.UpdateHRBanSettings(hrSttingsId, Convert.ToBoolean(enableStatus));
               
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnGetReportFields(int typeId)
        {
            var fields = _configDataProvider.GetReportFieldsByType((ReportFieldType)typeId);

            return new JsonResult(fields);
        }

        public JsonResult OnPostDeleteReportField(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _configDataProvider.DeleteReportField(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostReportField(IncidentReportField reportfield)
        {
            var status = true;
            var message = "Success";
            try
            {
                _configDataProvider.SaveReportField(reportfield);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        //code added for PSPF sub datas start
        public JsonResult OnGetLastNo()
        {
            return new JsonResult(_configDataProvider.GetLastValue());
        }
        public JsonResult OnGetPSPF()
        {
            return new JsonResult(_configDataProvider.GetPSPF());
        }

        public JsonResult OnPostSavePSPF(IncidentReportPSPF record)
        {
            int CountPSPF = _configDataProvider.GetLastValue();
            if (record.IsDefault == true && CountPSPF >= 1)
            {
                _configDataProvider.UpdateDefault();
            }
            var PsPFName = _configDataProvider.GetPSPFName(record.Name);

            if (record.Id == -1)
            {

                int LastOne = _configDataProvider.GetLastValue();
                if (LastOne != null)
                {
                    LastOne++;
                    string numberAsString = LastOne.ToString();
                    if (numberAsString.Length == 1)
                    {

                        record.ReferenceNo = "0" + LastOne;
                    }
                    else
                    {
                        record.ReferenceNo = LastOne.ToString();
                    }


                }
            }

            var success = false;
            var message = string.Empty;
            try
            {
                if (PsPFName == record.Name && record.Id == -1)
                {

                    success = false;
                }
                else
                {

                    _configDataProvider.SavePSPF(record);
                    success = true;
                }

            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //To save the IR Emaill CC start
        public JsonResult OnPostSaveIREmail(string Email)
        {
            var status = true;
            var message = "Success";
            try
            {
                int maxId = _configDataProvider.OnGetMaxIdIR();
                var info = new IncidentReportField { Id = maxId, Name = Email, TypeId = ReportFieldType.Reimburse, EmailTo = "" };
                _configDataProvider.SaveReportField(info);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });

        }

        //To save the IR Emaill CC End
        public JsonResult OnPostDeletePSPF(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _configDataProvider.DeletePSPF(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });

        }
        //code added for PSPF sub datas stop
        public JsonResult OnGetPositions()
        {
            return new JsonResult(_configDataProvider.GetPositions());
        }

        public JsonResult OnPostSavePositions(IncidentReportPosition record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _configDataProvider.SavePostion(record);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeletePosition(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _configDataProvider.DeletePosition(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });

        }
        public IActionResult OnGetCoreSettings(int companyId)
        {
            var template = _viewDataService.GetAllCoreSettings(companyId);
            return new JsonResult(template);
        }
        public JsonResult OnGetIREmailCCForReimbursements()
        {
            var fields = _configDataProvider.GetReportFieldsByType(ReportFieldType.Reimburse);
            return new JsonResult(fields);
        }


        public JsonResult OnPostCrPrimaryLogoUpload()
        {
            var success = false;
            var message = "Uploaded successfully";
            var dateTimeUpdated = DateTime.Now;
            var files = Request.Form.Files;
            var filepath = "";
            if (files.Count == 1)
            {
                var file = files[0];



                if (file.Length > 0)
                {
                    try
                    {
                        if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".png" && Path.GetExtension(file.FileName) != ".PNG" && Path.GetExtension(file.FileName) != ".GIF" && Path.GetExtension(file.FileName) != ".gif")
                            throw new ArgumentException("Unsupported file type");

                        var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
                        filepath = Path.Combine(reportRootDir, "cr_primarylogo.JPG");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir, "cr_primarylogo.JPG")))
                        {
                            file.CopyTo(stream);
                        }

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            return new JsonResult(new { success, message, dateTimeUpdated = dateTimeUpdated.ToString("dd MMM yyyy @ HH:mm"), filepath });
        }
        public JsonResult OnPostCrBinaryLogoUpload()
        {
            var success = false;
            var message = "Uploaded successfully";
            var dateTimeUpdated = DateTime.Now;
            var files = Request.Form.Files;
            var filepath = "";
            if (files.Count == 1)
            {
                var file = files[0];



                if (file.Length > 0)
                {
                    try
                    {
                        if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".png" && Path.GetExtension(file.FileName) != ".PNG" && Path.GetExtension(file.FileName) != ".gif" && Path.GetExtension(file.FileName) != ".GIF")
                            throw new ArgumentException("Unsupported file type");

                        var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
                        filepath = Path.Combine(reportRootDir, "cr_bannerlogo.JPG");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir, "cr_bannerlogo.JPG")))
                        {
                            file.CopyTo(stream);
                        }

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            return new JsonResult(new { success, message, dateTimeUpdated = dateTimeUpdated.ToString("dd MMM yyyy @ HH:mm"), filepath });
        }

        public JsonResult OnPostCompanyDetails(Data.Models.CompanyDetails company)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.SaveCompanyDetails(company);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostCompanyMailDetails(Data.Models.CompanyDetails company)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.SaveCompanyMailDetails(company);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        //for adding a report logo-start
        public JsonResult OnPostCrReportLogoUpload()
        {
            var success = false;
            var message = "Uploaded successfully";
            var dateTimeUpdated = DateTime.Now;
            var files = Request.Form.Files;
            var filepath = "";
            var filepath2 = "";
            if (files.Count == 1)
            {
                var file = files[0];



                if (file.Length > 0)
                {
                    try
                    {
                        if (Path.GetExtension(file.FileName) != ".JPG" && Path.GetExtension(file.FileName) != ".jpg" && Path.GetExtension(file.FileName) != ".JPEG" && Path.GetExtension(file.FileName) != ".jpeg" && Path.GetExtension(file.FileName) != ".png" && Path.GetExtension(file.FileName) != ".PNG" && Path.GetExtension(file.FileName) != ".GIF" && Path.GetExtension(file.FileName) != ".gif")
                            throw new ArgumentException("Unsupported file type");

                        var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
                        filepath = Path.Combine(reportRootDir, "CWSLogoPdf.png");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir, "CWSLogoPdf.png")))
                        {
                            file.CopyTo(stream);
                        }
                        //string kpipath = _webHostEnvironment.WebRootPath;
                        //kpipath=kpipath.Replace("CityWatch.Web", "CityWatch.Kpi");
                        string kpipath = "C:\\c4isystem\\Websites\\kpi\\prod-citywatch\\wwwroot";
                        var reportRootDir2 = Path.Combine(kpipath, "Images");
                        filepath2 = Path.Combine(reportRootDir2, "CWSLogoPdf.png");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir2, "CWSLogoPdf.png")))
                        {
                            file.CopyTo(stream);
                        }
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            return new JsonResult(new { success, message, dateTimeUpdated = dateTimeUpdated.ToString("dd MMM yyyy @ HH:mm"), filepath });
        }
        //for adding a report logo-end

        public JsonResult OnGetClientSitesNew1(string typeId)
        {
            if (typeId != null)
            {
                string[] typeId2 = typeId.Split(';');
                int[] typeId3 = new int[typeId2.Length];
                int i = 0;
                foreach (var item in typeId2)
                {

                    typeId3[i] = Convert.ToInt32(item);
                    i++;


                }

                return new JsonResult(_guardLogDataProvider.GetAllClientSites().Where(x => typeId == null || typeId3.Contains(x.TypeId)).OrderBy(z => z.Name).ThenBy(z => z.TypeId));
            }
            return new JsonResult(_guardLogDataProvider.GetAllClientSites().Where(x => x.TypeId == 0).OrderBy(z => z.Name).ThenBy(z => z.TypeId));
        }
        //p1 - 202 site allocation-start
        public JsonResult OnGetAreaReportFields(int typeId)
        {
            var fields = _configDataProvider.GetReportFieldsByType((ReportFieldType)typeId);

            foreach (var item in fields)
            {
                if (item.ClientSiteIds != null)
                {
                    var values = item.ClientSiteIds.Split(';');
                    int[] ids = new int[values.Length];
                    for (int i = 0; i < values.Length; i++)
                    {
                        ids[i] = Convert.ToInt32(values[i]);

                    }
                    string clientname = string.Empty;
                    var clientdetails = _clientDataProvider.GetClientSites(null).Where(x => ids.Contains(x.Id)).ToList();
                    foreach (var det in clientdetails)
                    {
                        if (clientname != "")
                        {
                            clientname = clientname + "," + det.Name;
                        }
                        else
                        {
                            clientname = det.Name;
                        }
                    }
                    item.clientSites = clientname;

                }
                if (item.ClientTypeIds != null)
                {
                    var values = item.ClientTypeIds.Split(';');
                    int[] ids = new int[values.Length];
                    for (int i = 0; i < values.Length; i++)
                    {
                        ids[i] = Convert.ToInt32(values[i]);

                    }
                    string clienttypename = string.Empty;
                    var clientdetails = _clientDataProvider.GetClientTypes().Where(x => ids.Contains(x.Id)).ToList();
                    foreach (var det in clientdetails)
                    {
                        if (clienttypename != "")
                        {
                            clienttypename = clienttypename + "," + det.Name;
                        }
                        else
                        {
                            clienttypename = det.Name;
                        }
                    }
                    item.clientTypes = clienttypename;

                }
            }

            return new JsonResult(fields);
        }
        public JsonResult OnGetClientSitesWithTypeId(string types)
        {
            if (!String.IsNullOrEmpty(types))
            {
                var values = types.Split(';');
                int[] ids = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    ids[i] = Convert.ToInt32(values[i]);

                }
                return new JsonResult(_clientDataProvider.GetClientSitesWithTypeId(ids).OrderBy(z => z.Name));
            }
            int[] idsn = new int[1];
            idsn[0] = 0;
            return new JsonResult(_clientDataProvider.GetClientSitesWithTypeId(idsn).OrderBy(z => z.Name));
        }
        //p1 - 202 site allocation-end
        //p1-213 Critical documents start

        public IActionResult OnGetClientSitesDoc(string type)
        {
            int GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                return new JsonResult(_viewDataService.GetClientSites(type));
            }
            else
            {
                return new JsonResult(_configDataProvider.GetClientSitesUsingLoginUserId(GuardId, type));
            }



        }
        public IActionResult OnGetDescriptionList(int HRGroupId)
        {
            return new JsonResult(_configDataProvider.GetDescList(HRGroupId));
        }
        public JsonResult OnPostSaveCriticalDocuments(CriticalDocumentViewModel CriticalDocModel)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(CriticalDocModel, new ValidationContext(CriticalDocModel), results, true))
                return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            var success = true;
            var message = "Saved successfully";
            try
            {
                var CriticalDoc = CriticalDocumentViewModel.ToDataModel(CriticalDocModel);
                _configDataProvider.SaveCriticalDoc(CriticalDoc, true);
                if (CriticalDocModel.IsCriticalDocumentDownselect == false)
                {
                    _configDataProvider.RemoveCriticalDownSelect(CriticalDoc);
                }
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetCriticalDocumentList()
        {
            int GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                var crdoclist = _configDataProvider.GetCriticalDocs();
                var crdoclist2 = crdoclist.Select(z => CriticalDocumentViewModel.FromDataModelForDisplay(z));
                return new JsonResult(crdoclist2);


            }
            else
            {
                return new JsonResult(_configDataProvider.GetCriticalDocs()
                   .Select(z => CriticalDocumentViewModel.FromDataModelForDisplay(z)));
                //return new JsonResult(_kpiSchedulesDataProvider.GetAllSendSchedulesUisngGuardId(GuardId)
                //   .Select(z => KpiSendScheduleViewModel.FromDataModel(z))
                //   .Where(z => z.CoverSheetType == (CoverSheetType)type && (string.IsNullOrEmpty(searchTerm) || z.ClientSites.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1))
                //   .OrderBy(x => x.ProjectName)
                //   .ThenBy(x => x.ClientTypes));

            }
        }
        public JsonResult OnGetCriticalDocList(int id)
        {
            int GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                var document = _configDataProvider.GetCriticalDocById(id);

                if (document == null)
                {
                    return new JsonResult(null);
                }

                var documentDto = new CriticalDocuments
                {
                    Id = document.Id,
                    ClientTypeId = document.ClientTypeId,
                    HRGroupID = document.HRGroupID,
                    GroupName = document.GroupName,
                    IsCriticalDocumentDownselect = document.IsCriticalDocumentDownselect,
                    CriticalDocumentsClientSites = document.CriticalDocumentsClientSites.Select(cs => new CriticalDocumentsClientSites
                    {
                        Id = cs.Id,
                        ClientSiteId = cs.ClientSiteId,
                        ClientSite = new ClientSite
                        {
                            Id = cs.ClientSite.Id,
                            Name = cs.ClientSite.Name,
                            //ClientTypeId = cs.ClientSite.ClientTypeId,

                        }
                    }).ToList(),
                    CriticalDocumentDescriptions = document.CriticalDocumentDescriptions.Select(desc => new CriticalDocumentDescriptions
                    {
                        Id = desc.Id,
                        DescriptionID = desc.DescriptionID,
                        HRSettings = desc.HRSettings == null ? null : new HrSettings
                        {
                            Id = desc.HRSettings.Id,
                            Description = desc.HRSettings.Description,
                            ReferenceNoNumbers = desc.HRSettings.ReferenceNoNumbers == null ? null : new ReferenceNoNumbers
                            {
                                Id = desc.HRSettings.ReferenceNoNumbers.Id,
                                Name = desc.HRSettings.ReferenceNoNumbers.Name
                            },
                            ReferenceNoAlphabets = desc.HRSettings.ReferenceNoAlphabets == null ? null : new ReferenceNoAlphabets
                            {
                                Id = desc.HRSettings.ReferenceNoAlphabets.Id,
                                Name = desc.HRSettings.ReferenceNoAlphabets.Name
                            },
                            HRGroups = desc.HRSettings.HRGroups == null ? null : new HRGroups
                            {
                                Id = desc.HRSettings.HRGroups.Id,
                                Name = desc.HRSettings.HRGroups.Name,
                                IsDeleted = desc.HRSettings.HRGroups.IsDeleted

                            }
                        }
                    }).ToList()
                };

                return new JsonResult(documentDto);
            }
            else
            {
                return new JsonResult(_configDataProvider.GetCriticalDocByIdandGuardId(id, GuardId));
            }
        }
        public JsonResult OnPostDeleteCriticalDoc(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _configDataProvider.DeleteCriticalDoc(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }
        //p1-213 Critical documents stop
        public JsonResult OnPostSaveGlobalComplianceAlertEmail(string Email)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.GlobalComplianceAlertEmail(Email);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = Email, message = message });
        }
        public JsonResult OnPostSaveDropboxDir(string DroboxDir)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DroboxDir(DroboxDir);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = DroboxDir, message = message });
        }
        public JsonResult OnPostSaveTimesheet(string weekname, string frequency, string mailid, string dropbox)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.TimesheetSave(weekname, frequency, mailid, dropbox);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = weekname, message = message });
        }
        public JsonResult OnGetSettingsDetails()
        {
            var Email = _clientDataProvider.GetEmail();
            var DropboxDir = _clientDataProvider.GetDropboxDir();
            return new JsonResult(new { Email = Email.Email, DropboxDir = DropboxDir.DropboxDir });
        }
        public JsonResult OnGetTimesheetDetails()
        {
            var Timesheet = _clientDataProvider.GetTimesheetDetails();
            if (Timesheet != null)
            {
                return new JsonResult(new { Week = Timesheet.weekName, Time = Timesheet.Frequency, mailid = Timesheet.Email, Dropbox = Timesheet.Dropbox });
            }
            else
            {
                return new JsonResult(new { Week = "", Time = "", mailid = "", Dropbox = "" });
            }

        }
       
        // To download Timesheet-Task 212
        //public IActionResult OnGetDownloadTimesheet(string startdate, string endDate, string frequency, int guradid)
        //{
        //    int siteid = 465;

        //    DateTime Start = DateTime.Parse(startdate);
        //    DateTime end = DateTime.Parse(endDate);
        //    var fileName = _TimesheetReportGenerator.GeneratePdfTimesheetReport(siteid);
        //    //var fileName = _ReportGenerator.GeneratePdfTimesheetReport(siteid);

        //    return File("application/pdf", fileName + ".pdf");
        //}
        public async Task<JsonResult> OnPostDownloadTimesheet(string startdate, string endDate, string frequency, int guradid)
        {

            var fileName = string.Empty;
            var statusCode = 0;
            int id = 1;
            try
            {

                fileName = _TimesheetReportGenerator.GeneratePdfTimesheetReportCustom(startdate, endDate, guradid);





            }
            catch (Exception ex)
            {

            }

            if (string.IsNullOrEmpty(fileName))
                return new JsonResult(new { fileName, message = "Failed to generate pdf", statusCode = -1 });




            return new JsonResult(new { fileName = @Url.Content($"~/Pdf/Output/{fileName}"), statusCode });
        }

        public async Task<JsonResult> OnPostDownloadTimesheetFrequency(string frequency, int guradid)
        {

            var fileName = string.Empty;
            var statusCode = 0;
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.MinValue;
            try
            {
                DateTime today = DateTime.Today;

                if (frequency == "ThisWeek")
                {

                    // Assuming the week starts on Monday and ends on Sunday
                    int daysToSubtract = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
                    startDate = today.AddDays(-daysToSubtract);

                    endDate = startDate.AddDays(6);
                }
                else if (frequency == "LastMonth")
                {
                    // Calculate the start date as the first day of the last month
                    startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);

                    // Calculate the end date as the last day of the last month
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    
                }
                else if (frequency == "Last2weeks")
                {
                    endDate = today;

                    startDate = endDate.AddDays(-13);
                }
                else if (frequency == "Last4weeks")
                {
                    endDate = today;

                    startDate = endDate.AddDays(-27);
                }
                else if (frequency == "Month")
                {
                    startDate = new DateTime(today.Year, today.Month, 1);


                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                else if (frequency == "Today")
                {
                    startDate = today;
                    endDate = today;
                }
                string StartDate = startDate.ToString();
                string EndDate = endDate.ToString();
                fileName = _TimesheetReportGenerator.GeneratePdfTimesheetReport(StartDate, EndDate, guradid);


            }
            catch (Exception ex)
            {

            }

            if (string.IsNullOrEmpty(fileName))
                return new JsonResult(new { fileName, message = "Failed to generate pdf", statusCode = -1 });




            return new JsonResult(new { fileName = @Url.Content($"~/Pdf/Output/{fileName}"), statusCode });
        }


        public JsonResult OnGetHelpDocValues()
        {
            /* list box for helpdoc module select */
            List<helpDocttype> helpDoctypeList = new List<helpDocttype>();
            helpDocttype objEmpty = new helpDocttype { Id = string.Empty, Name = string.Empty };
            helpDocttype objLB = new helpDocttype { Id = "LB", Name = "LB" };
            helpDocttype objKV = new helpDocttype { Id = "KV", Name = "KV" };
            helpDocttype objIR = new helpDocttype { Id = "IR", Name = "IR" };
            helpDocttype objSW = new helpDocttype { Id = "SW", Name = "SW" };
            helpDocttype objKPI = new helpDocttype { Id = "KPI", Name = "KPI" };
            helpDocttype objHR = new helpDocttype { Id = "HR", Name = "HR" };
            helpDocttype objRC = new helpDocttype { Id = "RC", Name = "RC" };
            helpDoctypeList.Add(objEmpty);
            helpDoctypeList.Add(objLB);
            helpDoctypeList.Add(objKV);
            helpDoctypeList.Add(objIR);
            helpDoctypeList.Add(objSW);
            helpDoctypeList.Add(objKPI);
            helpDoctypeList.Add(objHR);
            helpDoctypeList.Add(objRC);
            return new JsonResult(helpDoctypeList);
        }
        public JsonResult OnPostUpdateDocumentModuleType(StaffDocument record)
        {
            var status = true;
            var message = "Success";
            _configDataProvider.UpdateStaffDocumentModuleType(new StaffDocument()
            {
                Id = record.Id,
                LastUpdated = record.LastUpdated,
                DocumentModuleName = record.DocumentModuleName


            });
            return new JsonResult(new { status = status, message = message });
        }


        #region SOPClientSite



        public IActionResult OnGetClientSitesSOPClientSite(string type)
        {

            return new JsonResult(_viewDataService.GetClientSites(type));




        }

        public JsonResult OnGetClientSitesNew(int? page, int? limit, int? typeId, string searchTerm, string searchTermtwo)
        {

            return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, null, searchTerm, searchTermtwo));


        }

        public JsonResult OnGetSOPClientSitebyId(int id)
        {


            return new JsonResult(_clientDataProvider.GetStaffDocById(id));

        }

        public JsonResult OnPostDeleteSOPClientSite(int id)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.DeleteRCLinkedDuress(id);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }


        public JsonResult OnPostUploadStaffDocUsingTypeFour()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.docx,.xlsx".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");

                        var staffDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs");
                        if (!Directory.Exists(staffDocsFolder))
                            Directory.CreateDirectory(staffDocsFolder);
                        using (var stream = System.IO.File.Create(Path.Combine(staffDocsFolder, file.FileName)))
                        {
                            file.CopyTo(stream);
                        }

                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }




            var SOP = Request.Form["sop"];
            var ClientSite = int.Parse(Request.Form["site"]);
            var fileName = Request.Form["filename"];
            if (ClientSite != 0)
            {
                var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                var type = 4;

                _configDataProvider.SaveStaffDocument(new StaffDocument()
                {
                    Id = documentId,
                    FileName = fileName,
                    LastUpdated = DateTime.Now,
                    DocumentType = type,
                    SOP = SOP,
                    ClientSite = ClientSite

                });

                success = true;
            }
            else
            {
                throw new ArgumentException("Select the site and SOP");
            }


            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostUploadStaffDocUsingTypeSix()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.docx,.xlsx".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");

                        var staffDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "StaffDocs");
                        if (!Directory.Exists(staffDocsFolder))
                            Directory.CreateDirectory(staffDocsFolder);

                       

                        // Generate URL to the StaffDocs folder
                       

                        using (var stream = System.IO.File.Create(Path.Combine(staffDocsFolder, file.FileName)))
                        {
                            file.CopyTo(stream);
                        }

                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }




            var SOP = Request.Form["sop"];
            var ClientSite = int.Parse(Request.Form["site"]);
            var fileName = Request.Form["filename"];
            if (ClientSite != 0)
            {
                var staffDocsUrl = $"{Request.Scheme}://{Request.Host}/StaffDocs/";
                var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                var type = 6;

                _configDataProvider.SaveStaffDocument(new StaffDocument()
                {
                    Id = documentId,
                    FileName = fileName,
                    LastUpdated = DateTime.Now,
                    DocumentType = type,
                    SOP = SOP,
                    ClientSite = ClientSite,
                    FilePath= staffDocsUrl
                });

                success = true;
            }
            else
            {
                throw new ArgumentException("Select the site and SOP");
            }


            return new JsonResult(new { success, message });
        }


        #endregion


        #region domain

        public JsonResult OnGetDomainDetails(int typeId)
        {
            var success = false;
            var result = _userDataProvider.GetDomainDeatils(typeId);
            if (result != null)
            {
                success = true;
            }
            return new JsonResult(new { success, result });

        }

        public JsonResult OnPostClientSiteTypeDomainSettings()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            var newFileName = string.Empty;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        // Check for valid image extensions
                        if (".jpg,.png,.jpeg,.gif".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");

                        // Get the folder path where images will be saved
                        var staffDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "SubdomainLogo");
                        if (!Directory.Exists(staffDocsFolder))
                            Directory.CreateDirectory(staffDocsFolder);

                        // Get the file extension
                        var fileExtension = Path.GetExtension(file.FileName);

                        // Get the original file name without the extension
                        var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);

                        // Add the last 6 digits of the current UTC ticks to the file name
                        newFileName = $"{originalFileName}_{DateTime.UtcNow.Ticks.ToString().Substring(DateTime.UtcNow.Ticks.ToString().Length - 6)}{fileExtension}";

                        // Create the full path with the new file name
                        var filePath = Path.Combine(staffDocsFolder, newFileName);

                        // Save the file
                        using (var stream = System.IO.File.Create(filePath))
                        {
                            file.CopyTo(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }

            var domainName = Request.Form["domainName"];
            var siteTypeId = int.Parse(Request.Form["siteTypeId"]);
            var checkDomainStatus = Convert.ToBoolean(Request.Form["checkDomainStatus"]);
            if (newFileName == string.Empty)
            {
               newFileName = Request.Form["filename"];

            }
            var domainId = int.Parse(Request.Form["domainId"]);
            if (siteTypeId != 0)
            {


             var status=_configDataProvider.SaveSubDomain(new SubDomain()
                {
                    Id = domainId,
                    Domain = domainName,
                    TypeId = siteTypeId,
                    Enabled = checkDomainStatus,
                    Logo = newFileName


                });
                if (status == 1)
                {
                    success = true;
                }
                else
                {
                    success = false;
                    message = "Domain Name '"+domainName+"' already exist.";

                }
            }
            else
            {
                throw new ArgumentException("Select the site and SOP");
            }


            return new JsonResult(new { success, message });
        }
        #endregion
        public IActionResult OnGetClientSiteLastIncidentReportHistory(int guardId)
        {

            var clientIncidentReports = _guardLogDataProvider.GetActiveGuardIncidentReportHistoryForAdmin( guardId);

            return new JsonResult(clientIncidentReports);
        }

        public JsonResult OnGetLanguages()
        {
            return new JsonResult(_guardLogDataProvider.GetLanguages());
        }
        public JsonResult OnPostSavelanguages(LanguageMaster record)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                _guardLogDataProvider.SaveLanguages(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteLanguage(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                _guardLogDataProvider.DeleteLanguage(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetCourseDocsUsingSettingsId(int type)
        {
            return new JsonResult(_configDataProvider.GetCourseDocsUsingSettingsId(type));
        }
        public JsonResult OnGetTQNumbers()
        {
            return new JsonResult(_configDataProvider.GetTQNumbers());
        }

        [RequestSizeLimit(1073741824)] // 100 MB
        public JsonResult OnPostUploadCourseDocUsingHR()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.ppt,.pptx,.mp4".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");
                        var hrreferenceNumber = Request.Form["hrreferenceNumber"].ToString();
                        int hrsettingsid = Convert.ToInt32(Request.Form["hrsettingsid"]);
                        var CourseDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Course");
                        if (!Directory.Exists(CourseDocsFolder))
                            Directory.CreateDirectory(CourseDocsFolder);
                        if (System.IO.File.Exists(Path.Combine(CourseDocsFolder, file.FileName)))
                            //throw new ArgumentException("File Already Exists");
                            System.IO.File.Delete(Path.Combine(CourseDocsFolder, file.FileName));
                        using (var stream = System.IO.File.Create(Path.Combine(CourseDocsFolder, file.FileName)))
                        {
                            file.CopyTo(stream);
                        }
                        var DropboxDir = _guardDataProvider.GetDrobox();
                        //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardComplianceandlicense.Guard)}/{guardComplianceandlicense.FileName}");
                        var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{DropboxDir.DropboxDir}/TA/{hrreferenceNumber}/Course/{ file.FileName}");
                        var dbxUploaded = true;
                        dbxUploaded=UpoadDocumentToDropbox(Path.Combine(CourseDocsFolder, file.FileName),  dbxFilePath);
                    var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                        int TQNumbernew = Convert.ToInt32(Request.Form["tq-id"]);
                        if (TQNumbernew == 0)
                        {
                            int TQNumber = _configDataProvider.GetLastTQNumber(hrsettingsid);
                            if (TQNumber == 0)
                            {
                                throw new ArgumentException("TQ Number only contains from 01 to 10");
                            }
                            _configDataProvider.SaveTrainingCourses(new TrainingCourses()
                            {
                                Id = documentId,
                                FileName = file.FileName,
                                LastUpdated = DateTime.Now,
                                HRSettingsId = hrsettingsid,
                                TQNumberId = TQNumber

                            });
                        }
                        else
                        {
                            _configDataProvider.SaveTrainingCourses(new TrainingCourses()
                            {
                                Id = documentId,
                                FileName = file.FileName,
                                LastUpdated = DateTime.Now,
                                HRSettingsId = hrsettingsid,
                                TQNumberId = TQNumbernew

                            });
                        }

                        success = true;
                        if (".ppt,.pptx".IndexOf(Path.GetExtension(file.FileName).ToLower()) > 0)
                        {
                            Application pptApplication = new Application();
                            Presentation pptPresentation = null;

                            string inputPath = Path.Combine(CourseDocsFolder, file.FileName);
                            string outputPath = Path.ChangeExtension(Path.Combine(CourseDocsFolder, file.FileName), ".pdf");
                            if (System.IO.File.Exists(outputPath))
                                //throw new ArgumentException("File Already Exists");
                                System.IO.File.Delete(outputPath);

                            try
                            {
                                pptPresentation = pptApplication.Presentations.Open(inputPath, WithWindow: MsoTriState.msoFalse);
                                pptPresentation.SaveAs(outputPath, PpSaveAsFileType.ppSaveAsPDF);
                            }
                            finally
                            {
                                pptPresentation?.Close();
                                pptApplication.Quit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }
            return new JsonResult(new { success, message });
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
        public JsonResult OnPostDeleteCourseDocUsingHR(int id,string hrreferenceNumber)
        {
            var status = true;
            var message = "Success";
            try
            {
                var document = _configDataProvider.GetCourseDocuments().SingleOrDefault(x => x.Id == id);
                if (document != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber,"Course", document.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);
                    if (".ppt,.pptx".IndexOf(Path.GetExtension(document.FileName).ToLower()) > 0)
                    {
                        Application pptApplication = new Application();
                        Presentation pptPresentation = null;

                        string outputPath = Path.ChangeExtension(Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Course", document.FileName), ".pdf");
                        if (System.IO.File.Exists(outputPath))
                            System.IO.File.Delete(outputPath);
                    }
                        _configDataProvider.DeleteCourseDocument(id);
                }


            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnPostSaveTQSettings(TrainingTestQuestionSettings record)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                if(record.CertificateExpiryId==0)
                {
                    record.CertificateExpiryId = null;
                }
                _guardLogDataProvider.SaveTestQuestionSettings(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetTQSettings(int hrSettingsid)
        {
            return new JsonResult(_configDataProvider.GetTQSettings(hrSettingsid));
        }
        public JsonResult OnPostSaveTQAnswers(TrainingTestQuestions testquestions, List<TrainingTestQuestionsAnswers> testquestionanswers)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                
               int id= _guardLogDataProvider.SaveTestQuestions(testquestions);
                if(id!=0)
                {
                    foreach(var item in testquestionanswers)
                    {
                        item.TrainingTestQuestionsId = id;
                    }
                    _guardLogDataProvider.SaveTestQuestionsAnswers(id, testquestionanswers);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteTQAnswers(int Id)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                //int id = _guardLogDataProvider.SaveTestQuestions(testquestions);
                if (Id != 0)
                {
                   
                    _guardLogDataProvider.DeleteTestQuestions(Id);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetNextQuestionWithinSameTQNumber(int hrSettingsId,int tqNumberId)
        {
            return new JsonResult(_configDataProvider.GetNextQuestionWithinSameTQNumber(hrSettingsId, tqNumberId));
        }
        public JsonResult OnGetQuestionsCount(int hrSettingsId, int tqNumberId)
        {
            return new JsonResult(_configDataProvider.GetQuestionsCount(hrSettingsId, tqNumberId));
        }
        public JsonResult OnGetLastTQNumber(int hrSettingsId)
        {
            return new JsonResult(_configDataProvider.GetLastTQNumberFromQuestions(hrSettingsId));
        }
        public IActionResult OnGetQuestionWithQuestionNumber(int hrSettingsId, int tqNumberId, int questionumberId)
        {
            var Questions = _configDataProvider.GetTrainingQuestions(hrSettingsId, tqNumberId, questionumberId);


            return new JsonResult(Questions);
        }
        public IActionResult OnGetQuestionAndAnswersWithQuestionNumber(int questionId)
        {
            var Answers = _configDataProvider.GetTrainingQuestionsAnswers(questionId);
            
            return new JsonResult(Answers);
        }

        public JsonResult OnGetLastFeedbackQNumber(int hrSettingsId)
        {
            return new JsonResult(_configDataProvider.GetLastFeedbackQNumbers(hrSettingsId));
        }
        public JsonResult OnGetFeedbackQuestionsCount(int hrSettingsId)
        {
            return new JsonResult(_configDataProvider.GetFeedbackQuestionsCount(hrSettingsId));
        }
        public JsonResult OnPostSaveFeedbackQAnswers(TrainingTestFeedbackQuestions feedbackquestions, List<TrainingTestFeedbackQuestionsAnswers> feedbackquestionanswers)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                int id = _guardLogDataProvider.SaveFeedbackQuestions(feedbackquestions);
                if (id != 0)
                {
                    foreach (var item in feedbackquestionanswers)
                    {
                        item.TrainingTestFeedbackQuestionsId = id;
                    }
                    _guardLogDataProvider.SaveFeedbackQuestionsAnswers(id, feedbackquestionanswers);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteFeedbackQAnswers(int Id)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                //int id = _guardLogDataProvider.SaveTestQuestions(testquestions);
                if (Id != 0)
                {

                    _guardLogDataProvider.DeleteFeedbanckQuestions(Id);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public IActionResult OnGetFeedbackQuestionWithQuestionNumber(int hrSettingsId, int questionumberId)
        {
            var Questions = _configDataProvider.GetFeedbackQuestions(hrSettingsId, questionumberId);


            return new JsonResult(Questions);
        }
        public IActionResult OnGetFeedbackQuestionAndAnswersWithQuestionNumber(int questionId)
        {
            var Answers = _configDataProvider.GetTrainingFeedbackQuestionsAnswers(questionId);

            return new JsonResult(Answers);
        }
        public JsonResult OnPostUpdateDocumentTQNumber(int id,string name,TrainingCourses record)
        {
            var success = false;
            var message = "Updated successfully";
            
                    try
                    {

                int TQNumbernew = _configDataProvider.GetTQNumbers().Where(x => x.Name == name).FirstOrDefault().Id;

                        if (TQNumbernew != 0)
                        {
                           
                       


                    var courseswithSameTQNumber = _configDataProvider.GetTrainingCoursesWithHrSettingsId(record.HRSettingsId).Where(x => x.TQNumberId == TQNumbernew);
                    if (courseswithSameTQNumber.Count() == 0)
                    {
                        _configDataProvider.SaveTrainingCourses(new TrainingCourses()
                        {
                            Id = record.Id,
                            FileName = record.FileName,
                            LastUpdated = DateTime.Now,
                            HRSettingsId = record.HRSettingsId,
                            TQNumberId = TQNumbernew

                        });
                        success = true;
                    }
                    else
                    {
                        message = "Same TQ number is used for other courses";
                        success = false;
                    }
                }


                }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                
            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetInstructorAndPosition()
        {
            return new JsonResult(_guardLogDataProvider.GetTrainingInstructorNameandPositionFields());
        }
        
        public JsonResult OnGetCourseInstructor(int type)
        {
            return new JsonResult(_configDataProvider.GetCourseInstructor(type));
        }
        public JsonResult OnGetInstructorAndPositionWithId(string Id)
        {
            return new JsonResult(_guardLogDataProvider.GetTrainingInstructorNameandPositionFields().Where(x=>x.Id== Convert.ToInt32(Id)).FirstOrDefault());
        }
        public JsonResult OnPostSaveTrainingCourseInstructor(int id, int? instructorId, int hrsettingsId)
        {
            var success = false;
            var message = "Saved successfully";

            try
            {
               
                if (id == -1)
                {
                    id = 0;
                }

                    _configDataProvider.SaveTrainingCourseInstructor(new TrainingCourseInstructor()
                    {
                        Id = id,
                        TrainingInstructorId = instructorId,
                        HRSettingsId = hrsettingsId

                    });
                    success = true;

            }
            catch (Exception ex)
            {
                message = ex.Message;
            }


            return new JsonResult(new { success, message });
        }



        public JsonResult OnGetHrGroupsforCourseList()
        {
            return new JsonResult(_configDataProvider.GetHRGroupsDropDown());

        }
        //public JsonResult OnGetCourseList(int groupid)
        //{
        //    return new JsonResult(_configDataProvider.GetTrainingCoursesStatusWithOutcome(groupid));
        //}
        public JsonResult OnPostSaveGuardTrainingAndAssessmentTab(int HRSettingsId, int GuardId,int TrainingCourseStatusId)
        {
        
            var success = false;
            var message = string.Empty;
            try
            {
                var courseList = _configDataProvider.GetCourseDocuments().Where(x => x.HRSettingsId == HRSettingsId).ToList();
                foreach (var item in courseList)
                {
                    int TrainingCourseId = item.Id;

                    string description = _configDataProvider.GetCourseDocuments().Where(x => x.Id == TrainingCourseId).FirstOrDefault().FileName;
                    int hrsettingid = _configDataProvider.GetCourseDocuments().Where(x => x.Id == TrainingCourseId).FirstOrDefault().HRSettingsId;
                    int hrgroupid = _configDataProvider.GetHrSettingById(hrsettingid).HRGroupId;
                    var result = _guardDataProvider.GetGuardTrainingAndAssessment(GuardId).Where(x => x.TrainingCourseId == TrainingCourseId).ToList();
                    int id = 0;
                    if (result.Count > 0)
                    {
                        id = result.FirstOrDefault().Id;
                    }
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = id,
                        GuardId = GuardId,
                        TrainingCourseId = TrainingCourseId,
                        TrainingCourseStatusId = TrainingCourseStatusId,
                        Description = description,
                        HRGroupId = hrgroupid
                        //,
                        //IsCompleted = false

                    });
                }

                success = true;

            }
            catch (Exception ex)
            {
                message = ex.Message;
            }


            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteTrainingCourseInstructor(int Id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                if (Id == -1)
            {
                Id = 0;
            }
            if (Id != 0)
            {

                _guardLogDataProvider.DeleteTrainingCourseInstructor(Id);
            }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostUpdateCoursesStatus(int Id, int TrainingCourseStatusId)
        {


            var success = false;
            var message = string.Empty;
            try
            {

               

                
                var result = _guardDataProvider.GetGuardTrainingAndAssessmentwithId(Id).FirstOrDefault();
                var hrsettingsId = _configDataProvider.GetTrainingCoursesWithCourseId(result.TrainingCourseId).FirstOrDefault();
                bool selected = false;
                var trainingSettings = _configDataProvider.GetTQSettings(hrsettingsId.HRSettingsId);
                var  trainingSettingsQuestions = _configDataProvider.GetTrainingQuestionsWithHRSettings(hrsettingsId.HRSettingsId);
                if (trainingSettings.Count==0 || trainingSettingsQuestions.Count==0)
                {
                    selected = false;
                    message = "Training details for this course have not been saved. Please contact your administrator.";
                }
                else
                {
                    selected = true;
                }
                if (selected == true)
                {
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = Id,
                        GuardId = result.GuardId,
                        TrainingCourseId = result.TrainingCourseId,
                        TrainingCourseStatusId = TrainingCourseStatusId,
                        Description = result.Description,
                        HRGroupId = result.HRGroupId
                        //,
                        //IsCompleted = false

                    });


                    success = true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetCourseCertificateDocsUsingSettingsId(int type)
        {
            var result = _configDataProvider.GetCourseCertificateDocsUsingSettingsId(type);
            //foreach(var item in result)
            //{
            //    var getRPL = _configDataProvider.GetCourseCertificateRPLUsingId(item.Id);
            //    if(getRPL.Count>0)
            //    {
            //        foreach(var itemnew in getRPL)
            //        { 
            //            if(itemnew.isDeleted==false)
            //            {
            //                item.isRPLEnabled = true;
            //            }
            //            else
            //            {
            //                item.isRPLEnabled = false;
            //            }
            //        }
                    
            //    }
            //    else
            //    {
            //        item.isRPLEnabled = false;
            //    }
            //}
            return new JsonResult(result);
        }
        public JsonResult OnPostUploadCourseCertificateDocUsingHR()
        {
            var success = false;
            var message = "Uploaded successfully";
            var files = Request.Form.Files;
            if (files.Count == 1)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (".pdf,.ppt,.pptx".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
                            throw new ArgumentException("Unsupported file type");
                        var hrreferenceNumber = Request.Form["hrreferenceNumber"].ToString();
                        int hrsettingsid = Convert.ToInt32(Request.Form["hrsettingsid"]);
                        string filename = Request.Form["filename"].ToString();
                        var CourseDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Certificate");
                        if (!Directory.Exists(CourseDocsFolder))
                            Directory.CreateDirectory(CourseDocsFolder);
                        if (System.IO.File.Exists(Path.Combine(CourseDocsFolder, filename)))
                            //throw new ArgumentException("File Already Exists");
                            System.IO.File.Delete(Path.Combine(CourseDocsFolder, filename));
                        using (var stream = System.IO.File.Create(Path.Combine(CourseDocsFolder, filename)))
                        {
                            file.CopyTo(stream);
                        }
                        var DropboxDir = _guardDataProvider.GetDrobox();
                        //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardComplianceandlicense.Guard)}/{guardComplianceandlicense.FileName}");
                        var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{DropboxDir.DropboxDir}/TA/{hrreferenceNumber}/Certificate/{file.FileName}");
                        var dbxUploaded = true;
                        dbxUploaded = UpoadDocumentToDropbox(Path.Combine(CourseDocsFolder, file.FileName), dbxFilePath);
                        var documentId = Convert.ToInt32(Request.Form["doc-id"]);
                        
                            _configDataProvider.SaveTrainingCourseCertificate(new TrainingCourseCertificate()
                            {
                                Id = documentId,
                                FileName = filename,
                                LastUpdated = DateTime.Now,
                                HRSettingsId = hrsettingsid,
                                isRPLEnabled=false,

                            });
                        

                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                }
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteCourseCertificateDocUsingHR(int id, string hrreferenceNumber)
        {
            var status = true;
            var message = "Success";
            try
            {
                var document = _configDataProvider.GetCourseCertificateDocuments().SingleOrDefault(x => x.Id == id);
                if (document != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Certificate", document.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);

                    _configDataProvider.DeleteCourseCertificateDocument(id);
                }


            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }
        public JsonResult OnGetTrainingLocation()
        {
            return new JsonResult(_guardLogDataProvider.GetTrainingLocation());
        }
        public JsonResult OnPostSaveTrainingLocation(TrainingLocation record)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                _guardLogDataProvider.SaveTrainingLocation(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteTrainingLocation(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                if (id == 1)
                {
                    success = false;
                    message = "Online is not allowed to delete";
                }
                else
                {
                    _guardLogDataProvider.DeleteTrainingLocation(id);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetRPLDetails(int id,int guardid)
        {
            var success = false;
             return new JsonResult(_configDataProvider.GetCourseCertificateRPLUsingId(id).Where(x=> x.GuardId==guardid).FirstOrDefault());
        }
        public JsonResult OnPostSaveRPLDetails(TrainingCourseCertificateRPL record)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                _guardLogDataProvider.SaveTrainingCourseCertificateRPL(record);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        
        public JsonResult OnPostDeleteRPLDetails(int id)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                _guardLogDataProvider.DeleteTrainingCourseCertificateRPL(id);

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }


        public JsonResult OnGetClientTypesThirdParty(int UserID)
        {
            
            var clienttypes = _viewDataService.GetUserClientTypesHavingAccessThird(UserID);
            foreach (var item in clienttypes)
            {
               
                var result = _userDataProvider.GetDomainDeatils(item.Id);
                if (result != null)
                {
                    item.IsSubDomainEnabled = result.Enabled;
                }
            }
            var ClientTypesThirdParty = clienttypes.Where(x => x.IsSubDomainEnabled == true).ToList();
            return new JsonResult(ClientTypesThirdParty);
            
        }
        public JsonResult OnGetDuressAppDetails(int typeId)
        {
            var fields = _configDataProvider.GetDuressAppByType(typeId);

            return new JsonResult(fields);

        }
        public JsonResult OnPostSaveGuardTrainingPracticalDetails(int guardId,int courseId,int practicalLocationId, int instructorId,DateTime practicalDate)
        {
            var success = false;
            var message = string.Empty;
            int hrsettingsId = _configDataProvider.GetTrainingCoursesWithCourseId(courseId).FirstOrDefault().HRSettingsId;
            try
            {
                


                _configDataProvider.SaveGuardTrainingPracticalDetails(new GuardTrainingAndAssessmentPractical()
                {
                    Id = 0,
                    GuardId = guardId,
                    HRSettingsId = hrsettingsId,
                    PracticalocationlId = practicalLocationId,
                    PracticalDate=practicalDate,
                    InstructorId=instructorId

                });
               

                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message , hrsettingsId });
        }
        public JsonResult OnPostUpdateCourseStatusToComplete(int guardId, int hrSettingsId)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                var TrainingCourses = _configDataProvider.GetTrainingCoursesWithOnlyHrSettingsId(hrSettingsId);
                foreach (var item in TrainingCourses)
                {
                    var record = _guardDataProvider.GetGuardTrainingAndAssessment(guardId).Where(x => x.TrainingCourseId == item.Id).FirstOrDefault();
                    if (record != null)
                    {
                        _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                        {
                            Id = record.Id,
                            GuardId = guardId,
                            TrainingCourseId = item.Id,
                            TrainingCourseStatusId = 4,
                            Description = record.Description,
                            HRGroupId = record.HRGroupId
                            //,
                            //IsCompleted = true

                        });
                    }
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetTrainingCourses()
        {
            var hrGroups = ConfigDataProiver.GetHRGroupsDropDown();
            var result = hrGroups.Select(group => new
            {
                GroupId = group.Value,
                Courses = ConfigDataProiver.GetTrainingCoursesStatusWithOutcome(Convert.ToInt32(group.Value))
                    .Select(course => new
                    {
                        course.Id,
                        course.Description
                        //,
                        //course.CourseStatus
                    }).ToList()
            }).Where(group => group.Courses.Any()).ToList();

            return new JsonResult(result);
        }
        public JsonResult OnPostDeleteGuardCourseByAdmin(int Id)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                //int id = _guardLogDataProvider.SaveTestQuestions(testquestions);
                if (Id != 0)
                {

                    _guardLogDataProvider.DeleteGuardCourseByAdmin( Id);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetCourseStatusColorById(int hrSettingsid)
        {
            bool courseLength = false;
            bool testQuestionsLength = false;
            bool certificateLength = false;
            var coursesList = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrSettingsid).ToList();
            if(coursesList.Count()>0)
            {
                courseLength = true;
                
            }
            var testQuestionsSettingsList = _configDataProvider.GetTrainingTestQuestionsColor(hrSettingsid).ToList();
            if (testQuestionsSettingsList.Count() > 0)
            {
                testQuestionsLength = true;
            }
            var courseCertificatesList = _configDataProvider.GetCourseCertificateDocuments().Where(x=>x.HRSettingsId== hrSettingsid).ToList();
            if (courseCertificatesList.Count() > 0)
            {
                certificateLength = true;
            }

            return new JsonResult(new { courseLength, testQuestionsLength, certificateLength });

        }
        public JsonResult OnPostSaveCourseCertificateIsRPL(int certificateId,bool isRPLchecked)
        {
            var success = false;
            var message = "Saved successfully";
           
                    try
                    {

                        var record = _configDataProvider.GetCourseCertificateDocuments().Where(x => x.Id == certificateId).FirstOrDefault();
                        _configDataProvider.SaveTrainingCourseCertificate(new TrainingCourseCertificate()
                        {
                            Id = record.Id,
                            FileName = record.FileName,
                            LastUpdated = record.LastUpdated,
                            HRSettingsId = record.HRSettingsId,
                            isRPLEnabled = isRPLchecked,

                        });


                        success = true;
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }
                
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetCertificateIsRPL(int courseId)
        {
            int hrSettingsid = _configDataProvider.GetTrainingCoursesWithCourseId(courseId).FirstOrDefault().HRSettingsId;

            return new JsonResult(_configDataProvider.GetCourseCertificateDocsUsingSettingsId(hrSettingsid).FirstOrDefault());
        }
        public JsonResult OnPostCompanyAPIDetails(Data.Models.CompanyDetails company)
        {
            var status = true;
            var message = "Success";
            try
            {
                _clientDataProvider.SaveCompanyAPIDetails(company);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }


    }
    public class helpDocttype
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    
}
