using CityWatch.Data.Enums;
using CityWatch.Data.Helpers;
using CityWatch.Data.Models;
using CityWatch.Data.Services;
using CityWatch.Kpi.Models;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IO = System.IO;

namespace CityWatch.Kpi.Services
{
    public class WeeklySummaryReportGenerator : ISummaryReportGenerator
    {
        private const float CELL_FONT_SIZE = 6f;
        private const float PDF_DOC_MARGIN = 75f;
        private const string REPORT_DIR = "Output";
        private const int MAX_SITES_PER_PAGE = 8;
        private const int MAX_SITES_PER_PAGE_FOR_FOOTER = 7;

        private const string CELL_BG_GREEN = "#96e3ac";
        private const string CELL_BG_RED = "#ffcccc";
        private const string CELL_HEADER_BLUE = "#bdd7ee";
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_GREY = "#666362";

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;
        private readonly IViewDataService _viewDataService;
        private readonly IPatrolDataReportService _patrolDataReportService;
        public WeeklySummaryReportGenerator(IWebHostEnvironment webHostEnvironment,
            IViewDataService viewDataService, IPatrolDataReportService patrolDataReportService)
        {
            _viewDataService = viewDataService;
            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            //nEWLY ADDAED-START
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            _patrolDataReportService = patrolDataReportService;
            //nEWLY ADDAED-END
        }

        public string GeneratePdfReport(KpiSendSchedule schedule, DateTime fromDate, DateTime toDate)
        {
            var reportFileName = $"{DateTime.Now.ToString("yyyyMMdd")} -  KPI Summary Report - {fromDate.ToString("MMM")} {fromDate.Year}_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            var doc = new Document(pdfDoc);
            doc.SetLeftMargin(PDF_DOC_MARGIN);
            doc.SetRightMargin(PDF_DOC_MARGIN);

            var tableReportHeader = CreateReportHeader(schedule.ProjectName, toDate.ToString("MMMM yyyy"));
            doc.Add(tableReportHeader);

            var clientSiteIds = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSiteId).ToArray();
            var summaryData = _viewDataService.GetKpiReportData(clientSiteIds, fromDate, toDate);
            var totalSitePrinted = CreateSummaryTable(summaryData, doc, toDate, schedule.IsHrTimerPaused);

            if (totalSitePrinted > MAX_SITES_PER_PAGE_FOR_FOOTER)
                doc.Add(new AreaBreak());
            //NEWLY ADDED-START
            var patrolDataReport = _patrolDataReportService.GetDailyPatrolData(new PatrolRequest()
            {
                FromDate = fromDate,
                ToDate = toDate,
                DataFilter = PatrolDataFilter.Custom,
                ClientSites = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSite.Name).ToArray(),
            });

