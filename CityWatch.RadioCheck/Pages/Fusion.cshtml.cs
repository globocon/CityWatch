using CityWatch.Data.Models;
using CityWatch.Web.Models;
using CityWatch.RadioCheck.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Presentation;
using CityWatch.Web.Extensions;
using Microsoft.AspNetCore.Http;
using CityWatch.Data.Providers;
using CityWatch.RadioCheck.Helpers;

namespace CityWatch.RadioCheck.Pages
{
    public class FusionModel : PageModel
    {
       
        
        private readonly IGuardLogZipGenerator _guardLogZipGenerator;
        private readonly IAuditLogViewDataService _auditLogViewDataService;
        private readonly IClientSiteViewDataService _clientViewDataService;
        private readonly IGuardDataProvider _guardDataProvider;
        public int GuardId { get; set; }
        [BindProperty]
        public string GuardIdCheck { get; set; }
        public FusionModel(
           
            IGuardLogZipGenerator guardLogZipGenerator,
            IAuditLogViewDataService auditLogViewDataService,
            IClientSiteViewDataService clientViewDataService, IGuardDataProvider guardDataProvider)
        {
           
            
            _guardLogZipGenerator = guardLogZipGenerator;
            _auditLogViewDataService = auditLogViewDataService;
            _clientViewDataService = clientViewDataService;
            _guardDataProvider = guardDataProvider;
        }

        public KeyVehicleLogAuditLogRequest KeyVehicleLogAuditLogRequest { get; set; }

        public ActionResult OnGet()
        {

            GuardId = HttpContext.Session.GetInt32("GuardId") ?? 0;
            GuardIdCheck = GuardId.ToString();
            var guard = _guardDataProvider.GetGuards().SingleOrDefault(z => z.Id == GuardId);

            if (guard != null)
            {
                if (guard.IsAdminPowerUser || guard.IsAdminGlobal)
                {
                    return Page();
                }
                else
                {
                    return Redirect(Url.Page("/Account/Unauthorized"));
                }
                
            }
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
        public JsonResult OnGetGenerateRCGraphs(int clientSiteId,
                                                    DateTime logFromDate, DateTime logToDate, bool excludeSystemLogs)
        {
            //p4-73 new piechart-start
            //duress entries per week-start
            var today = logFromDate;

            var rcChartTypesForWeekNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //var rcChartTypes = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ")));
            //var rcChartTypes1 = rcChartTypes.GroupBy(z => z.EventDateTime.DayOfWeek).ToDictionary(z => z.Key, z => (double)z.Count());
            int rcChartTypesForWeekNewCountnew = 0;
            TimeSpan ts =logToDate.Subtract(today);
            int dateDiff = ts.Days;
            int totalWeeks = (int)dateDiff / 7;
            for (int i = 1; i <= totalWeeks; i++)
            {

                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
                if (thisWeekStart < today)
                {
                    thisWeekStart = today;
                }

                if (thisWeekEnd > logToDate)
                {
                    thisWeekEnd = logToDate;
                }
                var rcChartTypesForWeek = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, thisWeekStart, thisWeekEnd,excludeSystemLogs).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ")));
                string newdaterange = thisWeekStart.ToString("dd-MM-yyy") + " to " + thisWeekEnd.ToString("dd-MM-yyy");
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForWeek.Count();
                rcChartTypesForWeekNew.Add(obj);
                rcChartTypesForWeekNewCountnew = rcChartTypesForWeekNewCountnew + obj.RecordCount;
                today = thisWeekEnd.AddDays(1);

            }
            var rcChartTypesForWeekNewCount = rcChartTypesForWeekNewCountnew;
            //duress entries per week-end


            //duress entries per month-start
            today = logFromDate;

            var rcChartTypesForMonthNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForMonthNewCountnew = 0;

            //int months = (int)(ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Month);
            int months = (logToDate.Year * 12 + logToDate.Month) - (logFromDate.Year * 12 + logFromDate.Month) + 1;
            for (int i = 1; i <= months; i++)
            {

                var thisMonthStart = new DateTime(today.Year, today.Month, 1);
                var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
                //if (thisMonthStart < today)
                //{
                //    thisMonthStart = today;
                //}

                //if (thisMonthEnd > ReportRequest.ToDate)
                //{
                //    thisMonthEnd = ReportRequest.ToDate;
                //}
                var rcChartTypesForMonth = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, thisMonthStart, thisMonthEnd,excludeSystemLogs).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
                string newdaterange = thisMonthStart.ToString("MMM");
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForMonth.Count();
                rcChartTypesForMonthNew.Add(obj);
                rcChartTypesForMonthNewCountnew = rcChartTypesForMonthNewCountnew + obj.RecordCount;
                today = thisMonthEnd.AddDays(1);

            }
            var rcChartTypesForMonthNewCount = rcChartTypesForMonthNewCountnew;
            //duress entries per month-end

