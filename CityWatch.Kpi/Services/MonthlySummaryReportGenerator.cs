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
using System.IO;
using System.Linq;
using System.Reflection;
using IO = System.IO;

namespace CityWatch.Kpi.Services
{
    public enum ChartType
    {
        Pie = 1,

        Bar
    }

    public class MonthlySummaryReportGenerator : ISummaryReportGenerator
    {
        private const float CELL_FONT_SIZE = 6f;
        private const float PDF_DOC_MARGIN = 45f;
        private const string REPORT_DIR = "Output";
        private const int MAX_SITES_PER_PAGE_FOR_FOOTER = 10;

        private const string HEADER_COLOR_BLUE = "#bdd7ee";
        private const string COLOR_WHITE = "#ffffff";
        private const string COLOR_PASS = "#2FB254";
        private const string COLOR_FAIL = "#FF323A";
        private const string COLOR_DEFAULT = "#000000";
        private const string COLOR_ORANGE = "#EA6326";
        private const string COLOR_GREY = "#666362";

        const string PASS_TEXT = "PASS";
        const string FAIL_TEXT = "FAIL";

        private readonly string _reportRootDir;
        private readonly string _imageRootDir;
        private readonly string _siteImageRootDir;
        private readonly string _graphImageRootDir;
        private readonly string _summaryImageDir;

        private readonly IViewDataService _viewDataService;
        private readonly IPatrolDataReportService _patrolDataReportService;

