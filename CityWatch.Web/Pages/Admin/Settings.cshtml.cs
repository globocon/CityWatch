using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using Dropbox.Api.Files;
using MailKit.Search;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using static Dropbox.Api.TeamLog.ActorLogInfo;
using static Dropbox.Api.TeamLog.EventCategory;

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

        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IViewDataService viewDataService,
            IGuardLogDataProvider guardLogDataProvider)
        {
            _guardLogDataProvider = guardLogDataProvider;
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _userDataProvider = userDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _viewDataService = viewDataService;
        }
        public string IsAdminminOrPoweruser=string.Empty;
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

        public IActionResult OnGet()
        {
            if (!AuthUserHelper.IsAdminUserLoggedIn && !AuthUserHelper.IsAdminGlobal && !AuthUserHelper.IsAdminPowerUser)
            {
                return Redirect(Url.Page("/Account/Unauthorized"));
            }
            else
            {

                ReportTemplate = _configDataProvider.GetReportTemplate();
                return Page();

            }
        }

        public JsonResult OnGetClientTypes(int? page, int? limit)
        {
            return new JsonResult(_viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId));
        }

        public JsonResult OnGetClientSites(int? page, int? limit, int? typeId, string searchTerm,string searchTermtwo)
        {
            return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, AuthUserHelper.LoggedInUserId, searchTerm, searchTermtwo));
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
        public JsonResult OnGetStaffDocs()
        {
            return new JsonResult(_configDataProvider.GetStaffDocuments());
        }
        public JsonResult OnGetStaffDocsUsingType(int type)
        {
            return new JsonResult(_configDataProvider.GetStaffDocumentsUsingType(type));
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

        public JsonResult OnGetUsers()
        {
            var users = _userDataProvider.GetUsers().Select(x => new { x.Id, x.UserName, x.IsDeleted });
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
                    if (status ==-1)
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
                    success= _clientDataProvider.SaveSiteLinkDetails(reportfield);
                if(success!=1)
                {   if (success == 2)
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
       
        public JsonResult OnGetUserClientAccess()
        {
            return new JsonResult(_viewDataService.GetAllUsersClientSiteAccess());
        }

        public JsonResult OnGetClientAccessByUserId(int userId)
        {
            return new JsonResult(_viewDataService.GetUserClientSiteAccess(userId));
        }

        public JsonResult OnPostClientAccessByUserId(int userId, int[] selectedSites)
        {
            var status = true;
            var message = "Success";
            try
            {
                var clientSiteAccess = selectedSites.Select(x => new UserClientSiteAccess()
                {
                    ClientSiteId = x,
                    UserId = userId
                }).ToList();
                _userDataProvider.SaveUserClientSiteAccess(userId, clientSiteAccess);
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
            int CountPSPF= _configDataProvider.GetLastValue();
            if (record.IsDefault==true && CountPSPF >= 1)
            {
                _configDataProvider.UpdateDefault();
            }
            var PsPFName = _configDataProvider.GetPSPFName(record.Name);
            
            if (record.Id== -1)
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
                if (PsPFName==record.Name && record.Id == -1)
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
                var info = new IncidentReportField { Id = maxId, Name = Email, TypeId=ReportFieldType.Reimburse, EmailTo="" };
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

        public JsonResult OnGetClientSitesNew(string typeId)
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
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
        public JsonResult OnGetCriticalDocumentList(int type, string searchTerm)
        {
            int GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId == 0)
            {
                var ddd = _configDataProvider.GetCriticalDocs()
                    .Select(z => CriticalDocumentViewModel.FromDataModel(z));
                return new JsonResult(_configDataProvider.GetCriticalDocs()
                    .Select(z => CriticalDocumentViewModel.FromDataModel(z)));
                   

            }
            else
            {
                return new JsonResult(_configDataProvider.GetCriticalDocs()
                   .Select(z => CriticalDocumentViewModel.FromDataModel(z)));
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
                return new JsonResult(_configDataProvider.GetCriticalDocById(id));
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
    }
}
