using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;

namespace CityWatch.Web.Pages.Admin
{
    public class SettingsModel : PageModel
    {
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IUserDataProvider _userDataProvider;
        public readonly IConfigDataProvider _configDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;

        public SettingsModel(IWebHostEnvironment webHostEnvironment,
            IClientDataProvider clientDataProvider,
            IConfigDataProvider configDataProvider,
            IUserDataProvider userDataProvider,
            IViewDataService viewDataService)
        {
            _clientDataProvider = clientDataProvider;
            _configDataProvider = configDataProvider;
            _userDataProvider = userDataProvider;
            _webHostEnvironment = webHostEnvironment;
            _viewDataService = viewDataService;
        }

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
            if (!AuthUserHelper.IsAdminUserLoggedIn)
                return Redirect(Url.Page("/Account/Unauthorized"));

            ReportTemplate = _configDataProvider.GetReportTemplate();
            return Page();
        }

        public JsonResult OnGetClientTypes(int? page, int? limit)
        {
            return new JsonResult(_viewDataService.GetUserClientTypesHavingAccess(AuthUserHelper.LoggedInUserId));
        }

        public JsonResult OnGetClientSites(int? page, int? limit, int? typeId, string searchTerm)
        {
            return new JsonResult(_viewDataService.GetUserClientSitesHavingAccess(typeId, AuthUserHelper.LoggedInUserId, searchTerm));
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
                _clientDataProvider.DeleteClientType(id);
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
                _clientDataProvider.DeleteClientSite(id);
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

        public JsonResult OnPostShowPassword(User user)
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

        public JsonResult OnPostUser(User record)
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
    }
}