        public MonthlySummaryReportGenerator(IWebHostEnvironment webHostEnvironment,
           IViewDataService viewDataService,
           IPatrolDataReportService patrolDataReportService)
        {
            _viewDataService = viewDataService;
            _patrolDataReportService = patrolDataReportService;

            _reportRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "Pdf");
            _imageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "images");
            _siteImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SiteImage");
            _graphImageRootDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "GraphImage");
            _summaryImageDir = IO.Path.Combine(webHostEnvironment.WebRootPath, "SummaryImage");

            if (!IO.Directory.Exists(_graphImageRootDir))
                IO.Directory.CreateDirectory(_graphImageRootDir);
        }

        public string GeneratePdfReport(KpiSendSchedule schedule, DateTime fromDate, DateTime toDate)
        {
            var reportFileName = $"{DateTime.Now:yyyyMMdd} -  KPI Summary Report - {fromDate:MMM} {fromDate.Year}_{new Random().Next()}.pdf";
            var reportPdf = IO.Path.Combine(_reportRootDir, REPORT_DIR, reportFileName);
            var pdfDoc = new PdfDocument(new PdfWriter(reportPdf));
            var doc = new Document(pdfDoc);

            doc.SetLeftMargin(0);
            doc.SetRightMargin(0);

            var frontCoverTable = CreateFrontCoverTable(schedule.ProjectName, toDate.ToString("MMMM yyyy"));
            doc.Add(frontCoverTable);

            doc.SetLeftMargin(PDF_DOC_MARGIN);
            doc.SetRightMargin(PDF_DOC_MARGIN);

            var summaryTable = CreateSummaryTable(schedule, toDate);
            doc.Add(summaryTable);

            CreateSummaryNoteFooterImages(doc);

            doc.Add(new AreaBreak());

            var tableReportHeader = CreateReportHeader(schedule.ProjectName, toDate.ToString("MMMM yyyy"));
            doc.Add(tableReportHeader);

            var clientSiteIds = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSiteId).ToArray();
            var monthlyKpiReportData = _viewDataService.GetMonthlyKpiReportData(clientSiteIds, fromDate, toDate);
            var totalSitePrinted = CreateSummaryTable(monthlyKpiReportData, doc, fromDate, schedule.IsHrTimerPaused);

            if (totalSitePrinted > MAX_SITES_PER_PAGE_FOR_FOOTER)
                doc.Add(new AreaBreak());

            var patrolDataReport = _patrolDataReportService.GetDailyPatrolData(new PatrolRequest()
            {
                FromDate = fromDate,
                ToDate = toDate,
                DataFilter = PatrolDataFilter.Custom,
                ClientSites = schedule.KpiSendScheduleClientSites.Select(z => z.ClientSite.Name).ToArray(),
            });
                        
            var tableLegend = CreateLegend();
            doc.Add(tableLegend);

            if (!(string.IsNullOrEmpty(schedule.SummaryNote1) && string.IsNullOrEmpty(schedule.SummaryNote2)))
            {
                var tableNotes = CreateNotes(schedule.SummaryNote1, schedule.SummaryNote2);
                doc.Add(tableNotes);
            }

            if (patrolDataReport.ResultsCount > 0)
            {                
                doc.Add(new AreaBreak());
                doc.Add(tableReportHeader);
                var graphsTable = CreateGraphsTables(patrolDataReport);
                doc.Add(graphsTable);
            }

            doc.Close();
            pdfDoc.Close();
            return reportFileName;
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

        private Table CreateFrontCoverTable(string projectName, string reportMonth)
        {
            var frontCoverTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();

            var logoImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogoPdf.png")))
               .SetHeight(75)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            var cellLogoImage = new Cell()
                .Add(logoImage)
                .SetBorder(Border.NO_BORDER)
                .SetPaddingBottom(24f);
            frontCoverTable.AddCell(cellLogoImage);

            var coverPageHeading = new Cell()
                .Add(new Paragraph().Add(new Text("SITE KPI Telematics Report")))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 2.5f)
                .SetBorder(Border.NO_BORDER)
                .SetPaddingBottom(20f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            frontCoverTable.AddCell(coverPageHeading);

            var coverPageProjectName = new Cell()
                .Add(new Paragraph().Add(new Text(projectName)))
                .SetFont(PdfHelper.GetPdfFont())
                .SetFontSize(CELL_FONT_SIZE * 4f)
                .SetFontColor(WebColors.GetRGBColor(COLOR_ORANGE))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(20f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            frontCoverTable.AddCell(coverPageProjectName);

            var coverPageReportingPeriod = new Cell()
                 .Add(new Paragraph().Add(new Text("Reporting Period : ").SetFont(PdfHelper.GetPdfFont())).Add(new Text(reportMonth)))
                 .SetBorder(Border.NO_BORDER)
                 .SetPaddingBottom(6f)
                 .SetTextAlignment(TextAlignment.CENTER)
                 .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            frontCoverTable.AddCell(coverPageReportingPeriod);

            var coverPageReleaseDate = new Cell()
                .Add(new Paragraph().Add(new Text("Release : ").SetFont(PdfHelper.GetPdfFont())).Add(new Text(DateTime.Now.ToString("dddd, dd MMMM yyyy"))))
                .SetBorder(Border.NO_BORDER)
                .SetPaddingBottom(50f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            frontCoverTable.AddCell(coverPageReleaseDate);

            var coverPageImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "cwsbanner.png")))
               .SetHeight(250)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            var cellCoverPageImage = new Cell()
                .Add(coverPageImage)
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_GREY))
                .SetBorder(Border.NO_BORDER)
                .SetPaddingTop(40f)
                .SetPaddingBottom(40f);
            frontCoverTable.AddCell(cellCoverPageImage);

            var isoImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "ISOv3.jpg")))
              .SetHeight(30)
              .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            var cellIsoImage = new Cell()
               .Add(isoImage)
               .SetBorder(Border.NO_BORDER)
               .SetPaddingTop(40f)
               .SetPaddingRight(40f);
            frontCoverTable.AddCell(cellIsoImage);

            return frontCoverTable;
        }

        private Table CreateSummaryTable(KpiSendSchedule schedule, DateTime toDate)
        {
            var noteCellPaddingTop = 200f;
            var summaryTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            var summaryImage = schedule.KpiSendScheduleSummaryImage?.FileName;
            if (!string.IsNullOrEmpty(summaryImage) && File.Exists(Path.Combine(_summaryImageDir, summaryImage)))
            {
                var summaryImageTable = CreateSummaryImageTable(Path.Combine(_summaryImageDir, summaryImage));
                summaryTable.AddCell(new Cell().Add(summaryImageTable).SetBorder(Border.NO_BORDER));
                noteCellPaddingTop = 0f;
            }
            var summaryNoteTable = CreateSummaryNoteTable(schedule, toDate);
            summaryTable.AddCell(new Cell().Add(summaryNoteTable).SetBorder(Border.NO_BORDER).SetPaddingTop(noteCellPaddingTop));

            return summaryTable;
        }

        private Table CreateSummaryImageTable(string summaryImageFileName)
        {
            var summaryImageTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();

            var summaryImage = new Image(ImageDataFactory.Create(summaryImageFileName))
                .SetAutoScaleWidth(true)
               .SetHorizontalAlignment(HorizontalAlignment.CENTER);
            var cellSummaryImage = new Cell()
                .Add(summaryImage)
                .SetBorder(Border.NO_BORDER);
            summaryImageTable.AddCell(cellSummaryImage);

            return summaryImageTable;
        }

        private Table CreateSummaryNoteTable(KpiSendSchedule schedule, DateTime toDate)
        {
            var summaryNoteTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();

            var summaryNoteHeading = new Cell()
               .Add(new Paragraph().Add(new Text("SUMMARY").SetFont(PdfHelper.GetPdfFont())))
               .SetFontSize(CELL_FONT_SIZE * 2.5f)
               .SetTextAlignment(TextAlignment.CENTER)
               .SetPaddingTop(50f)
               .SetPaddingBottom(35f)
               .SetBorder(Border.NO_BORDER);
            summaryNoteTable.AddCell(summaryNoteHeading);

            var summaryNoteMonthYear = new Cell()
                .Add(new Paragraph().Add(new Text(toDate.ToString("MMMM yyyy")).SetFont(PdfHelper.GetPdfFont())))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetPaddingBottom(6f)
                .SetBorder(Border.NO_BORDER);
            summaryNoteTable.AddCell(summaryNoteMonthYear);

            var monthNote = schedule.KpiSendScheduleSummaryNotes?.SingleOrDefault(z => z.ForMonth == new DateTime(toDate.Year, toDate.Month, 1))?.Notes ?? "N/A";
            var summaryNote = new Cell()
                .Add(new Paragraph().Add(new Text(monthNote)))
                .SetFontSize(CELL_FONT_SIZE * 1.5f)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetPaddingBottom(10f)
                .SetBorder(Border.NO_BORDER);
            summaryNoteTable.AddCell(summaryNote);

            return summaryNoteTable;
        }

        private void CreateSummaryNoteFooterImages(Document doc)
        {
            var cwsLogoImage = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "CWSLogo.png")))
                .SetFixedPosition(35f, 20f)
                .SetHeight(30)
                .SetHorizontalAlignment(HorizontalAlignment.LEFT);
            doc.Add(cwsLogoImage);

            var isoV3Image = new Image(ImageDataFactory.Create(IO.Path.Combine(_imageRootDir, "ISOv3.jpg")))
                .SetFixedPosition(420f, 20f)
               .SetHeight(30)
               .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            doc.Add(isoV3Image);
        }

        private int CreateSummaryTable(Dictionary<int, MonthlyKpiResult> monthlyKpiReportData, Document doc, DateTime fromDate, bool isHrTimerPaused)
        {
            var tableWidth = UnitValue.CreatePercentArray(new float[] { 21, 8, 8, 8, 8, 8, 8, 8, 23 });
            var table = new Table(tableWidth).UseAllAvailableWidth();

            CreateSummaryHeader(table);

            foreach (var clientMonthlyData in monthlyKpiReportData)
            {
                var clientData = clientMonthlyData.Value;

                if (clientData.ClientSiteKpiSetting == null)
                    continue;

                for (int row = 0; row < 2; row++)
                {
                    if (row == 0)
                    {
                        var cellSiteDetails = new Cell(2, 1)
                            .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                            .SetFontSize(CELL_FONT_SIZE)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .Add(new Paragraph(clientData.ClientSiteKpiSetting.ClientSite.Name).SetPaddingBottom(3));
                        var siteImage = GetSiteImage(clientData.ClientSiteKpiSetting);
                        if (siteImage != null)
                            cellSiteDetails.Add(siteImage);
                        table.AddCell(cellSiteDetails);
                    }

                    var shiftFilledVsRoster = clientData.ShiftFilledVsRosterPercentage;
                    var logReportsVsRoster = clientData.LogReportsVsRosterPercentage;
                    var guardCompetency = "See Excel";
                    var kpiForFlir = clientData.ImageCountPercentage;
                    var kpiForWand = clientData.WandScanPercentage;
                    var irLodged = clientData.IrCountTotal;
                    var alarmOrFire = clientData.AlarmCountTotal;
                    var uploadGuardLogEnabled = clientData.ClientSiteKpiSetting.ClientSite.UploadGuardLog;

                    if (row == 0)
                    {
                        table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{shiftFilledVsRoster.GetValueOrDefault():0.00} %")));
                        var logReportsVsRosterValue = isHrTimerPaused ? 0 : logReportsVsRoster.GetValueOrDefault();
                        table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{logReportsVsRosterValue:0.00} %")));
                        table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph(guardCompetency)));
                        table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{kpiForFlir.GetValueOrDefault():0.00} %")));
                        table.AddCell(new Cell().SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{kpiForWand.GetValueOrDefault():0.00} %")));
                        table.AddCell(new Cell(2, 1).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{irLodged}")));
                        table.AddCell(new Cell(2, 1).SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE)).SetTextAlignment(TextAlignment.CENTER).SetPadding(0).SetFontSize(CELL_FONT_SIZE).Add(new Paragraph($"{alarmOrFire}")));
                    }
                    else if (row == 1)
                    {
                        var logReportsVsRosterStatus = "N/A";
                        var logReportsVsRosterColor = COLOR_DEFAULT;
                        if (!isHrTimerPaused)
                        {
                            logReportsVsRosterStatus = uploadGuardLogEnabled ? (logReportsVsRoster.HasValue ? (logReportsVsRoster >= 100 ? PASS_TEXT : FAIL_TEXT) : "N/A") : "N/A";
                            logReportsVsRosterColor = uploadGuardLogEnabled ? (logReportsVsRoster.HasValue ? (logReportsVsRoster >= 100 ? COLOR_PASS : COLOR_FAIL) : COLOR_DEFAULT) : COLOR_DEFAULT;
                        }
                        CreateKpiStatusCell(table, shiftFilledVsRoster.HasValue ? (shiftFilledVsRoster >= 100 ? PASS_TEXT : FAIL_TEXT) : "N/A", shiftFilledVsRoster.HasValue ? (shiftFilledVsRoster >= 100 ? COLOR_PASS : COLOR_FAIL) : COLOR_DEFAULT);
                        CreateKpiStatusCell(table, logReportsVsRosterStatus, logReportsVsRosterColor);
                        CreateKpiStatusCell(table, string.Empty, string.Empty);
                        CreateKpiStatusCell(table, kpiForFlir.HasValue ? (kpiForFlir >= 100 ? PASS_TEXT : FAIL_TEXT) : "N/A", kpiForFlir.HasValue ? (kpiForFlir >= 100 ? COLOR_PASS : COLOR_FAIL) : COLOR_DEFAULT);
                        CreateKpiStatusCell(table, kpiForWand.HasValue ? (kpiForWand >= 100 ? PASS_TEXT : FAIL_TEXT) : "N/A", kpiForWand.HasValue ? (kpiForWand >= 100 ? COLOR_PASS : COLOR_FAIL) : COLOR_DEFAULT);
                    }

                    if (row == 0)
                    {
                        var monthNote = clientData.ClientSiteKpiSetting.Notes?.SingleOrDefault(z => z.ForMonth == new DateTime(fromDate.Year, fromDate.Month, 1))?.Notes ?? string.Empty;
                        table.AddCell(new Cell(2, 1)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                                .SetFontSize(CELL_FONT_SIZE)
                                .SetPadding(0)
                                .Add(new Paragraph(monthNote)));
                    }
                }
            }
            doc.Add(table);
            return monthlyKpiReportData.Count;
        }

        private void CreateKpiStatusCell(Table table, string text, string color)
        {
            table.AddCell(new Cell()
                .SetBackgroundColor(WebColors.GetRGBColor(COLOR_WHITE))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(0).SetFontSize(CELL_FONT_SIZE)
                .SetFontColor(WebColors.GetRGBColor(color))
                .Add(new Paragraph(text)));
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

        private Table CreateLegend()
        {
            var legendTable = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            var cellLegend = new Cell()
                .SetFontSize(CELL_FONT_SIZE * 0.9f)
                .SetPadding(5)
                .SetBorder(Border.NO_BORDER)
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

            var cellSiteImage = new Cell().SetBorder(Border.NO_BORDER);
            headerTable.AddCell(cellSiteImage);

            var cellReportTitle = new Cell()
                .SetBorder(Border.NO_BORDER)
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
                .SetBorder(Border.NO_BORDER)
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            headerTable.AddCell(cellLogoImage);

            headerTable.AddCell(new Cell(1, 3).SetPadding(3).SetBorder(Border.NO_BORDER));
            return headerTable;
        }

        private void CreateSummaryHeader(Table table)
        {
            table.AddHeaderCell(new Paragraph().Add("Location")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE).SetTextAlignment(TextAlignment.CENTER);
            table.AddHeaderCell(new Paragraph().Add("Shifts Filled\nVs. Roster")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("Log Reports \nVs. Roster")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("Guard Competency \n(Compliance)")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("KPI \nfor FLIR")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("KPI \nfor WAND")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("IR \nLodged?")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("ALARM or FIRE")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
            table.AddHeaderCell(new Paragraph().Add("Notes")).SetBackgroundColor(WebColors.GetRGBColor(HEADER_COLOR_BLUE)).SetFontSize(CELL_FONT_SIZE);
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

                var task = StaticNodeJSService.InvokeFromFileAsync<string>("Scripts/ir-chart.js", "drawChart", args: new object[] { options, modifiedData });
                var success = task.Result == "OK";

                if (!success)
                    throw new ApplicationException("Create graph failed");

                if (success && !IO.File.Exists(graphFileName))
                    throw new ApplicationException($"Graph image not found. File Name: {graphFileName}");

                var graphImage = new Image(ImageDataFactory.Create(graphFileName)).SetHeight(101);

                IO.File.Delete(graphFileName);

                return graphImage;
            }
            catch
            {
                // no ops
            }
            return null;
        }
    }
}