            //duress entries per year-start
            today = logFromDate;

            var rcChartTypesForYearNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForYearNewCountnew = 0;

            int years = (int)(logToDate.Year - logFromDate.Year) +
        (((logToDate.Month > logFromDate.Month) ||
        ((logToDate.Month == logFromDate.Month) && (logToDate.Day >= logFromDate.Day))) ? 1 : 0);

            for (int i = 1; i <= years; i++)
            {

                var thisYearStart = new DateTime(today.Year, 1, 1);
                var thisYearEnd = new DateTime(today.Year, 12, 1);
                //if (thisYearStart < today)
                //{
                //    thisYearStart = today;
                //}

                //if (thisYearEnd > ReportRequest.ToDate)
                //{
                //    thisYearEnd = ReportRequest.ToDate;
                //}
                var rcChartTypesForYear = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, thisYearStart, thisYearEnd, excludeSystemLogs).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
                string newdaterange = thisYearStart.Year.ToString();
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForYear.Count();
                rcChartTypesForYearNew.Add(obj);
                rcChartTypesForYearNewCountnew = rcChartTypesForYearNewCountnew + obj.RecordCount;
                today = new DateTime(today.Year + 1, 1, 1);

            }
            var rcChartTypesForYearNewCount = rcChartTypesForYearNewCountnew;


            //duress entries per year-end
            //no of guards went to prelarm-start
            var rcChartTypesGuardsPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesGuardsPrealarmCountnew = 0;
            var rcChartTypesGuardsPrealarm = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs).Where(z => z.NotificationType==1).GroupBy(z => z.ClientSiteId); ;
            foreach (var item in rcChartTypesGuardsPrealarm)
            {

                string newdaterange = item.FirstOrDefault().SiteName;
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                
                    obj.DateRange = newdaterange;
                    obj.RecordCount = item.Count();

                    rcChartTypesGuardsPrealarmNew.Add(obj);

                    rcChartTypesGuardsPrealarmCountnew = rcChartTypesGuardsPrealarmCountnew + obj.RecordCount;
                

            }



            //no of guards went to prealram-end
            //no of guards went from prelarm-start
            var rcChartTypesGuardsFromPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesGuardsFromPrealarmCountnew = 0;
            var rcChartTypesGuardsFromPrealarm = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Control Room Alert:Guard Off Duty  "))).GroupBy(z => z.ClientSiteId); ;
            foreach (var item in rcChartTypesGuardsPrealarm)
            {

                string newdaterange = item.FirstOrDefault().SiteName;
                var rcChartradiochecks = _auditLogViewDataService.GetClientSiteRadioChecks(item.FirstOrDefault().ClientSite.Id, logFromDate, logToDate).Where(z => z.RadioCheckStatusId == 1);
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                if (rcChartradiochecks.Count() != 0)
                {
                    obj.DateRange = newdaterange;
                    obj.RecordCount = rcChartradiochecks.Count();

                    rcChartTypesGuardsFromPrealarmNew.Add(obj);

                    rcChartTypesGuardsFromPrealarmCountnew = rcChartTypesGuardsFromPrealarmCountnew + obj.RecordCount;
                }

            }



            //no of guards went to prealram-end
            //no of tomes cro pushed radio button -start
            var rcChartTypesCRONew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesCROCountnew = 0;
            var rcChartTypesGuardsFromCRO = _auditLogViewDataService.GetAuditGuardFusionLogs(clientSiteId, logFromDate, logToDate, excludeSystemLogs).Where(z => (z.Notes != null && z.Notes.Contains("Control Room Alert"))).GroupBy(z => z.ClientSiteId); ;

            foreach (var item in rcChartTypesGuardsFromCRO)
            {

                string newdaterange = item.FirstOrDefault().SiteName;
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                obj.DateRange = newdaterange;
                obj.RecordCount = item.Count();
                rcChartTypesCRONew.Add(obj);
                rcChartTypesCROCountnew = rcChartTypesCROCountnew + obj.RecordCount;

            }



            //no of tomes cro pushed radio button-end
            //p4 - 73 new piechart- end
            return new JsonResult(new { chartData = new {  rcChartTypesForWeekNew, rcChartTypesForMonthNew, rcChartTypesForYearNew, rcChartTypesGuardsPrealarmNew, rcChartTypesCRONew, rcChartTypesGuardsFromPrealarmNew }, rcChartTypesForWeekNewCount, rcChartTypesForMonthNewCount, rcChartTypesForYearNewCount, rcChartTypesGuardsPrealarmCountnew, rcChartTypesCROCountnew, rcChartTypesGuardsFromPrealarmCountnew });

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