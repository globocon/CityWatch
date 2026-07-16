using CityWatch.Common.Helpers;
using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Providers;
using CityWatch.Data.Services;
using CityWatch.Kpi.Helpers;
using CityWatch.Kpi.Models;
using iText.IO.Font.Otf;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Svg.Renderers.Impl;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static Dropbox.Api.TeamLog.SpaceCapsType;
using IO = System.IO;

namespace CityWatch.Kpi.Services
{
    public interface IReportGenerator
    {
        string GeneratePdfReport(int clientSiteId, DateTime fromDate, DateTime toDate, bool isHrTimerPaused = false, bool IsDownselect = false, int CriticalDocumentID = 0);
        public string GeneratePdfTimesheetReport(int clientSiteId);
    }

    public class ReportGenerator : IReportGenerator
    {
        private const float CELL_FONT_SIZE = 7.5f;
        private const float PDF_DOC_MARGIN = 15f;
        private const string REPORT_DIR = "Output";

        private const string CELL_BG_GREEN = "#96e3ac";
        private const string CELL_BG_RED = "#ffcccc";
        private const string CELL_BG_YELLOW = "#fcf8d1";
        private const string CELL_BG_ORANGE = "#FFA500";
        private const string CELL_BG_BLUE_HEADER = "#bdd7ee";
        private const string CELL_BG_YELLOW_IR_COUNT = "#feff9a";
        private const string CELL_BG_ORANGE_IR_ALARM = "#ffdab3";
        private const string CELL_FONT_GREEN = "#008000";
        private const string CELL_FONT_RED = "#FF0000";
        private const string CELL_FONT_YELLOW = "#FFFF00";
        private const string CELL_FONT_ORANGE = "#FF8C00";
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_GREY = "#666362";

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;

        private readonly IViewDataService _viewDataService;
        private readonly IClientDataProvider _clientDataProvider;
        private readonly ILogger<ReportGenerator> _logger;
        private readonly Settings _settings;
        private readonly IPatrolDataReportService _patrolDataReportService;
        private readonly IGuardLogDataProvider _guardLogDataProvider;
        private readonly IClientSiteWandDataProvider _clientSiteWandDataProvider;

        public ReportGenerator(IOptions<Settings> settings,
            IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService,
            IClientDataProvider clientDataProvider,
            ILogger<ReportGenerator> logger, IPatrolDataReportService patrolDataReportService, IGuardLogDataProvider guardLogDataProvider,IClientSiteWandDataProvider clientSiteWandDataProvider)
        {
            _viewDataService = viewDataService;
            _clientDataProvider = clientDataProvider;
            _logger = logger;
            _settings = settings.Value;

            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            //nEWLY ADDAED-START

            _patrolDataReportService = patrolDataReportService;
            //nEWLY ADDAED-END

            if (!IO.Directory.Exists(IO.Path.Combine(_reportRootDir, REPORT_DIR)))
                IO.Directory.CreateDirectory(IO.Path.Combine(_reportRootDir, REPORT_DIR));

            if (!IO.Directory.Exists(_graphImageRootDir))
                IO.Directory.CreateDirectory(_graphImageRootDir);
            _guardLogDataProvider = guardLogDataProvider;
            _clientSiteWandDataProvider = clientSiteWandDataProvider;
        }

        public string GeneratePdfReport(int clientSiteId, DateTime fromDate, DateTime toDate, bool isHrTimerPaused, bool IsDownselect, int CriticalDocumentID)
        {
            var _clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (_clientSiteKpiSetting == null)
                return string.Empty;

            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(_clientSiteKpiSetting.ClientSite.Name)} - Daily KPI Reports - {fromDate.ToString("MMM")} {fromDate.Year}_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);

            var headerTable = CreateReportHeader(_clientSiteKpiSetting);
            doc.Add(headerTable);

            //to get the data in 3rd page of report
            var monthlyDataGuard = _viewDataService.GetKpiGuardDetailsData(_clientSiteKpiSetting.ClientSiteId, fromDate, toDate);
            var GuradIds = monthlyDataGuard.Select(z => z.GuardId).ToArray();
            var monthlyDataGuardCompliance = _viewDataService.GetKpiGuardDetailsComplianceData(GuradIds);

            var monthlyData = _viewDataService.GetKpiReportData(_clientSiteKpiSetting.ClientSiteId, fromDate, toDate);
            var tableData = CreateReportData(_clientSiteKpiSetting, fromDate, monthlyData.DailyKpiResults, isHrTimerPaused);
            CreateReportDataSummary(tableData, monthlyData);
            var clientsiteLogBook = _viewDataService.GetTagStatusPendingForSpecificClientSite(_clientSiteKpiSetting.ClientSiteId, fromDate, toDate);

            var tableSitemartwands = CreateGuardWandScanDetails(clientsiteLogBook);
            var tableSiteStats = CreateSiteStatsData(_clientSiteKpiSetting, monthlyData, fromDate);

