using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace CityWatch.Web.Pages.Reports
{
    public class PatrolDataModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IViewDataService _viewDataService;
        private readonly IPatrolDataReportService _irChartDataService;
        private readonly IIncidentReportGenerator _incidentReportGenerator;

        public PatrolDataModel(IViewDataService viewDataService, 
            IWebHostEnvironment webHostEnvironment,
            IPatrolDataReportService irChartDataService, IIncidentReportGenerator incidentReportGenerator)
        {
            _viewDataService = viewDataService;
            _webHostEnvironment = webHostEnvironment;
            _irChartDataService = irChartDataService;
            _incidentReportGenerator = incidentReportGenerator;
        }

        [BindProperty]
        public PatrolRequest ReportRequest { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public ActionResult OnGet()
        {
            //if (!AuthUserHelper.IsAdminUserLoggedIn)
            //    return Redirect(Url.Page("/Account/Unauthorized"));

            return Page();
        }

        public IActionResult OnPostGenerateReport()
        {
            var patrolDataReport = _irChartDataService.GetDailyPatrolData(ReportRequest);
            var results = patrolDataReport.Results;
           
            var reportFileName = results.FirstOrDefault().fileNametodownload;
            var sitePercentage = patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray();
            var areaWardPercentage = patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray();
            var eventTypePercentage = patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray();
            var eventTypeCount = patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray();
            var colorCodePercentage = patrolDataReport.ColorCodePercentage.OrderBy(z => z.Key).ToArray();
            var recordCount = patrolDataReport.ResultsCount;

            var dataTable = _viewDataService.PatrolDataToDataTable(results);
            var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            if (!Directory.Exists(excelFileDir))
                Directory.CreateDirectory(excelFileDir);
            var fileName = $"Alarm Responses {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));

            return new JsonResult(new { results, fileName, chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage }, recordCount });
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

    }
}