            if (patrolDataReport.ResultsCount > 0)
            {
                doc.Add(new AreaBreak());
                doc.Add(tableReportHeader);
                PatrolRequest ReportRequest = new PatrolRequest();
                ReportRequest.FromDate = fromDate;
                ReportRequest.ToDate = toDate;

                ReportRequest.ClientSites = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSite.Name).ToArray() ;
                ReportRequest.ClientTypes = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSite.ClientType.Name).ToArray();
                var hrGraphsTable = CreateHRGraphsTables(ReportRequest);
                doc.Add(hrGraphsTable);
                var graphsTable = CreateGraphsTables(patrolDataReport);
                doc.Add(graphsTable);
            }
            //NEWLY ADDED-END
            
            var tableLegend = CreateLegend();
            doc.Add(tableLegend);

            //var tableNotes = CreateNotes(schedule.SummaryNote1, schedule.SummaryNote2);
            //doc.Add(tableNotes);

            doc.Close();
            pdfDoc.Close();
            return reportFileName;
        }

        private Table CreateLegend()
        {
            var legendTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            var cellLegend = new Cell()
                .SetFontSize(CELL_FONT_SIZE * 0.9f)
                .SetPadding(5)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph().Add(new Text("LEGEND").SetUnderline()))
                .Add(new Paragraph().Add(new Text("PASS").SetFontColor(WebColors.GetRGBColor("#2FB254"))).Add(new Text(" = Required KPI were met")))
                .Add(new Paragraph().Add(new Text("ONGOING").SetFontColor(WebColors.GetRGBColor("#d19404"))).Add(new Text(" = Some Data exists from the AM shift; shift is split;  Nightshift is expected to “top up” short fall to reach KPI")))
                .Add(new Paragraph().Add(new Text("FAIL").SetFontColor(WebColors.GetRGBColor("#FF323A"))).Add(new Text(" = Required KPI not met ‐ needs to be investigated to determine if IR exists to explain situation, if there is a technical fault, or if guard failed performance")))
                .Add(new Paragraph().Add(new Text("N/A").SetFontColor(WebColors.GetRGBColor("#928382"))).Add(new Text(" = Weekend only site or no fixed shift (ADHOC support)")))
                .Add(new Paragraph().Add(new Text("IR").SetFontColor(WebColors.GetRGBColor("#000000"))).Add(new Text(" = How many Incident Reports were lodged or created; default value is 0")));
            legendTable.AddCell(cellLegend);
            return legendTable;
        }

        private Table CreateNotes(string note1, string note2)
        {
            var noteTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            var cellSummaryNote1 = new Cell()
                .SetFontSize(CELL_FONT_SIZE * 0.9f)
                .Add(new Paragraph(note1 ?? string.Empty));
            noteTable.AddCell(cellSummaryNote1);
            var cellSummaryNote2 = new Cell()
                .SetFontSize(CELL_FONT_SIZE * 0.9f)
                .Add(new Paragraph(note2 ?? string.Empty));
            noteTable.AddCell(cellSummaryNote2);
            return noteTable;
        }

        private Table CreateReportHeader(string projectName, string reportMonth)
        {
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 60, 20 })).UseAllAvailableWidth();

            var cellSiteImage = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            headerTable.AddCell(cellSiteImage);

            var cellReportTitle = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("SITE KPI Telematics Report\n").SetFont(PdfHelper.GetPdfFont()).SetFontSize(CELL_FONT_SIZE * 1.2f))
                .Add(new Paragraph("Engine v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()).SetFontSize(CELL_FONT_SIZE))
                .Add(new Paragraph(projectName)).SetFontSize(CELL_FONT_SIZE)
                .Add(new Paragraph(reportMonth)).SetFontSize(CELL_FONT_SIZE);
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

        private void CreateSummaryHeader(Table table)
        {
            table.AddHeaderCell(new Paragraph().Add("Location")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE).SetTextAlignment(TextAlignment.CENTER);
            table.AddHeaderCell(new Paragraph().Add("DAY")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("DATE")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("KPI \nfor FLIR")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("KPI \nfor WAND")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("Daily \nLog Timer")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("IR \nLodged?")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("ALARM or FIRE")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("Notes")).SetBackgroundColor(WebColors.GetRGBColor(CELL_HEADER_BLUE)).SetFontSize(CELL_FONT_SIZE);
        }

        private int CreateSummaryTable(List<DailyKpiResult> summaryData, Document doc, DateTime toDate, bool isHrTimerPaused)
        {
            var totalSitePrinted = 0;
            var tableWidth = UnitValue.CreatePercentArray(new float[] { 21, 9, 6, 9, 9, 5, 5, 5, 31 });
            var table = new Table(tableWidth).UseAllAvailableWidth();

            CreateSummaryHeader(table);

            foreach (var clientData in summaryData.GroupBy(x => x.ClientSiteKpiSetting.ClientSite.TypeId))
            {
                foreach (var siteData in clientData.GroupBy(z => z.ClientSiteId))
                {
                    if (totalSitePrinted > MAX_SITES_PER_PAGE)
                    {
                        doc.Add(table);
                        doc.Add(new AreaBreak());
                        table = new Table(tableWidth).UseAllAvailableWidth();
                        CreateSummaryHeader(table);
                        totalSitePrinted = 0;
                    }

                    var clientSiteSetting = siteData.First().ClientSiteKpiSetting;
                    var siteDataArray = siteData.ToArray();
                    for (var row = 0; row < 7; row++)
                    {
                        if (row == 0)
                        {
                            var cellSiteDetails = new Cell(7, 1)
                                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                                .SetFontSize(CELL_FONT_SIZE)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .Add(new Paragraph(clientSiteSetting.ClientSite.Name).SetPaddingBottom(3));
                            var siteImage = GetSiteImage(clientSiteSetting);
                            if (siteImage != null)
                                cellSiteDetails.Add(siteImage);

                            table.AddCell(cellSiteDetails);
                        }

                        if (siteDataArray.Length - row > 0)
                        {
                            var dailySiteData = siteDataArray[row];
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(dailySiteData.NameOfDay.ToString())));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(dailySiteData.Date.Day.ToString())));
                            table.AddCell(GetKpiImageStatusCell(dailySiteData));
                            table.AddCell(GetKpiWandScanStatusCell(dailySiteData));
                            table.AddCell(GetKpiDailyLogTimerCell(dailySiteData, isHrTimerPaused));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(dailySiteData.IncidentCount.ToString())));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(string.IsNullOrEmpty(dailySiteData.HasFireOrAlarm) ? "0" : "1")));
                        }
                        else
                        {
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("0").SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("0").SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("0").SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("0").SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("0").SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))));
                            table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph("0").SetFontColor(WebColors.GetRGBColor(COLOR_WHITE))));
                        }

                        if (row == 0)
                        {
                            var monthNote = clientSiteSetting.Notes?.SingleOrDefault(z => z.ForMonth == new DateTime(toDate.Year, toDate.Month, 1))?.Notes ?? string.Empty;
                            table.AddCell(new Cell(7, 1)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                                .SetFontSize(CELL_FONT_SIZE)
                                .SetPadding(0)
                                .Add(new Paragraph(monthNote)));
                        }
                    }

                    totalSitePrinted++;
                }

                table.AddCell(new Cell(1, 9)
                    .SetPadding(3)
                    .SetBackgroundColor(WebColors.GetRGBColor("#c3c3c3")));
            }

            doc.Add(table);
            return totalSitePrinted;
        }

        private Image GetSiteImage(ClientSiteKpiSetting clientSiteSetting)
        {
            if (!string.IsNullOrEmpty(clientSiteSetting.SiteImage))
            {
                var siteImagePath = IO.Path.Combine(_siteImageRootDir, IO.Path.GetFileName(clientSiteSetting.SiteImage));
                if (IO.File.Exists(siteImagePath))
                {
                    return new Image(ImageDataFactory.Create(siteImagePath))
                        .SetHeight(42)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                }
            }
            return null;
        }

        private Cell GetKpiWandScanStatusCell(DailyKpiResult dailyKpiResult)
        {
            var text = string.Empty;
            var color = string.Empty;
            if (dailyKpiResult.EffectiveEmployeeHours == 0)
            {
                text = "N/A";
                color = "#928382";
            }
            else if (dailyKpiResult.WandScanCountPerHr >= dailyKpiResult.WandScansTarget)
            {
                text = "PASS";
                color = "#2FB254";
            }
            else if (dailyKpiResult.WandScanCountPerHr < dailyKpiResult.WandScansTarget && dailyKpiResult.Date < DateTime.Today)
            {
                text = "FAIL";
                color = "#FF323A";
            }
            else if (dailyKpiResult.WandScanCountPerHr < dailyKpiResult.WandScansTarget && dailyKpiResult.Date == DateTime.Today)
            {
                text = "ONGOING";
                color = "#d19404";
            }
            return new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).SetFontColor(WebColors.GetRGBColor(color)).Add(new Paragraph(text));
        }

        private Cell GetKpiImageStatusCell(DailyKpiResult dailyKpiResult)
        {
            var text = string.Empty;
            var color = string.Empty;
            if (dailyKpiResult.EffectiveEmployeeHours == 0)
            {
                text = "N/A";
                color = "#928382";
            }
            else if (dailyKpiResult.ImageCountPerHr >= dailyKpiResult.ImagesTarget)
            {
                text = "PASS";
                color = "#2FB254";
            }
            else if (dailyKpiResult.ImageCountPerHr < dailyKpiResult.ImagesTarget && dailyKpiResult.Date < DateTime.Today)
            {
                text = "FAIL";
                color = "#FF323A";
            }
            else if (dailyKpiResult.ImageCountPerHr < dailyKpiResult.ImagesTarget && dailyKpiResult.Date == DateTime.Today)
            {
                text = "ONGOING";
                color = "#d19404";
            }
            return new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).SetFontColor(WebColors.GetRGBColor(color)).Add(new Paragraph(text));
        }

        private Cell GetKpiDailyLogTimerCell(DailyKpiResult dailyKpiResult, bool isHrTimerPaused)
        {
            string text = "-";
            var bgColor = COLOR_WHITE;
            if (!isHrTimerPaused && dailyKpiResult.IsAcceptableLogFreq.HasValue)
            {
                bgColor = dailyKpiResult.IsAcceptableLogFreq.Value ? CELL_BG_GREEN : CELL_BG_RED;
                text = dailyKpiResult.IsAcceptableLogFreq.Value ? "< 2hr" : "> 2hr";
            }
            return new Cell().SetBackgroundColor(WebColors.GetRGBColor(bgColor)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).SetFontColor(WebColors.GetRGBColor("#000000")).Add(new Paragraph(text));
        }
        //NEWLY ADDED-START
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
                .Add(CreateHRGraphsTable1(guardIds)));
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
        private Table CreateHRGraphsTable1(int[]? guardIds)
        {
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 49, 2, 49 })).UseAllAvailableWidth().SetMarginBottom(5);

            var activeAndInActive = GetActiveAndInactiveGuardHrReport(guardIds).ToList();
            chartDataTable.AddCell(GetChartHeaderCell("Active Guard Vs Inactive Guard", " (Count: " + activeAndInActive.Count() + ")"));

            // row 1 blank cell
            chartDataTable.AddCell(new Cell().SetBorder(Border.NO_BORDER));

            var genderReport = GetGenderBasedGuardHrReport(guardIds).ToList();

            chartDataTable.AddCell(GetChartHeaderCell("Gender", "(Count: " + genderReport.Count() + ")"));

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

            var hrChartData1BarChartImage = GetChartImage(hrChartData1, ChartType.Bar);
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
            var chartDataTable = new Table(UnitValue.CreatePercentArray(new float[] { 49, 51 })).UseAllAvailableWidth().SetMarginBottom(5);

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

            var eventTypePieChartImage = GetChartImage(patrolDataReport.EventTypePercentage.OrderBy(z => z.Key).ToArray(), chartWidth: 615);
            chartDataTable.AddCell(GetChartImageCell(eventTypePieChartImage).SetBorderRight(Border.NO_BORDER));

            var eventTypeBarChartImage = GetChartImage(patrolDataReport.EventTypeQuantity.OrderBy(z => z.Key).ToArray(), ChartType.Bar);
            chartDataTable.AddCell(GetChartImageCell(eventTypeBarChartImage).SetBorderLeft(Border.NO_BORDER));

            return chartDataTable;
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

        private Cell GetChartImageCell(Image chartImage)
        {
            var imageCell = new Cell();
            if (chartImage != null)
                imageCell.Add(chartImage).SetVerticalAlignment(VerticalAlignment.MIDDLE);

            return imageCell;
        }
        private Image GetChartImage(KeyValuePair<string, double>[] data, ChartType chartType = ChartType.Pie, int? chartWidth = null)
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
                var graphFileName = IO.Path.Combine(_graphImageRootDir, $"{DateTime.Now: ddMMyyyy_HHmmss}.png");
                var options = new { type = chartType, fileName = graphFileName, width = chartWidth };

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/ir-chart.js", "drawChart", args: new object[] { options, data });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(90);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch
            {
                // no ops
            }
            return null;
        }
        //NEWLY ADDED END
    }
}