            var tableLayout = new Table(UnitValue.CreatePercentArray(new float[] { 75, 25 })).UseAllAvailableWidth();
            tableLayout.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).Add(tableData));
            tableLayout.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).Add(tableSiteStats));
            
            doc.Add(tableLayout);
            doc.Add(new AreaBreak());

            doc.Add(headerTable);
            doc.Add(tableSitemartwands);
            List<string> list = new List<string>();
            list.Add(_clientSiteKpiSetting.ClientSite.Name);
            string[] clientsitename = list.ToArray();

            var patrolDataReport = _patrolDataReportService.GetDailyPatrolData(new PatrolRequest()
            {
                FromDate = fromDate,
                ToDate = toDate,
                DataFilter = PatrolDataFilter.Custom,
                ClientSites = clientsitename,
            });
            PatrolRequest ReportRequest = new PatrolRequest();
            ReportRequest.FromDate = fromDate;
            ReportRequest.ToDate = toDate;

            ReportRequest.ClientSites = new string[] { _clientSiteKpiSetting.ClientSite.Name };
            ReportRequest.ClientTypes = new string[] { _clientSiteKpiSetting.ClientSite.ClientType.Name };
            var wandGraphsTable = CreateWandGraphsTables(ReportRequest);
            doc.Add(wandGraphsTable);
            if (_settings.GuardListOn)
            {
                //doc.Add(new AreaBreak());

                //doc.Add(headerTable);
                var monthlyGuardData = _viewDataService.GetMonthlyKpiGuardData(clientSiteId, fromDate, toDate);
                var tableGuardData = CreateGuardReportData(monthlyGuardData, fromDate);
                //p2-145 – Telematics Error-start
                foreach (var table in tableGuardData)// if contains multiple tables
                {
                    doc.Add(new AreaBreak());

                    doc.Add(headerTable);
                    doc.Add(table);          // Table implements IBlockElement
                    //doc.Add(new AreaBreak()); // Optional: new page per table
                }
                //p2-145 – Telematics Error-end
                if (monthlyDataGuard.Count > 0)
                {
                    // To add 3rd Page
                    var HRGroupList = _viewDataService.GetKpiGuardHRGroup();
                    var ClientSiteState = _clientDataProvider.GetClientSites(null).Where(x => x.Id == clientSiteId).FirstOrDefault().State;
                    for (int i = 0; i < HRGroupList.Count; i++)
                    {
                        doc.Add(new AreaBreak());

                        doc.Add(headerTable);
                        var hrGroupName = HRGroupList[i];
                        var tableGuardDetailsData = CreateGuardDetailsLicenseAndCompliance(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName.Name, hrGroupName.Id, clientSiteId, ClientSiteState, IsDownselect, CriticalDocumentID);
                        doc.Add(tableGuardDetailsData);
                        doc.Add(new Paragraph("\n"));
                        var tableGuardDetailsData1 = CreateGuardDetailsLicenseAndComplianceHR1(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName.Name, hrGroupName.Id, clientSiteId, ClientSiteState, IsDownselect, CriticalDocumentID);
                        doc.Add(tableGuardDetailsData1);
                    }
                }

            }
            //NEWLY ADDED-START
            doc.Add(new AreaBreak());

            doc.Add(headerTable);
           

            // Disable on 21-06-2024 by binoy to enable empty graph for zero data. Task P2#126 
            //if (patrolDataReport.ResultsCount > 0)
            //{
            //    var graphsTable = CreateGraphsTables(patrolDataReport);
            //    doc.Add(graphsTable);
            //}
            //p2-184-hr-charts-start
            
            var hrGraphsTable = CreateHRGraphsTables(ReportRequest);
            doc.Add(hrGraphsTable);
            //p2-184-hr-charts-end
            var graphsTable = CreateGraphsTables(patrolDataReport);
            doc.Add(graphsTable);

            //NEWLY ADDED-END
            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }


        public string GeneratePdfTimesheetReport(int clientSiteId)
        {
            var _clientSiteKpiSetting = _clientDataProvider.GetClientSiteKpiSetting(clientSiteId);
            if (_clientSiteKpiSetting == null)
                return string.Empty;

            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} - {FileNameHelper.GetSanitizedFileNamePart(_clientSiteKpiSetting.ClientSite.Name)} - Daily KPI Reports -_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);

            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            pdfDoc.SetDefaultPageSize(PageSize.A4.Rotate());
            var doc = new Document(pdfDoc);
            doc.SetMargins(PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN, PDF_DOC_MARGIN);

            var headerTable = CreateReportHeader(_clientSiteKpiSetting);
            doc.Add(headerTable);



            doc.Close();
            pdfDoc.Close();

            return reportFileName;
        }
        private Table CreateGuardWandScanDetails(List<SiteTagStatusPendingNew> clientSiteLogBook)
        {
            var guardWandScanDetailsTable =
                new Table(UnitValue.CreatePercentArray(new float[] { 2, 16, 2 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(15);

            // HEADER ROW
            guardWandScanDetailsTable.AddCell(
                new Cell()
                 .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Type")));

            guardWandScanDetailsTable.AddCell(
                new Cell()
                 .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Label")));

            guardWandScanDetailsTable.AddCell(
                new Cell()
                 .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                
                .SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph("Scans")));

            // DATA ROWS
            foreach (var groupItem in clientSiteLogBook)
            {
                guardWandScanDetailsTable.AddCell(
                    new Cell()
                    
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.TagType)));

                guardWandScanDetailsTable.AddCell(
                    new Cell()
                   
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.LabelDescription)));

                guardWandScanDetailsTable.AddCell(
                    new Cell()
                   
                    .SetFontSize(CELL_FONT_SIZE)
                    .Add(new Paragraph(groupItem.TodayScanCount.ToString())));
            }

            return guardWandScanDetailsTable;
        }
        private Table CreateReportDataSummary(Table table, MonthlyKpiResult monthlyKpiResult)
        {
            //row 1
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .SetKeepTogether(true)
              .Add(new Paragraph("Expected Hrs Vs Actual")));
            table.AddCell(new Cell(1, 1)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.RIGHT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph($"{monthlyKpiResult.TotalExpectedEmployeeHours:0.00}"))); ;
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.TotalActualEmployeeHours:0.00}")));
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph(string.Empty))); ;
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph(string.Empty)));



            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph(string.Empty)));

            table.AddCell(new Cell(1, 1).Add(new Paragraph(""))); // *** Fq Column Added Here ***
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph(string.Empty)));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph(string.Empty)));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph(string.Empty)));
            //row 2
            table.AddCell(new Cell(1, 4)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph("Site Results Average Against KPI")));
            table.AddCell(new Cell(1, 2)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.RIGHT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph($"{monthlyKpiResult.ImageCountAverage:0.00}"))); ;
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.WandScanAverage:0.00}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.WandPatrolsAverage:0.00}")));




            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.WandFqAverage:0.00}"))); // *** Fq Column Added Here ***

            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.NotInAcceptableLogFreqCount}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.IrCountTotal}")));
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.LEFT)
              .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
              .Add(new Paragraph($"{monthlyKpiResult.AlarmCountTotal}")));

            // row 3
            table.AddCell(new Cell(1, 4)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph("Site Results % Against KPI")));

            var colorImagePercentage = CELL_BG_RED;
            if (monthlyKpiResult.ImageCountPercentage >= 100)
                colorImagePercentage = CELL_BG_GREEN;
            table.AddCell(new Cell(1, 2)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.RIGHT)
               .SetBackgroundColor(WebColors.GetRGBColor(colorImagePercentage))
               .Add(new Paragraph($"{monthlyKpiResult.ImageCountPercentage.GetValueOrDefault():0.00}%")));

            var colorWandPercentage = CELL_BG_RED;
            if (monthlyKpiResult.WandScanPercentage >= 100)
                colorWandPercentage = CELL_BG_GREEN;
            table.AddCell(new Cell(1, 2)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(colorWandPercentage))
              .Add(new Paragraph($"{monthlyKpiResult.WandScanPercentage.GetValueOrDefault():0.00}%")));

            table.AddCell(new Cell(1, 1)
             .SetFontSize(CELL_FONT_SIZE)
             .SetTextAlignment(TextAlignment.RIGHT)
             .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
             .Add(new Paragraph($"{monthlyKpiResult.WandPatrolsPercentage:0.00}%")));


            //table.AddCell(new Cell(1, 1)
            //.SetFontSize(CELL_FONT_SIZE)
            //.SetTextAlignment(TextAlignment.RIGHT)
            //.SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
            //.Add(new Paragraph($"{monthlyKpiResult.WandFqPercentage:0.00}%"))); // *** Fq Column Added Here ***

            table.AddCell(new Cell(1, 1)
    .SetFontSize(CELL_FONT_SIZE)
    .SetTextAlignment(TextAlignment.RIGHT)
    .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
    .Add(new Paragraph("")));


            table.AddCell(new Cell(1, 1)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph(string.Empty)));

            table.AddCell(new Cell(1, 1)
               .SetFontSize(CELL_FONT_SIZE)
               .SetTextAlignment(TextAlignment.LEFT)
               .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_YELLOW))
               .Add(new Paragraph("Site Score")));

            var colorSiteScore = CELL_BG_RED;
            if (monthlyKpiResult.SiteScorePercentage >= 100)
                colorSiteScore = CELL_BG_GREEN;
            table.AddCell(new Cell(1, 1)
              .SetFontSize(CELL_FONT_SIZE)
              .SetTextAlignment(TextAlignment.RIGHT)
              .SetBackgroundColor(WebColors.GetRGBColor(colorSiteScore))
              .Add(new Paragraph($"{monthlyKpiResult.SiteScorePercentage.GetValueOrDefault():0.00}%")));

            return table;
        }

        private Table CreateReportHeader(ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

            var cellSiteImage = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            if (!string.IsNullOrEmpty(clientSiteKpiSetting.SiteImage))
            {
                var siteImagePath = IO.Path.Combine(_siteImageRootDir, IO.Path.GetFileName(clientSiteKpiSetting.SiteImage));
                if (IO.File.Exists(siteImagePath))
                {
                    var siteImage = new Image(ImageDataFactory.Create(siteImagePath)).SetHeight(60);
                    cellSiteImage.Add(siteImage);
                }
            }
            var thermalCamSite = clientSiteKpiSetting.IsThermalCameraSite ? "Day Camera + Thermal (Ti) Channel" : "Day Camera Only";
            cellSiteImage.Add(new Paragraph(thermalCamSite).SetFontSize(CELL_FONT_SIZE * .8f));

            var weekendSite = clientSiteKpiSetting.IsWeekendOnlySite ? "Weekend Only Site" : "7-Day Site";
            cellSiteImage.Add(new Paragraph(weekendSite).SetFontSize(CELL_FONT_SIZE * .8f));
            headerTable.AddCell(cellSiteImage);

            var cellReportTitle = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Site KPI Telematics Engine & Statistics\n").SetFont(PdfHelper.GetPdfFont()).SetFontSize(CELL_FONT_SIZE * 1.5f))
                .Add(new Paragraph("KPI v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()).SetFontSize(CELL_FONT_SIZE * 1.25f))
                .Add(new Paragraph(clientSiteKpiSetting.ClientSite.ClientType.Name).SetFontSize(CELL_FONT_SIZE * 1.25f))
                .Add(new Paragraph($"{clientSiteKpiSetting.ClientSite.Name} \n\n").SetFontSize(CELL_FONT_SIZE));
            headerTable.AddCell(cellReportTitle);

            var image = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
                .SetHeight(50)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            var cellLogoImage = new Cell()
                .Add(image)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            headerTable.AddCell(cellLogoImage);

            headerTable.AddCell(new Cell(1, 3).SetPadding(3).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            return headerTable;
        }

        private Table CreateReportData(ClientSiteKpiSetting clientSiteKpiSetting, DateTime fromDate, List<DailyKpiResult> dailyKpiResults, bool isHrTimerPaused)
        {
            //var colWidth = new float[] { 2, 10, 8, 8, 9, 9, 9, 9, 12, 8, 8, 8 };
            var colWidth = new float[]
     {
                   2,   // DATE
                   11,  // DAY
                   8,   // Expected Hours
                   8,   // Hours Change

                   9,   // Images: Day + Total
                   9,   // Images: p/hr

                   9,   // Patrol: Total
                   9, // Patrol: p/hr (split from 9)

                   6, // Fq        (split from 9)

                   6,  // 2HR Timer
                   8,   // IR Reports
                   8,   // Fire/Alarms
                   8    // Existing last column
     };







            var table = new Table(UnitValue.CreatePercentArray(colWidth)).UseAllAvailableWidth();

            CreateHeaderRow(table, fromDate, clientSiteKpiSetting);

            foreach (var item in dailyKpiResults)
            {
                CreateDataRow(table, item, clientSiteKpiSetting, isHrTimerPaused);
            }

            return table;
        }

        private Table CreateSiteStatsData(ClientSiteKpiSetting clientSiteKpiSetting, MonthlyKpiResult monthlyKpiResult, DateTime fromDate)
        {
            var tableSiteStats = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();

            var siteStatsTitle = new Paragraph(new Text("SITE Statistics\n\n")).SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER);
            var siteStatsLine1 = new Paragraph($"Expected Patrol Duration {clientSiteKpiSetting.ExpPatrolDuration} min");
            var siteStatsLine2 = new Paragraph($"Min. Patrol Freq. {clientSiteKpiSetting.MinPatrolFreq} p/hr");
            var siteStatsLine3 = new Paragraph($"Min. Images per patrol {clientSiteKpiSetting.MinImagesPerPatrol}");

            var cellTop = new Cell().SetFontSize(CELL_FONT_SIZE);
            cellTop.Add(siteStatsTitle)
                .Add(siteStatsLine1)
                .Add(siteStatsLine2)
                .Add(siteStatsLine3)
                .SetHeight(75);
            tableSiteStats.AddCell(cellTop);

            var graphImage = GetGraphImage(monthlyKpiResult.EffortCounts);
            var cellGraphImage = new Cell();
            if (graphImage != null)
            {
                cellGraphImage.Add(new Paragraph("Effort Counter: Week Vs. Week")
                    .SetFontSize(CELL_FONT_SIZE)
                    .SetTextAlignment(TextAlignment.CENTER));
                cellGraphImage.Add(graphImage);
            }
            tableSiteStats.AddCell(cellGraphImage);

            var notesTitle = new Paragraph("NOTES:\n\n").SetFont(PdfHelper.GetPdfFont()).SetTextAlignment(TextAlignment.CENTER);
            var monthNote = clientSiteKpiSetting.Notes?.SingleOrDefault(z => z.ForMonth == new DateTime(fromDate.Year, fromDate.Month, 1))?.Notes ?? string.Empty;
            var notes = new Paragraph(monthNote);
            var cellBottom = new Cell().SetFontSize(CELL_FONT_SIZE);
            cellBottom.Add(notesTitle)
                .Add(notes)
                .SetHeight(242);
            tableSiteStats.AddCell(cellBottom);

            tableSiteStats.AddCell(new Cell()
                .SetFontSize(CELL_FONT_SIZE)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph("Data Generated: " + monthlyKpiResult.ReportTimeStamp)));

            return tableSiteStats;
        }

        private void CreateHeaderRow(Table table, DateTime fromDate, ClientSiteKpiSetting clientSiteKpiSetting)
        {
            var tuneBuffer = string.Empty;
            if (clientSiteKpiSetting.TuneDowngradeBuffer.HasValue)
                tuneBuffer = ((clientSiteKpiSetting.TuneDowngradeBuffer.Value - 1) * 100).ToString("0");

            table.AddCell(new Cell(1, 4)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text($"MONTH/YEAR: {fromDate.ToString("MMM").ToUpper()} {fromDate.Year}\n"))
                    .Add(new Text($"KPI Tune buffer @ {tuneBuffer}%").SetFontSize(CELL_FONT_SIZE * .8f))));
            table.AddCell(new Cell(1, 2)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text("DAILY IMAGES\n"))
                    .Add(new Text($"{clientSiteKpiSetting.ImageTargetText}").SetFontSize(CELL_FONT_SIZE * .8f))));
            table.AddCell(new Cell(1, 2)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text("DAILY WAND SCANS\n"))
                    .Add(new Text($"{clientSiteKpiSetting.WandScanTargetText}").SetFontSize(CELL_FONT_SIZE * .8f))));
            table.AddCell(new Cell(1, 2)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph()
                    .Add(new Text("DAILY PATROLS\n"))
                    .Add(new Text($"{clientSiteKpiSetting.PatrolsTargetText}").SetFontSize(CELL_FONT_SIZE * .8f))));

            table.AddCell(new Cell(1, 3)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph("EXCEPTION ALARM EVENTS")));

            table.AddCell(CreateHeaderCell("DATE"));
            table.AddCell(CreateHeaderCell("DAY"));
            table.AddCell(CreateHeaderCell("EXPECTED\nHOURS"));
            table.AddCell(CreateHeaderCell("HOURS\nCHANGE"));

            // PatrolFrequency: 1 = per day, 0 = per hr (the toggle in site settings).
            // Only days the site actually operates count: on part-week sites (e.g. weekend
            // only) the unused days keep the default per-hr toggle with all-zero values and
            // must not drag the header back to p/hr. Genuinely mixed sites keep p/hr.
            var activeDays = clientSiteKpiSetting.ClientSiteDayKpiSettings
                .Where(z => z.EmpHours.GetValueOrDefault() > 0
                         || z.ImagesTarget.GetValueOrDefault() > 0
                         || z.WandScansTarget.GetValueOrDefault() > 0
                         || z.NoOfPatrols.GetValueOrDefault() > 0)
                .ToList();
            var perDayAllDays = activeDays.Any() && activeDays.All(z => z.PatrolFrequency == 1);
            var hourlyUnit = perDayAllDays ? "p/day" : "p/hr";
            // 14-07-2026: images column changed from "p/24 hr" to "p/day" so all per-day
            // column headers use the same wording as the target text and the wand/patrol
            // columns ("p/24 hr" and "p/day" mean the same thing; the mixed wording was
            // confusing on the client-facing report). Old line kept for reference:
            //var hourlyImageUnit = perDayAllDays ? "p/24 hr" : "p/hr";
            var hourlyImageUnit = hourlyUnit;
            var hourlyImagesHeader = clientSiteKpiSetting.IsThermalCameraSite ? "Ti Only" : string.Empty;
            table.AddCell(CreateHeaderCell(clientSiteKpiSetting.IsThermalCameraSite ? "Day + Ti Total" : "Day + Total"));
            table.AddCell(CreateHeaderCell($"{hourlyImagesHeader} {hourlyImageUnit}"));
            table.AddCell(CreateHeaderCell("Total"));
            table.AddCell(CreateHeaderCell(hourlyUnit));
            table.AddCell(CreateHeaderCell(hourlyUnit));
            table.AddCell(CreateHeaderCell("Fq"));

            table.AddCell(CreateHeaderCell("DAILY LOG 2HR TIMER"));
            table.AddCell(CreateHeaderCell("IR REPORTS"));
            table.AddCell(CreateHeaderCell("FIRE or ALARMS"));
        }

        private void CreateDataRow(Table table, DailyKpiResult item, ClientSiteKpiSetting clientSiteKpiSetting, bool isHrTimerPaused)
        {
            // Date
            table.AddCell(CreateDataCell(item.DayOfDate.ToString()));

            // Name of Day
            table.AddCell(CreateDataCell(item.NameOfDay.ToString()));

            // Employee Hours
            table.AddCell(CreateDataCell(item.EmployeeHours.ToString()));

            //ActualEmployeeHours
            table.AddCell(CreateDataCell(item.ActualEmployeeHours.ToString()));

            // Image Count (from Dropbox)
            table.AddCell(CreateDataCell(item.ImageCount.ToString()));

            // Image Count Per Hour
            var cellValue = item.ImageCountPerHr.HasValue ? item.ImageCountPerHr.ToString() : "N/A";
            var cellHasBg = item.ImageCountPerHr.GetValueOrDefault() > 0;
            if (cellHasBg && item.Date > DateTime.Today) cellHasBg = false;
            var cellColor = CELL_BG_GREEN;
            if (item.ImageCountPerHr.GetValueOrDefault() > 0 &&
                item.ImagesTarget.GetValueOrDefault() > 0 &&
                item.ImageCountPerHr.GetValueOrDefault() < item.ImagesTarget.GetValueOrDefault())
            {
                cellColor = CELL_BG_RED;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor));

            // Wand Scan Count (from KOIOS API)
            table.AddCell(CreateDataCell(item.WandScanCount.ToString()));

            // Wand Scans Per Hour
            cellValue = item.WandScanCountPerHr.HasValue ? item.WandScanCountPerHr.ToString() : "N/A";
            cellHasBg = item.WandScanCountPerHr.GetValueOrDefault() > 0;
            if (cellHasBg && item.Date > DateTime.Today) cellHasBg = false;
            cellColor = CELL_BG_GREEN;
            if (item.WandScanCountPerHr.GetValueOrDefault() > 0 &&
                item.WandScansTarget.GetValueOrDefault() > 0 &&
                item.WandScanCountPerHr.GetValueOrDefault() < item.WandScansTarget.GetValueOrDefault())
            {
                cellColor = CELL_BG_RED;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor));

            // Daily Patrols
            cellValue = item.WandPatrolsRatio.HasValue ? item.WandPatrolsRatio.ToString() : "N/A";
            cellHasBg = item.WandPatrolsRatio.GetValueOrDefault() > 0;
            if (cellHasBg && item.Date > DateTime.Today) cellHasBg = false;
            cellColor = CELL_BG_GREEN;
            var patrolTarget = clientSiteKpiSetting.ClientSiteDayKpiSettings.SingleOrDefault(z => z.WeekDay.ToString() == item.NameOfDay)?.NoOfPatrols;
            if (item.WandPatrolsRatio.GetValueOrDefault() > 0 &&
                item.WandScansTarget.GetValueOrDefault() > 0 &&
                item.WandPatrolsRatio.GetValueOrDefault() < patrolTarget.GetValueOrDefault())
            {
                cellColor = CELL_BG_RED;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor));

            // Fq column - Print "N/A" when the value is null (e.g., for non-working days) instead of falling back to blank/0.
            var fqCellValue = item.WandScanFq.HasValue ? item.WandScanFq.ToString() : "N/A";
            table.AddCell(CreateDataCell(fqCellValue));


            //DAILY LOG 2HR TIMER
            cellColor = string.Empty;
            cellHasBg = false;
            cellValue = "-";
            if (!isHrTimerPaused && item.IsAcceptableLogFreq.HasValue)
            {
                cellColor = item.IsAcceptableLogFreq.Value ? CELL_BG_GREEN : CELL_BG_RED;
                cellValue = item.IsAcceptableLogFreq.Value ? "< 2hr" : "> 2hr";
                cellHasBg = true;
            }
            table.AddCell(CreateDataCell(cellValue, cellHasBg, cellColor).SetTextAlignment(TextAlignment.LEFT));

            // IR count
            table.AddCell(CreateDataCell(item.IncidentCount.ToString(), item.IncidentCount.GetValueOrDefault() > 0, CELL_BG_YELLOW_IR_COUNT));

            // IR has fire alarm event
            table.AddCell(CreateDataCell(item.HasFireOrAlarm, !string.IsNullOrEmpty(item.HasFireOrAlarm), CELL_BG_ORANGE_IR_ALARM));
        }

        private Cell CreateHeaderCell(string text)
        {
            var cell = new Cell()
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph(text));

            return cell;
        }

        private Cell CreateDataCell(string text)
        {
            var cell = new Cell();
            cell.SetFontSize(CELL_FONT_SIZE)
                .SetPaddingTop(0)
                .SetPaddingBottom(0)
                .SetKeepTogether(true);
            cell.Add(new Paragraph(text ?? string.Empty));
            return cell;
        }

        private Cell CreateDataCell(string text, bool hasBg, string colorHex)
        {
            var cell = new Cell();
            cell.SetFontSize(CELL_FONT_SIZE)
                .SetPaddingTop(0)
                .SetPaddingBottom(0);
            if (hasBg)
                cell.SetBackgroundColor(WebColors.GetRGBColor(colorHex));
            cell.Add(new Paragraph(text ?? string.Empty));
            return cell;
        }

        private Cell CreateHrDataCell(string text)
        {
            var cell = new Cell();
            cell.SetFontSize(CELL_FONT_SIZE)
                .SetPaddingTop(0)
                .SetPaddingBottom(0);

            var p = new Paragraph();
            var arTexts = text.Split(",").ToArray();
            for (int index = 0; index < arTexts.Length; index++)
            {
                var textValue = arTexts[index] ?? string.Empty;
                string textColorHex = GetHrTextColor(textValue);
                p.Add(new Text(textValue).SetFontColor(WebColors.GetRGBColor(textColorHex)));

                if (index < arTexts.Length - 1)
                    p.Add("\n");
            }
            cell.Add(p);
            return cell;
        }

        private Image GetGraphImage(List<EffortCount> effortCounts)
        {

            if (effortCounts.All(z => z.IsEmpty))
                return null;

            try
            {
                var graphFileName = IO.Path.Combine(_graphImageRootDir, DateTime.Now.ToString("ddMMyyyy_HHmm") + ".png");
                var options = new { fileName = graphFileName };

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/effort-chart.js", args: new object[] { options, effortCounts.ToArray() });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(101);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
            }
            return null;
        }

        //private Table CreateGuardReportData(List<DailyKpiGuard> monthlyKpiGuardData, DateTime fromDate)
        //{
        //    var kpiGuardTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 6, 6, 6, 12, 9, 3, 3, 3, 12, 9, 3, 3, 3, 12, 9, 3, 3, 3 })).UseAllAvailableWidth();
        //    CreateGuardReportHeader(kpiGuardTable, fromDate);
        //    foreach (var data in monthlyKpiGuardData)
        //    {
        //        kpiGuardTable.AddCell(CreateDataCell(data.Date.ToString("dd")));
        //        kpiGuardTable.AddCell(CreateDataCell(data.Date.ToString("dddd")));
        //        kpiGuardTable.AddCell(CreateDataCell(data.EmployeeHours?.ToString() ?? string.Empty));
        //        kpiGuardTable.AddCell(CreateDataCell(data.ActualEmployeeHours?.ToString() ?? string.Empty));

        //        var Shift1GuardName = (data.Shift1GuardName.Length > 0 ? data.Shift1GuardName.Split("\n")[0] : string.Empty);
        //        Shift1GuardName = Shift1GuardName.Length > 16 ? Shift1GuardName.Substring(0, 16) : Shift1GuardName; ;
        //        kpiGuardTable.AddCell(CreateDataCell(Shift1GuardName));
        //        kpiGuardTable.AddCell(CreateDataCell(data.Shift1GuardSecurityNo.Length > 0 ? data.Shift1GuardSecurityNo.Split("\n")[0] : string.Empty));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift1GuardHr1.Length > 0 ? data.Shift1GuardHr1.Split(",")[0] : string.Empty)));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift1GuardHr2.Length > 0 ? data.Shift1GuardHr2.Split(",")[0] : string.Empty)));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift1GuardHr3.Length > 0 ? data.Shift1GuardHr3.Split(",")[0] : string.Empty)));

        //        var Shift2GuardName = (data.Shift2GuardName.Length > 0 ? data.Shift2GuardName.Split("\n")[0] : string.Empty);
        //        Shift2GuardName = Shift2GuardName.Length > 16 ? Shift2GuardName.Substring(0, 16) : Shift2GuardName; ;
        //        kpiGuardTable.AddCell(CreateDataCell(Shift2GuardName));
        //        kpiGuardTable.AddCell(CreateDataCell(data.Shift2GuardSecurityNo.Length > 0 ? data.Shift2GuardSecurityNo.Split("\n")[0] : string.Empty));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift2GuardHr1.Length > 0 ? data.Shift2GuardHr1.Split(",")[0] : string.Empty)));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift2GuardHr2.Length > 0 ? data.Shift2GuardHr2.Split(",")[0] : string.Empty)));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift2GuardHr3.Length > 0 ? data.Shift2GuardHr3.Split(",")[0] : string.Empty)));

        //        var Shift3GuardName = (data.Shift3GuardName.Length > 0 ? data.Shift3GuardName.Split("\n")[0] : string.Empty);
        //        Shift3GuardName = Shift3GuardName.Length > 16 ? Shift3GuardName.Substring(0, 16) : Shift3GuardName; ;
        //        kpiGuardTable.AddCell(CreateDataCell(Shift3GuardName));
        //        kpiGuardTable.AddCell(CreateDataCell(data.Shift3GuardSecurityNo.Length > 0 ? data.Shift3GuardSecurityNo.Split("\n")[0] : string.Empty));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift3GuardHr1.Length > 0 ? data.Shift3GuardHr1.Split(",")[0] : string.Empty)));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift3GuardHr2.Length > 0 ? data.Shift3GuardHr2.Split(",")[0] : string.Empty)));
        //        kpiGuardTable.AddCell(CreateHrDataCell((data.Shift3GuardHr3.Length > 0 ? data.Shift3GuardHr3.Split(",")[0] : string.Empty)));
        //    }
        //    return kpiGuardTable;
        //}

        //p2-145 – Telematics Error-start
        private List<Table> CreateGuardReportData(List<DailyKpiGuard> monthlyKpiGuardData, DateTime fromDate)
        {
            var tables = new List<Table>();

            // Determine max guards (from Shift Block 1,2 & 3)
            int maxTables = monthlyKpiGuardData
            .Select(day => new[]
            {
                Split(day.Shift1GuardName, '\n').Count,
                Split(day.Shift2GuardName, '\n').Count,
                Split(day.Shift3GuardName, '\n').Count
            }.Max())
            .Max();

            for (int guardIndex = 0; guardIndex < maxTables; guardIndex++)
            {
                var kpiGuardTable = new Table(
                    UnitValue.CreatePercentArray(
                        new float[] { 2, 6, 6, 6, 12, 9, 3, 3, 3, 12, 9, 3, 3, 3, 12, 9, 3, 3, 3 }
                    )
                ).UseAllAvailableWidth();

                CreateGuardReportHeader(kpiGuardTable, fromDate);

                foreach (var data in monthlyKpiGuardData)
                {
                    AddDayRow(kpiGuardTable, data, guardIndex);
                }

                tables.Add(kpiGuardTable);
            }

            return tables;
        }
        private void AddDayRow(Table table, DailyKpiGuard data, int index)
        {
            table.AddCell(CreateDataCell(data.Date.ToString("dd")));
            table.AddCell(CreateDataCell(data.Date.ToString("dddd")));
            table.AddCell(CreateDataCell(data.EmployeeHours?.ToString() ?? ""));
            table.AddCell(CreateDataCell(data.ActualEmployeeHours?.ToString() ?? ""));

            AddShiftCells(
                table,
                data.Shift1GuardName,
                data.Shift1GuardSecurityNo,
                data.Shift1GuardHr1,
                data.Shift1GuardHr2,
                data.Shift1GuardHr3,
                index
            );

            AddShiftCells(
                table,
                data.Shift2GuardName,
                data.Shift2GuardSecurityNo,
                data.Shift2GuardHr1,
                data.Shift2GuardHr2,
                data.Shift2GuardHr3,
                index
            );

            AddShiftCells(
                table,
                data.Shift3GuardName,
                data.Shift3GuardSecurityNo,
                data.Shift3GuardHr1,
                data.Shift3GuardHr2,
                data.Shift3GuardHr3,
                index
            );
        }
        private void AddShiftCells(
            Table table,
            string names,
            string secNos,
            string hr1,
            string hr2,
            string hr3,
            int index)
        {
            var nameList = names.Split("\n").ToArray();
            var secList = secNos.Split("\n").ToArray();
            var h1 = hr1.Split(",").ToArray();
            var h2 = hr2.Split(",").ToArray();
            var h3 = hr3.Split(",").ToArray();

            table.AddCell(CreateDataCell(index < nameList.Length ? nameList[index] : ""));
            table.AddCell(CreateDataCell(index < secList.Length ? secList[index] : ""));
            table.AddCell(CreateHrDataCell(index < h1.Length ? h1[index] : ""));
            table.AddCell(CreateHrDataCell(index < h2.Length ? h2[index] : ""));
            table.AddCell(CreateHrDataCell(index < h3.Length ? h3[index] : ""));
        }

        private Cell CreateDataCell(object value)
        {
            throw new NotImplementedException();
        }

        private List<string> Split(string value, char separator)
        {
            return string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(separator).Select(x => x.Trim()).ToList();
        }

        //p2-145 – Telematics Error-end



        private Table CreateGuardDetailsData(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance)
        {

            var guards = monthlyDataGuard
                .Select(guardLogin => guardLogin.Guard)
                .Distinct()
                .ToArray();
            List<int> complianceDataCounts = new List<int>();
            foreach (var guard in guards)
            {
                var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsCompliance(guard.Id);
                complianceDataCounts.Add(monthlyDataGuardComplianceData.Count);
            }
            int[] countsArray = complianceDataCounts.ToArray();
            int largestNumber;
            if (countsArray.Length > 0)
            {
                largestNumber = countsArray.Max();

            }
            else
            {
                largestNumber = 0;
            }

            int numColumns = monthlyDataGuardCompliance.Count;
            float[] columnPercentages = new float[largestNumber + 2];

            var kpiGuardTable = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();
            CreateGuardDetailsHeader(kpiGuardTable, monthlyDataGuard);

            int maxComplianceCount = guards.Select(g => _viewDataService.GetKpiGuardDetailsCompliance(g.Id).Count).Max();

            foreach (var guard in guards)
            {
                var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsCompliance(guard.Id);
                kpiGuardTable.AddCell(CreateDataCell(guard.Name));
                kpiGuardTable.AddCell(CreateDataCell(guard.SecurityNo));

                for (int i = 0; i < maxComplianceCount; i++)
                {
                    var cellColor = "";
                    DateTime? alertDate = null;
                    var compliance = i < monthlyDataGuardComplianceData.Count ? monthlyDataGuardComplianceData[i] : null;
                    if (compliance != null && compliance.ExpiryDate != null && compliance.ExpiryDate.ToString() != "")
                    {
                        alertDate = Convert.ToDateTime(compliance.ExpiryDate).AddDays(-45);
                    }

                    if (alertDate <= DateTime.Today && compliance.ExpiryDate > DateTime.Today)
                    {
                        cellColor = CELL_BG_YELLOW;
                    }
                    else if (compliance?.ExpiryDate < DateTime.Today)
                    {
                        cellColor = CELL_BG_RED;
                    }
                    else if (compliance?.ExpiryDate == null)
                    {
                        cellColor = "white";
                    }
                    else
                    {
                        cellColor = "#96e3ac";
                    }

                    DateTime? expiryDate = compliance?.ExpiryDate?.Date;
                    string expiryDateString = expiryDate.HasValue ? expiryDate.Value.ToString("dd/MM/yyyy") : "";
                    kpiGuardTable.AddCell(CreateDataCell(expiryDateString, true, cellColor));
                }

            }



            return kpiGuardTable;
        }
        private Table CreateGuardDetailsLicenseAndCompliance(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {
            var guards = monthlyDataGuard.Select(guardLogin => guardLogin.Guard).Distinct().OrderBy(x=> x.Name).ToArray();
            var hrGrpStr = RemoveBrackets(hrGroupName).Trim();
            var activeRefNos = new HashSet<string>();
            var allGuardComps = new Dictionary<int, List<GuardComplianceAndLicense>>();

            if (Enum.TryParse<HrGroup>(hrGrpStr, out var hrGrpVal))
            {
                foreach (var guard in guards)
                {
                    var comps = _viewDataService.GetKpiGuardDetailsComplianceAndLicenseHR(guard.Id, hrGrpVal);
                    allGuardComps[guard.Id] = comps;
                }
            }

            var HTList = IsDownselect ? _viewDataService.GetHRSettingsCriticalDoc(Id, CriticalDocumentID) : _viewDataService.GetHRSettings(Id);

            var refinedHTList = HTList.Where(item => item.hrSettingsClientStates.Any(x => x.State == ClientSiteState) || item.hrSettingsClientSites.Any(x => x.ClientSiteId == clientSiteId)).ToList();
            HTList = refinedHTList;

            var displayedRefNos = new List<HrSettings>();
            foreach (var item in HTList)
            {
                var SiteConditions = item.hrSettingsClientSites;
                var StateConditions = item.hrSettingsClientStates;
                //bool isEligible = (SiteConditions.Count == 0 && StateConditions.Count == 0) || SiteConditions.Any(x => x.ClientSiteId == clientSiteId);
                bool isEligible = (SiteConditions.Count == 0 && StateConditions.Count != 0) || SiteConditions.Any(x => x.ClientSiteId == clientSiteId);

                if (isEligible)
                {
                    displayedRefNos.Add(item);
                    var refNo = item.ReferenceNo ?? "";
                    var normRef = new string(refNo.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
                    foreach (var comps in allGuardComps.Values)
                    {
                        if (comps.Any(c => !string.IsNullOrEmpty(c.Description) && new string(c.Description.Where(char.IsLetterOrDigit).ToArray()).ToUpper().Contains(normRef)))
                        {
                            activeRefNos.Add(refNo);
                            break;
                        }
                    }
                }
            }

            int numBaseCols = (Id == 1) ? 3 : 2;
            int numHRCols = displayedRefNos.Count;
            var rc = UnitValue.CreatePercentArray(numBaseCols + numHRCols);

            float totalWidth = 812f;
            float fixedBase = 130f + (Id == 1 ? 60f : 0f); // Name(80), License(50), DOH(60)
            float emptyWidth = 20f;
            int activeCount = displayedRefNos.Count(r => activeRefNos.Contains(r.ReferenceNo ?? ""));
            float available = totalWidth - fixedBase - ((numHRCols - activeCount) * emptyWidth);
            float activeWidth = activeCount > 0 ? available / activeCount : available / Math.Max(1, numHRCols);

            int j = 0;
            var hrColWidths = new Dictionary<string, float>();
            rc[j++] = new UnitValue(UnitValue.POINT, 80);
            rc[j++] = new UnitValue(UnitValue.POINT, 50);
            if (Id == 1) rc[j++] = new UnitValue(UnitValue.POINT, 60);

            foreach (var item in displayedRefNos)
            {
                var refNo = item.ReferenceNo ?? "";
                float w = activeRefNos.Contains(refNo) ? activeWidth : emptyWidth;
                hrColWidths[refNo] = w;
                rc[j++] = new UnitValue(UnitValue.POINT, w);
            }

            var kpiGuardTable = new Table(rc).UseAllAvailableWidth();
            CreateGuardDetailsNewHeader(kpiGuardTable, monthlyDataGuard, hrGroupName, Id, clientSiteId, ClientSiteState, IsDownselect, CriticalDocumentID);

            foreach (var guard in guards)
            {
                List<GuardComplianceAndLicense> monthlyDataGuardComplianceData = allGuardComps.ContainsKey(guard.Id) ? allGuardComps[guard.Id] : new List<GuardComplianceAndLicense>();
                kpiGuardTable.AddCell(CreateDataCell(guard.Name.Length > 16 ? guard.Name[..16] : guard.Name));
                kpiGuardTable.AddCell(CreateDataCell(guard.SecurityNo));
                if (Id == 1)
                {
                    string DOH = guard.DateEnrolled.HasValue ? guard.DateEnrolled.Value.ToString("dd/MM/yyyy") : string.Empty;
                    kpiGuardTable.AddCell(CreateDataCell(DOH));
                }

                foreach (var item in displayedRefNos)
                {
                    var referenceNo = item.ReferenceNo ?? "";
                    var normalizedRefNo = new string(referenceNo.Where(char.IsLetterOrDigit).ToArray()).ToUpper();

                    var matchingDescription = monthlyDataGuardComplianceData
                        .Where(data =>
                        {
                            if (string.IsNullOrEmpty(data.Description)) return false;
                            var normalizedDocDesc = new string(data.Description.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
                            return normalizedDocDesc.Contains(normalizedRefNo);
                        })
                        .FirstOrDefault();

                    var cellColor = "white";
                    DateTime? expiryDate = matchingDescription?.ExpiryDate?.Date;
                    float colWidth = hrColWidths.ContainsKey(referenceNo) ? hrColWidths[referenceNo] : 45f;
                    string dateFormat = colWidth > 45 ? "dd/MM/yyyy" : "dd/MM\nyyyy";
                    string expiryDateString = expiryDate.HasValue ? expiryDate.Value.ToString(dateFormat) : "";

                    if (matchingDescription != null && expiryDate.HasValue)
                    {
                        DateTime alertDate = expiryDate.Value.AddDays(-45);
                        if (alertDate <= DateTime.Today && expiryDate > DateTime.Today)
                        {
                            cellColor = CELL_BG_YELLOW;
                        }
                        else if (expiryDate < DateTime.Today)
                        {
                            var daysAfterExpiry = (DateTime.Today.Date - expiryDate.Value.Date).TotalDays;
                            // EXPLANATION: If the record is expired but marked as "Pending" (toggle ON), 
                            // it will show an ORANGE clock to indicate a grace period.
                            // After 99 days past the expiry date, this grace period expires and it forcefully turns RED.
                            if (matchingDescription.IsPending && daysAfterExpiry <= 99)
                            {
                                cellColor = CELL_BG_ORANGE;
                                int digits = daysAfterExpiry.ToString().Length;
                                if(digits == 1 )
                                    expiryDateString = "Pending - 0" + daysAfterExpiry.ToString();
                                else
                                    expiryDateString = "Pending - " + daysAfterExpiry.ToString();
                            }
                            else
                            {
                                cellColor = CELL_BG_RED;
                            }
                            
                        }
                        else
                        {
                            cellColor = "#96e3ac";
                        }

                        if (matchingDescription.DateType == true)
                        {
                            cellColor = "#96e3ac";
                            expiryDateString += "(I)";
                        }
                    }

                    kpiGuardTable.AddCell(CreateDataCell(expiryDateString, true, cellColor));
                }
            }

            return kpiGuardTable;
        }

        private void CreateGuardDetailsNewHeader(Table table, List<GuardLogin> monthlyDataGuard, string hrGroupname, int id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {
            try
            {
                var HTList = IsDownselect ? _viewDataService.GetHRSettingsCriticalDoc(id, CriticalDocumentID) : _viewDataService.GetHRSettings(id);

                var refinedHTList = HTList.Where(item => item.hrSettingsClientStates.Any(x => x.State == ClientSiteState) || item.hrSettingsClientSites.Any(x => x.ClientSiteId == clientSiteId)).ToList();

                HTList = refinedHTList;

                // Row 1: Group label + Reference numbers
                if (id == 1)
                {
                    table.AddCell(new Cell(1, 3).SetFontSize(CELL_FONT_SIZE).SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER)).Add(new Paragraph(hrGroupname)));
                }
                else
                {
                    table.AddCell(new Cell(1, 2).SetFontSize(CELL_FONT_SIZE).SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER)).Add(new Paragraph(hrGroupname)));
                }

                foreach (var item in HTList)
                {
                    var SiteConditions = item.hrSettingsClientSites;
                    var StateConditions = item.hrSettingsClientStates;
                    //bool isEligible = (SiteConditions.Count == 0 && StateConditions.Count == 0) || SiteConditions.Any(x => x.ClientSiteId == clientSiteId);
                    bool isEligible = (SiteConditions.Count == 0 && StateConditions.Count != 0) || SiteConditions.Any(x => x.ClientSiteId == clientSiteId);

                    if (isEligible)
                    {
                        var referenceNo = item.ReferenceNo ?? "";
                        table.AddCell(new Cell(1, 1).SetFontSize(CELL_FONT_SIZE).SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER)).Add(new Paragraph(referenceNo)));
                    }
                }

                // Row 2: Name, License, DOH labels + Empty cells for columns
                table.AddCell(CreateHeaderCell("Name\n"));
                table.AddCell(CreateHeaderCell("License"));
                if (id == 1) table.AddCell(CreateHeaderCell("DOH"));

                foreach (var item in HTList)
                {
                    var SiteConditions = item.hrSettingsClientSites;
                    var StateConditions = item.hrSettingsClientStates;
                    //bool isEligible = (SiteConditions.Count == 0 && StateConditions.Count == 0) || SiteConditions.Any(x => x.ClientSiteId == clientSiteId);
                    bool isEligible = (SiteConditions.Count == 0 && StateConditions.Count != 0) || SiteConditions.Any(x => x.ClientSiteId == clientSiteId);

                    if (isEligible)
                    {
                        table.AddCell(CreateHeaderCell(""));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in CreateGuardDetailsNewHeader: {ex.Message}");
                throw;
            }
        }

        private void CreateGuardDetailsNewHeaderHR(Table table, List<GuardLogin> monthlyDataGuard, string hrGroupname, int id)
        {
            try
            {
                Color CELL_BG_GREY_HEADER = new DeviceRgb(211, 211, 211);
                table.AddCell(new Cell(1, 1).SetFontSize(CELL_FONT_SIZE).SetBackgroundColor(CELL_BG_GREY_HEADER).Add(new Paragraph().Add(new Text($"Reference No"))));
                table.AddCell(new Cell(1, 1).SetFontSize(CELL_FONT_SIZE).SetBackgroundColor(CELL_BG_GREY_HEADER).Add(new Paragraph().Add(new Text($"Description"))));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
        private string RemoveBrackets(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string pattern = @"\[.*?\]|\{.*?\}|\(.*?\)";
            return Regex.Replace(input, pattern, string.Empty);
        }

        private Table CreateGuardDetailsLicenseAndComplianceHR1(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {
            int HTListCount = 0;
            var HTList = new List<HrSettings>();
            if (IsDownselect == true)
            {
                HTList = _viewDataService.GetHRSettingsCriticalDoc(Id, CriticalDocumentID);
            }
            else
            {
                HTList = _viewDataService.GetHRSettings(Id);
            }

            var refinedHTList = HTList.Where(item => item.hrSettingsClientStates.Any(x => x.State == ClientSiteState) || item.hrSettingsClientSites.Any(x => x.ClientSiteId == clientSiteId)).ToList();

            HTList = refinedHTList;

            if (HTList.Count > 0)
            {
                foreach (var item in HTList)
                {
                    //var SiteConditions = item.hrSettingsClientSites;
                    //var StateConditions = item.hrSettingsClientStates;
                    //if (SiteConditions.Count != 0 || StateConditions.Count != 0)
                    //{
                    //    var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                    //    var SelctedStateExist = StateConditions.Where(x => x.State == ClientSiteState).ToList();
                    //    if (SelctedStateExist.Count != 0 && SelctedSiteExist.Count == 0)
                    //    {
                    //        HTListCount++;
                    //    }
                    //    else if (SelctedSiteExist.Count != 0)
                    //    {
                    //        if (SelctedStateExist.Count != 0)
                    //        {
                    //            HTListCount++;
                    //        }
                    //        else
                    //        {
                    //            HTListCount++;
                    //        }

                    //    }
                    //}
                    //else
                    //{
                    //    HTListCount++;
                    //}

                    var SiteConditions = item.hrSettingsClientSites;
                    var StateConditions = item.hrSettingsClientStates;

                    if (SiteConditions.Count == 0 && StateConditions.Count == 0)
                    {
                        // dont add to list
                    }
                    else if (StateConditions.Count != 0 && SiteConditions.Count == 0)
                    {
                        var SelctedStateExist = StateConditions.Where(x => x.State == ClientSiteState).ToList();
                        if (SelctedStateExist.Count != 0)
                        {
                            //Add to list if state is selected and no site is selected
                            HTListCount++;
                        }
                    }
                    else if (StateConditions.Count == 0 && SiteConditions.Count != 0)
                    {
                        var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                        if (SelctedSiteExist.Count != 0)
                        {
                            //Add to list if site is selected and no state is selected
                            HTListCount++;
                        }
                    }
                    else if (StateConditions.Count != 0 && SiteConditions.Count != 0)
                    {
                        var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                        if (SelctedSiteExist.Count != 0)
                        {
                            HTListCount++;
                        }
                    }
                }

            }
            var graphTable = new Table(UnitValue.CreatePercentArray(3)) // Two columns
       .UseAllAvailableWidth()
       .SetMarginTop(5)
       .SetKeepTogether(true);

            // Add first table as a cell
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGuardDetailsLicenseAndComplianceHRTbl1(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName, Id, clientSiteId, ClientSiteState, IsDownselect, CriticalDocumentID)));

            if (HTListCount > 9)
            {
                // Add second table as a cell
                graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGuardDetailsLicenseAndComplianceHRTbl2(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName, Id, clientSiteId, ClientSiteState, IsDownselect, CriticalDocumentID)));
            }
            if (HTListCount > 18)
            {
                graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGuardDetailsLicenseAndComplianceHRTbl3(monthlyDataGuard, monthlyDataGuardCompliance, hrGroupName, Id, clientSiteId, ClientSiteState, IsDownselect, CriticalDocumentID)));
            }
            return graphTable;

        }

        private Table CreateGuardDetailsLicenseAndComplianceHRTbl1(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {

            float[] columnPercentages = new float[2];
            var kpiGuardTable1 = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();


            CreateGuardDetailsNewHeaderHR(kpiGuardTable1, monthlyDataGuard, hrGroupName, Id);
            var HTList = new List<HrSettings>();
            if (IsDownselect == true)
            {
                HTList = _viewDataService.GetHRSettingsCriticalDoc(Id, CriticalDocumentID);
            }
            else
            {
                HTList = _viewDataService.GetHRSettings(Id);
            }

            var refinedHTList = HTList.Where(item => item.hrSettingsClientStates.Any(x => x.State == ClientSiteState) || item.hrSettingsClientSites.Any(x => x.ClientSiteId == clientSiteId)).ToList();

            HTList = refinedHTList;

            if (HTList.Count > 0)
            {
                //var filteredList = new List<HrSettings>();

                //foreach (var item in HTList)
                //{
                //    var SiteConditions = item.hrSettingsClientSites;
                //    var StateConditions = item.hrSettingsClientStates;

                //    if (SiteConditions.Count != 0 || StateConditions.Count != 0)
                //    {
                //        var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                //        var SelctedStateExist = StateConditions.Where(x => x.State == ClientSiteState).ToList();

                //        if (SelctedStateExist.Count != 0 && SelctedSiteExist.Count == 0)
                //        {
                //            filteredList.Add(item);
                //        }
                //        else if (SelctedSiteExist.Count != 0)
                //        {
                //            if (SelctedStateExist.Count != 0)
                //            {
                //                filteredList.Add(item);
                //            }
                //            else
                //            {
                //                filteredList.Add(item);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        filteredList.Add(item);
                //    }
                //}

                var filteredList = FilterHRSettings(HTList, clientSiteId, ClientSiteState);

                // Take only the first 9 relevant items
                foreach (var item in filteredList.Take(9))
                {
                    kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                    kpiGuardTable1.AddCell(CreateDataCell(item.Description));
                }
            }




            return kpiGuardTable1;
        }
        private Table CreateGuardDetailsLicenseAndComplianceHRTbl2(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {

            float[] columnPercentages = new float[2];
            var kpiGuardTable1 = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();


            CreateGuardDetailsNewHeaderHR(kpiGuardTable1, monthlyDataGuard, hrGroupName, Id);

            var HTList = IsDownselect
                ? _viewDataService.GetHRSettingsCriticalDoc(Id, CriticalDocumentID)
                : _viewDataService.GetHRSettings(Id);

            var refinedHTList = HTList.Where(item => item.hrSettingsClientStates.Any(x => x.State == ClientSiteState) || item.hrSettingsClientSites.Any(x => x.ClientSiteId == clientSiteId)).ToList();

            HTList = refinedHTList;

            if (HTList.Count > 0)
            {
                var filteredList = FilterHRSettings(HTList, clientSiteId, ClientSiteState);

                // Skip the first 9 items already used in the first table and take the next 9
                foreach (var item in filteredList.Skip(9).Take(9))
                {
                    kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                    kpiGuardTable1.AddCell(CreateDataCell(item.Description));
                }
            }

            return kpiGuardTable1;

        }

        private Table CreateGuardDetailsLicenseAndComplianceHRTbl3(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {

            float[] columnPercentages = new float[2];
            var kpiGuardTable1 = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();


            CreateGuardDetailsNewHeaderHR(kpiGuardTable1, monthlyDataGuard, hrGroupName, Id);

            var HTList = new List<HrSettings>();
            if (IsDownselect == true)
            {
                HTList = _viewDataService.GetHRSettingsCriticalDoc(Id, CriticalDocumentID);
            }
            else
            {
                HTList = _viewDataService.GetHRSettings(Id);
            }

            var refinedHTList = HTList.Where(item => item.hrSettingsClientStates.Any(x => x.State == ClientSiteState) || item.hrSettingsClientSites.Any(x => x.ClientSiteId == clientSiteId)).ToList();

            HTList = refinedHTList;

            if (HTList.Count > 0)
            {
                var filteredList = FilterHRSettings(HTList, clientSiteId, ClientSiteState);

                // Skip the first 9 items already used in the first table and take the next 9
                foreach (var item in filteredList.Skip(18).Take(9))
                {
                    kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                    kpiGuardTable1.AddCell(CreateDataCell(item.Description));
                }
            }

            return kpiGuardTable1;
        }
        private List<HrSettings> FilterHRSettings(List<HrSettings> HTList, int clientSiteId, string ClientSiteState)
        {
            var filteredList = new List<HrSettings>();

            foreach (var item in HTList)
            {
                //var SiteConditions = item.hrSettingsClientSites;
                //var StateConditions = item.hrSettingsClientStates;

                //if (SiteConditions.Count != 0 || StateConditions.Count != 0)
                //{
                //    var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                //    var SelctedStateExist = StateConditions.Where(x => x.State == ClientSiteState).ToList();

                //    if (SelctedStateExist.Count != 0 && SelctedSiteExist.Count == 0)
                //    {
                //        filteredList.Add(item);
                //    }
                //    else if (SelctedSiteExist.Count != 0)
                //    {
                //        if (SelctedStateExist.Count != 0)
                //        {
                //            filteredList.Add(item);
                //        }
                //        else
                //        {
                //            filteredList.Add(item);
                //        }
                //    }
                //}
                //else
                //{
                //    filteredList.Add(item);
                //}

                var SiteConditions = item.hrSettingsClientSites;
                var StateConditions = item.hrSettingsClientStates;

                if (SiteConditions.Count == 0 && StateConditions.Count == 0)
                {
                    // dont add to list
                }
                else if (StateConditions.Count != 0 && SiteConditions.Count == 0)
                {
                    var SelctedStateExist = StateConditions.Where(x => x.State == ClientSiteState).ToList();
                    if (SelctedStateExist.Count != 0)
                    {
                        //Add to list if state is selected and no site is selected
                        filteredList.Add(item);
                    }
                }
                else if (StateConditions.Count == 0 && SiteConditions.Count != 0)
                {
                    var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                    if (SelctedSiteExist.Count != 0)
                    {
                        //Add to list if site is selected and no state is selected
                        filteredList.Add(item);
                    }
                }
                else if (StateConditions.Count != 0 && SiteConditions.Count != 0)
                {
                    var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                    if (SelctedSiteExist.Count != 0)
                    {
                        filteredList.Add(item);
                    }
                }
            }


            return filteredList;
        }
        private Table CreateGuardDetailsLicenseAndComplianceHR(List<GuardLogin> monthlyDataGuard, List<GuardCompliance> monthlyDataGuardCompliance, string hrGroupName, int Id, int clientSiteId, string ClientSiteState, bool IsDownselect, int CriticalDocumentID)
        {

            float[] columnPercentages = new float[2];
            var kpiGuardTable1 = new Table(UnitValue.CreatePercentArray(columnPercentages)).UseAllAvailableWidth();


            CreateGuardDetailsNewHeaderHR(kpiGuardTable1, monthlyDataGuard, hrGroupName, Id);
            var HTList = new List<HrSettings>();
            if (IsDownselect == true)
            {
                HTList = _viewDataService.GetHRSettingsCriticalDoc(Id, CriticalDocumentID);
            }
            else
            {
                HTList = _viewDataService.GetHRSettings(Id);
            }

            if (HTList.Count > 0)
            {
                foreach (var item in HTList)
                {
                    var SiteConditions = item.hrSettingsClientSites;
                    var StateConditions = item.hrSettingsClientStates;
                    if (SiteConditions.Count != 0 || StateConditions.Count != 0)
                    {
                        var SelctedSiteExist = SiteConditions.Where(x => x.ClientSiteId == clientSiteId).ToList();
                        var SelctedStateExist = StateConditions.Where(x => x.State == ClientSiteState).ToList();
                        if (SelctedStateExist.Count != 0 && SelctedSiteExist.Count != 0)
                        {
                            kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                            kpiGuardTable1.AddCell(CreateDataCell(item.Description));
                        }
                        else if (SelctedSiteExist.Count != 0)
                        {
                            if (SelctedStateExist.Count != 0)
                            {
                                kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                                kpiGuardTable1.AddCell(CreateDataCell(item.Description));

                            }
                            else
                            {
                                kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                                kpiGuardTable1.AddCell(CreateDataCell(item.Description));
                            }

                        }
                    }
                    else
                    {
                        kpiGuardTable1.AddCell(CreateDataCell(item.ReferenceNo));
                        kpiGuardTable1.AddCell(CreateDataCell(item.Description));
                    }

                }

            }
            //var ClientDetailsList = new List<ClientDetailsData>();
            //foreach (var hrSetting in HTList)
            //{
            //    foreach (var site in hrSetting.hrSettingsClientSites)
            //    {
            //        var combinedData = new ClientDetailsData
            //        {
            //            ClientSiteId= site.ClientSiteId,
            //            Description= hrSetting.Description,
            //            ReferenceNo= hrSetting.ReferenceNo,
            //            HRGroupID=hrSetting.HRGroupId
            //        };
            //        ClientDetailsList.Add(combinedData);
            //    }
            //}
            //for (int i = 0; i < HTList.Count; i++)
            //{

            //    kpiGuardTable1.AddCell(CreateDataCell(HTList[i].ReferenceNo));
            //    kpiGuardTable1.AddCell(CreateDataCell(HTList[i].Description));


            //}

            return kpiGuardTable1;
        }

        private static string GetHrTextColor(string hrValue)
        {
            if (hrValue == "Y")
                return CELL_FONT_GREEN;

            if (hrValue == "E")
                return CELL_FONT_RED;
            if (hrValue == "N")
                return CELL_FONT_YELLOW;
            if (hrValue == "P")
                return CELL_FONT_ORANGE;
            

            return string.Empty;
        }

        private void CreateGuardReportHeader(Table table, DateTime fromDate)
        {
            table.AddCell(new Cell(1, 4)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .Add(new Paragraph().Add(new Text($"MONTH/YEAR: {fromDate.ToString("MMM").ToUpper()} {fromDate.Year}\n"))));
            table.AddCell(new Cell(1, 5)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph().Add(new Text("Shift Block 1"))));
            table.AddCell(new Cell(1, 5)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph().Add(new Text("Shift Block 2"))));
            table.AddCell(new Cell(1, 5)
                .SetFontSize(CELL_FONT_SIZE)
                .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                 .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph().Add(new Text("Shift Block 3"))));
            table.AddCell(CreateHeaderCell("Date"));
            table.AddCell(CreateHeaderCell("Day"));
            table.AddCell(CreateHeaderCell("Expected Hours"));
            table.AddCell(CreateHeaderCell("Hours Change"));
            table.AddCell(CreateHeaderCell("Guard Name"));
            table.AddCell(CreateHeaderCell("Security No"));
            table.AddCell(CreateHeaderCell("HR 1"));
            table.AddCell(CreateHeaderCell("HR 2"));
            table.AddCell(CreateHeaderCell("HR 3"));
            table.AddCell(CreateHeaderCell("Guard Name"));
            table.AddCell(CreateHeaderCell("Security No"));
            table.AddCell(CreateHeaderCell("HR 1"));
            table.AddCell(CreateHeaderCell("HR 2"));
            table.AddCell(CreateHeaderCell("HR 3"));
            table.AddCell(CreateHeaderCell("Guard Name"));
            table.AddCell(CreateHeaderCell("Security No"));
            table.AddCell(CreateHeaderCell("HR 1"));
            table.AddCell(CreateHeaderCell("HR 2"));
            table.AddCell(CreateHeaderCell("HR 3"));
        }
        private void CreateGuardDetailsHeader(Table table, List<GuardLogin> monthlyDataGuard)
        {
            try
            {
                List<int> complianceDataCounts = new List<int>();
                var guards = monthlyDataGuard
                    .Select(guardLogin => guardLogin.Guard)
                    .Distinct()
                    .ToArray();

                foreach (var guard in guards)
                {
                    var monthlyDataGuardComplianceData = _viewDataService.GetKpiGuardDetailsCompliance(guard.Id);
                    complianceDataCounts.Add(monthlyDataGuardComplianceData.Count);
                }

                int[] countsArray = complianceDataCounts.ToArray();
                int largestNumber;

                if (countsArray.Length > 0)
                {
                    largestNumber = countsArray.Max();
                }
                else
                {
                    largestNumber = 0;
                }

                table.AddCell(new Cell(1, 2)
                    .SetFontSize(CELL_FONT_SIZE)
                    .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                    .Add(new Paragraph().Add(new Text($"\n"))));

                for (int i = 0; i < largestNumber; i++)
                {
                    string sequentialNumber = (i + 1).ToString("D2");
                    table.AddCell(new Cell(1, 1)
                        .SetFontSize(CELL_FONT_SIZE)
                        .SetBackgroundColor(WebColors.GetRGBColor(CELL_BG_BLUE_HEADER))
                        .Add(new Paragraph().Add(new Text(sequentialNumber))));
                }

                table.AddCell(CreateHeaderCell($"Name\n"));
                table.AddCell(CreateHeaderCell("C4i+License"));

                var firstGuardId = monthlyDataGuard.Select(guardLogin => guardLogin.GuardId).Distinct().FirstOrDefault();
                var monthlyDataGuardComplianceData1 = _viewDataService.GetKpiGuardDetailsCompliance(firstGuardId);

                for (int i = 0; i < largestNumber; i++)
                {
                    if (i < monthlyDataGuardComplianceData1.Count)
                    {
                        var Description = monthlyDataGuardComplianceData1[i].Description;

                        if (!string.IsNullOrEmpty(Description))
                        {
                            table.AddCell(CreateHeaderCell(Description));
                        }
                        else
                        {
                            table.AddCell(CreateHeaderCell(""));
                        }
                    }
                    else
                    {

                        table.AddCell(CreateHeaderCell(""));
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception here, for example, log it or show an error message.
                Console.WriteLine($"An error occurred: {ex.Message}");
                // You can rethrow the exception if needed.
                throw;
            }
        }
        //p2-184-hr-charts-start
        // create hr charts for each individual sites
        private Table CreateHRGraphsTables(PatrolRequest ReportRequest)
        {
            int[]? guardIds = null;
            var clientsites = _viewDataService.GetGuardLoginsWithClientTypesAndSites(ReportRequest);

            if (clientsites.Count() > 0)
            {
                guardIds = clientsites.Select(x => x.GuardId).Distinct().ToArray();
            }
            var graphTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth()
                .SetMarginTop(5)
                .SetKeepTogether(true);
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateHRGraphsTable1( guardIds)));
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateHRGraphsTable2(guardIds)));
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateHRGraphsTable3(guardIds)));
            return graphTable;
        }
        private Table CreateHRGraphsTable1( int[]? guardIds)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 49, 2, 49 })).UseAllAvailableWidth().SetMarginBottom(5);
          
            var activeAndInActive = GetActiveAndInactiveGuardHrReport(guardIds).ToList();
            chartDataTable.AddCell(GetChartHeaderCell("Active Guard Vs Inactive Guard", " (Count: " + activeAndInActive.Count() + ")"));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var genderReport = GetGenderBasedGuardHrReport(guardIds).ToList();

            chartDataTable.AddCell(GetChartHeaderCell("Gender", "(Count: " + genderReport.Count() +")"));

            // row 1 blank cell
            //chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            //chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY COLOUR CODE", "\nTotal Color Code Count: " + patrolDataReport.ColorCodePercentage.Count));
            var hrChartData1 = activeAndInActive.Cast<dynamic>()
    .Select(x => new KeyValuePair<string, double>(
        (string)x.Status,
        (double)x.Percentage))
    .OrderByDescending(x => x.Key)
    .ToArray();

            var hrChartData1PieChartImage = GetChartImage(hrChartData1);
            chartDataTable.AddCell(GetChartImageCell(hrChartData1PieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
            var hrChartData2 = genderReport.Cast<dynamic>()
    .Select(x => new KeyValuePair<string, double>(
        (string)x.Key,
        (double)x.Value))
    .OrderByDescending(x => x.Key)
    .ToArray();
            
            var hrChartData2PieChartImage = GetChartImage(hrChartData2);
            chartDataTable.AddCell(GetChartImageCell(hrChartData2PieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            //var colorCodeChartImage = GetChartImage(patrolDataReport.ColorCodePercentage.OrderByDescending(z => z.Value).ToArray());
            //chartDataTable.AddCell(GetChartImageCell(colorCodeChartImage));

            return chartDataTable;
        }
        private Table CreateHRGraphsTable2(int[]? guardIds)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 49, 2, 49 })).UseAllAvailableWidth().SetMarginBottom(5);
         
            var yearOfOnBoradingBarChart = GetYearofOnBoardingGuardHrReportBarchart(guardIds).ToList();
            chartDataTable.AddCell(GetChartHeaderCell("Year of Onboarding", " (Count: " + yearOfOnBoradingBarChart.Count() + ")"));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var attributionReport = GetGuardAttributionPerAnnumReport(guardIds).ToList();

            chartDataTable.AddCell(GetChartHeaderCell("Attrition Per Annum", "(Count: " + attributionReport.Count() + ")"));

            // row 1 blank cell
            //chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            //chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY COLOUR CODE", "\nTotal Color Code Count: " + patrolDataReport.ColorCodePercentage.Count));
            var hrChartData1 = yearOfOnBoradingBarChart.Cast<dynamic>()
    .Select(x => new KeyValuePair<string, double>(
        (string)x.Status,
        (double)x.Percentage))
    .OrderByDescending(x => x.Key)
    .ToArray();

            var hrChartData1BarChartImage = GetChartImage(hrChartData1,ChartType.Bar);
            chartDataTable.AddCell(GetChartImageCell(hrChartData1BarChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
            var hrChartData2 = attributionReport.Cast<dynamic>()
    .Select(x => new KeyValuePair<string, double>(
        (string)x.Year,
        (double)x.Percentage))
    .OrderByDescending(x => x.Key)
    .ToArray();

            var hrChartData2PieChartImage = GetChartImage(hrChartData2);
            chartDataTable.AddCell(GetChartImageCell(hrChartData2PieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

           
            return chartDataTable;
        }
        private Table CreateHRGraphsTable3(int[]? guardIds)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 49,51 })).UseAllAvailableWidth().SetMarginBottom(5);
          
            var languageReport = GetGuardLanguagesHrReport(guardIds).ToList();
            chartDataTable.AddCell(GetChartHeaderCell("LOTE", " (Count: " + languageReport.Count() + ")"));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

           
            // row 1 blank cell
            //chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            //chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY COLOUR CODE", "\nTotal Color Code Count: " + patrolDataReport.ColorCodePercentage.Count));
            var hrChartData1 = languageReport
                .Cast<dynamic>()
    .Select(x => new KeyValuePair<string, double>(
        (string)x.Language,
        (double)x.Percentage))
    .OrderByDescending(x => x.Key)
    .ToArray();

            var hrChartData1PieChartImage = GetChartImage(hrChartData1);
            chartDataTable.AddCell(GetChartImageCell(hrChartData1PieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));
         
            


            return chartDataTable;
        }
        public IEnumerable<object> GetActiveAndInactiveGuardHrReport(int[]? guardIds)
        {

            var guards = _viewDataService.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));
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
         
            var guards = _viewDataService.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));
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
        public IEnumerable<object> GetYearofOnBoardingGuardHrReportBarchart(int[]? guardIds)
        {
           
            var guards = _viewDataService.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));

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
        public IEnumerable<object> GetGuardAttributionPerAnnumReport(int[]? guardIds)
        {
            
            var inactiveGuards = _viewDataService.GetInActiveGuardDetails().Where(x =>
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
        public IEnumerable<object> GetGuardLanguagesHrReport(int[]? guardIds)
        {
      
            var guards = _viewDataService.GetGuards().Where(x => (guardIds == null) || (guardIds.Contains(x.Id)));
            //var guardsIds = _guardDataProvider.GetGuards().Select(x=>x.Id).ToArray();

            var languages = _viewDataService.GetGuardLanguages(guards.Select(z => z.Id).ToArray()).ToList();
            // Total count of guards
            int totalLanguagesCount = languages.Count();

            // Group, count, and calculate percentages for pie chart
            var groupedByLanguage = languages
                .GroupBy(g => g.LanguageMaster.Language.ToString()) // Convert year to string
                .Select(g => new
                {
                    Language = g.Key,
                    Count = g.Count(),
                    Percentage = Math.Round((double)g.Count() / totalLanguagesCount * 100, 2) // Calculate percentage and round to 2 decimals
                })
                .OrderBy(kvp => kvp.Language); // Sort by year (string representation)

            return groupedByLanguage;
        }
        //p2-184-hr-charts-end
        //NEWLY ADDED-START
        private Table CreateGraphsTables(PatrolDataReport patrolDataReport)
        {
            var graphTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth()
                .SetMarginTop(5)
                .SetKeepTogether(true);
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGraphsTable1(patrolDataReport)));
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateGraphsTable2(patrolDataReport)));
            return graphTable;
        }

        private Table CreateGraphsTable1(PatrolDataReport patrolDataReport)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 33, 1, 32, 1, 33 })).UseAllAvailableWidth().SetMarginBottom(5);

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY SITE", "\nTotal Site Count: " + patrolDataReport.SitePercentage.Count));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY AREA/WARD", "\nTotal Area/Ward Count: " + patrolDataReport.AreaWardPercentage.Count));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("IR RECORDS PERCENTAGE BY COLOUR CODE", "\nTotal Color Code Count: " + patrolDataReport.ColorCodePercentage.Count));

            var sitesPieChartImage = GetChartImage(patrolDataReport.SitePercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(sitesPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var areaPieChartImage = GetChartImage(patrolDataReport.AreaWardPercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(areaPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var colorCodeChartImage = GetChartImage(patrolDataReport.ColorCodePercentage.OrderByDescending(z => z.Value).ToArray());
            chartDataTable.AddCell(GetChartImageCell(colorCodeChartImage));


            return chartDataTable;
        }

        private Table CreateGraphsTable2(PatrolDataReport patrolDataReport)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 })).UseAllAvailableWidth().SetMarginTop(5);

            var eventTypeCount = patrolDataReport.EventTypeQuantity.Sum(z => z.Value);
            chartDataTable.AddCell(GetChartHeaderCell("IR EVENT TYPE QUANTITY", "Total IR Count: " + eventTypeCount, 2));

            // 1015 (not 615): the old "options.width | 500" bitwise bug in ir-chart.js turned
            // 615 into 1015, and this chart's approved layout grew around that. Now that the
            // bug is fixed, pass the effective width explicitly so the chart stays identical.
            var eventTypePieChartImage = GetChartImage(patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray(), chartWidth: 1015);
            chartDataTable.AddCell(GetChartImageCell(eventTypePieChartImage).SetBorderRight(Border.NO_BORDER));

            var eventTypeBarChartImage = GetChartImage(patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray(), ChartType.Bar);
            chartDataTable.AddCell(GetChartImageCell(eventTypeBarChartImage).SetBorderLeft(Border.NO_BORDER));

            //var PyramidImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "Pyrimid.jpg"))).SetHorizontalAlignment(HorizontalAlignment.CENTER).SetHeight(250).SetMarginTop(20);
            //chartDataTable.AddCell(new Cell().Add(PyramidImage).SetBorder(Border.NO_BORDER));
            var PyramidImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "Pyrimid.jpg"))).SetHorizontalAlignment(HorizontalAlignment.CENTER).SetHeight(160).SetMarginTop(20);
            chartDataTable.AddCell(new Cell().Add(PyramidImage).SetBorder(Border.NO_BORDER));

            return chartDataTable;
        }
        private Table CreateWandGraphsTables(PatrolRequest ReportRequest)
        {
            var graphTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth()
                .SetMarginTop(5)
                .SetKeepTogether(true);
            graphTable.AddCell(new Cell()
                .SetPadding(0)
                .SetBorder(Border.NO_BORDER)
                .Add(CreateWandGraphsTable1(ReportRequest)));
         
            return graphTable;
        }
        private Table CreateWandGraphsTable1(PatrolRequest ReportRequest)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 49,2,49 })).UseAllAvailableWidth().SetMarginBottom(5);
            var dailyLogWandStrikeReportForSiteController = _guardLogDataProvider.GetGuardLogsWithWandStrikes(ReportRequest, true);

            // Strikes inside the requested report range. Used by BOTH charts below so the
            // two "Count" headers always agree with each other and with the web report.
            var filteredLogs = dailyLogWandStrikeReportForSiteController
 .Where(x => x.HitUtcDateTime.Date >= ReportRequest.FromDate.Date &&
             x.HitUtcDateTime.Date <= ReportRequest.ToDate.Date)
 .ToList();

            int totalStrikes = filteredLogs.Count;

            var dailySiteControllerWandStrikeData = BuildWandStrikeChartSeries(
                filteredLogs.Select(x => x.HitUtcDateTime.Date).ToList(),
                ReportRequest.FromDate.Date,
                ReportRequest.ToDate.Date);
            var individualFQWandStrikeDataList = _clientSiteWandDataProvider.GetClientSiteSmartWandTags()
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
            var individualFQWandStrikeData= individualFQWandStrikeDataList.Cast<dynamic>()
   .Select(x => new KeyValuePair<string, double>(
       (string)x.Wands,
       (double)x.Strikes))
   .OrderByDescending(x => x.Key)
   .ToArray();
            chartDataTable.AddCell(GetChartHeaderCell("SITE COMBINED WAND STRIKES", "\nCount: " + totalStrikes));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            chartDataTable.AddCell(GetChartHeaderCell("INDIVIDUAL WAND POINT FQ", "Count: " + individualFQWandStrikeDataList.Count));

            // Chronological series - do NOT re-sort by value, and use the Column chart:
            // day labels repeat (M T W T F S S), and the horizontal Bar chart's label-keyed
            // band scale collapses duplicate labels onto a single bar.
            // Client feedback: the chart must be stretched across its cell, so the image is
            // scaled to 100% of the cell width (fitCellWidth) instead of a fixed height.
            // chartWidth 800 (not the default 500) matches the source aspect ratio to the
            // ~377pt-wide cell on landscape A4, keeping the rendered height at ~150pt.
            var sitesColumnChartImage = GetChartImage(dailySiteControllerWandStrikeData, ChartType.Column, chartWidth: 800, fitCellWidth: true);
            chartDataTable.AddCell(GetChartImageCell(sitesColumnChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            // Client feedback: the pie must fill its cell like the column chart beside it.
            // Render on a 800x320 canvas (same 2.5:1 aspect as the ~377pt cell) and stretch
            // to the cell width: both charts come out the same height, the pie is centred in
            // the left half at near-full height, and the legend gets the right half with
            // clear margins instead of being squeezed into a 101pt thumbnail.
            var areaPieChartImage = GetChartImage(individualFQWandStrikeData.OrderByDescending(z => z.Value).ToArray(), ChartType.Pie, chartWidth: 800, fitCellWidth: true);
            chartDataTable.AddCell(GetChartImageCell(areaPieChartImage));

            // row 2 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            return chartDataTable;
        }
        /// <summary>
        /// Site Combined Wand Strikes series for the PDF, kept in step with the web report
        /// (PatrolData.BuildWandStrikeSeries). The series used to be hardcoded to 28 days
        /// ("always 4 weeks", dropping days 29-31 of a month) and sorted by value, which
        /// destroyed the chronological order. Buckets adapt to the range so the bars stay
        /// readable: daily up to ~1 month, weekly up to ~6 months, monthly beyond that.
        /// The daily bucket is padded to a fixed 31 slots (max month length) per client
        /// request, so every monthly chart has the same shape.
        /// </summary>
        private static KeyValuePair<string, double>[] BuildWandStrikeChartSeries(List<DateTime> hitDates, DateTime fromDate, DateTime toDate)
        {
            if (toDate < fromDate)
                toDate = fromDate;

            var totalDays = (toDate - fromDate).Days + 1;
            var series = new List<KeyValuePair<string, double>>();

            if (totalDays <= 31)
            {
                var byDay = hitDates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
                for (var day = fromDate; day <= toDate; day = day.AddDays(1))
                {
                    byDay.TryGetValue(day, out int strikes);
                    series.Add(new KeyValuePair<string, double>(day.ToString("dddd")[0].ToString(), strikes)); // MTWTFSS
                }

                // Client feedback: a month has 31 days MAX, so the daily axis is FIXED at
                // 31 slots. Shorter months / ranges get unlabeled empty slots at the end,
                // keeping the bar width and chart shape identical from month to month.
                while (series.Count < 31)
                    series.Add(new KeyValuePair<string, double>(string.Empty, 0));
            }
            else if (totalDays <= 182)
            {
                var byWeek = hitDates.GroupBy(d => (d - fromDate).Days / 7).ToDictionary(g => g.Key, g => g.Count());
                var weekCount = (totalDays + 6) / 7;
                for (var week = 0; week < weekCount; week++)
                {
                    byWeek.TryGetValue(week, out int strikes);
                    series.Add(new KeyValuePair<string, double>(fromDate.AddDays(week * 7).ToString("dd/MM"), strikes)); // week starting
                }
            }
            else
            {
                var byMonth = hitDates.GroupBy(d => new DateTime(d.Year, d.Month, 1)).ToDictionary(g => g.Key, g => g.Count());
                for (var month = new DateTime(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1))
                {
                    byMonth.TryGetValue(month, out int strikes);
                    series.Add(new KeyValuePair<string, double>(month.ToString("MMM yy"), strikes));
                }
            }

            return series.ToArray();
        }

        private Cell GetChartHeaderCell(string leftText, string rightText, int colspan = 1)
        {
            var cell = new Cell(1, colspan)
               .SetFont(PdfHelper.GetPdfFont())
               .SetFontSize(CELL_FONT_SIZE)
               .SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))
               .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY))
               .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            var p = new Paragraph(leftText);
            p.Add(new Tab());
            p.AddTabStops(new TabStop(1000, TabAlignment.RIGHT));
            p.Add(new Text(rightText).SetFontSize(4.5f));
            cell.Add(p);

            return cell;
        }

        private Cell GetChartImageCell(Image chartImage, int colspan = 1)
        {
            var imageCell = new Cell(1, colspan);
            if (chartImage != null)
                imageCell.Add(chartImage).SetVerticalAlignment(VerticalAlignment.MIDDLE);

            return imageCell;
        }
        private Image GetChartImage(KeyValuePair<string, double>[] data, ChartType chartType = ChartType.Pie, int? chartWidth = null, float displayHeight = 101f, bool fitCellWidth = false)
        {
            var modifiedData = data;
            if (data.All(z => z.Value == 0))
            {
                modifiedData = new KeyValuePair<string, double>[]
                {
                    new KeyValuePair<string, double>("no/data", 100)
                };
            }


            try
            {
                var graphFileName = IO.Path.Combine(_graphImageRootDir, $"{DateTime.Now:ddMMyyyy_HHmmss}.png");
              
                var options = new { type = chartType, fileName = graphFileName, width = chartWidth };

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/ir-chart.js", "drawChart", args: new object[] { options, modifiedData });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                // fitCellWidth: stretch across the full cell width, height follows the
                // source aspect ratio. Otherwise fixed height, width follows.
                var graphImage = new Image(ImageDataFactory.Create(graphFileName));
                if (fitCellWidth)
                    graphImage.SetWidth(UnitValue.CreatePercentValue(100));
                else
                    graphImage.SetHeight(displayHeight);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch(Exception ex)
            {
                // no ops
            }
            return null;
        }
        //NEWLY ADDED END

    }
}
