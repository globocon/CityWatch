using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Kpi.Models;
using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace CityWatch.Kpi.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly IViewDataService _viewDataService;
        private readonly IReportGenerator _kpiReportGenerator;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly IImportJobDataProvider _importJobDataProvider;
        private readonly IImportDataService _importDataService;
        public readonly IKpiDataProvider _kpiDataProvider;
        private readonly IUserAuthenticationService _userAuthentication;

        public DashboardModel(IViewDataService viewDataService,
            IImportJobDataProvider importJobDataProvider,
            IImportDataService importDataService,
            IClientDataProvider clientDataProvider,
            IReportGenerator kpiReportGenerator,
            IKpiDataProvider kpiDataProvider)
        {
            _viewDataService = viewDataService;
            _kpiReportGenerator = kpiReportGenerator;
            _clientDataProvider = clientDataProvider;
            _importJobDataProvider = importJobDataProvider;
            _importDataService = importDataService;
            _kpiDataProvider = kpiDataProvider;
        }

        [BindProperty]
        public KpiRequest ReportRequest { get; set; }

        public int UserId { get; set; }
        public int GuardId { get; set; }
        public int ClientTypeId { get; set; }
        public int ClientSiteId { get; set; }
        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IActionResult OnGet()
        {// all qurey string check in dashbaord to avoid to show query string in url
            /* The following changes done for allowing guard to access the KPI*/
            var claimsIdentity = User.Identity as ClaimsIdentity;
            /* For Guard Login using securityLicenseNo*/
            string securityLicenseNo = Request.Query["Sl"];
            string LoginGuardId = Request.Query["guid"];
            /* For Guard Login using securityLicenseNo the office staff UserId*/
            string loginUserId = Request.Query["lud"];
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            var loginUserIdNew = HttpContext.Session.GetInt32("loginUserId") ?? 0;
            string LoginClientTypeId= Request.Query["ClientTypeId"];
            string LoginClientSiteIdId = Request.Query["ClientSiteId"];
            string type = Request.Query["type"];
            ClientTypeId = HttpContext.Session.GetInt32("ClientTypeId") ?? 0;
            ClientSiteId = HttpContext.Session.GetInt32("ClientSiteId") ?? 0;
            if (!string.IsNullOrEmpty(securityLicenseNo) && !string.IsNullOrEmpty(loginUserId) && !string.IsNullOrEmpty(LoginGuardId))
            {
                ReportRequest = new KpiRequest();
                UserId = int.Parse(loginUserId);
                GuardId = int.Parse(LoginGuardId);
                HttpContext.Session.SetInt32("GuardId", GuardId);
                HttpContext.Session.SetInt32("loginUserId", UserId);
                if (!string.IsNullOrEmpty(LoginClientTypeId))
                {
                    ClientTypeId = int.Parse(LoginClientTypeId);
                    
                    HttpContext.Session.SetInt32("ClientTypeId", ClientTypeId);

                    return Redirect(Url.Page("/Admin/Settings"));
                }
                if ( !string.IsNullOrEmpty(LoginClientSiteIdId))
                {
                    ClientSiteId = int.Parse(LoginClientSiteIdId);
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);

                }
                if(type== "settings")
                {
                    return Redirect(Url.Page("/Admin/Settings"));
                }
                else
                {
                    return Redirect(Url.Page("/Admin/Settings"));
                    //return Page();
                }
            }
            // Check if the user is authenticated(Normal Admin Login)
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {   /*Old Code for admin only*/
                ReportRequest = new KpiRequest();
                HttpContext.Session.SetInt32("GuardId", 0);
                HttpContext.Session.SetInt32("loginUserId", 0);

                if (!string.IsNullOrEmpty(LoginClientTypeId) && !string.IsNullOrEmpty(LoginClientSiteIdId))
                {
                    ClientTypeId = int.Parse(LoginClientTypeId);
                    ClientSiteId = int.Parse(LoginClientSiteIdId);
                    HttpContext.Session.SetInt32("ClientTypeId", ClientTypeId);
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);
                    return Redirect(Url.Page("/Admin/Settings"));
                }
                else
                {
                    return Redirect(Url.Page("/Admin/Settings"));
                }
                //return Page();
               

            }
            else if(GuardId!=0)
            {
             
                HttpContext.Session.SetInt32("GuardId", GuardId);
                if (loginUserIdNew != 0)
                {
                    HttpContext.Session.SetInt32("loginUserId", loginUserIdNew);
                }
                if (!string.IsNullOrEmpty(LoginClientTypeId) && !string.IsNullOrEmpty(LoginClientSiteIdId))
                {
                    ClientTypeId = int.Parse(LoginClientTypeId);
                    ClientSiteId = int.Parse(LoginClientSiteIdId);
                    HttpContext.Session.SetInt32("ClientTypeId", ClientTypeId);
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);
                    return Redirect(Url.Page("/Admin/Settings"));
                }
                else
                {
                    return Redirect(Url.Page("/Admin/Settings"));
                    //return Page();
                }
            }           
            else
            {
               
                if (!string.IsNullOrEmpty(LoginClientTypeId) && !string.IsNullOrEmpty(LoginClientSiteIdId))
                {
                    ClientTypeId = int.Parse(LoginClientTypeId);
                    ClientSiteId = int.Parse(LoginClientSiteIdId);
                    HttpContext.Session.SetInt32("ClientTypeId", ClientTypeId);
                    HttpContext.Session.SetInt32("ClientSiteId", ClientSiteId);
                    return Redirect(Url.Page("/Admin/Settings"));
                }
                else
                {
                    HttpContext.Session.SetInt32("GuardId", 0);
                    HttpContext.Session.SetInt32("loginUserId", 0);
                    HttpContext.Session.SetInt32("ClientTypeId", 0);
                    HttpContext.Session.SetInt32("ClientSiteId", 0);
                    return Redirect(Url.Page("/Account/Login"));
                }
            }
        }

        /// <summary>
        ///  Get Client Sites Using type and UserId
        /// </summary>
        /// <param name="type">Client Type</param>
        /// <param name="UserId">Login User Id(Admin/Guard)</param>
        /// <returns></returns>
        public IActionResult OnGetClientSitesUsingUserId(string type, string guardId)
        {
            if (string.IsNullOrEmpty(guardId) || guardId=="0")
            {
                return new JsonResult(_viewDataService.GetClientSites(type));
            }
            else
            {
                return new JsonResult(_viewDataService.GetClientSitesUsingLoginUserIdNew(int.Parse(guardId), type));
            }

        }
        public IActionResult OnGetClientSites(string type)
        {
            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            if (GuardId==0)
            {
                return new JsonResult(_viewDataService.GetClientSites(type));
            }
            else
            {
                return new JsonResult(_viewDataService.GetClientSitesUsingLoginUserId(GuardId, type));
            }



        }

        public IActionResult OnGetClientSiteKpiSettings(int siteId, int month, int year)
        {
            var clientSiteKpiSettings = _clientDataProvider.GetClientSiteKpiSetting(siteId);
            var lastImportDateTime = GetLastImportDateTime(siteId, month, year);
            return new JsonResult(new { clientSiteKpiSettings, lastImportDateTime });
        }

        public IActionResult OnGetLastImportDateTime(int siteId, int month, int year)
        {
            var lastImportDateTime = GetLastImportDateTime(siteId, month, year);
            return new JsonResult(new { lastImportDateTime });
        }

        public async Task<ActionResult> OnPostGenerateReport(int siteId, int month, int year, bool withImport)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            if (withImport)
            {
                var serviceLog = new KpiDataImportJob()
                {
                    ClientSiteId = siteId,
                    ReportDate = startDate,
                    CreatedDate = DateTime.Now,
                };
                var jobId = _importJobDataProvider.SaveKpiDataImportJob(serviceLog);
                await _importDataService.Run(jobId);
            }

            var clientSiteSettings = _clientDataProvider.GetClientSiteKpiSetting(siteId);
            if (clientSiteSettings != null)
            {
                var header = new
                {
                    ClientSiteSettings = clientSiteSettings,
                    Date = startDate.ToString("MMM yyyy")
                };
                var data = _viewDataService.GetKpiReportData(siteId, startDate, endDate);
                
                var fileName = _kpiReportGenerator.GeneratePdfReport(siteId, startDate, endDate);
                return new JsonResult(new { success = true, header, data, fileName });
            }
            return new JsonResult(new { success = false });
        }

        private string GetLastImportDateTime(int siteId, int month, int year)
        {
            var lastImportDateTime = "Never";
            var latestKpiImportJob = _importJobDataProvider.GetLatestKpiDataImportJob(siteId, new DateTime(year, month, 1));
            if (latestKpiImportJob != null)
                lastImportDateTime = $"{latestKpiImportJob.CompletedDate ?? latestKpiImportJob.CreatedDate:dd MMM yyyy HH:mm}";

            return lastImportDateTime;
        }

        public void OnPostUpdateActualEmployeeHours(int id, decimal? actualEmpHours)
        {
            _kpiDataProvider.UpdateActualEmployeeHours(id, actualEmpHours);
        }
    }
}
