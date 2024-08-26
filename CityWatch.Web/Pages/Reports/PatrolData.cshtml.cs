using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Web.Helpers;
using CityWatch.Web.Services;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
        private readonly IConfigDataProvider _configDataProvider;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly Settings _settings;
        private readonly IGuardDataProvider _guardDataProvider;
        public PatrolDataModel(IViewDataService viewDataService, 
            IWebHostEnvironment webHostEnvironment,
            IPatrolDataReportService irChartDataService, IIncidentReportGenerator incidentReportGenerator, IConfigDataProvider configurationProvider,IClientDataProvider clientDataProvider, IOptions<Settings> settings, IGuardDataProvider guardDataProvider)
        {
            _viewDataService = viewDataService;
            _webHostEnvironment = webHostEnvironment;
            _irChartDataService = irChartDataService;
            _incidentReportGenerator = incidentReportGenerator;
            _configDataProvider = configurationProvider;
            _clientDataProvider = clientDataProvider;
            _settings = settings.Value;
            _guardDataProvider = guardDataProvider;
        }

        [BindProperty]
        public PatrolRequest ReportRequest { get; set; }

        public IViewDataService ViewDataService { get { return _viewDataService; } }

        public IConfigDataProvider ConfigDataProiver { get { return _configDataProvider; } }
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
           
            //var reportFileName = results.FirstOrDefault().fileNametodownload;
            var sitePercentage = patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray();
            var areaWardPercentage = patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray();
            var eventTypePercentage = patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray();
            var eventTypeCount = patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray();
            var colorCodePercentage = patrolDataReport.ColorCodePercentage.OrderBy(z => z.Key).ToArray();
            var recordCount = patrolDataReport.ResultsCount;
            var colourcode = _configDataProvider.GetFeedbackTypesId("Colour Codes");
            var feedbackTemplates = _configDataProvider.GetFeedbackTemplates().Where(z => z.Type == colourcode).ToList();

            var feedbackTemplatesColour = ArrageColurCode(colorCodePercentage,feedbackTemplates).ToArray();

            var dataTable = _viewDataService.PatrolDataToDataTable(results).Result;
            var excelFileDir = Path.Combine(_webHostEnvironment.WebRootPath, "Excel", "Output");
            if (!Directory.Exists(excelFileDir))
                Directory.CreateDirectory(excelFileDir);
            var fileName = $"IR Statistics {ReportRequest.FromDate:ddMMyyyy} - {ReportRequest.ToDate:ddMMyyyy}.xlsx";
            PatrolReportGenerator.CreateExcelFile(dataTable, Path.Combine(excelFileDir, fileName));

            return new JsonResult(new { results, fileName, chartData = new { sitePercentage, areaWardPercentage, eventTypePercentage, eventTypeCount, colorCodePercentage, feedbackTemplatesColour }, recordCount });
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
    }
}
