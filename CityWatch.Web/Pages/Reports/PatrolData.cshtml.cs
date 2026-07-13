using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Models;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ImageMagick;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Office.Interop.Excel;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace CityWatch.Web.Pages.Reports
{
    public class PatrolDataModel : PageModel
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CityWatch.Data.Models.PatrolDataReport> _reportCache = new();
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IPatrolDataReportService _irChartDataService;
        private readonly IIncidentReportGenerator _incidentReportGenerator;
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly IGuardDataProvider _guardDataProvider;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly string _downloadsFolderPath;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;
        public string ClientNameTitle { get; set; }

        public PatrolDataModel(IViewDataService viewDataService, 
            IWebHostEnvironment webHostEnvironment,
            IPatrolDataReportService irChartDataService, IIncidentReportGenerator incidentReportGenerator, IConfigDataProvider configurationProvider,IClientDataProvider clientDataProvider, IOptions<Settings> settings, IGuardDataProvider guardDataProvider,
            IGuardLogDataProvider guardLogDataProvider, IClientSiteWandDataProvider clientSiteWandDataProvider)
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
            _downloadsFolderPath = System.IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf", "FromDropbox");
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
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
            var host = HttpContext.Request.Host.Host;
            var hostParts = host.Split('.');

            // Extract the client name
            string clientName = hostParts.Length > 1 && hostParts[0].Trim().ToLower() == "www"
                                ? hostParts[1]
                                : hostParts[0];
            if (!string.IsNullOrEmpty(clientName))
            {
                if (
                    clientName.Trim().ToLower() != "www" &&
                    clientName.Trim().ToLower() != "cws-ir" &&
                    clientName.Trim().ToLower() != "test"
                    &&
                    clientName.Trim().ToLower() != "localhost"
                )
                {
                    int domain = _configDataProvider.GetSubDomainDetails(clientName).TypeId;
                    if (domain != 0)
                    {

                        ClientNameTitle = _configDataProvider.GetSubDomainDetails(clientName).Domain;
                    }
                    else
                    {

                        ClientNameTitle = "Citywatch Security";
                    }
                }
                else
                {

                    ClientNameTitle = "Citywatch Security";
                }
            }
                return Page();
        }

        public IActionResult OnPostGenerateReport()
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolDataNew(ReportRequest);
            _reportCache[HttpContext.Session.Id] = patrolDataReport;
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
            var pdfFileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.pdf";
            PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));
            PatrolReportGenerator.CreatePdfFile(dataTable, Path.Combine(excelFileDir, pdfFileName));
            return new JsonResult(new { results, fileName, pdfFileName });
        }


        public IActionResult OnPostGenerateReportGraphFirstTab()
        {
            if (!_reportCache.TryRemove(HttpContext.Session.Id, out var patrolDataReport))
            {
                patrolDataReport = _irChartDataService.GetDailyPatrolDataNew(ReportRequest);
            }
            var results = patrolDataReport.Results;

            //var reportFileName = results.FirstOrDefault().fileNametodownload;
            var sitePercentage = patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray();
            var excludedAreas = new[] { "Select", "0" };
            var areaWardPercentage = patrolDataReport.AreaWardPercentage.Where(z => !excludedAreas.Contains(z.Key)).OrderBy(z => z.Key).ToArray();
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

            int[]? guardIds = null;
            if (ReportRequest.ClientTypes != null || ReportRequest.ClientSites != null)
            {
                var clientsites = _guardDataProvider.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

                if (clientsites.Count() > 0)
                {
                    guardIds = clientsites.Select(x => x.GuardId).Distinct().ToArray();
                }
            }
            var activeAndInActive = GetActiveAndInactiveGuardHrReport(guardIds).ToArray();
            var activeAndInActiveCount = activeAndInActive.Length;
            //var yearOfOnBoarding = GetYearofOnBoardingGuardHrReport().ToArray();
            //var yearOfOnBoardingcount = yearOfOnBoarding.Length;
            var yearOfOnBoradingBarChart = GetYearofOnBoardingGuardHrReportBarchart(guardIds).ToArray();
            var yearOfOnBoardingcount = yearOfOnBoradingBarChart.Length;
            var genderReport = GetGenderBasedGuardHrReport(guardIds).ToArray(); ;
            var genderReportCount = genderReport.Length;
            //no of tomes cro pushed radio button-end
            //p4 - 73 new piechart- end

            //    var dataTable = _viewDataService.PatrolDataToDataTable(results).Result;
            //    var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            //    if (!Directory.Exists(excelFileDir))
            //        Directory.CreateDirectory(excelFileDir);
            //    var fileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            //    PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));
            var languageReport = GetGuardLanguagesHrReport(guardIds).ToArray();
            var languageReportCount = languageReport.Length;

            var attributionReport = GetGuardAttributionPerAnnumReport(guardIds).ToArray();
            var attributionReportCount = attributionReport.Length;

            return new JsonResult(new { chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage, feedbackTemplatesColour }, recordCount, yearOfOnBoardingcount, activeAndInActive, activeAndInActiveCount, genderReport, genderReportCount, yearOfOnBoradingBarChart, languageReport, languageReportCount, attributionReport, attributionReportCount });

            //return new JsonResult(new {  chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage, feedbackTemplatesColour }, recordCount, yearOfOnBoarding, yearOfOnBoardingcount, activeAndInActive, activeAndInActiveCount, genderReport, genderReportCount, yearOfOnBoradingBarChart, languageReport, languageReportCount, attributionReport, attributionReportCount });
        }

        public IActionResult OnPostGenerateReportGraphSecondTab()
        {
            var fromDate = ReportRequest.FromDate;
            var toDate = ReportRequest.ToDate;

            // =========================
            // YEAR RANGE (Full Calendar Years)
            // =========================
            var yearStart = new DateTime(fromDate.Year, 1, 1);
            var yearEnd = new DateTime(toDate.Year, 12, 31);

            // First day of FromDate month
            var monthStart = new DateTime(fromDate.Year, fromDate.Month, 1);

            // Last day of ToDate month
            var monthEnd = new DateTime(toDate.Year, toDate.Month,
                            DateTime.DaysInMonth(toDate.Year, toDate.Month));
            // 🔥 ONE DB CALL ONLY
            var allLogs = _irChartDataService
                .GetAuditGuardFusionLogs(ReportRequest, yearStart, yearEnd)
                .ToList();

            // =========================
            // Pre-filtered datasets
            // =========================
            var duressLogs = allLogs
                .Where(x => x.LogBookNotes?.Contains("Duress Alarm Activated By ") == true)
                .ToList();

            var preAlarmLogs = allLogs
                .Where(x => x.NotificationType == 1)
                .ToList();

            var croLogs = allLogs
                .Where(x => x.Notes?.Contains("Control Room Alert") == true)
                .ToList();

            var guardFromPreAlarmLogs = allLogs
                .Where(x => x.LogBookNotes?.Contains(
                    "Guard Off Duty (NOTE: CRO did manual stamp as Guard went home without hitting OFF DUTY") == true)
                .ToList();

            // =========================
            // WEEKLY
            // =========================
            var rcChartTypesForWeekNew = BuildWeeklyStats(duressLogs, fromDate, toDate, out int rcChartTypesForWeekNewCount);

            // =========================
            // MONTHLY
            // =========================
            var rcChartTypesForMonthNew = BuildMonthlyStats(duressLogs, monthStart, monthEnd, out int rcChartTypesForMonthNewCount);

            // =========================
            // YEARLY
            // =========================
            var rcChartTypesForYearNew = BuildYearlyStats(duressLogs, yearStart, yearEnd, out int rcChartTypesForYearNewCount);

            // =========================
            // PRE-ALARM (BY SITE)
            // =========================
            var rcChartTypesGuardsPrealarmNew =
                BuildSiteStats(preAlarmLogs, out int rcChartTypesGuardsPrealarmCountnew,fromDate,toDate);

            // =========================
            // CRO ALERTS (BY SITE)
            // =========================
            var rcChartTypesCRONew =
                BuildSiteStats(croLogs, out int rcChartTypesCROCountnew, fromDate, toDate);

            // =========================
            // GUARDS FROM PRE-ALARM
            // =========================
            var rcChartTypesGuardsFromPrealarmNew =
                BuildSiteStats(guardFromPreAlarmLogs, out int rcChartTypesGuardsFromPrealarmCountnew, fromDate, toDate);

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            return new JsonResult(new
            {
                chartData = new
                {
                    rcChartTypesForWeekNew,
                    rcChartTypesForMonthNew,
                    rcChartTypesForYearNew,
                    rcChartTypesGuardsPrealarmNew,
                    rcChartTypesCRONew,
                    rcChartTypesGuardsFromPrealarmNew
                },
                rcChartTypesForWeekNewCount,
                rcChartTypesForMonthNewCount,
                rcChartTypesForYearNewCount,
                rcChartTypesGuardsPrealarmCountnew,
                rcChartTypesCROCountnew,
                rcChartTypesGuardsFromPrealarmCountnew
            }, options);
        }

        private List<ClientSiteRadioChecksActivityStatus_HistoryReport> BuildWeeklyStats(
            List<ClientSiteRadioChecksActivityStatus_History> logs,DateTime fromDate, DateTime toDate,out int total)
        {
            total = 0;
            var result = new List<ClientSiteRadioChecksActivityStatus_HistoryReport>();

            // Align start date to Monday
            DateTime weekStart = fromDate.AddDays(
                -((int)fromDate.DayOfWeek == 0 ? 6 : (int)fromDate.DayOfWeek - 1));

            while (weekStart <= toDate)
            {
                DateTime weekEnd = weekStart.AddDays(6);

                var weekLogs = logs
                    .Where(x => x.EventDateTime >= weekStart && x.EventDateTime <= weekEnd)
                    .ToList();

                int count = weekLogs.Count;
                total += count;

                result.Add(new ClientSiteRadioChecksActivityStatus_HistoryReport
                {
                    DateRange =
                        $"{weekStart:dd-MM-yyyy} to {weekEnd:dd-MM-yyyy}",
                    RecordCount = count
                });

                weekStart = weekStart.AddDays(7);
            }

            ApplyPercentages(result, total);
            return result;
        }

        private List<ClientSiteRadioChecksActivityStatus_HistoryReport> BuildMonthlyStats(List<ClientSiteRadioChecksActivityStatus_History> logs, DateTime from, DateTime to, out int total)
        {
            var list = logs.Where(z => z.EventDateTime >= from && z.EventDateTime < to.AddDays(1))
                .GroupBy(x => new { x.EventDateTime.Year, x.EventDateTime.Month })
                .Select(g => new ClientSiteRadioChecksActivityStatus_HistoryReport
                {
                    DateRange = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    RecordCount = g.Count()
                })
                .ToList();

            total = list.Sum(x => x.RecordCount);
            ApplyPercentages(list, total);
            return list;
        }
        private List<ClientSiteRadioChecksActivityStatus_HistoryReport> BuildYearlyStats(List<ClientSiteRadioChecksActivityStatus_History> logs, DateTime from, DateTime to, out int total)
        {
            var list = logs.Where(z => z.EventDateTime >= from && z.EventDateTime < to.AddDays(1))
                .GroupBy(x => x.EventDateTime.Year)
                .Select(g => new ClientSiteRadioChecksActivityStatus_HistoryReport
                {
                    DateRange = g.Key.ToString(),
                    RecordCount = g.Count()
                })
                .ToList();

            total = list.Sum(x => x.RecordCount);
            ApplyPercentages(list, total);
            return list;
        }

        private List<ClientSiteRadioChecksActivityStatus_HistoryReport> BuildSiteStats(List<ClientSiteRadioChecksActivityStatus_History> logs, out int total,DateTime from, DateTime to)
        {
            var list = logs.Where(z => z.EventDateTime >= from && z.EventDateTime < to.AddDays(1))
                .GroupBy(x => x.ClientSiteId)
                .Select(g => new ClientSiteRadioChecksActivityStatus_HistoryReport
                {
                    DateRange = g.First().SiteName,
                    RecordCount = g.Count()
                })
                .ToList();

            total = list.Sum(x => x.RecordCount);
            ApplyPercentages(list, total);
            return list;
        }
        private void ApplyPercentages( List<ClientSiteRadioChecksActivityStatus_HistoryReport> list, int total)
        {
            foreach (var item in list)
            {
                item.RecordCountNew = total == 0
                    ? 0
                    : Math.Round((double)item.RecordCount / total * 100, 1);
            }
        }


        //public IActionResult OnPostGenerateReportGraphSecondTab()
        //{

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
        //        double rawValue = newc * 100;
        //        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        //        {
        //            obj.RecordCountNew = 0; // or any fallback value
        //        }
        //        else
        //        {
        //            obj.RecordCountNew = Math.Round(rawValue, 1);
        //        }
        //        //obj.RecordCountNew = Math.Round(newc * 100, 1);
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
        //        double rawValue = newc * 100;
        //        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        //        {
        //            obj.RecordCountNew = 0; // or any fallback value
        //        }
        //        else
        //        {
        //            obj.RecordCountNew = Math.Round(rawValue, 1);
        //        }
        //        //obj.RecordCountNew = Math.Round(newc * 100, 1);
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
        //        double rawValue = newc * 100;
        //        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        //        {
        //            obj.RecordCountNew = 0; // or any fallback value
        //        }
        //        else
        //        {
        //            obj.RecordCountNew = Math.Round(rawValue, 1);
        //        }
        //        //obj.RecordCountNew = Math.Round(newc * 100, 1);
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

        //            string newdaterange = item.FirstOrDefault().SiteName;
        //            //var rcChartradiochecks = _irChartDataService.GetClientSiteRadioChecks(item.FirstOrDefault().ClientSite.Id, ReportRequest.FromDate,ReportRequest.ToDate).Where(z=>z.RadioCheckStatusId==1);
        //            ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

        //            obj.DateRange = newdaterange;
        //            obj.RecordCount = item.Count();

        //            rcChartTypesGuardsPrealarmNewPercent.Add(obj);

        //            rcChartTypesGuardsPrealarmCountnew = rcChartTypesGuardsPrealarmCountnew + obj.RecordCount;




        //    }
        //    foreach (var item in rcChartTypesGuardsPrealarmNewPercent)
        //    {
        //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
        //        obj.DateRange = item.DateRange;
        //        obj.RecordCount = item.RecordCount;
        //        var newc = (double)item.RecordCount / rcChartTypesGuardsPrealarmCountnew;
        //        double rawValue = newc * 100;
        //        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        //        {
        //            obj.RecordCountNew = 0; // or any fallback value
        //        }
        //        else
        //        {
        //            obj.RecordCountNew = Math.Round(rawValue, 1);
        //        }
        //        //obj.RecordCountNew = Math.Round(newc * 100, 1);
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


        //            string newdaterange = item.FirstOrDefault().SiteName;

        //            ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

        //            obj.DateRange = newdaterange;
        //            obj.RecordCount = item.Count();

        //            rcChartTypesGuardsFromPrealarmNewPercent.Add(obj);

        //            rcChartTypesGuardsFromPrealarmCountnew = rcChartTypesGuardsFromPrealarmCountnew + obj.RecordCount;




        //    }
        //    foreach (var item in rcChartTypesGuardsFromPrealarmNewPercent)
        //    {
        //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
        //        obj.DateRange = item.DateRange;
        //        obj.RecordCount = item.RecordCount;
        //        var newc = (double)item.RecordCount / rcChartTypesGuardsFromPrealarmCountnew;
        //        double rawValue = newc * 100;
        //        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        //        {
        //            obj.RecordCountNew = 0; // or any fallback value
        //        }
        //        else
        //        {
        //            obj.RecordCountNew = Math.Round(rawValue, 1);
        //        }
        //        //obj.RecordCountNew = Math.Round(newc * 100, 1);
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

        //        string newdaterange = item.FirstOrDefault().SiteName;
        //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();

        //            obj.DateRange = newdaterange;
        //            obj.RecordCount = item.Count();
        //            rcChartTypesCRONewPercent.Add(obj);
        //            rcChartTypesCROCountnew = rcChartTypesCROCountnew + obj.RecordCount;


        //    }

        //    foreach (var item in rcChartTypesCRONewPercent)
        //    {
        //        ClientSiteRadioChecksActivityStatus_HistoryReport obj = new ClientSiteRadioChecksActivityStatus_HistoryReport();
        //        obj.DateRange = item.DateRange;
        //        obj.RecordCount = item.RecordCount;
        //        var newc = (double)item.RecordCount / rcChartTypesCROCountnew;
        //        double rawValue = newc * 100;
        //        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        //        {
        //            obj.RecordCountNew = 0; // or any fallback value
        //        }
        //        else
        //        {
        //            obj.RecordCountNew = Math.Round(rawValue, 1);
        //        }
        //        //obj.RecordCountNew = Math.Round(newc * 100, 1);
        //        rcChartTypesCRONew.Add(obj);
        //    }

        //    var options = new JsonSerializerOptions
        //    {
        //        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        //    };

        //    return new JsonResult(new { chartData = new {rcChartTypesForWeekNew, rcChartTypesForMonthNew, rcChartTypesForYearNew, rcChartTypesGuardsPrealarmNew, rcChartTypesCRONew, rcChartTypesGuardsFromPrealarmNew },  rcChartTypesForWeekNewCount, rcChartTypesForMonthNewCount, rcChartTypesForYearNewCount, rcChartTypesGuardsPrealarmCountnew, rcChartTypesCROCountnew, rcChartTypesGuardsFromPrealarmCountnew }, options);
        //}



        public IActionResult OnPostGenerateReportGraphThirdTab()
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolDataNew(ReportRequest);
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
            int[]? guardIds = null;
            if (ReportRequest.ClientTypes != null || ReportRequest.ClientSites != null)
            {
                var clientsites = _guardDataProvider.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

                if (clientsites.Count() > 0)
                {
                    guardIds = clientsites.Select(x => x.GuardId).ToArray();
                }
            }

            var activeAndInActive = GetActiveAndInactiveGuardHrReport(guardIds).ToArray();
            var activeAndInActiveCount = activeAndInActive.Length;
            var yearOfOnBoarding = GetYearofOnBoardingGuardHrReport().ToArray();
            var yearOfOnBoardingcount = yearOfOnBoarding.Length;

            var yearOfOnBoradingBarChart = GetYearofOnBoardingGuardHrReportBarchart(guardIds).ToArray();

            var genderReport = GetGenderBasedGuardHrReport(guardIds).ToArray(); ;
            var genderReportCount = genderReport.Length;
            //no of tomes cro pushed radio button-end
            //p4 - 73 new piechart- end

            var dataTable = _viewDataService.PatrolDataToDataTable(results).Result;
            var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            if (!Directory.Exists(excelFileDir))
                Directory.CreateDirectory(excelFileDir);
            var fileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            var pdfFileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.pdf";
            PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));
            PatrolReportGenerator.CreatePdfFile(dataTable, Path.Combine(excelFileDir, pdfFileName));

            return new JsonResult(new { results, fileName, pdfFileName, chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage, feedbackTemplatesColour, rcChartTypesForWeekNew, rcChartTypesForMonthNew, rcChartTypesForYearNew, rcChartTypesGuardsPrealarmNew, rcChartTypesCRONew, rcChartTypesGuardsFromPrealarmNew }, recordCount, rcChartTypesForWeekNewCount, rcChartTypesForMonthNewCount, rcChartTypesForYearNewCount, rcChartTypesGuardsPrealarmCountnew, rcChartTypesCROCountnew, rcChartTypesGuardsFromPrealarmCountnew, yearOfOnBoarding, yearOfOnBoardingcount, activeAndInActive, activeAndInActiveCount, genderReport, genderReportCount, yearOfOnBoradingBarChart });
        }

        public IActionResult OnPostGenerateReportGraphFourthTab()
        {
           
            var dailyLogWandStrikeReportForSiteController = _guardLogDataProvider.GetGuardLogsWithWandStrikes(ReportRequest, true);

            var dailySiteControllerWandStrikeData =
                BuildWandStrikeSeries(dailyLogWandStrikeReportForSiteController, ReportRequest.FromDate.Date, ReportRequest.ToDate.Date);
            var filteredLogs = dailyLogWandStrikeReportForSiteController
    .Where(x => x.HitUtcDateTime.Date >= ReportRequest.FromDate.Date &&
                x.HitUtcDateTime.Date <= ReportRequest.ToDate.Date)
    .ToList();

            int totalStrikes = filteredLogs.Count;

            var individualFQWandStrikeData = _clientSiteWandDataProvider.GetClientSiteSmartWandTags()
                .Where(z =>
                    (ReportRequest.ClientTypes == null || ReportRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                    (ReportRequest.ClientSites == null || ReportRequest.ClientSites.Contains(z.ClientSite.Name)))
                .Select(item =>
                {
                    var normalizedLabel = item.UId;
                    int strikes = filteredLogs.Count(x => x.TagUId.Contains(normalizedLabel));
                    double percent = totalStrikes > 0 ? Math.Round((double)strikes / totalStrikes * 100, 2) : 0;

                    return new
                    {
                        Wands = item.LabelDescription, // MTWTFSS
                        Strikes = percent
                    };
                })
                .Where(x => x.Strikes > 0)
                .ToList();

            //   var individualFQWandStrikeData = new List<object>();
            //   var clientsitesmartwands= _clientSiteWandDataProvider.GetClientSiteSmartWandTags()
            //       .Where(z =>
            //(ReportRequest.ClientTypes == null
            //    || ReportRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
            //(ReportRequest.ClientSites == null
            //    || ReportRequest.ClientSites.Contains(z.ClientSite.Name)));
            //   foreach (var item in clientsitesmartwands)
            //   {
            //       int strikes = dailyLogWandStrikeReportForSiteController.Where(x => (x.ClientSiteLogBook.Date >= ReportRequest.FromDate.Date && x.ClientSiteLogBook.Date <= ReportRequest.ToDate.Date)
            //                       && x.Notes.Contains(item.LabelDescription)).Count();
            //       int totalStrikes = dailyLogWandStrikeReportForSiteController.Where(x => (x.ClientSiteLogBook.Date >= ReportRequest.FromDate.Date && x.ClientSiteLogBook.Date <= ReportRequest.ToDate.Date)).Count();
            //       var percent = Math.Round((double)strikes / totalStrikes * 100, 2);
            //       if (strikes != 0) { 
            //           individualFQWandStrikeData.Add(new
            //           {
            //               Wands = item.LabelDescription,   // ?? gives MTWTFSS

            //               Strikes = percent
            //           });
            //       }
            //   }

            return new JsonResult(new {  chartData = new { dailySiteControllerWandStrikeData, individualFQWandStrikeData, totalWandStrikes = totalStrikes } });
        }
        public IActionResult OnPostGenerateReportGraphFifthTab(PatrolRequest ReportRequestnew, string[] TagId, string[] TagTypeId, string[] TagLabel,string GuardName,string LicenseNo, string[] SmartWandId)
        {
            //string[] TagIds = TagId.ToString()?.Split(",").ToArray() ?? Array.Empty<string>();
            int[] TagTypeIds = TagTypeId?.Select(z => int.Parse(z)).ToArray() ?? Array.Empty<int>();
            //string[] TagLabels = TagLabel?.Split(",").ToArray() ?? Array.Empty<string>();

            var dailyLogWandStrikeReportForSiteController = _guardLogDataProvider.GetGuardLogsWithWandStrikes(ReportRequest, true);

            var filterLogsLatest = dailyLogWandStrikeReportForSiteController.Where(z =>
            // hit log TagUId can carry extra characters around the tag UID, so match by
            // substring the same way the Individual Wand Point Fq pie does
            (TagId == null || !TagId.Any() || (z.TagUId != null && TagId.Any(t => z.TagUId.Contains(t)))) &&
            (TagTypeId == null || !TagTypeId.Any() || TagTypeIds.Contains(Convert.ToInt16(z.TagsTypeId))) &&
            (TagLabel == null || !TagLabel.Any() || (z.LabelDescription != null && TagLabel.Any(term => z.LabelDescription.Contains(term)))) &&
                     //(string.IsNullOrEmpty(TagId.ToString()) || TagIds.Contains(z.TagUId)) &&
                     //(string.IsNullOrEmpty(TagTypeId) || TagTypeIds.Contains(Convert.ToInt16(z.TagsTypeId))) &&
                     //(string.IsNullOrEmpty(TagLabel) || TagLabels.Contains(z.LabelDescription)) &&
                     (string.IsNullOrEmpty(GuardName) || string.Equals(z.LoggedInGuard?.Name, GuardName, StringComparison.OrdinalIgnoreCase)) &&
                     (string.IsNullOrEmpty(LicenseNo) || string.Equals(z.LoggedInGuard?.SecurityNo, LicenseNo, StringComparison.OrdinalIgnoreCase))
                     ).ToList();

            

            
            var dailySiteControllerWandStrikeDataForDownselect =
                BuildWandStrikeSeries(filterLogsLatest, ReportRequest.FromDate.Date, ReportRequest.ToDate.Date);

            var filteredLogs = filterLogsLatest
    .Where(x => x.HitUtcDateTime.Date >= ReportRequest.FromDate.Date &&
                x.HitUtcDateTime.Date <= ReportRequest.ToDate.Date)
    .ToList();

            int totalStrikes = filteredLogs.Count;

            var individualFQWandStrikeDataForDownselect = _clientSiteWandDataProvider.GetClientSiteSmartWandTags()
                .Where(z =>
                    (ReportRequest.ClientTypes == null || ReportRequest.ClientTypes.Contains(z.ClientSite.ClientType.Name)) &&
                    (ReportRequest.ClientSites == null || ReportRequest.ClientSites.Contains(z.ClientSite.Name)) &&
                     (TagId == null || !TagId.Any() || TagId.Contains(z.UId)) &&
                     (TagTypeId == null || !TagTypeId.Any() || TagTypeIds.Contains(Convert.ToInt16(z.TagsTypeId))) &&
                    (TagLabel == null || !TagLabel.Any() || TagLabel.Any(term => z.LabelDescription.Contains(term))))
                    //(string.IsNullOrEmpty(TagId.ToString()) || TagIds.Contains(z.UId)) &&
                    //(string.IsNullOrEmpty(TagTypeId) || TagTypeIds.Contains(Convert.ToInt16(z.TagsTypeId))) &&
                    //(string.IsNullOrEmpty(TagLabel) || TagLabels.Contains(z.LabelDescription)))
                .Select(item =>
                {
                    var normalizedLabel = item.UId;
                    int strikes = filteredLogs.Count(x => x.TagUId.Contains(normalizedLabel));
                    double percent = totalStrikes > 0 ? Math.Round((double)strikes / totalStrikes * 100, 2) : 0;

                    return new
                    {
                        Wands = item.LabelDescription, // MTWTFSS
                        Strikes = percent
                    };
                })
                .Where(x => x.Strikes > 0)
                .ToList();



            return new JsonResult(new { chartData = new { dailySiteControllerWandStrikeDataForDownselect, individualFQWandStrikeDataForDownselect, totalWandStrikes = totalStrikes } });
        }

        /// <summary>
        /// Site Combined Wand Strikes series over the full report range. The series used to be
        /// capped at 28 days ("always 4 weeks"), which left the chart blank for EFY reports
        /// spanning 12 months. Buckets adapt to the range so the bars stay readable:
        /// daily up to ~1 month, weekly up to ~6 months, monthly beyond that.
        /// </summary>
        private static List<object> BuildWandStrikeSeries(IEnumerable<ClientSiteSmartWandTagsHitLog> logs, DateTime fromDate, DateTime toDate)
        {
            if (toDate < fromDate)
                toDate = fromDate;

            var hitDates = logs
                .Select(x => x.HitUtcDateTime.Date)
                .Where(d => d >= fromDate && d <= toDate)
                .ToList();

            var totalDays = (toDate - fromDate).Days + 1;
            var series = new List<object>();

            if (totalDays <= 31)
            {
                var byDay = hitDates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
                for (var day = fromDate; day <= toDate; day = day.AddDays(1))
                {
                    byDay.TryGetValue(day, out int strikes);
                    series.Add(new
                    {
                        DayLabel = day.ToString("dddd")[0].ToString(), // MTWTFSS
                        Strikes = strikes
                    });
                }
            }
            else if (totalDays <= 182)
            {
                var byWeek = hitDates.GroupBy(d => (d - fromDate).Days / 7).ToDictionary(g => g.Key, g => g.Count());
                var weekCount = (totalDays + 6) / 7;
                for (var week = 0; week < weekCount; week++)
                {
                    byWeek.TryGetValue(week, out int strikes);
                    series.Add(new
                    {
                        DayLabel = fromDate.AddDays(week * 7).ToString("dd/MM"), // week starting
                        Strikes = strikes
                    });
                }
            }
            else
            {
                var byMonth = hitDates.GroupBy(d => new DateTime(d.Year, d.Month, 1)).ToDictionary(g => g.Key, g => g.Count());
                for (var month = new DateTime(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1))
                {
                    byMonth.TryGetValue(month, out int strikes);
                    series.Add(new
                    {
                        DayLabel = month.ToString("MMM yy"),
                        Strikes = strikes
                    });
                }
            }

            return series;
        }

        public JsonResult OnGetClientSiteWandAndTags(string clientSites)
        {
            var tagIds = new List<SelectListItem>();
            var tagTypeIds = new List<SelectListItem>();
            var tagLabels = new List<SelectListItem>();
            var smartWandIds = new List<SelectListItem>();
            var arClientSites = clientSites.Split(",").ToArray();
            var arClientSiteIds = _clientDataProvider.GetClientSiteDetailsWithName(arClientSites).Select(x => x.Id).ToArray();
            var tags = _viewDataService.GetClientSiteTagIds(arClientSiteIds);

            tagIds = tags.DistinctBy(x => x.UId)
                        .Select(x => new SelectListItem
                        {
                            Text = x.UId,
                            Value = x.UId
                        })
                        .ToList();

            tagTypeIds = tags.DistinctBy(x => x.SmartWandTagsType.Id)
                            .Select(x => new SelectListItem
                            {
                                Value = x.SmartWandTagsType.Id.ToString(),
                                Text = x.SmartWandTagsType.value
                            })
                            .OrderBy(x => x.Text)
                            .ToList();

            tagLabels = tags.DistinctBy(x => x.LabelDescription)
                            .Select(x => new SelectListItem
                            {
                                Value = x.LabelDescription,
                                Text = x.LabelDescription
                            })
                            .OrderBy(x => x.Text)
                            .ToList();

            smartWandIds = _viewDataService.GetClientSiteSmartWandIds(arClientSiteIds);

            return new JsonResult(new { tagIds, tagTypeIds, tagLabels, smartWandIds });
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
            //return new JsonResult(_viewDataService.GetUserClientSites(types).OrderBy(z => z.Text));

            if (!System.String.IsNullOrEmpty(types))
            {
                var values = types.Split(';');


                return new JsonResult(_viewDataService.GetUserClientSitesWithPatrolData(AuthUserHelper.LoggedInUserId, values).OrderBy(z => z.Text));
            }

            return new JsonResult(_viewDataService.GetUserClientSites(AuthUserHelper.LoggedInUserId, types).OrderBy(z => z.Text));
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
                                        _clientDataProvider.UpdateClientSiteStatus(clientSiteKpiSetting.ClientSiteId, clientSiteKpiSetting.ClientSite.StatusDate, clientSiteKpiSetting.ClientSite.Status, clientSiteKpiSetting.Id, clientSiteKpiSetting.KPITelematicsFieldID);
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


        public JsonResult OnPostDeleteWorkerADHOC(string settingsId)
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
                        _clientDataProvider.RemoveWorkerADHOC(settId, orderId);
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

        //public IEnumerable<KeyValuePair<string, double>> GetActiveAndInactiveGuardHrReport()
        //{
        //    var guards = _guardDataProvider.GetGuards();

        //    int totalGuards = guards.Count();

        //    if (totalGuards == 0)
        //        return Enumerable.Empty<KeyValuePair<string, double>>();

        //    // Group, count, and calculate percentages for active and inactive guards
        //    var groupedByStatus = guards
        //        .GroupBy(g => g.IsActive ? "Active" : "Inactive") // Group by IsActive field
        //        .Select(g => new KeyValuePair<string, double>(
        //            g.Key,
        //            Math.Round((double)g.Count() / totalGuards * 100, 2) // Calculate percentage and round to 2 decimals
        //        ))
        //        .OrderBy(kvp => kvp.Key); // Sort alphabetically (Active first)

        //    return groupedByStatus;
        //}

        public IEnumerable<object> GetActiveAndInactiveGuardHrReport(int[]? guardIds)
        {
           
            var guards = _guardDataProvider.GetGuards().Where(x=>(guardIds==null) || (guardIds.Contains(x.Id)));
            int totalGuards = guards.Count();

            if (totalGuards == 0)
                return Enumerable.Empty<object>();

            var groupedByStatus = guards
                .GroupBy(g => g.IsActive ? "Active" : "Inactive")
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / totalGuards * 100, 2)
                })
                .OrderBy(x => x.Status);

            return groupedByStatus;
        }

        public IEnumerable<KeyValuePair<string, double>> GetGenderBasedGuardHrReport(int[]? guardIds)
        {
            //var guards = _guardDataProvider.GetGuards();
            //int[]? guardIds = null;
            //if (ReportRequest.ClientTypes != null || ReportRequest.ClientSites != null)
            //{
            //    var clientsites = _guardDataProvider.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

            //    if (clientsites.Count() > 0)
            //    {
            //        guardIds = clientsites.Select(x => x.GuardId).ToArray();
            //    }
            //}
            var guards = _guardDataProvider.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));
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


        //public IEnumerable<KeyValuePair<string, int>> GetYearofOnBoardingGuardHrReportBarchart()
        //{
        //    var guards = _guardDataProvider.GetGuards();

        //    // Set all blank/null DateEnrolled to 01-Jan-2022
        //    foreach (var guard in guards)
        //    {
        //        if (!guard.DateEnrolled.HasValue)
        //        {
        //            guard.DateEnrolled = new DateTime(2022, 1, 1);
        //        }
        //    }

        //    // Group, count, and return the number of guards for each year
        //    var groupedByYear = guards
        //        .GroupBy(g => g.DateEnrolled.Value.Year.ToString()) // Convert year to string
        //        .Select(g => new KeyValuePair<string, int>(
        //            g.Key,
        //            g.Count() // Return count directly
        //        ))
        //        .OrderBy(kvp => kvp.Key); // Sort by year (string representation)

        //    return groupedByYear;
        //}
        public IEnumerable<object> GetYearofOnBoardingGuardHrReportBarchart(int[]? guardIds)
        {
            ////var guards = _guardDataProvider.GetGuards();
            //int[]? guardIds = null;
            //if (ReportRequest.ClientTypes != null || ReportRequest.ClientSites != null)
            //{
            //    var clientsites = _guardDataProvider.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

            //    if (clientsites.Count() > 0)
            //    {
            //        guardIds = clientsites.Select(x => x.GuardId).ToArray();
            //    }
            //}
            var guards = _guardDataProvider.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));

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

            // Group, count, and return the number of guards for each year
            var groupedByYear = guards
                .GroupBy(g => g.DateEnrolled.Value.Year.ToString()) // Convert year to string
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / totalGuards * 100, 2) // Return count directly
                })
                .OrderBy(kvp => kvp.Status); // Sort by year (string representation)
           
            return groupedByYear;
        }
        public async Task<IActionResult> OnPostGenerateBulkIRReportAsync()
        {
            string zipFileName = string.Empty;
            try
            {
                var patrolDataReport = _irChartDataService.GetDailyPatrolDataNew(ReportRequest);
                var results = patrolDataReport.Results;

                string baseUrl = "https://c4istorage1.blob.core.windows.net/irfiles/";
                string zipFolderPath = GetZipFolderPath();
                string workingDirectory = Path.Combine(zipFolderPath, $"{DateTime.Today:yyyyMMdd}_IncidentReports_Bulk_SN");

                if (!Directory.Exists(workingDirectory))
                    Directory.CreateDirectory(workingDirectory);

                foreach (var item in results)
                {
                    string subFolder = Path.Combine(workingDirectory, item.fileNametodownload.Substring(0, 8));
                    if (!Directory.Exists(subFolder))
                        Directory.CreateDirectory(subFolder);

                    string fileUrl = $"{baseUrl}{item.fileNametodownload.Substring(0, 8)}/{item.fileNametodownload}";
                    string outputFile = Path.Combine(subFolder, item.fileNametodownload);

                    using (HttpClient client = new HttpClient())
                    {
                        var response = await client.GetAsync(fileUrl);
                        response.EnsureSuccessStatusCode();

                        using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }
                }

                // Create the zip file
                 zipFileName = GetZipFileName(zipFolderPath, ReportRequest.FromDate, ReportRequest.ToDate, $"{DateTime.Today:yyyyMMdd}_IncidentReports_Bulk_SN.zip");
                if (System.IO.File.Exists(System.IO.Path.Combine(_downloadsFolderPath, zipFileName)))
                    System.IO.File.Delete(System.IO.Path.Combine(_downloadsFolderPath, zipFileName)); // Overwrite if exists

                ZipFile.CreateFromDirectory(workingDirectory, System.IO.Path.Combine(_downloadsFolderPath, zipFileName));
                if (Directory.Exists(workingDirectory))
                    Directory.Delete(workingDirectory, recursive: true);

            }
            catch(Exception ex)
            {

            }
            return new JsonResult(new { zipFile = @Url.Content($"~/Pdf/FromDropbox/{zipFileName}") });
            //return new JsonResult(new { results, zipFile = zipFileName });
        }
       
        private string GetZipFolderPath()
        {
            var zipFolderPath = System.IO.Path.Combine(_downloadsFolderPath);
            if (!Directory.Exists(zipFolderPath))
                Directory.CreateDirectory(zipFolderPath);
            return zipFolderPath;
        }
        private string GetZipFileName(string zipFolderPath, DateTime logFromDate, DateTime logToDate, string fileNamePart)
        {
            var zipFileName = $"{FileNameHelper.GetSanitizedFileNamePart(fileNamePart)}";


            //if (System.IO.File.Exists(System.IO.Path.Combine(_downloadsFolderPath, zipFileName)))
            //    System.IO.File.Delete(System.IO.Path.Combine(_downloadsFolderPath, zipFileName));
            //ZipFile.CreateFromDirectory(zipFolderPath, System.IO.Path.Combine(_downloadsFolderPath, zipFileName), CompressionLevel.Optimal, false);
            //if (Directory.Exists(zipFolderPath))
            //    Directory.Delete(zipFolderPath, true);
            return  zipFileName;
        }
        public JsonResult OnGetIRSerialNumbers(string snoPart)
        {
            
            return new JsonResult(_guardLogDataProvider.GetIRSerialNumbers(snoPart).ToList());

        }
        public IActionResult OnPostKeyVehicleSiteLogsWithDocket()
        {
            //int[] clientsiteIds =  _clientDataProvider.GetClientSites(null).Where(z =>
            //(ReportRequest.ClientTypes == null || ReportRequest.ClientTypes.Contains(z.ClientType.Name)) &&
            //                   (ReportRequest.ClientSites == null || ReportRequest.ClientSites.Contains(z.Name))).Select(x => x.Id).ToArray(); 


            //var keyVehicleAuditLogRequest = _viewDataService.GetKeyVehicleLogsWithDockets(ReportRequest.FromDate, ReportRequest.ToDate, clientsiteIds)
            //.Where(x=>(ReportRequest.SerialNo.IsNullOrEmpty()) || x.Detail.DocketSerialNo==ReportRequest.SerialNo);
            //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var keyVehicleAuditLogRequest = _viewDataService.GetKeyVehicleLogDocketHistoryWithIR(ReportRequest);


            //        // return new JsonResult(new { results, fileName });
            //        //duress entries per year-end

            //        //duress entries per year-end
            //return new JsonResult(new { "fileName" });
            return new JsonResult(new {  keyVehicleAuditLogRequest });
        }
        //p3-36-hrcharts partial-start
        public IEnumerable<object> GetGuardLanguagesHrReport(int[]? guardIds)
        {
            //int[]? guardIds = null;
            //if (ReportRequest.ClientTypes != null || ReportRequest.ClientSites != null)
            //{
            //    var clientsites = _guardDataProvider.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

            //    if (clientsites.Count() > 0)
            //    {
            //        guardIds = clientsites.Select(x => x.GuardId).ToArray();
            //    }
            //}
            //else
            //{
            //     guardIds = _guardDataProvider.GetGuards().Select(x => x.Id).ToArray();
            //}
            var guards = _guardDataProvider.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));
            //var guardsIds = _guardDataProvider.GetGuards().Select(x=>x.Id).ToArray();

            var languages = _guardDataProvider.GetGuardLanguages(guards.Select(z => z.Id).ToArray()).ToList();
            // Total count of guards
            int totalLanguagesCount = languages.Count();

            // Group, count, and calculate percentages for pie chart
            var groupedByLanguage = languages
                .GroupBy(g => g.LanguageMaster.Language.ToString()) // Convert year to string
                .Select(g => new
                {
                    Language=g.Key,
                    Count=g.Count(),
                    Percentage=Math.Round((double)g.Count() / totalLanguagesCount * 100, 2) // Calculate percentage and round to 2 decimals
                })
                .OrderBy(kvp => kvp.Language); // Sort by year (string representation)

            return groupedByLanguage;
        }
        
         public IEnumerable<object> GetGuardAttributionPerAnnumReport(int[]? guardIds)
        {
            //int[]? guardIds = null;
            //if (ReportRequest.ClientTypes != null || ReportRequest.ClientSites != null)
            //{
            //    var clientsites = _guardDataProvider.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

            //    if (clientsites.Count() > 0)
            //    {
            //        guardIds = clientsites.Select(x => x.GuardId).ToArray();
            //    }
            //}
            //var guards = _guardDataProvider.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));
            var inactiveGuards = _guardDataProvider.GetInActiveGuardDetails().Where(x =>
            ((guardIds == null) || (guardIds.Contains(x.GuardId)))
            //&& (x.LastWorkingDate >= ReportRequest.FromDate
            //                && x.LastWorkingDate < ReportRequest.ToDate.AddDays(1))
                            );

            // Total count of guards
            int totalInactiveGuardsCount = inactiveGuards.Count();

            // Group, count, and calculate percentages for pie chart
            var groupedByExpiredYears = inactiveGuards
                .GroupBy(g => g.LastWorkingDate.Value.Year.ToString()) // Convert year to string
                .Select(g => new
                {
                    Year = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / totalInactiveGuardsCount * 100, 2) // Calculate percentage and round to 2 decimals
                })
                .OrderBy(kvp => kvp.Year); // Sort by year (string representation)

            return groupedByExpiredYears;
        }
        //p3-36-hrcharts partial-end
    }







}


