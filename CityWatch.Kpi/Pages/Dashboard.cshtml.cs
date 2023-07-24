using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Kpi.Models;
using CityWatch.Kpi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

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

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public void OnGet()
        {
            ReportRequest = new KpiRequest();
        }

        public IActionResult OnGetClientSites(string type)
        {
            return new JsonResult(_viewDataService.GetClientSites(type));
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
            return new JsonResult(new { success =  false });
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
