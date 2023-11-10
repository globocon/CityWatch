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
            /* The following changes done for allowing guard to access the KPI*/
            var claimsIdentity = User.Identity as ClaimsIdentity;
            /* For Guard Login using securityLicenseNo*/
            string securityLicenseNo = Request.Query["Sl"];
            string LoginGuardId = Request.Query["guid"];
            /* For Guard Login using securityLicenseNo the office staff UserId*/
            string loginUserId = Request.Query["lud"];
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
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
