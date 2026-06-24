using CityWatch.Common.Helpers;
using CityWatch.Common.Models;
using CityWatch.Common.Models;
using CityWatch.Common.Services;
using CityWatch.Common.Services;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Dropbox.Api.Files;
using Dropbox.Api.Users;
using ImageMagick;
using MailKit.Net.Smtp;
using MailKit.Search;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using MimeKit;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dropbox.Api.FileRequests.GracePeriod;
using static Dropbox.Api.Sharing.ListFileMembersIndividualResult;
using static Dropbox.Api.Sharing.MemberSelector;
using static Dropbox.Api.Team.GroupSelector;
using static Dropbox.Api.TeamLog.ActorLogInfo;
using static Dropbox.Api.TeamLog.EventCategory;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using CityWatch.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using CityWatch.Web.API;




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
        private readonly ICertificateGenerator _certificateGenerator;
        private readonly EmailOptions _EmailOptions;
        private readonly CityWatchDbContext _context;
        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IViewDataService viewDataService,
            IGuardLogDataProvider guardLogDataProvider,
             ITimesheetReportGenerator TimesheetReportGenerator, IGuardDataProvider guardDataProvider, IOptions<Helpers.Settings> settings,
             IDropboxService dropboxUploadService, ICertificateGenerator certificateGenerator,
             IOptions<EmailOptions> emailOptions, CityWatchDbContext context)
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
            _certificateGenerator = certificateGenerator;
            _EmailOptions = emailOptions.Value;
            _context = context;
        }
        public string IsAdminminOrPoweruser = string.Empty;
        public HrSettings HrSettings;
        public IncidentReportField IncidentReportField;
        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }

        public IUserDataProvider UserDataProvider { get { return _userDataProvider; } }

        public IClientDataProvider ClientDataProvider { get { return _clientDataProvider; } }
               
        public IGuardLogDataProvider GuardLogDataProvider { get { return _guardLogDataProvider; } }

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
        public string ClientNameTitle { get; set; }
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
                    var domain = _configDataProvider.GetSubDomainDetails(clientName).TypeId;
                    if (domain != 0)
                    {
                        ClientTypeId = domain;
                        ClientNameTitle = _configDataProvider.GetSubDomainDetails(clientName).Domain;
                    }
                    else
                    {
                        ClientTypeId = 0;
                        ClientNameTitle = "Citywatch Security";
                    }
                }
                else
                {
                    ClientTypeId = 0;
                    ClientNameTitle = "Citywatch Security";
                }
            }
            if (GuardId != 0)
            {
                Guard = _viewDataService.GetGuards().SingleOrDefault(x => x.Id == GuardId);

            }
            if (!AuthUserHelper.IsAdminUserLoggedIn && !AuthUserHelper.IsAdminGlobal && !AuthUserHelper.IsAdminPowerUser && !Guard.IsAdminSOPToolsAccess && !Guard.IsAdminAuditorAccess && !Guard.IsAdminInvestigatorAccess && !Guard.IsAdminThirdPartyAccess)
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
            //var clienttypes = _viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId);
            var clienttypes = _viewDataService.GetUserClientTypesHavingAccess(null);// for getting access to global admin also
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
                return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, null, searchTerm, searchTermtwo));
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
            if (files.Count == 1 && domainId != string.Empty)
            {
                var file = files[0];
                if (file.Length > 0)
                {
                    try
                    {
                        if (Path.GetExtension(file.FileName) != ".pdf")
                            throw new ArgumentException("Unsupported file type");

                        var fileName = domainId == "0" ? "IR_Form_Template.pdf" : "IR_Form_Template_" + domainName.Trim() + ".pdf";
                        var reportRootDir = Path.Combine(_webHostEnvironment.WebRootPath, "Pdf", "Template");
                        using (var stream = System.IO.File.Create(Path.Combine(reportRootDir, fileName)))
                        {
                            file.CopyTo(stream);
                        }

                        _configDataProvider.SaveDefaultEmailThirdPartyDomains(string.Empty, int.Parse(domainId), fileName);
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
        public JsonResult OnGetStaffDocsUsingType(int type, string query,string companyProfile)
        {
            if(companyProfile!=null)
            {
                return new JsonResult(_configDataProvider.GetStaffDocumentsUsingType(type, query).Where(x=>x.SubDomainId == Convert.ToInt32(companyProfile)));
            }
            return new JsonResult(_configDataProvider.GetStaffDocumentsUsingType(type, query));
        }

        [DisableRequestSizeLimit]
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
                        // 07-05-2026 - MP4 support added to allow video SOPs and Training resources
                        if (".pdf,.docx,.xlsx,.mp4".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
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

        [DisableRequestSizeLimit]
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
                        // 07-05-2026 - MP4 support added to allow video SOPs and Training resources
                        if (".pdf,.docx,.xlsx,.mp4".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
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
                        int subdomainid = 0;
                        var domain = Request.Form["profile"].ToString();
                        if (Request.Form["profile"].ToString()!=null && Request.Form["profile"].ToString() !="")
                        {
                            subdomainid = Convert.ToInt32(Request.Form["profile"]);
                        }
                        _configDataProvider.SaveStaffDocument(new StaffDocument()
                        {
                            Id = documentId,
                            FileName = file.FileName,
                            LastUpdated = DateTime.Now,
                            DocumentType = type,
                            SubDomainId = subdomainid

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

        public JsonResult OnGetGuardUnavailabilities(int guardId)
        {
            var records = _guardDataProvider.GetGuardUnavailabilities(guardId);
            return new JsonResult(records);
        }

        public JsonResult OnPostSaveGuardUnavailability(int guardId, string reason, string reasonOther, DateTime fromDate, DateTime toDate)
        {
            var success = false;
            var message = "Leave saved successfully.";
            try
            {
                if (fromDate.Date < DateTime.Today)
                {
                    return new JsonResult(new { success = false, message = "Leaves cannot be added for past dates." });
                }

                if (fromDate > toDate)
                {
                    return new JsonResult(new { success = false, message = "'From Date' must be before or equal to 'To Date'." });
                }

                // Check overlap
                if (_guardDataProvider.IsGuardUnavailable(guardId, fromDate, toDate, out var conflict))
                {
                    return new JsonResult(new { success = false, message = "Guard is already marked unavailable during this period: " + conflict.FromDate.ToString("dd MMMM yyyy") + " to " + conflict.ToDate.ToString("dd MMMM yyyy") });
                }

                _guardDataProvider.SaveGuardUnavailability(new GuardUnavailability
                {
                    GuardId = guardId,
                    Reason = reason,
                    ReasonOther = reasonOther,
                    FromDate = fromDate,
                    ToDate = toDate
                });
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteGuardUnavailability(int id)
        {
            var success = true;
            try
            {
                _guardDataProvider.DeleteGuardUnavailability(id);
            }
            catch (Exception)
            {
                success = false;
            }
            return new JsonResult(new { success });
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

        public JsonResult OnPostClientAccessByUserId(int userId, int[] selectedSites, int ClientTypeId)
        {
            var status = true;
            var message = "Success";
            try
            {
                var clientSiteAccess = selectedSites.Select(x => new UserClientSiteAccess()
                {
                    ClientSiteId = x,
                    UserId = userId,
                    ThirdPartyID = ClientTypeId
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

        public JsonResult OnPostHrSettingsLockedClientSites(int hrSttingsId, int[] selectedSites, int enableStatus)
        {
            var status = true;
            var message = "Success";
            try
            {
                _guardLogDataProvider.UpdateHRLockSettings(hrSttingsId, Convert.ToBoolean(enableStatus));
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
                var rtn = _guardLogDataProvider.GetAllClientSites().Where(x => (typeId == null || typeId3.Contains(x.TypeId)) && x.IsActive).OrderBy(z => z.Name).ThenBy(z => z.TypeId);                
                return new JsonResult(rtn);
            }
            var rtn2 = _guardLogDataProvider.GetAllClientSites().Where(x => x.TypeId == 0 && x.IsActive).OrderBy(z => z.Name).ThenBy(z => z.TypeId);
            return new JsonResult(rtn2);
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

            if (!string.IsNullOrWhiteSpace(CriticalDocModel.GroupName))
            {
                var existingDocs = _configDataProvider.GetCriticalDocs();
                if (existingDocs.Any(x => !string.IsNullOrEmpty(x.GroupName) && x.GroupName.Trim().Equals(CriticalDocModel.GroupName.Trim(), StringComparison.OrdinalIgnoreCase) && x.Id != CriticalDocModel.Id))
                {
                    return new JsonResult(new { success = false, message = "Group name already exists. Please choose a different name." });
                }
            }

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
        public async Task<JsonResult> OnPostDownloadTimesheet(string startdate, string endDate, string frequency, int guradid, int? siteId)
        {

            var fileName = string.Empty;
            var statusCode = 0;
            int id = 1;
            try
            {

                if (guradid > 0)
                {
                    fileName = _TimesheetReportGenerator.GeneratePdfTimesheetReportCustom(startdate, endDate, guradid);
                }
                else if (siteId > 0)
                {
                    fileName = await _TimesheetReportGenerator.GenerateTimesheetZipFile(new int[] { siteId.Value }, startdate, endDate);
                }





            }
            catch (Exception ex)
            {

            }

            if (string.IsNullOrEmpty(fileName))
                return new JsonResult(new { fileName, message = "Failed to generate pdf/zip", statusCode = -1 });





            var downloadPath = fileName.EndsWith(".zip") ? $"~/Pdf/FromDropbox/{fileName}" : $"~/Pdf/Output/{fileName}";

            return new JsonResult(new { fileName = @Url.Content(downloadPath), statusCode });
        }

        public async Task<JsonResult> OnPostDownloadTimesheetFrequency(string frequency, int guradid, int? siteId)
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
                string StartDate = startDate.ToString("yyyy-MM-dd");
                string EndDate = endDate.ToString("yyyy-MM-dd");
                if (guradid > 0)
                {
                    fileName = _TimesheetReportGenerator.GeneratePdfTimesheetReport(StartDate, EndDate, guradid);
                }
                else if (siteId > 0)
                {
                    fileName = await _TimesheetReportGenerator.GenerateTimesheetZipFileFrequency(new int[] { siteId.Value }, StartDate, EndDate);
                }


            }
            catch (Exception ex)
            {

            }

            if (string.IsNullOrEmpty(fileName))
                return new JsonResult(new { fileName, message = "Failed to generate pdf/zip", statusCode = -1 });





            var downloadPath = fileName.EndsWith(".zip") ? $"~/Pdf/FromDropbox/{fileName}" : $"~/Pdf/Output/{fileName}";

            return new JsonResult(new { fileName = @Url.Content(downloadPath), statusCode });
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


        [DisableRequestSizeLimit]
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
                        // 07-05-2026 - MP4 support added to allow video SOPs and Training resources
                        if (".pdf,.docx,.xlsx,.mp4".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
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

        [DisableRequestSizeLimit]
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
                        // 07-05-2026 - MP4 support added to allow video SOPs and Training resources
                        if (".pdf,.docx,.xlsx,.mp4".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
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
                    FilePath = staffDocsUrl
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


                var status = _configDataProvider.SaveSubDomain(new SubDomain()
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
                    message = "Domain Name '" + domainName + "' already exist.";

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

            var clientIncidentReports = _guardLogDataProvider.GetActiveGuardIncidentReportHistoryForAdmin(guardId);

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
            return new JsonResult(_configDataProvider.GetCourseDocsUsingSettingsId(type).Where(x => Path.HasExtension(x.FileName)));
        }
        public JsonResult OnGetTQNumbers()
        {
            return new JsonResult(_configDataProvider.GetTQNumbers());
        }

        [RequestSizeLimit(1073741824)] // 100 MB
        [HttpPost]
        public async Task<IActionResult> OnPostUploadCourseDocUsingHR(IFormFile chunk, string fileName, int chunkIndex, int totalChunks, int hrsettingsid, string hrreferenceNumber, int docid, int tqid)
        {
            var success = false;
            var message = "Uploaded successfully";


            try
            {

                var CourseDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Course");
                var tempFolder = Path.Combine(CourseDocsFolder, "UploadedChunks", fileName);

                Directory.CreateDirectory(tempFolder);
                if (System.IO.File.Exists(Path.Combine(CourseDocsFolder, fileName)))
                    //throw new ArgumentException("File Already Exists");
                    System.IO.File.Delete(Path.Combine(CourseDocsFolder, fileName));

                var chunkPath = Path.Combine(tempFolder, $"{chunkIndex}.part");

                using (var stream = new FileStream(chunkPath, FileMode.Create))
                {
                    await chunk.CopyToAsync(stream);
                }

                // Optional: If all chunks received, combine them
                if (Directory.GetFiles(tempFolder).Length == totalChunks)
                {
                    var finalpath = Path.Combine(CourseDocsFolder, fileName);


                    using (var finalStream = new FileStream(finalpath, FileMode.Create))
                    {
                        //for (int i = 1; i <= totalChunks; i++)
                        //{
                        //    var partPath = Path.Combine(tempFolder, $"{i}.part");
                        //    var bytes = await System.IO.File.ReadAllBytesAsync(partPath);
                        //    await finalStream.WriteAsync(bytes, 0, bytes.Length);
                        //    System.IO.File.Delete(partPath); // optional
                        //}
                        for (int i = 1; i <= totalChunks; i++)
                        {
                            var partPath = Path.Combine(tempFolder, $"{i}.part");
                            using (var partStream = new FileStream(partPath, FileMode.Open, FileAccess.Read))
                            {
                                await partStream.CopyToAsync(finalStream);
                            }
                            System.IO.File.Delete(partPath);
                        }
                    }

                    Directory.Delete(tempFolder);
                    
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
                            FileName = fileName,
                            LastUpdated = DateTime.Now,
                            HRSettingsId = hrsettingsid,
                            TQNumberId = TQNumber,
                            IsDeleted = false

                        });

                    }
                    else
                    {
                        _configDataProvider.SaveTrainingCourses(new TrainingCourses()
                        {
                            Id = documentId,
                            FileName = fileName,
                            LastUpdated = DateTime.Now,
                            HRSettingsId = hrsettingsid,
                            TQNumberId = TQNumbernew,
                            IsDeleted = false

                        });
                    }
                    var tqsettings = _configDataProvider.GetTQSettings(hrsettingsid).ToList();
                    if (tqsettings.Count == 0)
                    {
                        _guardLogDataProvider.SaveTestQuestionSettings(new TrainingTestQuestionSettings()
                        {
                            Id = -1,
                            HRSettingsId = hrsettingsid,
                            CourseDurationId = 3,
                            TestDurationId = 3,
                            PassMarkId = 1,
                            AttemptsId = 1,
                            IsCertificateExpiry = false,
                            CertificateExpiryId = null,
                            IsCertificateWithQAndADump = false,
                            IsCertificateHoldUntilPracticalTaken = false,
                            IsAnonymousFeedback = false,
                            IsDeleted = false


                        });
                    }

                    success = true;
                    if (".mp4".IndexOf(Path.GetExtension(fileName).ToLower()) < 0)
                    {
                        var DropboxDir = _guardDataProvider.GetDrobox();
                        //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardComplianceandlicense.Guard)}/{guardComplianceandlicense.FileName}");
                        var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{DropboxDir.DropboxDir}/TA/{hrreferenceNumber}/Course/{fileName}");
                        var dbxUploaded = true;
                        dbxUploaded = UpoadDocumentToDropbox(Path.Combine(CourseDocsFolder, fileName), dbxFilePath);
                    }
                }

            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message }); ;
        }

        //public JsonResult OnPostUploadCourseDocUsingHR()
        //{
        //    var success = false;
        //    var message = "Uploaded successfully";
        //    var files = Request.Form.Files;
        //    if (files.Count == 1)
        //    {
        //        var file = files[0];
        //        if (file.Length > 0)
        //        {
        //            try
        //            {
        //                if (".pdf,.ppt,.pptx,.mp4".IndexOf(Path.GetExtension(file.FileName).ToLower()) < 0)
        //                    throw new ArgumentException("Unsupported file type");
        //                var hrreferenceNumber = Request.Form["hrreferenceNumber"].ToString();
        //                int hrsettingsid = Convert.ToInt32(Request.Form["hrsettingsid"]);
        //                var CourseDocsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Course");
        //                if (!Directory.Exists(CourseDocsFolder))
        //                    Directory.CreateDirectory(CourseDocsFolder);
        //                if (System.IO.File.Exists(Path.Combine(CourseDocsFolder, file.FileName)))
        //                    //throw new ArgumentException("File Already Exists");
        //                    System.IO.File.Delete(Path.Combine(CourseDocsFolder, file.FileName));
        //                using (var stream = System.IO.File.Create(Path.Combine(CourseDocsFolder, file.FileName)))
        //                {
        //                    file.CopyTo(stream);
        //                }
        //                var DropboxDir = _guardDataProvider.GetDrobox();
        //                //var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{GuardHelper.GetGuardDocumentDbxRootFolder(guardComplianceandlicense.Guard)}/{guardComplianceandlicense.FileName}");
        //                var dbxFilePath = FileNameHelper.GetSanitizedDropboxFileNamePart($"{DropboxDir.DropboxDir}/TA/{hrreferenceNumber}/Course/{ file.FileName}");
        //                var dbxUploaded = true;
        //                dbxUploaded=UpoadDocumentToDropbox(Path.Combine(CourseDocsFolder, file.FileName),  dbxFilePath);
        //            var documentId = Convert.ToInt32(Request.Form["doc-id"]);
        //                int TQNumbernew = Convert.ToInt32(Request.Form["tq-id"]);
        //                if (TQNumbernew == 0)
        //                {
        //                    int TQNumber = _configDataProvider.GetLastTQNumber(hrsettingsid);
        //                    if (TQNumber == 0)
        //                    {
        //                        throw new ArgumentException("TQ Number only contains from 01 to 10");
        //                    }
        //                    _configDataProvider.SaveTrainingCourses(new TrainingCourses()
        //                    {
        //                        Id = documentId,
        //                        FileName = file.FileName,
        //                        LastUpdated = DateTime.Now,
        //                        HRSettingsId = hrsettingsid,
        //                        TQNumberId = TQNumber,
        //                        IsDeleted = false

        //                    });

        //                }
        //                else
        //                {
        //                    _configDataProvider.SaveTrainingCourses(new TrainingCourses()
        //                    {
        //                        Id = documentId,
        //                        FileName = file.FileName,
        //                        LastUpdated = DateTime.Now,
        //                        HRSettingsId = hrsettingsid,
        //                        TQNumberId = TQNumbernew,
        //                        IsDeleted = false

        //                    });
        //                }
        //                var tqsettings = _configDataProvider.GetTQSettings(hrsettingsid).ToList();
        //                if (tqsettings.Count == 0)
        //                {
        //                    _guardLogDataProvider.SaveTestQuestionSettings(new TrainingTestQuestionSettings()
        //                    {
        //                        Id = -1,
        //                        HRSettingsId = hrsettingsid,
        //                        CourseDurationId = 3,
        //                        TestDurationId = 3,
        //                        PassMarkId = 1,
        //                        AttemptsId = 1,
        //                        IsCertificateExpiry = false,
        //                        CertificateExpiryId = null,
        //                        IsCertificateWithQAndADump = false,
        //                        IsCertificateHoldUntilPracticalTaken = false,
        //                        IsAnonymousFeedback = false,
        //                        IsDeleted = false


        //                    });
        //                }

        //                success = true;
        //                if (".ppt,.pptx".IndexOf(Path.GetExtension(file.FileName).ToLower()) > 0)
        //                {
        //                    Application pptApplication = new Application();
        //                    Presentation pptPresentation = null;

        //                    string inputPath = Path.Combine(CourseDocsFolder, file.FileName);
        //                    string outputPath = Path.ChangeExtension(Path.Combine(CourseDocsFolder, file.FileName), ".pdf");
        //                    if (System.IO.File.Exists(outputPath))
        //                        //throw new ArgumentException("File Already Exists");
        //                        System.IO.File.Delete(outputPath);

        //                    try
        //                    {
        //                        pptPresentation = pptApplication.Presentations.Open(inputPath, WithWindow: MsoTriState.msoFalse);
        //                        pptPresentation.SaveAs(outputPath, PpSaveAsFileType.ppSaveAsPDF);
        //                    }
        //                    finally
        //                    {
        //                        pptPresentation?.Close();
        //                        pptApplication.Quit();
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                message = ex.Message;
        //            }
        //        }
        //    }
        //    return new JsonResult(new { success, message });
        //}

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
            catch(Exception ex)
            {
            }

            return uploaded;
        }
        public JsonResult OnPostDeleteCourseDocUsingHR(int id, string hrreferenceNumber)
        {
            var status = true;
            var message = "Success";
            try
            {
                var document = _configDataProvider.GetCourseDocuments().SingleOrDefault(x => x.Id == id);
                if (document != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Course", document.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);
                  
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
                if (record.CertificateExpiryId == 0)
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

                int id = _guardLogDataProvider.SaveTestQuestions(testquestions);
                if (id != 0)
                {
                    foreach (var item in testquestionanswers)
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
        public JsonResult OnGetNextQuestionWithinSameTQNumber(int hrSettingsId, int tqNumberId)
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
            // return new JsonResult(_configDataProvider.GetLastFeedbackQNumbers(hrSettingsId));
            return new JsonResult(_configDataProvider.GetLastFeedbackQNumbers());
        }
        public JsonResult OnGetFeedbackQuestionsCount(int hrSettingsId)
        {
            //return new JsonResult(_configDataProvider.GetFeedbackQuestionsCount(hrSettingsId));
            return new JsonResult(_configDataProvider.GetFeedbackQuestionsCount());
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
            //var Questions = _configDataProvider.GetFeedbackQuestions(hrSettingsId, questionumberId);
            var Questions = _configDataProvider.GetFeedbackQuestions(questionumberId);

            return new JsonResult(Questions);
        }
        public IActionResult OnGetFeedbackQuestionAndAnswersWithQuestionNumber(int questionId)
        {
            var Answers = _configDataProvider.GetTrainingFeedbackQuestionsAnswers(questionId);

            return new JsonResult(Answers);
        }
        public JsonResult OnPostUpdateDocumentTQNumber(int id, string name, TrainingCourses record)
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
                            TQNumberId = TQNumbernew,
                            IsDeleted = false

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
            return new JsonResult(_guardLogDataProvider.GetTrainingInstructorNameandPositionFields().Where(x => x.Id == Convert.ToInt32(Id)).FirstOrDefault());
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
        public JsonResult OnPostSaveGuardTrainingAndAssessmentTab(int HRSettingsId, int GuardId, int TrainingCourseStatusId)
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
                // Defensive check: Ensure the guard training record exists before proceeding
                var result = _guardDataProvider.GetGuardTrainingAndAssessmentwithId(Id).FirstOrDefault();
                if (result == null)
                {
                    return new JsonResult(new { success = false, message = "Guard training record not found." });
                }

                // Defensive check: Ensure the training course details are available
                var trainingCourse = _configDataProvider.GetTrainingCoursesWithCourseId(result.TrainingCourseId).FirstOrDefault();
                if (trainingCourse == null)
                {
                    // Fallback: If training course details are missing, proceed with a basic status update to avoid blocking user flow
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = Id,
                        GuardId = result.GuardId,
                        TrainingCourseId = result.TrainingCourseId,
                        TrainingCourseStatusId = TrainingCourseStatusId,
                        Description = result.Description,
                        HRGroupId = result.HRGroupId,
                        Attempts = result.Attempts
                    });
                    return new JsonResult(new { success = true, message = "" });
                }

                // Retrieve training settings (e.g., attempt limits) based on HR settings ID
                var trainingSettings = _configDataProvider.GetTQSettings(trainingCourse.HRSettingsId).FirstOrDefault();
                
                // If specific attempt limits are defined, enforce them
                if (trainingSettings != null && trainingSettings.Attempts != null)
                {
                    if (result.Attempts < Convert.ToInt32(trainingSettings.Attempts.Name))
                    {
                        _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                        {
                            Id = Id,
                            GuardId = result.GuardId,
                            TrainingCourseId = result.TrainingCourseId,
                            TrainingCourseStatusId = TrainingCourseStatusId,
                            Description = result.Description,
                            HRGroupId = result.HRGroupId,
                            Attempts = result.Attempts + 1
                        });
                    }
                }
                else
                {
                    // If no specific training settings or attempt limits are defined, just update the status
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = Id,
                        GuardId = result.GuardId,
                        TrainingCourseId = result.TrainingCourseId,
                        TrainingCourseStatusId = TrainingCourseStatusId,
                        Description = result.Description,
                        HRGroupId = result.HRGroupId,
                        Attempts = result.Attempts
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
                        bool isrpl = false;
                        var rpldetails = _configDataProvider.GetCourseCertificateDocuments().Where(x => x.Id == documentId).FirstOrDefault();
                        if (rpldetails != null)
                        {
                            isrpl = rpldetails.isRPLEnabled;
                        }
                        _configDataProvider.SaveTrainingCourseCertificate(new TrainingCourseCertificate()
                        {
                            Id = documentId,
                            FileName = filename,
                            LastUpdated = DateTime.Now,
                            HRSettingsId = hrsettingsid,
                            isRPLEnabled = isrpl,
                            IsDeleted = false

                        });
                        var courses = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrsettingsid).ToList();
                        var hrdesc = _configDataProvider.GetHRSettings().Where(x => x.Id == hrsettingsid).FirstOrDefault().Description;
                        if (courses.Count() == 0)
                        {
                            _configDataProvider.SaveTrainingCourses(new TrainingCourses()
                            {
                                Id = 0,
                                FileName = hrdesc,
                                HRSettingsId = hrsettingsid,
                                LastUpdated = DateTime.Now,
                                TQNumberId = 1,
                                IsDeleted = false
                            });
                        }

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
                int hrsettingsid = document.HRSettingsId;
                if (document != null)
                {
                    var fileToDelete = Path.Combine(_webHostEnvironment.WebRootPath, "TA", hrreferenceNumber, "Certificate", document.FileName);
                    if (System.IO.File.Exists(fileToDelete))
                        System.IO.File.Delete(fileToDelete);

                    _configDataProvider.DeleteCourseCertificateDocument(id);
                    var courses = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrsettingsid).Where(x => !Path.HasExtension(x.FileName)).ToList();
                    var questions = _configDataProvider.GetTrainingTestQuestions().Where(x => x.HRSettingsId == hrsettingsid).ToList();
                    if (courses.Count() != 0 && questions.Count() == 0)
                    {
                        foreach (var item in courses)
                        {
                            _configDataProvider.DeleteCourseDocument(item.Id);
                        }
                    }
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
        public JsonResult OnGetRPLDetails(int id, int guardid)
        {
            var success = false;
            return new JsonResult(_configDataProvider.GetCourseCertificateRPLUsingId(id).Where(x => x.GuardId == guardid).FirstOrDefault());
        }
        public JsonResult OnPostSaveRPLDetails(TrainingCourseCertificateRPL record)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                _guardLogDataProvider.SaveTrainingCourseCertificateRPL(record);
                string input = GenerateFormattedString();
                string hashCode = GenerateHashCode(input);
                int hrSettingsId = _configDataProvider.GetCourseCertificateDocuments().Where(x => x.Id == record.TrainingCourseCertificateId).FirstOrDefault().HRSettingsId;
                var filename = _certificateGenerator.GeneratePdf(record.GuardId, hrSettingsId, hashCode, false, false, false);
                var compliance = _guardDataProvider.GetGuardCompliancesAndLicense(record.GuardId).Where(x => x.FileName == filename).FirstOrDefault();
                int id = 0;
                DateTime? expirydate = DateTime.Now;
                bool IsExpiry = true;
                var settings = _configDataProvider.GetTQSettings(hrSettingsId).FirstOrDefault();
                if(settings !=null )
                {
                    if (settings.IsCertificateExpiry == false)
                        IsExpiry = true;
                    else
                        IsExpiry = false;
                }
                if (compliance != null)
                {
                    id = compliance.Id;
                }
                var hrSettingsList = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault();
                var hrdesription = hrSettingsList.ReferenceNoNumbers.Name + hrSettingsList.ReferenceNoAlphabets.Name + " " + hrSettingsList.Description;
                var hrgroupid = _configDataProvider.GetHRSettings().Where(x => x.Id == hrSettingsId).FirstOrDefault().HRGroupId;

                _guardDataProvider.SaveGuardComplianceandlicanse(new GuardComplianceAndLicense()
                {
                    Id = id,
                    GuardId = record.GuardId,
                    Description = hrdesription,
                    CurrentDateTime = DateTime.Now.ToString(),
                    FileName = filename,
                    HrGroup = (HrGroup?)hrgroupid,
                    ExpiryDate = expirydate,
                    DateType = IsExpiry,
                    Reminder1 = 45,
                    Reminder2 = 7
                });
                var courses = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrSettingsId);
                foreach (var item in courses)
                {
                    var report = _configDataProvider.ReturnCourseTestStatusTostart(record.GuardId, item.Id);
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = report.Id,
                        GuardId = record.GuardId,
                        TrainingCourseId = report.TrainingCourseId,
                        TrainingCourseStatusId = 4,
                        Description = report.Description,
                        HRGroupId = report.HRGroupId
                        //,
                        //IsCompleted = true

                    });
                }
                var emailBody = GiveGuardCourseCompletedNotification(record.GuardId, hrdesription);
                SendEmailNew(emailBody);
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
        public JsonResult OnGetDuressAppDetails(int typeId, int? profileid = null)
        {
            var fields = _configDataProvider.GetDuressAppByType(typeId, profileid);

            return new JsonResult(fields);

        }
        public JsonResult OnPostSaveGuardTrainingPracticalDetails(int guardId, int courseId, int practicalLocationId, int instructorId, DateTime practicalDate, string FileName)
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
                    PracticalDate = practicalDate,
                    InstructorId = instructorId,
                    FileName = FileName

                });
                string input = GenerateFormattedString();
                string hashCode = GenerateHashCode(input);
                var getcertificateSatus = _configDataProvider.GetTQSettings(hrsettingsId).FirstOrDefault();
                var filename = _certificateGenerator.GeneratePdf(guardId, hrsettingsId, hashCode, true, getcertificateSatus.IsCertificateWithQAndADump, getcertificateSatus.IsCertificateExpiry);
                DateTime? expirydate = DateTime.Now;
                bool IsExpiry = false;
                if (getcertificateSatus.IsCertificateExpiry == true)
                {

                    var expiryyears = _configDataProvider.GetTQSettings(hrsettingsId).Where(x => x.IsCertificateExpiry == true).FirstOrDefault().CertificateExpiryYears.Name;
                    IsExpiry = false;
                    string newexpiry = string.Empty;
                    if (expiryyears.Contains("year"))
                        newexpiry = expiryyears.Replace("year", "");
                    if (expiryyears.Contains("years"))
                        newexpiry = expiryyears.Replace("years", "");
                    DateTime currentdate = DateTime.Now;
                    expirydate = currentdate.AddYears(Convert.ToInt32(newexpiry));

                }
                else
                {
                    expirydate = DateTime.Now;
                    IsExpiry = true;
                }
                var hrSettingsList = _configDataProvider.GetHRSettings().Where(x => x.Id == hrsettingsId).FirstOrDefault();
                var hrdesription = hrSettingsList.ReferenceNoNumbers.Name + hrSettingsList.ReferenceNoAlphabets.Name + " " + hrSettingsList.Description;
                var hrgroupid = _configDataProvider.GetHRSettings().Where(x => x.Id == hrsettingsId).FirstOrDefault().HRGroupId;
                var compliance = _guardDataProvider.GetGuardCompliancesAndLicense(guardId).Where(x => x.FileName == filename).FirstOrDefault();
                int id = 0;
                if (compliance != null)
                {
                    id = compliance.Id;
                }

                _guardDataProvider.SaveGuardComplianceandlicanse(new GuardComplianceAndLicense()
                {
                    Id = id,
                    GuardId = guardId,
                    Description = hrdesription,
                    CurrentDateTime = DateTime.Now.ToString(),
                    FileName = filename,
                    HrGroup = (HrGroup?)hrgroupid,
                    ExpiryDate = expirydate,
                    DateType = IsExpiry,
                    Reminder1 = 45,
                    Reminder2 = 7
                });


                var courses = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrsettingsId);
                foreach (var item in courses)
                {
                    var report = _configDataProvider.ReturnCourseTestStatusTostart(guardId, item.Id);
                    _configDataProvider.SaveGuardTrainingAndAssessmentTab(new GuardTrainingAndAssessment()
                    {
                        Id = report.Id,
                        GuardId = guardId,
                        TrainingCourseId = report.TrainingCourseId,
                        TrainingCourseStatusId = 4,
                        Description = report.Description,
                        HRGroupId = report.HRGroupId
                        //,
                        //IsCompleted = true

                    });
                }
                success = true;
                var emailBody = GiveGuardCourseCompletedNotification(guardId, hrdesription);
                SendEmailNew(emailBody);


            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message, hrsettingsId });
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
        public JsonResult OnGetTrainingCourses(bool isOnboardingUser = false)
        {
            var hrGroups = ConfigDataProiver.GetHRGroupsDropDown();

            List<int> allowedCourseIds = null;
            if (isOnboardingUser)
            {
                var onboardingUser = _userDataProvider.GetUsers().FirstOrDefault(x => string.Equals(x.UserName, "onboarding", StringComparison.OrdinalIgnoreCase));
                if (onboardingUser != null)
                {
                    allowedCourseIds = _guardDataProvider.GetOnBoardUsersTrainingAndAssessment(onboardingUser.Id)
                        .Select(c => c.TrainingCourseId)
                        .ToList();
                }
            }

            var result = hrGroups.Select(group => new
            {
                GroupId = group.Value,
                Courses = ConfigDataProiver.GetTrainingCoursesStatusWithOutcome(Convert.ToInt32(group.Value))
                    .Where(course => allowedCourseIds == null || allowedCourseIds.Contains(course.Id))
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

                    _guardLogDataProvider.DeleteGuardCourseByAdmin(Id);
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
            bool instructorLength = false;
            var coursesList = _configDataProvider.GetTrainingCoursesWithHrSettingsId(hrSettingsid).Where(x => Path.HasExtension(x.FileName)).ToList();
            if (coursesList.Count() > 0)
            {
                courseLength = true;

            }
            var testQuestionsSettingsList = _configDataProvider.GetTrainingTestQuestionsColor(hrSettingsid).ToList();
            if (testQuestionsSettingsList.Count() > 0)
            {
                testQuestionsLength = true;
            }
            var courseCertificatesList = _configDataProvider.GetCourseCertificateDocuments().Where(x => x.HRSettingsId == hrSettingsid).ToList();
            if (courseCertificatesList.Count() > 0)
            {
                certificateLength = true;
            }
            var courseInstructorList = _configDataProvider.GetCourseInstructor(hrSettingsid).ToList();
            if (courseInstructorList.Count() > 0)
            {
                instructorLength = true;
            }

            return new JsonResult(new { courseLength, testQuestionsLength, certificateLength, instructorLength });

        }
        public JsonResult OnPostSaveCourseCertificateIsRPL(int certificateId, bool isRPLchecked)
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
                    IsDeleted = false

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
        private string GenerateHashCode(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        private string GenerateFormattedString()
        {
            string[] segments = new string[5];
            Random random = new Random();

            for (int i = 0; i < segments.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        segments[i] = GenerateRandomAlphanumeric(5, random);
                        break;
                    case 1:
                        segments[i] = GenerateRandomAlphanumeric(8, random);
                        break;
                    case 2:
                        segments[i] = GenerateRandomAlphanumeric(7, random);
                        break;
                    case 3:
                        segments[i] = "fjfjfjjfl9999";
                        break;
                    case 4:
                        segments[i] = "3456";
                        break;
                }
            }

            return string.Join("-", segments);
        }

        private string GenerateRandomAlphanumeric(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public string GiveGuardCourseCompletedNotification(int guardId, string hrdesription)
        {
            var guardDetails = _guardDataProvider.GetGuardDetailsUsingId(guardId).FirstOrDefault();
            var sb = new StringBuilder();

            var messageBody = string.Empty;
            messageBody = $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Name of Guard</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardDetails.Name}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>License</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardDetails.SecurityNo}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Provider</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{guardDetails.Provider}</td>";
            messageBody = messageBody + $" <tr><td style=\"width:2% ;border: 1px solid #000000;\"><b>Course</b></td><td style=\"width:5% ;border: 1px solid #000000;\">{hrdesription}</td>";

            sb.Append("Hi , <br/><br/>The following guard successfully completed a course <br/><br/>");
            sb.Append(" <table width=\"50%\" cellpadding=\"5\" cellspacing=\"5\" border=\"1\" style=\"border:ridge;border-color:#000000;border-width:thin\">");
            sb.Append(" <tr><td style=\"width:2% ;border: 1px solid #000000;text-align:center \" colspan=\"2\"><b>Guard Details</b></td></tr>");
            sb.Append(messageBody);
            sb.Append("");


            //mailBodyHtml.Append("");
            return sb.ToString();
        }
        private void SendEmailNew(string mailBodyHtml)
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


            message.Subject = "New Certificate Issued";
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

        public JsonResult OnGetLogActivityProfilesList()
        {
            List<MobileLogActivityProfile> logActivityProfiles = new List<MobileLogActivityProfile>();
            logActivityProfiles = _guardLogDataProvider.GetMobileLogActivityProfiles();

            return new JsonResult(logActivityProfiles);
        }

        public JsonResult OnPostAddLogActivityProfile(string ProfileName)
        {
            var status = true;
            var message = "Success: ";
            MobileLogActivityProfile _profile = null;
            try
            {
                if(string.IsNullOrEmpty(ProfileName))
                {
                    status = false;
                    message = "Error: Profile name cannot be empty.";
                    return new JsonResult(new { success = status, message = message });
                }
                _profile = _guardLogDataProvider.SaveLogActivityProfile(ProfileName,out string msg);
                if (_profile!= null)
                {
                    message += msg;
                }
                else
                {
                    status = false;
                    message = $"Error: {msg}";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = $"Error: {ex.Message}";
            }
            return new JsonResult(new { success = status, message = message, id = _profile?.Id, name= _profile?.ProfileName });
        }
        public JsonResult OnPostUpdateLogActivityProfile(MobileLogActivityProfile _Profile)
        {
            var status = true;
            var message = "Success: ";
            MobileLogActivityProfile _rtnProfile = null;
            try
            {
                if(_Profile == null || string.IsNullOrEmpty(_Profile.ProfileName))
                {
                    status = false;
                    message = "Error: Invalid profile.";
                    return new JsonResult(new { success = status, message = message });
                }
                _rtnProfile = _guardLogDataProvider.UpdateLogActivityProfile(_Profile, out string msg);
                if (_rtnProfile != null)
                {
                    message += msg;
                }
                else
                {
                    status = false;
                    message = $"Error: {msg}";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = $"Error: {ex.Message}";
            }
            return new JsonResult(new { success = status, message = message, id = _rtnProfile?.Id, name = _rtnProfile?.ProfileName });
        }
        public JsonResult OnPostDeleteLogActivityProfile(int _Profile)
        {
            var status = true;
            var message = "Profile deleted successfully.";
            try
            {
                if(_Profile <= 0)
                {
                    status = false;
                    message = "Error: Invalid profile.";
                    return new JsonResult(new { success = status, message = message });
                }

                status = _guardLogDataProvider.DeleteLogActivityProfile(_Profile, out string msg);
                if (status) { 
                    message = msg;
                }
                else
                {                    
                    message = $"Error: {msg}";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = $"Error: {ex.Message}";
            }
            return new JsonResult(new { success = status, message = message });
        }


        public JsonResult OnGetRcClientAccessByGuardId(int guardId)
        {
            return new JsonResult(_viewDataService.GetGuardRcClientSiteAccess(guardId));
        }
        
        public JsonResult OnPostRcClientAccessByGuardId(int guardId, int[] selectedSites)
        {
            var status = true;
            var message = "Success";
            try
            {
                if(guardId <= 0)
                {
                    status = false;
                    message = "Error: Invalid guard ID.";
                    return new JsonResult(new { status = status, message = message });
                }


                var clientSiteAccess = selectedSites.Select(x => new GuardRcClientSiteAccess()
                {
                    ClientSiteId = x,
                    GuardId = guardId
                }).ToList();
                _guardDataProvider.SaveGuardRcClientSiteAccess(guardId, clientSiteAccess);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        // ClearAllRcSiteAccessFromGuard
        public JsonResult OnPostClearAllRcSiteAccessFromGuard(int guardId)
        {
            var status = true;
            var message = "Success";
            try
            {                
                _guardDataProvider.RemoveGuardRcClientSiteAccess(guardId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status = status, message = message });
        }

        public JsonResult OnPostSaveDuplicateTQAnswers(TrainingTestQuestions testquestions, List<TrainingTestQuestionsAnswers> testquestionanswers)
        {
            var success = false;
            var message = string.Empty;
            try
            {
                testquestions.QuestionNoId = _guardLogDataProvider.GetLatestQuestionNumber(testquestions.HRSettingsId, testquestions.TQNumberId);
                int id = _guardLogDataProvider.SaveTestQuestions(testquestions);
                if (id != 0)
                {
                    foreach (var item in testquestionanswers)
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
            return new JsonResult(new { success, message, testquestions.QuestionNoId });
        }


        public JsonResult OnGetPayRatesList(int? page, int? pageNo, int? limit, string searchString, int? groupId)
        {
            var data = _configDataProvider.GetPayRates();
            
            if (groupId.HasValue && groupId > 0)
            {
                data = data.Where(x => x.PayRateGroupId == groupId.Value).ToList();
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                data = data.Where(x => (x.Description != null && x.Description.ToLower().Contains(searchString)) || 
                                      (x.Currency != null && x.Currency.ToLower().Contains(searchString)) ||
                                      (x.PayRateGroup != null && x.PayRateGroup.Name.ToLower().Contains(searchString))).ToList();
            }

            var total = data.Count();

            // Support both 'page' and 'pageNo' as some parts of the system use different naming conventions
            int currentPage = page ?? pageNo ?? 1;
            int pageSize = limit ?? 10;

            int skip = (currentPage - 1) * pageSize;
            var records = data.Skip(skip).Take(pageSize).Select(x => new {
                x.Id,
                x.Description,
                x.PayRateGroupId,
                GroupName = x.PayRateGroup != null ? x.PayRateGroup.Name : "No Group",
                x.SellRateToClient,
                x.Comms1,
                x.Comms2,
                x.GuardPayRate,
                x.Currency,
                x.IsDeleted
            }).ToList();

            return new JsonResult(new { records = records, total = total });
        }

        public IActionResult OnGetPayRatesExport(string searchString)
        {
            var data = _configDataProvider.GetPayRates();
             if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                data = data.Where(x => (x.Description != null && x.Description.ToLower().Contains(searchString)) || (x.Currency != null && x.Currency.ToLower().Contains(searchString))).ToList();
            }

            using (var mem = new MemoryStream())
            {
                using (var spreadsheetDocument = SpreadsheetDocument.Create(mem, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = spreadsheetDocument.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();

                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = new Worksheet(new SheetData());

                    var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                    var sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Renumeration – Pay Rates" };
                    sheets.Append(sheet);

                    var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    // Header Row
                    var headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
                    headerRow.Append(
                        new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue("Description"), DataType = CellValues.String },
                        new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue("Sell Rate to Client"), DataType = CellValues.String },
                        new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue("Comms 1"), DataType = CellValues.String },
                        new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue("Comms 2"), DataType = CellValues.String },
                        new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue("Guard Pay Rate"), DataType = CellValues.String },
                        new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue("Currency"), DataType = CellValues.String }
                    );
                    sheetData.Append(headerRow);

                    // Data Rows
                    foreach (var item in data)
                    {
                         var row = new DocumentFormat.OpenXml.Spreadsheet.Row();
                        row.Append(
                            new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue(item.Description ?? ""), DataType = CellValues.String },
                            new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue(item.SellRateToClient.ToString()), DataType = CellValues.Number },
                            new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue(item.Comms1.ToString()), DataType = CellValues.Number },
                            new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue(item.Comms2.ToString()), DataType = CellValues.Number },
                            new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue(item.GuardPayRate.ToString()), DataType = CellValues.Number },
                             new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new CellValue(item.Currency ?? ""), DataType = CellValues.String }
                        );
                        sheetData.Append(row);
                    }
                }

                return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Renumeration_PayRates.xlsx");
            }
        }

        public JsonResult OnPostSavePayRate(PayRate payRate)
        {
             var success = false;
            var message = "Saved successfully";
            try
            {
                _configDataProvider.SavePayRate(payRate);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeletePayRate(int id)
        {
            var success = false;
            var message = "Deleted successfully";
            try
            {
                _configDataProvider.DeletePayRate(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnGetPayRateGroupsList()
        {
            try
            {
                var data = _configDataProvider.GetPayRateGroups().Select(x => new
                {
                    x.Id,
                    x.Name,
                    AssignedSites = x.PayRateGroupSites?.Select(s => new { s.ClientSiteId, s.ClientSite?.Name }).ToList()
                }).ToList();
                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        public JsonResult OnGetPayRateGroupAssignments(int groupId)
        {
            try
            {
                var groupAssignments = _context.PayRateGroupSites
                    .Where(x => x.PayRateGroupId == groupId)
                    .Select(x => x.ClientSiteId)
                    .ToList();

                var results = _context.ClientSites
                    .Include(x => x.ClientType)
                    .Where(x => x.IsActive)
                    .AsEnumerable() // Move to memory for safe null handling and grouping
                    .GroupBy(x => x.ClientType?.Name ?? "Uncategorized")
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Name = g.Key,
                        ClientSites = g.OrderBy(s => s.Name).Select(s => new
                        {
                            Id = s.Id,
                            s.Name,
                            Checked = groupAssignments.Contains(s.Id)
                        }).ToList()
                    })
                    .ToList();

                return new JsonResult(results);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = ex.Message });
            }
        }

        public JsonResult OnPostSavePayRateGroupAssignments(int groupId, List<int> selectedSites)
        {
            try
            {
                _configDataProvider.SavePayRateGroupSites(groupId, selectedSites);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public JsonResult OnPostSavePayRateGroup(PayRateGroup group)
        {
            var success = false;
            var message = "Saved successfully";
            try
            {
                _configDataProvider.SavePayRateGroup(group);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeletePayRateGroup(int id)
        {
            var success = false;
            var message = "Deleted successfully";
            try
            {
                _configDataProvider.DeletePayRateGroup(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }


        public JsonResult OnGetAllowancesList(int? page, int? pageNo, int? limit, string searchString)
        {
            var data = _configDataProvider.GetAllowances();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                data = data.Where(x => (x.Description != null && x.Description.ToLower().Contains(searchString)) ||
                                      (x.FQ != null && x.FQ.ToLower().Contains(searchString)) ||
                                      (x.Currency != null && x.Currency.ToLower().Contains(searchString))).ToList();
            }

            var total = data.Count();

            int currentPage = page ?? pageNo ?? 1;
            int pageSize = limit ?? 10;

            int skip = (currentPage - 1) * pageSize;
            var records = data.Skip(skip).Take(pageSize).Select(x => new {
                x.Id,
                x.Description,
                x.FQ,
                x.SellRateToClient,
                x.Comms1,
                x.Comms2,
                x.GuardPayRate,
                x.Currency,
                x.IsDeleted
            }).ToList();

            return new JsonResult(new { records = records, total = total });
        }

        public JsonResult OnPostSaveAllowance(Allowance allowance)
        {
            var success = false;
            var message = "Saved successfully";
            try
            {
                _configDataProvider.SaveAllowance(allowance);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteAllowance(int id)
        {
            var success = false;
            var message = "Deleted successfully";
            try
            {
                _configDataProvider.DeleteAllowance(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetOnBoardingUsers()
        {
            try
            {
                string searchTerm = "onboarding";

                var users = _userDataProvider.GetUsers()
                    .Where(x => string.IsNullOrEmpty(searchTerm) ||
                                x.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .Select(x => new
                    {
                        x.Id,
                        x.UserName,
                        x.IsDeleted,
                        x.LastLoginDate,
                        x.LastLoginIPAdress,
                        x.FormattedLastLoginDate
                    });

                var results = new List<object>();

                foreach (var user in users)
                {
                    var thirdPartyID = _userDataProvider.GetUserClientSiteAccessThirdParty(user.Id);

                    var allUserAccess = _userDataProvider.GetUserClientSiteAccess(user.Id);

                    var currUserAccess = allUserAccess.Where(x => x.UserId == user.Id);

                    int[] clientsites = currUserAccess
                        .Select(y => y.ClientSiteId)
                        .ToArray();

                    var firstClientSiteId = clientsites.FirstOrDefault();

                    CriticalDocuments documentDto = null;

                    if (firstClientSiteId != 0)
                    {
                        var criticalDocs = _configDataProvider
                            .GetCriticalDocsByClientSiteId(firstClientSiteId)?
                            .Select(z => CriticalDocumentViewModel.FromDataModel(z))
                            .FirstOrDefault();

                        if (criticalDocs != null)
                        {
                            var document = _configDataProvider.GetCriticalDocById(criticalDocs.Id);

                            if (document != null)
                            {
                                documentDto = new CriticalDocuments
                                {
                                    Id = document.Id,
                                    ClientTypeId = document.ClientTypeId,
                                    HRGroupID = document.HRGroupID,
                                    GroupName = document.GroupName,
                                    IsCriticalDocumentDownselect = document.IsCriticalDocumentDownselect,

                                    CriticalDocumentsClientSites = document.CriticalDocumentsClientSites?
                                        .Select(cs => new CriticalDocumentsClientSites
                                        {
                                            Id = cs.Id,
                                            ClientSiteId = cs.ClientSiteId,
                                            ClientSite = cs.ClientSite == null ? null : new ClientSite
                                            {
                                                Id = cs.ClientSite.Id,
                                                Name = cs.ClientSite.Name
                                            }
                                        }).ToList(),

                                    CriticalDocumentDescriptions = document.CriticalDocumentDescriptions?
                                        .Select(desc => new CriticalDocumentDescriptions
                                        {
                                            Id = desc.Id,
                                            DescriptionID = desc.DescriptionID,

                                            HRSettings = desc.HRSettings == null ? null : new HrSettings
                                            {
                                                Id = desc.HRSettings.Id,
                                                Description = desc.HRSettings.Description,

                                                ReferenceNoNumbers = desc.HRSettings.ReferenceNoNumbers == null
                                                    ? null
                                                    : new ReferenceNoNumbers
                                                    {
                                                        Id = desc.HRSettings.ReferenceNoNumbers.Id,
                                                        Name = desc.HRSettings.ReferenceNoNumbers.Name
                                                    },

                                                ReferenceNoAlphabets = desc.HRSettings.ReferenceNoAlphabets == null
                                                    ? null
                                                    : new ReferenceNoAlphabets
                                                    {
                                                        Id = desc.HRSettings.ReferenceNoAlphabets.Id,
                                                        Name = desc.HRSettings.ReferenceNoAlphabets.Name
                                                    },

                                                HRGroups = desc.HRSettings.HRGroups == null
                                                    ? null
                                                    : new HRGroups
                                                    {
                                                        Id = desc.HRSettings.HRGroups.Id,
                                                        Name = desc.HRSettings.HRGroups.Name,
                                                        IsDeleted = desc.HRSettings.HRGroups.IsDeleted
                                                    }
                                            }
                                        }).ToList()
                                };
                            }
                        }
                    }

                    results.Add(new
                    {
                        user.Id,
                        user.UserName,
                        user.IsDeleted,
                        user.LastLoginDate,
                        user.LastLoginIPAdress,
                        user.FormattedLastLoginDate,

                        //ClientTypeCsv = GetFormattedClientTypes(currUserAccess),
                        //ClientSiteCsv = GetFormattedClientSites(currUserAccess),

                        //ThirdParty = (thirdPartyID != null && thirdPartyID.ThirdPartyID != 0)
                        //    ? thirdPartyID.ThirdPartyID
                        //    : null,

                        CriticalDocs = documentDto
                    });
                }

                return new JsonResult(results);
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
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
        private string GetFormattedClientSites(IEnumerable<UserClientSiteAccess> userClientSiteAccess)
        {
            var clientSites = userClientSiteAccess.Select(x => x.ClientSite.Name).OrderBy(x => x);
            if (clientSites.Count() == 0)
                return "None";
            if (clientSites.Count() <= 3)
                return string.Join(", ", clientSites);

            return $"{string.Join(", ", clientSites.Take(3))} and {clientSites.Count() - 3} more sites";
        }
        public JsonResult OnGetHRGroups()
        {
            
            return new JsonResult(_viewDataService.GetHRGroups());
        }
        public JsonResult OnPostSaveCriticalDocumentsForOnboardingUsers(int criticalDocId,int userId,string docIds,int hrId)
        {
                var success = true;
                var message = "Saved successfully";
            if (!string.IsNullOrEmpty(docIds))
            {
                CriticalDocumentViewModel CriticalDocModel = new CriticalDocumentViewModel();
                var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
                var currUserAccess = allUserAccess.Where(x => x.UserId == userId);
                CriticalDocModel.ClientSiteIds = currUserAccess.Select(x => x.ClientSiteId).ToArray();
                CriticalDocModel.HRGroupID = hrId;
                CriticalDocModel.ClientTypeId = currUserAccess.FirstOrDefault().ClientSite.TypeId;
                CriticalDocModel.DescriptionIds = docIds.Split(",").Select(int.Parse).ToArray();
                CriticalDocModel.Id = criticalDocId;
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(CriticalDocModel, new ValidationContext(CriticalDocModel), results, true))
                    return new JsonResult(new { success = false, message = string.Join(",", results.Select(z => z.ErrorMessage).ToArray()) });

            
                try
                {
                    var CriticalDoc = CriticalDocumentViewModel.ToDataModel(CriticalDocModel);
                    _configDataProvider.SaveCriticalDoc(CriticalDoc, true);

                }

                catch (Exception ex)
                {
                    success = false;
                    message = ex.Message;
                }
            }
            else
            {
                try
                {
                    _configDataProvider.DeleteCriticalDoc(criticalDocId);
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Error " + ex.Message;
                }
            }
                return new JsonResult(new { success, message });
        }
        public JsonResult OnPostDeleteCriticalDocumentsForOnboardingUsers(int criticalDocId)
        {
            var status = true;
            var message = "Success";
            try
            {
                _configDataProvider.DeleteCriticalDoc(criticalDocId);
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }

            return new JsonResult(new { status, message });
        }
        public JsonResult OnPostSaveOnboardUsersTrainingAndAssessmentTab(int HRSettingsId, int UserId, int TrainingCourseStatusId)
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
                    var result = _guardDataProvider.GetOnBoardUsersTrainingAndAssessment(UserId).Where(x => x.TrainingCourseId == TrainingCourseId).ToList();
                    int id = 0;
                    if (result.Count > 0)
                    {
                        id = result.FirstOrDefault().Id;
                    }
                    _configDataProvider.SaveOnboardUsersTrainingAndAssessmentTab(new OnBoardUsersTrainingAndAssessment()
                    {
                        Id = id,
                        UserId = UserId,
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
        public JsonResult OnPostDeleteOnboardUsersCourseByAdmin(int Id)
        {
            var success = false;
            var message = string.Empty;
            try
            {

                //int id = _guardLogDataProvider.SaveTestQuestions(testquestions);
                if (Id != 0)
                {

                    _guardLogDataProvider.DeleteOnBoardUsersCourseByAdmin(Id);
                }
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetOnboardTrainingCourses(int userId)
        {
            try
            {
                
               
                 
                    var trainingCourses = _guardDataProvider.GetOnBoardUsersTrainingAndAssessment(userId).ToList();


                    
                
                return new JsonResult(trainingCourses);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex);
            }
        }
        public JsonResult OnGetOnBoardingUserClientSiteAccsess(int userId)
        {
            try
            {


                var results = new List<object>();
                var ThirdPartyID = _userDataProvider.GetUserClientSiteAccessThirdParty(userId);
                var allUserAccess = _userDataProvider.GetUserClientSiteAccess(userId);
                var currUserAccess = allUserAccess.Where(x => x.UserId == userId);
               

                results.Add(new
                {
                    
                    ClientTypeCsv = GetFormattedClientTypes(currUserAccess),
                    ClientSiteCsv = GetFormattedClientSites(currUserAccess),
                    ThirdParty = (ThirdPartyID != null && ThirdPartyID.ThirdPartyID != 0) ? ThirdPartyID.ThirdPartyID : null

                });



                return new JsonResult(results);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex);
            }
        }

        public async Task<IActionResult> OnPostUploadWelcomePackZipAsync(IFormFile welcomePackZipFile)
        {
            if (welcomePackZipFile != null && welcomePackZipFile.Length > 0)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var folderPath = Path.Combine(webRootPath, "WelcomePack");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, "DataPack.zip");
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await welcomePackZipFile.CopyToAsync(fileStream);
                }
                return new JsonResult(new { success = true });
            }

            return new JsonResult(new { success = false, message = "No file selected." });
        }
    }
    public class helpDocttype
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

}
