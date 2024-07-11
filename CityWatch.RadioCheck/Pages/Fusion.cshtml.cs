using CityWatch.Data.Models;
using CityWatch.Web.Models;
using CityWatch.RadioCheck.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace CityWatch.RadioCheck.Pages
{
    public class FusionModel : PageModel
    {
       
        
        private readonly IGuardLogZipGenerator _guardLogZipGenerator;
        private readonly IAuditLogViewDataService _auditLogViewDataService;
        private readonly IClientSiteViewDataService _clientViewDataService;

        public FusionModel(
           
            IGuardLogZipGenerator guardLogZipGenerator,
            IAuditLogViewDataService auditLogViewDataService,
            IClientSiteViewDataService clientViewDataService)
        {
           
            
            _guardLogZipGenerator = guardLogZipGenerator;
            _auditLogViewDataService = auditLogViewDataService;
            _clientViewDataService = clientViewDataService;
        }

        public KeyVehicleLogAuditLogRequest KeyVehicleLogAuditLogRequest { get; set; }

        public ActionResult OnGet()
        {
          
            return Page();
        }

      
       

       
       
      

     
       
        public JsonResult OnGetClientSites(string types)
        {
            return new JsonResult(_clientViewDataService.GetUserClientSitesWithId(types).OrderBy(z => z.Text));
        }

       

    
      
        

        //fusion Start
        public JsonResult OnGetDailyGuardFusionSiteLogs(int pageNo, int limit, int clientSiteId,
                                                    DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            var start = (pageNo - 1) * limit;
            var dailyGuardLogs = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs);
            var records = dailyGuardLogs.Skip(start).Take(limit).ToList();
            return new JsonResult(new { records, total = dailyGuardLogs.Count });
        }


        public JsonResult OnPostDownloadDailyFusionGuardLogZip(int clientSiteId, DateTime logFromDate, DateTime logToDate)
        {
            var success = true;
            var message = string.Empty;
            var zipFileName = string.Empty;

            try
            {
                zipFileName = _guardLogZipGenerator.GenerateFusionZipFile(new int[] { clientSiteId }, logFromDate, logToDate, LogBookType.DailyGuardLog).Result;
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.Message;

                if (ex.InnerException != null)
                    message = ex.InnerException.Message;
            }

            return new JsonResult(new { success, message, fileName = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
        }
        //fusion end
    }
}