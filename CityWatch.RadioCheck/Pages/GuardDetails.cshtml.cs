using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using static Dropbox.Api.TeamLog.EventCategory;
using MailKit.Net.Smtp;
using CityWatch.RadioCheck.Services;

namespace CityWatch.Web.Pages.Radio
{
    public class GuardDetails : PageModel
    {



        private readonly IViewDataService _viewDataService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly EmailOptions _EmailOptions;
        private readonly IConfiguration _configuration;
        public GuardDetails(IGuardLogDataProvider guardLogDataProvider, IOptions<EmailOptions> emailOptions,
            IConfiguration configuration, IViewDataService viewDataService)
        {

            _guardLogDataProvider = guardLogDataProvider;
            _EmailOptions = emailOptions.Value;
            _configuration = configuration; 
            _viewDataService = viewDataService;

        }
        public int UserId { get; set; }
        public int GuardId { get; set; }
        public string SelectedGuardId { get; set; }
        public int InActiveGuardCount { get; set; }

        public int ActiveGuardCount { get; set; }
        public IActionResult OnGet()
        {

            var activeGuardDetails = _guardLogDataProvider.GetActiveGuardDetails();
            ActiveGuardCount = activeGuardDetails.Count();
            var inActiveGuardDetails = _guardLogDataProvider.GetInActiveGuardDetails();
            InActiveGuardCount = inActiveGuardDetails.Count();


            var guardLoginId = HttpContext.Session.GetInt32("GuardLoginId");
            /* The following changes done for allowing guard to access the KPI*/
            var claimsIdentity = User.Identity as ClaimsIdentity;
            /* For Guard Login using securityLicenseNo*/
            string securityLicenseNo = Request.Query["Sl"];
            string LoginGuardId = Request.Query["guid"];
            /* For Guard Login using securityLicenseNo the office staff UserId*/
            string loginUserId = Request.Query["lud"];
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            SelectedGuardId = Request.Query["gId"];
            if (!string.IsNullOrEmpty(SelectedGuardId))
            {

                HttpContext.Session.SetInt32("SelectedGuardId", Convert.ToInt32(SelectedGuardId));
            }
            else
            {
                SelectedGuardId = "0";
                HttpContext.Session.SetInt32("SelectedGuardId", Convert.ToInt32(SelectedGuardId));
            }
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
        public JsonResult OnGetGuards()
        {
            return new JsonResult(new { data = _viewDataService.GetGuards() });
        }
  
        public JsonResult OnGetActiveGuards()
        {
            var guardLoginId = HttpContext.Session.GetInt32("SelectedGuardId");
            if (guardLoginId !=0)
            {
                return new JsonResult(new { data = _viewDataService.GetActiveGuards().Where(z=>z.Id==guardLoginId) });
            }
            return new JsonResult(new { data = _viewDataService.GetActiveGuards() });
        }
        public JsonResult OnGetCrmSupplierData(string companyName)
        {
            return new JsonResult(_guardLogDataProvider.GetCompanyDetailsVehLog(companyName));
        }
        public IActionResult OnGetLastTimeLogin(int guardId)
        {

            return new JsonResult(_guardLogDataProvider.GetLastLoginNew(guardId));
        }

    }
}
