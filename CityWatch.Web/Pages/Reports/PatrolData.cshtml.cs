using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CityWatch.Web.Pages.Reports
{
    public class PatrolDataModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IPatrolDataReportService _irChartDataService;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        public PatrolDataModel(IViewDataService viewDataService, 
            IWebHostEnvironment webHostEnvironment,
            IPatrolDataReportService irChartDataService, IIncidentReportGenerator incidentReportGenerator, IConfigDataProvider configurationProvider,IClientDataProvider clientDataProvider, IOptions<Settings> settings, IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider)
        {
            _viewDataService = viewDataService;
            _webHostEnvironment = webHostEnvironment;
            _irChartDataService = irChartDataService;
            _incidentReportGenerator = incidentReportGenerator;
            _configDataProvider = configurationProvider;
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
            _guardDataProvider = guardDataProvider;
            _guardLogDataProvider = guardLogDataProvider;
        }

        [BindProperty]
        public PatrolRequest ReportRequest { get; set; }
        public GuardViewModel guard { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }
        public ActionResult OnGet()
        {
            //if (!AuthUserHelper.IsAdminUserLoggedIn)
            //    return Redirect(Url.Page("/Account/Unauthorized"));
            if (HttpContext.Session.GetString("GuardId") != null)
            {
                var guardList = _viewDataService.GetGuards().Where(x => x.Id == Convert.ToInt32(HttpContext.Session.GetString("GuardId")));
                foreach (var item in guardList)
                {
                    guard = item;
                    
                }

                
            }
            return Page();
        }

        public IActionResult OnPostGenerateReport()
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(ReportRequest);
            var results = patrolDataReport.Results;

            //var reportFileName = results.FirstOrDefault().fileNametodownload;
            //    var sitePercentage = patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray();
            //    var areaWardPercentage = patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray();
            //    var eventTypePercentage = patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray();
            //    var eventTypeCount = patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray();
            //    var colorCodePercentage = patrolDataReport.ColorCodePercentage.OrderBy(z => z.Key).ToArray();
            //    var recordCount = patrolDataReport.ResultsCount;
            //    var colourcode = _configDataProvider.GetFeedbackTypesId("Colour Codes");
            //    var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == colourcode).ToList();

            //    var feedbackTemplatesColour = ArrageColurCode(colorCodePercentage,feedbackTemplates).ToArray();
            //    //p4-73 new piechart-start
            //    //duress entries per week-start
            //    var today = ReportRequest.FromDate;

            //    var rcChartTypesForWeekNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesForWeekNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesForWeekNewCountnew = 0;
            //    TimeSpan ts = ReportRequest.ToDate.Subtract(today);
            //    int dateDiff = ts.Days;
            //    int totalWeeks = (int)dateDiff / 7;
            //    for (int i = 1; i <= totalWeeks; i++)
            //    {

            //        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            //        var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
            //        if (thisWeekStart<today)
            //        {
            //            thisWeekStart = today;
            //        }

            //        if(thisWeekEnd > ReportRequest.ToDate)
            //        {
            //            thisWeekEnd = ReportRequest.ToDate;
            //        }
            //        var rcChartTypesForWeek = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisWeekStart, thisWeekEnd).Where(z=> (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ")));
            //        string newdaterange = thisWeekStart.ToString("dd-MM-yyy") + " to " + thisWeekEnd.ToString("dd-MM-yyy");
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = rcChartTypesForWeek.Count();
            //        rcChartTypesForWeekNewPercent.Add(obj);
            //        rcChartTypesForWeekNewCountnew = rcChartTypesForWeekNewCountnew + obj.RecordCount;
            //        today = thisWeekEnd.AddDays(1);

            //    }
            //    var rcChartTypesForWeekNewCount = rcChartTypesForWeekNewCountnew;
            //    foreach(var item in rcChartTypesForWeekNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc= (double)item.RecordCount/rcChartTypesForWeekNewCount;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1) ;
            //        rcChartTypesForWeekNew.Add(obj);
            //    }
            //    //duress entries per week-end


            //    //duress entries per month-start
            //     today = ReportRequest.FromDate;

            //    var rcChartTypesForMonthNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesForMonthNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesForMonthNewCountnew = 0;

            //    //int months = (int)(ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Month);
            //    int months=   (ReportRequest.ToDate.Year * 12 + ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Year * 12 + ReportRequest.FromDate.Month) + 1;
            //    for (int i = 1; i <= months; i++)
            //    {

            //        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            //        var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
            //        //if (thisMonthStart < today)
            //        //{
            //        //    thisMonthStart = today;
            //        //}

            //        //if (thisMonthEnd > ReportRequest.ToDate)
            //        //{
            //        //    thisMonthEnd = ReportRequest.ToDate;
            //        //}
            //        var rcChartTypesForMonth = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisMonthStart, thisMonthEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
            //        string newdaterange = thisMonthStart.ToString("MMM");
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = rcChartTypesForMonth.Count();
            //        rcChartTypesForMonthNewPercent.Add(obj);
            //        rcChartTypesForMonthNewCountnew = rcChartTypesForMonthNewCountnew + obj.RecordCount;
            //        today = thisMonthEnd.AddDays(1);

            //    }
            //    var rcChartTypesForMonthNewCount = rcChartTypesForMonthNewCountnew;
            //    foreach (var item in rcChartTypesForMonthNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesForMonthNewCount;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesForMonthNew.Add(obj);
            //    }
            //    //duress entries per month-end

            //    //duress entries per year-start
            //    today = ReportRequest.FromDate;

            //    var rcChartTypesForYearNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesForYearNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesForYearNewCountnew = 0;

            //    int years = (int)(ReportRequest.ToDate.Year - ReportRequest.FromDate.Year  ) +
            //(((ReportRequest.ToDate.Month > ReportRequest.FromDate.Month) ||
            //((ReportRequest.ToDate.Month == ReportRequest.FromDate.Month) && (ReportRequest.ToDate.Day >= ReportRequest.FromDate.Day))) ? 1 : 0);

            //    for (int i = 1; i <= years; i++)
            //    {

            //        var thisYearStart = new DateTime(today.Year, 1, 1);
            //        var thisYearEnd = new DateTime(today.Year, 12, 1);
            //        //if (thisYearStart < today)
            //        //{
            //        //    thisYearStart = today;
            //        //}

            //        //if (thisYearEnd > ReportRequest.ToDate)
            //        //{
            //        //    thisYearEnd = ReportRequest.ToDate;
            //        //}
            //        var rcChartTypesForYear = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisYearStart, thisYearEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
            //        string newdaterange = thisYearStart.Year.ToString();
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = rcChartTypesForYear.Count();
            //        rcChartTypesForYearNewPercent.Add(obj);
            //        rcChartTypesForYearNewCountnew = rcChartTypesForYearNewCountnew + obj.RecordCount;
            //        today = new DateTime(today.Year + 1, 1, 1);

            //    }
            //    var rcChartTypesForYearNewCount = rcChartTypesForYearNewCountnew;
            //    foreach (var item in rcChartTypesForYearNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;

            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesForYearNewCount;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesForYearNew.Add(obj);
            //    }

            //    //duress entries per year-end
            //    //no of guards went to prelarm-start
            //    var rcChartTypesGuardsPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesGuardsPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesGuardsPrealarmCountnew = 0;
            //    var rcChartTypesGuardsPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => z.NotificationType == 1).GroupBy(z=>z.ClientSiteId); ;
            //    foreach (var item in rcChartTypesGuardsPrealarm)
            //    {

            //        string newdaterange = item.FirstOrDefault().ClientSite.Name;
            //        //var rcChartradiochecks = _irChartDataService.GetClientSiteRadioChecks(item.FirstOrDefault().ClientSite.Id, ReportRequest.FromDate,ReportRequest.ToDate).Where(z=>z.RadioCheckStatusId==1);
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

            //            obj.DateRange = newdaterange;
            //            obj.RecordCount = item.Count();

            //        rcChartTypesGuardsPrealarmNewPercent.Add(obj);

            //            rcChartTypesGuardsPrealarmCountnew = rcChartTypesGuardsPrealarmCountnew + obj.RecordCount;


            //    }
            //    foreach (var item in rcChartTypesGuardsPrealarmNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesGuardsPrealarmCountnew;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesGuardsPrealarmNew.Add(obj);
            //    }


            //    //no of guards went to prealram-end
            //    //no of guards went from prelarm-start
            //    var rcChartTypesGuardsFromPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesGuardsFromPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesGuardsFromPrealarmCountnew = 0;
            //    var rcChartTypesGuardsFromPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Guard Off Duty (NOTE: CRO did manual stamp as Guard went home without hitting OFF DUTY which is a breach of SOP"))).GroupBy(z => z.ClientSiteId); ;
            //    foreach (var item in rcChartTypesGuardsFromPrealarm)
            //    {

            //        string newdaterange = item.FirstOrDefault().ClientSite.Name;
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

            //            obj.DateRange = newdaterange;
            //            obj.RecordCount = item.Count();

            //        rcChartTypesGuardsFromPrealarmNewPercent.Add(obj);

            //            rcChartTypesGuardsFromPrealarmCountnew = rcChartTypesGuardsFromPrealarmCountnew + obj.RecordCount;


            //    }
            //    foreach (var item in rcChartTypesGuardsFromPrealarmNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesGuardsFromPrealarmCountnew;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesGuardsFromPrealarmNew.Add(obj);
            //    }


            //    //no of guards went to prealram-end
            //    //no of tomes cro pushed radio button -start
            //    var rcChartTypesCRONew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesCRONewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesCROCountnew = 0;
            //    var rcChartTypesGuardsFromCRO = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.Notes != null && z.Notes.Contains("Control Room Alert"))).GroupBy(z => z.ClientSiteId); ;

            //    foreach (var item in rcChartTypesGuardsFromCRO)
            //    {

            //        string newdaterange = item.FirstOrDefault().ClientSite.Name;
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

            //            obj.DateRange = newdaterange;
            //            obj.RecordCount = item.Count();
            //        rcChartTypesCRONewPercent.Add(obj);
            //            rcChartTypesCROCountnew = rcChartTypesCROCountnew + obj.RecordCount;

            //    }

            //    foreach (var item in rcChartTypesCRONewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesCROCountnew;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesCRONew.Add(obj);
            //    }


            //    var activeAndInActive = GetActiveAndInactiveGuardHrReport().ToArray();
            //    var activeAndInActiveCount = activeAndInActive.Length;
            //    var yearOfOnBoarding = GetYearofOnBoardingGuardHrReport().ToArray();
            //    var yearOfOnBoardingcount = yearOfOnBoarding.Length;

            //    var yearOfOnBoradingBarChart = GetYearofOnBoardingGuardHrReportBarchart().ToArray();

            //    var genderReport = GetGenderBasedGuardHrReport().ToArray(); ;
            //    var genderReportCount = genderReport.Length;
            //no of tomes cro pushed radio button-end
            //p4 - 73 new piechart- end

            var dataTable = _viewDataService.PatrolDataToDataTable(results).Result;
            var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            if (!Directory.Exists(excelFileDir))
                Directory.CreateDirectory(excelFileDir);
            var fileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));
            return new JsonResult(new { results, fileName });
        }


        public IActionResult OnPostGenerateReportGraphFirstTab()
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(ReportRequest);
            var results = patrolDataReport.Results;

            //var reportFileName = results.FirstOrDefault().fileNametodownload;
            var sitePercentage = patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray();
            var areaWardPercentage = patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray();
            var eventTypePercentage = patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray();
            var eventTypeCount = patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray();
            var colorCodePercentage = patrolDataReport.ColorCodePercentage.OrderBy(z => z.Key).ToArray();
            var recordCount = patrolDataReport.ResultsCount;
            var colourcode = _configDataProvider.GetFeedbackTypesId("Colour Codes");
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == colourcode).ToList();

            var feedbackTemplatesColour = ArrageColurCode(colorCodePercentage, feedbackTemplates).ToArray();
            //p4-73 new piechart-start
            //duress entries per week-start
            //    var today = ReportRequest.FromDate;

            //    var rcChartTypesForWeekNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesForWeekNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesForWeekNewCountnew = 0;
            //    TimeSpan ts = ReportRequest.ToDate.Subtract(today);
            //    int dateDiff = ts.Days;
            //    int totalWeeks = (int)dateDiff / 7;
            //    for (int i = 1; i <= totalWeeks; i++)
            //    {

            //        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            //        var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
            //        if (thisWeekStart < today)
            //        {
            //            thisWeekStart = today;
            //        }

            //        if (thisWeekEnd > ReportRequest.ToDate)
            //        {
            //            thisWeekEnd = ReportRequest.ToDate;
            //        }
            //        var rcChartTypesForWeek = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisWeekStart, thisWeekEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ")));
            //        string newdaterange = thisWeekStart.ToString("dd-MM-yyy") + " to " + thisWeekEnd.ToString("dd-MM-yyy");
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = rcChartTypesForWeek.Count();
            //        rcChartTypesForWeekNewPercent.Add(obj);
            //        rcChartTypesForWeekNewCountnew = rcChartTypesForWeekNewCountnew + obj.RecordCount;
            //        today = thisWeekEnd.AddDays(1);

            //    }
            //    var rcChartTypesForWeekNewCount = rcChartTypesForWeekNewCountnew;
            //    foreach (var item in rcChartTypesForWeekNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesForWeekNewCount;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesForWeekNew.Add(obj);
            //    }
            //    //duress entries per week-end


            //    //duress entries per month-start
            //    today = ReportRequest.FromDate;

            //    var rcChartTypesForMonthNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesForMonthNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesForMonthNewCountnew = 0;

            //    //int months = (int)(ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Month);
            //    int months = (ReportRequest.ToDate.Year * 12 + ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Year * 12 + ReportRequest.FromDate.Month) + 1;
            //    for (int i = 1; i <= months; i++)
            //    {

            //        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            //        var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
            //        //if (thisMonthStart < today)
            //        //{
            //        //    thisMonthStart = today;
            //        //}

            //        //if (thisMonthEnd > ReportRequest.ToDate)
            //        //{
            //        //    thisMonthEnd = ReportRequest.ToDate;
            //        //}
            //        var rcChartTypesForMonth = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisMonthStart, thisMonthEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
            //        string newdaterange = thisMonthStart.ToString("MMM");
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = rcChartTypesForMonth.Count();
            //        rcChartTypesForMonthNewPercent.Add(obj);
            //        rcChartTypesForMonthNewCountnew = rcChartTypesForMonthNewCountnew + obj.RecordCount;
            //        today = thisMonthEnd.AddDays(1);

            //    }
            //    var rcChartTypesForMonthNewCount = rcChartTypesForMonthNewCountnew;
            //    foreach (var item in rcChartTypesForMonthNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesForMonthNewCount;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesForMonthNew.Add(obj);
            //    }
            //    //duress entries per month-end

            //    //duress entries per year-start
            //    today = ReportRequest.FromDate;

            //    var rcChartTypesForYearNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesForYearNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesForYearNewCountnew = 0;

            //    int years = (int)(ReportRequest.ToDate.Year - ReportRequest.FromDate.Year) +
            //(((ReportRequest.ToDate.Month > ReportRequest.FromDate.Month) ||
            //((ReportRequest.ToDate.Month == ReportRequest.FromDate.Month) && (ReportRequest.ToDate.Day >= ReportRequest.FromDate.Day))) ? 1 : 0);

            //    for (int i = 1; i <= years; i++)
            //    {

            //        var thisYearStart = new DateTime(today.Year, 1, 1);
            //        var thisYearEnd = new DateTime(today.Year, 12, 1);
            //        //if (thisYearStart < today)
            //        //{
            //        //    thisYearStart = today;
            //        //}

            //        //if (thisYearEnd > ReportRequest.ToDate)
            //        //{
            //        //    thisYearEnd = ReportRequest.ToDate;
            //        //}
            //        var rcChartTypesForYear = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisYearStart, thisYearEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
            //        string newdaterange = thisYearStart.Year.ToString();
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = rcChartTypesForYear.Count();
            //        rcChartTypesForYearNewPercent.Add(obj);
            //        rcChartTypesForYearNewCountnew = rcChartTypesForYearNewCountnew + obj.RecordCount;
            //        today = new DateTime(today.Year + 1, 1, 1);

            //    }
            //    var rcChartTypesForYearNewCount = rcChartTypesForYearNewCountnew;
            //    foreach (var item in rcChartTypesForYearNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;

            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesForYearNewCount;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesForYearNew.Add(obj);
            //    }

            //    //duress entries per year-end
            //    //no of guards went to prelarm-start
            //    var rcChartTypesGuardsPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesGuardsPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesGuardsPrealarmCountnew = 0;
            //    var rcChartTypesGuardsPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => z.NotificationType == 1).GroupBy(z => z.ClientSiteId); ;
            //    foreach (var item in rcChartTypesGuardsPrealarm)
            //    {

            //        string newdaterange = item.FirstOrDefault().ClientSite.Name;
            //        //var rcChartradiochecks = _irChartDataService.GetClientSiteRadioChecks(item.FirstOrDefault().ClientSite.Id, ReportRequest.FromDate,ReportRequest.ToDate).Where(z=>z.RadioCheckStatusId==1);
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = item.Count();

            //        rcChartTypesGuardsPrealarmNewPercent.Add(obj);

            //        rcChartTypesGuardsPrealarmCountnew = rcChartTypesGuardsPrealarmCountnew + obj.RecordCount;


            //    }
            //    foreach (var item in rcChartTypesGuardsPrealarmNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesGuardsPrealarmCountnew;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesGuardsPrealarmNew.Add(obj);
            //    }


            //    //no of guards went to prealram-end
            //    //no of guards went from prelarm-start
            //    var rcChartTypesGuardsFromPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesGuardsFromPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesGuardsFromPrealarmCountnew = 0;
            //    var rcChartTypesGuardsFromPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Guard Off Duty (NOTE: CRO did manual stamp as Guard went home without hitting OFF DUTY which is a breach of SOP"))).GroupBy(z => z.ClientSiteId); ;
            //    foreach (var item in rcChartTypesGuardsFromPrealarm)
            //    {

            //        string newdaterange = item.FirstOrDefault().ClientSite.Name;
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = item.Count();

            //        rcChartTypesGuardsFromPrealarmNewPercent.Add(obj);

            //        rcChartTypesGuardsFromPrealarmCountnew = rcChartTypesGuardsFromPrealarmCountnew + obj.RecordCount;


            //    }
            //    foreach (var item in rcChartTypesGuardsFromPrealarmNewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesGuardsFromPrealarmCountnew;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesGuardsFromPrealarmNew.Add(obj);
            //    }


            //    //no of guards went to prealram-end
            //    //no of tomes cro pushed radio button -start
            //    var rcChartTypesCRONew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    var rcChartTypesCRONewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            //    int rcChartTypesCROCountnew = 0;
            //    var rcChartTypesGuardsFromCRO = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.Notes != null && z.Notes.Contains("Control Room Alert"))).GroupBy(z => z.ClientSiteId); ;

            //    foreach (var item in rcChartTypesGuardsFromCRO)
            //    {

            //        string newdaterange = item.FirstOrDefault().ClientSite.Name;
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

            //        obj.DateRange = newdaterange;
            //        obj.RecordCount = item.Count();
            //        rcChartTypesCRONewPercent.Add(obj);
            //        rcChartTypesCROCountnew = rcChartTypesCROCountnew + obj.RecordCount;

            //    }

            //    foreach (var item in rcChartTypesCRONewPercent)
            //    {
            //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
            //        obj.DateRange = item.DateRange;
            //        obj.RecordCount = item.RecordCount;
            //        var newc = (double)item.RecordCount / rcChartTypesCROCountnew;
            //        obj.RecordCountNew = Math.Round(newc * 100, 1);
            //        rcChartTypesCRONew.Add(obj);
            //    }


            var activeAndInActive = GetActiveAndInactiveGuardHrReport().ToArray();
            var activeAndInActiveCount = activeAndInActive.Length;
            var yearOfOnBoarding = GetYearofOnBoardingGuardHrReport().ToArray();
            var yearOfOnBoardingcount = yearOfOnBoarding.Length;
            var yearOfOnBoradingBarChart = GetYearofOnBoardingGuardHrReportBarchart().ToArray();
            var genderReport = GetGenderBasedGuardHrReport().ToArray(); ;
            var genderReportCount = genderReport.Length;
            //no of tomes cro pushed radio button-end
            //p4 - 73 new piechart- end

            //    var dataTable = _viewDataService.PatrolDataToDataTable(results).Result;
            //    var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            //    if (!Directory.Exists(excelFileDir))
            //        Directory.CreateDirectory(excelFileDir);
            //    var fileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            //    PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));

            return new JsonResult(new {  chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage, feedbackTemplatesColour }, recordCount, yearOfOnBoarding, yearOfOnBoardingcount, activeAndInActive, activeAndInActiveCount, genderReport, genderReportCount, yearOfOnBoradingBarChart });
        }



        public IActionResult OnPostGenerateReportGraphSecondTab()
        {
            
            //p4-73 new piechart-start
            //duress entries per week-start
            var today = ReportRequest.FromDate;

            var rcChartTypesForWeekNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesForWeekNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForWeekNewCountnew = 0;
            TimeSpan ts = ReportRequest.ToDate.Subtract(today);
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

                if (thisWeekEnd > ReportRequest.ToDate)
                {
                    thisWeekEnd = ReportRequest.ToDate;
                }
                var rcChartTypesForWeek = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisWeekStart, thisWeekEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ")));
                string newdaterange = thisWeekStart.ToString("dd-MM-yyy") + " to " + thisWeekEnd.ToString("dd-MM-yyy");
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForWeek.Count();
                rcChartTypesForWeekNewPercent.Add(obj);
                rcChartTypesForWeekNewCountnew = rcChartTypesForWeekNewCountnew + obj.RecordCount;
                today = thisWeekEnd.AddDays(1);

            }
            var rcChartTypesForWeekNewCount = rcChartTypesForWeekNewCountnew;
            foreach (var item in rcChartTypesForWeekNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesForWeekNewCount;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesForWeekNew.Add(obj);
            }
            //duress entries per week-end


            //duress entries per month-start
            today = ReportRequest.FromDate;

            var rcChartTypesForMonthNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesForMonthNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForMonthNewCountnew = 0;

            //int months = (int)(ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Month);
            int months = (ReportRequest.ToDate.Year * 12 + ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Year * 12 + ReportRequest.FromDate.Month) + 1;
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
                var rcChartTypesForMonth = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisMonthStart, thisMonthEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
                string newdaterange = thisMonthStart.ToString("MMM");
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForMonth.Count();
                rcChartTypesForMonthNewPercent.Add(obj);
                rcChartTypesForMonthNewCountnew = rcChartTypesForMonthNewCountnew + obj.RecordCount;
                today = thisMonthEnd.AddDays(1);

            }
            var rcChartTypesForMonthNewCount = rcChartTypesForMonthNewCountnew;
            foreach (var item in rcChartTypesForMonthNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesForMonthNewCount;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesForMonthNew.Add(obj);
            }
            //duress entries per month-end

            //duress entries per year-start
            today = ReportRequest.FromDate;

            var rcChartTypesForYearNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesForYearNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForYearNewCountnew = 0;

            int years = (int)(ReportRequest.ToDate.Year - ReportRequest.FromDate.Year) +
        (((ReportRequest.ToDate.Month > ReportRequest.FromDate.Month) ||
        ((ReportRequest.ToDate.Month == ReportRequest.FromDate.Month) && (ReportRequest.ToDate.Day >= ReportRequest.FromDate.Day))) ? 1 : 0);

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
                var rcChartTypesForYear = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisYearStart, thisYearEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
                string newdaterange = thisYearStart.Year.ToString();
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForYear.Count();
                rcChartTypesForYearNewPercent.Add(obj);
                rcChartTypesForYearNewCountnew = rcChartTypesForYearNewCountnew + obj.RecordCount;
                today = new DateTime(today.Year + 1, 1, 1);

            }
            var rcChartTypesForYearNewCount = rcChartTypesForYearNewCountnew;
            foreach (var item in rcChartTypesForYearNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;

                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesForYearNewCount;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesForYearNew.Add(obj);
            }

            //duress entries per year-end
            //no of guards went to prelarm-start
            var rcChartTypesGuardsPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesGuardsPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesGuardsPrealarmCountnew = 0;
            var rcChartTypesGuardsPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => z.NotificationType == 1).GroupBy(z => z.ClientSiteId); ;
            foreach (var item in rcChartTypesGuardsPrealarm)
            {
              
                    string newdaterange = item.FirstOrDefault().SiteName;
                    //var rcChartradiochecks = _irChartDataService.GetClientSiteRadioChecks(item.FirstOrDefault().ClientSite.Id, ReportRequest.FromDate,ReportRequest.ToDate).Where(z=>z.RadioCheckStatusId==1);
                    ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                    obj.DateRange = newdaterange;
                    obj.RecordCount = item.Count();

                    rcChartTypesGuardsPrealarmNewPercent.Add(obj);

                    rcChartTypesGuardsPrealarmCountnew = rcChartTypesGuardsPrealarmCountnew + obj.RecordCount;

                


            }
            foreach (var item in rcChartTypesGuardsPrealarmNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesGuardsPrealarmCountnew;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesGuardsPrealarmNew.Add(obj);
            }


            //no of guards went to prealram-end
            //no of guards went from prelarm-start
            var rcChartTypesGuardsFromPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesGuardsFromPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesGuardsFromPrealarmCountnew = 0;
            var rcChartTypesGuardsFromPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Guard Off Duty (NOTE: CRO did manual stamp as Guard went home without hitting OFF DUTY which is a breach of SOP"))).GroupBy(z => z.ClientSiteId); ;
            foreach (var item in rcChartTypesGuardsFromPrealarm)
            {

              
                    string newdaterange = item.FirstOrDefault().SiteName;

                    ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                    obj.DateRange = newdaterange;
                    obj.RecordCount = item.Count();

                    rcChartTypesGuardsFromPrealarmNewPercent.Add(obj);

                    rcChartTypesGuardsFromPrealarmCountnew = rcChartTypesGuardsFromPrealarmCountnew + obj.RecordCount;

                


            }
            foreach (var item in rcChartTypesGuardsFromPrealarmNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesGuardsFromPrealarmCountnew;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesGuardsFromPrealarmNew.Add(obj);
            }


            //no of guards went to prealram-end
            //no of tomes cro pushed radio button -start
            var rcChartTypesCRONew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesCRONewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesCROCountnew = 0;
            var rcChartTypesGuardsFromCRO = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.Notes != null && z.Notes.Contains("Control Room Alert"))).GroupBy(z => z.ClientSiteId); ;

            foreach (var item in rcChartTypesGuardsFromCRO)
            {

                string newdaterange = item.FirstOrDefault().SiteName;
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                    obj.DateRange = newdaterange;
                    obj.RecordCount = item.Count();
                    rcChartTypesCRONewPercent.Add(obj);
                    rcChartTypesCROCountnew = rcChartTypesCROCountnew + obj.RecordCount;

                
            }

            foreach (var item in rcChartTypesCRONewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesCROCountnew;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesCRONew.Add(obj);
            }
            return new JsonResult(new { chartData = new {rcChartTypesForWeekNew, rcChartTypesForMonthNew, rcChartTypesForYearNew, rcChartTypesGuardsPrealarmNew, rcChartTypesCRONew, rcChartTypesGuardsFromPrealarmNew },  rcChartTypesForWeekNewCount, rcChartTypesForMonthNewCount, rcChartTypesForYearNewCount, rcChartTypesGuardsPrealarmCountnew, rcChartTypesCROCountnew, rcChartTypesGuardsFromPrealarmCountnew });
        }



        public IActionResult OnPostGenerateReportGraphThirdTab()
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(ReportRequest);
            var results = patrolDataReport.Results;

            //var reportFileName = results.FirstOrDefault().fileNametodownload;
            var sitePercentage = patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray();
            var areaWardPercentage = patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray();
            var eventTypePercentage = patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray();
            var eventTypeCount = patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray();
            var colorCodePercentage = patrolDataReport.ColorCodePercentage.OrderBy(z => z.Key).ToArray();
            var recordCount = patrolDataReport.ResultsCount;
            var colourcode = _configDataProvider.GetFeedbackTypesId("Colour Codes");
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == colourcode).ToList();

            var feedbackTemplatesColour = ArrageColurCode(colorCodePercentage, feedbackTemplates).ToArray();
            //p4-73 new piechart-start
            //duress entries per week-start
            var today = ReportRequest.FromDate;

            var rcChartTypesForWeekNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesForWeekNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForWeekNewCountnew = 0;
            TimeSpan ts = ReportRequest.ToDate.Subtract(today);
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

                if (thisWeekEnd > ReportRequest.ToDate)
                {
                    thisWeekEnd = ReportRequest.ToDate;
                }
                var rcChartTypesForWeek = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisWeekStart, thisWeekEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By ")));
                string newdaterange = thisWeekStart.ToString("dd-MM-yyy") + " to " + thisWeekEnd.ToString("dd-MM-yyy");
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForWeek.Count();
                rcChartTypesForWeekNewPercent.Add(obj);
                rcChartTypesForWeekNewCountnew = rcChartTypesForWeekNewCountnew + obj.RecordCount;
                today = thisWeekEnd.AddDays(1);

            }
            var rcChartTypesForWeekNewCount = rcChartTypesForWeekNewCountnew;
            foreach (var item in rcChartTypesForWeekNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesForWeekNewCount;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesForWeekNew.Add(obj);
            }
            //duress entries per week-end


            //duress entries per month-start
            today = ReportRequest.FromDate;

            var rcChartTypesForMonthNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesForMonthNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForMonthNewCountnew = 0;

            //int months = (int)(ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Month);
            int months = (ReportRequest.ToDate.Year * 12 + ReportRequest.ToDate.Month) - (ReportRequest.FromDate.Year * 12 + ReportRequest.FromDate.Month) + 1;
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
                var rcChartTypesForMonth = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisMonthStart, thisMonthEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
                string newdaterange = thisMonthStart.ToString("MMM");
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForMonth.Count();
                rcChartTypesForMonthNewPercent.Add(obj);
                rcChartTypesForMonthNewCountnew = rcChartTypesForMonthNewCountnew + obj.RecordCount;
                today = thisMonthEnd.AddDays(1);

            }
            var rcChartTypesForMonthNewCount = rcChartTypesForMonthNewCountnew;
            foreach (var item in rcChartTypesForMonthNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesForMonthNewCount;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesForMonthNew.Add(obj);
            }
            //duress entries per month-end

            //duress entries per year-start
            today = ReportRequest.FromDate;

            var rcChartTypesForYearNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesForYearNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesForYearNewCountnew = 0;

            int years = (int)(ReportRequest.ToDate.Year - ReportRequest.FromDate.Year) +
        (((ReportRequest.ToDate.Month > ReportRequest.FromDate.Month) ||
        ((ReportRequest.ToDate.Month == ReportRequest.FromDate.Month) && (ReportRequest.ToDate.Day >= ReportRequest.FromDate.Day))) ? 1 : 0);

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
                var rcChartTypesForYear = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, thisYearStart, thisYearEnd).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Duress Alarm Activated By "))); ;
                string newdaterange = thisYearStart.Year.ToString();
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = newdaterange;
                obj.RecordCount = rcChartTypesForYear.Count();
                rcChartTypesForYearNewPercent.Add(obj);
                rcChartTypesForYearNewCountnew = rcChartTypesForYearNewCountnew + obj.RecordCount;
                today = new DateTime(today.Year + 1, 1, 1);

            }
            var rcChartTypesForYearNewCount = rcChartTypesForYearNewCountnew;
            foreach (var item in rcChartTypesForYearNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;

                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesForYearNewCount;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesForYearNew.Add(obj);
            }

            //duress entries per year-end
            //no of guards went to prelarm-start
            var rcChartTypesGuardsPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesGuardsPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesGuardsPrealarmCountnew = 0;
            var rcChartTypesGuardsPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => z.NotificationType == 1).GroupBy(z => z.ClientSiteId); ;
            foreach (var item in rcChartTypesGuardsPrealarm)
            {

                string newdaterange = item.FirstOrDefault().ClientSite.Name;
                //var rcChartradiochecks = _irChartDataService.GetClientSiteRadioChecks(item.FirstOrDefault().ClientSite.Id, ReportRequest.FromDate,ReportRequest.ToDate).Where(z=>z.RadioCheckStatusId==1);
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                obj.DateRange = newdaterange;
                obj.RecordCount = item.Count();

                rcChartTypesGuardsPrealarmNewPercent.Add(obj);

                rcChartTypesGuardsPrealarmCountnew = rcChartTypesGuardsPrealarmCountnew + obj.RecordCount;


            }
            foreach (var item in rcChartTypesGuardsPrealarmNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesGuardsPrealarmCountnew;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesGuardsPrealarmNew.Add(obj);
            }


            //no of guards went to prealram-end
            //no of guards went from prelarm-start
            var rcChartTypesGuardsFromPrealarmNew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesGuardsFromPrealarmNewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesGuardsFromPrealarmCountnew = 0;
            var rcChartTypesGuardsFromPrealarm = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.LogBookNotes != null && z.LogBookNotes.Contains("Guard Off Duty (NOTE: CRO did manual stamp as Guard went home without hitting OFF DUTY which is a breach of SOP"))).GroupBy(z => z.ClientSiteId); ;
            foreach (var item in rcChartTypesGuardsFromPrealarm)
            {

                string newdaterange = item.FirstOrDefault().ClientSite.Name;
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                obj.DateRange = newdaterange;
                obj.RecordCount = item.Count();

                rcChartTypesGuardsFromPrealarmNewPercent.Add(obj);

                rcChartTypesGuardsFromPrealarmCountnew = rcChartTypesGuardsFromPrealarmCountnew + obj.RecordCount;


            }
            foreach (var item in rcChartTypesGuardsFromPrealarmNewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesGuardsFromPrealarmCountnew;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesGuardsFromPrealarmNew.Add(obj);
            }


            //no of guards went to prealram-end
            //no of tomes cro pushed radio button -start
            var rcChartTypesCRONew = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            var rcChartTypesCRONewPercent = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();
            int rcChartTypesCROCountnew = 0;
            var rcChartTypesGuardsFromCRO = _irChartDataService.GetAuditGuardFusionLogs(ReportRequest, ReportRequest.FromDate, ReportRequest.ToDate).Where(z => (z.Notes != null && z.Notes.Contains("Control Room Alert"))).GroupBy(z => z.ClientSiteId); ;

            foreach (var item in rcChartTypesGuardsFromCRO)
            {

                string newdaterange = item.FirstOrDefault().ClientSite.Name;
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

                obj.DateRange = newdaterange;
                obj.RecordCount = item.Count();
                rcChartTypesCRONewPercent.Add(obj);
                rcChartTypesCROCountnew = rcChartTypesCROCountnew + obj.RecordCount;

            }

            foreach (var item in rcChartTypesCRONewPercent)
            {
                ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
                obj.DateRange = item.DateRange;
                obj.RecordCount = item.RecordCount;
                var newc = (double)item.RecordCount / rcChartTypesCROCountnew;
                obj.RecordCountNew = Math.Round(newc * 100, 1);
                rcChartTypesCRONew.Add(obj);
            }


            var activeAndInActive = GetActiveAndInactiveGuardHrReport().ToArray();
            var activeAndInActiveCount = activeAndInActive.Length;
            var yearOfOnBoarding = GetYearofOnBoardingGuardHrReport().ToArray();
            var yearOfOnBoardingcount = yearOfOnBoarding.Length;

            var yearOfOnBoradingBarChart = GetYearofOnBoardingGuardHrReportBarchart().ToArray();

            var genderReport = GetGenderBasedGuardHrReport().ToArray(); ;
            var genderReportCount = genderReport.Length;
            //no of tomes cro pushed radio button-end
            //p4 - 73 new piechart- end

            var dataTable = _viewDataService.PatrolDataToDataTable(results).Result;
            var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            if (!Directory.Exists(excelFileDir))
                Directory.CreateDirectory(excelFileDir);
            var fileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));

            return new JsonResult(new { results, fileName, chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage, feedbackTemplatesColour, rcChartTypesForWeekNew, rcChartTypesForMonthNew, rcChartTypesForYearNew, rcChartTypesGuardsPrealarmNew, rcChartTypesCRONew, rcChartTypesGuardsFromPrealarmNew }, recordCount, rcChartTypesForWeekNewCount, rcChartTypesForMonthNewCount, rcChartTypesForYearNewCount, rcChartTypesGuardsPrealarmCountnew, rcChartTypesCROCountnew, rcChartTypesGuardsFromPrealarmCountnew, yearOfOnBoarding, yearOfOnBoardingcount, activeAndInActive, activeAndInActiveCount, genderReport, genderReportCount, yearOfOnBoradingBarChart });
        }





        public List<string> ArrageColurCode(KeyValuePair<string, double>[] ColourCodeName, List<FeedbackTemplate> FeedBackTempletes)
        {
            List<string> Colour= new List<string>();
            foreach(var item in ColourCodeName)
            {
                foreach (var color in FeedBackTempletes)
                {
                    if(item.Key.Trim()== color.Name.Trim())
                    {
                        Colour.Add(color.BackgroundColour);

                    }
                    else if(item.Key.Trim()=="N/A")
                    {
                        Colour.Add("#9467bd");
                    }

                }

            }

            return Colour;
        }

        public IActionResult OnGetDownloadReport(string file)
        {
            var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            var result = PhysicalFile(Path.Combine(excelFileDir, file), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            Response.Headers["Content-Disposition"] = new ContentDispositionHeaderValue("attachment") { FileName = file }.ToString();

            return result;
        }

        public IActionResult OnGetClientSites(string types)
        {
            return new JsonResult(_viewDataService.GetUserClientSites(types).OrderBy(z => z.Text));
        }
        public IActionResult OnPostGeneratePdfReport()
        {
            //var patrolDataReport = _irChartDataService.GetDailyPatrolData(ReportRequest);
            var fileName = _incidentReportGenerator.GeneratePdfReport(ReportRequest);

            return new JsonResult(new {  fileName});
        }

        //public IActionResult OnGetFeedbackTemplateListByType()
        //{

        //}
        //p3-132 Contracted Manning Button-start
        public PartialViewResult OnGetClientSiteKpiSettings(string site)
        {
            int siteId = _guardDataProvider.GetClientSiteID(site).Id;
            var clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(siteId);
            clientSiteKpiSetting ??= new ClientSiteKpiSetting() { ClientSiteId = siteId };
            if (clientSiteKpiSetting.rclistKP.ClientSiteID == 0)
            {
                clientSiteKpiSetting.rclistKP.ClientSiteID = siteId;

            }
            if (clientSiteKpiSetting.rclistKP.Imagepath != null)
            {
                if (clientSiteKpiSetting.rclistKP.Imagepath.Length > 0 && clientSiteKpiSetting.rclistKP.Imagepath.Trim() != "")
                {
                    clientSiteKpiSetting.rclistKP.Imagepath = clientSiteKpiSetting.rclistKP.Imagepath + ":-:" + ConvertFileToBase64(clientSiteKpiSetting.rclistKP.Imagepath);

                }

            }
            return Partial("../admin/_ClientSiteKpiSetting", clientSiteKpiSetting);
        }
        public string ConvertFileToBase64(string imageName)
        {
            string rtnstring = "";

            if (!string.IsNullOrEmpty(imageName))
            {
                var fileToConvert = Path.Combine(_settings.WebActionListKpiImageFolder, imageName);
                if (System.IO.File.Exists(fileToConvert))
                {
                    byte[] AsBytes = System.IO.File.ReadAllBytes(fileToConvert);
                    rtnstring = "data:application/octet-stream;base64," + Convert.ToBase64String(AsBytes);
                }
            }

            return rtnstring;
        }
        public IActionResult OnGetOfficerPositions(OfficerPositionFilter filter)
        {
            return new JsonResult(_viewDataService.GetOfficerPositions((OfficerPositionFilter)filter));
        }
        public JsonResult OnPostClientSiteManningKpiSettings(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var success = 0;
            var clientSiteId = 0;
            var erorrMessage = string.Empty;
            try
            {
                if (clientSiteKpiSetting != null)
                {
                    if (clientSiteKpiSetting.Id != 0)
                    {
                        clientSiteId = clientSiteKpiSetting.ClientSiteId;
                        var positionIdGuard = clientSiteKpiSetting.ClientSiteManningGuardKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        var positionIdPatrolCar = clientSiteKpiSetting.ClientSiteManningPatrolCarKpiSettings.Where(x => x.PositionId != 0).FirstOrDefault();
                        var InvalidTimes = _clientDataProvider.ValidDateTime(clientSiteKpiSetting);
                        if (InvalidTimes.Trim() == string.Empty)
                        {
                            if (positionIdGuard != null || positionIdPatrolCar != null)
                            {
                                var rulenumberOne = _clientDataProvider.CheckRulesOneinKpiManningInput(clientSiteKpiSetting);

                                if (rulenumberOne.Trim() == string.Empty)
                                {
                                    var rulenumberTwo = _clientDataProvider.CheckRulesTwoinKpiManningInput(clientSiteKpiSetting);
                                    if (rulenumberTwo.Trim() == string.Empty)
                                    {
                                        success = _clientDataProvider.SaveClientSiteManningKpiSetting(clientSiteKpiSetting);
                                        /* If change in the status update start */
                                        _clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id);
                                        /* If change in the status update end */
                                    }
                                    else
                                    {
                                        erorrMessage = rulenumberTwo;
                                        success = 7;

                                    }

                                }
                                else
                                {
                                    erorrMessage = rulenumberOne;
                                    success = 6;

                                }



                            }
                            else
                            {
                                success = 3;
                            }
                        }
                        else
                        {
                            erorrMessage = InvalidTimes;
                            success = 5;
                        }
                    }
                    else
                    {
                        success = 2;
                    }
                }
                else
                {
                    success = 4;
                }
            }
            catch
            {
                success = 4;
            }

            return new JsonResult(new { success, clientSiteId, erorrMessage });
        }

        public JsonResult OnPostDeleteWorker(string settingsId)
        {
            var status = true;
            var message = "Success";
            var clientSiteId = 0;
            try
            {
                if (settingsId != string.Empty)
                {
                    var split = settingsId.Split('_');
                    if (split.Length > 0)
                    {
                        var settId = int.Parse(split[0]);
                        var orderId = int.Parse(split[1]);
                        clientSiteId = int.Parse(split[2]);
                        _clientDataProvider.RemoveWorker(settId, orderId);
                    }
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = "Error " + ex.Message;
            }
            return new JsonResult(new { status, message, clientSiteId });
        }
        public IActionResult OnGetOfficerPositionsNew(OfficerPositionFilter filter)
        {
            return new JsonResult(_viewDataService.GetOfficerPositionsNew((OfficerPositionFilter)filter));
        }
        //p3-132 Contracted Manning Button-end
        public JsonResult OnGetCrmSupplierData(string companyName)
        {
            return new JsonResult(_guardLogDataProvider.GetCompanyDetailsVehLog(companyName));
        }

        public IEnumerable<KeyValuePair<string, double>> GetYearofOnBoardingGuardHrReport()
        {
            var guards = _guardDataProvider.GetGuards();

            // Set all blank/null DateEnrolled to 01-Jan-2022
            foreach (var guard in guards)
            {
                if (!guard.DateEnrolled.HasValue)
                {
                    guard.DateEnrolled = new DateTime(2022, 1, 1);
                }
            }

            // Total count of guards
            int totalGuards = guards.Count();

            // Group, count, and calculate percentages for pie chart
            var groupedByYear = guards
                .GroupBy(g => g.DateEnrolled.Value.Year.ToString()) // Convert year to string
                .Select(g => new KeyValuePair<string, double>(
                    g.Key,
                    Math.Round((double)g.Count() / totalGuards * 100, 2) // Calculate percentage and round to 2 decimals
                ))
                .OrderBy(kvp => kvp.Key); // Sort by year (string representation)

            return groupedByYear;
        }

        public IEnumerable<KeyValuePair<string, double>> GetActiveAndInactiveGuardHrReport()
        {
            var guards = _guardDataProvider.GetGuards();

            int totalGuards = guards.Count();

            if (totalGuards == 0)
                return Enumerable.Empty<KeyValuePair<string, double>>();

            // Group, count, and calculate percentages for active and inactive guards
            var groupedByStatus = guards
                .GroupBy(g => g.IsActive ? "Active" : "Inactive") // Group by IsActive field
                .Select(g => new KeyValuePair<string, double>(
                    g.Key,
                    Math.Round((double)g.Count() / totalGuards * 100, 2) // Calculate percentage and round to 2 decimals
                ))
                .OrderBy(kvp => kvp.Key); // Sort alphabetically (Active first)

            return groupedByStatus;
        }


        public IEnumerable<KeyValuePair<string, double>> GetGenderBasedGuardHrReport()
        {
            var guards = _guardDataProvider.GetGuards();

            int totalGuards = guards.Count();

            if (totalGuards == 0)
                return Enumerable.Empty<KeyValuePair<string, double>>();

            // Group, count, and calculate percentages for each gender
            var groupedByGender = guards
                .GroupBy(g => g.Gender ?? "Unknown") // Use "Unknown" for null or unspecified gender
                .Select(g => new KeyValuePair<string, double>(
                    g.Key,
                    Math.Round((double)g.Count() / totalGuards * 100, 2) // Calculate percentage and round to 2 decimals
                ))
                .OrderBy(kvp => kvp.Key); // Sort alphabetically

            return groupedByGender;
        }


        public IEnumerable<KeyValuePair<string, int>> GetYearofOnBoardingGuardHrReportBarchart()
        {
            var guards = _guardDataProvider.GetGuards();

            // Set all blank/null DateEnrolled to 01-Jan-2022
            foreach (var guard in guards)
            {
                if (!guard.DateEnrolled.HasValue)
                {
                    guard.DateEnrolled = new DateTime(2022, 1, 1);
                }
            }

            // Group, count, and return the number of guards for each year
            var groupedByYear = guards
                .GroupBy(g => g.DateEnrolled.Value.Year.ToString()) // Convert year to string
                .Select(g => new KeyValuePair<string, int>(
                    g.Key,
                    g.Count() // Return count directly
                ))
                .OrderBy(kvp => kvp.Key); // Sort by year (string representation)

            return groupedByYear;
        }
    }


   




}


