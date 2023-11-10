using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;

namespace CityWatch.Web.Pages.Radio
{
    public class RadioCheckNewModel : PageModel
    {


        
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public RadioCheckNewModel(IGuardLogDataProvider guardLogDataProvider)
        {

            _guardLogDataProvider = guardLogDataProvider;
        }
        public int UserId { get; set; }
        public int GuardId { get; set; }
        public IActionResult OnGet()
        {

          

            var guardLoginId = HttpContext.Session.GetInt32("GuardLoginId");
            /* The following changes done for allowing guard to access the KPI*/
            var claimsIdentity = User.Identity as ClaimsIdentity;
            /* For Guard Login using securityLicenseNo*/
            string securityLicenseNo = Request.Query["Sl"];
            string LoginGuardId = Request.Query["guid"];
            /* For Guard Login using securityLicenseNo the office staff UserId*/
            string loginUserId = Request.Query["lud"];
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            string sidValue = "";
            var UserId1 = claimsIdentity.Claims;
            foreach (var item in UserId1)
            {
                if (item.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid")
                {
                     sidValue = item.Value;
                  
                    break;
                }
            }
            if (int.TryParse(sidValue, out int sid))
            {
                int sids = int.Parse(sidValue);
                ViewData["IsDuressEnabled"] = _guardLogDataProvider.IsRadiocheckDuressEnabled(sids);
                
            }
            

            if (!string.IsNullOrEmpty(securityLicenseNo) && !string.IsNullOrEmpty(loginUserId) && !string.IsNullOrEmpty(LoginGuardId))
            {
               
                UserId = int.Parse(loginUserId);
                GuardId = int.Parse(LoginGuardId);
                HttpContext.Session.SetInt32("GuardId", GuardId);
                return Page();
            }
            // Check if the user is authenticated(Normal Admin Login)
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {   /*Old Code for admin only*/
              
                HttpContext.Session.SetInt32("GuardId", 0);
                return Page();
            }
            else if (GuardId != 0)
            {

                HttpContext.Session.SetInt32("GuardId", GuardId);
                return Page();
            }
            else
            {
                HttpContext.Session.SetInt32("GuardId", 0);
                return Redirect(Url.Page("/Account/Login"));
            }
            
        }
        //code added to save the duress button start
        public JsonResult OnPostSaveDuress()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var UserId = claimsIdentity.Claims;
            foreach (var item in UserId)
            {
                if (item.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid")
                {
                    string sidValue = item.Value;
                    int Sid= int.Parse(sidValue);
                    var UserIDDuress = _guardLogDataProvider.UserIDDuress(Sid);
                    if (UserIDDuress==0)
                    {
                        _guardLogDataProvider.SaveRadioCheckDuress(sidValue);
                    }
                   
                    break; 
                }
            }
            

            var status = true;
            var message = "Success";
            
            ViewData["IsDuressEnabled"] = true;
            return new JsonResult(new { status, message });
        }
        //code added to save the duress button stop
        public IActionResult OnGetClientSiteActivityStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardDetails());
        }

        public IActionResult OnGetClientSiteInActivityStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetInActiveGuardDetails());
        }
        //for getting logBookDetails of Guards-start
        public IActionResult OnGetClientSitelogBookActivityStatus(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardlogBookDetails(clientSiteId, guardId));
        }

        //for getting logBookDetails of Guards-end

        //for getting Key Vehicle Details of Guards-start
        public IActionResult OnGetClientSiteKeyVehicleLogActivityStatus(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardKeyVehicleLogDetails(clientSiteId, guardId));
        }

        //for getting Key Vehicle of Guards-end
        //for getting Incident Report Details of Guards-start
        public IActionResult OnGetClientSiteIncidentReportActivityStatus(int clientSiteId, int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetActiveGuardIncidentReportDetails(clientSiteId, guardId));
        }

        //for getting Incident Report details of Guards-end

        //for getting guards not available -start

        public IActionResult OnGetClientSiteNotAvailableStatus(string clientSiteIds)
        {

            return new JsonResult(_guardLogDataProvider.GetNotAvailableGuardDetails());
        }
        //for getting guards not available -end

        public JsonResult OnGetGuardData(int id)
        {
            return new JsonResult(_guardLogDataProvider.GetGuards(id));
        }

        //SaveRadioStatus -start
        public JsonResult OnPostSaveRadioStatus(int clientSiteId, int guardId, string checkedStatus,bool active)
        {
            var success = true;
            var message = "success";
            try
            {
                _guardLogDataProvider.SaveClientSiteRadioCheck(new ClientSiteRadioCheck()
                {
                    ClientSiteId = clientSiteId,
                    GuardId = guardId,
                    Status = checkedStatus,
                    CheckedAt = DateTime.Now,
                    Active = active
                }) ;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;
            }
            return new JsonResult(new { success, message });
        }
        //SaveRadioStatus -end
    }
}
