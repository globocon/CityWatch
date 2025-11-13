using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace CityWatch.Web.Pages.Admin
{
    public class MobileAppUpgradeModel : PageModel
    {
        private readonly IViewDataService _viewDataService;

        public MobileAppUpgradeModel(IViewDataService viewDataService)
        {            
            _viewDataService = viewDataService;           
        }


        public void OnGet()
        {
        }


        public JsonResult OnGetAllAppVersionDetails(bool active)
        {
            var r = _viewDataService.GetAllMobileAppVersion().Where(x => x.IsActive == active).ToList();
            return new JsonResult(r);
        }

        public JsonResult OnPostSaveAppVersion(MobileAppUpgrade record)
        {

            // Need to upload the app apk file
            var success = false;
            var message = "Mobile App Version Saved Successfully.";
            try
            {
                _viewDataService.SaveMobileAppUpgrade(record);
                success = true;                
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }

        public JsonResult OnPostDeleteAppVersion(int id)
        {
            var success = false;
            var message = "Mobile App Version De-activated Successfully.";
            try
            {
                _viewDataService.DeleteMobileAppUpgrade(id);
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new JsonResult(new { success, message });
        }
    }
}
