using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace CityWatch.Web.Pages.Admin
{
    public class MobileAppUpgradeModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IWebHostEnvironment _env;

        //[BindProperty]
        //public IFormFile AppVersionFileUpload { get; set; }
        public MobileAppUpgradeModel(IViewDataService viewDataService, IWebHostEnvironment env)
        {
            _viewDataService = viewDataService;
            _env = env; 
        }


        public void OnGet()
        {
        }


        public JsonResult OnGetAllAppVersionDetails(bool active)
        {
            var r = _viewDataService.GetAllMobileAppVersion().Where(x => x.IsActive == active).ToList();
            return new JsonResult(r);
        }

        public async Task<IActionResult> OnPostNewAppVersionUpload()
        {
            var files = Request.Form.Files;

            var AppType = Request.Form["AppType"].ToString();
            var AppVersionMajor = int.Parse(Request.Form["AppVersionMajor"]);
            var AppVersionMinor = int.Parse(Request.Form["AppVersionMinor"]);
            var AppVersionPatch = int.Parse(Request.Form["AppVersionPatch"]);
            var AppVersionNotes = Request.Form["AppVersionNotes"].ToString();
            //var AppVersionFileUpload = Request.Form.Files["AppVersionFileUpload"];
            var AppVersionFileUpload = files[0];

            // Need to upload the app apk file
            var success = false;
            var message = "Mobile App Version Saved Successfully.";
            MobileAppUpgrade data;

            try
            {
                data = new MobileAppUpgrade();
                data.AppType = AppType;
                data.AppVersionMajor = AppVersionMajor;
                data.AppVersionMinor = AppVersionMinor;
                data.AppVersionPatch = AppVersionPatch;
                data.AppVersionNotes = AppVersionNotes;
                data.TotalDownloadCount = 0;
                

                // Handle file upload if exists
                if (AppVersionFileUpload != null && AppVersionFileUpload.Length > 0)
                {
                    // Validate file extension
                    var allowedExtensions = new[] { ".apk" };
                    var fileExtension = Path.GetExtension(AppVersionFileUpload.FileName).ToLower();

                    if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
                    {
                        return new JsonResult(new { success = false, message = "Invalid file type" })
                        {
                            StatusCode = 400
                        };
                    }

                    string folder = Path.Combine(_env.WebRootPath, $"Downloads\\MobileApp\\{AppType}\\{AppVersionMajor}.{AppVersionMinor}.{AppVersionPatch}\\");
                    Directory.CreateDirectory(folder);

                    string fileName = $"{AppVersionFileUpload.FileName}";
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await AppVersionFileUpload.CopyToAsync(stream);
                    }

                    data.FileName = fileName;
                    data.AppDownloadUrl = $"{GetCurrentUrl()}/api/AppUpgrade/DownloadMobileApp?platform={AppType}";
                    _viewDataService.SaveMobileAppUpgrade(data);
                    success = true;
                }
                else
                {
                    message = "No file uploaded. Please upload the APK file.";
                }
                
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success = success, message = message });            
        }
                
        public JsonResult OnPostDeleteAppVersion(int id)
        {
            var success = false;
            var message = "Mobile App Version Deleted Successfully.";
            try
            {
                _viewDataService.DeleteMobileAppUpgrade(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success = success, message = message });
        }

        public JsonResult OnPostRollBackToAppVersion(int recordId)
        {
            var success = false;
            var message = "Mobile App Version Rollbacked Successfully.";
            try
            {
                _viewDataService.RollBackToVersion(recordId);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success = success, message = message });
        }

        private string GetCurrentUrl()
        {
            var request = HttpContext.Request;

            return $"{request.Scheme}://{request.Host}";
        }


    }
}
